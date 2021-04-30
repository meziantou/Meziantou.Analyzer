using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EventsShouldHaveProperArgumentsAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_senderInstanceRule = new(
            RuleIdentifiers.SenderShouldBeThisForInstanceEvents,
            title: "Sender should be 'this' for instance events",
            messageFormat: "Sender parameter should be 'this' for instance events",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SenderShouldBeThisForInstanceEvents));

        private static readonly DiagnosticDescriptor s_senderStaticRule = new(
            RuleIdentifiers.SenderShouldBeNullForStaticEvents,
            title: "Sender should be 'null' for static events",
            messageFormat: "Sender parameter should be 'null' for static events",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SenderShouldBeNullForStaticEvents));

        private static readonly DiagnosticDescriptor s_eventArgsRule = new(
            RuleIdentifiers.EventArgsSenderShouldNotBeNullForEvents,
            title: "EventArgs should not be null",
            messageFormat: "EventArgs should not be null, use 'EventArgs.Empty' instead",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.EventArgsSenderShouldNotBeNullForEvents));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_senderInstanceRule, s_senderStaticRule, s_eventArgsRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(AnalyzeRaiseEvent, OperationKind.Invocation);
        }

        private static void AnalyzeRaiseEvent(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            var targetMethod = operation.TargetMethod;

            if (targetMethod.Name != nameof(EventHandler.Invoke))
                return;

            if (targetMethod.Parameters.Length != 2)
                return;

            if (!targetMethod.Parameters[0].Type.IsObject())
                return;

            var eventArgsSymbol = context.Compilation.GetTypeByMetadataName("System.EventArgs");
            if (!targetMethod.Parameters[1].Type.IsOrInheritFrom(eventArgsSymbol))
                return;

            var multicastDelegateSymbol = context.Compilation.GetTypeByMetadataName("System.MulticastDelegate");
            if (!targetMethod.ContainingType.IsOrInheritFrom(multicastDelegateSymbol))
                return;

            var instance = operation.Instance;
            if (instance == null)
                return;

            var ev = FindEvent(instance);
            if (ev == null)
                return;

            // Argument validation
            var senderArgument = operation.Arguments[0];
            if (ev.IsStatic)
            {
                if (!IsNull(senderArgument))
                {
                    context.ReportDiagnostic(s_senderStaticRule, senderArgument);
                }
            }
            else
            {
                if (!IsThis(senderArgument))
                {
                    context.ReportDiagnostic(s_senderInstanceRule, senderArgument);
                }
            }

            var eventArgsArgument = operation.Arguments[1];
            if (IsNull(eventArgsArgument))
            {
                context.ReportDiagnostic(s_eventArgsRule, eventArgsArgument);
            }
        }

        private static bool IsNull(IArgumentOperation operation)
        {
            return operation.Value.ConstantValue.HasValue && operation.Value.ConstantValue.Value == null;
        }

        private static bool IsThis(IArgumentOperation operation)
        {
            var value = operation.Value;
            while (value is IConversionOperation conversion)
            {
                value = conversion.Operand;
            }

            return value is IInstanceReferenceOperation;
        }

        private static IEventSymbol? FindEvent(IOperation operation)
        {
            var eventFinder = new EventReferenceVisitor();
            eventFinder.Visit(operation);
            return eventFinder.EventSymbol;
        }

        private sealed class EventReferenceVisitor : OperationVisitor
        {
            public IEventSymbol? EventSymbol { get; set; }

            public override void VisitEventReference(IEventReferenceOperation operation)
            {
                EventSymbol = operation.Event;
                base.VisitEventReference(operation);
            }

            public override void VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation)
            {
                var semanticModel = operation.SemanticModel!;
                var syntax = operation.Syntax;
                var symbol = semanticModel.GetSymbolInfo(syntax).Symbol;
                if (symbol != null)
                {
                    if (symbol is IEventSymbol eventSymbol)
                    {
                        EventSymbol = eventSymbol;
                        return;
                    }
                    else if (symbol is ILocalSymbol localSymbol)
                    {
                        FindFromLocalSymbol(semanticModel, localSymbol);
                    }
                }

                base.VisitConditionalAccessInstance(operation);
            }

            private void FindFromLocalSymbol(SemanticModel semanticModel, ILocalSymbol localSymbol)
            {
                foreach (var symbolLocation in localSymbol.DeclaringSyntaxReferences)
                {
                    var variableDeclarator = symbolLocation.GetSyntax() as Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax;
                    if (variableDeclarator?.Initializer?.Value != null)
                    {
                        var initializerSymbol = semanticModel.GetSymbolInfo(variableDeclarator.Initializer.Value).Symbol;
                        if (initializerSymbol is IEventSymbol initializerEventSymbol)
                        {
                            EventSymbol = initializerEventSymbol;
                            return;
                        }
                        else if (initializerSymbol is ILocalSymbol initializerLocalSymbol)
                        {
                            FindFromLocalSymbol(semanticModel, initializerLocalSymbol);
                        }
                    }
                }
            }

            public override void VisitLocalReference(ILocalReferenceOperation operation)
            {
                if (operation.SemanticModel != null)
                {
                    FindFromLocalSymbol(operation.SemanticModel, operation.Local);
                }

                base.VisitLocalReference(operation);
            }
        }
    }
}
