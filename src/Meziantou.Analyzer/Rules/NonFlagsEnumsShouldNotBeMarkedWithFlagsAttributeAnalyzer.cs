using System.Collections.Immutable;
using Meziantou.Analyzer.Configurations;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonFlagsEnumsShouldNotBeMarkedWithFlagsAttributeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.NonFlagsEnumsShouldNotBeMarkedWithFlagsAttribute,
        title: "Non-flags enums should not be marked with \"FlagsAttribute\"",
        messageFormat: "Non-flags enums should not be marked with \"FlagsAttribute\" ({0} is not a power of two or a combinaison of other values)",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.NonFlagsEnumsShouldNotBeMarkedWithFlagsAttribute));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.EnumUnderlyingType is null)
            return;

        if (!symbol.HasAttribute(context.Compilation.GetBestTypeByMetadataName("System.FlagsAttribute")))
            return;

        if (!symbol.GetMembers().OfType<IFieldSymbol>().All(member => member.HasConstantValue && member.ConstantValue is not null))
            return; // I cannot reproduce this case, but it was reported by some users.

        var members = symbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(member => member.ConstantValue is not null)
            .Select(member => (member, IsSingleBitSet: IsSingleBitSet(member.ConstantValue), IsZero: NumericHelpers.IsZero(member.ConstantValue)))
            .ToArray();
        foreach (var member in members)
        {
            if (member.IsSingleBitSet || member.IsZero)
                continue;

            if (IsAllBitsSet(member.member.ConstantValue) && context.Options.GetConfigurationValue(member.member, RuleIdentifiers.NonFlagsEnumsShouldNotBeMarkedWithFlagsAttribute + ".allow_all_bits_set_value", defaultValue: false))
                continue;

            var value = member.member.ConstantValue!;
            foreach (var otherMember in members)
            {
                if (!otherMember.IsSingleBitSet)
                    continue;

                if (otherMember.member.ConstantValue is not null)
                {
                    value = RemoveValue(value, otherMember.member.ConstantValue);
                }
            }

            if (!NumericHelpers.IsZero(value))
            {
                context.ReportDiagnostic(Rule, symbol, member.member.Name);
                return;
            }
        }
    }

    private static bool IsSingleBitSet(object? o)
    {
        // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum?WT.mc_id=DT-MVP-5003978
        // The approved types for an enum are byte, sbyte, short, ushort, int, uint, long, or ulong.
        return o switch
        {
            null => false,
            sbyte x => IsSingleBitSet((byte)x),
            byte x => x > 0 && (x & (x - 1)) == 0,
            short x => IsSingleBitSet((ushort)x),
            ushort x => x > 0 && (x & (x - 1)) == 0,
            int x => IsSingleBitSet((uint)x),
            uint x => x > 0 && (x & (x - 1)) == 0,
            long x => IsSingleBitSet((ulong)x),
            ulong x => x > 0 && (x & (x - 1)) == 0,
            _ => throw new ArgumentOutOfRangeException(nameof(o), $"Type {o.GetType().FullName} is not supported"),
        };
    }



    private static bool IsAllBitsSet(object? o)
    {
        // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum?WT.mc_id=DT-MVP-5003978
        // The approved types for an enum are byte, sbyte, short, ushort, int, uint, long, or ulong.
        return o switch
        {
            null => false,
            sbyte x => x == -1,
            byte x => x == 0xFF,
            short x => x == -1,
            ushort x => x == 0xFFFF,
            int x => x == -1,
            uint x => x == 0xFFFF_FFFF,
            long x => x == -1,
            ulong x => x == 0xFFFF_FFFF_FFFF_FFFF,
            _ => throw new ArgumentOutOfRangeException(nameof(o), $"Type {o.GetType().FullName} is not supported"),
        };
    }

    [SuppressMessage("Style", "IDE0004:Remove Unnecessary Cast", Justification = "Clearer")]
    private static object RemoveValue(object o, object valueToRemove)
    {
        // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum?WT.mc_id=DT-MVP-5003978
        // The approved types for an enum are byte, sbyte, short, ushort, int, uint, long, or ulong.
        return o switch
        {
            null => throw new ArgumentOutOfRangeException(nameof(o), "null is not a valid value"),
            byte x => (byte)(x & (~(byte)valueToRemove)),
            sbyte x => (sbyte)(x & (~(sbyte)valueToRemove)),
            short x => (short)(x & (~(short)valueToRemove)),
            ushort x => (ushort)(x & (~(ushort)valueToRemove)),
            int x => (int)(x & (~(int)valueToRemove)),
            uint x => (uint)(x & (~(uint)valueToRemove)),
            long x => (long)(x & (~(long)valueToRemove)),
            ulong x => (object)(ulong)(x & (~(ulong)valueToRemove)),
            _ => throw new ArgumentOutOfRangeException(nameof(o), $"Type {o.GetType().FullName} is not supported"),
        };
    }
}
