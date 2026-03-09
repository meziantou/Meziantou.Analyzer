using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseTimeProviderInsteadOfInterfaceAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseTimeProviderInsteadOfInterface,
        title: "Use System.TimeProvider instead of a custom time abstraction",
        messageFormat: "Use System.TimeProvider instead of defining a custom time abstraction",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseTimeProviderInsteadOfInterface));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var dateTimeSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.DateTime");
            var dateTimeOffsetSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.DateTimeOffset");

            if (dateTimeSymbol is null && dateTimeOffsetSymbol is null)
                return;

            ctx.RegisterSymbolAction(ctx => AnalyzeNamedType(ctx, dateTimeSymbol, dateTimeOffsetSymbol), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol? dateTimeSymbol, INamedTypeSymbol? dateTimeOffsetSymbol)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind is not TypeKind.Interface)
            return;

        // Must have at least one member
        var members = type.GetMembers();
        if (members.IsEmpty)
            return;

        // All members must be time-provider-like (skip property accessor methods as they're covered by property symbols)
        var hasAtLeastOneTimeMember = false;
        foreach (var member in members)
        {
            // Skip property accessor methods - they are represented by the IPropertySymbol
            if (member is IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet })
                continue;

            if (!IsTimeProviderLikeMember(member, dateTimeSymbol, dateTimeOffsetSymbol))
                return;

            hasAtLeastOneTimeMember = true;
        }

        if (!hasAtLeastOneTimeMember)
            return;

        context.ReportDiagnostic(Rule, type);
    }

    private static bool IsTimeProviderLikeMember(ISymbol member, INamedTypeSymbol? dateTimeSymbol, INamedTypeSymbol? dateTimeOffsetSymbol)
    {
        if (member is IPropertySymbol property)
        {
            if (property.Name is not ("Now" or "UtcNow"))
                return false;

            return IsDateTimeOrDateTimeOffset(property.Type, dateTimeSymbol, dateTimeOffsetSymbol);
        }

        if (member is IMethodSymbol method)
        {
            // Exclude property accessor methods
            if (method.MethodKind is not MethodKind.Ordinary)
                return false;

            if (method.Name is not ("GetNow" or "GetUtcNow"))
                return false;

            if (!method.Parameters.IsEmpty)
                return false;

            return IsDateTimeOrDateTimeOffset(method.ReturnType, dateTimeSymbol, dateTimeOffsetSymbol);
        }

        return false;
    }

    private static bool IsDateTimeOrDateTimeOffset(ITypeSymbol type, INamedTypeSymbol? dateTimeSymbol, INamedTypeSymbol? dateTimeOffsetSymbol)
    {
        return (dateTimeSymbol is not null && type.IsEqualTo(dateTimeSymbol)) ||
               (dateTimeOffsetSymbol is not null && type.IsEqualTo(dateTimeOffsetSymbol));
    }
}
