using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    namespace Meziantou.Analyzer.Rules
    {
        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public class AvoidClosureWhenUsingConcurrentDictionaryAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor s_rule = new(
                RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionary,
                title: "Use the lambda parameters instead of using a closure",
                messageFormat: "Use the lambda parameters instead of using a closure",
                RuleCategories.Performance,
                DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                description: "",
                helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionary));

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

            /// <inheritdoc/>
            public override void Initialize(AnalysisContext context)
            {
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
                context.EnableConcurrentExecution();

                context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            }

            private void AnalyzeInvocation(OperationAnalysisContext context)
            {
                var op = (IInvocationOperation)context.Operation;
                var concurrentDictionarySymbol = context.Compilation.GetTypeByMetadataName("System.Collections.Concurrent.ConcurrentDictionary`2");
                var func2Symbol = context.Compilation.GetTypeByMetadataName("System.Func`2");
                var func3Symbol = context.Compilation.GetTypeByMetadataName("System.Func`3");
                var func4Symbol = context.Compilation.GetTypeByMetadataName("System.Func`4");
                if (op.TargetMethod.Name is "GetOrAdd" && op.TargetMethod.ContainingSymbol.OriginalDefinition.IsEqualTo(concurrentDictionarySymbol))
                {
                    // a.GetOrAdd(key, (k) => key);
                    if (op.Arguments.Length == 2 && op.Arguments[1].Parameter!.Type.OriginalDefinition.IsEqualTo(func2Symbol))
                    {
                        DetectBadUsage(context, op.Arguments[1].Value, op.Arguments[0]);
                    }
                    // a.GetOrAdd(key, (k, arg) => key, arg);
                    else if (op.Arguments.Length == 3 && op.Arguments[1].Parameter!.Type.OriginalDefinition.IsEqualTo(func3Symbol))
                    {
                        DetectBadUsage(context, op.Arguments[1].Value, op.Arguments[0]);
                        DetectBadUsage(context, op.Arguments[1].Value, op.Arguments[2]);
                    }

                }
                else if (op.TargetMethod.Name is "AddOrUpdate" && op.TargetMethod.ContainingSymbol.OriginalDefinition.IsEqualTo(concurrentDictionarySymbol))
                {
                    // a.AddOrUpdate(key, (k) => k, (k, v) => k + v + 1);
                    if (op.Arguments.Length == 3 && op.Arguments[1].Parameter!.Type.OriginalDefinition.IsEqualTo(func2Symbol) && op.Arguments[2].Parameter!.Type.OriginalDefinition.IsEqualTo(func3Symbol))
                    {
                        DetectBadUsage(context, op.Arguments[1].Value, op.Arguments[0]);
                        DetectBadUsage(context, op.Arguments[2].Value, op.Arguments[0]);
                    }
                    // a.AddOrUpdate(key, value, (k, v) => k + v);
                    else if (op.Arguments.Length == 3 && op.Arguments[2].Parameter!.Type.OriginalDefinition.IsEqualTo(func3Symbol))
                    {
                        DetectBadUsage(context, op.Arguments[2].Value, op.Arguments[1]);
                        DetectBadUsage(context, op.Arguments[2].Value, op.Arguments[2]);
                    }
                    // a.AddOrUpdate(key, (k, arg) => k + arg, (k, v, arg) => k + v + arg, factoryArg);
                    else if (op.Arguments.Length == 4 && op.Arguments[1].Parameter!.Type.OriginalDefinition.IsEqualTo(func3Symbol) && op.Arguments[2].Parameter!.Type.OriginalDefinition.IsEqualTo(func4Symbol))
                    {
                        DetectBadUsage(context, op.Arguments[1].Value, op.Arguments[0]);
                        DetectBadUsage(context, op.Arguments[1].Value, op.Arguments[3]);
                        DetectBadUsage(context, op.Arguments[2].Value, op.Arguments[0]);
                        DetectBadUsage(context, op.Arguments[2].Value, op.Arguments[3]);
                    }
                }
            }

            private static void DetectBadUsage(OperationAnalysisContext context, IOperation argumentOperation, IArgumentOperation potentialVariableOperation)
            {
                // Check if potential is a _reference_ (variable / parameter)
                var value = potentialVariableOperation.Value;
                ISymbol? symbol = null;

                if (value is ILocalReferenceOperation localReferenceOperation)
                {
                    symbol = localReferenceOperation.Local;
                }
                else if (value is IParameterReferenceOperation parameterReferenceOperation)
                {
                    symbol = parameterReferenceOperation.Parameter;
                }

                if (symbol is null)
                    return;

                if (argumentOperation is IAnonymousFunctionOperation or IDelegateCreationOperation)
                {
                    var syntax = GetDataFlowArgument(argumentOperation.Syntax);
                    var semanticModel = context.Operation.SemanticModel!;
                    var dataFlow = semanticModel.AnalyzeDataFlow(syntax);
                    foreach (var read in dataFlow.ReadInside)
                    {
                        if (read.IsEqualTo(symbol))
                        {
                            foreach (var written in dataFlow.WrittenInside)
                            {
                                if (written.IsEqualTo(symbol))
                                    return;
                            }

                            context.ReportDiagnostic(s_rule, argumentOperation);
                        }
                    }
                }
            }

            [return: NotNullIfNotNull("node")]
            private static SyntaxNode? GetDataFlowArgument(SyntaxNode? node)
            {
                if (node == null)
                    return null;

                if (node is ArrowExpressionClauseSyntax expression)
                {
                    return expression.Expression;
                }

                return node;
            }
        }
    }
}
