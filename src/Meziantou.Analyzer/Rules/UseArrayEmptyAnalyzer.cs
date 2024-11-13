using System.Collections.Immutable;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseArrayEmptyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseArrayEmpty,
        title: "Use Array.Empty<T>()",
        messageFormat: "Use Array.Empty<T>()",
        RuleCategories.Performance,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseArrayEmpty));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var typeSymbol = compilationContext.Compilation.GetBestTypeByMetadataName("System.Array");
            if (typeSymbol is null || typeSymbol.DeclaredAccessibility != Accessibility.Public)
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

            context.ReportDiagnostic(Rule, operation);
        }
    }

    private static bool IsZeroLengthArrayCreation(IArrayCreationOperation operation)
    {
        if (operation.DimensionSizes.Length != 1)
            return false;

        var dimensionSize = operation.DimensionSizes[0].ConstantValue;
        return dimensionSize.HasValue && IsZero(dimensionSize.Value);

        static bool IsZero(object? value)
        {
            return value switch
            {
                int i => i == 0,
                long l => l == 0,
                uint ui => ui == 0,
                _ => false,
            };
        }
    }

    private static bool IsInAttribute(IArrayCreationOperation operation)
    {
        return operation.Syntax.AncestorsAndSelf().OfType<AttributeSyntax>().Any();
    }

    private static bool IsCompilerGeneratedParamsArray(IArrayCreationOperation arrayCreationExpression, OperationAnalysisContext context)
    {
        var semanticModel = context.Operation.SemanticModel!;

        // Compiler generated array creation seems to just use the syntax from the parent.
        var parent = semanticModel.GetOperation(arrayCreationExpression.Syntax, context.CancellationToken);
        if (parent is null)
            return false;

        ISymbol? targetSymbol = null;
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

        if (targetSymbol is null)
            return false;

        var parameters = GetParameters(targetSymbol);
        if (parameters.Length == 0 || !parameters[^1].IsParams)
            return false;

        // At this point the array creation is known to be compiler synthesized as part of a call
        // to a method with a params parameter, and so it is probably sound to return true at this point.
        // As a sanity check, verify that the last argument to the call is equivalent to the array creation.
        // (Comparing for object identity does not work because the semantic model can return a fresh operation tree.)
        var lastArgument = arguments.LastOrDefault();
        return lastArgument is not null && lastArgument.Value.Syntax == arrayCreationExpression.Syntax;
    }

    private static ImmutableArray<IParameterSymbol> GetParameters(ISymbol symbol)
    {
        return symbol.Kind switch
        {
            SymbolKind.Method => ((IMethodSymbol)symbol).Parameters,
            SymbolKind.Property => ((IPropertySymbol)symbol).Parameters,
            _ => [],
        };
    }
}
