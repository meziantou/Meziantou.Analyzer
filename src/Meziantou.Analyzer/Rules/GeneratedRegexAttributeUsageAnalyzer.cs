namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GeneratedRegexAttributeUsageAnalyzer : RegexUsageAnalyzerBase
{
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);
            context.RegisterSymbolAction(analyzerContext.AnalyzeGeneratedRegexSymbol, SymbolKind.Method, SymbolKind.Property);
        });
    }


}
