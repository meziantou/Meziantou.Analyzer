using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ClassMustBeSealedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.ClassMustBeSealed,
        title: "Make class sealed",
        messageFormat: "Make class sealed",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ClassMustBeSealed));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

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

    private sealed class AnalyzerContext
    {
        private readonly List<ITypeSymbol> _potentialClasses = new();
        private readonly HashSet<ITypeSymbol> _cannotBeSealedClasses = new(SymbolEqualityComparer.Default);

        private INamedTypeSymbol? ExceptionSymbol { get; }
        private INamedTypeSymbol? ComImportSymbol { get; }
        private INamedTypeSymbol? BenchmarkSymbol { get; }

        public AnalyzerContext(Compilation compilation)
        {
            ExceptionSymbol = compilation.GetBestTypeByMetadataName("System.Exception");
            ComImportSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.ComImportAttribute");
            BenchmarkSymbol = compilation.GetBestTypeByMetadataName("BenchmarkDotNet.Attributes.BenchmarkAttribute");
        }

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

                    if (symbol.BaseType != null)
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
            if (symbol.ContainingType != null && symbol.HasAttribute(BenchmarkSymbol))
            {
                lock (_cannotBeSealedClasses)
                {
                    _cannotBeSealedClasses.Add(symbol.ContainingType);
                    _cannotBeSealedClasses.Add(symbol.ContainingType.OriginalDefinition);
                }
            }
        }

        public void AnalyzeCompilationEnd(CompilationAnalysisContext context)
        {
            foreach (var @class in _potentialClasses)
            {
                if (_cannotBeSealedClasses.Contains(@class))
                    continue;

                context.ReportDiagnostic(s_rule, @class);
            }
        }

        private bool IsPotentialSealed(AnalyzerOptions options, INamedTypeSymbol symbol, CancellationToken cancellationToken)
        {
            if (symbol.IsSealed || symbol.IsAbstract || symbol.IsStatic || symbol.IsValueType)
                return false;

            if (symbol.InheritsFrom(ExceptionSymbol))
                return false;

            if (symbol.HasAttribute(ComImportSymbol))
                return false;

            if (symbol.IsTopLevelStatement(cancellationToken))
                return false;

            if (symbol.GetMembers().Any(member => member.IsVirtual) && !SealedClassWithVirtualMember(options, symbol))
                return false;

            if (symbol.IsVisibleOutsideOfAssembly() && !PublicClassShouldBeSealed(options, symbol))
                return false;

            return true;
        }

        private static bool PublicClassShouldBeSealed(AnalyzerOptions options, ISymbol symbol)
        {
            return options.GetConfigurationValue(symbol, RuleIdentifiers.ClassMustBeSealed + ".public_class_should_be_sealed", defaultValue: false);
        }

        private static bool SealedClassWithVirtualMember(AnalyzerOptions options, ISymbol symbol)
        {
            return options.GetConfigurationValue(symbol, RuleIdentifiers.ClassMustBeSealed + ".class_with_virtual_member_shoud_be_sealed", defaultValue: false);
        }
    }
}
