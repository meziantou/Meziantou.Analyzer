namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStructLayoutAttributeAnalyzer : DiagnosticAnalyzer
{
    // The fields of an invalid struct can cycle back to the struct (CS0523), directly or through other types,
    // and the generic structs can create an infinite number of types (struct S<T> { S<S<T>> field; }).
    // The depth is limited, so these types are not blittable instead of overflowing the stack.
    private const int MaxBlittableTypeDepth = 32;

    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.MissingStructLayoutAttribute,
        title: "Add StructLayoutAttribute",
        messageFormat: "Add StructLayoutAttribute",
        RuleCategories.Performance,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingStructLayoutAttribute));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var attributeType = compilationContext.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
            if (attributeType is null)
                return;

            compilationContext.RegisterSymbolAction(context => Analyze(context, attributeType), SymbolKind.NamedType);
        });
    }

    private static void Analyze(SymbolAnalysisContext context, INamedTypeSymbol attributeType)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (!symbol.IsValueType || symbol.EnumUnderlyingType is not null) // Only support struct
            return;

        if (symbol.HasAttribute(attributeType))
            return;

        var memberCount = 0;
        foreach (var member in symbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.IsConst || member.IsStatic)
                continue;

            if (!IsBlittableType(member.Type, depth: 0))
                return;

            memberCount++;
        }

        if (memberCount > 1)
        {
            context.ReportDiagnostic(Rule, symbol);
        }
    }

    private static bool IsBlittableType(ITypeSymbol symbol, int depth)
    {
        if (symbol.SpecialType is SpecialType.System_Byte or SpecialType.System_SByte
            or SpecialType.System_Int16 or SpecialType.System_UInt16
            or SpecialType.System_Int32 or SpecialType.System_UInt32
            or SpecialType.System_Int64 or SpecialType.System_UInt64
            or SpecialType.System_Single or SpecialType.System_Double
            or SpecialType.System_IntPtr or SpecialType.System_UIntPtr)
        {
            return true;
        }

        if (symbol.SpecialType is not SpecialType.None)
            return false;

        if (symbol.TypeKind is TypeKind.Pointer)
            return true;

        if (symbol is not INamedTypeSymbol namedType)
            return false;

        if (namedType.EnumUnderlyingType is not null)
            return true;

        if (!namedType.IsValueType || depth >= MaxBlittableTypeDepth)
            return false;

        foreach (var member in namedType.GetMembers())
        {
            if (member is not IFieldSymbol field || field.IsConst || field.IsStatic)
                continue;

            if (!IsBlittableType(field.Type, depth + 1))
                return false;
        }

        return true;
    }
}
