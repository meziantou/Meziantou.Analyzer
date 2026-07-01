#if CSHARP12_OR_GREATER
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantBackingFieldForPrimaryConstructorParameterAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.RedundantBackingFieldForPrimaryConstructorParameter,
        title: "Do not create a backing field that merely copies a primary constructor parameter",
        messageFormat: "Field '{0}' is a redundant copy of primary constructor parameter '{1}'. Use the parameter directly.",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.RedundantBackingFieldForPrimaryConstructorParameter));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            if (context.Compilation.GetCSharpLanguageVersion() < Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
                return;

            context.RegisterOperationAction(AnalyzeFieldInitializer, OperationKind.FieldInitializer);
        });
    }

    private void AnalyzeFieldInitializer(OperationAnalysisContext context)
    {
        var fieldInitializer = (IFieldInitializerOperation)context.Operation;
        if (fieldInitializer.InitializedFields is not { Length: 1 } fields)
            return;

        var fieldSymbol = fields[0];
        var value = fieldInitializer.Value;

        // Unwrap compiler-generated implicit non-user-defined conversions
        // (e.g. int -> long widening, int -> int? nullable lifting) so that
        // `private readonly long _x = x;` is still flagged. Explicit casts
        // (`(string)obj`) and user-defined conversions are NOT unwrapped.
        while (value is IConversionOperation conversion
               && conversion.IsImplicit
               && conversion.Conversion.IsImplicit
               && !conversion.Conversion.IsUserDefined)
        {
            value = conversion.Operand;
        }

        if (value is not IParameterReferenceOperation parameterReference)
            return;

        if (parameterReference.Parameter.ContainingSymbol is not IMethodSymbol methodSymbol)
            return;

        if (!methodSymbol.IsPrimaryConstructor(context.CancellationToken, includeRecordDeclarations: true))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            fieldSymbol.Locations[0],
            fieldSymbol.Name,
            parameterReference.Parameter.Name));
    }
}
#endif
