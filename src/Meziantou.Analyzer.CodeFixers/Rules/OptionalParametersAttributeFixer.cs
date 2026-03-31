using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class OptionalParametersAttributeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        RuleIdentifiers.ParametersWithDefaultValueShouldBeMarkedWithOptionalParameter,
        RuleIdentifiers.DefaultValueShouldNotBeUsedWhenParameterDefaultValueIsMeant);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var parameter = nodeToFix as ParameterSyntax ?? nodeToFix.AncestorsAndSelf().OfType<ParameterSyntax>().FirstOrDefault();
        if (parameter is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var optionalAttributeSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.OptionalAttribute");
        var defaultValueAttributeSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.ComponentModel.DefaultValueAttribute");
        var defaultParameterValueAttributeSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.DefaultParameterValueAttribute");
        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id == RuleIdentifiers.ParametersWithDefaultValueShouldBeMarkedWithOptionalParameter)
            {
                if (optionalAttributeSymbol is null)
                    continue;

                var hasOptionalAttribute = parameter.AttributeLists
                    .SelectMany(a => a.Attributes)
                    .Any(attribute =>
                    {
                        var attributeSymbol = semanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol?.ContainingType;
                        return attributeSymbol is not null && attributeSymbol.IsEqualTo(optionalAttributeSymbol);
                    });

                if (!hasOptionalAttribute)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Add [Optional]",
                            ct => AddOptionalAttribute(context.Document, parameter, optionalAttributeSymbol, ct),
                            equivalenceKey: "Add [Optional]"),
                        diagnostic);
                }
            }
            else if (diagnostic.Id == RuleIdentifiers.DefaultValueShouldNotBeUsedWhenParameterDefaultValueIsMeant)
            {
                if (defaultValueAttributeSymbol is null || defaultParameterValueAttributeSymbol is null)
                    continue;

                var defaultValueAttribute = parameter.AttributeLists
                    .SelectMany(a => a.Attributes)
                    .FirstOrDefault(attribute =>
                    {
                        var attributeSymbol = semanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol?.ContainingType;
                        return attributeSymbol is not null && attributeSymbol.IsEqualTo(defaultValueAttributeSymbol);
                    });

                if (defaultValueAttribute is null)
                    continue;

                var hasDefaultParameterValueAttribute = parameter.AttributeLists
                    .SelectMany(a => a.Attributes)
                    .Any(attribute =>
                    {
                        var attributeSymbol = semanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol?.ContainingType;
                        return attributeSymbol is not null && attributeSymbol.IsEqualTo(defaultParameterValueAttributeSymbol);
                    });

                if (!hasDefaultParameterValueAttribute)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Add [DefaultParameterValue]",
                            ct => AddDefaultParameterValueAttribute(context.Document, defaultValueAttribute, ct),
                            equivalenceKey: "Add [DefaultParameterValue]"),
                        diagnostic);
                }
            }
        }
    }

    private static async Task<Document> AddOptionalAttribute(Document document, ParameterSyntax parameterSyntax, ITypeSymbol optionalAttributeSymbol, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var attributeName = (NameSyntax)editor.Generator.TypeExpression(optionalAttributeSymbol, addImport: true).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);
        var optionalAttribute = Attribute(attributeName);
        ParameterSyntax updatedParameter;
        if (parameterSyntax.AttributeLists.Count == 0)
        {
            var attributeList = AttributeList(SingletonSeparatedList(optionalAttribute));
            updatedParameter = parameterSyntax.WithAttributeLists(parameterSyntax.AttributeLists.Add(attributeList));
        }
        else
        {
            var firstList = parameterSyntax.AttributeLists[0];
            var updatedFirstList = firstList.WithAttributes(firstList.Attributes.Insert(0, optionalAttribute));
            updatedParameter = parameterSyntax.WithAttributeLists(parameterSyntax.AttributeLists.Replace(firstList, updatedFirstList));
        }

        editor.ReplaceNode(parameterSyntax, updatedParameter);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> AddDefaultParameterValueAttribute(Document document, AttributeSyntax defaultValueAttribute, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (defaultValueAttribute.Parent is not AttributeListSyntax attributeList)
            return document;

        var attribute = Attribute(ParseName("DefaultParameterValue"), defaultValueAttribute.ArgumentList)
            .WithLeadingTrivia(Space);

        editor.ReplaceNode(attributeList, attributeList.WithAttributes(attributeList.Attributes.Add(attribute)));
        return editor.GetChangedDocument();
    }
}
