using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class OptimizeGuidCreationFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.OptimizeGuidCreation);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticMode = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticMode is null)
            return;

        var operation = semanticMode.GetOperation(nodeToFix, context.CancellationToken);
        string? guidString = null;
        if (operation is IObjectCreationOperation creation)
        {
            guidString = creation.Arguments.FirstOrDefault()?.Value.ConstantValue.Value as string;
        }
        else if (operation is IInvocationOperation invocation)
        {
            guidString = invocation.Arguments.FirstOrDefault()?.Value.ConstantValue.Value as string;
        }

        if (guidString is null || !Guid.TryParse(guidString, out var guid))
            return;


        var title = "Optimize guid creation";
        var codeAction = CodeAction.Create(
            title,
            ct => Update(context.Document, operation!, guid, guidString, ct),
            equivalenceKey: title);
        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> Update(Document document, IOperation nodeToFix, Guid guid, string rawGuid, CancellationToken cancellationToken)
    {
        var parts = guid.ToByteArray();
        var data1 = BitConverter.ToInt32(parts, 0);
        var data2 = BitConverter.ToInt16(parts, 4);
        var data3 = BitConverter.ToInt16(parts, 6);
        var data4 = new byte[8];
        Array.Copy(parts, 8, data4, 0, 8);

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var uppercase = rawGuid.All(c => !char.IsLetter(c) || char.IsUpper(c));
        var newExpression = generator.ObjectCreationExpression(editor.SemanticModel.Compilation.GetBestTypeByMetadataName("System.Guid")!,
            [
                CreateHexLiteral(data1, uppercase),
                CreateHexLiteral(data2, uppercase),
                CreateHexLiteral(data3, uppercase),
                CreateHexLiteral(data4[0], uppercase),
                CreateHexLiteral(data4[1], uppercase),
                CreateHexLiteral(data4[2], uppercase),
                CreateHexLiteral(data4[3], uppercase),
                CreateHexLiteral(data4[4], uppercase),
                CreateHexLiteral(data4[5], uppercase),
                CreateHexLiteral(data4[6], uppercase),
                CreateHexLiteral(data4[7], uppercase),
            ])
            .WithTrailingTrivia(SyntaxFactory.Comment($" /* {rawGuid} */"));

        editor.ReplaceNode(nodeToFix.Syntax, newExpression);
        return editor.GetChangedDocument();
    }

    private static LiteralExpressionSyntax CreateHexLiteral(int value, bool uppercase) => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0x" + value.ToString(uppercase ? "X" : "x", CultureInfo.InvariantCulture), value));
    private static LiteralExpressionSyntax CreateHexLiteral(short value, bool uppercase) => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0x" + value.ToString(uppercase ? "X" : "x", CultureInfo.InvariantCulture), value));
    private static LiteralExpressionSyntax CreateHexLiteral(byte value, bool uppercase) => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0x" + value.ToString(uppercase ? "X" : "x", CultureInfo.InvariantCulture), value));
}
