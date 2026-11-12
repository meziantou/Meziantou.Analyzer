// This analyzer only runs in the tests, so it is never loaded during a command line compilation (RS1038)
#pragma warning disable RS1038 // This compiler extension should not be implemented in an assembly containing a reference to Microsoft.CodeAnalysis.Workspaces

using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Test.Internals;

public sealed class DiagnosticDescriptorExtensionsTests
{
    private const string RuleId = "TEST0001";

    [Theory]
    [InlineData(false, null, null, null, true)]
    [InlineData(false, ReportDiagnostic.Info, null, null, false)]
    [InlineData(false, ReportDiagnostic.Default, null, null, false)]
    [InlineData(false, ReportDiagnostic.Suppress, null, null, true)]
    [InlineData(false, null, "dotnet_diagnostic.TEST0001.severity = suggestion", null, false)]
    [InlineData(false, null, "dotnet_diagnostic.TEST0001.severity = none", null, true)]
    [InlineData(false, null, null, "dotnet_diagnostic.TEST0001.severity = suggestion", false)]
    [InlineData(false, null, null, "dotnet_diagnostic.TEST0001.severity = none", true)]
    [InlineData(false, ReportDiagnostic.Suppress, "dotnet_diagnostic.TEST0001.severity = suggestion", null, true)]
    [InlineData(false, ReportDiagnostic.Default, "dotnet_diagnostic.TEST0001.severity = none", null, true)]
    [InlineData(false, null, "dotnet_diagnostic.TEST0001.severity = suggestion", "dotnet_diagnostic.TEST0001.severity = none", false)]
    [InlineData(false, null, "dotnet_analyzer_diagnostic.severity = warning", null, true)]
    [InlineData(false, null, "dotnet_analyzer_diagnostic.category-Design.severity = warning", null, true)]
    [InlineData(true, null, null, null, false)]
    [InlineData(true, ReportDiagnostic.Suppress, null, null, true)]
    [InlineData(true, null, "dotnet_diagnostic.TEST0001.severity = none", null, true)]
    public async Task IsSuppressed_MatchesCompiler(bool isEnabledByDefault, ReportDiagnostic? compilationOption, string? editorConfig, string? globalConfig, bool expected)
    {
        var descriptor = new DiagnosticDescriptor(RuleId, "Title", "Message", "Design", DiagnosticSeverity.Info, isEnabledByDefault);
        var (isSuppressed, isReported) = await AnalyzeAsync(descriptor, compilationOption, editorConfig, globalConfig);

        Assert.Equal(expected, isSuppressed);
        Assert.Equal(!expected, isReported);
    }

    private static async Task<(bool IsSuppressed, bool IsReported)> AnalyzeAsync(DiagnosticDescriptor descriptor, ReportDiagnostic? compilationOption, string? editorConfig, string? globalConfig)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        if (compilationOption is { } option)
        {
            compilationOptions = compilationOptions.WithSpecificDiagnosticOptions([new(descriptor.Id, option)]);
        }

        var root = Path.Combine(Path.GetTempPath(), "DiagnosticDescriptorExtensionsTests");
        var solution = workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "Test", "Test", LanguageNames.CSharp, filePath: Path.Combine(root, "Test.csproj"), compilationOptions: compilationOptions))
            .AddDocument(DocumentId.CreateNewId(projectId), "Test.cs", SourceText.From("class Test { }"), filePath: Path.Combine(root, "Test.cs"));

        if (editorConfig is not null)
        {
            solution = solution.AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId), ".editorconfig", SourceText.From("root = true\n[*.cs]\n" + editorConfig), filePath: Path.Combine(root, ".editorconfig"));
        }

        if (globalConfig is not null)
        {
            solution = solution.AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId), ".globalconfig", SourceText.From("is_global = true\n" + globalConfig), filePath: Path.Combine(root, ".globalconfig"));
        }

        var project = solution.GetProject(projectId)!;
        var compilation = (await project.GetCompilationAsync(cancellationToken))!;
        var analyzer = new TestAnalyzer(descriptor);
        var diagnostics = await compilation.WithAnalyzers([analyzer], project.AnalyzerOptions).GetAnalyzerDiagnosticsAsync(cancellationToken);

        return (analyzer.IsSuppressed ?? throw new InvalidOperationException("The analyzer did not run"), diagnostics.Any(diagnostic => diagnostic.Id == descriptor.Id));
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    private sealed class TestAnalyzer(DiagnosticDescriptor descriptor) : DiagnosticAnalyzer
    {
        // A rule enabled by default, so that the compiler runs the analyzer when the tested rule is suppressed
        private static readonly DiagnosticDescriptor EnabledRule = new("TEST0002", "Title", "Message", "Other", DiagnosticSeverity.Hidden, isEnabledByDefault: true);

        public bool? IsSuppressed { get; private set; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [descriptor, EnabledRule];

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSemanticModelAction(context =>
            {
                IsSuppressed = descriptor.IsSuppressed(context.SemanticModel.Compilation, context.SemanticModel.SyntaxTree, context.CancellationToken);
                context.ReportDiagnostic(Diagnostic.Create(descriptor, context.SemanticModel.SyntaxTree.GetRoot(context.CancellationToken).GetLocation()));
            });
        }
    }
}
