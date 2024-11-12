using System.Collections.Generic;
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ThrowIfNullWithNonNullableInstanceAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ThrowIfNullWithNonNullableInstance,
        title: "ArgumentNullException.ThrowIfNull should not be used with non-nullable types",
        messageFormat: "ArgumentNullException.ThrowIfNull should not be used with non-nullable types",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ThrowIfNullWithNonNullableInstance));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var nullableSymbol = context.Compilation.GetBestTypeByMetadataName("System.Nullable`1");
            var symbol = context.Compilation.GetBestTypeByMetadataName("System.ArgumentNullException");
            if (symbol is null)
                return;

            var members = symbol.GetMembers("ThrowIfNull");
            if (members.Length == 0)
                return;

            var memberHashSet = new HashSet<ISymbol>(members, SymbolEqualityComparer.Default);

            context.RegisterOperationAction(context =>
            {
                var operation = (IInvocationOperation)context.Operation;
                if (operation.Arguments.Length == 0)
                    return;

                if (!memberHashSet.Contains(operation.TargetMethod))
                    return;

                var instance = operation.Arguments[0].Value;
                var type = instance.GetActualType();
                if (type is null)
                    return;

                // Generic type (T)
                if (type.TypeKind is TypeKind.TypeParameter)
                    return;

                if (type.IsReferenceType)
                    return;

                // void*
                if (type.TypeKind is TypeKind.Pointer)
                    return;

                if (type.SpecialType is SpecialType.System_IntPtr or SpecialType.System_UIntPtr)
                    return;

                if (type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
                    return;

                context.ReportDiagnostic(Rule, operation);
            }, OperationKind.Invocation);
        });
    }
}
