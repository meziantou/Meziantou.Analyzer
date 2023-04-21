using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ThrowIfNullWithNonNullableInstanceAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.ThrowIfNullWithNonNullableInstance,
        title: "ArgumentNullException.ThrowIfNull should not be used with non-nullable types",
        messageFormat: "ArgumentNullException.ThrowIfNull should not be used with non-nullable types",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ThrowIfNullWithNonNullableInstance));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var nullableSymbol = context.Compilation.GetBestTypeByMetadataName("System.Nullable`1");
            var symbol = context.Compilation.GetBestTypeByMetadataName("System.ArgumentNullException");
            if (symbol == null)
                return;

            var members = symbol.GetMembers("ThrowIfNull");
            if (members.Length == 0)
                return;

            context.RegisterOperationAction(context =>
            {
                var operation = (IInvocationOperation)context.Operation;
                if (!operation.TargetMethod.IsStatic)
                    return;

                if (operation.Arguments.Length == 0)
                    return;

                if (!operation.TargetMethod.Parameters[0].Type.IsObject())
                    return;

                if (operation.TargetMethod.Name != "ThrowIfNull")
                    return;

                if (!operation.TargetMethod.ContainingType.IsEqualTo(symbol))
                    return;

                var instance = operation.Arguments[0].Value;
                var type = instance.GetActualType();
                if (type == null)
                    return;

                if (type.IsReferenceType)
                    return;

                if (type.SpecialType is SpecialType.System_IntPtr or SpecialType.System_UIntPtr)
                    return;

                if (type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
                    return;

                context.ReportDiagnostic(s_rule, operation);
            }, OperationKind.Invocation);
        });
    }
}
