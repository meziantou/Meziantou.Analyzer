using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseShellExecuteAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor UseShellExecuteMustBeExplicitlySet = new(
        RuleIdentifiers.UseShellExecuteMustBeSet,
        title: "UseShellExecute must be explicitly set",
        messageFormat: "UseShellExecute must be explicitly set when initializing a ProcessStartInfo",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseShellExecuteMustBeSet));

    private static readonly DiagnosticDescriptor UseProcessStartOverload = new(
        RuleIdentifiers.UseShellExecuteMustBeSet,
        title: "UseShellExecute must be explicitly set",
        messageFormat: "Use an overload of Process.Start that has a ProcessStartInfo parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseShellExecuteMustBeSet));

    private static readonly DiagnosticDescriptor SetToFalseWhenRedirectingOutput = new(
        RuleIdentifiers.UseShellExecuteMustBeSet,
        title: "UseShellExecute must be explicitly set",
        messageFormat: "Set UseShellExecute to false when redirecting standard input or output",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseShellExecuteMustBeSet));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(UseShellExecuteMustBeExplicitlySet);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (!analyzerContext.IsValid)
                return;

            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeObjectCreation, OperationKind.ObjectCreation);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly INamedTypeSymbol? _processStartInfoSymbol = compilation.GetBestTypeByMetadataName("System.Diagnostics.ProcessStartInfo");

        private readonly INamedTypeSymbol? _processSymbol = compilation.GetBestTypeByMetadataName("System.Diagnostics.Process");

        public bool IsValid => _processStartInfoSymbol is not null;

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (IsProcessStartInvocation(operation))
            {
                if (!operation.Arguments.Any(IsProcessStartInfo))
                {
                    // Calling Process.Start without ProcessStartInfo
                    context.ReportDiagnostic(UseProcessStartOverload, operation);
                }
            }
        }

        public void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            var operation = (IObjectCreationOperation)context.Operation;
            if (IsProcessStartInfoCreation(operation))
            {
                if (operation.Syntax is ObjectCreationExpressionSyntax { Initializer: {} initializer } )
                {
                    var useShellExecuteInitializer = initializer.Expressions.OfType<AssignmentExpressionSyntax>()
                        .FirstOrDefault(x => x.Left is IdentifierNameSyntax
                        {
                            Identifier.Text: "UseShellExecute"
                        });

                    if (useShellExecuteInitializer is null)
                    {
                        if (IsRedirectingInputOrOutput(operation.SemanticModel!, initializer))
                        {
                            // Redirecting standard input or output while UseShellExecute is not explicitly set
                            context.ReportDiagnostic(SetToFalseWhenRedirectingOutput, operation);
                        }
                        else
                        {
                            // Constructing ProcessStartInfo without setting UseShellExecute in the initializer
                            context.ReportDiagnostic(UseShellExecuteMustBeExplicitlySet, operation);
                        }
                    }
                    else if (IsInitializedToTrue(operation.SemanticModel!, useShellExecuteInitializer))
                    {
                        if (IsRedirectingInputOrOutput(operation.SemanticModel!, initializer))
                        {
                            // Redirecting standard input or output while UseShellExecute is set to true
                            context.ReportDiagnostic(SetToFalseWhenRedirectingOutput, operation);
                        }
                    }
                }
                else
                {
                    // Constructing ProcessStartInfo with not initializer at all
                    context.ReportDiagnostic(UseShellExecuteMustBeExplicitlySet, operation);
                }
            }
        }

        private static bool IsInitializedToTrue(SemanticModel semanticModel, AssignmentExpressionSyntax assignmentExpressionSyntax)
            => semanticModel.GetConstantValue(assignmentExpressionSyntax.Right) is { HasValue: true, Value: true };

        private static bool IsRedirectingInputOrOutput(SemanticModel semanticModel,
            InitializerExpressionSyntax initializer) =>
            initializer.Expressions.OfType<AssignmentExpressionSyntax>()
                .Any(x => x.Left is IdentifierNameSyntax { Identifier.Text: "RedirectStandardError" or "RedirectStandardInput" or "RedirectStandardOutput" }
                                             && IsInitializedToTrue(semanticModel, x));

        private bool IsProcessStartInfo(IArgumentOperation operation)
            => operation.Value.Type.IsEqualTo(_processStartInfoSymbol);

        private bool IsProcessStartInfoCreation(IObjectCreationOperation operation)
            => operation.Type.IsEqualTo(_processStartInfoSymbol);

        private bool IsProcessStartInvocation(IInvocationOperation operation)
            => operation.TargetMethod.Name == "Start"
            && operation.TargetMethod.ContainingType.IsEqualTo(_processSymbol)
            && operation.TargetMethod.IsStatic;

    }
}
