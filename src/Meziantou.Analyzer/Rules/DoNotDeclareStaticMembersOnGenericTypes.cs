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
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

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

                // The accessors are reported through the property or event they belong to
                if (member is IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove })
                    continue;

                // skip operators
                if (member.IsOperator())
                    continue;

                // only public methods
                if (!member.IsVisibleOutsideOfAssembly())
                    continue;

                // Exclude protected member as the usage is easy from a derived class
                if (member.DeclaredAccessibility is Accessibility.Protected or Accessibility.ProtectedOrInternal)
                    continue;

                context.ReportDiagnostic(Rule, member);
            }
        }
    }
}
