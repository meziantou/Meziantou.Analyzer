using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class DoNotUseImplicitCultureSensitiveToStringInterpolationFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        if (context.Document.Project.ParseOptions is not CSharpParseOptions parseOptions || !parseOptions.LanguageVersion.IsCSharp10OrAbove())
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null || !CanUseStringCreate(semanticModel.Compilation))
            return;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix?.AncestorsAndSelf().OfType<InterpolatedStringExpressionSyntax>().FirstOrDefault() is not InterpolatedStringExpressionSyntax interpolatedString)
            return;

        var title = "Use string.Create with CultureInfo.InvariantCulture";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                ct => Fix(context.Document, interpolatedString, ct),
                equivalenceKey: title),
            context.Diagnostics);
    }

    private static async Task<Document> Fix(Document document, InterpolatedStringExpressionSyntax interpolatedStringExpression, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var compilation = editor.SemanticModel.Compilation;
        if (!CanUseStringCreate(compilation))
            return document;

        var cultureInfoType = compilation.GetBestTypeByMetadataName("System.Globalization.CultureInfo");
        if (cultureInfoType is null)
            return document;

        var generator = editor.Generator;
        var replacement = generator.InvocationExpression(
            generator.MemberAccessExpression(generator.TypeExpression(compilation.GetSpecialType(SpecialType.System_String)), "Create"),
            generator.MemberAccessExpression(generator.TypeExpression(cultureInfoType).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation), "InvariantCulture"),
            interpolatedStringExpression);

        editor.ReplaceNode(interpolatedStringExpression, replacement.WithTriviaFrom(interpolatedStringExpression));
        return editor.GetChangedDocument();
    }

    private static bool CanUseStringCreate(Compilation compilation)
    {
        var formatProviderType = compilation.GetBestTypeByMetadataName("System.IFormatProvider");
        var defaultInterpolatedStringHandlerType = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler");

        if (formatProviderType is null || defaultInterpolatedStringHandlerType is null)
            return false;

        return compilation.GetSpecialType(SpecialType.System_String)
            .GetMembers("Create")
            .OfType<IMethodSymbol>()
            .Any(method =>
                method.ReturnType.IsString() &&
                method.Parameters.Length == 2 &&
                method.Parameters[0].Type.IsEqualTo(formatProviderType) &&
                method.Parameters[1].Type.IsEqualTo(defaultInterpolatedStringHandlerType));
    }
}