namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MakeClassStaticAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.MakeClassStatic,
        title: "Make class static",
        messageFormat: "Make class static",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MakeClassStatic));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.Analyze);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);

            ctx.RegisterSymbolAction(analyzerContext.AnalyzeNamedTypeSymbol, SymbolKind.NamedType);
            ctx.RegisterSymbolAction(analyzerContext.AnalyzeMemberSymbol, SymbolKind.Field, SymbolKind.Property, SymbolKind.Event, SymbolKind.Method);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeObjectCreation, OperationKind.ObjectCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeArrayCreation, OperationKind.ArrayCreation);
            ctx.RegisterOperationAction(
                analyzerContext.AnalyzeTypeUsage,
                OperationKind.VariableDeclarator,
                OperationKind.DeclarationExpression,
                OperationKind.Conversion,
                OperationKind.DefaultValue,
                OperationKind.IsType,
                OperationKind.DeclarationPattern,
                OperationKind.TypePattern,
                OperationKind.RecursivePattern,
                OperationKind.TypeOf,
                OperationKind.AnonymousFunction,
                OperationKind.LocalFunction,
                OperationKind.FieldReference,
                OperationKind.PropertyReference,
                OperationKind.EventReference,
                OperationKind.MethodReference);
            ctx.RegisterCompilationEndAction(analyzerContext.AnalyzeCompilationEnd);
        });
    }

    private static bool IsPotentialStatic(INamedTypeSymbol symbol, CancellationToken cancellationToken)
    {
        return !symbol.IsAbstract &&
            !symbol.IsStatic &&
            !symbol.IsImplicitlyDeclared &&
            !symbol.Interfaces.Any() &&
            !HasBaseClass() &&
            !symbol.IsUnitTestClass() &&
            !symbol.IsTopLevelStatement(cancellationToken) &&
            symbol.GetMembers().All(member => (member.IsStatic || member.IsImplicitlyDeclared) && !member.IsOperator());

        bool HasBaseClass()
        {
            return symbol.BaseType is not null && symbol.BaseType.SpecialType != SpecialType.System_Object;
        }
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly List<ITypeSymbol> _potentialClasses = [];
        // The types are collected from the operations of every syntax tree, which are analyzed concurrently,
        // so a concurrent set is used instead of locking on every reference.
        private readonly ConcurrentHashSet<ITypeSymbol> _cannotBeStaticClasses = new(SymbolEqualityComparer.Default);

        public INamedTypeSymbol? CoClassAttributeSymbol { get; } = compilation.GetTypeByMetadataName("System.Runtime.InteropServices.CoClassAttribute");

        public void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            AddTypeParameterConstraintTypes(symbol.TypeParameters);
            if (symbol.DelegateInvokeMethod is not null)
            {
                AddMethodSignatureTypes(symbol.DelegateInvokeMethod);
            }

            switch (symbol.TypeKind)
            {
                case TypeKind.Class:
                    if (IsPotentialStatic(symbol, context.CancellationToken))
                    {
                        lock (_potentialClasses)
                        {
                            _potentialClasses.Add(symbol);
                        }
                    }

                    if (symbol.BaseType is not null)
                    {
                        AddCannotBeStaticType(symbol.BaseType);
                    }

                    foreach (var iface in symbol.AllInterfaces)
                    {
                        AddCannotBeStaticType(iface);
                    }

                    break;

                case TypeKind.Interface:
                    foreach (var attribute in symbol.GetAttributes().Where(attr => attr.AttributeClass.IsEqualTo(CoClassAttributeSymbol)))
                    {
                        var attributeValue = attribute.ConstructorArguments.FirstOrDefault();
                        if (!attributeValue.IsNull && attributeValue.Kind == TypedConstantKind.Type && attributeValue.Value is ITypeSymbol type)
                        {
                            AddCannotBeStaticType(type);
                        }
                    }

                    break;
            }
        }

        // Static types cannot be used as the type of a member, a parameter or a return value, nor as a generic constraint
        public void AnalyzeMemberSymbol(SymbolAnalysisContext context)
        {
            switch (context.Symbol)
            {
                case IFieldSymbol field:
                    AddCannotBeStaticType(field.Type);
                    break;

                case IPropertySymbol property:
                    AddCannotBeStaticType(property.Type);
                    AddParameterTypes(property.Parameters);
                    break;

                case IEventSymbol @event:
                    AddCannotBeStaticType(@event.Type);
                    break;

                case IMethodSymbol method:
                    AddMethodSignatureTypes(method);
                    break;
            }
        }

        public void AnalyzeTypeUsage(OperationAnalysisContext context)
        {
            switch (context.Operation)
            {
                case IVariableDeclaratorOperation operation:
                    AddCannotBeStaticType(operation.Symbol.Type);
                    break;

                case IIsTypeOperation operation:
                    AddCannotBeStaticType(operation.TypeOperand);
                    break;

                case IPatternOperation operation:
                    AddCannotBeStaticType(operation.NarrowedType);
                    break;

                // typeof(StaticClass) is valid, but not typeof(List<StaticClass>) or typeof(StaticClass[])
                case ITypeOfOperation operation:
                    AddTypeComponents(operation.TypeOperand);
                    break;

                case IAnonymousFunctionOperation operation:
                    AddMethodSignatureTypes(operation.Symbol);
                    break;

                case ILocalFunctionOperation operation:
                    AddMethodSignatureTypes(operation.Symbol);
                    break;

                // StaticClass.Member does not create an operation for the type, but Generic<StaticClass>.Member is invalid
                case IMethodReferenceOperation operation:
                    AddTypeArguments(operation.Method);
                    break;

                case IMemberReferenceOperation operation:
                    AddContainingTypeArguments(operation.Member);
                    break;

                // Conversions, default values and declaration expressions (out StaticClass value) are typed with the static class
                case { Type: { } type }:
                    AddCannotBeStaticType(type);
                    break;
            }
        }

        public void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            var operation = (IObjectCreationOperation)context.Operation;
            if (operation.Constructor is null)
                return;

            AddCannotBeStaticType(operation.Constructor.ContainingType);
            foreach (var typeArgument in operation.Constructor.TypeArguments)
            {
                AddCannotBeStaticType(typeArgument);
            }
        }

        public void AnalyzeArrayCreation(OperationAnalysisContext context)
        {
            var operation = (IArrayCreationOperation)context.Operation;
            if (operation.Type is not null)
            {
                AddCannotBeStaticType(operation.Type);
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            AddTypeArguments(operation.TargetMethod);
        }

        public void AnalyzeCompilationEnd(CompilationAnalysisContext context)
        {
            foreach (var @class in _potentialClasses)
            {
                if (_cannotBeStaticClasses.Contains(@class))
                    continue;

                context.ReportDiagnostic(Rule, @class);
            }
        }

        private void AddCannotBeStaticType(ITypeSymbol typeSymbol)
        {
            // The set is only used to exclude the candidates, which are all declared in the analyzed assembly,
            // so the types coming from the references are not kept. The referenced types are still walked, as
            // they can use a type of the analyzed assembly as a type argument (List&lt;MyClass&gt;).
            if (typeSymbol.ContainingAssembly.IsEqualTo(compilation.Assembly))
            {
                _cannotBeStaticClasses.Add(typeSymbol);
            }

            if (!typeSymbol.IsEqualTo(typeSymbol.OriginalDefinition))
            {
                AddCannotBeStaticType(typeSymbol.OriginalDefinition);
            }

            AddTypeComponents(typeSymbol);
        }

        // Adds the types that compose the type (array element type, type arguments, ...), but not the type itself
        private void AddTypeComponents(ITypeSymbol typeSymbol)
        {
            switch (typeSymbol)
            {
                case IArrayTypeSymbol arrayTypeSymbol:
                    AddCannotBeStaticType(arrayTypeSymbol.ElementType);
                    break;

                case IPointerTypeSymbol pointerTypeSymbol:
                    AddCannotBeStaticType(pointerTypeSymbol.PointedAtType);
                    break;

                case IFunctionPointerTypeSymbol functionPointerTypeSymbol:
                    AddMethodSignatureTypes(functionPointerTypeSymbol.Signature);
                    break;

                case INamedTypeSymbol namedTypeSymbol:
                    foreach (var typeArgument in namedTypeSymbol.TypeArguments)
                    {
                        AddCannotBeStaticType(typeArgument);
                    }

                    // Outer<StaticClass>.Inner
                    if (namedTypeSymbol.ContainingType is not null)
                    {
                        AddTypeComponents(namedTypeSymbol.ContainingType);
                    }

                    break;
            }
        }

        private void AddContainingTypeArguments(ISymbol symbol)
        {
            if (symbol.ContainingType is not null)
            {
                AddTypeComponents(symbol.ContainingType);
            }
        }

        private void AddTypeArguments(IMethodSymbol method)
        {
            foreach (var typeArgument in method.TypeArguments)
            {
                AddCannotBeStaticType(typeArgument);
            }

            AddContainingTypeArguments(method);
        }

        private void AddMethodSignatureTypes(IMethodSymbol method)
        {
            AddCannotBeStaticType(method.ReturnType);
            AddParameterTypes(method.Parameters);
            AddTypeParameterConstraintTypes(method.TypeParameters);
        }

        private void AddParameterTypes(ImmutableArray<IParameterSymbol> parameters)
        {
            foreach (var parameter in parameters)
            {
                AddCannotBeStaticType(parameter.Type);
            }
        }

        private void AddTypeParameterConstraintTypes(ImmutableArray<ITypeParameterSymbol> typeParameters)
        {
            foreach (var typeParameter in typeParameters)
            {
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    AddCannotBeStaticType(constraintType);
                }
            }
        }
    }
}
