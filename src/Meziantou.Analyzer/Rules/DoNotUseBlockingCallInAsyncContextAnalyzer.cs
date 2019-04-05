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

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

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

        private class Context
        {
            private readonly Compilation _compilation;

            public Context(Compilation compilation)
            {
                _compilation = compilation;
                TaskSymbol = _compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
                TaskOfTSymbol = _compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
                TaskAwaiterSymbol = _compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.TaskAwaiter");
            }

            private INamedTypeSymbol TaskSymbol { get; }
            private INamedTypeSymbol TaskOfTSymbol { get; }
            private INamedTypeSymbol TaskAwaiterSymbol { get; }

            public bool IsValid => TaskSymbol != null && TaskOfTSymbol != null && TaskAwaiterSymbol != null;

            internal void AnalyzeInvocation(OperationAnalysisContext context)
            {
                var operation = (IInvocationOperation)context.Operation;
                var targetMethod = operation.TargetMethod;

                // Task.Wait()
                // Task`1.Wait()
                if (string.Equals(targetMethod.Name, nameof(Task.Wait), StringComparison.Ordinal))
                {
                    if (targetMethod.ContainingType.OriginalDefinition.IsEqualsToAny(TaskSymbol, TaskOfTSymbol) && IsAsyncContext(operation))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation(), "Use await instead of 'Wait()'"));
                    }

                    return;
                }

                // Task.GetAwaiter().GetResult()
                if (string.Equals(targetMethod.Name, nameof(TaskAwaiter.GetResult), StringComparison.Ordinal))
                {
                    if (targetMethod.ContainingType.OriginalDefinition.IsEqualsTo(TaskAwaiterSymbol) && IsAsyncContext(operation))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation(), "Use await instead of 'Result'"));
                    }

                    return;
                }

                // Search async equivalent: sample.Write() => sample.WriteAsync()
                if (!targetMethod.ReturnType.OriginalDefinition.IsEqualsToAny(TaskSymbol, TaskOfTSymbol) && IsAsyncContext(operation))
                {
                    var potentialMethod = targetMethod.ContainingType.GetMembers().FirstOrDefault(IsPotentialMember);
                    if (potentialMethod != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation(), $"Use '{potentialMethod.Name}' instead of '{targetMethod.Name}'"));
                    }

                    bool IsPotentialMember(ISymbol symbol)
                    {
                        if (symbol.Equals(targetMethod))
                            return false;

                        if (symbol is IMethodSymbol methodSymbol)
                        {
                            return
                                (!targetMethod.IsStatic || methodSymbol.IsStatic) &&
                                string.Equals(methodSymbol.Name, targetMethod.Name, StringComparison.Ordinal) ||
                                string.Equals(methodSymbol.Name, targetMethod.Name + "Async", StringComparison.Ordinal) &&
                                methodSymbol.ReturnType.OriginalDefinition.IsEqualsToAny(TaskSymbol, TaskOfTSymbol);
                        }

                        return false;
                    }
                }
            }

            internal void AnalyzePropertyReference(OperationAnalysisContext context)
            {
                var operation = (IPropertyReferenceOperation)context.Operation;

                // Task`1.Result
                if (string.Equals(operation.Property.Name, nameof(Task<int>.Result), StringComparison.Ordinal))
                {
                    if (operation.Member.ContainingType.OriginalDefinition.IsEqualsTo(TaskOfTSymbol) && IsAsyncContext(operation))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation()));
                    }
                }
            }

            private bool IsAsyncContext(IOperation operation)
            {
                // lamdba, delegate, method, local function
                // Check if returns Task or async void
                var methodSymbol = operation.SemanticModel.GetEnclosingSymbol(operation.Syntax.SpanStart) as IMethodSymbol;
                if (methodSymbol != null)
                {
                    return methodSymbol.IsAsync || methodSymbol.ReturnType.OriginalDefinition.IsEqualsToAny(TaskSymbol, TaskOfTSymbol);
                }

                return false;
            }
        }
    }
}
