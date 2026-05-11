using System.Collections.Immutable;
using System.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

internal static partial class ContextExtensions
{
    private static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, Location? location, ImmutableDictionary<string, string?>? properties, object?[]? messageArgs)
    {
        return Diagnostic.Create(descriptor, location, properties, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, IEnumerable<Location> locations, object?[]? messageArgs = null) => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, locations, messageArgs);
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IEnumerable<Location> locations, params object?[]? messageArgs)
    {
        List<Location>? inSourceLocations = null;
        foreach (var location in locations)
        {
            if (!location.IsInSource)
                continue;

            inSourceLocations ??= [];
            inSourceLocations.Add(location);
        }

        if (inSourceLocations is null)
        {
            context.ReportDiagnostic(CreateDiagnostic(descriptor, location: null, properties, messageArgs));
            return;
        }

        var diagnostic = Diagnostic.Create(
                 descriptor,
                 location: inSourceLocations[0],
                 additionalLocations: inSourceLocations.Count > 1 ? GetAdditionalLocations(inSourceLocations) : null,
                 properties: properties,
                 messageArgs: messageArgs);

        context.ReportDiagnostic(diagnostic);

        static IEnumerable<Location> GetAdditionalLocations(List<Location> locations)
        {
            for (var i = 1; i < locations.Count; i++)
            {
                yield return locations[i];
            }
        }
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, SyntaxReference syntaxReference, object?[]? messageArgs = null)
        => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, syntaxReference, messageArgs);

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxReference syntaxReference, object?[]? messageArgs = null)
    {
        var syntaxNode = syntaxReference.GetSyntax(context.CancellationToken);
        context.ReportDiagnostic(CreateDiagnostic(descriptor, syntaxNode.GetLocation(), properties, messageArgs));
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, Location location, object?[]? messageArgs = null) => context.ReportDiagnostic(CreateDiagnostic(descriptor, location, ImmutableDictionary<string, string?>.Empty, messageArgs));
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, Location location, object?[]? messageArgs = null) => context.ReportDiagnostic(CreateDiagnostic(descriptor, location, properties, messageArgs));

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, SyntaxNode syntax, object?[]? messageArgs = null) => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, syntax.GetLocation(), messageArgs);
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxNode syntax, object?[]? messageArgs = null) => ReportDiagnostic(context, descriptor, properties, syntax.GetLocation(), messageArgs);

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, SyntaxToken token, object?[]? messageArgs = null) => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, token.GetLocation(), messageArgs);
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxToken syntaxToken, params object?[]? messageArgs)
    {
        context.ReportDiagnostic(CreateDiagnostic(descriptor, syntaxToken.GetLocation(), properties, messageArgs));
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ISymbol symbol, object?[]? messageArgs = null) => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, symbol, messageArgs);
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ISymbol symbol, object?[]? messageArgs = null)
    {
        ReportDiagnostic(context, descriptor, properties, symbol.Locations, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, object?[]? messageArgs = null) => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, object?[]? messageArgs = null)
    {
        List<Location>? locations = null;
        foreach (var location in symbol.Locations)
        {
            if (reportOptions.HasFlag(DiagnosticFieldReportOptions.ReportOnReturnType))
            {
                var node = location.SourceTree?.GetRoot(context.CancellationToken).FindNode(location.SourceSpan);
                if (node is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type: not null and var type } })
                {
                    ReportDiagnostic(context, descriptor, properties, type.GetLocation(), messageArgs);
                    return;
                }
            }

            locations ??= [];
            locations.Add(location);
        }

        ReportDiagnostic(context, descriptor, properties, locations ?? [], messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, object?[]? messageArgs = null) => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, object?[]? messageArgs = null)
    {
        List<Location>? locations = null;
        foreach (var location in symbol.Locations)
        {
            if (reportOptions.HasFlag(DiagnosticMethodReportOptions.ReportOnReturnType))
            {
                var node = location.SourceTree?.GetRoot(context.CancellationToken).FindNode(location.SourceSpan);
                if (node is MethodDeclarationSyntax methodDeclarationSyntax)
                {
                    ReportDiagnostic(context, descriptor, properties, methodDeclarationSyntax.ReturnType.GetLocation(), messageArgs);
                    return;
                }

                if (node is DelegateDeclarationSyntax delegateDeclarationSyntax)
                {
                    ReportDiagnostic(context, descriptor, properties, delegateDeclarationSyntax.ReturnType.GetLocation(), messageArgs);
                    return;
                }
            }

            locations ??= [];
            locations.Add(location);
        }

        ReportDiagnostic(context, descriptor, properties, locations ?? [], messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, object?[]? messageArgs = null) => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, object?[]? messageArgs = null)
    {
        List<Location>? locations = null;
        foreach (var location in symbol.Locations)
        {
            if (reportOptions.HasFlag(DiagnosticParameterReportOptions.ReportOnType))
            {
                var node = location.SourceTree?.GetRoot(context.CancellationToken).FindNode(location.SourceSpan);
                if (node is ParameterSyntax { Type: not null and var parameterType })
                {
                    ReportDiagnostic(context, descriptor, properties, parameterType.GetLocation(), messageArgs);
                    return;
                }
            }

            locations ??= [];
            locations.Add(location);
        }

        ReportDiagnostic(context, descriptor, properties, locations ?? [], messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, object?[]? messageArgs = null) => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, object?[]? messageArgs = null)
    {
        List<Location>? locations = null;
        foreach (var location in symbol.Locations)
        {
            if (reportOptions.HasFlag(DiagnosticPropertyReportOptions.ReportOnReturnType))
            {
                var node = location.SourceTree?.GetRoot(context.CancellationToken).FindNode(location.SourceSpan);
                if (node is PropertyDeclarationSyntax { Type: not null and var returnType })
                {
                    ReportDiagnostic(context, descriptor, properties, returnType.GetLocation(), messageArgs);
                    return;
                }

                if (node is IndexerDeclarationSyntax { Type: not null and var returnType2 })
                {
                    ReportDiagnostic(context, descriptor, properties, returnType2.GetLocation(), messageArgs);
                    return;
                }
            }

            locations ??= [];
            locations.Add(location);
        }

        ReportDiagnostic(context, descriptor, properties, locations ?? [], messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, IOperation operation, object?[]? messageArgs = null)
        => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, operation, messageArgs);
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IOperation operation, object?[]? messageArgs = null)
        => context.ReportDiagnostic(CreateDiagnostic(descriptor, operation.Syntax.GetLocation(), properties, messageArgs));

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ILocalFunctionOperation operation, DiagnosticMethodReportOptions options, object?[]? messageArgs = null)
    {
        if (options.HasFlag(DiagnosticMethodReportOptions.ReportOnMethodName) && operation.Syntax is LocalFunctionStatementSyntax memberAccessExpression)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, memberAccessExpression.Identifier.GetLocation(), properties, messageArgs));
            return;
        }

        if (options.HasFlag(DiagnosticMethodReportOptions.ReportOnReturnType) && operation.Syntax is LocalFunctionStatementSyntax memberAccessExpression2)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, memberAccessExpression2.ReturnType.GetLocation(), properties, messageArgs));
            return;
        }

        context.ReportDiagnostic(descriptor, properties, operation, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IInvocationOperation operation, DiagnosticInvocationReportOptions options, params object?[]? messageArgs)
    {
        TextSpan? span = null;

        if (options.HasFlag(DiagnosticInvocationReportOptions.ReportOnMember) &&
            operation.Syntax.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax memberAccessExpression)
        {
            SetSpan(memberAccessExpression.Name.Span);
        }

        if (options.HasFlag(DiagnosticInvocationReportOptions.ReportOnArguments) &&
            operation.Syntax is InvocationExpressionSyntax invocationExpression)
        {
            SetSpan(invocationExpression.ArgumentList.Span);
        }

        if (span is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.Create(operation.Syntax.SyntaxTree, span.Value), properties, messageArgs));
            return;
        }

        context.ReportDiagnostic(descriptor, properties, operation, messageArgs);

        void SetSpan(TextSpan newSpan)
        {
            if (span is null)
            {
                span = newSpan;
            }
            else
            {
                span = TextSpan.FromBounds(Math.Min(span.Value.Start, newSpan.Start), Math.Max(span.Value.End, newSpan.End));
            }
        }
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, AttributeData attribute, params object?[]? messageArgs)
    {
        ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, attribute, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, AttributeData attribute, params object?[]? messageArgs)
    {
        if (attribute.ApplicationSyntaxReference is not null)
        {
            context.ReportDiagnostic(descriptor, properties, attribute.ApplicationSyntaxReference, messageArgs);
        }
    }
}
