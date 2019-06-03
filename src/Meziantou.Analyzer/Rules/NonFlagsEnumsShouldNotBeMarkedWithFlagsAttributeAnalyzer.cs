using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NonFlagsEnumsShouldNotBeMarkedWithFlagsAttributeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.NonFlagsEnumsShouldNotBeMarkedWithFlagsAttribute,
            title: "Non-flags enums should not be marked with \"FlagsAttribute\"",
            messageFormat: "Non-flags enums should not be marked with \"FlagsAttribute\" ({0} is not a power of two or a combinaison of other values)",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.NonFlagsEnumsShouldNotBeMarkedWithFlagsAttribute));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (symbol.EnumUnderlyingType == null)
                return;

            if (!symbol.HasAttribute(context.Compilation.GetTypeByMetadataName("System.FlagsAttribute")))
                return;

            var members = symbol.GetMembers().OfType<IFieldSymbol>().Select(member => (member, IsPowerOfTwo: IsPowerOfTwo(member.ConstantValue))).ToList();
            foreach (var member in members.Where(member => !member.IsPowerOfTwo))
            {
                var value = member.member.ConstantValue;
                foreach (var powerOfTwo in members.Where(member => member.IsPowerOfTwo))
                {
                    value = RemoveValue(value, powerOfTwo.member.ConstantValue);
                }

                if (!Equals(value, 0))
                {
                    context.ReportDiagnostic(s_rule, symbol, member.member.Name);
                    return;
                }
            }
        }

        private static bool IsPowerOfTwo(object o)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum
            // The approved types for an enum are byte, sbyte, short, ushort, int, uint, long, or ulong.
            switch (o)
            {
                case byte x:
                    return (x == 0) || ((x & (x - 1)) == 0);

                case sbyte x:
                    return (x == 0) || ((x & (x - 1)) == 0);

                case short x:
                    return (x == 0) || ((x & (x - 1)) == 0);

                case ushort x:
                    return (x == 0) || ((x & (x - 1)) == 0);

                case int x:
                    return (x == 0) || ((x & (x - 1)) == 0);

                case uint x:
                    return (x == 0) || ((x & (x - 1)) == 0);

                case long x:
                    return (x == 0) || ((x & (x - 1)) == 0);

                case ulong x:
                    return (x == 0) || ((x & (x - 1)) == 0);

                default:
                    throw new ArgumentOutOfRangeException(nameof(o));
            }
        }

        private static object RemoveValue(object o, object valueToRemove)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum
            // The approved types for an enum are byte, sbyte, short, ushort, int, uint, long, or ulong.
            switch (o)
            {
                case byte x:
                    return (byte)(x & (~(byte)valueToRemove));

                case sbyte x:
                    return (sbyte)(x & (~(sbyte)valueToRemove));

                case short x:
                    return (short)(x & (~(short)valueToRemove));

                case ushort x:
                    return (ushort)(x & (~(ushort)valueToRemove));

                case int x:
                    return (int)(x & (~(int)valueToRemove));

                case uint x:
                    return (uint)(x & (~(uint)valueToRemove));

                case long x:
                    return (long)(x & (~(long)valueToRemove));

                case ulong x:
                    return (ulong)(x & (~(ulong)valueToRemove));

                default:
                    throw new ArgumentOutOfRangeException(nameof(o));
            }
        }
    }
}
