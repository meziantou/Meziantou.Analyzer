using System.Collections.Concurrent;
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
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidUnusedInternalTypes),
        customTags: ["CompilationEnd"]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);

            ctx.RegisterSymbolAction(analyzerContext.AnalyzeNamedTypeSymbol, SymbolKind.NamedType);
            ctx.RegisterSymbolAction(analyzerContext.AnalyzePropertyOrFieldSymbol, SymbolKind.Property, SymbolKind.Field);
            ctx.RegisterSymbolAction(analyzerContext.AnalyzeMethodSymbol, SymbolKind.Method);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeObjectCreation, OperationKind.ObjectCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeArrayCreation, OperationKind.ArrayCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeTypeOf, OperationKind.TypeOf);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeMemberReference, OperationKind.PropertyReference, OperationKind.FieldReference, OperationKind.MethodReference, OperationKind.EventReference);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeVariableDeclarator, OperationKind.VariableDeclarator);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeConversion, OperationKind.Conversion);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeIsPattern, OperationKind.IsPattern);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeDelegateCreation, OperationKind.DelegateCreation);
            ctx.RegisterCompilationEndAction(analyzerContext.AnalyzeCompilationEnd);
        });
    }

    private static bool IsPotentialUnusedType(INamedTypeSymbol symbol, INamedTypeSymbol? dynamicallyAccessedMembersAttribute, CancellationToken cancellationToken)
    {
        // Only analyze types not visible outside of assembly
        if (symbol.IsVisibleOutsideOfAssembly())
            return false;

        // Exclude compiler-generated types (e.g., extension types, anonymous types)
        if (!symbol.CanBeReferencedByName)
            return false;

        if (symbol.IsStatic || symbol.IsImplicitlyDeclared)
            return false;

        // Exclude unit test classes
        if (symbol.IsUnitTestClass())
            return false;

        // Exclude top-level statements
        if (symbol.IsTopLevelStatement(cancellationToken))
            return false;

        // Exclude types with DynamicallyAccessedMembers attribute (accessed via reflection)
        if (dynamicallyAccessedMembersAttribute is not null && symbol.HasAttribute(dynamicallyAccessedMembersAttribute))
            return false;

        return true;
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly List<ITypeSymbol> _potentialUnusedTypes = [];
        private readonly HashSet<ITypeSymbol> _usedTypes = new(SymbolEqualityComparer.Default);
        private readonly INamedTypeSymbol? _dynamicallyAccessedMembersAttribute = compilation.GetBestTypeByMetadataName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute");

        public void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (IsPotentialUnusedType(symbol, _dynamicallyAccessedMembersAttribute, context.CancellationToken))
            {
                lock (_potentialUnusedTypes)
                {
                    _potentialUnusedTypes.Add(symbol);
                }
            }

            // Track base type (skip system types)
            if (symbol.BaseType is not null && !symbol.BaseType.IsVisibleOutsideOfAssembly())
            {
                AddUsedType(symbol, symbol.BaseType);
            }

            // Track implemented interfaces (skip system interfaces)
            foreach (var @interface in symbol.Interfaces)
            {
                if (!@interface.IsVisibleOutsideOfAssembly())
                {
                    AddUsedType(symbol, @interface);
                }
            }

            // Track types used in generic constraints
            foreach (var typeParameter in symbol.TypeParameters)
            {
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    AddUsedType(symbol, constraintType);
                }
            }

