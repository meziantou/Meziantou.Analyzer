using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

// https://github.com/dotnet/runtime/issues/34812
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseSystemThreadingLockInsteadOfObjectAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseSystemThreadingLockInsteadOfObject,
        title: "Use System.Threading.Lock",
        messageFormat: "Use System.Threading.Lock",
        RuleCategories.Performance,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseSystemThreadingLockInsteadOfObject));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var lockType = context.Compilation.GetBestTypeByMetadataName("System.Threading.Lock");
            if (lockType is null)
                return;

            if (!context.Compilation.GetCSharpLanguageVersion().IsCSharp13OrGreater())
                return;

            context.RegisterOperationBlockStartAction(context =>
            {
                foreach (var block in context.OperationBlocks)
                {
                    if (block.Syntax is StatementSyntax or ExpressionSyntax)
                    {
                        var symbols = new SymbolLockContext(lockType);
                        context.RegisterOperationAction(context => symbols.HandleOperation((ILocalReferenceOperation)context.Operation), OperationKind.LocalReference);
                        context.RegisterOperationAction(context => symbols.HandleOperation((IVariableDeclaratorOperation)context.Operation), OperationKind.VariableDeclarator);
                        context.RegisterOperationBlockEndAction(context => symbols.ReportSymbols(context, Rule));
                    }
                }
            });

            var symbols = new SymbolLockContext(lockType);
            context.RegisterOperationAction(context => symbols.HandleOperation((IFieldReferenceOperation)context.Operation), OperationKind.FieldReference);
            context.RegisterOperationAction(context => symbols.HandleOperation((IFieldInitializerOperation)context.Operation), OperationKind.FieldInitializer);
            context.RegisterCompilationEndAction(context => symbols.ReportSymbols(context, Rule));
        });
    }

    private sealed class SymbolLockContext(INamedTypeSymbol lockType)
    {
        private readonly ConcurrentDictionary<ISymbol, bool> _symbols = new(SymbolEqualityComparer.Default);

        public void ReportSymbols(DiagnosticReporter reporter, DiagnosticDescriptor descriptor)
        {
            foreach (var symbol in _symbols)
            {
                if (symbol.Value)
                {
                    reporter.ReportDiagnostic(descriptor, symbol.Key);
                }
            }
        }

        private static bool IsPotentialSymbol(ISymbol symbol)
        {
            if (symbol is IFieldSymbol { Type.SpecialType: SpecialType.System_Object } && !symbol.IsVisibleOutsideOfAssembly())
                return true;

            // Only the locals declared by a variable declaration can be declared with a different type.
            // For instance, the type of a foreach variable or of a pattern variable is tied to the value it gets.
            if (symbol is ILocalSymbol { Type.SpecialType: SpecialType.System_Object, DeclaringSyntaxReferences: [var syntaxReference] } && syntaxReference.GetSyntax() is VariableDeclaratorSyntax)
                return true;

            return false;
        }

        // The value must still be valid once the type of the symbol is System.Threading.Lock.
        // The code fixer replaces the creation of an object with `new()`, the other values are kept as-is.
        private bool IsSupportedValue(IOperation value)
        {
            return value.UnwrapImplicitConversions() switch
            {
                IObjectCreationOperation { Type.SpecialType: SpecialType.System_Object } => true,
                ILiteralOperation operation when operation.IsNull() => true,
                IDefaultValueOperation { Syntax: LiteralExpressionSyntax } => true,
                var operation => operation.Type.IsEqualTo(lockType),
            };
        }

        public void HandleOperation(IFieldInitializerOperation operation)
        {
            foreach (var field in operation.InitializedFields)
            {
                if (IsPotentialSymbol(field) && !IsSupportedValue(operation.Value))
                {
                    ExcludeSymbol(field);
                }
            }
        }

        public void HandleOperation(IVariableDeclaratorOperation operation)
        {
            var symbol = operation.Symbol;
            if (IsPotentialSymbol(symbol) && operation.GetVariableInitializer() is { } initializer && !IsSupportedValue(initializer.Value))
            {
                ExcludeSymbol(symbol);
            }
        }

        public void HandleOperation(ILocalReferenceOperation operation)
        {
            var symbol = operation.Local;
            HandleOperation(symbol, operation);
        }

        public void HandleOperation(IFieldReferenceOperation operation)
        {
            var symbol = operation.Field;
            HandleOperation(symbol, operation);
        }

        private void HandleOperation(ISymbol symbol, IOperation operation)
        {
            if (!IsPotentialSymbol(symbol))
                return;

            // Assignment targets (e.g., initializations in constructors) are not usages, but the assigned value must be compatible with the new type
            if (operation.Parent is IAssignmentOperation assignment && assignment.Target == operation)
            {
                if (assignment is not (ISimpleAssignmentOperation or ICoalesceAssignmentOperation) || !IsSupportedValue(assignment.Value))
                {
                    ExcludeSymbol(symbol);
                }

                return;
            }

            if (operation.Parent is not ILockOperation)
            {
                ExcludeSymbol(symbol);
            }
            else
            {
                AddPotentialSymbol(symbol);
            }
        }

        public void ExcludeSymbol(ISymbol symbol) => _symbols.AddOrUpdate(symbol, addValue: false, (_, _) => false);

        public void AddPotentialSymbol(ISymbol symbol) => _symbols.TryAdd(symbol, value: true);
    }
}
