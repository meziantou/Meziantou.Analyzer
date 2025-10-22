using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Meziantou.Analyzer.Internals;
using System.Linq.Expressions;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseLangwordInXmlCommentAnalyzer : DiagnosticAnalyzer
{
    private static readonly ObjectPool<Queue<SyntaxNode>> NodeQueuePool = ObjectPool.Create<Queue<SyntaxNode>>();

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while",
    };

    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseLangwordInXmlComment,
        title: "Use langword in XML comment",
        messageFormat: "Use langword in XML comment",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseLangwordInXmlComment));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType, SymbolKind.Method, SymbolKind.Field, SymbolKind.Event, SymbolKind.Property);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;
        if (symbol.IsImplicitlyDeclared)
            return;

        if (symbol is INamedTypeSymbol namedTypeSymbol && (namedTypeSymbol.IsImplicitClass || symbol.Name.Contains('$', StringComparison.Ordinal)))
            return;

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax(context.CancellationToken);
            if (!syntax.HasStructuredTrivia)
                continue;

            foreach (var trivia in syntax.GetLeadingTrivia())
            {
                var structure = trivia.GetStructure();
                if (structure is null)
                    continue;

                if (structure is not DocumentationCommentTriviaSyntax documentation)
                    continue;

                // Detect the following patterns
                // <c>{keyword}</c>
                // <code>{keyword}</code>

                var queue = NodeQueuePool.Get();
                foreach (var item in documentation.ChildNodes())
                {
                    queue.Enqueue(item);
                }

                while (queue.TryDequeue(out var childNode))
                {
                    if (childNode is XmlElementSyntax elementSyntax)
                    {
                        var elementName = elementSyntax.StartTag.Name.LocalName.Text;
                        if (string.Equals(elementName, "c", StringComparison.OrdinalIgnoreCase) || string.Equals(elementName, "code", StringComparison.OrdinalIgnoreCase))
                        {
                            var item = elementSyntax.Content.SingleOrDefaultIfMultiple();
                            if (item is XmlTextSyntax { TextTokens: [var codeText] } && CSharpKeywords.Contains(codeText.Text))
                            {
                                var properties = ImmutableDictionary<string, string?>.Empty.Add("keyword", codeText.Text);
                                context.ReportDiagnostic(Rule, properties, elementSyntax);
                            }
                        }
                        else
                        {
                            foreach (var child in elementSyntax.Content)
                            {
                                queue.Enqueue(child);
                            }
                        }
                    }
                }

                NodeQueuePool.Return(queue);
            }
        }
    }

    private sealed class NodeQueuePoolPolicy : IPooledObjectPolicy<Queue<SyntaxNode>>
    {
        private readonly Func<Queue<SyntaxNode>, int> _func;

        public NodeQueuePoolPolicy()
        {
            var field = typeof(Queue<SyntaxNode>).GetField("_array");
            if (field is not null)
            {
                var param = Expression.Parameter(typeof(Queue<SyntaxNode>));
                var lambda = Expression.Lambda(Expression.Property(Expression.Field(param, field), "Length"), param);
                _func = (Func<Queue<SyntaxNode>, int>)lambda.Compile();
            }
            else
            {
                _func = item => 0;
            }
        }

        public Queue<SyntaxNode> Create() => new(capacity: 30);

        public bool Return(Queue<SyntaxNode> obj)
        {
            if (_func(obj) > 100)
                return false;

            obj.Clear();
            return true;
        }
    }
}
