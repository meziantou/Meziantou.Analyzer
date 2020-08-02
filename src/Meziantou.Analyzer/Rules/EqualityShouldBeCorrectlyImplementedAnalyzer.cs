using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EqualityShouldBeCorrectlyImplementedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_implementIEquatableRule = new DiagnosticDescriptor(
            RuleIdentifiers.ClassWithEqualsTShouldImplementIEquatableT,
            title: "A class that provides Equals(T) should implement IEquatable<T>",
            messageFormat: "A class that provides Equals(T) should implement IEquatable<T>",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ClassWithEqualsTShouldImplementIEquatableT));

        private static readonly DiagnosticDescriptor s_implementIComparableOfTRule = new DiagnosticDescriptor(
            RuleIdentifiers.ClassWithCompareToTShouldImplementIComparableT,
            title: "A class that provides CompareTo(T) should implement IComparable<T>",
            messageFormat: "A class that provides CompareTo(T) should implement IComparable<T>",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ClassWithCompareToTShouldImplementIComparableT));

        private static readonly DiagnosticDescriptor s_overrideEqualsObjectRule = new DiagnosticDescriptor(
            RuleIdentifiers.ClassWithEqualsTShouldOverrideEqualsObject,
            title: "A class that implements IEquatable<T> should override Equals(object)",
            messageFormat: "A class that implements IEquatable<T> should override Equals(object)",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ClassWithEqualsTShouldOverrideEqualsObject));

        private static readonly DiagnosticDescriptor s_implementIEquatableWhenIComparableRule = new DiagnosticDescriptor(
            RuleIdentifiers.ClassImplementingIComparableTShouldImplementIEquatableT,
            title: "A class that implements IComparable<T> should also implement IEquatable<T>",
            messageFormat: "A class that implements IComparable<T> should also implement IEquatable<T>",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ClassImplementingIComparableTShouldImplementIEquatableT));

        private static readonly DiagnosticDescriptor s_addComparisonRule = new DiagnosticDescriptor(
            RuleIdentifiers.TheComparisonOperatorsShouldBeOverriddenWhenImplementingIComparable,
            title: "A class that implements IComparable<T> or IComparable should override comparison operators",
            messageFormat: "A class that implements IComparable<T> or IComparable should override comparison operators",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.TheComparisonOperatorsShouldBeOverriddenWhenImplementingIComparable));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            s_implementIEquatableWhenIComparableRule,
            s_overrideEqualsObjectRule,
            s_implementIEquatableRule,
            s_implementIComparableOfTRule,
            s_addComparisonRule);

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

        private sealed class AnalyzerContext
        {
            public AnalyzerContext(Compilation compilation)
            {
                IComparableSymbol = compilation.GetTypeByMetadataName("System.IComparable");
                IComparableOfTSymbol = compilation.GetTypeByMetadataName("System.IComparable`1");
                IEquatableOfTSymbol = compilation.GetTypeByMetadataName("System.IEquatable`1");
            }

            public INamedTypeSymbol? IComparableSymbol { get; set; }
            public INamedTypeSymbol? IComparableOfTSymbol { get; set; }
            public INamedTypeSymbol? IEquatableOfTSymbol { get; set; }

            public void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                var symbol = (INamedTypeSymbol)context.Symbol;
                if (symbol.TypeKind != TypeKind.Class && symbol.TypeKind != TypeKind.Structure)
                    return;

                var implementIComparable = false;
                var implementIComparableOfT = false;
                var implementIEquatableOfT = false;
                foreach (var implementedInterface in symbol.AllInterfaces)
                {
                    if (implementedInterface.IsEqualTo(IComparableSymbol))
                    {
                        implementIComparable = true;
                    }
                    else if (IComparableOfTSymbol != null && implementedInterface.IsEqualTo(IComparableOfTSymbol.Construct(symbol)))
                    {
                        implementIComparableOfT = true;
                    }
                    else if (IEquatableOfTSymbol != null && implementedInterface.IsEqualTo(IEquatableOfTSymbol.Construct(symbol)))
                    {
                        implementIEquatableOfT = true;
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
                    context.ReportDiagnostic(s_implementIEquatableWhenIComparableRule, symbol);
                }

                // IEquatable<T> without Equals(object)
                if (implementIEquatableOfT && !HasMethod(symbol, IsEqualsMethod))
                {
                    context.ReportDiagnostic(s_overrideEqualsObjectRule, symbol);
                }

                // Equals(T) without IEquatable<T>
                if (!implementIEquatableOfT && HasMethod(symbol, IsEqualsOfTMethod))
                {
                    context.ReportDiagnostic(s_implementIEquatableRule, symbol);
                }

                // CompareTo(T) without IComparable<T>
                if (!implementIComparableOfT && HasMethod(symbol, IsCompareToOfTMethod))
                {
                    context.ReportDiagnostic(s_implementIComparableOfTRule, symbol);
                }

                // IComparable/IComparable<T> without operators
                if ((implementIComparable || implementIComparableOfT) && !HasComparisonOperator(symbol))
                {
                    context.ReportDiagnostic(s_addComparisonRule, symbol);
                }
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

            foreach (var member in parentType.GetMembers().OfType<IMethodSymbol>())
            {
                if (member.MethodKind == MethodKind.UserDefinedOperator)
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

        internal static bool IsEqualsOfTMethod(IMethodSymbol symbol)
        {
            return symbol.Name == nameof(object.Equals) &&
            symbol.ReturnType.IsBoolean() &&
            symbol.Parameters.Length == 1 &&
            symbol.Parameters[0].Type.IsEqualTo(symbol.ContainingType) &&
            symbol.DeclaredAccessibility == Accessibility.Public &&
            !symbol.IsStatic;
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
