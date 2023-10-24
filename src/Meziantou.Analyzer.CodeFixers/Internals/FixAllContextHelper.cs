﻿// File initially copied from
//  https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/4d9b3e3bb785a55f73b3029a843f0c0b73cc9ea7/StyleCop.Analyzers/StyleCop.Analyzers.CodeFixes/Helpers/FixAllContextHelper.cs
// Original copyright statement:
//  Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
//  Licensed under the MIT License. See LICENSE in the project root for license information.
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Meziantou.Analyzer;

internal static class FixAllContextHelper
{
    public static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(FixAllContext fixAllContext)
    {
        var allDiagnostics = ImmutableArray<Diagnostic>.Empty;
        var projectsToFix = ImmutableArray<Project>.Empty;

        var document = fixAllContext.Document;
        var project = fixAllContext.Project;

        switch (fixAllContext.Scope)
        {
            case FixAllScope.Document:
                if (document != null)
                {
                    var documentDiagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                    return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty.SetItem(document, documentDiagnostics);
                }

                break;

            case FixAllScope.Project:
                projectsToFix = ImmutableArray.Create(project);
                allDiagnostics = await GetAllDiagnosticsAsync(fixAllContext, project).ConfigureAwait(false);
                break;

            case FixAllScope.Solution:
                projectsToFix = project.Solution.Projects
                    .Where(p => p.Language == project.Language)
                    .ToImmutableArray();

                var diagnostics = new ConcurrentDictionary<ProjectId, ImmutableArray<Diagnostic>>();
                var tasks = new Task[projectsToFix.Length];
                for (var i = 0; i < projectsToFix.Length; i++)
                {
                    fixAllContext.CancellationToken.ThrowIfCancellationRequested();
                    var projectToFix = projectsToFix[i];
                    tasks[i] = Task.Run(
                        async () =>
                        {
                            var projectDiagnostics = await GetAllDiagnosticsAsync(fixAllContext, projectToFix).ConfigureAwait(false);
                            diagnostics.TryAdd(projectToFix.Id, projectDiagnostics);
                        }, fixAllContext.CancellationToken);
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
                allDiagnostics = allDiagnostics.AddRange(diagnostics.SelectMany(i => i.Value.Where(x => fixAllContext.DiagnosticIds.Contains(x.Id))));
                break;
        }

        if (allDiagnostics.IsEmpty)
        {
            return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
        }

        return await GetDocumentDiagnosticsToFixAsync(allDiagnostics, projectsToFix, fixAllContext.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all <see cref="Diagnostic"/> instances within a specific <see cref="Project"/> which are relevant to a
    /// <see cref="FixAllContext"/>.
    /// </summary>
    /// <param name="fixAllContext">The context for the Fix All operation.</param>
    /// <param name="project">The project.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. When the task completes
    /// successfully, the <see cref="Task{TResult}.Result"/> will contain the requested diagnostics.</returns>
    private static async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync(FixAllContext fixAllContext, Project project)
    {
        return await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
    }

    private static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
        ImmutableArray<Diagnostic> diagnostics,
        ImmutableArray<Project> projects,
        CancellationToken cancellationToken)
    {
        var treeToDocumentMap = await GetTreeToDocumentMapAsync(projects, cancellationToken).ConfigureAwait(false);

        var builder = ImmutableDictionary.CreateBuilder<Document, ImmutableArray<Diagnostic>>();
        foreach (var documentAndDiagnostics in diagnostics.GroupBy(d => GetReportedDocument(d, treeToDocumentMap)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var document = documentAndDiagnostics.Key;
            if (document != null)
            {
                var diagnosticsForDocument = documentAndDiagnostics.ToImmutableArray();
                builder.Add(document, diagnosticsForDocument);
            }
        }

        return builder.ToImmutable();
    }

    private static async Task<ImmutableDictionary<SyntaxTree, Document>> GetTreeToDocumentMapAsync(ImmutableArray<Project> projects, CancellationToken cancellationToken)
    {
        var builder = ImmutableDictionary.CreateBuilder<SyntaxTree, Document>();
        foreach (var project in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                if (tree != null)
                {
                    builder.Add(tree, document);
                }
            }
        }

        return builder.ToImmutable();
    }

    private static Document? GetReportedDocument(Diagnostic diagnostic, ImmutableDictionary<SyntaxTree, Document> treeToDocumentsMap)
    {
        var tree = diagnostic.Location.SourceTree;
        if (tree != null)
        {
            if (treeToDocumentsMap.TryGetValue(tree, out var document))
            {
                return document;
            }
        }

        return null;
    }
}
