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
            ctx.RegisterSymbolAction(analyzerContext.AnalyzePropertyOrFieldSymbol, SymbolKind.Property, SymbolKind.Field);
            ctx.RegisterSymbolAction(analyzerContext.AnalyzeMethodSymbol, SymbolKind.Method);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeObjectCreation, OperationKind.ObjectCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeArrayCreation, OperationKind.ArrayCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeTypeOf, OperationKind.TypeOf);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeMemberReference, OperationKind.PropertyReference, OperationKind.FieldReference, OperationKind.MethodReference, OperationKind.EventReference);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeVariableDeclarator, OperationKind.VariableDeclarator);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeConversion, OperationKind.Conversion);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeIsType, OperationKind.IsType);
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
                AddUsedType(operation, operation.Type);
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
            AddUsedType(operation, operation.TargetMethod.ContainingType);

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

        public void AnalyzeIsType(OperationAnalysisContext context)
        {
            var operation = (IIsTypeOperation)context.Operation;
            if (operation.TypeOperand is not null)
            {
                AddUsedType(operation, operation.TypeOperand);
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

        private void AddUsedType(IOperation? referenceLocation, ITypeSymbol typeSymbol)
        {
            var location = ReferenceLocation.FromOperation(referenceLocation);
            AddUsedType(ref location, typeSymbol);
        }

        private void AddUsedType(ISymbol? containingSymbol, ITypeSymbol typeSymbol)
        {
            var location = ReferenceLocation.FromSymbol(containingSymbol);
            AddUsedType(ref location, typeSymbol);
        }

        private void AddUsedType(ITypeSymbol typeSymbol)
        {
            var location = ReferenceLocation.None;
            AddUsedType(ref location, typeSymbol);
        }

        private void AddUsedType(ref ReferenceLocation referenceLocation, ITypeSymbol typeSymbol)
        {
            // The reference location is always a type of the analyzed assembly, so it can only be the type itself
            // when the type belongs to that assembly. Resolving the reference location queries the semantic model,
            // which is much more expensive than comparing the assemblies, so it is only resolved for those types.
            if (ShouldConsiderType(typeSymbol))
            {
                if (referenceLocation.Resolve().IsEqualTo(typeSymbol))
                    return;

                // Prevent re-processing already seen types
                if (!_usedTypes.Add(typeSymbol))
                    return;
            }

            // Also mark the original definition as used (in case of generic instantiations)
            if (!typeSymbol.IsEqualTo(typeSymbol.OriginalDefinition))
            {
                AddUsedType(ref referenceLocation, typeSymbol.OriginalDefinition);
            }

            // Handle array element types
            if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                AddUsedType(ref referenceLocation, arrayTypeSymbol.ElementType);
            }

            // Handle pointer types
            if (typeSymbol is IPointerTypeSymbol pointerTypeSymbol)
            {
                AddUsedType(ref referenceLocation, pointerTypeSymbol.PointedAtType);
            }

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                // Handle generic type arguments
                foreach (var typeArgument in namedTypeSymbol.TypeArguments)
                {
                    AddUsedType(ref referenceLocation, typeArgument);
                }

                // Iterate containing types (e.g. Interop.Kernel32.CreateFile)
                var containingType = namedTypeSymbol.ContainingType;
                if (containingType is not null)
                {
                    AddUsedType(ref referenceLocation, containingType);
                }
            }
        }

        private bool ShouldConsiderType(ITypeSymbol typeSymbol)
        {
            return typeSymbol.ContainingAssembly.IsEqualTo(compilation.Assembly);
        }

        /// <summary>
        /// The type that contains the reference to the used types. It is only needed to ignore the references a type
        /// makes to itself, so the enclosing symbol of an operation is resolved at most once, and only when a type
        /// of the analyzed assembly is reached.
        /// </summary>
        private struct ReferenceLocation
        {
            private IOperation? _operation;
            private ITypeSymbol? _type;

            public static ReferenceLocation None => default;

            public static ReferenceLocation FromOperation(IOperation? operation)
            {
                return operation?.SemanticModel is null ? None : new ReferenceLocation { _operation = operation };
            }

            public static ReferenceLocation FromSymbol(ISymbol? symbol)
            {
                if (symbol is not null and not ITypeSymbol)
                {
                    symbol = symbol.ContainingType;
                }

                return new ReferenceLocation { _type = symbol as ITypeSymbol };
            }

            public ITypeSymbol? Resolve()
            {
                if (_operation is null)
                    return _type;

                return ResolveOperation();
            }

            private ITypeSymbol? ResolveOperation()
            {
                var operation = _operation!;
                _operation = null;

                var enclosingSymbol = operation.SemanticModel!.GetEnclosingSymbol(operation.Syntax.SpanStart);
                _type = FromSymbol(enclosingSymbol)._type;
                return _type;
            }
        }
    }
}
