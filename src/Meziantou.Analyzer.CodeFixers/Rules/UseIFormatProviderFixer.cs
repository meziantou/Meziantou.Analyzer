namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseIFormatProviderFixer : CodeFixProvider
{
    private const string CurrentCultureExpression = "System.Globalization.CultureInfo.CurrentCulture";
    private const string InvariantCultureExpression = "System.Globalization.CultureInfo.InvariantCulture";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseIFormatProviderParameter);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        var invocationExpression = nodeToFix?.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocationExpression is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetOperation(invocationExpression, context.CancellationToken) is not IInvocationOperation invocationOperation)
            return;

        var formatProviderSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.IFormatProvider");
        var stringSymbol = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
        if (formatProviderSymbol is null || stringSymbol is null)
            return;

        var generator = SyntaxGenerator.GetGenerator(context.Document);
        var overloadFinder = new OverloadFinder(semanticModel.Compilation);
        var overload = overloadFinder.FindOverloadWithAdditionalParameterOfType(
            invocationOperation,
            new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: true),
            [new OverloadParameterType(formatProviderSymbol, AllowInherits: true)]);

        if (overload is not null && TryGetFormatProviderParameterInfo(invocationOperation.TargetMethod, overload, formatProviderSymbol, out var parameterIndex, out var parameterName))
        {
            var registered = RegisterCodeFix(InvariantCultureExpression, "Use CultureInfo.InvariantCulture");
            registered |= RegisterCodeFix(CurrentCultureExpression, "Use CultureInfo.CurrentCulture");
            if (registered)
                return;
        }

        if (invocationOperation.TargetMethod.Name == nameof(object.ToString) && invocationOperation.Arguments.IsEmpty)
        {
            overload = overloadFinder.FindOverloadWithAdditionalParameterOfType(
                invocationOperation,
                new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: false),
                [new OverloadParameterType(stringSymbol), new OverloadParameterType(formatProviderSymbol, AllowInherits: true)]);

            if (overload is not null && CanFixToStringOverload(overload, formatProviderSymbol))
            {
                RegisterToStringCodeFix(InvariantCultureExpression, "Use CultureInfo.InvariantCulture");
                RegisterToStringCodeFix(CurrentCultureExpression, "Use CultureInfo.CurrentCulture");
            }
        }

        bool RegisterCodeFix(string formatProviderExpression, string title)
        {
            var newInvocation = CreateInvocationWithFormatProvider(semanticModel, generator, invocationExpression, parameterIndex, parameterName, formatProviderExpression, formatProviderSymbol);
            if (newInvocation is null)
                return false;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => FixInvocation(context.Document, invocationExpression, newInvocation, ct),
                    equivalenceKey: title),
                context.Diagnostics);
            return true;
        }

        void RegisterToStringCodeFix(string formatProviderExpression, string title)
        {
            var newInvocation = CreateToStringInvocation(semanticModel, invocationExpression, overload!, formatProviderSymbol, formatProviderExpression);
            if (newInvocation is null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => FixInvocation(context.Document, invocationExpression, newInvocation, ct),
                    equivalenceKey: title),
                context.Diagnostics);
        }
    }

    private static InvocationExpressionSyntax? CreateInvocationWithFormatProvider(SemanticModel semanticModel, SyntaxGenerator generator, InvocationExpressionSyntax invocationExpression, int parameterIndex, string parameterName, string formatProviderExpression, ITypeSymbol formatProviderSymbol)
    {
        var arguments = invocationExpression.ArgumentList.Arguments;

        // A positional argument can only be added at the index of the parameter when all the arguments written before it are positional.
        // Otherwise, C# does not allow it (CS1738, CS1739) or it would be bound to another parameter.
        if (parameterIndex <= arguments.Count && !arguments.Take(parameterIndex).Any(argument => argument.NameColon is not null))
        {
            var positionalArgument = (ArgumentSyntax)generator.Argument(SyntaxFactory.ParseExpression(formatProviderExpression));
            var candidate = ReplaceArguments(invocationExpression, arguments.Insert(parameterIndex, positionalArgument));
            if (GetTargetMethod(semanticModel, invocationExpression, candidate) is { } method && parameterIndex < method.Parameters.Length && method.Parameters[parameterIndex].Type.IsOrInheritsFrom(formatProviderSymbol))
                return candidate;
        }

        // A named argument added at the end of the list is valid whatever the order of the existing arguments, and keeps their evaluation order
        var namedArgument = (ArgumentSyntax)generator.Argument(parameterName, RefKind.None, SyntaxFactory.ParseExpression(formatProviderExpression));
        var namedCandidate = ReplaceArguments(invocationExpression, arguments.Add(namedArgument));
        var namedParameter = GetTargetMethod(semanticModel, invocationExpression, namedCandidate)?.Parameters.FirstOrDefault(parameter => string.Equals(parameter.Name, parameterName, StringComparison.Ordinal));
        if (namedParameter is not null && namedParameter.Type.IsOrInheritsFrom(formatProviderSymbol))
            return namedCandidate;

        return null;
    }

    private static InvocationExpressionSyntax? CreateToStringInvocation(SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression, IMethodSymbol overload, ITypeSymbol formatProviderSymbol, string formatProviderExpression)
    {
        var arguments = new List<ArgumentSyntax>(capacity: overload.Parameters.Length);
        foreach (var parameter in overload.Parameters)
        {
            ExpressionSyntax expression;
            if (parameter.Type.IsString())
            {
                expression = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
            }
            else if (parameter.Type.IsOrInheritsFrom(formatProviderSymbol))
            {
                expression = SyntaxFactory.ParseExpression(formatProviderExpression);
            }
            else
            {
                return null;
            }

            arguments.Add(SyntaxFactory.Argument(expression));
        }

        var candidate = ReplaceArguments(invocationExpression, SyntaxFactory.SeparatedList(arguments));
        if (GetTargetMethod(semanticModel, invocationExpression, candidate) is { } method && method.Parameters.Any(parameter => parameter.Type.IsOrInheritsFrom(formatProviderSymbol)))
            return candidate;

        return null;
    }

    private static InvocationExpressionSyntax ReplaceArguments(InvocationExpressionSyntax invocationExpression, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        return invocationExpression.WithArgumentList(invocationExpression.ArgumentList.WithArguments(arguments));
    }

    /// <summary>
    /// Gets the method the invocation would be bound to, so the fix is only offered when the new invocation compiles
    /// and the new argument is bound to the expected parameter.
    /// </summary>
    private static IMethodSymbol? GetTargetMethod(SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression, InvocationExpressionSyntax newInvocation)
    {
        return semanticModel.GetSpeculativeSymbolInfo(invocationExpression.SpanStart, newInvocation, SpeculativeBindingOption.BindAsExpression).Symbol as IMethodSymbol;
    }

    private static async Task<Document> FixInvocation(Document document, InvocationExpressionSyntax invocationExpression, InvocationExpressionSyntax newInvocation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(invocationExpression, newInvocation);
        return editor.GetChangedDocument();
    }

    private static bool TryGetFormatProviderParameterInfo(IMethodSymbol method, IMethodSymbol overload, ITypeSymbol formatProviderSymbol, out int parameterIndex, out string parameterName)
    {
        for (var i = 0; i < overload.Parameters.Length; i++)
        {
            var parameter = overload.Parameters[i];
            if (!parameter.Type.IsOrInheritsFrom(formatProviderSymbol))
                continue;

            if (i >= method.Parameters.Length || !method.Parameters[i].Type.IsOrInheritsFrom(formatProviderSymbol))
            {
                parameterIndex = i;
                parameterName = parameter.Name;
                return true;
            }
        }

        parameterIndex = -1;
        parameterName = string.Empty;
        return false;
    }

    private static bool CanFixToStringOverload(IMethodSymbol overload, ITypeSymbol formatProviderSymbol)
    {
        if (overload.Parameters.Length != 2)
            return false;

        return overload.Parameters.Any(parameter => parameter.Type.IsString()) &&
               overload.Parameters.Any(parameter => parameter.Type.IsOrInheritsFrom(formatProviderSymbol)) &&
               overload.Parameters.All(parameter => parameter.Type.IsString() || parameter.Type.IsOrInheritsFrom(formatProviderSymbol));
    }
}
