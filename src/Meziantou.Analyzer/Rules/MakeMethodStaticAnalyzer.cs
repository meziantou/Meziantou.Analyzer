using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MakeMethodStaticAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor MethodRule = new(
        RuleIdentifiers.MakeMethodStatic,
        title: "Make method static (deprecated, use CA1822 instead)",
        messageFormat: "Make method static (deprecated, use CA1822 instead)",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MakeMethodStatic));

    private static readonly DiagnosticDescriptor PropertyRule = new(
     RuleIdentifiers.MakePropertyStatic,
     title: "Make property static (deprecated, use CA1822 instead)",
     messageFormat: "Make property static (deprecated, use CA1822 instead)",
     RuleCategories.Design,
     DiagnosticSeverity.Info,
     isEnabledByDefault: true,
     description: "",
     helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MakePropertyStatic));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(MethodRule, PropertyRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.Analyze);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);

            ctx.RegisterOperationBlockAction(analyzerContext.AnalyzeOperationBlock);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeDelegateCreation, OperationKind.DelegateCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeMemberReference, OperationKind.Invocation, OperationKind.MethodReference, OperationKind.PropertyReference);
            ctx.RegisterCompilationEndAction(analyzerContext.CompilationEnd);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly ConcurrentHashSet<ISymbol> _potentialSymbols = new(SymbolEqualityComparer.Default);
        private readonly ConcurrentHashSet<ISymbol> _cannotBeStaticSymbols = new(SymbolEqualityComparer.Default);

        private readonly ITypeSymbol? _httpContextSymbol = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpContext");
        private readonly ITypeSymbol? _iapplicationBuilder = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Builder.IApplicationBuilder");
        private readonly ITypeSymbol? _iserviceCollectionSymbol = compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection");
        private readonly ITypeSymbol? _imiddlewareSymbol = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IMiddleware");

        public void CompilationEnd(CompilationAnalysisContext context)
        {
            foreach (var symbol in _potentialSymbols)
            {
                if (_cannotBeStaticSymbols.Contains(symbol))
                    continue;

                if (symbol is IMethodSymbol)
                {
                    context.ReportDiagnostic(MethodRule, symbol);
                }
                else if (symbol is IPropertySymbol)
                {
                    context.ReportDiagnostic(PropertyRule, symbol);
                }
                else
                {
                    throw new InvalidOperationException("Symbol is not supported: " + symbol);
                }
            }
        }

        public void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
        {
            if (context.OwningSymbol is not IMethodSymbol methodSymbol)
                return;

            switch (methodSymbol)
            {
                case { MethodKind: MethodKind.Ordinary }:
                    AnalyzeMethod(context, methodSymbol);
                    break;

                case { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet, AssociatedSymbol: IPropertySymbol propertySymbol }:
                    AnalyzeProperty(context, propertySymbol);
                    break;
            }
        }

        private void AnalyzeMethod(OperationBlockAnalysisContext context, IMethodSymbol methodSymbol)
        {
            if (!IsDeclaredBy<MethodDeclarationSyntax>(methodSymbol, context.CancellationToken))
                return;

            if (!IsPotentialStatic(methodSymbol) || methodSymbol.IsUnitTestMethod() || IsAspNetCoreMiddleware(methodSymbol) || IsAspNetCoreStartup(methodSymbol))
                return;

            if (HasInstanceUsages(context.OperationBlocks))
                return;

            _potentialSymbols.Add(methodSymbol);
        }

        /// <summary>
        /// The accessors of a property are analyzed separately, so the property can be made static when none of
        /// its accessors uses the instance. The auto-implemented accessors have no operation block, and use the instance.
        /// </summary>
        private void AnalyzeProperty(OperationBlockAnalysisContext context, IPropertySymbol propertySymbol)
        {
            if (!IsPotentialStatic(propertySymbol))
                return;

            if (propertySymbol.DeclaringSyntaxReferences is not [var syntaxReference] || syntaxReference.GetSyntax(context.CancellationToken) is not PropertyDeclarationSyntax node)
                return;

            if (HasInstanceUsages(context.OperationBlocks))
            {
                _cannotBeStaticSymbols.Add(propertySymbol);
                return;
            }

            if (node.AccessorList is not null && node.AccessorList.Accessors.Any(accessor => accessor.Body is null && accessor.ExpressionBody is null))
                return;

            _potentialSymbols.Add(propertySymbol);
        }

        private static bool IsDeclaredBy<TSyntax>(ISymbol symbol, CancellationToken cancellationToken)
            where TSyntax : SyntaxNode
        {
            foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax(cancellationToken) is TSyntax)
                    return true;
            }

            return false;
        }

        public void AnalyzeDelegateCreation(OperationAnalysisContext context)
        {
            var operation = (IDelegateCreationOperation)context.Operation;
            if (operation.Target is not IMethodReferenceOperation methodReference)
                return;

            // xaml cannot add event to static methods
            if (IsInXamlGeneratedFile(operation))
            {
                _cannotBeStaticSymbols.Add(methodReference.Method);
            }
        }

        /// <summary>
        /// A static member cannot be accessed through an instance (CS0176), nor in an object initializer or a property pattern.
        /// The code fixer replaces <c>this.Member</c> with <c>Type.Member</c>, but the other instances cannot be removed, as
        /// they can have side effects.
        /// </summary>
        public void AnalyzeMemberReference(OperationAnalysisContext context)
        {
            var (member, instance) = context.Operation switch
            {
                IInvocationOperation operation => ((ISymbol)operation.TargetMethod, operation.Instance),
                IMemberReferenceOperation operation => (operation.Member, operation.Instance),
                _ => default,
            };

            if (member is null || instance is null or IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance })
                return;

            if (member.IsStatic || !member.ContainingAssembly.IsEqualTo(compilation.Assembly))
                return;

            _cannotBeStaticSymbols.Add(member.OriginalDefinition);
        }

        private static bool IsPotentialStatic(IMethodSymbol symbol)
        {
            return
                !symbol.IsAbstract &&
                !symbol.IsVirtual &&
                !symbol.IsOverride &&
                !symbol.IsStatic &&
                !symbol.IsInterfaceImplementation() &&
                !IsPatternBasedMember(symbol) &&
                symbol.PartialDefinitionPart is null;
        }

        private static bool IsPotentialStatic(IPropertySymbol symbol)
        {
            return
                !symbol.IsAbstract &&
                !symbol.IsVirtual &&
                !symbol.IsOverride &&
                !symbol.IsStatic &&
                !symbol.IsInterfaceImplementation() &&
                !IsPatternBasedMember(symbol);
        }

        /// <summary>
        /// The compiler binds some members by name on an instance, without operations referencing them: foreach, await,
        /// using, deconstruction, collection initializers and collection expressions, fixed statements, indexes and ranges.
        /// They cannot be static.
        /// </summary>
        private static bool IsPatternBasedMember(ISymbol symbol)
        {
            return symbol switch
            {
                IMethodSymbol => symbol.Name is "GetEnumerator" or "GetAsyncEnumerator" or "MoveNext" or "MoveNextAsync" or "Dispose" or "DisposeAsync" or "GetAwaiter" or "GetResult" or "OnCompleted" or "UnsafeOnCompleted" or "Deconstruct" or "Add" or "GetPinnableReference" or "Slice",
                IPropertySymbol => symbol.Name is "Current" or "IsCompleted" or "Length" or "Count",
                _ => false,
            };
        }

        private static bool HasInstanceUsages(ImmutableArray<IOperation> operationBlocks)
        {
            var operations = new Queue<IOperation>(operationBlocks);

            while (operations.Count > 0)
            {
                var op = operations.Dequeue();
                foreach (var child in op.GetChildOperations())
                {
                    operations.Enqueue(child);
                }

                switch (op)
                {
                    case IInstanceReferenceOperation instanceReferenceOperation when instanceReferenceOperation.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance:
                        return true;

                    // The parameters of a constructor can only be referenced from a method or an accessor when they are the parameters of the primary constructor, which are captured by the instance
                    case IParameterReferenceOperation { Parameter.ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Constructor } }:
                        return true;
                }
            }

            return false;
        }

        private bool IsAspNetCoreMiddleware(IMethodSymbol methodSymbol)
        {
            if (string.Equals(methodSymbol.Name, "Invoke", StringComparison.Ordinal) ||
                string.Equals(methodSymbol.Name, "InvokeAsync", StringComparison.Ordinal))
            {
                if (methodSymbol.Parameters.Length == 0 || !methodSymbol.Parameters[0].Type.IsEqualTo(_httpContextSymbol))
                    return false;

                return true;
            }

            if (methodSymbol.ContainingType.Implements(_imiddlewareSymbol))
            {
                var invokeAsyncSymbol = _imiddlewareSymbol.GetMembers("InvokeAsync").FirstOrDefault();
                if (invokeAsyncSymbol is not null)
                {
                    var implementationMember = methodSymbol.ContainingType.FindImplementationForInterfaceMember(invokeAsyncSymbol);
                    if (methodSymbol.IsEqualTo(implementationMember))
                        return true;
                }
            }

            return false;
        }

        private bool IsAspNetCoreStartup(IMethodSymbol methodSymbol)
        {
            // void ConfigureServices Microsoft.Extensions.DependencyInjection.IServiceCollection
            if (string.Equals(methodSymbol.Name, "ConfigureServices", StringComparison.Ordinal))
            {
                if (methodSymbol.ReturnsVoid && methodSymbol.Parameters.Length == 1 && methodSymbol.Parameters[0].Type.IsEqualTo(_iserviceCollectionSymbol))
                    return true;

                return false;
            }

            // void Configure Microsoft.AspNetCore.Builder.IApplicationBuilder
            if (string.Equals(methodSymbol.Name, "Configure", StringComparison.Ordinal))
            {
                if (methodSymbol.Parameters.Length > 0 && methodSymbol.Parameters[0].Type.IsEqualTo(_iapplicationBuilder))
                    return true;

                return false;
            }

            return false;
        }

        private static bool IsInXamlGeneratedFile(IOperation operation)
        {
            return operation.Syntax.GetLocation().GetMappedLineSpan().Path?.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }
}
