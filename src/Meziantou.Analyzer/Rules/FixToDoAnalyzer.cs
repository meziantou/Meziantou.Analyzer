using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FixToDoAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.FixToDo,
            title: "Fix TODO comment",
            messageFormat: "TODO {0}",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.FixToDo));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxTreeAction(AnalyzeTree);
        }

        private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);
            var commentNodes = root.DescendantTrivia().Where(node => node.IsKind(SyntaxKind.MultiLineCommentTrivia) || node.IsKind(SyntaxKind.SingleLineCommentTrivia));

            foreach (var node in commentNodes)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.SingleLineCommentTrivia:
                        ProcessLine(new TextSpan(0, node.ToString()).Substring(2));
                        break;
                    case SyntaxKind.MultiLineCommentTrivia:
                        var nodeText = new TextSpan(0, node.ToString());
                        nodeText = nodeText.Substring(2, nodeText.Length - 4);

                        foreach (var line in nodeText.GetLines())
                        {
                            ProcessLine(line);
                        }

                        break;
                }

                void ProcessLine(TextSpan line)
                {
                    line = line.TrimStart(' ', '\t', '*', '-', '/');
                    if (string.Equals(line.Text, "TODO", StringComparison.OrdinalIgnoreCase) ||
                        line.Text.StartsWith("TODO ", StringComparison.OrdinalIgnoreCase) ||
                        line.Text.StartsWith("TODO:", StringComparison.OrdinalIgnoreCase))
                    {
                        var location = node.SyntaxTree.GetLocation(new Microsoft.CodeAnalysis.Text.TextSpan(node.SpanStart + line.SpanStart, line.Text.Length));

                        var diagnostic = Diagnostic.Create(s_rule, location, line.Text.Substring(4).Trim());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct TextSpan
        {
            public TextSpan(int spanStart, string text)
            {
                SpanStart = spanStart;
                Text = text;
            }

            public int SpanStart { get; }
            public string Text { get; }
            public int Length => Text.Length;

            public TextSpan TrimStart(params char[] characters)
            {
                for (var i = 0; i < Text.Length; i++)
                {
                    if (!characters.Contains(Text[i]))
                    {
                        return new TextSpan(SpanStart + i, Text.Substring(i));
                    }
                }

                return this;
            }

            public TextSpan Substring(int startIndex)
            {
                return new TextSpan(SpanStart + startIndex, Text.Substring(startIndex));
            }

            public TextSpan Substring(int startIndex, int length)
            {
                return new TextSpan(SpanStart + startIndex, Text.Substring(startIndex, length));
            }

            public IEnumerable<TextSpan> GetLines()
            {
                var lineBegin = 0;
                for (var i = 0; i < Text.Length; i++)
                {
                    var c = Text[i];
                    if (c == '\n')
                    {
                        var length = i - lineBegin;
                        if (i > 0 && Text[i - 1] == '\r')
                        {
                            length -= 1;
                        }

                        yield return new TextSpan(SpanStart + lineBegin, Text.Substring(lineBegin, length));
                        lineBegin = i + 1;
                    }
                }

                if (lineBegin < Text.Length)
                {
                    yield return new TextSpan(SpanStart + lineBegin, Text.Substring(lineBegin));
                }
            }
        }
    }
}
