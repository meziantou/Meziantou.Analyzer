using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseNotYetInitializedStaticFieldAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseNotYetInitializedStaticField,
        title: "Do not use static fields before they are initialized",
        messageFormat: "Static field '{0}' may not be initialized yet",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseNotYetInitializedStaticField));

    private static readonly DiagnosticDescriptor RuleStaticConstructor = new(
        RuleIdentifiers.DoNotUseNotYetInitializedStaticField,
        title: "Do not use static fields before they are initialized",
        messageFormat: "Static field '{0}' is assigned in the static constructor, which runs after the static field initializers",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseNotYetInitializedStaticField));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, RuleStaticConstructor);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var fieldDeclarationInfos = new ConcurrentDictionary<IFieldSymbol, FieldDeclarationInfo?>(SymbolEqualityComparer.Default);

            context.RegisterSymbolStartAction(context =>
            {
                var analyzerContext = new AnalyzerContext(fieldDeclarationInfos);
                context.RegisterOperationAction(analyzerContext.AnalyzeFieldReference, OperationKind.FieldReference);
                context.RegisterSymbolEndAction(analyzerContext.ReportDiagnostics);
            }, SymbolKind.NamedType);
        });
    }

    private sealed class AnalyzerContext(ConcurrentDictionary<IFieldSymbol, FieldDeclarationInfo?> fieldDeclarationInfos)
    {
        private readonly ConcurrentBag<FieldReferenceInfo> _candidates = [];
        private readonly ConcurrentDictionary<IFieldSymbol, bool> _fieldsAssignedInStaticConstructor = new(SymbolEqualityComparer.Default);

        public void AnalyzeFieldReference(OperationAnalysisContext context)
        {
            var fieldReferenceOperation = (IFieldReferenceOperation)context.Operation;
            if (fieldReferenceOperation.IsInNameofOperation())
                return;

            if (IsInDeferredExecutionContext(fieldReferenceOperation))
                return;

            var referencedField = fieldReferenceOperation.Field;
            if (referencedField is not { IsImplicitlyDeclared: false, IsStatic: true, IsConst: false })
                return;

            if (!TryGetContainingFieldInitializerField(fieldReferenceOperation, out var currentField))
            {
                if (IsWrittenInStaticConstructor(context, fieldReferenceOperation))
                {
                    _fieldsAssignedInStaticConstructor.TryAdd(referencedField, true);
                }

                return;
            }

            if (!referencedField.ContainingType.IsEqualTo(currentField.ContainingType))
                return;

            if (referencedField.IsEqualTo(currentField))
                return;

            _candidates.Add(new(fieldReferenceOperation.Syntax.GetLocation(), referencedField, currentField));
        }

        public void ReportDiagnostics(SymbolAnalysisContext context)
        {
            foreach (var (location, referencedField, currentField) in _candidates)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var currentFieldInfo = GetFieldDeclarationInfo(currentField, fieldDeclarationInfos, context.CancellationToken);
                if (currentFieldInfo is null)
                    continue;

                var referencedFieldInfo = GetFieldDeclarationInfo(referencedField, fieldDeclarationInfos, context.CancellationToken);
                if (referencedFieldInfo is null)
                    continue;

                if (referencedFieldInfo.Value.Initializer is null)
                {
                    // A field with no initializer is only observed as not-yet-initialized when the static constructor
                    // assigns it, as the static constructor body runs after all the static field initializers.
                    if (!_fieldsAssignedInStaticConstructor.ContainsKey(referencedField))
                        continue;

                    context.ReportDiagnostic(RuleStaticConstructor, location, [referencedField.Name]);
                    continue;
                }

                if (!ShouldReport(currentFieldInfo.Value, referencedFieldInfo.Value))
                    continue;

                context.ReportDiagnostic(Rule, location, [referencedField.Name]);
            }
        }

        private static bool IsWrittenInStaticConstructor(OperationAnalysisContext context, IFieldReferenceOperation operation)
        {
            if (context.ContainingSymbol is not IMethodSymbol { MethodKind: MethodKind.StaticConstructor })
                return false;

            return operation.Parent switch
            {
                IAssignmentOperation assignment => assignment.Target == operation,
                IIncrementOrDecrementOperation incrementOrDecrement => incrementOrDecrement.Target == operation,
                IArgumentOperation { Parameter.RefKind: RefKind.Ref or RefKind.Out } => true,
                _ => false,
            };
        }
    }

    private readonly record struct FieldReferenceInfo(Location Location, IFieldSymbol ReferencedField, IFieldSymbol CurrentField);

    private static bool IsInDeferredExecutionContext(IOperation operation)
    {
        foreach (var ancestor in operation.Ancestors())
        {
            if (ancestor is IAnonymousFunctionOperation or ILocalFunctionOperation)
                return true;
        }

        return false;
    }

    private static bool TryGetContainingFieldInitializerField(IOperation operation, [NotNullWhen(true)] out IFieldSymbol? field)
    {
        foreach (var ancestor in operation.Ancestors())
        {
            if (ancestor is IFieldInitializerOperation fieldInitializerOperation)
            {
                var initializedField = fieldInitializerOperation.InitializedFields.FirstOrDefault(field => field is { IsImplicitlyDeclared: false, IsStatic: true, IsConst: false });
                if (initializedField is not null)
                {
                    field = initializedField;
                    return true;
                }
            }
        }

        field = null;
        return false;
    }

    private static bool ShouldReport(FieldDeclarationInfo currentField, FieldDeclarationInfo referencedField)
    {
        if (!currentField.IsInSamePartialDeclarationAs(referencedField))
            return true;

        return referencedField.DeclaratorStart > currentField.DeclaratorStart;
    }

    private static FieldDeclarationInfo? GetFieldDeclarationInfo(IFieldSymbol field, ConcurrentDictionary<IFieldSymbol, FieldDeclarationInfo?> cache, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(field, out var result))
            return result;

        result = CreateFieldDeclarationInfo(field, cancellationToken);
        cache.TryAdd(field, result);
        return result;
    }

    private static FieldDeclarationInfo? CreateFieldDeclarationInfo(IFieldSymbol field, CancellationToken cancellationToken)
    {
        if (field.DeclaringSyntaxReferences is not [var syntaxReference])
            return null;

        if (syntaxReference.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax variableDeclarator)
            return null;

        if (variableDeclarator.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not TypeDeclarationSyntax typeDeclaration)
            return null;

        return new(
            variableDeclarator.SyntaxTree,
            typeDeclaration.SpanStart,
            variableDeclarator.SpanStart,
            variableDeclarator.Initializer);
    }

    private readonly record struct FieldDeclarationInfo(SyntaxTree SyntaxTree, int TypeDeclarationSpanStart, int DeclaratorStart, EqualsValueClauseSyntax? Initializer)
    {
        public bool IsInSamePartialDeclarationAs(FieldDeclarationInfo other)
        {
            return TypeDeclarationSpanStart == other.TypeDeclarationSpanStart && SyntaxTree == other.SyntaxTree;
        }
    }
}
