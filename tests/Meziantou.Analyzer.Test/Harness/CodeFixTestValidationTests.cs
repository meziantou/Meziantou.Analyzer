// The analyzers of this file are test helpers, not compiler extensions that ship to users, so the analyzer
// authoring rules do not apply
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

namespace Meziantou.Analyzer.Test.Harness;

/// <summary>
/// The tests of <see cref="CSharpCodeFixTest{TAnalyzer, TCodeFix}"/> itself, so that a change to the harness that
/// stops reporting a defective analyzer or code fixer is detected.
/// </summary>
public sealed class CodeFixTestValidationTests
{
    private const string SourceCode = """
        class TestClass
        {
            bool Test(object o) => [|o is string|];
        }
        """;

    [Fact]
    public async Task VerifyFix_ReportsFixedCodeThatDoesNotCompile()
    {
        // The fixer produces a valid tree, but the code it produces does not compile
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => new CSharpCodeFixTest<TypeCheckAnalyzer, ReplaceWithUndefinedIdentifierFixer>
        {
            TestCode = SourceCode,
            FixedCode = """
                class TestClass
                {
                    bool Test(object o) => undefinedVariable;
                }
                """,
        }.RunAsync());

        Assert.Contains("CS0103", exception.Message);
    }

    [Fact]
    public async Task VerifyFix_DoesNotReportValidFix()
    {
        await new CSharpCodeFixTest<TypeCheckAnalyzer, NegateWithParenthesesFixer>
        {
            TestCode = SourceCode,
            FixedCode = """
                class TestClass
                {
                    bool Test(object o) => !(o is string);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task VerifyDiagnostic_ReportsAnalyzerException()
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => new CSharpAnalyzerTest<ThrowingAnalyzer>
        {
            TestCode = """
                class TestClass
                {
                    bool Test(object o) => o is string;
                }
                """,
        }.RunAsync());

        Assert.Contains(nameof(ThrowingAnalyzer), exception.Message);
    }

    [Fact]
    public async Task WithoutFixedCode_ReportsFixerThrowingWhenRegisteringTheFix()
    {
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() => new CSharpCodeFixTest<TypeCheckAnalyzer, ThrowingWhenRegisteringFixer>
        {
            TestCode = SourceCode,
        }.RunAsync());

        Assert.Equal("Registration failure", exception.Message);
    }

    [Fact]
    public async Task WithoutFixedCode_ReportsFixerThrowingWhenComputingTheChanges()
    {
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() => new CSharpCodeFixTest<TypeCheckAnalyzer, ThrowingWhenComputingChangesFixer>
        {
            TestCode = SourceCode,
        }.RunAsync());

        Assert.Equal("Computation failure", exception.Message);
    }

    [Fact]
    public async Task WithoutFixedCode_DoesNotReportValidFixer()
    {
        await new CSharpCodeFixTest<TypeCheckAnalyzer, NegateWithParenthesesFixer>
        {
            TestCode = SourceCode,
        }.RunAsync();
    }

    [Fact]
    public async Task WithoutFixedCode_DoesNotInvokeTheFixerWhenNoDiagnosticIsReported()
    {
        await new CSharpCodeFixTest<TypeCheckAnalyzer, ThrowingWhenRegisteringFixer>
        {
            TestCode = """
                class TestClass
                {
                    bool Test(object o) => o != null;
                }
                """,
        }.RunAsync();
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
            // The type checks that are already negated are not reported, so that applying the fix removes the diagnostic
            context.RegisterSyntaxNodeAction(ctx =>
            {
                if (ctx.Node.Parent is not ParenthesizedExpressionSyntax)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(Rule, ctx.Node.GetLocation()));
                }
            }, SyntaxKind.IsExpression);
        }
    }

    private abstract class RewriteTypeCheckFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("TEST0001");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        protected abstract ExpressionSyntax Rewrite(BinaryExpressionSyntax expression);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not BinaryExpressionSyntax expression)
                return;

            context.RegisterCodeFix(CodeAction.Create("Rewrite", async ct =>
            {
                var editor = await DocumentEditor.CreateAsync(context.Document, ct).ConfigureAwait(false);
                editor.ReplaceNode(expression, Rewrite(expression));
                return editor.GetChangedDocument();
            }, equivalenceKey: "Rewrite"), context.Diagnostics);
        }
    }

    private sealed class NegateWithParenthesesFixer : RewriteTypeCheckFixer
    {
        protected override ExpressionSyntax Rewrite(BinaryExpressionSyntax expression) =>
            PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, ParenthesizedExpression(expression.WithoutTrivia()));
    }

    private sealed class ReplaceWithUndefinedIdentifierFixer : RewriteTypeCheckFixer
    {
        protected override ExpressionSyntax Rewrite(BinaryExpressionSyntax expression) =>
            IdentifierName("undefinedVariable");
    }

    private sealed class ThrowingWhenRegisteringFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("TEST0001");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context) => throw new InvalidOperationException("Registration failure");
    }

    private sealed class ThrowingWhenComputingChangesFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("TEST0001");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(CodeAction.Create("Throw", static Task<Document> (CancellationToken _) => throw new InvalidOperationException("Computation failure"), equivalenceKey: "Throw"), context.Diagnostics);
            return Task.CompletedTask;
        }
    }
}
