using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseArrayEmptyAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.UseArrayEmpty,
            title: "Use Array.Empty<T>()",
            messageFormat: "Use Array.Empty<T>()",
            RuleCategories.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseArrayEmpty));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var typeSymbol = compilationContext.Compilation.GetTypeByMetadataName("System.Array");
                if (typeSymbol == null || typeSymbol.DeclaredAccessibility != Accessibility.Public)
                    return;

                if (typeSymbol.GetMembers("Empty").FirstOrDefault() is IMethodSymbol methodSymbol &&
                    methodSymbol.DeclaredAccessibility == Accessibility.Public &&
                    methodSymbol.IsStatic && methodSymbol.Arity == 1 && methodSymbol.Parameters.Length == 0)
                {
                    compilationContext.RegisterOperationAction(AnalyzeArrayCreationOperation, OperationKind.ArrayCreation);
                }
            });
        }

        private static void AnalyzeArrayCreationOperation(OperationAnalysisContext context)
        {
            var operation = (IArrayCreationOperation)context.Operation;
            if (IsZeroLengthArrayCreation(operation))
            {
                // Cannot use Array.Empty<T>() as an attribute parameter
                if (IsInAttribute(operation))
                    return;

                if (IsCompilerGeneratedParamsArray(operation, context))
                    return;

                context.ReportDiagnostic(s_rule, operation);
            }
        }

        private static bool IsZeroLengthArrayCreation(IArrayCreationOperation operation)
        {
            if (operation.DimensionSizes.Length != 1)
                return false;

            var dimensionSize = operation.DimensionSizes[0].ConstantValue;
            return dimensionSize.HasValue && (int)dimensionSize.Value == 0;
        }

        private static bool IsInAttribute(IArrayCreationOperation operation)
        {
            return operation.Syntax.Ancestors().OfType<AttributeArgumentSyntax>().Any();
        }

        private static bool IsCompilerGeneratedParamsArray(IArrayCreationOperation arrayCreationExpression, OperationAnalysisContext context)
        {
            var model = context.Compilation.GetSemanticModel(arrayCreationExpression.Syntax.SyntaxTree);

            // Compiler generated array creation seems to just use the syntax from the parent.
            var parent = model.GetOperation(arrayCreationExpression.Syntax, context.CancellationToken);
            if (parent == null)
                return false;

            ISymbol targetSymbol = null;
            var arguments = ImmutableArray<IArgumentOperation>.Empty;
            if (parent is IInvocationOperation invocation)
            {
                targetSymbol = invocation.TargetMethod;
                arguments = invocation.Arguments;
            }
            else
            {
                if (parent is IObjectCreationOperation objectCreation)
                {
                    targetSymbol = objectCreation.Constructor;
                    arguments = objectCreation.Arguments;
                }
                else if (parent is IPropertyReferenceOperation propertyReference)
                {
                    targetSymbol = propertyReference.Property;
                    arguments = propertyReference.Arguments;
                }
            }

            if (targetSymbol == null)
                return false;

            var parameters = GetParameters(targetSymbol);
            if (parameters.Length == 0 || !parameters[parameters.Length - 1].IsParams)
                return false;

            // At this point the array creation is known to be compiler synthesized as part of a call
            // to a method with a params parameter, and so it is probably sound to return true at this point.
            // As a sanity check, verify that the last argument to the call is equivalent to the array creation.
            // (Comparing for object identity does not work because the semantic model can return a fresh operation tree.)
            var lastArgument = arguments.LastOrDefault();
            return lastArgument != null && lastArgument.Value.Syntax == arrayCreationExpression.Syntax;
        }

        private static ImmutableArray<IParameterSymbol> GetParameters(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    return ((IMethodSymbol)symbol).Parameters;
                case SymbolKind.Property:
                    return ((IPropertySymbol)symbol).Parameters;
                default:
                    return ImmutableArray<IParameterSymbol>.Empty;
            }
        }
    }
}
