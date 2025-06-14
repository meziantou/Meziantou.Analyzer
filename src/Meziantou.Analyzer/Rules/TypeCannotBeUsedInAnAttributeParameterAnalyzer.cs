using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeCannotBeUsedInAnAttributeParameterAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.TypeCannotBeUsedInAnAttributeParameter,
        title: "Type cannot be used as an attribute argument",
        messageFormat: "Type cannot be used as an attribute argument",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.TypeCannotBeUsedInAnAttributeParameter));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);
            if (!analyzerContext.IsValid)
                return;

            context.RegisterSymbolAction(context =>
            {
                var method = (IMethodSymbol)context.Symbol;
                if (method.MethodKind is MethodKind.Constructor && method.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal && analyzerContext.IsAttribute(method))
                {
                    foreach (var parameter in method.Parameters)
                    {
                        if (!analyzerContext.IsTypeValid(parameter.Type))
                        {
                            context.ReportDiagnostic(Rule, parameter);
                        }
                    }
                }
            }, SymbolKind.Method);

            context.RegisterSymbolAction(context =>
            {
                var field = (IFieldSymbol)context.Symbol;
                if (field.DeclaredAccessibility is Accessibility.Public && !field.IsStatic && !field.IsReadOnly && !field.IsConst && analyzerContext.IsAttribute(field))
                {
                    if (!analyzerContext.IsTypeValid(field.Type))
                    {
                        context.ReportDiagnostic(Rule, field);
                    }
                }
            }, SymbolKind.Field);

            context.RegisterSymbolAction(context =>
            {
                var property = (IPropertySymbol)context.Symbol;
                if (property.DeclaredAccessibility is Accessibility.Public && property.SetMethod is not null && !property.IsStatic && analyzerContext.IsAttribute(property))
                {
                    if (!analyzerContext.IsTypeValid(property.Type))
                    {
                        context.ReportDiagnostic(Rule, property);
                    }
                }
            }, SymbolKind.Property);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly ITypeSymbol? _attributeSymbol = compilation.GetBestTypeByMetadataName("System.Attribute");
        private readonly ITypeSymbol? _typeSymbol = compilation.GetBestTypeByMetadataName("System.Type");
        private readonly ITypeSymbol? _enumSymbol = compilation.GetBestTypeByMetadataName("System.Enum");

        public bool IsValid => _attributeSymbol is not null;

        public bool IsAttribute(ISymbol methodSymbol)
        {
            return methodSymbol.ContainingType.IsOrInheritFrom(_attributeSymbol);
        }

        public bool IsTypeValid(ITypeSymbol type)
        {
            return IsTypeValid(type, allowArray: true);

            bool IsTypeValid(ITypeSymbol type, bool allowArray)
            {
                switch (type.SpecialType)
                {
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Char:
                    case SpecialType.System_Byte:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_String:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Object:
                    case SpecialType.System_Enum:
                        return true;
                }

                if (type.IsEqualTo(_typeSymbol))
                    return true;

                if (type.IsOrInheritFrom(_enumSymbol))
                    return true;

                if (allowArray && type is IArrayTypeSymbol array && array.Rank is 1 && IsTypeValid(array.ElementType, allowArray: false))
                    return true;

                return false;
            }
        }
    }
}