#if CSHARP14_OR_GREATER
            if (symbol.ExtensionParameter is not null)
            {
                AddUsedType(symbol, symbol.ExtensionParameter.Type);
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
                AddUsedType(symbol, type);
            }
        }

        public void AnalyzeMethodSymbol(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            var parentType = method.ContainingType;

            // Track return type
            if (method.ReturnType is not null)
            {
                AddUsedType(parentType, method.ReturnType);
            }

            // Track parameter types
            foreach (var parameter in method.Parameters)
            {
                if (parameter.Type is not null)
                {
                    AddUsedType(parentType, parameter.Type);
                }
            }

            // Track types used in generic constraints
            foreach (var typeParameter in method.TypeParameters)
            {
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    AddUsedType(parentType, constraintType);
                }
            }
        }

        public void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            var operation = (IObjectCreationOperation)context.Operation;
            if (operation.Type is not null)
            {
                // Object creation always marks the type as used, even if it occurs within the same type.
                // This allows factory methods (where a type creates instances of itself) to be recognized as valid usage.
                AddUsedType((ITypeSymbol?)null, operation.Type);
            }
        }

        public void AnalyzeArrayCreation(OperationAnalysisContext context)
        {
            var operation = (IArrayCreationOperation)context.Operation;
            if (operation.Type is IArrayTypeSymbol arrayType)
            {
                AddUsedType(operation, arrayType.ElementType);
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;

            // Track type arguments used in method invocations (e.g., JsonSerializer.Deserialize<T>())
            foreach (var typeArgument in operation.TargetMethod.TypeArguments)
            {
                AddUsedType(operation, typeArgument);
            }
        }

        public void AnalyzeTypeOf(OperationAnalysisContext context)
        {
            var operation = (ITypeOfOperation)context.Operation;
            if (operation.TypeOperand is not null)
            {
                AddUsedType(operation, operation.TypeOperand);
            }
        }

        public void AnalyzeMemberReference(OperationAnalysisContext context)
        {
            var operation = (IMemberReferenceOperation)context.Operation;

            // Track type arguments in the containing type of the member being accessed
            // For example: Sample<InternalClass>.Empty
            if (operation.Member.ContainingType is not null)
            {
                AddUsedType(operation, operation.Member.ContainingType);
            }
        }

        public void AnalyzeVariableDeclarator(OperationAnalysisContext context)
        {
            var operation = (IVariableDeclaratorOperation)context.Operation;

            // Track the type of the variable being declared
            if (operation.Symbol is ILocalSymbol localSymbol && localSymbol.Type is not null)
            {
                AddUsedType(operation, localSymbol.Type);
            }
        }

        public void AnalyzeConversion(OperationAnalysisContext context)
        {
            var operation = (IConversionOperation)context.Operation;

            // Track the target type of the conversion
            if (operation.Type is not null)
            {
                AddUsedType(operation, operation.Type);
            }
        }

        public void AnalyzeIsPattern(OperationAnalysisContext context)
        {
            var operation = (IIsPatternOperation)context.Operation;

            // Track types used in pattern matching
            if (operation.Pattern is IDeclarationPatternOperation declarationPattern)
            {
                if (declarationPattern.MatchedType is not null)
                {
                    AddUsedType(declarationPattern, declarationPattern.MatchedType);
                }
            }
            else if (operation.Pattern is ITypePatternOperation typePattern)
            {
                if (typePattern.MatchedType is not null)
                {
                    AddUsedType(typePattern, typePattern.MatchedType);
                }
            }
            else if (operation.Pattern is IRecursivePatternOperation recursivePattern)
            {
                if (recursivePattern.MatchedType is not null)
                {
                    AddUsedType(recursivePattern, recursivePattern.MatchedType);
                }
            }
        }

        public void AnalyzeDelegateCreation(OperationAnalysisContext context)
        {
            var operation = (IDelegateCreationOperation)context.Operation;
            if (operation.Type is not null)
            {
                AddUsedType(operation, operation.Type);
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

        private void AddUsedType(IOperation? referenceLocation, ITypeSymbol typeSymbol)
        {
            if (referenceLocation?.SemanticModel is null)
            {
                AddUsedType((ITypeSymbol?)null, typeSymbol);
                return;
            }

            var semanticModel = referenceLocation.SemanticModel;
            var containingType = semanticModel.GetEnclosingSymbol(referenceLocation.Syntax.SpanStart);
            AddUsedType(containingType, typeSymbol);
        }

        private void AddUsedType(ISymbol? containingSymbol, ITypeSymbol typeSymbol)
        {
            if (containingSymbol is null)
            {
                AddUsedType((ITypeSymbol?)null, typeSymbol);
                return;
            }

            if (containingSymbol is not ITypeSymbol)
            {
                containingSymbol = containingSymbol.ContainingType;
            }

            AddUsedType(containingSymbol as ITypeSymbol, typeSymbol);
        }

        private void AddUsedType(ITypeSymbol? referenceLocation, ITypeSymbol typeSymbol)
        {
            if (referenceLocation is not null && referenceLocation.IsEqualTo(typeSymbol))
                return;

            lock (_usedTypes)
            {
                // Prevent re-processing already seen types
                if (ShouldConsiderType(typeSymbol) && !_usedTypes.Add(typeSymbol))
                    return;

                // Also mark the original definition as used (in case of generic instantiations)
                if (!typeSymbol.IsEqualTo(typeSymbol.OriginalDefinition))
                {
                    AddUsedType(referenceLocation, typeSymbol.OriginalDefinition);
                }

                // Handle array element types
                if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
                {
                    AddUsedType(referenceLocation, arrayTypeSymbol.ElementType);
                }

                // Handle pointer types
                if (typeSymbol is IPointerTypeSymbol pointerTypeSymbol)
                {
                    AddUsedType(referenceLocation, pointerTypeSymbol.PointedAtType);
                }

                // Handle generic type arguments
                if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
                {
                    foreach (var typeArgument in namedTypeSymbol.TypeArguments)
                    {
                        AddUsedType(referenceLocation, typeArgument);
                    }
                }
            }
        }

        private bool ShouldConsiderType(ITypeSymbol typeSymbol)
        {
            return typeSymbol.ContainingAssembly.IsEqualTo(compilation.Assembly);
        }
    }
}
