using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

// https://github.com/dotnet/runtime/issues/34812
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseSystemThreadingLockInsteadOfObjectAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseSystemThreadingLockInsteadOfObject,
        title: "Use System.Threading.Lock",
        messageFormat: "Use System.Threading.Lock",
        RuleCategories.Performance,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseSystemThreadingLockInsteadOfObject));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            if (context.Compilation.GetBestTypeByMetadataName("System.Threading.Lock") is null)
                return;

            if (!context.Compilation.GetCSharpLanguageVersion().IsCSharp13OrAbove())
                return;

            context.RegisterOperationBlockStartAction(context =>
            {
                foreach (var block in context.OperationBlocks)
                {
                    if (block.Syntax is StatementSyntax or ExpressionSyntax)
                    {
                        var symbols = new SymbolLockContext();
                        context.RegisterOperationAction(context => symbols.HandleOperation((ILocalReferenceOperation)context.Operation), OperationKind.LocalReference);
                        context.RegisterOperationBlockEndAction(context => symbols.ReportSymbols(context, Rule));
                    }
                }
            });

            var symbols = new SymbolLockContext();
            context.RegisterOperationAction(context => symbols.HandleOperation((IFieldReferenceOperation)context.Operation), OperationKind.FieldReference);
            context.RegisterCompilationEndAction(context => symbols.ReportSymbols(context, Rule));
        });
    }

    private sealed class SymbolLockContext
    {
        private readonly ConcurrentDictionary<ISymbol, bool> _symbols = new(SymbolEqualityComparer.Default);

        public void ReportSymbols(DiagnosticReporter reporter, DiagnosticDescriptor descriptor)
        {
            foreach (var symbol in _symbols)
            {
                if (symbol.Value)
                {
                    reporter.ReportDiagnostic(descriptor, symbol.Key);
                }
            }
        }

        private static bool IsPotentialSymbol(ISymbol symbol)
        {
            if (symbol is IFieldSymbol { Type.SpecialType: SpecialType.System_Object } && !symbol.IsVisibleOutsideOfAssembly())
                return true;

            if (symbol is ILocalSymbol { Type.SpecialType: SpecialType.System_Object })
                return true;

            return false;
        }

        public void HandleOperation(ILocalReferenceOperation operation)
        {
            var symbol = operation.Local;
            HandleOperation(symbol, operation);
        }

        public void HandleOperation(IFieldReferenceOperation operation)
        {
            var symbol = operation.Field;
            HandleOperation(symbol, operation);
        }

        private void HandleOperation(ISymbol symbol, IOperation operation)
        {
            if (!IsPotentialSymbol(symbol))
                return;

            if (operation.Parent is not ILockOperation)
            {
                ExcludeSymbol(symbol);
            }
            else
            {
                AddPotentialSymbol(symbol);
            }
        }

        public void ExcludeSymbol(ISymbol symbol)
        {
            _ = _symbols.AddOrUpdate(symbol, addValue: false, (_, _) => false);
        }

        public void AddPotentialSymbol(ISymbol symbol)
        {
            _ = _symbols.TryAdd(symbol, value: true);
        }
    }
}
