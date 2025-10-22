#if CSHARP12_OR_GREATER
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrimaryConstructorParameterShouldBeReadOnlyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.PrimaryConstructorParameterShouldBeReadOnly,
        title: "Primary constructor parameters should be readonly",
        messageFormat: "Primary constructor parameters should be readonly",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.PrimaryConstructorParameterShouldBeReadOnly));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            if (context.Compilation.GetCSharpLanguageVersion() < Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
                return;

            context.RegisterOperationAction(AnalyzerAssignment, OperationKind.SimpleAssignment);
            context.RegisterOperationAction(AnalyzerAssignment, OperationKind.CompoundAssignment);
            context.RegisterOperationAction(AnalyzerAssignment, OperationKind.CoalesceAssignment);
            context.RegisterOperationAction(AnalyzerAssignment, OperationKind.DeconstructionAssignment);
            context.RegisterOperationAction(AnalyzerIncrementOrDecrement, OperationKind.Increment);
            context.RegisterOperationAction(AnalyzerIncrementOrDecrement, OperationKind.Decrement);
            context.RegisterOperationAction(AnalyzerInitializer, OperationKind.VariableDeclarator);
            context.RegisterOperationAction(AnalyzerArgument, OperationKind.Argument);
        });
    }

    private void AnalyzerArgument(OperationAnalysisContext context)
    {
        var operation = (IArgumentOperation)context.Operation;
        if (operation.Parameter is { RefKind: RefKind.Ref or RefKind.Out } && IsPrimaryConstructorParameter(operation.Value, context.CancellationToken))
        {
            context.ReportDiagnostic(Rule, operation.Value);
        }
    }

    private void AnalyzerInitializer(OperationAnalysisContext context)
    {
        var operation = (IVariableDeclaratorOperation)context.Operation;
        if (operation.Initializer is null)
            return;

        if (operation.Symbol.RefKind is RefKind.Ref or RefKind.Out)
        {
            if (IsPrimaryConstructorParameter(operation.Initializer.Value, context.CancellationToken))
            {
                context.ReportDiagnostic(Rule, operation.Initializer.Value);
            }
        }
    }

    private void AnalyzerIncrementOrDecrement(OperationAnalysisContext context)
    {
        var operation = (IIncrementOrDecrementOperation)context.Operation;
        var target = operation.Target;

        if (IsPrimaryConstructorParameter(target, context.CancellationToken))
        {
            context.ReportDiagnostic(Rule, target);
        }
    }

    private void AnalyzerAssignment(OperationAnalysisContext context)
    {
        var operation = (IAssignmentOperation)context.Operation;
        var target = operation.Target;
        if (target is ITupleOperation)
        {
            foreach (var innerTarget in GetAllPrimaryCtorAssignmentTargets(target, context.CancellationToken))
            {
                context.ReportDiagnostic(Rule, innerTarget);
            }
        }
        else if (IsPrimaryConstructorParameter(target, context.CancellationToken))
        {
            context.ReportDiagnostic(Rule, target);
        }

        static IEnumerable<IOperation> GetAllPrimaryCtorAssignmentTargets(IOperation operation, CancellationToken cancellationToken)
        {
            List<IOperation>? result = null;
            GetAllAssignmentTargets(ref result, operation, cancellationToken);
            return result ?? Enumerable.Empty<IOperation>();

            static void GetAllAssignmentTargets(ref List<IOperation>? operations, IOperation operation, CancellationToken cancellationToken)
            {
                if (operation is ITupleOperation tuple)
                {
                    foreach (var element in tuple.Elements)
                    {
                        GetAllAssignmentTargets(ref operations, element, cancellationToken);
                    }
                }
                else
                {
                    if (IsPrimaryConstructorParameter(operation, cancellationToken))
                    {
                        operations ??= [];
                        operations.Add(operation);
                    }
                }
            }
        }
    }

    private static bool IsPrimaryConstructorParameter(IOperation operation, CancellationToken cancellationToken)
    {
        if (operation is IParameterReferenceOperation parameterReferenceOperation)
        {
            if (parameterReferenceOperation.Parameter.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor } ctor)
            {
                foreach (var syntaxRef in ctor.DeclaringSyntaxReferences)
                {
                    var syntax = syntaxRef.GetSyntax(cancellationToken);
                    if (syntax is ClassDeclarationSyntax or StructDeclarationSyntax)
                        return true;
                }
            }
        }

        return false;
    }
}
#endif
