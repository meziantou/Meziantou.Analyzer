using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseRegexSourceGeneratorFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseRegexSourceGenerator);

    public override FixAllProvider? GetFixAllProvider()
    {
        return null;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: false);
        if (nodeToFix is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use Regex Source Generator",
                cancellationToken => ConvertToSourceGenerator(context.Document, context.Diagnostics[0], cancellationToken),
                equivalenceKey: "Use Regex Source Generator"),
            context.Diagnostics);
    }

    private static async Task<Document> ConvertToSourceGenerator(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var compilation = semanticModel!.Compilation;

        var regexSymbol = compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.Regex");
        var regexGeneratorAttributeSymbol = compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.GeneratedRegexAttribute");
        if (regexSymbol is null || regexGeneratorAttributeSymbol is null)
            return document;

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: false);
        var typeDeclaration = nodeToFix?.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (root is null || nodeToFix is null || typeDeclaration is null)
            return document;

        // Get type info before changing the root
        var properties = diagnostic.Properties;
        var operation = semanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation is null)
            return document;

        // Generate unique method name
        var methodName = "MyRegex";
        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
        if (typeSymbol is not null)
        {
            var members = typeSymbol.GetAllMembers().ToArray();
            while (members.Any(m => m.Name == methodName))
            {
                methodName += "_";
            }
        }

        // Add partial to the type hierarchy
        var count = 0;
        root = root.ReplaceNodes(nodeToFix.Ancestors().OfType<TypeDeclarationSyntax>(), (_, r) =>
        {
            if (!r.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                count++;
                return r.AddModifiers(Token(SyntaxKind.PartialKeyword));
            }

            return r;
        });

        // Get the new node to fix in the new syntax tree
        nodeToFix = root.FindNode(new Microsoft.CodeAnalysis.Text.TextSpan(nodeToFix.Span.Start + (count * "partial".Length), nodeToFix.Span.Length));
        if (nodeToFix is null)
            return document;

        typeDeclaration = nodeToFix.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (typeDeclaration is null)
            return document;

        var newTypeDeclaration = typeDeclaration;

        // Use new method
        if (operation is IObjectCreationOperation)
        {
            var invokeMethod = generator.InvocationExpression(generator.IdentifierName(methodName));
            newTypeDeclaration = newTypeDeclaration.ReplaceNode(nodeToFix, invokeMethod);
        }
        else if (operation is IInvocationOperation invocationOperation)
        {
            var arguments = invocationOperation.Arguments;
            var indices = new[]
            {
                TryParseInt32(properties, UseRegexSourceGeneratorAnalyzerCommon.PatternIndexName),
                TryParseInt32(properties, UseRegexSourceGeneratorAnalyzerCommon.RegexOptionsIndexName),
                TryParseInt32(properties, UseRegexSourceGeneratorAnalyzerCommon.RegexTimeoutIndexName),
            };
            foreach (var index in indices.Where(value => value is not null).OrderDescending())
            {
                arguments = arguments.RemoveAt(index.GetValueOrDefault());
            }

            var createRegexMethod = generator.InvocationExpression(generator.IdentifierName(methodName));
            var method = generator.InvocationExpression(generator.MemberAccessExpression(createRegexMethod, invocationOperation.TargetMethod.Name), [.. arguments.Select(arg => arg.Syntax)]);

            newTypeDeclaration = newTypeDeclaration.ReplaceNode(nodeToFix, method);
        }

        // Generate method
        SyntaxNode? patternValue = null;
        SyntaxNode? regexOptionsValue = null;
        SyntaxNode? timeoutValue = null;

        var timeout = TryParseInt32(properties, UseRegexSourceGeneratorAnalyzerCommon.RegexTimeoutName);
        if (timeout is not null)
        {
            timeoutValue = generator.LiteralExpression(timeout.Value);
        }

        if (operation is IObjectCreationOperation objectCreationOperation)
        {
            patternValue = GetNode(objectCreationOperation.Arguments, properties, UseRegexSourceGeneratorAnalyzerCommon.PatternIndexName);
            regexOptionsValue = GetNode(objectCreationOperation.Arguments, properties, UseRegexSourceGeneratorAnalyzerCommon.RegexOptionsIndexName);
        }
        else if (operation is IInvocationOperation invocationOperation)
        {
            patternValue = GetNode(invocationOperation.Arguments, properties, UseRegexSourceGeneratorAnalyzerCommon.PatternIndexName);
            regexOptionsValue = GetNode(invocationOperation.Arguments, properties, UseRegexSourceGeneratorAnalyzerCommon.RegexOptionsIndexName);
        }

        if (timeoutValue is not null && regexOptionsValue is null)
        {
            regexOptionsValue = generator.MemberAccessExpression(generator.TypeExpression(compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.RegexOptions")!), "None");
        }

        var newMethod = (MethodDeclarationSyntax)generator.MethodDeclaration(
            name: methodName,
            returnType: generator.TypeExpression(regexSymbol),
            modifiers: DeclarationModifiers.Static | DeclarationModifiers.Partial,
            accessibility: Accessibility.Private);

        newMethod = newMethod.ReplaceToken(newMethod.Identifier, Identifier(methodName).WithAdditionalAnnotations(RenameAnnotation.Create()));

        // Extract arguments (pattern,options,timeout)
        var attributes = generator.Attribute(generator.TypeExpression(regexGeneratorAttributeSymbol), attributeArguments: (patternValue, regexOptionsValue, timeoutValue) switch
        {
            ({ }, null, null) => [patternValue],
            ({ }, { }, null) => [patternValue, regexOptionsValue],
            ({ }, { }, { }) => [patternValue, regexOptionsValue, AttributeArgument((ExpressionSyntax)timeoutValue).WithNameColon(NameColon(IdentifierName("matchTimeoutMilliseconds")))],
            _ => Array.Empty<SyntaxNode>(),
        });

        newMethod = (MethodDeclarationSyntax)generator.AddAttributes(newMethod, attributes);
        newTypeDeclaration = newTypeDeclaration.AddMembers(newMethod);
        return document.WithSyntaxRoot(root.ReplaceNode(typeDeclaration, newTypeDeclaration));
    }

    private static SyntaxNode? GetNode(ImmutableArray<IArgumentOperation> args, ImmutableDictionary<string, string?> properties, string name)
    {
        var index = TryParseInt32(properties, name);
        if (index is null)
            return null;

        return args[index.Value].Value.Syntax;
    }

    private static int? TryParseInt32(ImmutableDictionary<string, string?> properties, string name)
    {
        if (!properties.TryGetValue(name, out var value))
            return null;

        if (!int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return null;

        return result;
    }
}
