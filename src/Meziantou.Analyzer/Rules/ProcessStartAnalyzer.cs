using System.Runtime.InteropServices;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProcessStartAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor UseShellExecuteMustBeExplicitlySet = new(
        RuleIdentifiers.UseShellExecuteMustBeSet,
        title: "UseShellExecute must be explicitly set",
        messageFormat: "UseShellExecute must be explicitly set when initializing a ProcessStartInfo",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseShellExecuteMustBeSet));

    private static readonly DiagnosticDescriptor UseProcessStartOverload = new(
        RuleIdentifiers.UseProcessStartOverload,
        title: "Use Process.Start overload with ProcessStartInfo",
        messageFormat: "Use an overload of Process.Start that has a ProcessStartInfo parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseProcessStartOverload));

    private static readonly DiagnosticDescriptor SetToFalseWhenRedirectingOutput = new(
        RuleIdentifiers.UseShellExecuteMustBeFalse,
        title: "UseShellExecute must be false when redirecting standard input or output",
        messageFormat: "Set UseShellExecute to false when redirecting standard input or output",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseShellExecuteMustBeFalse));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(UseShellExecuteMustBeExplicitlySet, SetToFalseWhenRedirectingOutput, UseProcessStartOverload);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (!analyzerContext.IsValid)
                return;

            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeObjectCreation, OperationKind.ObjectCreation);
        });

    }

    private enum PropertyValue
    {
        NotSet,
        False,
        True,
        Unknown,
    }

    // The property names cannot use nameof as System.Diagnostics.ProcessStartInfo is banned in analyzers (RS1035)
    [StructLayout(LayoutKind.Auto)]
    private struct ProcessStartInfoProperties
    {
        public PropertyValue UseShellExecute { get; private set; }
        private PropertyValue RedirectStandardError { get; set; }
        private PropertyValue RedirectStandardInput { get; set; }
        private PropertyValue RedirectStandardOutput { get; set; }

        public readonly bool IsRedirecting
            => RedirectStandardError is PropertyValue.True || RedirectStandardInput is PropertyValue.True || RedirectStandardOutput is PropertyValue.True;

        public void Set(string propertyName, PropertyValue value)
        {
            switch (propertyName)
            {
                case "UseShellExecute":
                    UseShellExecute = value;
                    break;

                case "RedirectStandardError":
                    RedirectStandardError = value;
                    break;

                case "RedirectStandardInput":
                    RedirectStandardInput = value;
                    break;

                case "RedirectStandardOutput":
                    RedirectStandardOutput = value;
                    break;
            }
        }
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly INamedTypeSymbol? _processStartInfoSymbol = compilation.GetBestTypeByMetadataName("System.Diagnostics.ProcessStartInfo");

        private readonly INamedTypeSymbol? _processSymbol = compilation.GetBestTypeByMetadataName("System.Diagnostics.Process");

        public bool IsValid => _processStartInfoSymbol is not null;

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (IsProcessStartInvocation(operation))
            {
                if (!operation.Arguments.Any(IsProcessStartInfo))
                {
                    // Calling Process.Start without ProcessStartInfo
                    context.ReportDiagnostic(UseProcessStartOverload, operation);
                }
            }
        }

        public void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            var operation = (IObjectCreationOperation)context.Operation;
            if (!IsProcessStartInfoCreation(operation))
                return;

            var properties = GetProperties(operation);
            if (properties.IsRedirecting)
            {
                if (properties.UseShellExecute is PropertyValue.NotSet or PropertyValue.True)
                {
                    // Redirecting standard input or output while UseShellExecute is not explicitly set to false
                    context.ReportDiagnostic(SetToFalseWhenRedirectingOutput, operation);
                }
            }
            else if (properties.UseShellExecute is PropertyValue.NotSet)
            {
                // Constructing ProcessStartInfo without setting UseShellExecute
                context.ReportDiagnostic(UseShellExecuteMustBeExplicitlySet, operation);
            }
        }

        private ProcessStartInfoProperties GetProperties(IObjectCreationOperation operation)
        {
            var result = default(ProcessStartInfoProperties);
            if (operation.Initializer is not null)
            {
                foreach (var assignment in operation.Initializer.Initializers.OfType<ISimpleAssignmentOperation>())
                {
                    if (assignment.Target is IPropertyReferenceOperation propertyReference)
                    {
                        result.Set(propertyReference.Property.Name, GetAssignedValue(assignment.Value));
                    }
                }
            }

            // The properties can also be set after the object is created:
            //   var psi = new ProcessStartInfo();
            //   psi.UseShellExecute = false;
            var target = GetAssignmentTargetSymbol(operation);
            if (target is not null)
            {
                foreach (var descendant in GetRootOperation(operation).Descendants())
                {
                    // Only the assignments that follow the creation apply to the created instance
                    if (descendant is ISimpleAssignmentOperation { Target: IPropertyReferenceOperation { Instance: { } instance } propertyReference } assignment
                        && descendant.Syntax.SpanStart > operation.Syntax.SpanStart
                        && propertyReference.Property.ContainingType.IsEqualTo(_processStartInfoSymbol)
                        && target.IsEqualTo(GetReferencedSymbol(instance)))
                    {
                        result.Set(propertyReference.Property.Name, GetAssignedValue(assignment.Value));
                    }
                }
            }

            return result;
        }

        private static PropertyValue GetAssignedValue(IOperation operation) => operation.ConstantValue switch
        {
            { HasValue: true, Value: true } => PropertyValue.True,
            { HasValue: true, Value: false } => PropertyValue.False,
            _ => PropertyValue.Unknown,
        };

        private static ISymbol? GetAssignmentTargetSymbol(IObjectCreationOperation operation) => operation.Parent switch
        {
            IVariableInitializerOperation { Parent: IVariableDeclaratorOperation declarator } => declarator.Symbol,
            ISimpleAssignmentOperation assignment => GetReferencedSymbol(assignment.Target),
            _ => null,
        };

        private static ISymbol? GetReferencedSymbol(IOperation? operation) => operation switch
        {
            ILocalReferenceOperation localReference => localReference.Local,
            IParameterReferenceOperation parameterReference => parameterReference.Parameter,
            IFieldReferenceOperation { Instance: null or IInstanceReferenceOperation } fieldReference => fieldReference.Field,
            IPropertyReferenceOperation { Instance: null or IInstanceReferenceOperation } propertyReference => propertyReference.Property,
            _ => null,
        };

        private static IOperation GetRootOperation(IOperation operation)
        {
            var result = operation;
            while (result.Parent is not null)
            {
                result = result.Parent;
            }

            return result;
        }

        private bool IsProcessStartInfo(IArgumentOperation operation)
            => operation.Value.Type.IsEqualTo(_processStartInfoSymbol);

        private bool IsProcessStartInfoCreation(IObjectCreationOperation operation)
            => operation.Type.IsEqualTo(_processStartInfoSymbol);

        private bool IsProcessStartInvocation(IInvocationOperation operation)
            => operation.TargetMethod.Name == "Start"
            && operation.TargetMethod.ContainingType.IsEqualTo(_processSymbol)
            && operation.TargetMethod.IsStatic;
    }
}
