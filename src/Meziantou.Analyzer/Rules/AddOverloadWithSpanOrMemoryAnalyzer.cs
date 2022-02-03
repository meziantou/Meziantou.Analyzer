using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AddOverloadWithSpanOrMemoryAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.AddOverloadWithSpanOrMemory,
        title: "Consider adding an overload with a Span<T> or Memory<T>",
        messageFormat: "Consider adding an overload with a Span<T> or Memory<T>",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AddOverloadWithSpanOrMemory));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.IsImplicitlyDeclared || method.IsOverrideOrInterfaceImplementation())
            return;

        if (!method.IsVisibleOutsideOfAssembly())
            return;

        if (method.MethodKind is not MethodKind.Ordinary and not MethodKind.Constructor)
            return;

        if (!method.Parameters.Any(p => p.Type.TypeKind == TypeKind.Array))
            return;

        var overloads = method.ContainingType.GetMembers(method.Name);
        foreach (var overload in overloads.OfType<IMethodSymbol>())
        {
            if (IsValidOverload(context.Compilation, method, overload))
                return;
        }

        context.ReportDiagnostic(s_rule, method);
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
                if (!IsSpanOrMemory(compilation, overloadParameter, ((IArrayTypeSymbol)methodParameter).ElementType))
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

    private static bool IsSpanOrMemory(Compilation compilation, ITypeSymbol typeSymbol, ITypeSymbol expectedArrayType)
    {
        var types = new string[] { "System.Span`1", "System.ReadOnlySpan`1", "System.Memory`1", "System.ReadOnlyMemory`1" };
        foreach (var type in types)
        {
            if (typeSymbol.IsEqualTo(compilation.GetTypeByMetadataName(type)?.Construct(expectedArrayType)))
                return true;
        }

        return false;
    }
}
