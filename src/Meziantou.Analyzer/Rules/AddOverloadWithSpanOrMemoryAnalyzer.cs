using System.Collections.Immutable;
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

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var analyzerContext = new AnalyzerContext(compilationContext.Compilation);
            if (!analyzerContext.IsValid)
                return;

            compilationContext.RegisterSymbolAction(analyzerContext.AnalyzeMethod, SymbolKind.Method);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly INamedTypeSymbol? _spanType = compilation.GetBestTypeByMetadataName("System.Span`1");
        private readonly INamedTypeSymbol? _readOnlySpanType = compilation.GetBestTypeByMetadataName("System.ReadOnlySpan`1");
        private readonly INamedTypeSymbol? _memoryType = compilation.GetBestTypeByMetadataName("System.Memory`1");
        private readonly INamedTypeSymbol? _readOnlyMemoryType = compilation.GetBestTypeByMetadataName("System.ReadOnlyMemory`1");

        public bool IsValid => _spanType is not null || _readOnlySpanType is not null || _memoryType is not null || _readOnlyMemoryType is not null;

        public void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            if (method.IsImplicitlyDeclared || method.IsOverrideOrInterfaceImplementation())
                return;

            if (!method.IsVisibleOutsideOfAssembly())
                return;

            if (method.MethodKind is not MethodKind.Ordinary and not MethodKind.Constructor)
                return;

            // Skip the program entry point (e.g., Main(string[] args)) as the signature is mandated by the runtime
            if (method.IsEqualTo(context.Compilation.GetEntryPoint(context.CancellationToken)))
                return;

            if (!method.Parameters.Any(IsCandidateForSpanOrMemory))
                return;

            var overloads = method.ContainingType.GetMembers(method.Name);
            foreach (var overload in overloads.OfType<IMethodSymbol>())
            {
                if (IsValidOverload(method, overload))
                    return;
            }

            context.ReportDiagnostic(Rule, method);
        }

        public bool IsSpanOrMemory(ITypeSymbol typeSymbol, ITypeSymbol arrayType)
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

            if (typeSymbol.IsEqualTo(_spanType?.Construct(elementType)))
                return true;

            if (typeSymbol.IsEqualTo(_readOnlySpanType?.Construct(elementType)))
                return true;

            if (typeSymbol.IsEqualTo(_memoryType?.Construct(elementType)))
                return true;

            if (typeSymbol.IsEqualTo(_readOnlyMemoryType?.Construct(elementType)))
                return true;

            return false;
        }

        private static bool IsCandidateForSpanOrMemory(IParameterSymbol param)
        {
            return param.Type.TypeKind is TypeKind.Array && !param.IsParams && param.RefKind is RefKind.None;
        }

        private bool IsValidOverload(IMethodSymbol method, IMethodSymbol overload)
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

                    if (!IsSpanOrMemory(overloadParameter, methodParameter))
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
    }
}
