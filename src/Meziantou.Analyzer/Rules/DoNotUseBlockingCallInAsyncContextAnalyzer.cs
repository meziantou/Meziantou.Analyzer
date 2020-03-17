using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotUseBlockingCallInAsyncContextAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotUseBlockingCallInAsyncContext,
            title: "Do not use blocking call",
            messageFormat: "{0}",
            RuleCategories.Design,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseBlockingCallInAsyncContext));

        private static readonly DiagnosticDescriptor s_rule2 = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotUseBlockingCall,
            title: "Do not use blocking call (make method async)",
            messageFormat: "{0}",
            RuleCategories.Design,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
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
                }
            });
        }

        private sealed class Context
        {
            private readonly Compilation _compilation;

            public Context(Compilation compilation)
            {
                _compilation = compilation;
                var consoleSymbol = _compilation.GetTypeByMetadataName("System.Console");
                if (consoleSymbol != null)
                {
                    ConsoleErrorAndOutSymbols = consoleSymbol.GetMembers(nameof(Console.Out)).Concat(consoleSymbol.GetMembers(nameof(Console.Error))).ToArray();
                }

                CancellationTokenSymbol = _compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
                TaskSymbol = _compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
                TaskOfTSymbol = _compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
                TaskAwaiterSymbol = _compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.TaskAwaiter");
                ThreadSymbols = _compilation.GetTypesByMetadataName("System.Threading.Thread").ToArray();
            }

            private ISymbol[] ConsoleErrorAndOutSymbols { get; }
            private INamedTypeSymbol CancellationTokenSymbol { get; }
            private INamedTypeSymbol TaskSymbol { get; }
            private INamedTypeSymbol TaskOfTSymbol { get; }
            private INamedTypeSymbol TaskAwaiterSymbol { get; }
            private INamedTypeSymbol[] ThreadSymbols { get; }

            public bool IsValid => TaskSymbol != null && TaskOfTSymbol != null && TaskAwaiterSymbol != null;

            internal void AnalyzeInvocation(OperationAnalysisContext context)
            {
                var operation = (IInvocationOperation)context.Operation;
                var targetMethod = operation.TargetMethod;

                if (operation.IsInNameofOperation())
                    return;

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
                    if (targetMethod.ContainingType.OriginalDefinition.IsEqualTo(TaskAwaiterSymbol))
                    {
                        ReportDiagnosticIfNeeded(context, operation, "Use await instead of 'GetResult()'");
                        return;
                    }
                }

                // Thread.Sleep => Task.Delay
                if (string.Equals(targetMethod.Name, "Sleep", StringComparison.Ordinal))
                {
                    if (targetMethod.ContainingType.IsEqualToAny(ThreadSymbols))
                    {
                        ReportDiagnosticIfNeeded(context, operation, "Use await and 'Task.Delay()' instead of 'Thread.Sleep()'");
                        return;
                    }
                }

                // Console.Out|Error.Write
                if(string.Equals(targetMethod.Name, "Write", StringComparison.Ordinal) ||
                   string.Equals(targetMethod.Name, "WriteLine", StringComparison.Ordinal) ||
                   string.Equals(targetMethod.Name, "Flush", StringComparison.Ordinal))
                {
                    var left = operation.Children.FirstOrDefault();
                    if(left is IMemberReferenceOperation memberReference)
                    {
                        if (ConsoleErrorAndOutSymbols.Contains(memberReference.Member))
                            return;
                    }
                }

                // Search async equivalent: sample.Write() => sample.WriteAsync()
                if (!targetMethod.ReturnType.OriginalDefinition.IsEqualToAny(TaskSymbol, TaskOfTSymbol))
                {
                    var potentialMethod = targetMethod.ContainingType.GetMembers().FirstOrDefault(IsPotentialMember);
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

                            if (!string.Equals(methodSymbol.Name, targetMethod.Name, StringComparison.Ordinal) && !string.Equals(methodSymbol.Name, targetMethod.Name + "Async", StringComparison.Ordinal))
                                return false;

                            if (!methodSymbol.ReturnType.OriginalDefinition.IsEqualToAny(TaskSymbol, TaskOfTSymbol))
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

            internal void AnalyzePropertyReference(OperationAnalysisContext context)
            {
                var operation = (IPropertyReferenceOperation)context.Operation;

                if (operation.IsInNameofOperation())
                    return;

                // Task`1.Result
                if (string.Equals(operation.Property.Name, nameof(Task<int>.Result), StringComparison.Ordinal))
                {
                    if (operation.Member.ContainingType.OriginalDefinition.IsEqualTo(TaskOfTSymbol))
                    {
                        ReportDiagnosticIfNeeded(context, operation, "Use await instead of 'Result'");
                    }
                }
            }

            private void ReportDiagnosticIfNeeded(OperationAnalysisContext context, IOperation operation, string message)
            {
                if (!CanBeAsync(operation))
                    return;

                if (IsAsyncContext(operation))
                {
                    context.ReportDiagnostic(s_rule, operation, message);
                }
                else if (CanChangeParentMethodSignature(operation))
                {
                    context.ReportDiagnostic(s_rule2, operation, message + " and make method async");
                }
            }

            private bool IsAsyncContext(IOperation operation)
            {
                // lamdba, delegate, method, local function
                // Check if returns Task or async void
                var methodSymbol = operation.SemanticModel.GetEnclosingSymbol(operation.Syntax.SpanStart) as IMethodSymbol;
                if (methodSymbol != null)
                {
                    return methodSymbol.IsAsync || methodSymbol.ReturnType.OriginalDefinition.IsEqualToAny(TaskSymbol, TaskOfTSymbol);
                }

                return false;
            }

            private static bool CanChangeParentMethodSignature(IOperation operation)
            {
                var symbol = operation.SemanticModel.GetEnclosingSymbol(operation.Syntax.SpanStart);
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
        }
    }
}
