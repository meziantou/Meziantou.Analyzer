using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValidateUnsafeAccessorAttributeUsageAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor RuleInvalidSignature = new(
        RuleIdentifiers.UnsafeAccessorAttribute_InvalidSignature,
        title: "Signature for [UnsafeAccessorAttribute] method is not valid",
        messageFormat: "Signature for [UnsafeAccessorAttribute] method is not valid: {0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UnsafeAccessorAttribute_InvalidSignature));

    private static readonly DiagnosticDescriptor RuleNameMustBeSet = new(
        RuleIdentifiers.UnsafeAccessorAttribute_NameMustBeSet,
        title: "Name must be set explicitly on local functions",
        messageFormat: "Name must be set explicitly on local function",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UnsafeAccessorAttribute_NameMustBeSet));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleInvalidSignature, RuleNameMustBeSet);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var attributeSymbol = context.Compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.UnsafeAccessorAttribute");
            if (attributeSymbol is null)
                return;

            context.RegisterSymbolAction(context => AnalyzeMethodSymbol(context, attributeSymbol), SymbolKind.Method);
            context.RegisterOperationAction(context => AnalyzeLocalFunctions(context, attributeSymbol), OperationKind.LocalFunction);
        });
    }

    private static void AnalyzeLocalFunctions(OperationAnalysisContext context, INamedTypeSymbol attributeSymbol)
    {
        var operation = (ILocalFunctionOperation)context.Operation;
        var symbol = operation.Symbol;
        AnalyzeMethodSymbol(symbol, attributeSymbol, new(context));
    }

    private static void AnalyzeMethodSymbol(SymbolAnalysisContext context, INamedTypeSymbol attributeSymbol)
    {
        var symbol = (IMethodSymbol)context.Symbol;
        AnalyzeMethodSymbol(symbol, attributeSymbol, new(context));
    }

    // https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.unsafeaccessorattribute
    private static void AnalyzeMethodSymbol(IMethodSymbol methodSymbol, INamedTypeSymbol attributeSymbol, DiagnosticReporter diagnosticReporter)
    {
        var attribute = methodSymbol.GetAttribute(attributeSymbol, inherits: false);
        if (attribute is null)
            return;

        if (attribute.ConstructorArguments.IsEmpty)
            return; // Invalid usage

        if (attribute.ConstructorArguments[0].Value is not int accessorKindInt)
            return;

        if (!methodSymbol.IsExtern)
        {
            diagnosticReporter.ReportDiagnostic(RuleInvalidSignature, methodSymbol, messageArgs: ["method must be extern static"]);
            return;
        }

        var explicitName = GetName(attribute);
        if (explicitName is null && methodSymbol.MethodKind is MethodKind.LocalFunction)
        {
            diagnosticReporter.ReportDiagnostic(RuleNameMustBeSet, methodSymbol);
            return;
        }

        var accessorKind = (UnsafeAccessorKind)accessorKindInt;
        if (methodSymbol.Parameters.IsEmpty && accessorKind is UnsafeAccessorKind.Field or UnsafeAccessorKind.StaticField or UnsafeAccessorKind.Method or UnsafeAccessorKind.StaticMethod)
        {
            diagnosticReporter.ReportDiagnostic(RuleInvalidSignature, methodSymbol, messageArgs: ["method must have at least one parameter"]);
            return;
        }

        if (accessorKind is UnsafeAccessorKind.Field or UnsafeAccessorKind.StaticField && methodSymbol.ReturnsVoid)
        {
            diagnosticReporter.ReportDiagnostic(RuleInvalidSignature, methodSymbol, messageArgs: ["return type does not match the field type"]);
            return;
        }

        if (accessorKind is UnsafeAccessorKind.Field or UnsafeAccessorKind.StaticField && !IsRefOrRefReadOnly(methodSymbol.RefKind))
        {
            diagnosticReporter.ReportDiagnostic(RuleInvalidSignature, methodSymbol, messageArgs: ["must return by ref"]);
            return;
        }

        if (accessorKind is UnsafeAccessorKind.Field or UnsafeAccessorKind.StaticField && methodSymbol.Parameters.Length != 1)
        {
            diagnosticReporter.ReportDiagnostic(RuleInvalidSignature, methodSymbol, messageArgs: ["method must have a single parameter"]);
            return;
        }

        if (accessorKind is UnsafeAccessorKind.Field && !methodSymbol.ReturnsVoid && !methodSymbol.ReturnsByRef && !methodSymbol.ReturnsByRefReadonly)
        {
            diagnosticReporter.ReportDiagnostic(RuleInvalidSignature, methodSymbol, messageArgs: ["method must be extern static"]);
            return;
        }

        // When struct, first parameter must be by ref if field or method
        if (accessorKind is UnsafeAccessorKind.Method or UnsafeAccessorKind.Field && methodSymbol.Parameters[0].Type.IsValueType && !IsRefOrRefReadOnly(methodSymbol.Parameters[0].RefKind))
        {
            diagnosticReporter.ReportDiagnostic(RuleInvalidSignature, methodSymbol, messageArgs: ["the first parameter must be ref"]);
            return;
        }

        // Roslyn doesn't expose private members from other assemblies
        //var type = methodSymbol.Parameters[0].Type;
        //switch (accessorKind)
        //{
        //    case UnsafeAccessorKind.Method:
        //        if (!type.GetMembers(memberName).Where(m => m is IMethodSymbol method && !method.IsStatic).Any())
        //        {
        //            diagnosticReporter.ReportDiagnostic(s_ruleMemberNotFound, methodSymbol, messageArgs: [memberName, type.ToDisplayString()]);
        //            return;
        //        }
        //        break;
        //    case UnsafeAccessorKind.StaticMethod:
        //        if (!type.GetMembers(memberName).Where(m => m is IMethodSymbol method && method.IsStatic).Any())
        //        {
        //            diagnosticReporter.ReportDiagnostic(s_ruleMemberNotFound, methodSymbol, messageArgs: [memberName, type.ToDisplayString()]);
        //            return;
        //        }
        //        break;
        //    case UnsafeAccessorKind.Field:
        //        if (!type.GetMembers(memberName).Where(m => m is IFieldSymbol method && !method.IsStatic).Any())
        //        {
        //            diagnosticReporter.ReportDiagnostic(s_ruleMemberNotFound, methodSymbol, messageArgs: [memberName, type.ToDisplayString()]);
        //            return;
        //        }
        //        break;
        //    case UnsafeAccessorKind.StaticField:
        //        if (!type.GetMembers(memberName).Where(m => m is IFieldSymbol method && method.IsStatic).Any())
        //        {
        //            diagnosticReporter.ReportDiagnostic(s_ruleMemberNotFound, methodSymbol, messageArgs: [memberName, type.ToDisplayString()]);
        //            return;
        //        }
        //        break;
        //}
    }

    private static bool IsRefOrRefReadOnly(RefKind kind)
    {
        if (kind is RefKind.Ref or RefKind.In)
            return true;

#if CSHARP12_OR_GREATER
        if (kind is RefKind.RefReadOnlyParameter)
            return true;
#endif
        return false;
    }

    private static string? GetName(AttributeData data)
    {
        foreach (var prop in data.NamedArguments)
        {
            if (prop.Key == "Name")
            {
                if (prop.Value.IsNull)
                    return null;

                if (prop.Value.Kind is TypedConstantKind.Primitive && prop.Value.Value is string str)
                    return str;

                break;
            }
        }

        return null;
    }

    private enum UnsafeAccessorKind
    {
        /// <summary>
        /// Provide access to a constructor.
        /// </summary>
        Constructor,

        /// <summary>
        /// Provide access to a method.
        /// </summary>
        Method,

        /// <summary>
        /// Provide access to a static method.
        /// </summary>
        StaticMethod,

        /// <summary>
        /// Provide access to a field.
        /// </summary>
        Field,

        /// <summary>
        /// Provide access to a static field.
        /// </summary>
        StaticField,
    };
}
