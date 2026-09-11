using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RemoveUnnecessaryPartialModifierAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.RemoveUnnecessaryPartialModifier,
        title: "Remove unnecessary partial modifier",
        messageFormat: "Remove unnecessary partial modifier",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.RemoveUnnecessaryPartialModifier));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.Analyze);

        context.RegisterCompilationStartAction(context =>
        {
            var excludedBaseTypes = ImmutableArray.Create(
                context.Compilation.GetBestTypeByMetadataName("System.Windows.Controls.UserControl"),
                context.Compilation.GetBestTypeByMetadataName("System.Windows.Controls.Page"),
                context.Compilation.GetBestTypeByMetadataName("System.Windows.Window"),
                context.Compilation.GetBestTypeByMetadataName("System.Windows.Application"));
            var csWinRTCustomMappedInterfaces = ImmutableArray.Create(
                context.Compilation.GetBestTypeByMetadataName("System.IDisposable"),
                context.Compilation.GetBestTypeByMetadataName("System.IServiceProvider"),
                context.Compilation.GetBestTypeByMetadataName("System.Collections.IEnumerable"),
                context.Compilation.GetBestTypeByMetadataName("System.Collections.IList"),
                context.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IEnumerable`1"),
                context.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IEnumerator`1"),
                context.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IList`1"),
                context.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IDictionary`2"),
                context.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1"),
                context.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IReadOnlyDictionary`2"),
                context.Compilation.GetBestTypeByMetadataName("System.Collections.Specialized.INotifyCollectionChanged"),
                context.Compilation.GetBestTypeByMetadataName("System.ComponentModel.INotifyDataErrorInfo"),
                context.Compilation.GetBestTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged"),
                context.Compilation.GetBestTypeByMetadataName("System.Windows.Input.ICommand"));
            var hasCsWinRTAotSupport =
                context.Compilation.GetBestTypeByMetadataName("WinRT.WindowsRuntimeTypeAttribute") is not null &&
                context.Compilation.GetBestTypeByMetadataName("WinRT.GeneratedBindableCustomPropertyAttribute") is not null &&
                context.Compilation.GetBestTypeByMetadataName("WinRT.GeneratedWinRTExposedTypeAttribute") is not null &&
                context.Compilation.GetBestTypeByMetadataName("WinRT.WinRTExposedTypeAttribute") is not null;
            var isMauiCompilation =
                context.Compilation.GetBestTypeByMetadataName("Microsoft.Maui.Controls.Application") is not null &&
                context.Compilation.GetBestTypeByMetadataName("Microsoft.Maui.Controls.BindableObject") is not null;

            context.RegisterSymbolAction(context => AnalyzeNamedTypeSymbol(context, excludedBaseTypes, csWinRTCustomMappedInterfaces, hasCsWinRTAotSupport, isMauiCompilation), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context, ImmutableArray<INamedTypeSymbol?> excludedBaseTypes, ImmutableArray<INamedTypeSymbol?> csWinRTCustomMappedInterfaces, bool hasCsWinRTAotSupport, bool isMauiCompilation)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.TypeKind is not (TypeKind.Class or TypeKind.Struct or TypeKind.Interface))
            return;

        if (symbol.DeclaringSyntaxReferences.Length != 1)
            return;

        var typeDeclaration = symbol.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken) as TypeDeclarationSyntax;
        if (typeDeclaration is null)
            return;

        var partialToken = typeDeclaration.Modifiers.FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
        if (partialToken == default)
            return;

        if (InheritsFromExcludedType(symbol, excludedBaseTypes))
            return;

        if (RequiresPartialForCsWinRT(symbol, csWinRTCustomMappedInterfaces, hasCsWinRTAotSupport, isMauiCompilation))
            return;

        context.ReportDiagnostic(Rule, partialToken.GetLocation());
    }

    private static bool InheritsFromExcludedType(INamedTypeSymbol symbol, ImmutableArray<INamedTypeSymbol?> excludedBaseTypes)
    {
        foreach (var excludedType in excludedBaseTypes)
        {
            if (symbol.InheritsFrom(excludedType))
                return true;
        }

        return false;
    }

    private static bool RequiresPartialForCsWinRT(INamedTypeSymbol symbol, ImmutableArray<INamedTypeSymbol?> csWinRTCustomMappedInterfaces, bool hasCsWinRTAotSupport, bool isMauiCompilation)
    {
        if (!hasCsWinRTAotSupport && !isMauiCompilation)
            return false;

        return ContainsTypeRequiringCsWinRTPartial(symbol, csWinRTCustomMappedInterfaces);
    }

    private static bool ContainsTypeRequiringCsWinRTPartial(INamedTypeSymbol symbol, ImmutableArray<INamedTypeSymbol?> csWinRTCustomMappedInterfaces)
    {
        if (IsTypeRequiringCsWinRTPartial(symbol, csWinRTCustomMappedInterfaces))
            return true;

        foreach (var nestedType in symbol.GetTypeMembers())
        {
            if (ContainsTypeRequiringCsWinRTPartial(nestedType, csWinRTCustomMappedInterfaces))
                return true;
        }

        return false;
    }

    private static bool IsTypeRequiringCsWinRTPartial(INamedTypeSymbol symbol, ImmutableArray<INamedTypeSymbol?> csWinRTCustomMappedInterfaces)
    {
        if (symbol.TypeKind != TypeKind.Class || symbol.IsAbstract || symbol.IsStatic)
            return false;

        foreach (var interfaceType in symbol.AllInterfaces)
        {
            if (IsCsWinRTCustomMappedInterface(interfaceType, csWinRTCustomMappedInterfaces))
                return true;
        }

        return false;
    }

    private static bool IsCsWinRTCustomMappedInterface(INamedTypeSymbol interfaceType, ImmutableArray<INamedTypeSymbol?> csWinRTCustomMappedInterfaces)
    {
        foreach (var csWinRTCustomMappedInterface in csWinRTCustomMappedInterfaces)
        {
            if (csWinRTCustomMappedInterface is not null &&
                SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, csWinRTCustomMappedInterface))
            {
                return true;
            }
        }

        return false;
    }
}
