using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SimplifyCallerArgumentExpressionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.SimplifyCallerArgumentExpression,
        title: "Remove redundant argument value",
        messageFormat: "Remove redundant argument value",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SimplifyCallerArgumentExpression));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var operation = (IInvocationOperation)context.Operation;
        if (!operation.GetCSharpLanguageVersion().IsCSharp10OrAbove())
            return;

        foreach (var argument in operation.Arguments)
        {
            AnalyzeArgument(context, operation, argument);
        }
    }

    private static void AnalyzeArgument(OperationAnalysisContext context, IInvocationOperation invocation, IArgumentOperation argument)
    {
        if (!argument.Value.ConstantValue.HasValue)
            return;

        if (argument.IsImplicit || argument.ArgumentKind != ArgumentKind.Explicit)
            return;

        if (argument.Parameter is null)
            return;

        if (!argument.Parameter.Type.IsString())
            return;

        if (!argument.Parameter.HasExplicitDefaultValue)
            return;

        if (argument.Parameter.ExplicitDefaultValue is not null)
            return;

        var attributeSymbol = context.Compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.CallerArgumentExpressionAttribute");
        if (attributeSymbol is null)
            return;

        foreach (var attribute in argument.Parameter.GetAttributes())
        {
            if (attribute.ConstructorArguments.Length == 0)
                continue;

            if (!attribute.AttributeClass.IsEqualTo(attributeSymbol))
                continue;

            var parameterName = attribute.ConstructorArguments[0].Value as string;
            if (string.IsNullOrEmpty(parameterName))
                continue;

            var targetArgument = invocation.Arguments.FirstOrDefault(arg => arg.Parameter?.Name == parameterName);
            if (targetArgument is null)
                continue;

            var defaultValue = targetArgument.Value.Syntax.ToString();
            if (defaultValue == (string?)argument.Value.ConstantValue.Value)
            {
                context.ReportDiagnostic(Rule, argument);
            }
        }
    }
}
