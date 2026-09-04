// The diagnostics located in generated code are filtered here, so the ReportDiagnostic methods of
// ContextExtensions, DiagnosticReporter and the analysis contexts must not be called directly
#pragma warning disable RS0030 // Do not use banned APIs

namespace Meziantou.Analyzer;

/// <summary>
/// The <c>ReportDiagnostic</c> methods the analyzers use. They have the same signatures as the ones of
/// <see cref="Meziantou.Framework.Roslyn.ContextExtensions"/>, and hide them as this class is declared in a
/// namespace closer to the analyzers than the imported <c>Meziantou.Framework.Roslyn</c> namespace. The rules
/// that call an overload this class does not declare use the ones of the package, which are banned, so the
/// missing overload is a compilation error instead of a rule reporting in generated code when it should not.
/// </summary>
internal static class DiagnosticReportingExtensions
{
    /// <summary>
    /// Reports a diagnostic created by the rule itself, such as one with a different severity.
    /// It cannot be named <c>ReportDiagnostic</c>, as <see cref="DiagnosticReporter.ReportDiagnostic(Diagnostic)"/>
    /// would win over it and skip the filtering of the generated code.
    /// </summary>
    public static void Report(this DiagnosticReporter reporter, Diagnostic diagnostic)
    {
        if (CanReportDiagnostic(reporter, diagnostic.Descriptor, diagnostic.Location))
            reporter.ReportDiagnostic(diagnostic);
    }

    /// <inheritdoc cref="Report(DiagnosticReporter, Diagnostic)" />
    public static void Report(this CompilationAnalysisContext context, Diagnostic diagnostic) => Report((DiagnosticReporter)context, diagnostic);

    /// <inheritdoc cref="Report(DiagnosticReporter, Diagnostic)" />
    public static void Report(this SyntaxNodeAnalysisContext context, Diagnostic diagnostic) => Report((DiagnosticReporter)context, diagnostic);

