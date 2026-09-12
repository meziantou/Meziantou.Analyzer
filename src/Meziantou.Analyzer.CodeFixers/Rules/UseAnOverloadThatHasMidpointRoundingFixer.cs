namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseAnOverloadThatHasMidpointRoundingFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseAnOverloadThatHasMidpointRounding);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var invocationExpression = nodeToFix as InvocationExpressionSyntax ?? nodeToFix.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocationExpression is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (semanticModel.GetOperation(invocationExpression, context.CancellationToken) is not IInvocationOperation invocationOperation)
            return;

        var midpointRoundingSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.MidpointRounding");
        if (midpointRoundingSymbol is null)
            return;

        if (!TryGetFixInfo(semanticModel.Compilation, invocationOperation, invocationExpression, midpointRoundingSymbol, out var fixInfo))
            return;

        foreach (var midpointRoundingMember in midpointRoundingSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (midpointRoundingMember is { IsImplicitlyDeclared: true, Name: "value__" })
                continue;

            if (!midpointRoundingMember.HasConstantValue)
                continue;

            var midpointRoundingMemberName = midpointRoundingMember.Name;
            var title = "Add MidpointRounding." + midpointRoundingMemberName;
            var codeAction = CodeAction.Create(
                title,
                ct => AddMidpointRounding(context.Document, invocationExpression, fixInfo, midpointRoundingSymbol, midpointRoundingMemberName, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static bool TryGetFixInfo(Compilation compilation, IInvocationOperation invocationOperation, InvocationExpressionSyntax invocationExpression, INamedTypeSymbol midpointRoundingSymbol, [NotNullWhen(true)] out MidpointRoundingFixInfo? fixInfo)
    {
        fixInfo = null;

        var overloadFinder = new OverloadFinder(compilation);
        var overload = overloadFinder.FindOverloadWithAdditionalParameterOfType(invocationOperation, new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: true), [midpointRoundingSymbol]);
        if (overload is null)
            return false;

        var parameterIndex = -1;
        for (var i = 0; i < overload.Parameters.Length; i++)
        {
            if (overload.Parameters[i].Type.IsEqualTo(midpointRoundingSymbol))
            {
                parameterIndex = i;
                break;
            }
        }

        if (parameterIndex < 0)
            return false;

        var parameterName = overload.Parameters[parameterIndex].Name;
        var arguments = invocationExpression.ArgumentList.Arguments;

        // All the arguments are positional, so they keep their bindings and the new argument can be added at the
        // position of the parameter. When the position is after the last argument, some optional parameters are
        // omitted, so the new argument must be named.
        if (!arguments.Any(argument => argument.NameColon is not null))
        {
            fixInfo = new MidpointRoundingFixInfo(invocationExpression.ArgumentList, parameterIndex <= arguments.Count ? parameterIndex : null, parameterName);
            return true;
        }

        // Some arguments are named, so they may not be in the parameter order. A positional argument cannot be added
        // after them, and the parameters of the overload may not have the same names. The new argument is named and
        // appended, and the existing named arguments are bound to the parameters of the overload.
        var newArguments = arguments;
        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            var parameter = GetParameter(invocationOperation, argument);
            if (parameter is null)
                return false;

            var overloadParameterIndex = parameter.Ordinal < parameterIndex ? parameter.Ordinal : parameter.Ordinal + 1;
            if (overloadParameterIndex >= overload.Parameters.Length)
                return false;

            // The overload can declare the parameters in a different order, in which case the arguments cannot be reused
            var overloadParameter = overload.Parameters[overloadParameterIndex];
            if (!overloadParameter.Type.IsEqualTo(parameter.Type))
                return false;

            if (argument.NameColon is null)
            {
                // A positional argument binds to the parameter at the same position, so it must come before the new parameter
                if (i >= parameterIndex)
                    return false;
            }
            else if (!string.Equals(argument.NameColon.Name.Identifier.ValueText, overloadParameter.Name, StringComparison.Ordinal))
            {
                var newArgument = argument.WithNameColon(argument.NameColon.WithName(SyntaxFactory.IdentifierName(overloadParameter.Name)));
                newArguments = newArguments.Replace(newArguments[i], newArgument);
            }
        }

        fixInfo = new MidpointRoundingFixInfo(invocationExpression.ArgumentList.WithArguments(newArguments), ArgumentIndex: null, parameterName);
        return true;

        static IParameterSymbol? GetParameter(IInvocationOperation invocationOperation, ArgumentSyntax argument)
        {
            foreach (var argumentOperation in invocationOperation.Arguments)
            {
                if (argumentOperation.Syntax == argument)
                    return argumentOperation.Parameter;
            }

            return null;
        }
    }

    private static async Task<Document> AddMidpointRounding(Document document, InvocationExpressionSyntax invocationExpression, MidpointRoundingFixInfo fixInfo, INamedTypeSymbol midpointRoundingSymbol, string midpointRoundingMember, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var midpointRoundingExpression = generator.TypeMemberAccessExpression(midpointRoundingSymbol, midpointRoundingMember, addImport: true);

        var arguments = fixInfo.ArgumentList.Arguments;
        var newArguments = fixInfo.ArgumentIndex is int argumentIndex
            ? arguments.Insert(argumentIndex, (ArgumentSyntax)generator.Argument(midpointRoundingExpression))
            : arguments.Add((ArgumentSyntax)generator.Argument(fixInfo.ParameterName, RefKind.None, midpointRoundingExpression));

        var newInvocation = invocationExpression.WithArgumentList(fixInfo.ArgumentList.WithArguments(newArguments));

        editor.ReplaceNode(invocationExpression, newInvocation);
        return editor.GetChangedDocument();
    }

    /// <param name="ArgumentList">The arguments of the invocation, bound to the parameters of the overload.</param>
    /// <param name="ArgumentIndex">The index at which the positional argument must be added, or <see langword="null"/> when the argument must be named and appended.</param>
    /// <param name="ParameterName">The name of the <see cref="System.MidpointRounding"/> parameter of the overload.</param>
    private sealed record MidpointRoundingFixInfo(ArgumentListSyntax ArgumentList, int? ArgumentIndex, string ParameterName);
}
