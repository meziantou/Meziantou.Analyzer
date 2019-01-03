using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseStringComparerInHashSetConstructorAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.UseStringComparerInHashSetConstructor,
            title: "IEqualityComparer<string> is missing",
            messageFormat: "Use an overload of the constructor that has a IEqualityComparer<string> parameter",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringComparerInHashSetConstructor));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var hashSetTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1");
                var dictionaryTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
                var equalityComparerInterfaceType = compilationContext.Compilation.GetTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1");

                if (dictionaryTokenType != null || hashSetTokenType != null)
                {
                    compilationContext.RegisterSyntaxNodeAction(symbolContext =>
                    {
                        var creationNode = (ObjectCreationExpressionSyntax)symbolContext.Node;
                        var variableTypeInfo = symbolContext.SemanticModel.GetTypeInfo(symbolContext.Node).ConvertedType as INamedTypeSymbol;
                        if (variableTypeInfo == null)
                            return;

                        if (!variableTypeInfo.OriginalDefinition.Equals(dictionaryTokenType) &&
                            !variableTypeInfo.OriginalDefinition.Equals(hashSetTokenType))
                            return;

                        // We only care about dictionaries who use a string as the key
                        if (variableTypeInfo.TypeArguments[0].SpecialType != SpecialType.System_String)
                            return;

                        var arguments = creationNode.ArgumentList?.Arguments;
                        if (arguments == null || arguments.Value.Count == 0)
                        {
                            symbolContext.ReportDiagnostic(Diagnostic.Create(s_rule, symbolContext.Node.GetLocation()));
                            return;
                        }

                        var hasEqualityComparer = false;
                        foreach (var argument in arguments)
                        {
                            var argumentType = symbolContext.SemanticModel.GetTypeInfo(argument.Expression);

                            if (argumentType.ConvertedType == null)
                                return;

                            if (argumentType.ConvertedType.OriginalDefinition.Equals(equalityComparerInterfaceType))
                            {
                                hasEqualityComparer = true;
                                break;
                            }
                        }

                        if (!hasEqualityComparer)
                        {
                            symbolContext.ReportDiagnostic(Diagnostic.Create(s_rule, symbolContext.Node.GetLocation()));
                        }
                    }, SyntaxKind.ObjectCreationExpression);
                }
            });
        }
    }
}
