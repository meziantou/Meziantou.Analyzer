using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Fuzz;

/// <summary>
/// The analyzers and code fixers to run on an input.
/// </summary>
internal sealed class ComponentFilter
{
    private ComponentFilter(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders)
    {
        Analyzers = analyzers;
        CodeFixProviders = codeFixProviders
            .SelectMany(provider => provider.FixableDiagnosticIds.Select(id => (Id: id, Provider: provider)))
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .ToImmutableDictionary(group => group.Key, group => group.Select(item => item.Provider).ToImmutableArray(), StringComparer.Ordinal);
    }

    public ImmutableArray<DiagnosticAnalyzer> Analyzers { get; }

    public ImmutableDictionary<string, ImmutableArray<CodeFixProvider>> CodeFixProviders { get; }

    /// <summary>
    /// Create a filter.
    /// </summary>
    /// <param name="only">
    /// The full names of the components to run. When they are analyzers, only these analyzers run, without code fixers.
    /// When they are code fixers, all the analyzers run, so the diagnostics the code fixers fix are reported.
    /// </param>
    /// <param name="suppressed">The full names of the components not to run.</param>
    /// <param name="runCodeFixes">Whether to run the code fixers.</param>
    public static ComponentFilter Create(IReadOnlyCollection<string> only, IReadOnlySet<string> suppressed, bool runCodeFixes)
    {
        foreach (var name in only)
        {
            if (Components.FindByName(name) is null)
                throw new ArgumentException($"Cannot find the analyzer or the code fixer '{name}'", nameof(only));
        }

        var onlyAnalyzers = Components.Analyzers.Where(analyzer => only.Contains(Components.GetName(analyzer))).ToImmutableArray();
        var onlyCodeFixProviders = Components.CodeFixProviders.Where(provider => only.Contains(Components.GetName(provider))).ToImmutableArray();

        var analyzers = !onlyAnalyzers.IsEmpty
            ? onlyAnalyzers
            : Components.Analyzers.Where(analyzer => !suppressed.Contains(Components.GetName(analyzer))).ToImmutableArray();

        ImmutableArray<CodeFixProvider> codeFixProviders;
        if (!runCodeFixes || (!onlyAnalyzers.IsEmpty && onlyCodeFixProviders.IsEmpty))
        {
            codeFixProviders = [];
        }
        else if (!onlyCodeFixProviders.IsEmpty)
        {
            codeFixProviders = onlyCodeFixProviders;
        }
        else
        {
            codeFixProviders = Components.CodeFixProviders.Where(provider => !suppressed.Contains(Components.GetName(provider))).ToImmutableArray();
        }

        return new ComponentFilter(analyzers, codeFixProviders);
    }
}
