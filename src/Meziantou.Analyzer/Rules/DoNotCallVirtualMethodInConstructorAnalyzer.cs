using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotCallVirtualMethodInConstructorAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.DoNotCallVirtualMethodInConstructor,
        title: "Do not call overridable members in constructor",
        messageFormat: "Do not call overridable members in constructor",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotCallVirtualMethodInConstructor));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationBlockStartAction(ctx =>
        {
            if (ctx.OwningSymbol is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.MethodKind != MethodKind.Constructor)
                    return;

                if (methodSymbol.ContainingType.IsSealed)
                    return;

                ctx.RegisterOperationAction(AnalyzeEventOperation, OperationKind.EventAssignment);
                ctx.RegisterOperationAction(AnalyzeInvocationOperation, OperationKind.Invocation);
                ctx.RegisterOperationAction(AnalyzePropertyReferenceOperation, OperationKind.PropertyReference);
            }
        });
    }

    private static void AnalyzeEventOperation(OperationAnalysisContext context)
    {
        var operation = (IEventAssignmentOperation)context.Operation;
        if (operation.EventReference is IEventReferenceOperation eventReference)
        {
            if (IsOverridable(eventReference.Member) && IsCurrentInstanceMethod(eventReference.Instance) && !IsInDelegate(operation))
            {
                context.ReportDiagnostic(s_rule, operation);
            }
        }
    }

    private static void AnalyzePropertyReferenceOperation(OperationAnalysisContext context)
    {
        var operation = (IPropertyReferenceOperation)context.Operation;
        var member = operation.Member;
        if (IsOverridable(member) && !operation.IsInNameofOperation() && !IsInDelegate(operation))
        {
            var children = operation.GetChildOperations().Take(2).ToList();
            if (children.Count == 1 && IsCurrentInstanceMethod(children[0]))
            {
                context.ReportDiagnostic(s_rule, operation);
            }
        }
    }

    private static void AnalyzeInvocationOperation(OperationAnalysisContext context)
    {
        var operation = (IInvocationOperation)context.Operation;
        if (IsOverridable(operation.TargetMethod) && IsCurrentInstanceMethod(operation.Instance) && !IsInDelegate(operation))
        {
            context.ReportDiagnostic(s_rule, operation);
        }
    }

    private static bool IsOverridable(ISymbol symbol)
    {
        return !symbol.IsSealed && (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride);
    }

    private static bool IsCurrentInstanceMethod(IOperation? operation)
    {
        if (operation == null)
            return false;

        return operation is IInstanceReferenceOperation i && i.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance;
    }

    private static bool IsInDelegate(IOperation? operation)
    {
        while (operation != null)
        {
            if (operation.Kind is OperationKind.DelegateCreation)
                return true;

            operation = operation.Parent;
        }

        return false;
    }
}
