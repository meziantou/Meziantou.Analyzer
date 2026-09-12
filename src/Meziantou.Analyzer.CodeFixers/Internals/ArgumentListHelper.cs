namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Creates the argument lists of the code fixes that add an argument to an invocation.
/// </summary>
internal static class ArgumentListHelper
{
    /// <summary>
    /// Creates a copy of <paramref name="invocationExpression"/> with an additional argument bound to the parameter
    /// named <paramref name="parameterName"/>, declared at the index <paramref name="parameterIndex"/> of the invoked
    /// overload, or <see langword="null"/> when the argument cannot be added without changing how the invocation binds.
    /// </summary>
    /// <param name="parameterIndex">The index of the parameter in the overload the invocation must be bound to after the fix.</param>
    /// <param name="parameterName">The name of that parameter.</param>
    /// <param name="argumentExpression">The expression of the argument to add.</param>
    /// <param name="isExpectedParameter">Validates the parameter the new argument is bound to.</param>
    /// <param name="overload">The overload the invocation must be bound to after the fix, when it is known. It is used to rename the existing named arguments when the overload declares the parameters with other names.</param>
    public static InvocationExpressionSyntax? AddArgument(
        SemanticModel semanticModel,
        SyntaxGenerator generator,
        InvocationExpressionSyntax invocationExpression,
        int parameterIndex,
        string parameterName,
        SyntaxNode argumentExpression,
        Func<IParameterSymbol, bool> isExpectedParameter,
        IMethodSymbol? overload = null,
        CancellationToken cancellationToken = default)
    {
        if (parameterIndex < 0)
            return null;

        var arguments = invocationExpression.ArgumentList.Arguments;

        // A positional argument can only be added at the index of the parameter when all the arguments written before it are positional.
        // Otherwise, C# does not allow it (CS1738, CS1739, CS8323) or it would be bound to another parameter.
        if (parameterIndex <= arguments.Count && !arguments.Take(parameterIndex).Any(argument => argument.NameColon is not null))
        {
            var positionalArgument = (ArgumentSyntax)generator.Argument(argumentExpression);
            var candidate = WithArguments(invocationExpression, arguments.Insert(parameterIndex, positionalArgument));
            if (GetBoundParameter(semanticModel, invocationExpression, candidate, parameterIndex) is { } parameter && isExpectedParameter(parameter))
                return candidate;
        }

        // The new parameter shifts the parameters declared after it, so the positional arguments written at their
        // positions would be bound to other parameters once the argument is added at the end of the list.
        for (var i = parameterIndex; i < arguments.Count; i++)
        {
            if (arguments[i].NameColon is null)
                return null;
        }

        var namedArgument = (ArgumentSyntax)generator.Argument(parameterName, RefKind.None, argumentExpression);

        // A named argument added at the end of the list is valid whatever the order of the existing arguments, and keeps their evaluation order
        var namedCandidate = WithArguments(invocationExpression, arguments.Add(namedArgument));
        if (GetBoundParameter(semanticModel, invocationExpression, namedCandidate, parameterName) is { } namedParameter && isExpectedParameter(namedParameter))
            return namedCandidate;

        // The overload can declare the parameters with other names, in which case the existing named arguments must be renamed
        if (overload is not null && RenameArguments(semanticModel, invocationExpression, parameterIndex, overload, cancellationToken) is { } renamedArguments)
        {
            var renamedCandidate = WithArguments(invocationExpression, renamedArguments.Add(namedArgument));
            if (GetBoundParameter(semanticModel, invocationExpression, renamedCandidate, parameterName) is { } renamedParameter && isExpectedParameter(renamedParameter))
                return renamedCandidate;
        }

        return null;
    }

    /// <summary>
    /// Creates a copy of <paramref name="invocationExpression"/> with the arguments <paramref name="arguments"/>.
    /// </summary>
    public static InvocationExpressionSyntax WithArguments(InvocationExpressionSyntax invocationExpression, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        return invocationExpression.WithArgumentList(invocationExpression.ArgumentList.WithArguments(arguments));
    }

    /// <summary>
    /// Gets the method <paramref name="newInvocation"/> would be bound to at the position of <paramref name="invocationExpression"/>,
    /// so a code fix is only offered when the new invocation compiles and its arguments are bound to the expected parameters.
    /// </summary>
    public static IMethodSymbol? GetTargetMethod(SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression, InvocationExpressionSyntax newInvocation)
    {
        return semanticModel.GetSpeculativeSymbolInfo(invocationExpression.SpanStart, newInvocation, SpeculativeBindingOption.BindAsExpression).Symbol as IMethodSymbol;
    }

    private static IParameterSymbol? GetBoundParameter(SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression, InvocationExpressionSyntax newInvocation, int parameterIndex)
    {
        var method = GetTargetMethod(semanticModel, invocationExpression, newInvocation);
        if (method is null || parameterIndex >= method.Parameters.Length)
            return null;

        return method.Parameters[parameterIndex];
    }

    private static IParameterSymbol? GetBoundParameter(SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression, InvocationExpressionSyntax newInvocation, string parameterName)
    {
        var method = GetTargetMethod(semanticModel, invocationExpression, newInvocation);
        return method?.Parameters.FirstOrDefault(parameter => string.Equals(parameter.Name, parameterName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Binds the arguments of <paramref name="invocationExpression"/> to the parameters of <paramref name="overload"/>,
    /// renaming the named arguments when the overload declares the parameters with other names, or returns
    /// <see langword="null"/> when the arguments cannot be reused.
    /// </summary>
    private static SeparatedSyntaxList<ArgumentSyntax>? RenameArguments(SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression, int parameterIndex, IMethodSymbol overload, CancellationToken cancellationToken)
    {
        if (semanticModel.GetOperation(invocationExpression, cancellationToken) is not IInvocationOperation invocationOperation)
            return null;

        var arguments = invocationExpression.ArgumentList.Arguments;
        var newArguments = arguments;
        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            if (argument.NameColon is null)
                continue;

            var parameter = GetParameter(invocationOperation, argument);
            if (parameter is null)
                return null;

            // The new parameter shifts the parameters declared after it
            var overloadParameterIndex = parameter.Ordinal < parameterIndex ? parameter.Ordinal : parameter.Ordinal + 1;
            if (overloadParameterIndex >= overload.Parameters.Length)
                return null;

            // The overload can declare the parameters in a different order, in which case the arguments cannot be reused
            var overloadParameter = overload.Parameters[overloadParameterIndex];
            if (!overloadParameter.Type.IsEqualTo(parameter.Type))
                return null;

            if (!string.Equals(argument.NameColon.Name.Identifier.ValueText, overloadParameter.Name, StringComparison.Ordinal))
            {
                var newArgument = argument.WithNameColon(argument.NameColon.WithName(SyntaxFactory.IdentifierName(overloadParameter.Name)));
                newArguments = newArguments.Replace(newArguments[i], newArgument);
            }
        }

        return newArguments;

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
}
