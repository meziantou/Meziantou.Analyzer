using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseBlockingCallInAsyncContextAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.DoNotUseBlockingCallInAsyncContext,
        title: "Do not use blocking calls in an async method",
        messageFormat: "{0}",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseBlockingCallInAsyncContext));

    private static readonly DiagnosticDescriptor s_rule2 = new(
        RuleIdentifiers.DoNotUseBlockingCall,
        title: "Do not use blocking call in a sync method (need to make containing method async)",
        messageFormat: "{0}",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseBlockingCall));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule, s_rule2);

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
        private readonly Compilation _compilation;

        public Context(Compilation compilation)
        {
            _compilation = compilation;
            var consoleSymbol = _compilation.GetBestTypeByMetadataName("System.Console");
            if (consoleSymbol != null)
            {
                ConsoleErrorAndOutSymbols = consoleSymbol.GetMembers(nameof(Console.Out)).Concat(consoleSymbol.GetMembers(nameof(Console.Error))).ToArray();
            }
            else
            {
                ConsoleErrorAndOutSymbols = Array.Empty<ISymbol>();
            }

            ProcessSymbol = _compilation.GetBestTypeByMetadataName("System.Diagnostics.Process");
            CancellationTokenSymbol = _compilation.GetBestTypeByMetadataName("System.Threading.CancellationToken");

            TaskSymbol = _compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
            TaskOfTSymbol = _compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");
            TaskAwaiterSymbol = _compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.TaskAwaiter");
            TaskAwaiterOfTSymbol = _compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.TaskAwaiter`1");

            ValueTaskSymbol = _compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask");
            ValueTaskOfTSymbol = _compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
            ValueTaskAwaiterSymbol = _compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ValueTaskAwaiter");
            ValueTaskAwaiterOfTSymbol = _compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ValueTaskAwaiter`1");

            ThreadSymbol = _compilation.GetBestTypeByMetadataName("System.Threading.Thread");

            DbContextSymbol = _compilation.GetBestTypeByMetadataName("Microsoft.EntityFrameworkCore.DbContext");
            DbSetSymbol = _compilation.GetBestTypeByMetadataName("Microsoft.EntityFrameworkCore.DbSet`1");

            ServiceProviderServiceExtensionsSymbol = _compilation.GetBestTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions");
            if (ServiceProviderServiceExtensionsSymbol != null)
            {
                ServiceProviderServiceExtensions_CreateScopeSymbol = ServiceProviderServiceExtensionsSymbol.GetMembers("CreateScope").FirstOrDefault();
                ServiceProviderServiceExtensions_CreateAsyncScopeSymbol = ServiceProviderServiceExtensionsSymbol.GetMembers("CreateAsyncScope").FirstOrDefault();
            }
        }

        private ISymbol? ProcessSymbol { get; }
        private ISymbol[] ConsoleErrorAndOutSymbols { get; }
        private INamedTypeSymbol? CancellationTokenSymbol { get; }
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

        public bool IsValid => TaskSymbol != null && TaskOfTSymbol != null && TaskAwaiterSymbol != null;

        internal void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            var targetMethod = operation.TargetMethod;

            if (IsTaskSymbol(targetMethod.ReturnType))
                return;

            if (operation.IsInNameofOperation())
                return;

            // Process.WaitForExit => Skip because the async method is not equivalent https://github.com/dotnet/runtime/issues/42556
            if (string.Equals(targetMethod.Name, nameof(System.Diagnostics.Process.WaitForExit), StringComparison.Ordinal) &&
                targetMethod.ContainingType.IsEqualTo(ProcessSymbol))
            {
                if (targetMethod.ContainingType.ContainingAssembly.Identity.Version < new Version(6, 0, 0, 0))
                    return;
            }

            // Task.Wait()
            // Task`1.Wait()
            if (string.Equals(targetMethod.Name, nameof(Task.Wait), StringComparison.Ordinal))
            {
                if (targetMethod.ContainingType.OriginalDefinition.IsEqualToAny(TaskSymbol, TaskOfTSymbol))
                {
                    ReportDiagnosticIfNeeded(context, operation, "Use await instead of 'Wait()'");
                    return;
                }
            }

            // Task.GetAwaiter().GetResult()
            if (string.Equals(targetMethod.Name, nameof(TaskAwaiter.GetResult), StringComparison.Ordinal))
            {
                if (targetMethod.ContainingType.OriginalDefinition.IsEqualToAny(TaskAwaiterSymbol, TaskAwaiterOfTSymbol, ValueTaskAwaiterSymbol, ValueTaskAwaiterOfTSymbol))
                {
                    ReportDiagnosticIfNeeded(context, operation, "Use await instead of 'GetResult()'");
                    return;
                }
            }

            // Thread.Sleep => Task.Delay
            if (string.Equals(targetMethod.Name, "Sleep", StringComparison.Ordinal))
            {
                if (targetMethod.ContainingType.IsEqualTo(ThreadSymbol))
                {
                    ReportDiagnosticIfNeeded(context, operation, "Use await and 'Task.Delay()' instead of 'Thread.Sleep()'");
                    return;
                }
            }

            // Console.Out|Error.Write
            if (string.Equals(targetMethod.Name, "Write", StringComparison.Ordinal) ||
               string.Equals(targetMethod.Name, "WriteLine", StringComparison.Ordinal) ||
               string.Equals(targetMethod.Name, "Flush", StringComparison.Ordinal))
            {
                var left = operation.GetChildOperations().FirstOrDefault();
                if (left is IMemberReferenceOperation memberReference)
                {
                    if (ConsoleErrorAndOutSymbols.Contains(memberReference.Member, SymbolEqualityComparer.Default))
                        return;
                }
            }

            if (ServiceProviderServiceExtensions_CreateAsyncScopeSymbol != null && ServiceProviderServiceExtensions_CreateScopeSymbol != null && targetMethod.IsEqualTo(ServiceProviderServiceExtensions_CreateScopeSymbol))
            {
                ReportDiagnosticIfNeeded(context, operation, $"Use 'CreateAsyncScope' instead of '{targetMethod.Name}'");
                return;
            }

            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.addasync?view=efcore-6.0&WT.mc_id=DT-MVP-5003978#overloads
            if (DbContextSymbol != null && targetMethod.Name is "Add" or "AddRange" && targetMethod.ContainingType.IsEqualTo(DbContextSymbol))
            {
                return;
            }

            if (DbSetSymbol != null && targetMethod.Name is "Add" or "AddRange" && targetMethod.ContainingType.OriginalDefinition.IsEqualTo(DbSetSymbol))
            {
                return;
            }

            // Search async equivalent: sample.Write() => sample.WriteAsync()
            if (!targetMethod.ReturnType.OriginalDefinition.IsEqualToAny(TaskSymbol, TaskOfTSymbol))
            {
                var position = operation.Syntax.GetLocation().SourceSpan.End;

                var potentionalMethods = new List<ISymbol>();
                potentionalMethods.AddRange(operation.SemanticModel!.LookupSymbols(position, targetMethod.ContainingType, name: targetMethod.Name, includeReducedExtensionMethods: true));
                if (!targetMethod.Name.EndsWith("Async", StringComparison.Ordinal))
                {
                    potentionalMethods.AddRange(operation.SemanticModel.LookupSymbols(position, targetMethod.ContainingType, name: targetMethod.Name + "Async", includeReducedExtensionMethods: true));
                }

                var potentialMethod = potentionalMethods.Find(IsPotentialMember);
                if (potentialMethod != null)
                {
                    ReportDiagnosticIfNeeded(context, operation, $"Use '{potentialMethod.Name}' instead of '{targetMethod.Name}'");
                }

                bool IsPotentialMember(ISymbol memberSymbol)
                {
                    if (memberSymbol.IsEqualTo(targetMethod))
                        return false;

                    if (memberSymbol is IMethodSymbol methodSymbol)
                    {
                        if (targetMethod.IsStatic && !methodSymbol.IsStatic)
                            return false;

                        if (!IsTaskSymbol(methodSymbol.ReturnType))
                            return false;

                        if (methodSymbol.IsObsolete(context.Compilation))
                            return false;

                        if (!targetMethod.HasSimilarParameters(methodSymbol) && !targetMethod.HasSimilarParameters(methodSymbol, CancellationTokenSymbol))
                            return false;

                        return true;
                    }

                    return false;
                }
            }
        }

        private bool IsTaskSymbol(ITypeSymbol symbol)
        {
            return symbol.OriginalDefinition.IsEqualToAny(TaskSymbol, TaskOfTSymbol, ValueTaskSymbol, ValueTaskOfTSymbol);
        }

        internal void AnalyzePropertyReference(OperationAnalysisContext context)
        {
            var operation = (IPropertyReferenceOperation)context.Operation;

            if (operation.IsInNameofOperation())
                return;

            // Task`1.Result
            if (string.Equals(operation.Property.Name, nameof(Task<int>.Result), StringComparison.Ordinal))
            {
                if (operation.Member.ContainingType.OriginalDefinition.IsEqualToAny(TaskOfTSymbol, ValueTaskOfTSymbol))
                {
                    ReportDiagnosticIfNeeded(context, operation, "Use await instead of 'Result'");
                }
            }
        }

        private void ReportDiagnosticIfNeeded(OperationAnalysisContext context, IOperation operation, string message)
        {
            if (!CanBeAsync(operation))
                return;

            if (IsAsyncContext(operation, context.CancellationToken))
            {
                context.ReportDiagnostic(s_rule, operation, message);
            }
            else if (CanChangeParentMethodSignature(operation, context.CancellationToken))
            {
                context.ReportDiagnostic(s_rule2, operation, message + " and make method async");
            }
        }

        private bool IsAsyncContext(IOperation operation, CancellationToken cancellationToken)
        {
            // lamdba, delegate, method, local function
            // Check if returns Task or async void
            if (operation.SemanticModel!.GetEnclosingSymbol(operation.Syntax.SpanStart, cancellationToken) is IMethodSymbol methodSymbol)
            {
                return methodSymbol.IsAsync || methodSymbol.ReturnType.OriginalDefinition.IsEqualToAny(TaskSymbol, TaskOfTSymbol, ValueTaskSymbol, ValueTaskOfTSymbol);
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
            var members = symbol.GetMembers("DisposeAsync");
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

            return HasDisposeAsyncMethod(type);
        }

        private bool ReportIfCanBeAwaitUsing(OperationAnalysisContext context, IOperation usingOperation, IVariableDeclarationGroupOperation operation)
        {
            foreach (var declaration in operation.Declarations)
            {
                if (declaration.Initializer?.Value != null)
                {
                    if (CanBeAwaitUsing(declaration.Initializer.Value))
                    {
                        ReportDiagnosticIfNeeded(context, usingOperation, "Prefer using 'await using'");
                        return true;
                    }
                }

                foreach (var declarator in declaration.Declarators)
                {
                    if (declarator.Initializer != null && CanBeAwaitUsing(declarator.Initializer.Value))
                    {
                        ReportDiagnosticIfNeeded(context, usingOperation, "Prefer using 'await using'");
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
                ReportDiagnosticIfNeeded(context, operation, "Prefer using 'await using'");
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
}
