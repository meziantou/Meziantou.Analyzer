using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class EqualityShouldBeCorrectlyImplementedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor ImplementIEquatableRule = new(
        RuleIdentifiers.ClassWithEqualsTShouldImplementIEquatableT,
        title: "A class that provides Equals(T) should implement IEquatable<T>",
        messageFormat: "A class that provides Equals(T) should implement IEquatable<T>",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ClassWithEqualsTShouldImplementIEquatableT));

    private static readonly DiagnosticDescriptor ImplementIComparableOfTRule = new(
        RuleIdentifiers.ClassWithCompareToTShouldImplementIComparableT,
        title: "A class that provides CompareTo(T) should implement IComparable<T>",
        messageFormat: "A class that provides CompareTo(T) should implement IComparable<T>",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ClassWithCompareToTShouldImplementIComparableT));

    private static readonly DiagnosticDescriptor OverrideEqualsObjectRule = new(
        RuleIdentifiers.ClassWithEqualsTShouldOverrideEqualsObject,
        title: "A class that implements IEquatable<T> should override Equals(object)",
        messageFormat: "A class that implements IEquatable<T> should override Equals(object)",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ClassWithEqualsTShouldOverrideEqualsObject));

    private static readonly DiagnosticDescriptor ImplementIEquatableWhenIComparableRule = new(
        RuleIdentifiers.ClassImplementingIComparableTShouldImplementIEquatableT,
        title: "A class that implements IComparable<T> should also implement IEquatable<T>",
        messageFormat: "A class that implements IComparable<T> should also implement IEquatable<T>",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ClassImplementingIComparableTShouldImplementIEquatableT));

    private static readonly DiagnosticDescriptor AddComparisonRule = new(
        RuleIdentifiers.TheComparisonOperatorsShouldBeOverriddenWhenImplementingIComparable,
        title: "A class that implements IComparable<T> or IComparable should override comparison operators",
        messageFormat: "A class that implements IComparable<T> or IComparable should override comparison operators",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.TheComparisonOperatorsShouldBeOverriddenWhenImplementingIComparable));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        ImplementIEquatableWhenIComparableRule,
        OverrideEqualsObjectRule,
        ImplementIEquatableRule,
        ImplementIComparableOfTRule,
        AddComparisonRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            ctx.RegisterSymbolAction(analyzerContext.AnalyzeSymbol, SymbolKind.NamedType);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        public IMethodSymbol? ObjectEqualsSymbol { get; set; } = compilation.GetSpecialType(SpecialType.System_Object).GetMembers("Equals").FirstOrDefault() as IMethodSymbol;
        public IMethodSymbol? ValueTypeEqualsSymbol { get; set; } = compilation.GetSpecialType(SpecialType.System_ValueType).GetMembers("Equals").FirstOrDefault() as IMethodSymbol;

        public INamedTypeSymbol? IComparableSymbol { get; set; } = compilation.GetBestTypeByMetadataName("System.IComparable");
        public INamedTypeSymbol? IComparableOfTSymbol { get; set; } = compilation.GetBestTypeByMetadataName("System.IComparable`1");
        public INamedTypeSymbol? IEquatableOfTSymbol { get; set; } = compilation.GetBestTypeByMetadataName("System.IEquatable`1");
        public bool SupportRefStructs { get; } = compilation.GetCSharpLanguageVersion().IsCSharp13OrAbove() && compilation.IsNet9OrGreater();

        public void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (symbol.TypeKind is not TypeKind.Class and not TypeKind.Structure)
                return;

            if (symbol.TypeKind is TypeKind.Struct && symbol.IsRefLikeType && !SupportRefStructs)
                return;

            var implementIComparable = false;
            var implementIComparableOfT = false;
            var implementIEquatableOfT = false;
            var directlyImplementIEquatableOfT = false;
            foreach (var implementedInterface in symbol.AllInterfaces)
            {
                if (implementedInterface.IsEqualTo(IComparableSymbol))
                {
                    implementIComparable = true;
                }
                else if (IComparableOfTSymbol is not null && implementedInterface.IsEqualTo(IComparableOfTSymbol.Construct(symbol)))
                {
                    implementIComparableOfT = true;
                }
                else if (IEquatableOfTSymbol is not null && implementedInterface.IsEqualTo(IEquatableOfTSymbol.Construct(symbol)))
                {
                    implementIEquatableOfT = true;
                }
            }

            // Check if the type directly implements IEquatable<T> (not inherited from base class)
            foreach (var implementedInterface in symbol.Interfaces)
            {
                if (IEquatableOfTSymbol is not null && implementedInterface.IsEqualTo(IEquatableOfTSymbol.Construct(symbol)))
                {
                    directlyImplementIEquatableOfT = true;
                    break;
                }
            }

            // IComparable without IComparable<T>
            if (implementIComparable && !implementIComparableOfT)
            {
                // TODO-design report?
            }

            // IComparable<T> without IEquatable<T>
            if (implementIComparableOfT && !implementIEquatableOfT)
            {
                context.ReportDiagnostic(ImplementIEquatableWhenIComparableRule, symbol);
            }

            // IEquatable<T> without Equals(object)
            // Only report if directly implemented (not inherited via CRTP - Curiously Recurring Template Pattern)
            // Check the entire type hierarchy for an Equals(object) override
            if (directlyImplementIEquatableOfT && !symbol.GetMembers().OfType<IMethodSymbol>().Any(IsEqualsMethodOverride))
            {
                context.ReportDiagnostic(OverrideEqualsObjectRule, symbol);
            }

            // Equals(T) without IEquatable<T>
            if (!implementIEquatableOfT && HasMethod(symbol, EqualityShouldBeCorrectlyImplementedAnalyzerCommon.IsEqualsOfTMethod))
            {
                context.ReportDiagnostic(ImplementIEquatableRule, symbol);
            }

            // CompareTo(T) without IComparable<T>
            if (!implementIComparableOfT && HasMethod(symbol, IsCompareToOfTMethod))
            {
                context.ReportDiagnostic(ImplementIComparableOfTRule, symbol);
            }

            // IComparable/IComparable<T> without operators
            if ((implementIComparable || implementIComparableOfT) && !HasComparisonOperator(symbol))
            {
                context.ReportDiagnostic(AddComparisonRule, symbol);
            }
        }

        private static bool HasMethod(INamedTypeSymbol parentType, Func<IMethodSymbol, bool> predicate)
        {
            foreach (var member in parentType.GetMembers().OfType<IMethodSymbol>())
            {
                if (predicate(member))
                    return true;
            }

            return false;
        }

        private static bool HasMethodInHierarchy(INamedTypeSymbol type, Func<IMethodSymbol, bool> predicate)
        {
            foreach (var member in type.GetAllMembers().OfType<IMethodSymbol>())
            {
                if (predicate(member))
                    return true;
            }

            return false;
        }

        private static bool HasComparisonOperator(INamedTypeSymbol parentType)
        {
            var operatorNames = new List<string>(6)
        {
            "op_LessThan",
            "op_LessThanOrEqual",
            "op_GreaterThan",
            "op_GreaterThanOrEqual",
            "op_Equality",
            "op_Inequality",
        };

            foreach (var member in parentType.GetAllMembers().OfType<IMethodSymbol>())
            {
                if (member.MethodKind is MethodKind.UserDefinedOperator)
                {
                    operatorNames.Remove(member.Name);
                }
            }

            return operatorNames.Count == 0;
        }

        private static bool IsEqualsMethod(IMethodSymbol symbol)
        {
            return symbol.Name == nameof(object.Equals) &&
            symbol.ReturnType.IsBoolean() &&
            symbol.Parameters.Length == 1 &&
            symbol.Parameters[0].Type.IsObject() &&
            symbol.DeclaredAccessibility == Accessibility.Public &&
            !symbol.IsStatic;
        }

        private bool IsEqualsMethodOverride(IMethodSymbol symbol)
        {
            // Check if it's an Equals(object) method AND it's overridden (not the base System.Object method)
            return symbol.Name == nameof(object.Equals) &&
                   symbol.ReturnType.IsBoolean() &&
                   symbol.Parameters.Length == 1 &&
                   symbol.Parameters[0].Type.IsObject() &&
                   symbol.DeclaredAccessibility == Accessibility.Public &&
                   !symbol.IsStatic &&
                   !symbol.IsEqualTo(ObjectEqualsSymbol) &&
                   !symbol.IsEqualTo(ValueTypeEqualsSymbol);
        }

        private static bool IsCompareToMethod(IMethodSymbol symbol)
        {
            return symbol.Name == nameof(IComparable.CompareTo) &&
            symbol.ReturnType.IsInt32() &&
            symbol.Parameters.Length == 1 &&
            symbol.Parameters[0].Type.IsObject() &&
            symbol.DeclaredAccessibility == Accessibility.Public &&
            !symbol.IsStatic;
        }

        private static bool IsCompareToOfTMethod(IMethodSymbol symbol)
        {
            return symbol.Name == nameof(IComparable.CompareTo) &&
            symbol.ReturnType.IsInt32() &&
            symbol.Parameters.Length == 1 &&
            symbol.Parameters[0].Type.IsEqualTo(symbol.ContainingType) &&
            symbol.DeclaredAccessibility == Accessibility.Public &&
            !symbol.IsStatic;
        }
    }
}