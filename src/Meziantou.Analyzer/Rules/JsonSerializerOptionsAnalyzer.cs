using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class JsonSerializerOptionsAnalyzer : DiagnosticAnalyzer
{
    private const string RespectNullableAnnotationsPropertyName = "RespectNullableAnnotations";
    private const string RespectRequiredConstructorParametersPropertyName = "RespectRequiredConstructorParameters";

    private static readonly DiagnosticDescriptor RespectNullableAnnotationsRule = new(
        RuleIdentifiers.SetRespectNullableAnnotationsOnJsonSerializerOptions,
        title: "JsonSerializerOptions should set RespectNullableAnnotations",
        messageFormat: "Set RespectNullableAnnotations on the JsonSerializerOptions instance",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SetRespectNullableAnnotationsOnJsonSerializerOptions));

    private static readonly DiagnosticDescriptor RespectRequiredConstructorParametersRule = new(
        RuleIdentifiers.SetRespectRequiredConstructorParametersOnJsonSerializerOptions,
        title: "JsonSerializerOptions should set RespectRequiredConstructorParameters",
        messageFormat: "Set RespectRequiredConstructorParameters on the JsonSerializerOptions instance",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SetRespectRequiredConstructorParametersOnJsonSerializerOptions));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RespectNullableAnnotationsRule, RespectRequiredConstructorParametersRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var analyzerContext = new AnalyzerContext(compilationContext.Compilation);
            if (!analyzerContext.IsValid)
                return;

            compilationContext.RegisterOperationAction(analyzerContext.AnalyzeObjectCreation, OperationKind.ObjectCreation);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly INamedTypeSymbol? _optionsSymbol = compilation.GetTypeByMetadataName("System.Text.Json.JsonSerializerOptions");

        // JsonSerializerDefaults.Strict, introduced in .NET 10, sets both properties
        private readonly IFieldSymbol? _strictDefaults = compilation.GetTypeByMetadataName("System.Text.Json.JsonSerializerDefaults")?
            .GetMembers("Strict")
            .OfType<IFieldSymbol>()
            .FirstOrDefault(field => field.HasConstantValue);

        // Both properties were introduced in .NET 9
        private bool SupportsRespectNullableAnnotations => HasBooleanProperty(_optionsSymbol, RespectNullableAnnotationsPropertyName);

        private bool SupportsRespectRequiredConstructorParameters => HasBooleanProperty(_optionsSymbol, RespectRequiredConstructorParametersPropertyName);

        public bool IsValid => _optionsSymbol is not null
            && (SupportsRespectNullableAnnotations || SupportsRespectRequiredConstructorParameters);

        public void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            var operation = (IObjectCreationOperation)context.Operation;
            if (!operation.Type.IsEqualTo(_optionsSymbol))
                return;

            // The copy constructor copies the options of an instance that can be configured somewhere else
            if (operation.Arguments is [{ Parameter.Type: { } parameterType }] && parameterType.IsEqualTo(_optionsSymbol))
                return;

            var setByDefaults = UsesStrictDefaults(operation);

            if (SupportsRespectNullableAnnotations && !IsSet(operation, RespectNullableAnnotationsPropertyName, setByDefaults))
            {
                context.ReportDiagnostic(RespectNullableAnnotationsRule, GetLocation(operation));
            }

            if (SupportsRespectRequiredConstructorParameters && !IsSet(operation, RespectRequiredConstructorParametersPropertyName, setByDefaults))
            {
                context.ReportDiagnostic(RespectRequiredConstructorParametersRule, GetLocation(operation));
            }
        }

        private bool UsesStrictDefaults(IObjectCreationOperation operation)
        {
            if (_strictDefaults is null || operation.Arguments is not [var argument])
                return false;

            return argument.Parameter?.Type.IsEqualTo(_strictDefaults.ContainingType) is true
                && argument.Value.ConstantValue is { HasValue: true, Value: var value }
                && Equals(value, _strictDefaults.ConstantValue);
        }

        private static bool HasBooleanProperty(INamedTypeSymbol? symbol, string propertyName)
        {
            if (symbol is null)
                return false;

            foreach (var member in symbol.GetMembers(propertyName))
            {
                if (member is IPropertySymbol { Type.SpecialType: SpecialType.System_Boolean })
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Indicates whether the option is explicitly configured, whatever the value it is set to.
        /// </summary>
        private static bool IsSet(IObjectCreationOperation operation, string propertyName, bool setByDefaults)
        {
            if (setByDefaults)
                return true;

            if (operation.Initializer is { } initializer)
            {
                foreach (var initializerOperation in initializer.Initializers)
                {
                    if (initializerOperation is ISimpleAssignmentOperation { Target: IPropertyReferenceOperation property } && IsProperty(property, propertyName))
                        return true;
                }
            }

            return IsSetOnLocal(operation, propertyName);
        }

        /// <summary>
        /// Indicates whether the created instance is stored in a local that a following statement configures,
        /// such as <c>var options = new JsonSerializerOptions(); options.RespectNullableAnnotations = true;</c>.
        /// Only the statements that run before another instance is assigned to the local are considered, as the
        /// following ones configure that other instance.
        /// </summary>
        private static bool IsSetOnLocal(IObjectCreationOperation operation, string propertyName)
        {
            if (GetAssignedLocal(operation) is not { } local)
                return false;

            foreach (var statement in GetFollowingStatements(operation))
            {
                foreach (var descendant in statement.DescendantsAndSelf())
                {
                    if (descendant is ISimpleAssignmentOperation { Target: IPropertyReferenceOperation { Instance: ILocalReferenceOperation localReference } property }
                        && IsProperty(property, propertyName)
                        && local.IsEqualTo(localReference.Local))
                    {
                        return true;
                    }

                    if (IsAssignedTo(descendant, local))
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the statements that run after the statement containing the operation, in the order they run.
        /// </summary>
        private static IEnumerable<IOperation> GetFollowingStatements(IOperation operation)
        {
            for (var current = operation; current.Parent is { } parent; current = parent)
            {
                var statements = parent switch
                {
                    IBlockOperation block => block.Operations,
                    ISwitchCaseOperation switchCase => switchCase.Body,
                    _ => ImmutableArray<IOperation>.Empty,
                };

                for (var i = statements.IndexOf(current) + 1; i < statements.Length; i++)
                {
                    yield return statements[i];
                }
            }
        }

        /// <summary>
        /// Indicates whether the operation assigns another value to the local.
        /// </summary>
        private static bool IsAssignedTo(IOperation operation, ILocalSymbol local) => operation switch
        {
            IAssignmentOperation { Target: ILocalReferenceOperation localReference } => local.IsEqualTo(localReference.Local),
            IArgumentOperation { Parameter.RefKind: RefKind.Ref or RefKind.Out, Value: ILocalReferenceOperation localReference } => local.IsEqualTo(localReference.Local),
            _ => false,
        };

        private static bool IsProperty(IPropertyReferenceOperation operation, string propertyName)
            => string.Equals(operation.Property.Name, propertyName, StringComparison.Ordinal);

        private static ILocalSymbol? GetAssignedLocal(IObjectCreationOperation operation) => operation.Parent switch
        {
            IVariableInitializerOperation { Parent: IVariableDeclaratorOperation declarator } => declarator.Symbol,
            ISimpleAssignmentOperation { Target: ILocalReferenceOperation localReference } => localReference.Local,
            _ => null,
        };

        /// <summary>
        /// Gets the location of the creation without its object initializer, which can span many lines.
        /// </summary>
        private static Location GetLocation(IObjectCreationOperation operation)
        {
            if (operation.Syntax is not BaseObjectCreationExpressionSyntax { Initializer: not null } creation)
                return operation.Syntax.GetLocation();

            var end = creation.ArgumentList?.Span.End ?? (creation as ObjectCreationExpressionSyntax)?.Type.Span.End ?? creation.NewKeyword.Span.End;
            return Location.Create(creation.SyntaxTree, TextSpan.FromBounds(creation.SpanStart, end));
        }
    }
}
