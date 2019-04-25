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
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class OptimizeLinqUsageFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods,
            RuleIdentifiers.DuplicateEnumerable_OrderBy,
            RuleIdentifiers.OptimizeLinqUsage,
            RuleIdentifiers.OptimizeEnumerable_Count);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic == null)
                return;

            if (!Enum.TryParse(diagnostic.Properties.GetValueOrDefault("Data", ""), out OptimizeLinqUsageData data) || data == OptimizeLinqUsageData.None)
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
                    context.RegisterCodeFix(CodeAction.Create(title, ct => UseConstantValue(context.Document, nodeToFix, true, ct), equivalenceKey: title), context.Diagnostics);
                    break;

                case OptimizeLinqUsageData.UseFalse:
                    context.RegisterCodeFix(CodeAction.Create(title, ct => UseConstantValue(context.Document, nodeToFix, false, ct), equivalenceKey: title), context.Diagnostics);
                    break;
            }
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

            var newExpression = expression.WithName(SyntaxFactory.IdentifierName("Find"));

            editor.ReplaceNode(expression, newExpression);
            return editor.GetChangedDocument();
        }

        private async static Task<Document> UseIndexer(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var expression = GetParentMemberExpression(nodeToFix);
            if (expression == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var operation = semanticModel.GetOperation(nodeToFix, cancellationToken) as IInvocationOperation;
            if (operation == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var newExpression = generator.ElementAccessExpression(operation.Arguments[0].Syntax, operation.Arguments[1].Syntax);

            editor.ReplaceNode(nodeToFix, newExpression);
            return editor.GetChangedDocument();
        }

        private async static Task<Document> UseIndexerFirst(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var expression = GetParentMemberExpression(nodeToFix);
            if (expression == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var operation = semanticModel.GetOperation(nodeToFix, cancellationToken) as IInvocationOperation;
            if (operation == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var newExpression = generator.ElementAccessExpression(operation.Arguments[0].Syntax, generator.LiteralExpression(0));

            editor.ReplaceNode(nodeToFix, newExpression);
            return editor.GetChangedDocument();
        }

        private async static Task<Document> UseIndexerLast(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var expression = GetParentMemberExpression(nodeToFix);
            if (expression == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var operation = semanticModel.GetOperation(nodeToFix, cancellationToken) as IInvocationOperation;
            if (operation == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

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
            var firstOperationStart = int.Parse(diagnostic.Properties.GetValueOrDefault("FirstOperationStart"), NumberStyles.Integer, CultureInfo.InvariantCulture);
            var firstOperationLength = int.Parse(diagnostic.Properties.GetValueOrDefault("FirstOperationLength"), NumberStyles.Integer, CultureInfo.InvariantCulture);
            var lastOperationStart = int.Parse(diagnostic.Properties.GetValueOrDefault("LastOperationStart"), NumberStyles.Integer, CultureInfo.InvariantCulture);
            var lastOperationLength = int.Parse(diagnostic.Properties.GetValueOrDefault("LastOperationLength"), NumberStyles.Integer, CultureInfo.InvariantCulture);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var firstNode = root.FindNode(new TextSpan(firstOperationStart, firstOperationLength), getInnermostNodeForTie: true);
            var lastNode = root.FindNode(new TextSpan(lastOperationStart, lastOperationLength), getInnermostNodeForTie: true);
            if (firstNode == null || lastNode == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var firstOperation = semanticModel.GetOperation(firstNode, cancellationToken) as IInvocationOperation;
            var lastOperation = semanticModel.GetOperation(lastNode, cancellationToken) as IInvocationOperation;
            if (firstOperation == null || lastOperation == null)
                return document;

            var method = editor.Generator.MemberAccessExpression(firstOperation.Arguments[0].Syntax, lastOperation.TargetMethod.Name);
            var newExpression = editor.Generator.InvocationExpression(method, lastOperation.Arguments.Skip(1).Select(arg => arg.Syntax));

            editor.ReplaceNode(lastOperation.Syntax, newExpression);
            return editor.GetChangedDocument();
        }

        private static async Task<Document> UseThenBy(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var lastOperationStart = int.Parse(diagnostic.Properties.GetValueOrDefault("LastOperationStart"), NumberStyles.Integer, CultureInfo.InvariantCulture);
            var lastOperationLength = int.Parse(diagnostic.Properties.GetValueOrDefault("LastOperationLength"), NumberStyles.Integer, CultureInfo.InvariantCulture);
            var expectedMethodName = diagnostic.Properties["ExpectedMethodName"];

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeToFix = root.FindNode(new TextSpan(lastOperationStart, lastOperationLength), getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return document;

            var expression = GetMemberAccessExpression(nodeToFix);
            if (expression == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var newExpression = expression.WithName(SyntaxFactory.IdentifierName(expectedMethodName));

            editor.ReplaceNode(expression, newExpression);
            return editor.GetChangedDocument();
        }

        private static async Task<Document> CombineWhereWithNextMethod(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            // enumerable.Where(x=> x).C() => enumerable.C(x=> x)
            // enumerable.Where(x=> x).C(y=>y) => enumerable.C(y=> y && y)
            // enumerable.Where(Condition).C(y=>y) => enumerable.C(y=> Condition(y) && y)
            var firstOperationStart = int.Parse(diagnostic.Properties.GetValueOrDefault("FirstOperationStart"), NumberStyles.Integer, CultureInfo.InvariantCulture);
            var firstOperationLength = int.Parse(diagnostic.Properties.GetValueOrDefault("FirstOperationLength"), NumberStyles.Integer, CultureInfo.InvariantCulture);
            var lastOperationStart = int.Parse(diagnostic.Properties.GetValueOrDefault("LastOperationStart"), NumberStyles.Integer, CultureInfo.InvariantCulture);
            var lastOperationLength = int.Parse(diagnostic.Properties.GetValueOrDefault("LastOperationLength"), NumberStyles.Integer, CultureInfo.InvariantCulture);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var firstNode = root.FindNode(new TextSpan(firstOperationStart, firstOperationLength), getInnermostNodeForTie: true);
            var lastNode = root.FindNode(new TextSpan(lastOperationStart, lastOperationLength), getInnermostNodeForTie: true);
            if (firstNode == null || lastNode == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var firstOperation = semanticModel.GetOperation(firstNode, cancellationToken) as IInvocationOperation;
            var lastOperation = semanticModel.GetOperation(lastNode, cancellationToken) as IInvocationOperation;
            if (firstOperation == null || lastOperation == null)
                return document;

            var generator = editor.Generator;
            var method = generator.MemberAccessExpression(firstOperation.Arguments[0].Syntax, lastOperation.TargetMethod.Name);
            var argument = CombineArguments(firstOperation.Arguments.ElementAtOrDefault(1), lastOperation.Arguments.ElementAtOrDefault(1));
            var newExpression = generator.InvocationExpression(method, argument);

            editor.ReplaceNode(lastOperation.Syntax, newExpression);
            return editor.GetChangedDocument();

            SyntaxNode CombineArguments(IArgumentOperation argument1, IArgumentOperation argument2)
            {
                if (argument2 == null)
                    return argument1?.Syntax;

                if (argument1 == null)
                    return argument2?.Syntax;

                var value1 = argument1.Value as IDelegateCreationOperation;
                var value2 = argument2.Value as IDelegateCreationOperation;

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
                var anonymousMethod = delegateCreationOperation.Target as IAnonymousFunctionOperation;
                if (anonymousMethod != null)
                {
                    return ReplaceParameter(anonymousMethod, parameterName);
                }

                var method = delegateCreationOperation.Target as IMethodReferenceOperation;
                if (method != null)
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
            var semanticModel = method.SemanticModel;
            var parameterSymbol = method.Symbol.Parameters[0];
            return new ParameterRewriter(semanticModel, parameterSymbol, newParameterName).Visit(method.Body.Syntax);
        }

        private static MemberAccessExpressionSyntax GetMemberAccessExpression(SyntaxNode invocationExpressionSyntax)
        {
            var invocationExpression = invocationExpressionSyntax as InvocationExpressionSyntax;
            if (invocationExpression == null)
                return null;

            return invocationExpression.Expression as MemberAccessExpressionSyntax;
        }

        private static SyntaxNode GetParentMemberExpression(SyntaxNode invocationExpressionSyntax)
        {
            var memberAccessExpression = GetMemberAccessExpression(invocationExpressionSyntax);
            if (memberAccessExpression == null)
                return null;

            return memberAccessExpression.Expression;
        }

        private class ParameterRewriter : CSharpSyntaxRewriter
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

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                var symbol = _semanticModel.GetSymbolInfo(node).Symbol;
                if (symbol != null && symbol.Equals(_parameterSymbol))
                {
                    return SyntaxFactory.IdentifierName(_newParameterName);
                }

                return base.VisitIdentifierName(node);
            }
        }
    }
}
