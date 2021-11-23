using System;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class OptimizeLinqUsageFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods,
        RuleIdentifiers.UseIndexerInsteadOfElementAt,
        RuleIdentifiers.DuplicateEnumerable_OrderBy,
        RuleIdentifiers.OptimizeEnumerable_CombineMethods,
        RuleIdentifiers.OptimizeEnumerable_Count,
        RuleIdentifiers.OptimizeEnumerable_CastInsteadOfSelect);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix == null)
            return;

        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic == null)
            return;

        if (!Enum.TryParse(diagnostic.Properties.GetValueOrDefault("Data", ""), out OptimizeLinqUsageData data) || data == OptimizeLinqUsageData.None)
            return;

        // If the so-called nodeToFix is a Name (most likely a method name such as 'Select' or 'Count'),
        // adjust it so that it refers to its InvocationExpression ancestor instead.
        if ((nodeToFix.IsKind(SyntaxKind.IdentifierName) || nodeToFix.IsKind(SyntaxKind.GenericName)) && !TryGetInvocationExpressionAncestor(ref nodeToFix))
            return;

        var title = "Optimize linq usage";
        switch (data)
        {
            case OptimizeLinqUsageData.UseLengthProperty:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseLengthProperty(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseLongLengthProperty:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseLongLengthProperty(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseCountProperty:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseCountProperty(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseFindMethod:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseFindMethod(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseIndexer:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseIndexer(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseIndexerFirst:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseIndexerFirst(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseIndexerLast:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseIndexerLast(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.DuplicatedOrderBy:
                var useThenByTitle = "Use " + diagnostic.Properties["ExpectedMethodName"];
                var removeOrderByTitle = "Remove " + diagnostic.Properties["MethodName"];
                context.RegisterCodeFix(CodeAction.Create(useThenByTitle, ct => UseThenBy(context.Document, diagnostic, ct), equivalenceKey: "UseThenBy"), context.Diagnostics);
                context.RegisterCodeFix(CodeAction.Create(removeOrderByTitle, ct => RemoveDuplicatedOrderBy(context.Document, diagnostic, ct), equivalenceKey: "RemoveOrderBy"), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.CombineWhereWithNextMethod:
                context.RegisterCodeFix(CodeAction.Create(title, ct => CombineWhereWithNextMethod(context.Document, diagnostic, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseTrue:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseConstantValue(context.Document, nodeToFix, constantValue: true, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseFalse:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseConstantValue(context.Document, nodeToFix, constantValue: false, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseNotAny:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseAny(context.Document, diagnostic, nodeToFix, constantValue: false, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseAny:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseAny(context.Document, diagnostic, nodeToFix, constantValue: true, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseTakeAndCount:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseTakeAndCount(context.Document, diagnostic, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseSkipAndAny:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseSkipAndAny(context.Document, diagnostic, nodeToFix, comparandValue: true, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseSkipAndNotAny:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseSkipAndAny(context.Document, diagnostic, nodeToFix, comparandValue: false, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseCastInsteadOfSelect:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseCastInsteadOfSelect(context.Document, diagnostic, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;
        }
    }

    private static bool TryGetInvocationExpressionAncestor(ref SyntaxNode nodeToFix)
    {
        var node = nodeToFix;
        while (node != null)
        {
            if (node.IsKind(SyntaxKind.InvocationExpression))
            {
                nodeToFix = node;
                return true;
            }
            node = node.Parent;
        }
        return false;
    }

    private static async Task<Document> UseAny(Document document, Diagnostic diagnostic, SyntaxNode nodeToFix, bool constantValue, CancellationToken cancellationToken)
    {
        var countOperationStart = int.Parse(diagnostic.Properties["CountOperationStart"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var countOperationLength = int.Parse(diagnostic.Properties["CountOperationLength"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var countNode = root?.FindNode(new TextSpan(countOperationStart, countOperationLength), getInnermostNodeForTie: true);
        if (countNode == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        if (semanticModel.GetOperation(countNode, cancellationToken) is not IInvocationOperation countOperation)
            return document;

        var generator = editor.Generator;
        var newExpression = generator.InvocationExpression(
            generator.MemberAccessExpression(countOperation.Arguments[0].Syntax, "Any"),
            countOperation.Arguments.Skip(1).Select(arg => arg.Syntax));

        if (!constantValue)
        {
            newExpression = generator.LogicalNotExpression(newExpression);
        }

        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> UseTakeAndCount(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var countOperationStart = int.Parse(diagnostic.Properties["CountOperationStart"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var countOperationLength = int.Parse(diagnostic.Properties["CountOperationLength"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var operandOperationStart = int.Parse(diagnostic.Properties["OperandOperationStart"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var operandOperationLength = int.Parse(diagnostic.Properties["OperandOperationLength"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var countNode = root?.FindNode(new TextSpan(countOperationStart, countOperationLength), getInnermostNodeForTie: true);
        var operandNode = root?.FindNode(new TextSpan(operandOperationStart, operandOperationLength), getInnermostNodeForTie: true);
        if (countNode == null || operandNode == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        var operandOperation = semanticModel?.GetOperation(operandNode, cancellationToken);
        if (semanticModel?.GetOperation(countNode, cancellationToken) is not IInvocationOperation countOperation || operandOperation == null)
            return document;

        var generator = editor.Generator;

        var newExpression = countOperation.Arguments[0].Syntax;
        if (countOperation.Arguments.Length > 1)
        {
            newExpression = generator.InvocationExpression(
                generator.MemberAccessExpression(newExpression, "Where"),
                countOperation.Arguments.Skip(1).Select(arg => arg.Syntax));
        }

        SyntaxNode takeArgument;
        if (operandOperation.ConstantValue.Value is int value)
        {
            takeArgument = generator.LiteralExpression(value + 1);
        }
        else
        {
            takeArgument = generator.AddExpression(operandOperation.Syntax, generator.LiteralExpression(1));
        }

        newExpression = generator.InvocationExpression(
                generator.MemberAccessExpression(newExpression, "Take"),
                takeArgument);

        newExpression = generator.InvocationExpression(generator.MemberAccessExpression(newExpression, "Count"));

        editor.ReplaceNode(countOperation.Syntax, newExpression);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> UseSkipAndAny(Document document, Diagnostic diagnostic, SyntaxNode nodeToFix, bool comparandValue, CancellationToken cancellationToken)
    {
        var countOperationStart = int.Parse(diagnostic.Properties["CountOperationStart"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var countOperationLength = int.Parse(diagnostic.Properties["CountOperationLength"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var operandOperationStart = int.Parse(diagnostic.Properties["OperandOperationStart"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var operandOperationLength = int.Parse(diagnostic.Properties["OperandOperationLength"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var skipMinusOne = diagnostic.Properties.ContainsKey("SkipMinusOne");

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var countNode = root?.FindNode(new TextSpan(countOperationStart, countOperationLength), getInnermostNodeForTie: true);
        var operandNode = root?.FindNode(new TextSpan(operandOperationStart, operandOperationLength), getInnermostNodeForTie: true);
        if (countNode == null || operandNode == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        var operandOperation = semanticModel.GetOperation(operandNode, cancellationToken);
        if (semanticModel.GetOperation(countNode, cancellationToken) is not IInvocationOperation countOperation || operandOperation == null)
            return document;

        var generator = editor.Generator;

        var newExpression = countOperation.Arguments[0].Syntax;
        if (countOperation.Arguments.Length > 1)
        {
            newExpression = generator.InvocationExpression(
                generator.MemberAccessExpression(newExpression, "Where"),
                countOperation.Arguments.Skip(1).Select(arg => arg.Syntax));
        }

        SyntaxNode skipArgument;
        if (operandOperation.ConstantValue.Value is int value)
        {
            if (skipMinusOne)
            {
                skipArgument = generator.LiteralExpression(value - 1);
            }
            else
            {
                skipArgument = generator.LiteralExpression(value);
            }
        }
        else
        {
            if (skipMinusOne)
            {
                skipArgument = generator.SubtractExpression(operandOperation.Syntax, generator.LiteralExpression(1));
            }
            else
            {
                skipArgument = operandOperation.Syntax;
            }
        }

        newExpression = generator.InvocationExpression(
                generator.MemberAccessExpression(newExpression, "Skip"),
                skipArgument);

        newExpression = generator.InvocationExpression(generator.MemberAccessExpression(newExpression, "Any"));

        if (!comparandValue)
        {
            newExpression = generator.LogicalNotExpression(newExpression);
        }

        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> UseCastInsteadOfSelect(Document document, Diagnostic diagnostic, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        if (nodeToFix is not InvocationExpressionSyntax selectInvocationExpression)
            return document;

        if (selectInvocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
            return document;

        // Build the 'Cast<CastType>' name
        var castType = diagnostic.Properties.GetValueOrDefault("CastType");
        var castNameSyntax = GenericName(Identifier("Cast"))
            .WithTypeArgumentList(
                TypeArgumentList(
                    SingletonSeparatedList<TypeSyntax>(
                        IdentifierName(castType!))));

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        // Is the 'source' (i.e. the sequence of values 'Select' is invoked on) passed in as argument?
        //  If there is 1 argument      -> No 'source' argument, only 'selector'
        //  If there are 2 arguments    -> The 1st argument is the 'source'
        var argumentListArguments = selectInvocationExpression.ArgumentList.Arguments;
        var sourceArg = argumentListArguments.Reverse().Skip(1).FirstOrDefault();

        SyntaxNode castInvocationExpression;
        if (sourceArg is null)
        {
            castInvocationExpression = generator.InvocationExpression(
                generator.MemberAccessExpression(memberAccessExpression.Expression, castNameSyntax));
        }
        else
        {
            castInvocationExpression = generator.InvocationExpression(
                generator.MemberAccessExpression(memberAccessExpression.Expression, castNameSyntax),
                sourceArg);
        }

        editor.ReplaceNode(selectInvocationExpression, castInvocationExpression.WithAdditionalAnnotations(Simplifier.Annotation));
        return editor.GetChangedDocument();
    }

    private static async Task<Document> UseConstantValue(Document document, SyntaxNode nodeToFix, bool constantValue, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var literalNode = constantValue ? generator.TrueLiteralExpression() : generator.FalseLiteralExpression();

        editor.ReplaceNode(nodeToFix, literalNode);
        return editor.GetChangedDocument();
    }

    private async static Task<Document> UseLengthProperty(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var expression = GetParentMemberExpression(nodeToFix);
        if (expression == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var propertyAccess = generator.MemberAccessExpression(expression, "Length");

        editor.ReplaceNode(nodeToFix, propertyAccess);
        return editor.GetChangedDocument();
    }

    private async static Task<Document> UseLongLengthProperty(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var expression = GetParentMemberExpression(nodeToFix);
        if (expression == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var propertyAccess = generator.MemberAccessExpression(expression, "LongLength");

        editor.ReplaceNode(nodeToFix, propertyAccess);
        return editor.GetChangedDocument();
    }

    private async static Task<Document> UseCountProperty(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var expression = GetParentMemberExpression(nodeToFix);
        if (expression == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var propertyAccess = generator.MemberAccessExpression(expression, "Count");

        editor.ReplaceNode(nodeToFix, propertyAccess);
        return editor.GetChangedDocument();
    }

    private async static Task<Document> UseFindMethod(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var expression = GetMemberAccessExpression(nodeToFix);
        if (expression == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var newExpression = expression.WithName(IdentifierName("Find"));

        editor.ReplaceNode(expression, newExpression);
        return editor.GetChangedDocument();
    }

    private async static Task<Document> UseIndexer(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var expression = GetParentMemberExpression(nodeToFix);
        if (expression == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var semanticModel = editor.SemanticModel;
        if (semanticModel.GetOperation(nodeToFix, cancellationToken) is not IInvocationOperation operation)
            return document;


        var newExpression = generator.ElementAccessExpression(operation.Arguments[0].Syntax, operation.Arguments[1].Syntax);

        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }

    private async static Task<Document> UseIndexerFirst(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var expression = GetParentMemberExpression(nodeToFix);
        if (expression == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var semanticModel = editor.SemanticModel;
        if (semanticModel.GetOperation(nodeToFix, cancellationToken) is not IInvocationOperation operation)
            return document;

        var newExpression = generator.ElementAccessExpression(operation.Arguments[0].Syntax, generator.LiteralExpression(0));

        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }

    private async static Task<Document> UseIndexerLast(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var expression = GetParentMemberExpression(nodeToFix);
        if (expression == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var semanticModel = editor.SemanticModel;
        if (semanticModel.GetOperation(nodeToFix, cancellationToken) is not IInvocationOperation operation)
            return document;

        var newExpression = generator.ElementAccessExpression(operation.Arguments[0].Syntax,
            generator.SubtractExpression(
                generator.MemberAccessExpression(operation.Arguments[0].Syntax, GetMemberName()),
                generator.LiteralExpression(1)));

        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();

        string GetMemberName()
        {
            var type = operation.Arguments[0].Value.GetActualType();
            var isArray = type != null && type.TypeKind == TypeKind.Array;
            if (isArray)
                return "Length";

            return "Count";
        }
    }

    private static async Task<Document> RemoveDuplicatedOrderBy(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        // a."b()".c()
        // a.c()
        var firstOperationStart = int.Parse(diagnostic.Properties["FirstOperationStart"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var firstOperationLength = int.Parse(diagnostic.Properties["FirstOperationLength"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var lastOperationStart = int.Parse(diagnostic.Properties["LastOperationStart"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var lastOperationLength = int.Parse(diagnostic.Properties["LastOperationLength"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var firstNode = root?.FindNode(new TextSpan(firstOperationStart, firstOperationLength), getInnermostNodeForTie: true);
        var lastNode = root?.FindNode(new TextSpan(lastOperationStart, lastOperationLength), getInnermostNodeForTie: true);
        if (firstNode == null || lastNode == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        if (semanticModel?.GetOperation(firstNode, cancellationToken) is not IInvocationOperation firstOperation || semanticModel?.GetOperation(lastNode, cancellationToken) is not IInvocationOperation lastOperation)
            return document;

        var method = editor.Generator.MemberAccessExpression(firstOperation.Arguments[0].Syntax, lastOperation.TargetMethod.Name);
        var newExpression = editor.Generator.InvocationExpression(method, lastOperation.Arguments.Skip(1).Select(arg => arg.Syntax));

        editor.ReplaceNode(lastOperation.Syntax, newExpression);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> UseThenBy(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var lastOperationStart = int.Parse(diagnostic.Properties["LastOperationStart"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var lastOperationLength = int.Parse(diagnostic.Properties["LastOperationLength"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var expectedMethodName = diagnostic.Properties["ExpectedMethodName"]!;

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(new TextSpan(lastOperationStart, lastOperationLength), getInnermostNodeForTie: true);
        if (nodeToFix == null)
            return document;

        var expression = GetMemberAccessExpression(nodeToFix);
        if (expression == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var newExpression = expression.WithName(IdentifierName(expectedMethodName));

        editor.ReplaceNode(expression, newExpression);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> CombineWhereWithNextMethod(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        // enumerable.Where(x=> x).C() => enumerable.C(x=> x)
        // enumerable.Where(x=> x).C(y=>y) => enumerable.C(y=> y && y)
        // enumerable.Where(Condition).C(y=>y) => enumerable.C(y=> Condition(y) && y)
        var firstOperationStart = int.Parse(diagnostic.Properties["FirstOperationStart"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var firstOperationLength = int.Parse(diagnostic.Properties["FirstOperationLength"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var lastOperationStart = int.Parse(diagnostic.Properties["LastOperationStart"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var lastOperationLength = int.Parse(diagnostic.Properties["LastOperationLength"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var firstNode = root?.FindNode(new TextSpan(firstOperationStart, firstOperationLength), getInnermostNodeForTie: true);
        var lastNode = root?.FindNode(new TextSpan(lastOperationStart, lastOperationLength), getInnermostNodeForTie: true);
        if (firstNode == null || lastNode == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        if (semanticModel?.GetOperation(firstNode, cancellationToken) is not IInvocationOperation firstOperation || semanticModel?.GetOperation(lastNode, cancellationToken) is not IInvocationOperation lastOperation)
            return document;

        var generator = editor.Generator;
        var method = generator.MemberAccessExpression(firstOperation.Arguments[0].Syntax, lastOperation.TargetMethod.Name);
        var argument = CombineArguments(firstOperation.Arguments.ElementAtOrDefault(1), lastOperation.Arguments.ElementAtOrDefault(1));
        var newExpression = generator.InvocationExpression(method, argument);

        editor.ReplaceNode(lastOperation.Syntax, newExpression);
        return editor.GetChangedDocument();

        SyntaxNode? CombineArguments(IArgumentOperation? argument1, IArgumentOperation? argument2)
        {
            if (argument2 == null)
                return argument1?.Syntax;

            if (argument1 == null)
                return argument2?.Syntax;
            if (argument1.Value is not IDelegateCreationOperation value1 || argument2.Value is not IDelegateCreationOperation value2)
                return null;

            var anonymousMethod1 = value1.Target as IAnonymousFunctionOperation;
            var anonymousMethod2 = value2.Target as IAnonymousFunctionOperation;

            var newParameterName =
                anonymousMethod1?.Symbol.Parameters.ElementAtOrDefault(0)?.Name ??
                anonymousMethod2?.Symbol.Parameters.ElementAtOrDefault(0)?.Name ??
                "x";

            var left = PrepareSyntaxNode(generator, value1, newParameterName);
            var right = PrepareSyntaxNode(generator, value2, newParameterName);

            return generator.ValueReturningLambdaExpression(newParameterName,
                generator.LogicalAndExpression(left, right));
        }

        static SyntaxNode PrepareSyntaxNode(SyntaxGenerator generator, IDelegateCreationOperation delegateCreationOperation, string parameterName)
        {
            if (delegateCreationOperation.Target is IAnonymousFunctionOperation anonymousMethod)
            {
                return ReplaceParameter(anonymousMethod, parameterName);
            }

            if (delegateCreationOperation.Target is IMethodReferenceOperation)
            {
                return generator.InvocationExpression(
                    delegateCreationOperation.Syntax,
                    generator.IdentifierName("x"));
            }

            return delegateCreationOperation.Syntax;
        }
    }

    private static SyntaxNode ReplaceParameter(IAnonymousFunctionOperation method, string newParameterName)
    {
        var semanticModel = method.SemanticModel!;
        var parameterSymbol = method.Symbol.Parameters[0];
        return new ParameterRewriter(semanticModel, parameterSymbol, newParameterName).Visit(method.Body.Syntax);
    }

    private static MemberAccessExpressionSyntax? GetMemberAccessExpression(SyntaxNode invocationExpressionSyntax)
    {
        if (invocationExpressionSyntax is not InvocationExpressionSyntax invocationExpression)
            return null;

        return invocationExpression.Expression as MemberAccessExpressionSyntax;
    }

    private static SyntaxNode? GetParentMemberExpression(SyntaxNode invocationExpressionSyntax)
    {
        var memberAccessExpression = GetMemberAccessExpression(invocationExpressionSyntax);
        if (memberAccessExpression == null)
            return null;

        return memberAccessExpression.Expression;
    }

    private sealed class ParameterRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _semanticModel;
        private readonly IParameterSymbol _parameterSymbol;
        private readonly string _newParameterName;

        public ParameterRewriter(SemanticModel semanticModel, IParameterSymbol parameterSymbol, string newParameterName)
        {
            _semanticModel = semanticModel;
            _parameterSymbol = parameterSymbol;
            _newParameterName = newParameterName;
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            var symbol = _semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol != null && symbol.IsEqualTo(_parameterSymbol))
            {
                return IdentifierName(_newParameterName);
            }

            return base.VisitIdentifierName(node);
        }
    }
}
