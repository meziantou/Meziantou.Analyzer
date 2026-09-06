using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

internal static class NullableAnalysisAttributeFixerHelper
{
    public static async Task<Document> AddOrUpdateAttributeAsync(Document document, ParameterSyntax parameter, string attributeMetadataName, bool expectedValue, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        var generator = editor.Generator;

        var attributeSymbol = semanticModel.Compilation.GetBestTypeByMetadataName(attributeMetadataName);
        if (attributeSymbol is null)
            return document;

        var existingAttribute = parameter.AttributeLists
            .SelectMany(static attributeList => attributeList.Attributes)
            .FirstOrDefault(attribute =>
            {
                var symbol = semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol?.ContainingType;
                return symbol.IsEqualTo(attributeSymbol);
            });

        if (existingAttribute is not null)
        {
            var boolExpression = expectedValue ? LiteralExpression(SyntaxKind.TrueLiteralExpression) : LiteralExpression(SyntaxKind.FalseLiteralExpression);
            var argumentList = AttributeArgumentList(SingletonSeparatedList(AttributeArgument(boolExpression)));
            editor.ReplaceNode(existingAttribute, existingAttribute.WithArgumentList(argumentList));
            return editor.GetChangedDocument();
        }

        var newAttribute = generator.Attribute(
            generator.TypeExpression(attributeSymbol, addImport: true),
            [
                generator.AttributeArgument(generator.LiteralExpression(expectedValue)),
            ]);

        var newNode = generator.AddAttributes(parameter, newAttribute);
        editor.ReplaceNode(parameter, newNode);
        return editor.GetChangedDocument();
    }

    public static bool TryGetParameterToFix(SyntaxNode? root, SemanticModel? semanticModel, TextSpan span, string attributeMetadataName, string methodName, CancellationToken cancellationToken, [NotNullWhen(true)] out ParameterSyntax? parameter, [NotNullWhen(true)] out IParameterSymbol? parameterSymbol)
    {
        parameter = null;
        parameterSymbol = null;

        if (root?.FindNode(span, getInnermostNodeForTie: true)?.FirstAncestorOrSelf<ParameterSyntax>() is not { } parameterSyntax)
            return false;

        if (semanticModel is null)
            return false;

        if (semanticModel.GetDeclaredSymbol(parameterSyntax, cancellationToken) is not IParameterSymbol symbol)
            return false;

        if (symbol.ContainingSymbol is not IMethodSymbol methodSymbol || !string.Equals(methodSymbol.Name, methodName, StringComparison.Ordinal))
            return false;

        if (semanticModel.Compilation.GetBestTypeByMetadataName(attributeMetadataName) is null)
            return false;

        parameter = parameterSyntax;
        parameterSymbol = symbol;
        return true;
    }
}
