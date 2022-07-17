using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptimizeStartsWithAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.OptimizeStartsWith,
        title: "Optimize string method usage",
        messageFormat: "Replace string.{0}(\"{1}\") with string.{0}('{1}')",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeStartsWith));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.IsValid)
            {
                ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            }
        });
    }

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            var stringComparisonSymbol = compilation.GetBestTypeByMetadataName("System.StringComparison");
            if (stringComparisonSymbol != null)
            {
                StringComparison_Ordinal = stringComparisonSymbol.GetMembers(nameof(StringComparison.Ordinal)).FirstOrDefault();
                StringComparison_CurrentCulture = stringComparisonSymbol.GetMembers(nameof(StringComparison.CurrentCulture)).FirstOrDefault();
                StringComparison_InvariantCulture = stringComparisonSymbol.GetMembers(nameof(StringComparison.InvariantCulture)).FirstOrDefault();
            }

            // StartsWith methods
            var stringSymbol = compilation.GetSpecialType(SpecialType.System_String);
            if (stringSymbol != null)
            {
                var startsWithMethods = stringSymbol.GetMembers(nameof(string.StartsWith));
                foreach (var method in startsWithMethods.OfType<IMethodSymbol>())
                {
                    if (!method.IsStatic && method.Parameters.Length == 1 && method.Parameters[0].Type.IsChar())
                    {
                        StartsWith_Char = method;
                    }
                    else if (!method.IsStatic && method.Parameters.Length == 1 && method.Parameters[0].Type.IsString())
                    {
                        StartsWith_String = method;
                    }
                    else if (!method.IsStatic && method.Parameters.Length == 2 && method.Parameters[0].Type.IsString() && method.Parameters[1].Type.IsEqualTo(stringComparisonSymbol))
                    {
                        StartsWith_String_StringComparison = method;
                    }
                }

                var endsWithMethods = stringSymbol.GetMembers(nameof(string.EndsWith));
                foreach (var method in endsWithMethods.OfType<IMethodSymbol>())
                {
                    if (!method.IsStatic && method.Parameters.Length == 1 && method.Parameters[0].Type.IsChar())
                    {
                        EndsWith_Char = method;
                    }
                    else if (!method.IsStatic && method.Parameters.Length == 1 && method.Parameters[0].Type.IsString())
                    {
                        EndsWith_String = method;
                    }
                    else if (!method.IsStatic && method.Parameters.Length == 2 && method.Parameters[0].Type.IsString() && method.Parameters[1].Type.IsEqualTo(stringComparisonSymbol))
                    {
                        EndsWith_String_StringComparison = method;
                    }
                }
            }
        }

        public IMethodSymbol? StartsWith_Char { get; set; }
        public IMethodSymbol? StartsWith_String { get; set; }
        public IMethodSymbol? StartsWith_String_StringComparison { get; set; }

        public IMethodSymbol? EndsWith_Char { get; set; }
        public IMethodSymbol? EndsWith_String { get; set; }
        public IMethodSymbol? EndsWith_String_StringComparison { get; set; }

        public ISymbol? StringComparison_Ordinal { get; set; }
        public ISymbol? StringComparison_CurrentCulture { get; set; }
        public ISymbol? StringComparison_InvariantCulture { get; set; }

        public bool IsValid => StartsWith_Char != null || EndsWith_Char != null;

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            // StartsWith("a");
            // StartsWith("a", StringComparison.Ordinal);
            // StartsWith("a", StringComparison.CurrentCulture);
            var operation = (IInvocationOperation)context.Operation;
            if (operation.TargetMethod.IsEqualTo(StartsWith_Char) || operation.TargetMethod.IsEqualTo(EndsWith_Char))
                return;

            if (operation.TargetMethod.IsEqualTo(StartsWith_String) || operation.TargetMethod.IsEqualTo(StartsWith_String_StringComparison) ||
                operation.TargetMethod.IsEqualTo(EndsWith_String) || operation.TargetMethod.IsEqualTo(EndsWith_String_StringComparison))
            {
                if (operation.Arguments.Length == 0)
                    return;

                if (operation.Arguments[0].Value.ConstantValue.Value is not string prefix || prefix.Length != 1)
                    return;

                // Ensure the StringComparison is compatible
                if (operation.Arguments.Length == 2)
                {
                    if (operation.Arguments[1].Value is not IMemberReferenceOperation argumentValue)
                        return;

                    if (!argumentValue.Member.IsEqualTo(StringComparison_Ordinal) &&
                        !argumentValue.Member.IsEqualTo(StringComparison_CurrentCulture) &&
                        !argumentValue.Member.IsEqualTo(StringComparison_InvariantCulture))
                    {
                        return;
                    }
                }

                context.ReportDiagnostic(s_rule, operation.Arguments[0], operation.TargetMethod.Name, prefix);
            }
        }
    }
}
