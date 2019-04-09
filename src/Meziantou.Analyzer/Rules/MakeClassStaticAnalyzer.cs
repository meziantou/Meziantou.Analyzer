using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeClassStaticAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.MakeClassStatic,
            title: "Make class static",
            messageFormat: "Make class static",
            RuleCategories.Design,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AbstractTypesShouldNotHaveConstructors));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(s =>
            {
                var potentialClasses = new List<ITypeSymbol>();
                var cannotBeStaticClasses = new HashSet<ITypeSymbol>();

                var coClassAttributeSymbol = s.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.CoClassAttribute");

                s.RegisterSymbolAction(ctx =>
                {
                    var symbol = (INamedTypeSymbol)ctx.Symbol;
                    switch (symbol.TypeKind)
                    {
                        case TypeKind.Class:
                            if (IsPotentialStatic(symbol))
                            {
                                lock (potentialClasses)
                                {
                                    potentialClasses.Add(symbol);
                                }
                            }

                            if (symbol.BaseType != null)
                            {
                                lock (cannotBeStaticClasses)
                                {
                                    cannotBeStaticClasses.Add(symbol.BaseType);
                                }
                            }

                            break;

                        case TypeKind.Interface:
                            foreach (var attribute in symbol.GetAttributes().Where(attr => attr.AttributeClass.IsEqualsTo(coClassAttributeSymbol)))
                            {
                                var attributeValue = attribute.ConstructorArguments.FirstOrDefault();
                                if (!attributeValue.IsNull && attributeValue.Kind == TypedConstantKind.Type && attributeValue.Value is ITypeSymbol type)
                                {
                                    lock (cannotBeStaticClasses)
                                    {
                                        cannotBeStaticClasses.Add(type);
                                    }
                                }
                            }

                            break;
                    }
                }, SymbolKind.NamedType);

                s.RegisterOperationAction(ctx =>
                {
                    var operation = (IObjectCreationOperation)ctx.Operation;

                    lock (cannotBeStaticClasses)
                    {
                        cannotBeStaticClasses.Add(operation.Constructor.ContainingType);
                    }

                }, OperationKind.ObjectCreation);

                s.RegisterCompilationEndAction(ctx =>
                {
                    foreach (var c in potentialClasses)
                    {
                        if (cannotBeStaticClasses.Contains(c))
                            continue;

                        foreach (var location in c.Locations)
                        {
                            ctx.ReportDiagnostic(Diagnostic.Create(s_rule, location));
                        }
                    }
                });
            });
        }

        private static bool IsPotentialStatic(INamedTypeSymbol symbol)
        {
            return !symbol.IsAbstract &&
                !symbol.IsStatic &&
                !symbol.Interfaces.Any() &&
                !HasBaseClass() &&
                !symbol.IsUnitTestClass() &&
                symbol.GetMembers().All(member => (member.IsStatic || member.IsImplicitlyDeclared) && !member.IsOperator());

            bool HasBaseClass()
            {
                return symbol.BaseType != null && symbol.BaseType.SpecialType != SpecialType.System_Object;
            }
        }
    }
}
