using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidUnusedInternalTypesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.AvoidUnusedInternalTypes,
        title: "Avoid unused internal types",
        messageFormat: "Internal type '{0}' is apparently never used. If so, remove it from the assembly. If this type is intended to contain only static members, make it 'static'.",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidUnusedInternalTypes));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext();

            ctx.RegisterSymbolAction(analyzerContext.AnalyzeNamedTypeSymbol, SymbolKind.NamedType);
            ctx.RegisterSymbolAction(analyzerContext.AnalyzePropertyOrFieldSymbol, SymbolKind.Property, SymbolKind.Field);
            ctx.RegisterSymbolAction(analyzerContext.AnalyzeMethodSymbol, SymbolKind.Method);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeObjectCreation, OperationKind.ObjectCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeArrayCreation, OperationKind.ArrayCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeTypeOf, OperationKind.TypeOf);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeMemberReference, OperationKind.PropertyReference, OperationKind.FieldReference, OperationKind.MethodReference, OperationKind.EventReference);
            ctx.RegisterCompilationEndAction(analyzerContext.AnalyzeCompilationEnd);
        });
    }

    private static bool IsPotentialUnusedType(INamedTypeSymbol symbol, CancellationToken cancellationToken)
    {
        // Only analyze internal types
        if (symbol.DeclaredAccessibility != Accessibility.Internal)
            return false;

        // Exclude abstract types, static types, and implicitly declared types
        if (symbol.IsAbstract || symbol.IsStatic || symbol.IsImplicitlyDeclared)
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
        private readonly List<ITypeSymbol> _potentialUnusedTypes = [];
        private readonly HashSet<ITypeSymbol> _usedTypes = new(SymbolEqualityComparer.Default);

        public void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (IsPotentialUnusedType(symbol, context.CancellationToken))
            {
                lock (_potentialUnusedTypes)
                {
                    _potentialUnusedTypes.Add(symbol);
                }
            }

            // Track types used in generic constraints
            foreach (var typeParameter in symbol.TypeParameters)
            {
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    AddUsedType(constraintType);
                }
            }

#if CSHARP14_OR_GREATER
            if(symbol.ExtensionParameter is not null)
            {
                AddUsedType(symbol.ExtensionParameter.Type);
            }
#endif
        }

        public void AnalyzePropertyOrFieldSymbol(SymbolAnalysisContext context)
        {
            var symbol = context.Symbol;
            ITypeSymbol? type = symbol switch
            {
                IPropertySymbol property => property.Type,
                IFieldSymbol field => field.Type,
                _ => null,
            };

            if (type is not null)
            {
                AddUsedType(type);
            }
        }

        public void AnalyzeMethodSymbol(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            
            // Track return type
            if (method.ReturnType is not null)
            {
                AddUsedType(method.ReturnType);
            }
            
            // Track parameter types
            foreach (var parameter in method.Parameters)
            {
                if (parameter.Type is not null)
                {
                    AddUsedType(parameter.Type);
                }
            }

            // Track types used in generic constraints
            foreach (var typeParameter in method.TypeParameters)
            {
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    AddUsedType(constraintType);
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

        public void AnalyzeMemberReference(OperationAnalysisContext context)
        {
            var operation = (IMemberReferenceOperation)context.Operation;
            
            // Track type arguments in the containing type of the member being accessed
            // For example: Sample<InternalClass>.Empty
            if (operation.Member.ContainingType is not null)
            {
                AddUsedType(operation.Member.ContainingType);
            }
        }

        public void AnalyzeCompilationEnd(CompilationAnalysisContext context)
        {
            foreach (var type in _potentialUnusedTypes)
            {
                if (_usedTypes.Contains(type))
                    continue;

                var properties = ImmutableDictionary<string, string?>.Empty;
                context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations.FirstOrDefault(), properties, type.Name));
            }
        }

        private void AddUsedType(ITypeSymbol typeSymbol)
        {
            lock (_usedTypes)
            {
                // Prevent re-processing already seen types
                if (!_usedTypes.Add(typeSymbol))
                    return;

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
