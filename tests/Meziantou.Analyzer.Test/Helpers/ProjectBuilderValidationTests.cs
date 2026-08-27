// The analyzer and the code fixers of this file are test helpers, not compiler extensions that ship to users, so
// the analyzer authoring rules do not apply
#pragma warning disable RS1038 // This compiler extension should not be implemented in an assembly containing a reference to Microsoft.CodeAnalysis.Workspaces

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Test.Helpers;

public sealed class ProjectBuilderValidationTests
{
    private const string SourceCode = """
        class TestClass
        {
            bool Test(object o) => [|o is string|];
        }
        """;

    [Fact]
    public async Task VerifyFix_CompilesTheTextProducedByTheFixer()
    {
        // The fixer produces a valid tree ('!' applied to the is-expression), but its text is '!o is string',
        // which re-parses as '(!o) is string' and does not compile. The test framework compiles the expected
        // fixed code, so the text of the fix is validated and not only the tree it was built from.
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => new ProjectBuilder()
            .WithAnalyzer(new TypeCheckAnalyzer())
            .WithCodeFixProvider(new NegateWithoutParenthesesFixer())
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith("""
                class TestClass
                {
                    bool Test(object o) => !o is string;
                }
                """)
            .ValidateAsync());

        Assert.Contains("error CS0023", exception.Message);
    }

    [Fact]
    public async Task VerifyFix_DoesNotReportValidFix()
    {
        await new ProjectBuilder()
            .WithAnalyzer(new TypeCheckAnalyzer())
            .WithCodeFixProvider(new NegateWithParenthesesFixer())
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith("""
                class TestClass
                {
                    bool Test(object o) => !(o is string);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task VerifyDiagnostic_ReportsAnalyzerException()
    {
        // The exception is reported by Roslyn as an AD0001 diagnostic, which the default analyzer id must not filter out
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => new ProjectBuilder()
            .WithAnalyzer(new ThrowingAnalyzer(), id: "TEST0002")
            .WithSourceCode("""
                class TestClass
                {
                    bool Test(object o) => o is string;
                }
                """)
            .ValidateAsync());

        Assert.Contains("AD0001", exception.Message);
        Assert.Contains(nameof(ThrowingAnalyzer), exception.Message);
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    private sealed class ThrowingAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new("TEST0002", "Throw", "Throw", "Test", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(_ => throw new InvalidOperationException("Analyzer failure"), SyntaxKind.IsExpression);
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    private sealed class TypeCheckAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new("TEST0001", "Negate the type check", "Negate the type check", "Test", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(
                ctx =>
                {
                    // Do not report the type checks that the fixers produce, so that applying a fix converges.
                    // '!(o is string)' is the tree the fixers build, '(!o) is string' is what the text of the
                    // fixer that omits the parentheses re-parses to.
                    if (ctx.Node.Parent is ParenthesizedExpressionSyntax { Parent: PrefixUnaryExpressionSyntax })
                        return;

                    if (((BinaryExpressionSyntax)ctx.Node).Left is not IdentifierNameSyntax)
                        return;

                    ctx.ReportDiagnostic(Diagnostic.Create(Rule, ctx.Node.GetLocation()));
                },
                SyntaxKind.IsExpression);
        }
    }

    private abstract class NegateFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("TEST0001");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        protected abstract ExpressionSyntax Negate(BinaryExpressionSyntax expression);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not BinaryExpressionSyntax expression)
                return;

            context.RegisterCodeFix(CodeAction.Create("Negate", async ct =>
            {
                var editor = await DocumentEditor.CreateAsync(context.Document, ct).ConfigureAwait(false);
                editor.ReplaceNode(expression, Negate(expression));
                return editor.GetChangedDocument();
            }, equivalenceKey: "Negate"), context.Diagnostics);
        }
    }

    private sealed class NegateWithoutParenthesesFixer : NegateFixer
    {
        protected override ExpressionSyntax Negate(BinaryExpressionSyntax expression) =>
            PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, expression);
    }

    private sealed class NegateWithParenthesesFixer : NegateFixer
    {
        protected override ExpressionSyntax Negate(BinaryExpressionSyntax expression) =>
            PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, ParenthesizedExpression(expression.WithoutTrivia()));
    }
}
