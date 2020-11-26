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
        private static readonly DiagnosticDescriptor s_rule = new(
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

            if (!symbol.GetMembers().OfType<IFieldSymbol>().All(member => member.HasConstantValue && member.ConstantValue != null))
                return; // I cannot reproduce this case, but it was reported by some users.

            var members = symbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(member => member.ConstantValue != null)
                .Select(member => (member, IsPowerOfTwo: IsPowerOfTwo(member.ConstantValue!)))
                .ToList();
            foreach (var member in members.Where(member => !member.IsPowerOfTwo))
            {
                var value = member.member.ConstantValue;
                if (value != null)
                {
                    foreach (var powerOfTwo in members.Where(member => member.IsPowerOfTwo))
                    {
                        if (powerOfTwo.member.ConstantValue != null)
                        {
                            value = RemoveValue(value, powerOfTwo.member.ConstantValue);
                        }
                    }

                    if (!IsZero(value))
                    {
                        context.ReportDiagnostic(s_rule, symbol, member.member.Name);
                        return;
                    }
                }
            }
        }

        private static bool IsPowerOfTwo(object o)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum
            // The approved types for an enum are byte, sbyte, short, ushort, int, uint, long, or ulong.
            return o switch
            {
                null => throw new ArgumentOutOfRangeException(nameof(o), "null is not a valid value"),
                byte x => (x == 0) || ((x & (x - 1)) == 0),
                sbyte x => (x == 0) || ((x & (x - 1)) == 0),
                short x => (x == 0) || ((x & (x - 1)) == 0),
                ushort x => (x == 0) || ((x & (x - 1)) == 0),
                int x => (x == 0) || ((x & (x - 1)) == 0),
                uint x => (x == 0) || ((x & (x - 1)) == 0),
                long x => (x == 0) || ((x & (x - 1)) == 0),
                ulong x => (x == 0) || ((x & (x - 1)) == 0),
                _ => throw new ArgumentOutOfRangeException(nameof(o), $"Type {o.GetType().FullName} is not supported"),
            };
        }

        private static bool IsZero(object o)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum
            // The approved types for an enum are byte, sbyte, short, ushort, int, uint, long, or ulong.
            return o switch
            {
                null => throw new ArgumentOutOfRangeException(nameof(o), "null is not a valid value"),
                byte x => x == 0,
                sbyte x => x == 0,
                short x => x == 0,
                ushort x => x == 0,
                int x => x == 0,
                uint x => x == 0,
                long x => x == 0,
                ulong x => x == 0,
                _ => throw new ArgumentOutOfRangeException(nameof(o), $"Type {o.GetType().FullName} is not supported"),
            };
        }

        private static object RemoveValue(object o, object valueToRemove)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum
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
}