    // SyntaxTreeAnalysisContext is not supported by DiagnosticReporter, so the diagnostic is created here
    public static void ReportDiagnostic(this SyntaxTreeAnalysisContext context, DiagnosticDescriptor descriptor, Location location, params object?[]? messageArgs)
    {
        if (!GeneratedCodeReporting.CanReportDiagnostic(context.Options, descriptor, location.SourceTree, context.CancellationToken))
            return;

        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, IEnumerable<Location> locations, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, locations))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, locations, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IEnumerable<Location> locations, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, locations))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, locations, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, SyntaxToken syntaxToken, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, syntaxToken))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, syntaxToken, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxToken syntaxToken, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, syntaxToken))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, syntaxToken, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, SyntaxNode syntaxNode, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, syntaxNode))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, syntaxNode, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxNode syntaxNode, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, syntaxNode))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, syntaxNode, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ISymbol symbol, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, symbol))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, ImmutableDictionary<string, string?>.Empty, symbol, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ISymbol symbol, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, symbol))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, symbol, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, symbol))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, symbol, reportOptions, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, symbol))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, symbol, reportOptions, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, symbol))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, symbol, reportOptions, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, symbol))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, symbol, reportOptions, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, symbol))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, symbol, reportOptions, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, symbol))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, symbol, reportOptions, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, symbol))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, symbol, reportOptions, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, symbol))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, symbol, reportOptions, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, Location location, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, location))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, location, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, Location location, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, location))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, location, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, SyntaxReference syntaxReference, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, syntaxReference))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, syntaxReference, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxReference syntaxReference, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, syntaxReference))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, syntaxReference, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, IInvocationOperation operation, DiagnosticInvocationReportOptions options, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, operation))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, ImmutableDictionary<string, string?>.Empty, operation, options, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IInvocationOperation operation, DiagnosticInvocationReportOptions options, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, operation))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, operation, options, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ILocalFunctionOperation operation, DiagnosticMethodReportOptions options, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, operation))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, operation, options, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, IOperation operation, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, operation))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, operation, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IOperation operation, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, operation))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, operation, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, AttributeData attribute, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, attribute))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, attribute, messageArgs);
    }

    public static void ReportDiagnostic(this DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, AttributeData attribute, params object?[]? messageArgs)
    {
        if (CanReportDiagnostic(reporter, descriptor, attribute))
            ContextExtensions.ReportDiagnostic(reporter, descriptor, properties, attribute, messageArgs);
    }
    // SymbolAnalysisContext
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, IEnumerable<Location> locations, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, locations, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IEnumerable<Location> locations, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, locations, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxToken syntaxToken, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxToken, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxToken syntaxToken, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxToken, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxNode syntaxNode, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxNode, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxNode syntaxNode, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxNode, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ISymbol symbol, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ISymbol symbol, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, Location location, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, location, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, Location location, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, location, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxReference syntaxReference, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxReference, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxReference syntaxReference, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxReference, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, IInvocationOperation operation, DiagnosticInvocationReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, operation, options, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IInvocationOperation operation, DiagnosticInvocationReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, options, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ILocalFunctionOperation operation, DiagnosticMethodReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, options, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, IOperation operation, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, operation, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IOperation operation, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, AttributeData attribute, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, attribute, messageArgs);
    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, AttributeData attribute, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, attribute, messageArgs);

    // OperationAnalysisContext
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, IEnumerable<Location> locations, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, locations, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IEnumerable<Location> locations, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, locations, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxToken syntaxToken, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxToken, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxToken syntaxToken, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxToken, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxNode syntaxNode, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxNode, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxNode syntaxNode, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxNode, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ISymbol symbol, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ISymbol symbol, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, Location location, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, location, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, Location location, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, location, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxReference syntaxReference, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxReference, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxReference syntaxReference, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxReference, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, IInvocationOperation operation, DiagnosticInvocationReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, operation, options, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IInvocationOperation operation, DiagnosticInvocationReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, options, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ILocalFunctionOperation operation, DiagnosticMethodReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, options, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, IOperation operation, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, operation, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IOperation operation, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, AttributeData attribute, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, attribute, messageArgs);
    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, AttributeData attribute, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, attribute, messageArgs);

    // OperationBlockAnalysisContext
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, IEnumerable<Location> locations, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, locations, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IEnumerable<Location> locations, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, locations, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxToken syntaxToken, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxToken, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxToken syntaxToken, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxToken, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxNode syntaxNode, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxNode, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxNode syntaxNode, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxNode, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ISymbol symbol, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ISymbol symbol, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, Location location, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, location, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, Location location, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, location, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxReference syntaxReference, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxReference, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxReference syntaxReference, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxReference, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, IInvocationOperation operation, DiagnosticInvocationReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, operation, options, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IInvocationOperation operation, DiagnosticInvocationReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, options, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ILocalFunctionOperation operation, DiagnosticMethodReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, options, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, IOperation operation, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, operation, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IOperation operation, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, AttributeData attribute, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, attribute, messageArgs);
    public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, AttributeData attribute, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, attribute, messageArgs);

    // SyntaxNodeAnalysisContext
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, IEnumerable<Location> locations, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, locations, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IEnumerable<Location> locations, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, locations, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxToken syntaxToken, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxToken, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxToken syntaxToken, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxToken, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxNode syntaxNode, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxNode, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxNode syntaxNode, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxNode, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ISymbol symbol, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ISymbol symbol, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, Location location, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, location, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, Location location, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, location, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxReference syntaxReference, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxReference, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxReference syntaxReference, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxReference, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, IInvocationOperation operation, DiagnosticInvocationReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, operation, options, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IInvocationOperation operation, DiagnosticInvocationReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, options, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ILocalFunctionOperation operation, DiagnosticMethodReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, options, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, IOperation operation, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, operation, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IOperation operation, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, AttributeData attribute, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, attribute, messageArgs);
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, AttributeData attribute, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, attribute, messageArgs);

    // CompilationAnalysisContext
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, IEnumerable<Location> locations, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, locations, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IEnumerable<Location> locations, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, locations, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxToken syntaxToken, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxToken, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxToken syntaxToken, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxToken, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxNode syntaxNode, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxNode, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxNode syntaxNode, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxNode, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ISymbol symbol, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ISymbol symbol, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IFieldSymbol symbol, DiagnosticFieldReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IMethodSymbol symbol, DiagnosticMethodReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IParameterSymbol symbol, DiagnosticParameterReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IPropertySymbol symbol, DiagnosticPropertyReportOptions reportOptions, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, symbol, reportOptions, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, Location location, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, location, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, Location location, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, location, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxReference syntaxReference, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, syntaxReference, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, SyntaxReference syntaxReference, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, syntaxReference, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, IInvocationOperation operation, DiagnosticInvocationReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, operation, options, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IInvocationOperation operation, DiagnosticInvocationReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, options, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, ILocalFunctionOperation operation, DiagnosticMethodReportOptions options, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, options, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, IOperation operation, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, operation, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IOperation operation, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, operation, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, AttributeData attribute, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, attribute, messageArgs);
    public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, AttributeData attribute, params object?[]? messageArgs) => ReportDiagnostic((DiagnosticReporter)context, descriptor, properties, attribute, messageArgs);

    private static bool CanReportDiagnostic(DiagnosticReporter reporter, DiagnosticDescriptor descriptor, SyntaxTree? syntaxTree)
        => GeneratedCodeReporting.CanReportDiagnostic(reporter.Options, descriptor, syntaxTree, reporter.CancellationToken);

    private static bool CanReportDiagnostic(DiagnosticReporter reporter, DiagnosticDescriptor descriptor, IEnumerable<Location> locations)
        => CanReportDiagnostic(reporter, descriptor, GetSyntaxTree(locations));

    private static bool CanReportDiagnostic(DiagnosticReporter reporter, DiagnosticDescriptor descriptor, Location location)
        => CanReportDiagnostic(reporter, descriptor, location.SourceTree);

    private static bool CanReportDiagnostic(DiagnosticReporter reporter, DiagnosticDescriptor descriptor, SyntaxNode syntaxNode)
        => CanReportDiagnostic(reporter, descriptor, syntaxNode.SyntaxTree);

    private static bool CanReportDiagnostic(DiagnosticReporter reporter, DiagnosticDescriptor descriptor, SyntaxToken syntaxToken)
        => CanReportDiagnostic(reporter, descriptor, syntaxToken.SyntaxTree);

    private static bool CanReportDiagnostic(DiagnosticReporter reporter, DiagnosticDescriptor descriptor, SyntaxReference syntaxReference)
        => CanReportDiagnostic(reporter, descriptor, syntaxReference.SyntaxTree);

    private static bool CanReportDiagnostic(DiagnosticReporter reporter, DiagnosticDescriptor descriptor, ISymbol symbol)
        => CanReportDiagnostic(reporter, descriptor, GetSyntaxTree(symbol.Locations));

    private static bool CanReportDiagnostic(DiagnosticReporter reporter, DiagnosticDescriptor descriptor, IOperation operation)
        => CanReportDiagnostic(reporter, descriptor, operation.Syntax.SyntaxTree);

    private static bool CanReportDiagnostic(DiagnosticReporter reporter, DiagnosticDescriptor descriptor, AttributeData attribute)
        => CanReportDiagnostic(reporter, descriptor, attribute.ApplicationSyntaxReference?.SyntaxTree);

    private static SyntaxTree? GetSyntaxTree(IEnumerable<Location> locations)
    {
        foreach (var location in locations)
        {
            if (location.IsInSource)
                return location.SourceTree;
        }

        return null;
    }
}
