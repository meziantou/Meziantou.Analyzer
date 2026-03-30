using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class DoNotUseZeroToInitializeAnEnumValueFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.DoNotUseZeroToInitializeAnEnumValue);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var expressionToFix = nodeToFix as ExpressionSyntax ?? nodeToFix.AncestorsAndSelf().OfType<ExpressionSyntax>().FirstOrDefault();
        if (expressionToFix is null)
            return;

        var enumType = GetTargetEnumType(semanticModel, expressionToFix, context.CancellationToken);
        if (enumType is null)
            return;

        var zeroEnumField = enumType
            .GetMembers()
            .OfType<IFieldSymbol>()
            .Where(field => field.HasConstantValue)
            .FirstOrDefault(field => IsZero(field.ConstantValue));
        if (zeroEnumField is not null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Use {zeroEnumField.Name}",
                    ct => UseEnumField(context.Document, expressionToFix, zeroEnumField, ct),
                    equivalenceKey: "Use enum member"),
                context.Diagnostics);
        }
        else
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use explicit enum cast",
                    ct => UseEnumCast(context.Document, expressionToFix, enumType, ct),
                    equivalenceKey: "Use explicit enum cast"),
                context.Diagnostics);
        }

        static bool IsZero(object? value)
        {
            return value switch
            {
                sbyte v => v == 0,
                byte v => v == 0,
                short v => v == 0,
                ushort v => v == 0,
                int v => v == 0,
                uint v => v == 0,
                long v => v == 0,
                ulong v => v == 0,
                _ => false,
            };
        }
    }

    private static INamedTypeSymbol? GetTargetEnumType(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        if (semanticModel.GetTypeInfo(expression, cancellationToken).ConvertedType is INamedTypeSymbol { EnumUnderlyingType: not null } convertedEnumType)
            return convertedEnumType;

        if (expression.Parent is EqualsValueClauseSyntax equalsValueClause)
        {
            if (equalsValueClause.Parent is ParameterSyntax parameterSyntax)
            {
                if (semanticModel.GetDeclaredSymbol(parameterSyntax, cancellationToken) is IParameterSymbol parameterSymbol &&
                    parameterSymbol.Type is INamedTypeSymbol { EnumUnderlyingType: not null } parameterEnumType)
                {
                    return parameterEnumType;
                }
            }
            else if (equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclaratorSyntax &&
                     semanticModel.GetDeclaredSymbol(variableDeclaratorSyntax, cancellationToken) is ILocalSymbol localSymbol &&
                     localSymbol.Type is INamedTypeSymbol { EnumUnderlyingType: not null } localEnumType)
            {
                return localEnumType;
            }
        }

        if (expression.Parent is AssignmentExpressionSyntax assignmentExpression &&
            assignmentExpression.Right == expression &&
            semanticModel.GetTypeInfo(assignmentExpression.Left, cancellationToken).Type is INamedTypeSymbol { EnumUnderlyingType: not null } assignmentEnumType)
        {
            return assignmentEnumType;
        }

        if (expression.Parent is ArgumentSyntax argumentSyntax &&
            semanticModel.GetOperation(argumentSyntax, cancellationToken) is IArgumentOperation { Parameter.Type: INamedTypeSymbol { EnumUnderlyingType: not null } parameterEnumType2 })
        {
            return parameterEnumType2;
        }

        return null;
    }

    private static async Task<Document> UseEnumField(Document document, ExpressionSyntax expressionToFix, IFieldSymbol fieldSymbol, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var memberAccess = MemberAccessExpression(
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression,
            (NameSyntax)ParseName(fieldSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "", StringComparison.Ordinal)),
            IdentifierName(fieldSymbol.Name))
            .WithAdditionalAnnotations(Simplifier.Annotation);

        editor.ReplaceNode(expressionToFix, memberAccess);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> UseEnumCast(Document document, ExpressionSyntax expressionToFix, INamedTypeSymbol enumType, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var castExpression = CastExpression(
            (TypeSyntax)ParseName(enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "", StringComparison.Ordinal)),
            expressionToFix.WithoutTrivia())
            .WithTriviaFrom(expressionToFix);

        editor.ReplaceNode(expressionToFix, castExpression);
        return editor.GetChangedDocument();
    }
}
