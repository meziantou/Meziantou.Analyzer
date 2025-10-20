using System.Collections.Immutable;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UsePatternMatchingInsteadOfHasValueAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UsePatternMatchingInsteadOfHasvalue,
        title: "Use pattern matching instead of HasValue for Nullable<T> check",
        messageFormat: "Use pattern matching instead of HasValue for Nullable<T> check",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: null,
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UsePatternMatchingInsteadOfHasvalue));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var tree = context.Compilation.SyntaxTrees.FirstOrDefault();
            if (tree is null)
                return;

            if (tree.GetCSharpLanguageVersion() < Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8)
                return;

            var analyzerContext = new AnalyzerContext(context.Compilation);
            context.RegisterOperationAction(analyzerContext.AnalyzeHasValue, OperationKind.PropertyReference);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly OperationUtilities _operationUtilities = new(compilation);
        private readonly ISymbol? _nullableSymbol = compilation.GetBestTypeByMetadataName("System.Nullable`1");

        public void AnalyzeHasValue(OperationAnalysisContext context)
        {
            var propertyReference = (IPropertyReferenceOperation)context.Operation;
            if (propertyReference.Property.Name is "HasValue" && propertyReference.Property.ContainingType.ConstructedFrom.IsEqualTo(_nullableSymbol))
            {
                if (_operationUtilities.IsInExpressionContext(propertyReference))
                    return;

                context.ReportDiagnostic(Rule, propertyReference);
            }
        }
    }
}
