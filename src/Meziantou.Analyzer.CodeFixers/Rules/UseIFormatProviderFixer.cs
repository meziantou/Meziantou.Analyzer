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

        var formatProviderSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.IFormatProvider");
        var cultureInfoSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.Globalization.CultureInfo");
        var stringSymbol = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
        if (formatProviderSymbol is null || cultureInfoSymbol is null || stringSymbol is null)
            return;

        var formatProviderType = new FormatProviderType(formatProviderSymbol, cultureInfoSymbol);

        // The fix only searches the extension methods declared in a namespace that is not imported when the analyzer selected one of them
        var namespaceToImport = context.Diagnostics[0].Properties.GetValueOrDefault(OverloadFinder.NamespaceToImportPropertyName);
        var generator = SyntaxGenerator.GetGenerator(context.Document);
        var overloadFinder = new OverloadFinder(semanticModel.Compilation);
        var overload = overloadFinder.FindOverloadWithAdditionalParameterOfType(
            invocationOperation,
            new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: true, IncludeExtensionMethodsFromNotImportedNamespaces: namespaceToImport is not null),
            [new OverloadParameterType(formatProviderSymbol, AllowInherits: true)]);

        if (overload is not null && TryGetFormatProviderParameterInfo(invocationOperation.TargetMethod, overload, formatProviderType, out var parameterIndex, out var parameterName))
        {
            var registered = GetInvariantMethod(invocationOperation.TargetMethod) is { } invariantMethod
                ? RegisterInvariantMethodCodeFix(invariantMethod)
                : RegisterCodeFix(InvariantCultureExpression, "Use CultureInfo.InvariantCulture");
            registered |= RegisterCodeFix(CurrentCultureExpression, "Use CultureInfo.CurrentCulture");
            if (registered)
                return;
        }

        if (invocationOperation.TargetMethod.Name == nameof(object.ToString) && invocationOperation.Arguments.IsEmpty)
        {
            overload = overloadFinder.FindOverloadWithAdditionalParameterOfType(
                invocationOperation,
                new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: false, IncludeExtensionMethodsFromNotImportedNamespaces: namespaceToImport is not null),
                [new OverloadParameterType(stringSymbol), new OverloadParameterType(formatProviderSymbol, AllowInherits: true)]);

            if (overload is not null && CanFixToStringOverload(overload, formatProviderType))
            {
                RegisterToStringCodeFix(InvariantCultureExpression, "Use CultureInfo.InvariantCulture");
                RegisterToStringCodeFix(CurrentCultureExpression, "Use CultureInfo.CurrentCulture");
            }
        }

        bool RegisterCodeFix(string formatProviderExpression, string title)
        {
            var newInvocation = ArgumentListHelper.AddArgument(
                semanticModel,
                generator,
                invocationExpression,
                parameterIndex,
                parameterName,
                SyntaxFactory.ParseExpression(formatProviderExpression),
                parameter => formatProviderType.Matches(parameter.Type),
                namespaceToImport: namespaceToImport,
                cancellationToken: context.CancellationToken);

            if (newInvocation is null)
                return false;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => FixInvocation(context.Document, invocationExpression, newInvocation, namespaceToImport, ct),
                    equivalenceKey: title),
                context.Diagnostics);
            return true;
        }

        bool RegisterInvariantMethodCodeFix(IMethodSymbol invariantMethod)
        {
            SimpleNameSyntax? methodName = invocationExpression.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
                IdentifierNameSyntax identifierName => identifierName,
                _ => null,
            };

            if (methodName is not IdentifierNameSyntax)
                return false;

            var newInvocation = invocationExpression.ReplaceNode(methodName, SyntaxFactory.IdentifierName(invariantMethod.Name).WithTriviaFrom(methodName));

            // Use the same equivalence key as CultureInfo.InvariantCulture, so fixing all the occurrences uses the invariant methods when available
            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Use {invariantMethod.Name}",
                    ct => FixInvocation(context.Document, invocationExpression, newInvocation, namespaceToImport: null, ct),
                    equivalenceKey: "Use CultureInfo.InvariantCulture"),
                context.Diagnostics);
            return true;
        }

        void RegisterToStringCodeFix(string formatProviderExpression, string title)
        {
            var newInvocation = CreateToStringInvocation(semanticModel, invocationExpression, overload!, formatProviderType, formatProviderExpression, namespaceToImport, context.CancellationToken);
            if (newInvocation is null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => FixInvocation(context.Document, invocationExpression, newInvocation, namespaceToImport, ct),
                    equivalenceKey: title),
                context.Diagnostics);
        }
    }

    private static InvocationExpressionSyntax? CreateToStringInvocation(SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression, IMethodSymbol overload, FormatProviderType formatProviderType, string formatProviderExpression, string? namespaceToImport, CancellationToken cancellationToken)
    {
        var arguments = new List<ArgumentSyntax>(capacity: overload.Parameters.Length);
        foreach (var parameter in overload.Parameters)
        {
            ExpressionSyntax expression;
            if (parameter.Type.IsString())
            {
                expression = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
            }
            else if (formatProviderType.Matches(parameter.Type))
            {
                expression = SyntaxFactory.ParseExpression(formatProviderExpression);
            }
            else
            {
                return null;
            }

            arguments.Add(SyntaxFactory.Argument(expression));
        }

        var candidate = ArgumentListHelper.WithArguments(invocationExpression, SyntaxFactory.SeparatedList(arguments));
        if (ArgumentListHelper.GetTargetMethod(semanticModel, invocationExpression, candidate, namespaceToImport, cancellationToken) is { } method && method.Parameters.Any(parameter => formatProviderType.Matches(parameter.Type)))
            return candidate;

        return null;
    }

    private static async Task<Document> FixInvocation(Document document, InvocationExpressionSyntax invocationExpression, InvocationExpressionSyntax newInvocation, string? namespaceToImport, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(invocationExpression, newInvocation);
        if (namespaceToImport is not null)
        {
            UsingDirectiveHelper.AddUsingDirective(editor, invocationExpression, namespaceToImport);
        }

        return editor.GetChangedDocument();
    }

    // string.ToLower() => string.ToLowerInvariant(), char.ToUpper(char) => char.ToUpperInvariant(char)
    private static IMethodSymbol? GetInvariantMethod(IMethodSymbol method)
    {
        if (method.ContainingType.SpecialType is not (SpecialType.System_String or SpecialType.System_Char) || method.Name is not ("ToLower" or "ToUpper"))
            return null;

        foreach (var member in method.ContainingType.GetMembers(method.Name + "Invariant"))
        {
            if (member is IMethodSymbol candidate &&
                candidate.IsStatic == method.IsStatic &&
                candidate.Parameters.Length == method.Parameters.Length &&
                candidate.Parameters.Zip(method.Parameters, (a, b) => a.Type.IsEqualTo(b.Type) && a.Name == b.Name).All(match => match))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool TryGetFormatProviderParameterInfo(IMethodSymbol method, IMethodSymbol overload, FormatProviderType formatProviderType, out int parameterIndex, out string parameterName)
    {
        for (var i = 0; i < overload.Parameters.Length; i++)
        {
            var parameter = overload.Parameters[i];
            if (!formatProviderType.Matches(parameter.Type))
                continue;

            if (i >= method.Parameters.Length || !formatProviderType.Matches(method.Parameters[i].Type))
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

    private static bool CanFixToStringOverload(IMethodSymbol overload, FormatProviderType formatProviderType)
    {
        if (overload.Parameters.Length != 2)
            return false;

        return overload.Parameters.Any(parameter => parameter.Type.IsString()) &&
               overload.Parameters.Any(parameter => formatProviderType.Matches(parameter.Type)) &&
               overload.Parameters.All(parameter => parameter.Type.IsString() || formatProviderType.Matches(parameter.Type));
    }

    // The fix passes a CultureInfo, so the parameter must accept one. Parameters of a more derived type, such as NumberFormatInfo, cannot be used.
    private readonly record struct FormatProviderType(ITypeSymbol FormatProviderSymbol, ITypeSymbol CultureInfoSymbol)
    {
        public bool Matches(ITypeSymbol type) => type.IsAssignableTo(FormatProviderSymbol) && CultureInfoSymbol.IsAssignableTo(type);
    }
}
