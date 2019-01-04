using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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
                var types = new List<INamedTypeSymbol>();
                types.AddIfNotNull(compilationContext.Compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1"));
                types.AddIfNotNull(compilationContext.Compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2"));
                types.AddIfNotNull(compilationContext.Compilation.GetTypesByMetadataName("System.Collections.Concurrent.ConcurrentDictionary`2")); // Not sure we need to find types instead of type

                var equalityComparerInterfaceType = compilationContext.Compilation.GetTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1");
                var stringType = compilationContext.Compilation.GetSpecialType(SpecialType.System_String);
                var stringEqualityComparerInterfaceType = equalityComparerInterfaceType.Construct(stringType);

                if (types.Any() && stringEqualityComparerInterfaceType != null)
                {
                    compilationContext.RegisterOperationAction(operationContext =>
                    {
                        var operation = (IObjectCreationOperation)operationContext.Operation;
                        var type = operation.Type as INamedTypeSymbol;
                        if (type == null || type.OriginalDefinition == null)
                            return;

                        if (types.Any(t => type.OriginalDefinition.Equals(t)))
                        {
                            // We only care about dictionaries who use a string as the key
                            if (!type.TypeArguments[0].IsString())
                                return;

                            var hasEqualityComparer = false;
                            foreach (var argument in operation.Arguments)
                            {
                                var argumentType = argument.Value.Type;
                                if (argumentType == null)
                                    continue;

                                if (argumentType.GetAllInterfacesIncludingThis().Any(i => stringEqualityComparerInterfaceType.Equals(i)))
                                {
                                    hasEqualityComparer = true;
                                    break;
                                }
                            }

                            if (!hasEqualityComparer)
                            {
                                operationContext.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation()));
                            }
                        }
                    }, OperationKind.ObjectCreation);
                }
            });
        }
    }
}
