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
        RuleIdentifiers.OptimizeEnumerable_WhereBeforeOrderBy,
        RuleIdentifiers.OptimizeEnumerable_Count,
        RuleIdentifiers.OptimizeEnumerable_UseCountInsteadOfAny,
        RuleIdentifiers.OptimizeEnumerable_CastInsteadOfSelect,
        RuleIdentifiers.OptimizeEnumerable_UseOrder);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic is null)
            return;

        if (diagnostic.Id == RuleIdentifiers.OptimizeEnumerable_UseCountInsteadOfAny)
        {
            const string CodeFixTitle = "Optimize linq usage";
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (semanticModel?.GetOperation(nodeToFix, context.CancellationToken) is not IInvocationOperation { Arguments: [var source] })
                return;

            // The Count property of arrays is an explicit implementation of ICollection<T>
            var propertyName = source.Value.GetActualType() is { TypeKind: TypeKind.Array } ? "Length" : "Count";

            // 'items?.Any()' cannot be replaced by a comparison, as the result of the conditional access is nullable
            var countExpression = CreatePropertyAccess(SyntaxGenerator.GetGenerator(context.Document), nodeToFix, propertyName, allowConditionalAccess: false);
            if (countExpression is null)
                return;

            context.RegisterCodeFix(CodeAction.Create(CodeFixTitle, ct => UseCountGreaterThanZero(context.Document, nodeToFix, countExpression, ct), equivalenceKey: CodeFixTitle), context.Diagnostics);
            return;
        }

        if (!Enum.TryParse(diagnostic.Properties.GetValueOrDefault(OptimizeLinqUsageAnalyzerCommon.DataKey, ""), ignoreCase: false, out OptimizeLinqUsageData data) || data is OptimizeLinqUsageData.None)
            return;

        // If the so-called nodeToFix is a Name (most likely a method name such as 'Select' or 'Count'),
        // adjust it so that it refers to its InvocationExpression ancestor instead.
        if ((nodeToFix.IsKind(SyntaxKind.IdentifierName) || nodeToFix.IsKind(SyntaxKind.GenericName)) && !TryGetInvocationExpressionAncestor(ref nodeToFix))
            return;

        var title = "Optimize linq usage";
        switch (data)
        {
            case OptimizeLinqUsageData.UseLengthProperty:
            case OptimizeLinqUsageData.UseLongLengthProperty:
            case OptimizeLinqUsageData.UseCountProperty:
                var propertyName = data switch
                {
                    OptimizeLinqUsageData.UseLengthProperty => "Length",
                    OptimizeLinqUsageData.UseLongLengthProperty => "LongLength",
                    _ => "Count",
                };

                var propertyAccess = CreatePropertyAccess(SyntaxGenerator.GetGenerator(context.Document), nodeToFix, propertyName, allowConditionalAccess: true);
                if (propertyAccess is null)
                    return;

                context.RegisterCodeFix(CodeAction.Create(title, ct => ReplaceNode(context.Document, nodeToFix, propertyAccess.WithTriviaFrom(nodeToFix), ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseFindMethod:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseListMethod(context.Document, nodeToFix, "Find", convertPredicate: false, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseFindMethodWithConversion:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseListMethod(context.Document, nodeToFix, "Find", convertPredicate: true, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseTrueForAllMethod:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseListMethod(context.Document, nodeToFix, "TrueForAll", convertPredicate: false, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseTrueForAllMethodWithConversion:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseListMethod(context.Document, nodeToFix, "TrueForAll", convertPredicate: true, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseExistsMethod:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseListMethod(context.Document, nodeToFix, "Exists", convertPredicate: false, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseExistsMethodWithConversion:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseListMethod(context.Document, nodeToFix, "Exists", convertPredicate: true, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseIndexer:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseIndexer(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseIndexerFirst:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseIndexerFirst(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseIndexerLast:
                if (!await CanUseIndexerLast(context.Document, nodeToFix, context.CancellationToken).ConfigureAwait(false))
                    return;

                context.RegisterCodeFix(CodeAction.Create(title, ct => UseIndexerLast(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.DuplicatedOrderBy:
                if (!TryGetFirstOperationSpan(diagnostic, out var orderByFirstSpan) || !TryGetLastOperationSpan(diagnostic, out var orderByLastSpan))
                    return;

                if (!diagnostic.Properties.TryGetValue(OptimizeLinqUsageAnalyzerCommon.ExpectedMethodNameKey, out var expectedMethodName) || expectedMethodName is null)
                    return;

                if (!diagnostic.Properties.TryGetValue(OptimizeLinqUsageAnalyzerCommon.MethodNameKey, out var methodName) || methodName is null)
                    return;

                if (root?.FindNode(orderByLastSpan, getInnermostNodeForTie: true) is InvocationExpressionSyntax lastInvocation && GetMethodNameSyntax(lastInvocation) is { } lastMethodName)
                {
                    // Order() and OrderDescending() have no key selector, so ThenBy(x => x) must use the element as the key
                    string? keySelectorParameterName = null;
                    if (methodName is "Order" or "OrderDescending")
                    {
                        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                        if (semanticModel is null)
                            return;

                        keySelectorParameterName = GetUniqueParameterName(semanticModel, lastInvocation.SpanStart, _ => true);
                        if (keySelectorParameterName is null)
                            return;
                    }

                    context.RegisterCodeFix(CodeAction.Create("Use " + expectedMethodName, ct => UseThenBy(context.Document, lastInvocation, lastMethodName, expectedMethodName, keySelectorParameterName, ct), equivalenceKey: "UseThenBy"), context.Diagnostics);
                }

                context.RegisterCodeFix(CodeAction.Create("Remove " + methodName, ct => RemoveDuplicatedOrderBy(context.Document, orderByFirstSpan, orderByLastSpan, ct), equivalenceKey: "RemoveOrderBy"), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.CombineWhereWithNextMethod:
                if (!TryGetFirstOperationSpan(diagnostic, out var whereFirstSpan) || !TryGetLastOperationSpan(diagnostic, out var whereLastSpan))
                    return;

                if (diagnostic.Id == RuleIdentifiers.OptimizeEnumerable_WhereBeforeOrderBy)
                {
                    context.RegisterCodeFix(CodeAction.Create(title, ct => ReorderWhereBeforeOrderBy(context.Document, whereFirstSpan, whereLastSpan, ct), equivalenceKey: title), context.Diagnostics);
                }
                else
                {
                    var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                    var whereNode = root?.FindNode(whereFirstSpan, getInnermostNodeForTie: true);
                    var nextNode = root?.FindNode(whereLastSpan, getInnermostNodeForTie: true);
                    if (semanticModel is null || whereNode is null || nextNode is null)
                        return;

                    if (semanticModel.GetOperation(whereNode, context.CancellationToken) is not IInvocationOperation whereOperation || semanticModel.GetOperation(nextNode, context.CancellationToken) is not IInvocationOperation nextOperation)
                        return;

                    var combinedExpression = CombineWhereWithNextMethod(SyntaxGenerator.GetGenerator(context.Document), semanticModel, whereOperation, nextOperation, context.CancellationToken);
                    if (combinedExpression is null)
                        return;

                    context.RegisterCodeFix(CodeAction.Create(title, ct => ReplaceNode(context.Document, nextOperation.Syntax, combinedExpression, ct), equivalenceKey: title), context.Diagnostics);
                }

                break;

            case OptimizeLinqUsageData.UseTrue:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseConstantValue(context.Document, nodeToFix, constantValue: true, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseFalse:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseConstantValue(context.Document, nodeToFix, constantValue: false, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseNotAny:
            case OptimizeLinqUsageData.UseAny:
                if (!TryGetCountOperationSpan(diagnostic, out var anyCountSpan))
                    return;

                context.RegisterCodeFix(CodeAction.Create(title, ct => UseAny(context.Document, anyCountSpan, nodeToFix, constantValue: data is OptimizeLinqUsageData.UseAny, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseTakeAndCount:
                if (!TryGetCountOperationSpan(diagnostic, out var takeCountSpan) || !TryGetOperandOperationSpan(diagnostic, out var takeOperandSpan))
                    return;

                context.RegisterCodeFix(CodeAction.Create(title, ct => UseTakeAndCount(context.Document, takeCountSpan, takeOperandSpan, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseSkipAndAny:
            case OptimizeLinqUsageData.UseSkipAndNotAny:
                if (!TryGetCountOperationSpan(diagnostic, out var skipCountSpan) || !TryGetOperandOperationSpan(diagnostic, out var skipOperandSpan))
                    return;

                var skipMinusOne = diagnostic.Properties.ContainsKey(OptimizeLinqUsageAnalyzerCommon.SkipMinusOneKey);
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseSkipAndAny(context.Document, skipCountSpan, skipOperandSpan, skipMinusOne, nodeToFix, comparandValue: data is OptimizeLinqUsageData.UseSkipAndAny, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseCastInsteadOfSelect:
                context.RegisterCodeFix(CodeAction.Create(title, ct => UseCastInsteadOfSelect(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeLinqUsageData.UseOrder:
                if (nodeToFix is not InvocationExpressionSyntax orderByInvocation || GetMethodNameSyntax(orderByInvocation) is not { } orderByMethodName)
                    return;

                var orderBySemanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                if (orderBySemanticModel?.GetOperation(orderByInvocation, context.CancellationToken) is not IInvocationOperation orderByOperation)
                    return;

                // The key selector is the first argument of 'items.OrderBy(x => x)', but the second one of 'Enumerable.OrderBy(items, x => x)'
                if (orderByOperation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 1)?.Syntax is not ArgumentSyntax keySelectorArgument)
                    return;

                context.RegisterCodeFix(CodeAction.Create(title, ct => UseOrderInsteadOfOrderBy(context.Document, orderByMethodName, keySelectorArgument, ct), equivalenceKey: title), context.Diagnostics);
                break;
        }
    }

    private static bool TryGetFirstOperationSpan(Diagnostic diagnostic, out TextSpan span)
        => TryGetSpan(diagnostic, OptimizeLinqUsageAnalyzerCommon.FirstOperationStartKey, OptimizeLinqUsageAnalyzerCommon.FirstOperationLengthKey, out span);

    private static bool TryGetLastOperationSpan(Diagnostic diagnostic, out TextSpan span)
        => TryGetSpan(diagnostic, OptimizeLinqUsageAnalyzerCommon.LastOperationStartKey, OptimizeLinqUsageAnalyzerCommon.LastOperationLengthKey, out span);

    private static bool TryGetCountOperationSpan(Diagnostic diagnostic, out TextSpan span)
        => TryGetSpan(diagnostic, OptimizeLinqUsageAnalyzerCommon.CountOperationStartKey, OptimizeLinqUsageAnalyzerCommon.CountOperationLengthKey, out span);

    private static bool TryGetOperandOperationSpan(Diagnostic diagnostic, out TextSpan span)
        => TryGetSpan(diagnostic, OptimizeLinqUsageAnalyzerCommon.OperandOperationStartKey, OptimizeLinqUsageAnalyzerCommon.OperandOperationLengthKey, out span);

    private static bool TryGetSpan(Diagnostic diagnostic, string startKey, string lengthKey, out TextSpan span)
    {
        if (diagnostic.Properties.TryGetValue(startKey, out var startValue) && int.TryParse(startValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var start) &&
            diagnostic.Properties.TryGetValue(lengthKey, out var lengthValue) && int.TryParse(lengthValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var length))
        {
            span = new TextSpan(start, length);
            return true;
        }

        span = default;
        return false;
    }

    private static bool TryGetInvocationExpressionAncestor(ref SyntaxNode nodeToFix)
    {
        var node = nodeToFix;
        while (node is not null)
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

    private static async Task<Document> UseAny(Document document, TextSpan countOperationSpan, SyntaxNode nodeToFix, bool constantValue, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var countNode = root?.FindNode(countOperationSpan, getInnermostNodeForTie: true);
        if (countNode is null)
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

    private static async Task<Document> UseCountGreaterThanZero(Document document, SyntaxNode nodeToFix, ExpressionSyntax countExpression, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        // The invocation may be the operand of an operator or the target of a member access, both of which bind
        // tighter than '!=', so the comparison must be parenthesized. Simplifier removes the useless parentheses.
        var newExpression = generator.ValueNotEqualsExpression(countExpression, generator.LiteralExpression(0))
            .Parenthesize()
            .WithTrailingTrivia(nodeToFix.GetTrailingTrivia());

        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> UseTakeAndCount(Document document, TextSpan countOperationSpan, TextSpan operandOperationSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var countNode = root?.FindNode(countOperationSpan, getInnermostNodeForTie: true);
        var operandNode = root?.FindNode(operandOperationSpan, getInnermostNodeForTie: true);
        if (countNode is null || operandNode is null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        var operandOperation = semanticModel?.GetOperation(operandNode, cancellationToken);
        if (semanticModel?.GetOperation(countNode, cancellationToken) is not IInvocationOperation countOperation || operandOperation is null)
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

    private static async Task<Document> UseSkipAndAny(Document document, TextSpan countOperationSpan, TextSpan operandOperationSpan, bool skipMinusOne, SyntaxNode nodeToFix, bool comparandValue, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var countNode = root?.FindNode(countOperationSpan, getInnermostNodeForTie: true);
        var operandNode = root?.FindNode(operandOperationSpan, getInnermostNodeForTie: true);
        if (countNode is null || operandNode is null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        var operandOperation = semanticModel.GetOperation(operandNode, cancellationToken);
        if (semanticModel.GetOperation(countNode, cancellationToken) is not IInvocationOperation countOperation || operandOperation is null)
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

    private static async Task<Document> UseCastInsteadOfSelect(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        if (nodeToFix is not InvocationExpressionSyntax selectInvocationExpression)
            return document;

        if (selectInvocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        if (editor.SemanticModel.GetOperation(selectInvocationExpression, cancellationToken) is not IInvocationOperation operation)
            return document;

        var type = operation.TargetMethod.TypeArguments[1];
        var typeSyntax = (TypeSyntax)generator.TypeExpression(type);

        var castNameSyntax = GenericName(Identifier("Cast"))
            .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(typeSyntax)));

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

    private static async Task<Document> UseOrderInsteadOfOrderBy(Document document, SimpleNameSyntax methodName, ArgumentSyntax keySelectorArgument, CancellationToken cancellationToken)
    {
        var newName = methodName.Identifier.ValueText is "OrderBy" ? "Order" : "OrderDescending";

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.RemoveNode(keySelectorArgument);
        editor.ReplaceNode(methodName, IdentifierName(newName).WithTriviaFrom(methodName));
        return editor.GetChangedDocument();
    }

    private static async Task<Document> ReplaceNode(Document document, SyntaxNode nodeToReplace, SyntaxNode newNode, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(nodeToReplace, newNode);
        return editor.GetChangedDocument();
    }

    /// <summary>
    /// Creates the access to the property replacing a method without argument, such as 'items.Count' for 'items.Count()',
    /// 'Enumerable.Count(items)', or 'items?.Count()'. The trivia of the invocation are not copied.
    /// </summary>
    private static ExpressionSyntax? CreatePropertyAccess(SyntaxGenerator generator, SyntaxNode nodeToFix, string propertyName, bool allowConditionalAccess)
    {
        if (nodeToFix is not InvocationExpressionSyntax invocation)
            return null;

        return invocation switch
        {
            // Enumerable.Count(items)
            { ArgumentList.Arguments: [var source] } => (ExpressionSyntax)generator.MemberAccessExpression(source.Expression, propertyName),

            // items.Count()
            { ArgumentList.Arguments: [], Expression: MemberAccessExpressionSyntax memberAccess } => memberAccess.WithName(IdentifierName(propertyName).WithTriviaFrom(memberAccess.Name)),

            // items?.Count()
            { ArgumentList.Arguments: [], Expression: MemberBindingExpressionSyntax memberBinding } when allowConditionalAccess => memberBinding.WithName(IdentifierName(propertyName).WithTriviaFrom(memberBinding.Name)),

            _ => null,
        };
    }

    private static SimpleNameSyntax? GetMethodNameSyntax(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
            SimpleNameSyntax name => name,
            _ => null,
        };
    }

    /// <summary>
    /// Returns 'x', or 'x1', 'x2', ... when a symbol named 'x' is in scope, as a lambda parameter could hide it or conflict with it.
    /// </summary>
    private static string? GetUniqueParameterName(SemanticModel semanticModel, int position, Func<string, bool> isValid)
    {
        for (var i = 0; i < 1000; i++)
        {
            var name = i == 0 ? "x" : "x" + i.ToString(CultureInfo.InvariantCulture);
            if (semanticModel.LookupSymbols(position, name: name).IsEmpty && isValid(name))
                return name;
        }

        return null;
    }

    private static async Task<Document> UseListMethod(Document document, SyntaxNode nodeToFix, string methodName, bool convertPredicate, CancellationToken cancellationToken)
    {
        var expression = GetMemberAccessExpression(nodeToFix);
        if (expression is null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var newExpression = expression.WithName(IdentifierName(methodName));
        editor.ReplaceNode(expression, newExpression);
        if (convertPredicate)
        {
            var compilation = editor.SemanticModel.Compilation;
            if (editor.SemanticModel.GetSymbolInfo(nodeToFix, cancellationToken: cancellationToken).Symbol is not IMethodSymbol symbol || symbol.TypeArguments.Length != 1)
                return document;

            var type = symbol.TypeArguments[0];
            if (type is not null)
            {
                var predicateType = compilation.GetTypeByMetadataName("System.Predicate`1")?.Construct(type);
                if (predicateType is not null)
                {
                    var predicate = ((InvocationExpressionSyntax)nodeToFix).ArgumentList.Arguments.Last().Expression;
                    if (predicate is not null)
                    {
                        var newObject = editor.Generator.ObjectCreationExpression(predicateType, predicate);
                        editor.ReplaceNode(predicate, newObject);
                    }
                }
            }
        }

        return editor.GetChangedDocument();
    }

    private static async Task<Document> UseIndexer(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var expression = GetParentMemberExpression(nodeToFix);
        if (expression is null)
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

    private static async Task<Document> UseIndexerFirst(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var expression = GetParentMemberExpression(nodeToFix);
        if (expression is null)
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

    private static async Task<bool> CanUseIndexerLast(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return false;

        if (CanUseIndexFromEnd(nodeToFix, semanticModel.Compilation))
            return true;

        // 'source[source.Count - 1]' evaluates the source twice, while 'source.Last()' evaluates it once
        return semanticModel.GetOperation(nodeToFix, cancellationToken) is IInvocationOperation { Arguments: [var source] }
            && CanBeEvaluatedTwice(source.Value.UnwrapImplicitConversions());
    }

    private static bool CanUseIndexFromEnd(SyntaxNode node, Compilation compilation)
    {
        return node.SyntaxTree.GetCSharpLanguageVersion() >= LanguageVersion.CSharp8 && compilation.GetBestTypeByMetadataName("System.Index") is not null;
    }

    private static bool CanBeEvaluatedTwice(IOperation operation)
    {
        return operation switch
        {
            ILocalReferenceOperation or IParameterReferenceOperation => true,
            IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance } => true,
            IFieldReferenceOperation { Instance: var instance } => instance is null || CanBeEvaluatedTwice(instance),

            // The properties, indexers, and methods can execute any code, such as modifying a state or creating a new collection
            _ => false,
        };
    }

    private static async Task<Document> UseIndexerLast(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var expression = GetParentMemberExpression(nodeToFix);
        if (expression is null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var semanticModel = editor.SemanticModel;
        if (semanticModel.GetOperation(nodeToFix, cancellationToken) is not IInvocationOperation operation)
            return document;

        // if C# 8.0, use ^1
        if (CanUseIndexFromEnd(expression, semanticModel.Compilation))
        {
            var newExpression = generator.ElementAccessExpression(operation.Arguments[0].Syntax, PrefixUnaryExpression(SyntaxKind.IndexExpression, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1))));
            editor.ReplaceNode(nodeToFix, newExpression);
        }
        else
        {
            var newExpression = generator.ElementAccessExpression(operation.Arguments[0].Syntax,
                generator.SubtractExpression(
                    generator.MemberAccessExpression(operation.Arguments[0].Syntax, GetMemberName()),
                    generator.LiteralExpression(1)));
            editor.ReplaceNode(nodeToFix, newExpression);
        }

        return editor.GetChangedDocument();

        string GetMemberName()
        {
            var type = operation.Arguments[0].Value.GetActualType();
            var isArray = type is not null && type.TypeKind == TypeKind.Array;
            if (isArray)
                return "Length";

            return "Count";
        }
    }

    private static async Task<Document> RemoveDuplicatedOrderBy(Document document, TextSpan firstOperationSpan, TextSpan lastOperationSpan, CancellationToken cancellationToken)
    {
        // a."b()".c()
        // a.c()
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var firstNode = root?.FindNode(firstOperationSpan, getInnermostNodeForTie: true);
        var lastNode = root?.FindNode(lastOperationSpan, getInnermostNodeForTie: true);
        if (firstNode is null || lastNode is null)
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

    private static async Task<Document> UseThenBy(Document document, InvocationExpressionSyntax invocation, SimpleNameSyntax methodName, string expectedMethodName, string? keySelectorParameterName, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var newInvocation = invocation.ReplaceNode(methodName, IdentifierName(expectedMethodName).WithTriviaFrom(methodName));
        if (keySelectorParameterName is not null)
        {
            var keySelector = (ExpressionSyntax)generator.ValueReturningLambdaExpression(keySelectorParameterName, generator.IdentifierName(keySelectorParameterName));
            newInvocation = newInvocation.AddArgumentListArguments(Argument(keySelector));
        }

        editor.ReplaceNode(invocation, newInvocation);
        return editor.GetChangedDocument();
    }

    private static SyntaxNode? CombineWhereWithNextMethod(SyntaxGenerator generator, SemanticModel semanticModel, IInvocationOperation whereOperation, IInvocationOperation nextOperation, CancellationToken cancellationToken)
    {
        // enumerable.Where(x => x).C() => enumerable.C(x => x)
        // enumerable.Where(x => x).C(y => y) => enumerable.C(x => x && x)
        // enumerable.Where(Condition).C(y => y) => enumerable.C(y => Condition(y) && y)
        // enumerable.Where(condition1).C(condition2) => enumerable.C(x => condition1(x) && condition2(x))
        if (whereOperation.Arguments.Length != 2)
            return null;

        var method = generator.MemberAccessExpression(GetSourceSyntax(whereOperation), nextOperation.TargetMethod.Name);
        var whereArgument = whereOperation.Arguments[1];
        if (nextOperation.Arguments.Length == 1)
            return generator.InvocationExpression(method, whereArgument.Syntax);

        if (nextOperation.Arguments.Length != 2)
            return null;

        var wherePredicate = GetCombinablePredicate(whereArgument.Value);
        var nextPredicate = GetCombinablePredicate(nextOperation.Arguments[1].Value);
        if (wherePredicate is null || nextPredicate is null)
            return null;

        var parameterName = GetCombinedParameterName(semanticModel, nextOperation.Syntax.SpanStart, wherePredicate, nextPredicate, cancellationToken);
        if (parameterName is null)
            return null;

        var left = CreatePredicateBody(generator, wherePredicate, parameterName);
        var right = CreatePredicateBody(generator, nextPredicate, parameterName);
        return generator.InvocationExpression(method, generator.ValueReturningLambdaExpression(parameterName, generator.LogicalAndExpression(left, right)));

        static SyntaxNode GetSourceSyntax(IInvocationOperation operation)
        {
            // 'Enumerable.Where(items, predicate)' uses an argument, while 'items.Where(predicate)' uses the expression
            var syntax = operation.Arguments[0].Syntax;
            return syntax is ArgumentSyntax argument ? argument.Expression : syntax;
        }

        static CombinablePredicate? GetCombinablePredicate(IOperation operation)
        {
            // Queryable methods convert the lambda to an Expression<Func<...>>
            operation = operation.UnwrapImplicitConversions();

            var lambda = operation switch
            {
                IDelegateCreationOperation { Target: IAnonymousFunctionOperation anonymousFunction } => anonymousFunction,
                IAnonymousFunctionOperation anonymousFunction => anonymousFunction,
                _ => null,
            };

            if (lambda is not null)
            {
                // The body of 'x => { return x > 0; }' or 'delegate (int x) { return x > 0; }' cannot be combined with another condition
                if (lambda.Symbol.Parameters.Length != 1 || lambda.Syntax is not LambdaExpressionSyntax { ExpressionBody: { } body })
                    return null;

                return new CombinablePredicate(lambda, body);
            }

            if (operation.Syntax is not ExpressionSyntax expression)
                return null;

            // 'Filter' => 'Filter(x)'. The instance of the method is evaluated for each element instead of once.
            if (operation is IDelegateCreationOperation { Target: IMethodReferenceOperation methodReference })
            {
                if (methodReference.Instance is not null && !CanBeEvaluatedTwice(methodReference.Instance))
                    return null;

                return new CombinablePredicate(Lambda: null, expression);
            }

            // 'predicate' => 'predicate(x)'. The delegate is evaluated for each element instead of once.
            if (operation.Type is { TypeKind: TypeKind.Delegate } && CanBeEvaluatedTwice(operation))
                return new CombinablePredicate(Lambda: null, expression);

            return null;
        }

        static string? GetCombinedParameterName(SemanticModel semanticModel, int position, CombinablePredicate predicate1, CombinablePredicate predicate2, CancellationToken cancellationToken)
        {
            // Prefer the names chosen by the user. They are already valid at this location, as the lambdas are declared in the same scope.
            foreach (var predicate in new[] { predicate1, predicate2 })
            {
                var name = predicate.Lambda?.Symbol.Parameters[0].Name;
                if (name is not null and not "_" && IsValid(name))
                    return name;
            }

            return GetUniqueParameterName(semanticModel, position, IsValid);

            bool IsValid(string name) => !UsesName(predicate1, name) && !UsesName(predicate2, name);

            // The new parameter must not hide a symbol used by one of the predicates, nor conflict with a local declared in one of them
            bool UsesName(CombinablePredicate predicate, string name)
            {
                foreach (var token in predicate.Syntax.DescendantTokens())
                {
                    if (!token.IsKind(SyntaxKind.IdentifierToken) || token.ValueText != name)
                        continue;

                    // The references to the parameter of the lambda are renamed
                    if (predicate.Lambda is not null && token.Parent is IdentifierNameSyntax identifierName && semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol.IsEqualTo(predicate.Lambda.Symbol.Parameters[0]))
                        continue;

                    return true;
                }

                return false;
            }
        }

        static SyntaxNode CreatePredicateBody(SyntaxGenerator generator, CombinablePredicate predicate, string parameterName)
        {
            if (predicate.Lambda is not null)
                return ReplaceParameter(predicate.Lambda, predicate.Syntax, parameterName);

            return generator.InvocationExpression(predicate.Syntax, generator.IdentifierName(parameterName));
        }
    }

    private static async Task<Document> ReorderWhereBeforeOrderBy(Document document, TextSpan firstOperationSpan, TextSpan lastOperationSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var firstNode = root?.FindNode(firstOperationSpan, getInnermostNodeForTie: true);
        var lastNode = root?.FindNode(lastOperationSpan, getInnermostNodeForTie: true);
        if (firstNode is null || lastNode is null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        if (semanticModel?.GetOperation(firstNode, cancellationToken) is not IInvocationOperation firstOperation || semanticModel?.GetOperation(lastNode, cancellationToken) is not IInvocationOperation lastOperation)
            return document;

        var generator = editor.Generator;
        var source = firstOperation.Arguments[0].Syntax;

        var whereMethod = generator.MemberAccessExpression(source, lastOperation.TargetMethod.Name);
        var whereInvocation = generator.InvocationExpression(whereMethod, lastOperation.Arguments.Skip(1).Select(arg => arg.Syntax));

        var orderMethod = generator.MemberAccessExpression(whereInvocation, firstOperation.TargetMethod.Name);
        var reorderedInvocation = generator.InvocationExpression(orderMethod, firstOperation.Arguments.Skip(1).Select(arg => arg.Syntax));

        editor.ReplaceNode(lastOperation.Syntax, reorderedInvocation);
        return editor.GetChangedDocument();
    }

    private static SyntaxNode ReplaceParameter(IAnonymousFunctionOperation method, ExpressionSyntax body, string newParameterName)
    {
        var semanticModel = method.SemanticModel!;
        var parameterSymbol = method.Symbol.Parameters[0];
        if (parameterSymbol.Name == newParameterName)
            return body;

        return new ParameterRewriter(semanticModel, parameterSymbol, newParameterName).Visit(body);
    }

    private static MemberAccessExpressionSyntax? GetMemberAccessExpression(SyntaxNode invocationExpressionSyntax)
    {
        if (invocationExpressionSyntax is not InvocationExpressionSyntax invocationExpression)
            return null;

        return invocationExpression.Expression as MemberAccessExpressionSyntax;
    }

    private static ExpressionSyntax? GetParentMemberExpression(SyntaxNode invocationExpressionSyntax)
    {
        var memberAccessExpression = GetMemberAccessExpression(invocationExpressionSyntax);
        if (memberAccessExpression is null)
            return null;

        return memberAccessExpression.Expression;
    }

    /// <summary>
    /// A predicate that can be inlined in a lambda: the expression body of a lambda, or a delegate to invoke.
    /// </summary>
    private sealed record CombinablePredicate(IAnonymousFunctionOperation? Lambda, ExpressionSyntax Syntax);

    private sealed class ParameterRewriter(SemanticModel semanticModel, IParameterSymbol parameterSymbol, string newParameterName) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            var symbol = semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol is not null && symbol.IsEqualTo(parameterSymbol))
            {
                return IdentifierName(newParameterName).WithTriviaFrom(node);
            }

            return base.VisitIdentifierName(node);
        }
    }
}
