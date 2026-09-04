using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStaticLambdaAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseStaticLambda,
        title: "Use a static lambda",
        messageFormat: "Use a static lambda as it doesn't capture any state",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStaticLambda));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterOperationAction(AnalyzeAnonymousFunction, OperationKind.AnonymousFunction);
    }

    private static void AnalyzeAnonymousFunction(OperationAnalysisContext context)
    {
        var operation = (IAnonymousFunctionOperation)context.Operation;

        // Query expressions generate anonymous functions whose syntax is not a lambda
        if (operation.Syntax is not AnonymousFunctionExpressionSyntax syntax)
            return;

        if (syntax.Modifiers.Any(SyntaxKind.StaticKeyword))
            return;

        // static lambdas are available since C# 9
        if (!syntax.GetCSharpLanguageVersion().IsCSharp9OrGreater())
            return;

        var semanticModel = operation.SemanticModel;
        if (semanticModel is null)
            return;

        var dataFlow = semanticModel.AnalyzeDataFlow(syntax);
        if (dataFlow is null || !dataFlow.Succeeded)
            return;

        // Variables declared inside the lambda can be captured by a nested lambda, which doesn't prevent the lambda from being static
        foreach (var symbol in dataFlow.CapturedInside)
        {
            if (!IsDeclaredInside(symbol, syntax))
                return;
        }

        // A static lambda cannot reference a local function declared outside of it (CS8820)
        if (ReferencesLocalFunctionDeclaredOutside(operation, syntax))
            return;

        context.ReportDiagnostic(Rule, syntax);
    }

    private static bool ReferencesLocalFunctionDeclaredOutside(IOperation operation, SyntaxNode lambda)
    {
        var operations = new Queue<IOperation>();
        operations.Enqueue(operation);

        while (operations.Count > 0)
        {
            var op = operations.Dequeue();
            foreach (var child in op.GetChildOperations())
            {
                operations.Enqueue(child);
            }

            var method = op switch
            {
                IInvocationOperation invocation => invocation.TargetMethod,
                IMethodReferenceOperation methodReference => methodReference.Method,
                _ => null,
            };

            if (method is { MethodKind: MethodKind.LocalFunction, IsStatic: false } && !IsDeclaredInside(method, lambda))
                return true;
        }

        return false;
    }

    private static bool IsDeclaredInside(ISymbol symbol, SyntaxNode lambda)
    {
        // "this" has no declaring syntax reference
        if (symbol.DeclaringSyntaxReferences.Length is 0)
            return false;

        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            if (reference.SyntaxTree != lambda.SyntaxTree || !lambda.Span.Contains(reference.Span))
                return false;
        }

        return true;
    }
}
