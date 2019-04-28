using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ValidateArgumentsCorrectlyAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.ValidateArgumentsCorrectly,
            title: "Validate arguments correctly",
            messageFormat: "Validate arguments correctly",
            RuleCategories.Design,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ValidateArgumentsCorrectly));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var compilation = ctx.Compilation;
                var analyzerContext = new AnalyzerContext(compilation);

                ctx.RegisterOperationBlockStartAction(analyzerContext.AnalyzeMethodBodyStart);
            });
        }

        private class AnalyzerContext
        {
            private readonly List<ISymbol> _symbols;
            private readonly INamedTypeSymbol _argumentSymbol;

            public AnalyzerContext(Compilation compilation)
            {
                var symbols = new List<ISymbol>();
                symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.IEnumerable"));
                symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1"));
                symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.IEnumerator"));
                symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerator`1"));
                _symbols = symbols;

                _argumentSymbol = compilation.GetTypeByMetadataName("System.ArgumentException");
            }

            public bool IsArgumentException(IThrowOperation operation)
            {
                return operation.Exception != null && operation.Exception.Type.IsAssignableTo(_argumentSymbol);
            }

            public bool CanContainsYield(IMethodSymbol methodSymbol)
            {
                if (!_symbols.Contains(methodSymbol.ReturnType.OriginalDefinition))
                    return false;

                return methodSymbol.Parameters.All(p => p.RefKind == RefKind.None);
            }

            internal void AnalyzeMethodBodyStart(OperationBlockStartAnalysisContext context)
            {
                var symbol = context.OwningSymbol as IMethodSymbol;
                if (symbol == null || !CanContainsYield(symbol))
                    return;

                var methodContext = new MethodContext(this, symbol);
                context.RegisterOperationAction(methodContext.AnalyzeYield, OperationKind.YieldReturn);
                context.RegisterOperationAction(methodContext.AnalyzeYield, OperationKind.YieldBreak);
                context.RegisterOperationAction(methodContext.AnalyzeThrow, OperationKind.Throw);
                context.RegisterOperationBlockEndAction(methodContext.OperationBlockEndAction);
            }
        }

        private class MethodContext
        {
            private readonly AnalyzerContext _analyzerContext;
            private readonly ISymbol _symbol;
            private int _lastThrowIndex = -1;
            private int _firstYieldIndex = int.MaxValue;

            public MethodContext(AnalyzerContext analyzerContext, ISymbol symbol)
            {
                _analyzerContext = analyzerContext;
                _symbol = symbol;
            }

            internal void AnalyzeThrow(OperationAnalysisContext context)
            {
                var operation = (IThrowOperation)context.Operation;
                if (_analyzerContext.IsArgumentException(operation))
                {
                    _lastThrowIndex = Math.Max(GetEndOfBlockIndex(context.Operation), _lastThrowIndex);
                }
            }

            internal void AnalyzeYield(OperationAnalysisContext context)
            {
                _firstYieldIndex = Math.Min(context.Operation.Syntax.SpanStart, _firstYieldIndex);
            }

            internal void OperationBlockEndAction(OperationBlockAnalysisContext context)
            {
                if (_lastThrowIndex >= 0 && _firstYieldIndex != int.MaxValue && _lastThrowIndex < _firstYieldIndex)
                {
                    var properties = ImmutableDictionary.Create<string, string>()
                        .Add("Index", _lastThrowIndex.ToString(CultureInfo.InvariantCulture));

                    context.ReportDiagnostic(s_rule, properties, _symbol);
                }
            }

            private static int GetEndOfBlockIndex(IOperation operation)
            {
                while (operation != null)
                {
                    if (operation is IMethodBodyOperation)
                        break;

                    if (operation.Parent != null && operation.Parent is IBlockOperation)
                    {
                        if (operation.Parent.Parent is IMethodBodyOperation)
                            break;
                    }

                    operation = operation.Parent;
                }

                return operation.Syntax.Span.End;
            }
        }
    }
}
