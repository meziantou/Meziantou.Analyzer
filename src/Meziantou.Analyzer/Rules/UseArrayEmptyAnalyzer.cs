using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var typeSymbol = compilationContext.Compilation.GetTypeByMetadataName("System.Array");
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

        // The compiler synthesizes the array of a params parameter (method, constructor, indexer, attribute,
        // or the constructor called by a collection expression). The user cannot replace it with Array.Empty<T>().
        // The array created by an array initializer (int[] a = { }) is also implicit, but it is written by the user.
        if (operation is { IsImplicit: true, Syntax: not InitializerExpressionSyntax })
            return;

        if (IsZeroLengthArrayCreation(operation, context.CancellationToken))
        {
            // Pointer types cannot be used as generic type arguments (CS0306)
            if (operation.Type is IArrayTypeSymbol { ElementType.TypeKind: TypeKind.Pointer or TypeKind.FunctionPointer })
                return;

            // Cannot use Array.Empty<T>() as an attribute parameter
            if (IsInAttribute(operation))
                return;

            context.ReportDiagnostic(Rule, operation);
        }
    }

    private static bool IsZeroLengthArrayCreation(IArrayCreationOperation operation, CancellationToken cancellationToken)
    {
        if (operation.DimensionSizes.Length != 1)
            return false;

        return operation.DimensionSizes[0].TryGetConstantValue(out var dimensionSize, cancellationToken) && IsZero(dimensionSize);

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
}
