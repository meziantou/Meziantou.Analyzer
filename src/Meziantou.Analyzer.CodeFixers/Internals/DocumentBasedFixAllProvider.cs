#if ROSLYN_3_8
#pragma warning disable IDE0130 // Namespace does not match folder structure
// File initially copied from
//  https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/4d9b3e3bb785a55f73b3029a843f0c0b73cc9ea7/StyleCop.Analyzers/StyleCop.Analyzers.CodeFixes/Helpers/DocumentBasedFixAllProvider.cs
// Original copyright statement:
//  Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
//  Licensed under the MIT License. See LICENSE in the project root for license information.
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Meziantou.Analyzer;

/// <summary>
/// Provides a base class to write a <see cref="FixAllProvider"/> that fixes documents independently.
/// </summary>
internal abstract class DocumentBasedFixAllProvider : FixAllProvider
{
    protected abstract string GetFixAllTitle(FixAllContext fixAllContext);

    public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
    {
        return Task.FromResult(fixAllContext.Scope switch
        {
            FixAllScope.Document => CodeAction.Create(
                                        GetFixAllTitle(fixAllContext),
                                        cancellationToken => GetDocumentFixesAsync(fixAllContext.WithCancellationToken(cancellationToken)),
                                        nameof(DocumentBasedFixAllProvider)),
            FixAllScope.Project => CodeAction.Create(
                                        GetFixAllTitle(fixAllContext),
                                        cancellationToken => GetProjectFixesAsync(fixAllContext.WithCancellationToken(cancellationToken), fixAllContext.Project),
                                        nameof(DocumentBasedFixAllProvider)),
            FixAllScope.Solution => CodeAction.Create(
                                        GetFixAllTitle(fixAllContext),
                                        cancellationToken => GetSolutionFixesAsync(fixAllContext.WithCancellationToken(cancellationToken)),
                                        nameof(DocumentBasedFixAllProvider)),
            _ => null,
        });
    }

    /// <summary>
    /// Fixes all occurrences of a diagnostic in a specific document.
    /// </summary>
    /// <param name="fixAllContext">The context for the Fix All operation.</param>
    /// <param name="document">The document to fix.</param>
    /// <param name="diagnostics">The diagnostics to fix in the document.</param>
    /// <returns>
    /// <para>The new <see cref="Document"/> representing the root of the fixed document.</para>
    /// <para>-or-</para>
    /// <para><see langword="null"/>, if no changes were made to the document.</para>
    /// </returns>
    protected abstract Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics);

    private async Task<Document> GetDocumentFixesAsync(FixAllContext fixAllContext)
    {
        var document = fixAllContext.Document!;
        var documentDiagnosticsToFix = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);
        if (!documentDiagnosticsToFix.TryGetValue(document, out var diagnostics))
        {
            return document;
        }

        var newDocument = await FixAllAsync(fixAllContext, document, diagnostics).ConfigureAwait(false);
        return newDocument ?? document;
    }

    private async Task<Solution> GetSolutionFixesAsync(FixAllContext fixAllContext, ImmutableArray<Document> documents)
    {
        var documentDiagnosticsToFix = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);

        var solution = fixAllContext.Solution;
        var newDocuments = new List<Task<Document?>>(documents.Length);
        foreach (var document in documents)
        {
            if (!documentDiagnosticsToFix.TryGetValue(document, out var diagnostics))
            {
                newDocuments.Add(Task.FromResult<Document?>(document));
                continue;
            }

            newDocuments.Add(FixAllAsync(fixAllContext, document, diagnostics));
        }

        for (var i = 0; i < documents.Length; i++)
        {
            var newDocumentRoot = await newDocuments[i].ConfigureAwait(false);
            if (newDocumentRoot is null)
                continue;

            //solution = solution.WithDocumentSyntaxRoot(documents[i].Id, newDocumentRoot);
        }

        return solution;
    }

    private Task<Solution> GetProjectFixesAsync(FixAllContext fixAllContext, Project project)
    {
        return GetSolutionFixesAsync(fixAllContext, project.Documents.ToImmutableArray());
    }

    private Task<Solution> GetSolutionFixesAsync(FixAllContext fixAllContext)
    {
        var documents = fixAllContext.Solution.Projects.SelectMany(i => i.Documents).ToImmutableArray();
        return GetSolutionFixesAsync(fixAllContext, documents);
    }
}
#endif
