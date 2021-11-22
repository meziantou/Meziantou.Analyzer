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
                messageFormat: "Use the lambda parameters instead of using a closure (captured variable: {0})",
                RuleCategories.Performance,
                DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                description: "",
                helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionary));

            private static readonly DiagnosticDescriptor s_ruleFactoryArg = new(
                RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionaryByUsingFactoryArg,
                title: "Avoid closure by using an overload with the 'factoryArgument' parameter",
                messageFormat: "Avoid closure by using an overload with the 'factoryArgument' parameter (captured variable: {0})",
                RuleCategories.Performance,
                DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                description: "",
                helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionaryByUsingFactoryArg));

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule, s_ruleFactoryArg);

            /// <inheritdoc/>
            public override void Initialize(AnalysisContext context)
            {
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
                context.EnableConcurrentExecution();

                context.RegisterCompilationStartAction(ctx =>
                {
                    var symbol = ctx.Compilation.GetTypeByMetadataName("System.Collections.Concurrent.ConcurrentDictionary`2");
                    if (symbol == null)
                        return;

                    var members = symbol.GetMembers("GetOrAdd");
                    var hasOverloadWithArg = members.OfType<IMethodSymbol>().Any(m => m.Parameters.Any(p => p.Name == "factoryArgument"));

                    ctx.RegisterOperationAction(ctx => AnalyzeInvocation(ctx, hasOverloadWithArg), OperationKind.Invocation);
                });

            }

            private void AnalyzeInvocation(OperationAnalysisContext context, bool hasOverloadWithArg)
            {
                var op = (IInvocationOperation)context.Operation;
                var concurrentDictionarySymbol = context.Compilation.GetTypeByMetadataName("System.Collections.Concurrent.ConcurrentDictionary`2");
                if (!op.TargetMethod.ContainingSymbol.OriginalDefinition.IsEqualTo(concurrentDictionarySymbol))
                    return;

                var func2Symbol = context.Compilation.GetTypeByMetadataName("System.Func`2");
                var func3Symbol = context.Compilation.GetTypeByMetadataName("System.Func`3");
                var func4Symbol = context.Compilation.GetTypeByMetadataName("System.Func`4");

                // Check if the key/value parameter should be used
                var handled = false;
                if (op.TargetMethod.Name is "GetOrAdd")
                {
                    // a.GetOrAdd(key, (k) => key);
                    if (op.Arguments.Length == 2 && op.Arguments[1].Parameter!.Type.OriginalDefinition.IsEqualTo(func2Symbol))
                    {
                        handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[1].Value, op.Arguments[0]);
                    }
                    // a.GetOrAdd(key, (k, arg) => key, arg);
                    else if (op.Arguments.Length == 3 && op.Arguments[1].Parameter!.Type.OriginalDefinition.IsEqualTo(func3Symbol))
                    {
                        handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[1].Value, op.Arguments[0]);
                        handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[1].Value, op.Arguments[2]);
                    }

                }
                else if (op.TargetMethod.Name is "AddOrUpdate")
                {
                    // a.AddOrUpdate(key, (k) => k, (k, v) => k + v + 1);
                    if (op.Arguments.Length == 3 && op.Arguments[1].Parameter!.Type.OriginalDefinition.IsEqualTo(func2Symbol) && op.Arguments[2].Parameter!.Type.OriginalDefinition.IsEqualTo(func3Symbol))
                    {
                        handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[1].Value, op.Arguments[0]);
                        handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[2].Value, op.Arguments[0]);
                    }
                    // a.AddOrUpdate(key, value, (k, v) => k + v);
                    else if (op.Arguments.Length == 3 && op.Arguments[2].Parameter!.Type.OriginalDefinition.IsEqualTo(func3Symbol))
                    {
                        handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[2].Value, op.Arguments[1]);
                        handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[2].Value, op.Arguments[2]);
                    }
                    // a.AddOrUpdate(key, (k, arg) => k + arg, (k, v, arg) => k + v + arg, factoryArg);
                    else if (op.Arguments.Length == 4 && op.Arguments[1].Parameter!.Type.OriginalDefinition.IsEqualTo(func3Symbol) && op.Arguments[2].Parameter!.Type.OriginalDefinition.IsEqualTo(func4Symbol))
                    {
                        handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[1].Value, op.Arguments[0]);
                        handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[1].Value, op.Arguments[3]);

                        handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[2].Value, op.Arguments[0]);
                        handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[2].Value, op.Arguments[3]);
                    }
                }

                if (!handled && hasOverloadWithArg && op.TargetMethod.Name is "AddOrUpdate" or "GetOrAdd")
                {
                    foreach (var arg in op.Arguments)
                    {
                        if (arg.Parameter!.OriginalDefinition.Type.OriginalDefinition.IsEqualToAny(func2Symbol, func3Symbol, func4Symbol))
                        {
                            DetectClosure(context, arg.Value);
                        }
                    }
                }
            }

            private static bool DetectPotentialUsageOfLambdaParameter(OperationAnalysisContext context, IOperation argumentOperation, IArgumentOperation potentialVariableOperation)
            {
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
                    return false;

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
                                    return false;
                            }

                            context.ReportDiagnostic(s_rule, argumentOperation, read.Name);
                            return true;
                        }
                    }
                }

                return false;
            }

            private static void DetectClosure(OperationAnalysisContext context, IOperation argumentOperation)
            {
                if (argumentOperation is IAnonymousFunctionOperation or IDelegateCreationOperation)
                {
                    var syntax = GetDataFlowArgument(argumentOperation.Syntax);
                    var semanticModel = context.Operation.SemanticModel!;
                    var dataFlow = semanticModel.AnalyzeDataFlow(syntax);
                    if (dataFlow.CapturedInside.Length > 0)
                    {
                        context.ReportDiagnostic(s_ruleFactoryArg, argumentOperation, string.Join(", ", dataFlow.Captured.Select(symbol => symbol.Name)));
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
