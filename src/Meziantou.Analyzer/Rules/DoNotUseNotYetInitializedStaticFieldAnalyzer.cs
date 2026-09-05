using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseNotYetInitializedStaticFieldAnalyzer : DiagnosticAnalyzer
{
    private const string StaticConstructorReason = " because it is assigned in the static constructor, which runs after the static field initializers";

    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseNotYetInitializedStaticField,
        title: "Do not use static fields before they are initialized",
        messageFormat: "Static field '{0}' may not be initialized yet{1}",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseNotYetInitializedStaticField),
        customTags: [GeneratedCodeReporting.ReportInGeneratedCodeTag]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var fieldDeclarationInfos = new ConcurrentDictionary<IFieldSymbol, FieldDeclarationInfo?>(SymbolEqualityComparer.Default);

            context.RegisterSymbolStartAction(context =>
            {
                // Delegates have no field, and all the fields of an enum are const, so they can never report anything.
                // Most other types have no static field either, so this avoids allocating the analyzer context for them.
                var symbol = (INamedTypeSymbol)context.Symbol;
                if (!HasCandidateStaticField(symbol))
                    return;

                var analyzerContext = new AnalyzerContext(symbol, fieldDeclarationInfos);
                context.RegisterOperationAction(analyzerContext.AnalyzeFieldReference, OperationKind.FieldReference);
                context.RegisterSymbolEndAction(analyzerContext.ReportDiagnostics);
            }, SymbolKind.NamedType);
        });
    }

    private static bool HasCandidateStaticField(INamedTypeSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
        {
            if (member is IFieldSymbol { IsImplicitlyDeclared: false, IsStatic: true, IsConst: false })
                return true;
        }

        return false;
    }

    private sealed class AnalyzerContext(INamedTypeSymbol containingType, ConcurrentDictionary<IFieldSymbol, FieldDeclarationInfo?> fieldDeclarationInfos)
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
                if (referencedField.ContainingType.IsEqualTo(containingType) && IsWrittenInStaticConstructor(context, fieldReferenceOperation))
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

                    context.ReportDiagnostic(Rule, location, referencedField.Name, StaticConstructorReason);
                    continue;
                }

                if (!ShouldReport(currentFieldInfo.Value, referencedFieldInfo.Value))
                    continue;

                context.ReportDiagnostic(Rule, location, referencedField.Name, "");
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
