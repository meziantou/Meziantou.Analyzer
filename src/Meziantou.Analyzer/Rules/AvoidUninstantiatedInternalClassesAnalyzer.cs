using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidUninstantiatedInternalClassesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.AvoidUninstantiatedInternalClasses,
        title: "Avoid uninstantiated internal classes",
        messageFormat: "Internal class '{0}' is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it 'static'.",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidUninstantiatedInternalClasses),
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);

            ctx.RegisterSymbolAction(analyzerContext.AnalyzeNamedTypeSymbol, SymbolKind.NamedType);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeObjectCreation, OperationKind.ObjectCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeArrayCreation, OperationKind.ArrayCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeTypeOf, OperationKind.TypeOf);
            ctx.RegisterCompilationEndAction(analyzerContext.AnalyzeCompilationEnd);
        });
    }

    private static bool IsPotentialUninstantiatedClass(INamedTypeSymbol symbol, CancellationToken cancellationToken)
    {
        // Only analyze internal classes
        if (symbol.DeclaredAccessibility != Accessibility.Internal)
            return false;

        // Exclude abstract classes, static classes, and implicitly declared classes
        if (symbol.IsAbstract || symbol.IsStatic || symbol.IsImplicitlyDeclared)
            return false;

        // Exclude interfaces, enums, delegates, and records
        if (symbol.TypeKind is not TypeKind.Class)
            return false;

        // Exclude unit test classes
        if (symbol.IsUnitTestClass())
            return false;

        // Exclude top-level statements
        if (symbol.IsTopLevelStatement(cancellationToken))
            return false;

        return true;
    }

    private sealed class AnalyzerContext
    {
        private readonly List<ITypeSymbol> _potentialUninstantiatedClasses = [];
        private readonly HashSet<ITypeSymbol> _usedClasses = new(SymbolEqualityComparer.Default);

        public AnalyzerContext(Compilation compilation)
        {
            _ = compilation;
        }

        public void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (IsPotentialUninstantiatedClass(symbol, context.CancellationToken))
            {
                lock (_potentialUninstantiatedClasses)
                {
                    _potentialUninstantiatedClasses.Add(symbol);
                }
            }
        }

        public void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            var operation = (IObjectCreationOperation)context.Operation;
            if (operation.Type is not null)
            {
                AddUsedType(operation.Type);
            }
        }

        public void AnalyzeArrayCreation(OperationAnalysisContext context)
        {
            var operation = (IArrayCreationOperation)context.Operation;
            if (operation.Type is IArrayTypeSymbol arrayType)
            {
                AddUsedType(arrayType.ElementType);
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            
            // Track type arguments used in method invocations (e.g., JsonSerializer.Deserialize<T>())
            foreach (var typeArgument in operation.TargetMethod.TypeArguments)
            {
                AddUsedType(typeArgument);
            }
        }

        public void AnalyzeTypeOf(OperationAnalysisContext context)
        {
            var operation = (ITypeOfOperation)context.Operation;
            if (operation.TypeOperand is not null)
            {
                AddUsedType(operation.TypeOperand);
            }
        }

        public void AnalyzeCompilationEnd(CompilationAnalysisContext context)
        {
            foreach (var @class in _potentialUninstantiatedClasses)
            {
                if (_usedClasses.Contains(@class))
                    continue;

                var properties = ImmutableDictionary<string, string?>.Empty;
                context.ReportDiagnostic(Diagnostic.Create(Rule, @class.Locations.FirstOrDefault(), properties, @class.Name));
            }
        }

        private void AddUsedType(ITypeSymbol typeSymbol)
        {
            lock (_usedClasses)
            {
                _usedClasses.Add(typeSymbol);
                
                // Also mark the original definition as used (in case of generic instantiations)
                if (!typeSymbol.IsEqualTo(typeSymbol.OriginalDefinition))
                {
                    AddUsedType(typeSymbol.OriginalDefinition);
                }

                // Handle array element types
                if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
                {
                    AddUsedType(arrayTypeSymbol.ElementType);
                }

                // Handle generic type arguments
                if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
                {
                    foreach (var typeArgument in namedTypeSymbol.TypeArguments)
                    {
                        AddUsedType(typeArgument);
                    }
                }
            }
        }
    }
}
