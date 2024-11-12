using System.Collections.Immutable;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AddOverloadWithSpanOrMemoryAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.AddOverloadWithSpanOrMemory,
        title: "Consider adding an overload with a Span<T> or Memory<T>",
        messageFormat: "Consider adding an overload with a Span<T> or Memory<T>",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AddOverloadWithSpanOrMemory));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.IsImplicitlyDeclared || method.IsOverrideOrInterfaceImplementation())
            return;

        if (!method.IsVisibleOutsideOfAssembly())
            return;

        if (method.MethodKind is not MethodKind.Ordinary and not MethodKind.Constructor)
            return;

        if (!method.Parameters.Any(IsCandidateForSpanOrMemory))
            return;

        var overloads = method.ContainingType.GetMembers(method.Name);
        foreach (var overload in overloads.OfType<IMethodSymbol>())
        {
            if (IsValidOverload(context.Compilation, method, overload))
                return;
        }

        context.ReportDiagnostic(Rule, method);
    }

    private static bool IsCandidateForSpanOrMemory(IParameterSymbol param)
    {
        return param.Type.TypeKind is TypeKind.Array && !param.IsParams && param.RefKind is RefKind.None;
    }

    private static bool IsValidOverload(Compilation compilation, IMethodSymbol method, IMethodSymbol overload)
    {
        if (overload.IsEqualTo(method))
            return false;

        if (overload.Parameters.Length != method.Parameters.Length)
            return false;

        for (var i = 0; i < method.Parameters.Length; i++)
        {
            var methodParameter = method.Parameters[i].Type;
            var overloadParameter = overload.Parameters[i].Type;

            var methodParameterIsArray = methodParameter.TypeKind == TypeKind.Array;
            if (methodParameterIsArray)
            {
                if (!IsCandidateForSpanOrMemory(method.Parameters[i]) && methodParameter.IsEqualTo(overloadParameter))
                    continue;

                if (!IsSpanOrMemory(compilation, overloadParameter, methodParameter))
                    return false;
            }
            else
            {
                if (!methodParameter.IsEqualTo(overloadParameter))
                    return false;
            }
        }

        return true;
    }

    private static bool IsSpanOrMemory(Compilation compilation, ITypeSymbol typeSymbol, ITypeSymbol arrayType)
    {
        ITypeSymbol elementType;
        if (arrayType.IsString())
        {
            elementType = compilation.GetSpecialType(SpecialType.System_Char);
        }
        else
        {
            elementType = ((IArrayTypeSymbol)arrayType).ElementType;
        }

        var types = new string[] { "System.Span`1", "System.ReadOnlySpan`1", "System.Memory`1", "System.ReadOnlyMemory`1" };
        foreach (var type in types)
        {
            if (typeSymbol.IsEqualTo(compilation.GetBestTypeByMetadataName(type)?.Construct(elementType)))
                return true;
        }

        return false;
    }
}
