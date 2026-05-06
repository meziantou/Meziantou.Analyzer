using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Meziantou.Analyzer.Configurations;
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
        title: "Do not use blocking calls when the calling method is async",
        messageFormat: "{0}",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseBlockingCallInAsyncContext));

    private static readonly DiagnosticDescriptor Rule2 = new(
        RuleIdentifiers.DoNotUseBlockingCall,
        title: "Do not use blocking calls, even when the calling method must become async",
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
            private readonly OverloadFinder _overloadFinder;

        private readonly INamedTypeSymbol[] _taskAwaiterLikeSymbols;
        private readonly ConcurrentHashSet<IMethodSymbol> _symbolsWithNoAsyncOverloads = new(SymbolEqualityComparer.Default);

        public Context(Compilation compilation)
        {
            _awaitableTypes = new AwaitableTypes(compilation);
            _overloadFinder = new OverloadFinder(compilation);

            var consoleSymbol = compilation.GetBestTypeByMetadataName("System.Console");
            if (consoleSymbol is not null)
            {
                ConsoleErrorAndOutSymbols = [.. consoleSymbol.GetMembers("Out"), .. consoleSymbol.GetMembers("Error")];
            }
            else
            {
                ConsoleErrorAndOutSymbols = [];
            }

            ProcessSymbol = compilation.GetBestTypeByMetadataName("System.Diagnostics.Process");
            StreamSymbol = compilation.GetBestTypeByMetadataName("System.IO.Stream");
            TextWriterSymbol = compilation.GetBestTypeByMetadataName("System.IO.TextWriter");
            DbConnectionSymbol = compilation.GetBestTypeByMetadataName("System.Data.Common.DbConnection");
            DbCommandSymbol = compilation.GetBestTypeByMetadataName("System.Data.Common.DbCommand");
            DbDataReaderSymbol = compilation.GetBestTypeByMetadataName("System.Data.Common.DbDataReader");
            DbTransactionSymbol = compilation.GetBestTypeByMetadataName("System.Data.Common.DbTransaction");
            DbBatchSymbol = compilation.GetBestTypeByMetadataName("System.Data.Common.DbBatch");
            SqliteConnectionSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Data.Sqlite.SqliteConnection");
            SqliteCommandSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Data.Sqlite.SqliteCommand");
            SqliteDataReaderSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Data.Sqlite.SqliteDataReader");
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
            SemaphoreSlimSymbol = compilation.GetBestTypeByMetadataName("System.Threading.SemaphoreSlim");
            TimeSpanSymbol = compilation.GetBestTypeByMetadataName("System.TimeSpan");

            DbContextSymbol = compilation.GetBestTypeByMetadataName("Microsoft.EntityFrameworkCore.DbContext");
            DbSetSymbol = compilation.GetBestTypeByMetadataName("Microsoft.EntityFrameworkCore.DbSet`1");
            DbContextFactorySymbol = compilation.GetBestTypeByMetadataName("Microsoft.EntityFrameworkCore.IDbContextFactory`1");

            ServiceProviderServiceExtensionsSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions");
            if (ServiceProviderServiceExtensionsSymbol is not null)
            {
                ServiceProviderServiceExtensions_CreateScopeSymbol = ServiceProviderServiceExtensionsSymbol.GetMembers("CreateScope").FirstOrDefault();
                ServiceProviderServiceExtensions_CreateAsyncScopeSymbol = ServiceProviderServiceExtensionsSymbol.GetMembers("CreateAsyncScope").FirstOrDefault();
            }

            Moq_MockSymbol = compilation.GetBestTypeByMetadataName("Moq.Mock`1");

            // Detect test frameworks
            var xunitAssertSymbol = compilation.GetBestTypeByMetadataName("Xunit.Assert");
            var nunitAssertSymbol = compilation.GetBestTypeByMetadataName("NUnit.Framework.Assert");
            var msTestAssertSymbol = compilation.GetBestTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.Assert");
            IsTestProject = xunitAssertSymbol is not null || nunitAssertSymbol is not null || msTestAssertSymbol is not null;

            TemporaryDirectorySymbol = compilation.GetBestTypeByMetadataName("Meziantou.Framework.TemporaryDirectory");

            var taskAwaiterLikeSymbols = new List<INamedTypeSymbol>(4);
            taskAwaiterLikeSymbols.AddIfNotNull(TaskAwaiterSymbol);
            taskAwaiterLikeSymbols.AddIfNotNull(TaskAwaiterOfTSymbol);
            taskAwaiterLikeSymbols.AddIfNotNull(ValueTaskAwaiterSymbol);
            taskAwaiterLikeSymbols.AddIfNotNull(ValueTaskAwaiterOfTSymbol);
            _taskAwaiterLikeSymbols = [.. taskAwaiterLikeSymbols];
        }

        private ISymbol? StreamSymbol { get; }
        private INamedTypeSymbol? TextWriterSymbol { get; }
        private ISymbol? ProcessSymbol { get; }
        private INamedTypeSymbol? DbConnectionSymbol { get; }
        private INamedTypeSymbol? DbCommandSymbol { get; }
        private INamedTypeSymbol? DbDataReaderSymbol { get; }
        private INamedTypeSymbol? DbTransactionSymbol { get; }
        private INamedTypeSymbol? DbBatchSymbol { get; }
        private INamedTypeSymbol? SqliteConnectionSymbol { get; }
        private INamedTypeSymbol? SqliteCommandSymbol { get; }
        private INamedTypeSymbol? SqliteDataReaderSymbol { get; }
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
        private INamedTypeSymbol? SemaphoreSlimSymbol { get; }
        private INamedTypeSymbol? TimeSpanSymbol { get; }

        private INamedTypeSymbol? DbContextSymbol { get; }
        private INamedTypeSymbol? DbSetSymbol { get; }
        private INamedTypeSymbol? DbContextFactorySymbol { get; }

        public INamedTypeSymbol? Moq_MockSymbol { get; }

        private bool IsTestProject { get; }
        private INamedTypeSymbol? TemporaryDirectorySymbol { get; }

        public bool IsValid => TaskSymbol is not null && TaskOfTSymbol is not null && TaskAwaiterSymbol is not null;

        internal void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            var targetMethod = operation.TargetMethod;
            var sqliteSpecialCasesEnabled = IsSqliteSpecialCasesEnabled(context, operation);
            var isSqliteSpecialCaseMethod = IsSqliteSpecialCaseMethod(operation);

            // The cache only contains methods with no async equivalent methods.
            // This optimizes the best-case scenario where code is correctly written according to this analyzer.
            if (!isSqliteSpecialCaseMethod && _symbolsWithNoAsyncOverloads.Contains(targetMethod))
                return;

            if (HasAsyncEquivalent(operation, sqliteSpecialCasesEnabled, out var diagnosticMessage))
            {
                ReportDiagnosticIfNeeded(context, diagnosticMessage.CreateProperties(), operation, diagnosticMessage.DiagnosticMessage);
            }
            else if (!isSqliteSpecialCaseMethod)
            {
                _symbolsWithNoAsyncOverloads.Add(targetMethod);
            }
        }

        private bool HasAsyncEquivalent(IInvocationOperation operation, bool sqliteSpecialCasesEnabled, [NotNullWhen(true)] out DiagnosticData? data)
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
            else if (targetMethod.Name == nameof(Task.Wait) && targetMethod.ContainingType.OriginalDefinition.IsEqualToAny(TaskSymbol, TaskOfTSymbol))
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

            // IDbContextFactory<TContext>.CreateDbContext() - CreateDbContextAsync() is only for specific edge-cases
            // https://github.com/dotnet/efcore/issues/26630
            else if (DbContextFactorySymbol is not null && targetMethod.Name is "CreateDbContext" &&
                     (targetMethod.ContainingType.OriginalDefinition.IsEqualTo(DbContextFactorySymbol) ||
                      targetMethod.ContainingType.ImplementsGenericInterface(DbContextFactorySymbol)))
            {
                return false;
            }

            // Async APIs in Microsoft.Data.Sqlite have documented limitations.
            // Ignore any invocation on SqliteConnection, SqliteCommand, or SqliteDataReader by default.
            // https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async
            else if (sqliteSpecialCasesEnabled && IsSqliteSpecialCaseMethod(operation))
            {
                return false;
            }

            else if (Moq_MockSymbol is not null && targetMethod.Name is "Raise" && targetMethod.ContainingType.OriginalDefinition.IsEqualTo(Moq_MockSymbol))
            {
                return false;
            }

            // SemaphoreSlim.Wait(0) is a non-blocking try-acquire pattern, skip it
            else if (SemaphoreSlimSymbol is not null && targetMethod.Name == "Wait" && targetMethod.ContainingType.IsEqualTo(SemaphoreSlimSymbol) && IsSemaphoreSlimWaitWithZeroTimeout(operation))
            {
                return false;
            }

            // Search async equivalent: sample.Write() => sample.WriteAsync()
            if (!targetMethod.ReturnType.OriginalDefinition.IsEqualToAny(TaskSymbol, TaskOfTSymbol))
            {
                var asyncEquivalentMethod = FindPotentialAsyncEquivalent(operation, targetMethod, targetMethod.Name);
                if (asyncEquivalentMethod is not null)
                {
                    data = new($"Use '{asyncEquivalentMethod.Name}' instead of '{targetMethod.Name}'", DoNotUseBlockingCallInAsyncContextData.Overload, asyncEquivalentMethod.Name);
                    return true;
                }

                if (!targetMethod.Name.EndsWith("Async", StringComparison.Ordinal))
                {
                    asyncEquivalentMethod = FindPotentialAsyncEquivalent(operation, targetMethod, targetMethod.Name + "Async");
                    if (asyncEquivalentMethod is not null)
                    {
                        data = new($"Use '{asyncEquivalentMethod.Name}' instead of '{targetMethod.Name}'", DoNotUseBlockingCallInAsyncContextData.Overload, asyncEquivalentMethod.Name);
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsSqliteSpecialCasesEnabled(OperationAnalysisContext context, IOperation operation)
        {
            var defaultValue = context.Options.GetConfigurationValue(operation, RuleIdentifiers.DoNotUseBlockingCallInAsyncContext + ".enable_sqlite_special_cases", defaultValue: true);
            return context.Options.GetConfigurationValue(operation, RuleIdentifiers.DoNotUseBlockingCall + ".enable_sqlite_special_cases", defaultValue);
        }

        private bool IsSqliteSpecialCaseType(INamedTypeSymbol type)
        {
            return type.IsEqualToAny(SqliteConnectionSymbol, SqliteCommandSymbol, SqliteDataReaderSymbol);
        }

        private bool IsSqliteSpecialCaseMethod(IInvocationOperation operation)
        {
            if (IsSqliteSpecialCaseType(operation.TargetMethod.ContainingType))
                return true;

            if (operation.TargetMethod.IsExtensionMethod)
                return false;

            if (operation.TargetMethod.IsStatic)
                return false;

            if (operation.Instance?.GetActualType() is not INamedTypeSymbol type)
                return false;

            return IsSqliteSpecialCaseType(type);
        }

        private IMethodSymbol? FindPotentialAsyncEquivalent(IInvocationOperation operation, IMethodSymbol targetMethod, string methodName)
        {
            var options = new OverloadOptions(
                AllowOptionalParameters: false,
                IncludeExtensionsMethods: true,
                SyntaxNode: operation.Syntax);

            // When the method name is the same as the original method, and the original is non-generic
            // while a candidate is generic, the compiler will always prefer the non-generic original
            // over the generic candidate. Awaiting the call would therefore still resolve to the
            // non-generic (non-awaitable) method, making the suggested fix invalid.
            var sameNameSearch = string.Equals(methodName, targetMethod.Name, StringComparison.Ordinal);

            foreach (var candidateMethod in _overloadFinder.FindSimilarMethods(targetMethod, options, methodName, default))
            {
                if (sameNameSearch && !targetMethod.IsGenericMethod && candidateMethod.IsGenericMethod)
                    continue;

                if (IsPotentialAsyncEquivalent(operation, candidateMethod))
                    return candidateMethod;
            }

            if (CancellationTokenSymbol is not null)
            {
                foreach (var candidateMethod in _overloadFinder.FindSimilarMethods(targetMethod, options, methodName, [new OverloadParameterType(CancellationTokenSymbol)]))
                {
                    if (sameNameSearch && !targetMethod.IsGenericMethod && candidateMethod.IsGenericMethod)
                        continue;

                    if (IsPotentialAsyncEquivalent(operation, candidateMethod))
                        return candidateMethod;
                }
            }

            return null;
        }

        private bool IsPotentialAsyncEquivalent(IInvocationOperation operation, IMethodSymbol methodSymbol)
        {
            if (!_awaitableTypes.IsAwaitable(methodSymbol.ReturnType, operation.SemanticModel!, operation.Syntax.SpanStart))
                return false;

            // In test projects, exclude async methods from Meziantou.Framework.TemporaryDirectory
            if (IsTestProject && TemporaryDirectorySymbol is not null && methodSymbol.ContainingType.IsEqualTo(TemporaryDirectorySymbol))
                return false;

            return true;
        }

        private bool IsSemaphoreSlimWaitWithZeroTimeout(IInvocationOperation operation)
        {
            if (operation.Arguments.Length == 0)
                return false;

            var firstArgument = operation.Arguments[0];
            var constantValue = firstArgument.Value.ConstantValue;

            // Check for Wait(0) - integer literal 0
            if (constantValue.HasValue && constantValue.Value is int intValue && intValue == 0)
                return true;

            // Check for Wait(TimeSpan.Zero)
            if (TimeSpanSymbol is not null && firstArgument.Value is IMemberReferenceOperation memberRef)
            {
                if (memberRef.Member.Name == nameof(TimeSpan.Zero) && memberRef.Member.ContainingType.IsEqualTo(TimeSpanSymbol))
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

        /// <summary>
        /// Checks whether any type in the hierarchy from <paramref name="symbol"/> up to (but NOT including)
        /// <paramref name="baseTypeSymbol"/> declares or overrides a <c>DisposeAsync</c> method.
        /// Used to detect whether a subclass has a meaningful (truly async) <c>DisposeAsync</c> override,
        /// as opposed to relying on an inherited implementation that is not truly asynchronous.
        /// </summary>
        private bool HasDisposeAsyncMethodDeclaredInSubclass(INamedTypeSymbol symbol, INamedTypeSymbol baseTypeSymbol)
        {
            var current = symbol;
            while (current is not null && !current.IsEqualTo(baseTypeSymbol))
            {
                foreach (var member in current.GetMembers("DisposeAsync").OfType<IMethodSymbol>())
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

                current = current.BaseType;
            }

            return false;
        }

        private bool CanBeAwaitUsing(IOperation operation, bool sqliteSpecialCasesEnabled)
        {
            var unwrappedOperation = operation.UnwrapImplicitConversionOperations();
            if (sqliteSpecialCasesEnabled &&
                unwrappedOperation is IInvocationOperation invocationOperation &&
                IsSqliteSpecialCaseMethod(invocationOperation))
            {
                return false;
            }

            if (operation.GetActualType() is not INamedTypeSymbol type)
                return false;

            // For Stream subclasses (including MemoryStream) created directly (new T()), only report
            // if the concrete type being instantiated (or an intermediate subclass up to but not
            // including Stream) actually overrides DisposeAsync. Stream.DisposeAsync merely calls
            // Dispose() synchronously by default, so it is not a meaningful async override.
            if (StreamSymbol is INamedTypeSymbol streamSymbol && type.InheritsFrom(streamSymbol))
            {
                if (unwrappedOperation is IObjectCreationOperation)
                    return HasDisposeAsyncMethodDeclaredInSubclass(type, streamSymbol);
            }

            // For DbConnection subclasses created directly (new T()), only report if the exact
            // type being instantiated (or an intermediate subclass up to but not including
            // DbConnection) actually overrides DisposeAsync. DbConnection.DisposeAsync just calls
            // Dispose() synchronously, so it is not a meaningful async override.
            if (DbConnectionSymbol is not null && type.InheritsFrom(DbConnectionSymbol))
            {
                if (unwrappedOperation is IObjectCreationOperation)
                    return HasDisposeAsyncMethodDeclaredInSubclass(type, DbConnectionSymbol);
            }

            // For DbCommand subclasses created directly (new T()), only report if the exact
            // type being instantiated (or an intermediate subclass up to but not including
            // DbCommand) actually overrides DisposeAsync. DbCommand.DisposeAsync just calls
            // Dispose() synchronously, so it is not a meaningful async override.
            if (DbCommandSymbol is not null && type.InheritsFrom(DbCommandSymbol))
            {
                if (unwrappedOperation is IObjectCreationOperation)
                    return HasDisposeAsyncMethodDeclaredInSubclass(type, DbCommandSymbol);
            }

            // For DbDataReader subclasses created directly (new T()), only report if the exact
            // type being instantiated (or an intermediate subclass up to but not including
            // DbDataReader) actually overrides DisposeAsync. DbDataReader.DisposeAsync just calls
            // Dispose() synchronously, so it is not a meaningful async override.
            if (DbDataReaderSymbol is not null && type.InheritsFrom(DbDataReaderSymbol))
            {
                if (unwrappedOperation is IObjectCreationOperation)
                    return HasDisposeAsyncMethodDeclaredInSubclass(type, DbDataReaderSymbol);
            }

            // For DbTransaction subclasses created directly (new T()), only report if the exact
            // type being instantiated (or an intermediate subclass up to but not including
            // DbTransaction) actually overrides DisposeAsync. DbTransaction.DisposeAsync just calls
            // Dispose() synchronously, so it is not a meaningful async override.
            if (DbTransactionSymbol is not null && type.InheritsFrom(DbTransactionSymbol))
            {
                if (unwrappedOperation is IObjectCreationOperation)
                    return HasDisposeAsyncMethodDeclaredInSubclass(type, DbTransactionSymbol);
            }

            // For DbBatch subclasses created directly (new T()), only report if the exact
            // type being instantiated (or an intermediate subclass up to but not including
            // DbBatch) actually overrides DisposeAsync. DbBatch.DisposeAsync just calls
            // Dispose() synchronously, so it is not a meaningful async override.
            if (DbBatchSymbol is not null && type.InheritsFrom(DbBatchSymbol))
            {
                if (unwrappedOperation is IObjectCreationOperation)
                    return HasDisposeAsyncMethodDeclaredInSubclass(type, DbBatchSymbol);
            }

            // For TextWriter subclasses created directly (new T()), only report if the exact
            // type being instantiated (or an intermediate subclass up to but not including
            // TextWriter) actually overrides DisposeAsync. TextWriter.DisposeAsync just calls
            // Dispose() synchronously by default, so it is not a meaningful async override.
            if (TextWriterSymbol is not null && type.InheritsFrom(TextWriterSymbol))
            {
                if (unwrappedOperation is IObjectCreationOperation)
                    return HasDisposeAsyncMethodDeclaredInSubclass(type, TextWriterSymbol);
            }

            return HasDisposeAsyncMethod(type);
        }

        private bool ReportIfCanBeAwaitUsing(OperationAnalysisContext context, IOperation usingOperation, IVariableDeclarationGroupOperation operation, bool sqliteSpecialCasesEnabled)
        {
            foreach (var declaration in operation.Declarations)
            {
                if ((declaration.Initializer?.Value) is not null)
                {
                    if (CanBeAwaitUsing(declaration.Initializer.Value, sqliteSpecialCasesEnabled))
                    {
                        var data = new DiagnosticData("Prefer using 'await using'", DoNotUseBlockingCallInAsyncContextData.Using);
                        ReportDiagnosticIfNeeded(context, data.CreateProperties(), usingOperation, data.DiagnosticMessage);
                        return true;
                    }
                }

                foreach (var declarator in declaration.Declarators)
                {
                    if (declarator.Initializer is not null && CanBeAwaitUsing(declarator.Initializer.Value, sqliteSpecialCasesEnabled))
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

            var sqliteSpecialCasesEnabled = IsSqliteSpecialCasesEnabled(context, operation);
            if (operation.Resources is IVariableDeclarationGroupOperation variableDeclarationGroupOperation)
            {
                if (ReportIfCanBeAwaitUsing(context, operation, variableDeclarationGroupOperation, sqliteSpecialCasesEnabled))
                    return;
            }

            if (CanBeAwaitUsing(operation.Resources, sqliteSpecialCasesEnabled))
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

            var sqliteSpecialCasesEnabled = IsSqliteSpecialCasesEnabled(context, operation);
            ReportIfCanBeAwaitUsing(context, operation, operation.DeclarationGroup, sqliteSpecialCasesEnabled);
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
