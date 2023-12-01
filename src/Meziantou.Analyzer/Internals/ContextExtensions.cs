using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer;

internal static partial class ContextExtensions
{
    private static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, Location location, ImmutableDictionary<string, string?>? properties, string?[]? messageArgs)
    {
        return Diagnostic.Create(descriptor, location, properties, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, SyntaxReference syntaxReference, string?[]? messageArgs = null)
        => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, syntaxReference, messageArgs);

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxReference syntaxReference, string?[]? messageArgs = null)
    {
        var syntaxNode = syntaxReference.GetSyntax(context.CancellationToken);
        context.ReportDiagnostic(CreateDiagnostic(descriptor, syntaxNode.GetLocation(), properties, messageArgs));
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, Location location, string?[]? messageArgs = null) => context.ReportDiagnostic(CreateDiagnostic(descriptor, location, ImmutableDictionary<string, string?>.Empty, messageArgs));
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, Location location, string?[]? messageArgs = null) => context.ReportDiagnostic(CreateDiagnostic(descriptor, location, properties, messageArgs));

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, SyntaxNode syntax, string?[]? messageArgs = null) => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, syntax.GetLocation(), messageArgs);
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxNode syntax, string?[]? messageArgs = null) => ReportDiagnostic(context, descriptor, properties, syntax.GetLocation(), messageArgs);

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, SyntaxToken token, string?[]? messageArgs = null) => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, token.GetLocation(), messageArgs);
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxToken syntaxToken, params string?[]? messageArgs)
    {
        context.ReportDiagnostic(CreateDiagnostic(descriptor, syntaxToken.GetLocation(), properties, messageArgs));
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ISymbol symbol, string?[]? messageArgs = null) => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, symbol, messageArgs);
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ISymbol symbol, string?[]? messageArgs = null)
    {
        foreach (var location in symbol.Locations)
        {
            ReportDiagnostic(context, descriptor, properties, location, messageArgs);
        }
    }

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, IOperation operation, string?[]? messageArgs = null)
        => ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string?>.Empty, operation, messageArgs);
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IOperation operation, string?[]? messageArgs = null)
        => context.ReportDiagnostic(CreateDiagnostic(descriptor, operation.Syntax.GetLocation(), properties, messageArgs));

    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ILocalFunctionOperation operation, DiagnosticReportOptions options, string?[]? messageArgs = null)
    {
        if (options.HasFlag(DiagnosticReportOptions.ReportOnMethodName) &&
            operation.Syntax is LocalFunctionStatementSyntax memberAccessExpression)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, memberAccessExpression.Identifier.GetLocation(), properties, messageArgs));
            return;
        }

        context.ReportDiagnostic(descriptor, properties, operation, messageArgs);
    }
    public static void ReportDiagnostic(this DiagnosticReporter context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IInvocationOperation operation, DiagnosticReportOptions options, params string?[]? messageArgs)
    {
        if (options.HasFlag(DiagnosticReportOptions.ReportOnMethodName) &&
            operation.Syntax.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax memberAccessExpression)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, memberAccessExpression.Name.GetLocation(), properties, messageArgs));
            return;
        }

        context.ReportDiagnostic(descriptor, properties, operation, messageArgs);
    }
}
