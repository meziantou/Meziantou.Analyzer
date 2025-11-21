using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventsShouldHaveProperArgumentsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor SenderInstanceRule = new(
        RuleIdentifiers.SenderShouldBeThisForInstanceEvents,
        title: "Sender should be 'this' for instance events",
        messageFormat: "Sender parameter should be 'this' for instance events",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SenderShouldBeThisForInstanceEvents));

    private static readonly DiagnosticDescriptor SenderStaticRule = new(
        RuleIdentifiers.SenderShouldBeNullForStaticEvents,
        title: "Sender should be 'null' for static events",
        messageFormat: "Sender parameter should be 'null' for static events",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SenderShouldBeNullForStaticEvents));

    private static readonly DiagnosticDescriptor EventArgsRule = new(
        RuleIdentifiers.EventArgsSenderShouldNotBeNullForEvents,
        title: "EventArgs should not be null when raising an event",
        messageFormat: "EventArgs should not be null, use 'EventArgs.Empty' instead",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.EventArgsSenderShouldNotBeNullForEvents));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SenderInstanceRule, SenderStaticRule, EventArgsRule);

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

        var eventArgsSymbol = context.Compilation.GetBestTypeByMetadataName("System.EventArgs");
        if (!targetMethod.Parameters[1].Type.IsOrInheritFrom(eventArgsSymbol))
            return;

        var multicastDelegateSymbol = context.Compilation.GetBestTypeByMetadataName("System.MulticastDelegate");
        if (!targetMethod.ContainingType.IsOrInheritFrom(multicastDelegateSymbol))
            return;

        var instance = operation.Instance;
        if (instance is null)
            return;

        var ev = FindEvent(instance);
        if (ev is null)
            return;

        // Argument validation
        var senderArgument = operation.Arguments[0];
        if (ev.IsStatic)
        {
            if (!IsNull(senderArgument))
            {
                context.ReportDiagnostic(SenderStaticRule, senderArgument);
            }
        }
        else
        {
            if (!IsThis(senderArgument))
            {
                context.ReportDiagnostic(SenderInstanceRule, senderArgument.Value);
            }
        }

        var eventArgsArgument = operation.Arguments[1];
        if (IsNull(eventArgsArgument))
        {
            context.ReportDiagnostic(EventArgsRule, eventArgsArgument.Value);
        }
    }

    private static bool IsNull(IArgumentOperation operation)
    {
        return operation.Value.ConstantValue.HasValue && operation.Value.ConstantValue.Value is null;
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
            if (symbol is not null)
            {
                if (symbol is IEventSymbol eventSymbol)
                {
                    EventSymbol = eventSymbol;
                    return;
                }
                else if (symbol is ILocalSymbol localSymbol)
                {
                    FindFromLocalSymbol(semanticModel, localSymbol, CancellationToken.None);
                }
            }

            base.VisitConditionalAccessInstance(operation);
        }

        private void FindFromLocalSymbol(SemanticModel semanticModel, ILocalSymbol localSymbol, CancellationToken cancellationToken)
        {
            foreach (var symbolLocation in localSymbol.DeclaringSyntaxReferences)
            {
                var variableDeclarator = symbolLocation.GetSyntax(cancellationToken) as Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax;
                if ((variableDeclarator?.Initializer?.Value) is not null)
                {
                    var initializerSymbol = semanticModel.GetSymbolInfo(variableDeclarator.Initializer.Value, cancellationToken).Symbol;
                    if (initializerSymbol is IEventSymbol initializerEventSymbol)
                    {
                        EventSymbol = initializerEventSymbol;
                        return;
                    }
                    else if (initializerSymbol is ILocalSymbol initializerLocalSymbol)
                    {
                        FindFromLocalSymbol(semanticModel, initializerLocalSymbol, cancellationToken);
                    }
                }
            }
        }

        public override void VisitLocalReference(ILocalReferenceOperation operation)
        {
            if (operation.SemanticModel is not null)
            {
                FindFromLocalSymbol(operation.SemanticModel, operation.Local, CancellationToken.None);
            }

            base.VisitLocalReference(operation);
        }
    }
}
