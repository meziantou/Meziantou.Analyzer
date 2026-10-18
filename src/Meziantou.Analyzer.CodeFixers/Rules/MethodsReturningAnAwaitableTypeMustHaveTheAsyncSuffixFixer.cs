using Microsoft.CodeAnalysis.Rename;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffixFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            RuleIdentifiers.MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffix,
            RuleIdentifiers.MethodsNotReturningAnAwaitableTypeMustNotHaveTheAsyncSuffix,
            RuleIdentifiers.MethodsReturningIAsyncEnumerableMustHaveTheAsyncSuffix,
            RuleIdentifiers.MethodsNotReturningIAsyncEnumerableMustNotHaveTheAsyncSuffix);

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        IMethodSymbol? methodSymbol = null;

        // Try to get the method symbol: either from a method declaration or a local function
        var declarationNode = nodeToFix.AncestorsAndSelf().FirstOrDefault(n => n is MethodDeclarationSyntax or LocalFunctionStatementSyntax);

        // The renamer can throw when the declaration of the renamed method does not compile
        if (declarationNode is null || declarationNode.ContainsDiagnostics)
            return;

        if (declarationNode is MethodDeclarationSyntax methodDeclaration)
        {
            methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken) as IMethodSymbol;
        }
        else if (declarationNode is LocalFunctionStatementSyntax localFunctionStatement)
        {
            methodSymbol = semanticModel.GetDeclaredSymbol(localFunctionStatement, context.CancellationToken) as IMethodSymbol;
        }

        if (methodSymbol is null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            string newName;
            string title;
            if (diagnostic.Id is RuleIdentifiers.MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffix or RuleIdentifiers.MethodsReturningIAsyncEnumerableMustHaveTheAsyncSuffix)
            {
                newName = methodSymbol.Name + "Async";
                title = $"Rename to '{newName}'";
            }
            else
            {
                // A method named Async cannot be renamed to an empty name
                if (!methodSymbol.Name.EndsWith("Async", StringComparison.Ordinal) || methodSymbol.Name.Length == "Async".Length)
                    continue;

                newName = methodSymbol.Name[..^"Async".Length];
                title = $"Rename to '{newName}'";
            }

            if (IsNameAlreadyUsed(semanticModel, nodeToFix, methodSymbol, newName))
                continue;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => RenameMethodAsync(context.Document, methodSymbol, newName, ct),
                    equivalenceKey: title),
                diagnostic);
        }
    }

    /// <summary>
    /// Indicates whether renaming the method would conflict with a member of the type that has the same signature, or
    /// with a local or a local function in scope, as the code would not compile anymore.
    /// </summary>
    private static bool IsNameAlreadyUsed(SemanticModel semanticModel, SyntaxNode node, IMethodSymbol methodSymbol, string newName)
    {
        if (methodSymbol.MethodKind is MethodKind.LocalFunction)
        {
            foreach (var symbol in semanticModel.LookupSymbols(node.SpanStart, name: newName))
            {
                if (symbol is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol or IMethodSymbol { MethodKind: MethodKind.LocalFunction })
                    return true;
            }

            return false;
        }

        foreach (var member in methodSymbol.ContainingType.GetMembers(newName))
        {
            if (member is not IMethodSymbol otherMethod || HasSameSignature(methodSymbol, otherMethod))
                return true;
        }

        return false;
    }

    private static bool HasSameSignature(IMethodSymbol method, IMethodSymbol otherMethod)
    {
        if (method.Arity != otherMethod.Arity || method.Parameters.Length != otherMethod.Parameters.Length)
            return false;

        for (var i = 0; i < method.Parameters.Length; i++)
        {
            var parameter = method.Parameters[i];
            var otherParameter = otherMethod.Parameters[i];
            if (parameter.RefKind is RefKind.None != otherParameter.RefKind is RefKind.None)
                return false;

            // The type parameters of the methods are compared by position: Write<T>(T value) and Write<U>(U value) have the same signature
            if (parameter.Type is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } typeParameter && otherParameter.Type is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } otherTypeParameter)
            {
                if (typeParameter.Ordinal != otherTypeParameter.Ordinal)
                    return false;
            }
            else if (!parameter.Type.IsEqualTo(otherParameter.Type))
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<Solution> RenameMethodAsync(Document document, IMethodSymbol methodSymbol, string newName, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        return await Renamer.RenameSymbolAsync(solution, methodSymbol, new SymbolRenameOptions(), newName, cancellationToken).ConfigureAwait(false);
    }
}
