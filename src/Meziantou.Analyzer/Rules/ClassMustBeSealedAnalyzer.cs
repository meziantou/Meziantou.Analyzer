using System.Collections.Immutable;
using Meziantou.Analyzer.Configurations;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ClassMustBeSealedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ClassMustBeSealed,
        title: "Make class or record sealed",
        messageFormat: "Make class or record sealed",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ClassMustBeSealed));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);

            ctx.RegisterSymbolAction(analyzerContext.AnalyzeNamedTypeSymbol, SymbolKind.NamedType);
            ctx.RegisterSymbolAction(analyzerContext.AnalyzeMethodSymbol, SymbolKind.Method);
            ctx.RegisterCompilationEndAction(analyzerContext.AnalyzeCompilationEnd);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly List<ITypeSymbol> _potentialClasses = [];
        private readonly ConcurrentHashSet<ITypeSymbol> _cannotBeSealedClasses = new(SymbolEqualityComparer.Default);

        private INamedTypeSymbol? ExceptionSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Exception");
        private INamedTypeSymbol? ComImportSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.ComImportAttribute");
        private INamedTypeSymbol? BenchmarkSymbol { get; } = compilation.GetBestTypeByMetadataName("BenchmarkDotNet.Attributes.BenchmarkAttribute");

        public void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            switch (symbol.TypeKind)
            {
                case TypeKind.Class:
                    if (IsPotentialSealed(context.Options, symbol, context.CancellationToken))
                    {
                        lock (_potentialClasses)
                        {
                            _potentialClasses.Add(symbol);
                        }
                    }

                    if (symbol.BaseType is not null)
                    {
                        lock (_cannotBeSealedClasses)
                        {
                            _cannotBeSealedClasses.Add(symbol.BaseType);
                            _cannotBeSealedClasses.Add(symbol.BaseType.OriginalDefinition);
                        }
                    }

                    break;
            }
        }

        public void AnalyzeMethodSymbol(SymbolAnalysisContext context)
        {
            var symbol = (IMethodSymbol)context.Symbol;
            if (symbol.ContainingType is not null && symbol.HasAttribute(BenchmarkSymbol))
            {
                _cannotBeSealedClasses.Add(symbol.ContainingType);
                _cannotBeSealedClasses.Add(symbol.ContainingType.OriginalDefinition);
            }
        }

        public void AnalyzeCompilationEnd(CompilationAnalysisContext context)
        {
            foreach (var @class in _potentialClasses)
            {
                if (_cannotBeSealedClasses.Contains(@class))
                    continue;

                context.ReportDiagnostic(Rule, @class);
            }
        }

        private bool IsPotentialSealed(AnalyzerOptions options, INamedTypeSymbol symbol, CancellationToken cancellationToken)
        {
            if (symbol.IsSealed || symbol.IsAbstract || symbol.IsStatic || symbol.IsValueType)
                return false;

            if (symbol.InheritsFrom(ExceptionSymbol) && !ExceptionClassShouldBeSealed(options, symbol))
                return false;

            if (symbol.HasAttribute(ComImportSymbol))
                return false;

            if (symbol.IsTopLevelStatement(cancellationToken))
                return false;

            if (symbol.GetMembers().Any(member => member.IsVirtual) && !SealedClassWithVirtualMember(options, symbol))
                return false;

            var canBeInheritedOutsideOfAssembly = symbol.IsVisibleOutsideOfAssembly() && symbol.GetMembers().OfType<IMethodSymbol>().Any(member => member.MethodKind is MethodKind.Constructor && member.IsVisibleOutsideOfAssembly());
            if (canBeInheritedOutsideOfAssembly && !PublicClassShouldBeSealed(options, symbol))
                return false;


            return true;
        }

        private static bool ExceptionClassShouldBeSealed(AnalyzerOptions options, ISymbol symbol)
        {
            return options.GetConfigurationValue(symbol, RuleIdentifiers.ClassMustBeSealed + ".exceptions_should_be_sealed", defaultValue: false);
        }

        private static bool PublicClassShouldBeSealed(AnalyzerOptions options, ISymbol symbol)
        {
            return options.GetConfigurationValue(symbol, RuleIdentifiers.ClassMustBeSealed + ".public_class_should_be_sealed", defaultValue: false);
        }

        private static bool SealedClassWithVirtualMember(AnalyzerOptions options, ISymbol symbol)
        {
            var defaultValue = options.GetConfigurationValue(symbol, RuleIdentifiers.ClassMustBeSealed + ".class_with_virtual_member_shoud_be_sealed", defaultValue: false);
            return options.GetConfigurationValue(symbol, RuleIdentifiers.ClassMustBeSealed + ".class_with_virtual_member_should_be_sealed", defaultValue);
        }
    }
}
