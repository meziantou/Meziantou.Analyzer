using System.Globalization;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseValueFactoryWhenUsingConcurrentDictionaryFixer : CodeFixProvider
{
    private const string Title = "Use a value factory";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseValueFactoryWhenUsingConcurrentDictionary);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not { } nodeToFix)
            return;

        if (nodeToFix.AncestorsAndSelf().OfType<ArgumentSyntax>().FirstOrDefault() is not { } argument || argument.Expression.Span != context.Span)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        // Moving the expression to a lambda can make the code invalid, for instance when it uses 'await', a ref struct,
        // or declares a variable used after the invocation. It can also select another overload when TValue is a delegate.
        var annotation = new SyntaxAnnotation();
        var newArgument = CreateFactoryArgument(semanticModel, argument).WithAdditionalAnnotations(annotation);
        var newRoot = root.ReplaceNode(argument, newArgument);
        var newDocument = context.Document.WithSyntaxRoot(newRoot);
        if (!await IsValidFix(semanticModel, newDocument, annotation, context.CancellationToken).ConfigureAwait(false))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(Title, _ => Task.FromResult(newDocument), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static ArgumentSyntax CreateFactoryArgument(SemanticModel semanticModel, ArgumentSyntax argument)
    {
        var expression = argument.Expression;
        var parameterName = GetParameterName(semanticModel, expression.SpanStart);
        ExpressionSyntax lambda = SimpleLambdaExpression(Parameter(Identifier(parameterName)), expression.WithoutTrivia())
            .WithTriviaFrom(expression);

        var newArgument = argument.WithExpression(lambda);
        if (argument.NameColon is { } nameColon)
        {
            var factoryName = nameColon.Name.Identifier.ValueText + "Factory";
            newArgument = newArgument.WithNameColon(nameColon.WithName(IdentifierName(factoryName).WithTriviaFrom(nameColon.Name)));
        }

        return newArgument;
    }

    private static string GetParameterName(SemanticModel semanticModel, int position)
    {
        var name = "_";
        var index = 1;
        while (!semanticModel.LookupSymbols(position, name: name).IsEmpty)
        {
            name = "_" + index.ToString(CultureInfo.InvariantCulture);
            index++;
        }

        return name;
    }

    private static async Task<bool> IsValidFix(SemanticModel semanticModel, Document newDocument, SyntaxAnnotation annotation, CancellationToken cancellationToken)
    {
        var newSemanticModel = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (newSemanticModel is null || newRoot is null)
            return false;

        if (newRoot.GetAnnotatedNodes(annotation).FirstOrDefault() is not ArgumentSyntax newArgument)
            return false;

        // The lambda must be the factory, not the value of a ConcurrentDictionary whose TValue is a delegate type
        if (newSemanticModel.GetOperation(newArgument, cancellationToken) is not IArgumentOperation { Parameter.OriginalDefinition.Type: INamedTypeSymbol { TypeKind: TypeKind.Delegate } })
            return false;

        return CountErrors(newSemanticModel, cancellationToken) <= CountErrors(semanticModel, cancellationToken);

        static int CountErrors(SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return semanticModel.GetDiagnostics(cancellationToken: cancellationToken).Count(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error);
        }
    }
}
