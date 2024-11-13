using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotDeclareStaticMembersOnGenericTypes : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotDeclareStaticMembersOnGenericTypes,
        title: "Do not declare static members on generic types (deprecated; use CA1000 instead)",
        messageFormat: "Do not declare static members on generic types (deprecated; use CA1000 instead)",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotDeclareStaticMembersOnGenericTypes));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (!symbol.IsGenericType)
            return;

        foreach (var member in symbol.GetMembers())
        {
            if (member.IsStatic && !member.IsConst())
            {
                if (member.IsAbstract || member.IsVirtual)
                    continue;

                // skip properties
                if (member is IMethodSymbol method && (method.MethodKind == MethodKind.PropertyGet || method.MethodKind == MethodKind.PropertySet))
                    continue;

                // skip operators
                if (member.IsOperator())
                    continue;

                // only public methods
                if (!member.IsVisibleOutsideOfAssembly())
                    continue;

                // Exclude protected member as the usage is easy from a derived class
                if (member.DeclaredAccessibility == Accessibility.Protected || member.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
                    continue;

                context.ReportDiagnostic(Rule, member);
            }
        }
    }
}
