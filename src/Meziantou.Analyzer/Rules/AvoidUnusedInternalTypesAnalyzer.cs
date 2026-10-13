namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidUnusedInternalTypesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.AvoidUnusedInternalTypes,
        title: "Avoid unused internal types",
        messageFormat: "Internal type '{0}' is apparently never used. If so, remove it from the assembly.",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidUnusedInternalTypes),
        customTags: ["CompilationEnd"]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.Analyze);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);

            ctx.RegisterSymbolAction(analyzerContext.AnalyzeNamedTypeSymbol, SymbolKind.NamedType);
            ctx.RegisterSymbolAction(analyzerContext.AnalyzePropertyFieldOrEventSymbol, SymbolKind.Property, SymbolKind.Field, SymbolKind.Event);
            ctx.RegisterSymbolAction(analyzerContext.AnalyzeMethodSymbol, SymbolKind.Method);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeObjectCreation, OperationKind.ObjectCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeArrayCreation, OperationKind.ArrayCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeTypeOf, OperationKind.TypeOf);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeMemberReference, OperationKind.PropertyReference, OperationKind.FieldReference, OperationKind.MethodReference, OperationKind.EventReference);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeVariableDeclarator, OperationKind.VariableDeclarator);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeConversion, OperationKind.Conversion);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeIsType, OperationKind.IsType);
            ctx.RegisterOperationAction(analyzerContext.AnalyzePattern, OperationKind.DeclarationPattern, OperationKind.TypePattern, OperationKind.RecursivePattern);
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

        if (symbol.IsImplicitlyDeclared)
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
        // The used types are collected from the operations of every syntax tree, which are analyzed concurrently,
        // so a concurrent set is used instead of locking on every reference.
        private readonly ConcurrentHashSet<ITypeSymbol> _usedTypes = new(SymbolEqualityComparer.Default);

        // The types whose referenced types (type arguments, containing types) are all marked as used, so they do not
        // need to be processed again. A type is not added when a reference to the type that contains the reference was
        // ignored while processing it, as the ignored type must be marked as used when the type is referenced elsewhere.
        private readonly ConcurrentHashSet<ITypeSymbol> _processedTypes = new(SymbolEqualityComparer.Default);
        private readonly INamedTypeSymbol? _dynamicallyAccessedMembersAttribute = compilation.GetBestTypeByMetadataName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute");
        private readonly INamedTypeSymbol? _moduleInitializerAttribute = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute");

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

        public void AnalyzePropertyFieldOrEventSymbol(SymbolAnalysisContext context)
        {
            var symbol = context.Symbol;
            ITypeSymbol? type = symbol switch
            {
                IPropertySymbol property => property.Type,
                IFieldSymbol field => field.Type,
                IEventSymbol @event => @event.Type,
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

            // Mark types containing ModuleInitializer methods as used
            if (_moduleInitializerAttribute is not null && method.HasAttribute(_moduleInitializerAttribute))
            {
                AddUsedType(parentType);
            }

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
                AddUsedType(context.ContainingSymbol, operation.Type);
            }
        }

        public void AnalyzeArrayCreation(OperationAnalysisContext context)
        {
            var operation = (IArrayCreationOperation)context.Operation;
            if (operation.Type is IArrayTypeSymbol arrayType)
            {
                AddUsedType(context.ContainingSymbol, arrayType.ElementType);
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            AddUsedType(context.ContainingSymbol, operation.TargetMethod.ContainingType);

            // Track type arguments used in method invocations (e.g., JsonSerializer.Deserialize<T>())
            foreach (var typeArgument in operation.TargetMethod.TypeArguments)
            {
                AddUsedType(context.ContainingSymbol, typeArgument);
            }
        }

        public void AnalyzeTypeOf(OperationAnalysisContext context)
        {
            var operation = (ITypeOfOperation)context.Operation;
            if (operation.TypeOperand is not null)
            {
                AddUsedType(context.ContainingSymbol, operation.TypeOperand);
            }
        }

        public void AnalyzeMemberReference(OperationAnalysisContext context)
        {
            var operation = (IMemberReferenceOperation)context.Operation;

            // Track type arguments in the containing type of the member being accessed
            // For example: Sample<InternalClass>.Empty
            if (operation.Member.ContainingType is not null)
            {
                AddUsedType(context.ContainingSymbol, operation.Member.ContainingType);
            }
        }

        public void AnalyzeVariableDeclarator(OperationAnalysisContext context)
        {
            var operation = (IVariableDeclaratorOperation)context.Operation;

            // Track the type of the variable being declared
            if (operation.Symbol is ILocalSymbol localSymbol && localSymbol.Type is not null)
            {
                AddUsedType(context.ContainingSymbol, localSymbol.Type);
            }
        }

        public void AnalyzeConversion(OperationAnalysisContext context)
        {
            var operation = (IConversionOperation)context.Operation;

            // Track the target type of the conversion
            if (operation.Type is not null)
            {
                AddUsedType(context.ContainingSymbol, operation.Type);
            }
        }

        public void AnalyzePattern(OperationAnalysisContext context)
        {
            // Track types used in pattern matching. The patterns are analyzed wherever they are, so the patterns
            // nested in other patterns (not, and, or, property patterns) and the patterns of the switch statements
            // and switch expressions are also tracked.
            var matchedType = context.Operation switch
            {
                IDeclarationPatternOperation declarationPattern => declarationPattern.MatchedType,
                ITypePatternOperation typePattern => typePattern.MatchedType,
                IRecursivePatternOperation recursivePattern => recursivePattern.MatchedType,
                _ => null,
            };

            if (matchedType is not null)
            {
                AddUsedType(context.ContainingSymbol, matchedType);
            }
        }

        public void AnalyzeIsType(OperationAnalysisContext context)
        {
            var operation = (IIsTypeOperation)context.Operation;
            if (operation.TypeOperand is not null)
            {
                AddUsedType(context.ContainingSymbol, operation.TypeOperand);
            }
        }

        public void AnalyzeDelegateCreation(OperationAnalysisContext context)
        {
            var operation = (IDelegateCreationOperation)context.Operation;
            if (operation.Type is not null)
            {
                AddUsedType(context.ContainingSymbol, operation.Type);
            }
        }

        public void AnalyzeCompilationEnd(CompilationAnalysisContext context)
        {
            var entryPoint = compilation.GetEntryPoint(context.CancellationToken);
            if (entryPoint is not null)
            {
                AddUsedType(entryPoint.ContainingType);
            }

            foreach (var type in _potentialUnusedTypes)
            {
                if (_usedTypes.Contains(type))
                    continue;

                var properties = ImmutableDictionary<string, string?>.Empty;
                context.ReportDiagnostic(Rule, properties, type.Locations, type.Name);
            }
        }

        /// <param name="containingSymbol">The symbol that contains the reference, such as <see cref="OperationAnalysisContext.ContainingSymbol"/>.</param>
        private void AddUsedType(ISymbol? containingSymbol, ITypeSymbol typeSymbol)
        {
            // The type that contains the reference is only needed to ignore the references a type makes to itself.
            // It is computed from the symbol provided by the analysis context, as finding it from the location of
            // an operation (SemanticModel.GetEnclosingSymbol) is much more expensive than the rest of the analysis.
            var referenceLocation = containingSymbol as ITypeSymbol ?? containingSymbol?.ContainingType;
            AddUsedTypeCore(referenceLocation, typeSymbol);
        }

        private void AddUsedType(ITypeSymbol typeSymbol) => AddUsedTypeCore(referenceLocation: null, typeSymbol);

        /// <param name="referenceLocation">The type that contains the reference. It is only needed to ignore the references a type makes to itself.</param>
        /// <returns><see langword="true"/> if all the types referenced by <paramref name="typeSymbol"/> are marked as used, or <see langword="false"/> if the type that contains the reference was ignored.</returns>
        private bool AddUsedTypeCore(ITypeSymbol? referenceLocation, ITypeSymbol typeSymbol)
        {
            var considerType = ShouldConsiderType(typeSymbol);
            if (considerType)
            {
                // Prevent re-processing already seen types
                if (_processedTypes.Contains(typeSymbol))
                    return true;

                if (referenceLocation.IsEqualTo(typeSymbol))
                    return false;

                _usedTypes.Add(typeSymbol);
            }

            var isComplete = true;

            // Also mark the original definition as used (in case of generic instantiations)
            if (!typeSymbol.IsEqualTo(typeSymbol.OriginalDefinition))
            {
                isComplete &= AddUsedTypeCore(referenceLocation, typeSymbol.OriginalDefinition);
            }

            // Handle array element types
            if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                isComplete &= AddUsedTypeCore(referenceLocation, arrayTypeSymbol.ElementType);
            }

            // Handle pointer types
            if (typeSymbol is IPointerTypeSymbol pointerTypeSymbol)
            {
                isComplete &= AddUsedTypeCore(referenceLocation, pointerTypeSymbol.PointedAtType);
            }

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                // Handle generic type arguments
                foreach (var typeArgument in namedTypeSymbol.TypeArguments)
                {
                    isComplete &= AddUsedTypeCore(referenceLocation, typeArgument);
                }

                // Iterate containing types (e.g. Interop.Kernel32.CreateFile)
                var containingType = namedTypeSymbol.ContainingType;
                if (containingType is not null)
                {
                    isComplete &= AddUsedTypeCore(referenceLocation, containingType);
                }
            }

            if (considerType && isComplete)
            {
                _processedTypes.Add(typeSymbol);
            }

            return isComplete;
        }

        private bool ShouldConsiderType(ITypeSymbol typeSymbol)
        {
            return typeSymbol.ContainingAssembly.IsEqualTo(compilation.Assembly);
        }
    }
}
