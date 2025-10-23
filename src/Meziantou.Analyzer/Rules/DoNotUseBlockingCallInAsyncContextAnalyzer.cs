using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseBlockingCallInAsyncContextAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseBlockingCallInAsyncContext,
        title: "Do not use blocking calls in an async method",
        messageFormat: "{0}",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseBlockingCallInAsyncContext));

    private static readonly DiagnosticDescriptor Rule2 = new(
        RuleIdentifiers.DoNotUseBlockingCall,
        title: "Do not use blocking calls in a sync method (need to make calling method async)",
        messageFormat: "{0}",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseBlockingCall));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, Rule2);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new Context(ctx.Compilation);
            if (analyzerContext.IsValid)
            {
                ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
                ctx.RegisterOperationAction(analyzerContext.AnalyzePropertyReference, OperationKind.PropertyReference);
                ctx.RegisterOperationAction(analyzerContext.AnalyzeUsing, OperationKind.Using);
                ctx.RegisterOperationAction(analyzerContext.AnalyzeUsingDeclaration, OperationKind.UsingDeclaration);
            }
        });
    }

    private sealed class Context
    {
        private static readonly Version Version6 = new(6, 0, 0, 0);

        private readonly AwaitableTypes _awaitableTypes;

        private readonly INamedTypeSymbol[] _taskAwaiterLikeSymbols;
        private readonly ConcurrentHashSet<IMethodSymbol> _symbolsWithNoAsyncOverloads = new(SymbolEqualityComparer.Default);

        public Context(Compilation compilation)
        {
            _awaitableTypes = new AwaitableTypes(compilation);

            var consoleSymbol = compilation.GetBestTypeByMetadataName("System.Console");
            if (consoleSymbol is not null)
            {
                ConsoleErrorAndOutSymbols = [.. consoleSymbol.GetMembers("Out"), .. consoleSymbol.GetMembers("Error")];
            }
            else
            {
                ConsoleErrorAndOutSymbols = [];
            }

            MemoryStreamSymbol = compilation.GetBestTypeByMetadataName("System.IO.MemoryStream");
            ProcessSymbol = compilation.GetBestTypeByMetadataName("System.Diagnostics.Process");
            CancellationTokenSymbol = compilation.GetBestTypeByMetadataName("System.Threading.CancellationToken");
            ObsoleteAttributeSymbol = compilation.GetBestTypeByMetadataName("System.ObsoleteAttribute");

            TaskSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
            TaskOfTSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");
            TaskAwaiterSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.TaskAwaiter");
            TaskAwaiterOfTSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.TaskAwaiter`1");

            ValueTaskSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask");
            ValueTaskOfTSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
            ValueTaskAwaiterSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ValueTaskAwaiter");
            ValueTaskAwaiterOfTSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ValueTaskAwaiter`1");

            ThreadSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Thread");

            DbContextSymbol = compilation.GetBestTypeByMetadataName("Microsoft.EntityFrameworkCore.DbContext");
            DbSetSymbol = compilation.GetBestTypeByMetadataName("Microsoft.EntityFrameworkCore.DbSet`1");

            ServiceProviderServiceExtensionsSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions");
            if (ServiceProviderServiceExtensionsSymbol is not null)
            {
                ServiceProviderServiceExtensions_CreateScopeSymbol = ServiceProviderServiceExtensionsSymbol.GetMembers("CreateScope").FirstOrDefault();
                ServiceProviderServiceExtensions_CreateAsyncScopeSymbol = ServiceProviderServiceExtensionsSymbol.GetMembers("CreateAsyncScope").FirstOrDefault();
            }

            Moq_MockSymbol = compilation.GetBestTypeByMetadataName("Moq.Mock`1");

            var taskAwaiterLikeSymbols = new List<INamedTypeSymbol>(4);
            taskAwaiterLikeSymbols.AddIfNotNull(TaskAwaiterSymbol);
            taskAwaiterLikeSymbols.AddIfNotNull(TaskAwaiterOfTSymbol);
            taskAwaiterLikeSymbols.AddIfNotNull(ValueTaskAwaiterSymbol);
            taskAwaiterLikeSymbols.AddIfNotNull(ValueTaskAwaiterOfTSymbol);
            _taskAwaiterLikeSymbols = [.. taskAwaiterLikeSymbols];
        }

        private ISymbol? MemoryStreamSymbol { get; }
        private ISymbol? ProcessSymbol { get; }
        private ISymbol[] ConsoleErrorAndOutSymbols { get; }
        private INamedTypeSymbol? CancellationTokenSymbol { get; }
        private INamedTypeSymbol? ObsoleteAttributeSymbol { get; }
        private INamedTypeSymbol? ServiceProviderServiceExtensionsSymbol { get; }
        private ISymbol? ServiceProviderServiceExtensions_CreateScopeSymbol { get; }
        private ISymbol? ServiceProviderServiceExtensions_CreateAsyncScopeSymbol { get; }

        private INamedTypeSymbol? TaskSymbol { get; }
        private INamedTypeSymbol? TaskOfTSymbol { get; }
        private INamedTypeSymbol? TaskAwaiterSymbol { get; }
        private INamedTypeSymbol? TaskAwaiterOfTSymbol { get; }

        private INamedTypeSymbol? ValueTaskSymbol { get; }
        private INamedTypeSymbol? ValueTaskOfTSymbol { get; }
        private INamedTypeSymbol? ValueTaskAwaiterSymbol { get; }
        private INamedTypeSymbol? ValueTaskAwaiterOfTSymbol { get; }

        private INamedTypeSymbol? ThreadSymbol { get; }

        private INamedTypeSymbol? DbContextSymbol { get; }
        private INamedTypeSymbol? DbSetSymbol { get; }

        public INamedTypeSymbol? Moq_MockSymbol { get; }

        public bool IsValid => TaskSymbol is not null && TaskOfTSymbol is not null && TaskAwaiterSymbol is not null;

        internal void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            var targetMethod = operation.TargetMethod;

            // The cache only contains methods with no async equivalent methods.
            // This optimizes the best-case scenario where code is correctly written according to this analyzer.
            if (_symbolsWithNoAsyncOverloads.Contains(targetMethod))
                return;

            if (HasAsyncEquivalent(operation, out var diagnosticMessage))
            {
                ReportDiagnosticIfNeeded(context, diagnosticMessage.CreateProperties(), operation, diagnosticMessage.DiagnosticMessage);
            }
            else
            {
                _symbolsWithNoAsyncOverloads.Add(targetMethod);
            }
        }

        private bool HasAsyncEquivalent(IInvocationOperation operation, [NotNullWhen(true)] out DiagnosticData? data)
        {
            data = null;
            var targetMethod = operation.TargetMethod;

            if (_awaitableTypes.IsAwaitable(targetMethod.ReturnType, operation.SemanticModel!, operation.Syntax.SpanStart))
                return false;

            // Process.WaitForExit => Skip because the async method is not equivalent https://github.com/dotnet/runtime/issues/42556
            if (targetMethod.Name == nameof(System.Diagnostics.Process.WaitForExit) && targetMethod.ContainingType.IsEqualTo(ProcessSymbol))
            {
                if (targetMethod.ContainingType.ContainingAssembly.Identity.Version < Version6)
                    return false;
            }

            // Task.Wait()
            // Task`1.Wait()
            else if (targetMethod.Name == nameof(Task.Wait))
            {
                if (targetMethod.ContainingType.OriginalDefinition.IsEqualToAny(TaskSymbol, TaskOfTSymbol))
                {
                    if (operation.Arguments.Length == 0)
                    {
                        data = new("Use await instead of 'Wait()'", DoNotUseBlockingCallInAsyncContextData.Task_Wait);
                        return true;
                    }
                    else
                    {
                        data = new("Use 'WaitAsync' instead of 'Wait()'", DoNotUseBlockingCallInAsyncContextData.Task_Wait_Delay);
                        return true;
                    }
                }
            }

            // Task.GetAwaiter().GetResult()
            else if (targetMethod.Name == nameof(TaskAwaiter.GetResult))
            {
                if (targetMethod.ContainingType.OriginalDefinition.IsEqualToAny(_taskAwaiterLikeSymbols))
                {
                    data = new("Use await instead of 'GetResult()'", DoNotUseBlockingCallInAsyncContextData.TaskAwaiter_GetResult);
                    return true;
                }
            }

            // Thread.Sleep => Task.Delay
            else if (targetMethod.Name == "Sleep")
            {
                if (targetMethod.ContainingType.IsEqualTo(ThreadSymbol))
                {
                    data = new("Use await and 'Task.Delay()' instead of 'Thread.Sleep()'", DoNotUseBlockingCallInAsyncContextData.Thread_Sleep);
                    return true;
                }
            }

            // Console.Out|Error.Write
            else if (targetMethod.Name is "WriteLine" or "Write" or "Flush")
            {
                var left = operation.GetChildOperations().FirstOrDefault();
                if (left is IMemberReferenceOperation memberReference)
                {
                    if (ConsoleErrorAndOutSymbols.Contains(memberReference.Member, SymbolEqualityComparer.Default))
                        return false;
                }
            }

            else if (ServiceProviderServiceExtensions_CreateAsyncScopeSymbol is not null && ServiceProviderServiceExtensions_CreateScopeSymbol is not null && targetMethod.IsEqualTo(ServiceProviderServiceExtensions_CreateScopeSymbol))
            {
                data = new($"Use 'CreateAsyncScope' instead of '{targetMethod.Name}'", DoNotUseBlockingCallInAsyncContextData.CreateAsyncScope);
                return true;
            }

            // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.addasync?view=efcore-6.0&WT.mc_id=DT-MVP-5003978#overloads
            else if ((DbContextSymbol is not null || DbSetSymbol is not null) && targetMethod.Name is "Add" or "AddRange" && targetMethod.ContainingType.OriginalDefinition.IsEqualToAny(DbContextSymbol, DbSetSymbol))
            {
                return false;
            }

            else if (Moq_MockSymbol is not null && targetMethod.Name is "Raise" && targetMethod.ContainingType.OriginalDefinition.IsEqualTo(Moq_MockSymbol))
            {
                return false;
            }

            // Search async equivalent: sample.Write() => sample.WriteAsync()
            if (!targetMethod.ReturnType.OriginalDefinition.IsEqualToAny(TaskSymbol, TaskOfTSymbol))
            {
                var position = operation.Syntax.GetLocation().SourceSpan.End;

                var result = ProcessSymbols(operation.SemanticModel!.LookupSymbols(position, targetMethod.ContainingType, name: targetMethod.Name, includeReducedExtensionMethods: true));
                if (result is not null)
                {
                    data = result;
                    return true;
                }

                if (!targetMethod.Name.EndsWith("Async", StringComparison.Ordinal))
                {
                    result = ProcessSymbols(operation.SemanticModel!.LookupSymbols(position, targetMethod.ContainingType, name: targetMethod.Name + "Async", includeReducedExtensionMethods: true));
                    if (result is not null)
                    {
                        data = result;
                        return true;
                    }
                }

                DiagnosticData? ProcessSymbols(ImmutableArray<ISymbol> potentialMethods)
                {
                    foreach (var potentialMethod in potentialMethods)
                    {
                        if (IsPotentialMember(operation, targetMethod, potentialMethod))
                        {
                            return new($"Use '{potentialMethod.Name}' instead of '{targetMethod.Name}'", DoNotUseBlockingCallInAsyncContextData.Overload, potentialMethod.Name);
                        }
                    }

                    return null;
                }
            }

            return false;
        }

        private bool IsPotentialMember(IInvocationOperation operation, IMethodSymbol method, ISymbol potentialAsyncSymbol)
        {
            if (potentialAsyncSymbol.IsEqualTo(method))
                return false;

            if (potentialAsyncSymbol is IMethodSymbol methodSymbol)
            {
                if (method.IsStatic && !methodSymbol.IsStatic)
                    return false;

                if (!_awaitableTypes.IsAwaitable(methodSymbol.ReturnType, operation.SemanticModel!, operation.Syntax.SpanStart))
                    return false;

                if (methodSymbol.HasAttribute(ObsoleteAttributeSymbol))
                    return false;

                if (OverloadFinder.HasSimilarParameters(method, methodSymbol, allowOptionalParameters: false))
                    return true;

                if (CancellationTokenSymbol is not null && OverloadFinder.HasSimilarParameters(method, methodSymbol, allowOptionalParameters: false, CancellationTokenSymbol))
                    return true;
            }

            return false;
        }

        internal void AnalyzePropertyReference(OperationAnalysisContext context)
        {
            var operation = (IPropertyReferenceOperation)context.Operation;

            if (operation.IsInNameofOperation())
                return;

            // Task`1.Result
            if (string.Equals(operation.Property.Name, nameof(Task<>.Result), StringComparison.Ordinal))
            {
                if (operation.Member.ContainingType.OriginalDefinition.IsEqualToAny(TaskOfTSymbol, ValueTaskOfTSymbol))
                {
                    var data = new DiagnosticData("Use await instead of 'Result'", DoNotUseBlockingCallInAsyncContextData.Task_Result);
                    ReportDiagnosticIfNeeded(context, data.CreateProperties(), operation, data.DiagnosticMessage);
                }
            }
        }

        private void ReportDiagnosticIfNeeded(OperationAnalysisContext context, ImmutableDictionary<string, string?>? properties, IOperation operation, string message)
        {
            if (!CanBeAsync(operation))
                return;

            if (IsAsyncContext(operation, context.CancellationToken))
            {
                context.ReportDiagnostic(Rule, properties, operation, message);
            }
            else if (CanChangeParentMethodSignature(operation, context.CancellationToken))
            {
                context.ReportDiagnostic(Rule2, properties, operation, message + " and make method async");
            }
        }

        private bool IsAsyncContext(IOperation operation, CancellationToken cancellationToken)
        {
            // lambda, delegate, method, local function
            // Check if returns Task or async void
            if (operation.SemanticModel!.GetEnclosingSymbol(operation.Syntax.SpanStart, cancellationToken) is IMethodSymbol methodSymbol)
            {
                if (_awaitableTypes.DoesNotReturnVoidAndCanUseAsyncKeyword(methodSymbol, operation.SemanticModel, cancellationToken))
                    return true;
            }

            return false;
        }

        private static bool CanChangeParentMethodSignature(IOperation operation, CancellationToken cancellationToken)
        {
            var symbol = operation.SemanticModel!.GetEnclosingSymbol(operation.Syntax.SpanStart, cancellationToken);
            if (symbol is IMethodSymbol methodSymbol)
            {
                return !methodSymbol.IsOverrideOrInterfaceImplementation()
                    && !methodSymbol.IsVisibleOutsideOfAssembly();
            }

            return false;
        }

        private static bool CanBeAsync(IOperation operation)
        {
            if (operation.Ancestors().Any(op => op is ILockOperation))
                return false;

            return true;
        }

        private bool HasDisposeAsyncMethod(INamedTypeSymbol symbol)
        {
            var members = symbol.GetAllMembers("DisposeAsync");
            foreach (var member in members.OfType<IMethodSymbol>())
            {
                if (member.Parameters.Length != 0)
                    continue;

                if (member.IsGenericMethod)
                    continue;

                if (member.IsStatic)
                    continue;

                if (!member.ReturnType.IsEqualTo(ValueTaskSymbol))
                    continue;

                return true;
            }

            return false;
        }

        private bool CanBeAwaitUsing(IOperation operation)
        {
            if (operation.GetActualType() is not INamedTypeSymbol type)
                return false;

            // using var ms = new MemoryStream();
            if (operation is IObjectCreationOperation objectCreationOperation && objectCreationOperation.Type.IsEqualTo(MemoryStreamSymbol))
                return false;

            return HasDisposeAsyncMethod(type);
        }

        private bool ReportIfCanBeAwaitUsing(OperationAnalysisContext context, IOperation usingOperation, IVariableDeclarationGroupOperation operation)
        {
            foreach (var declaration in operation.Declarations)
            {
                if ((declaration.Initializer?.Value) is not null)
                {
                    if (CanBeAwaitUsing(declaration.Initializer.Value))
                    {
                        var data = new DiagnosticData("Prefer using 'await using'", DoNotUseBlockingCallInAsyncContextData.Using);
                        ReportDiagnosticIfNeeded(context, data.CreateProperties(), usingOperation, data.DiagnosticMessage);
                        return true;
                    }
                }

                foreach (var declarator in declaration.Declarators)
                {
                    if (declarator.Initializer is not null && CanBeAwaitUsing(declarator.Initializer.Value))
                    {
                        var data = new DiagnosticData("Prefer using 'await using'", DoNotUseBlockingCallInAsyncContextData.UsingDeclarator);
                        ReportDiagnosticIfNeeded(context, data.CreateProperties(), usingOperation, data.DiagnosticMessage);
                        return true;
                    }
                }
            }

            return false;
        }

        internal void AnalyzeUsing(OperationAnalysisContext context)
        {
            var operation = (IUsingOperation)context.Operation;
            if (operation.IsAsynchronous)
                return;

            if (operation.Resources is IVariableDeclarationGroupOperation variableDeclarationGroupOperation)
            {
                if (ReportIfCanBeAwaitUsing(context, operation, variableDeclarationGroupOperation))
                    return;
            }

            if (CanBeAwaitUsing(operation.Resources))
            {
                var data = new DiagnosticData("Prefer using 'await using'", DoNotUseBlockingCallInAsyncContextData.Using);
                ReportDiagnosticIfNeeded(context, data.CreateProperties(), operation, data.DiagnosticMessage);
            }
        }

        internal void AnalyzeUsingDeclaration(OperationAnalysisContext context)
        {
            var operation = (IUsingDeclarationOperation)context.Operation;
            if (operation.IsAsynchronous)
                return;

            ReportIfCanBeAwaitUsing(context, operation, operation.DeclarationGroup);
        }
    }

    private sealed class DiagnosticData
    {
        public string DiagnosticMessage { get; }
        public DoNotUseBlockingCallInAsyncContextData Data { get; }
        public string? AsyncMethodName { get; }

        public DiagnosticData(string diagnosticMessage, DoNotUseBlockingCallInAsyncContextData data)
            : this(diagnosticMessage, data, asyncMethodName: null)
        {
        }

        public DiagnosticData(string diagnosticMessage, DoNotUseBlockingCallInAsyncContextData data, string? asyncMethodName)
        {
            DiagnosticMessage = diagnosticMessage ?? throw new ArgumentNullException(nameof(diagnosticMessage));
            Data = data;
            AsyncMethodName = asyncMethodName;
        }

        public ImmutableDictionary<string, string?> CreateProperties()
        {
            return ImmutableDictionary<string, string?>.Empty
                .Add("Data", Data.ToString())
                .Add("MethodName", AsyncMethodName);
        }
    }
}
