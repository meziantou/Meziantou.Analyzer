﻿using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FixToDoAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.FixToDo,
        title: "Fix TODO comment",
        messageFormat: "TODO {0}",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.FixToDo));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    private static readonly char[] TrimStartChars = new[] { ' ', '\t', '*', '-', '/' };

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxTreeAction(AnalyzeTree);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);
        foreach (var node in root.DescendantTrivia())
        {
            if (node.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                // Remove leading "//"
                ProcessLine(context, node.ToString().AsSpan(2), startIndex: node.SpanStart + 2);
            }
            else if (node.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                var nodeText = node.ToString().AsSpan();
                nodeText = nodeText.Slice(2, nodeText.Length - 4); // Remove leading "/*" and trailing "*/"

                var startIndex = node.SpanStart + 2;
                foreach (var line in nodeText.SplitLines())
                {
                    ProcessLine(context, line, startIndex);
                    startIndex += line.Line.Length + line.Separator.Length;
                }
            }

            static void ProcessLine(SyntaxTreeAnalysisContext context, ReadOnlySpan<char> text, int startIndex)
            {
                // Trim start
                while (text.Length > 0 && text.Length >= 4 && TrimStartChars.Contains(text[0]))
                {
                    text = text.Slice(1);
                    startIndex++;
                }

                if (!text.StartsWith("TODO".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    return;

                if (text.Length == 4)
                {
                    var location = context.Tree.GetLocation(new TextSpan(startIndex, text.Length));
                    var diagnostic = Diagnostic.Create(s_rule, location, "");
                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    var nextChar = text[4];
                    if (nextChar is ' ' or '\t' or ':' or '!' or '?')
                    {
                        var location = context.Tree.GetLocation(new TextSpan(startIndex, text.Length));
                        var diagnostic = Diagnostic.Create(s_rule, location, text.Slice(5).Trim().ToString());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}

