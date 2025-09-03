using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseLazyInitializerEnsureInitializeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseLazyInitializerEnsureInitialize,
        title: "Use LazyInitializer.EnsureInitialize",
        messageFormat: "Use LazyInitializer.EnsureInitialize",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseLazyInitializerEnsureInitialize));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var interlockedType = compilationContext.Compilation.GetBestTypeByMetadataName("System.Threading.Interlocked");

            compilationContext.RegisterOperationAction(context =>
            {
                var operation = (IInvocationOperation)context.Operation;
                var targetMethod = operation.TargetMethod;

                // Interlocked.CompareExchange(ref _instance, new Sample(), null)
                if (operation.Arguments.Length is 3 && targetMethod.Name is "CompareExchange" && targetMethod.ContainingType.IsEqualTo(interlockedType))
                {
                    if (operation.Arguments[2].Value.IsNull() && operation.Arguments[1].Value is IObjectCreationOperation)
                    {
                        context.ReportDiagnostic(Rule, operation);
                    }
                }
            }, OperationKind.Invocation);
        });
    }

}
