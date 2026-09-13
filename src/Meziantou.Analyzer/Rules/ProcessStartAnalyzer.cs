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

    /// <summary>
    /// The values a property can have when the process is started. It can have multiple values when it is
    /// assigned by an operation that may not be executed, such as the body of an <c>if</c> statement.
    /// </summary>
    [Flags]
    private enum PropertyValues
    {
        None = 0x0,
        NotSet = 0x1,
        False = 0x2,
        True = 0x4,
        Unknown = 0x8,
    }

    // The property names cannot use nameof as System.Diagnostics.ProcessStartInfo is banned in analyzers (RS1035)
    [StructLayout(LayoutKind.Auto)]
    private struct ProcessStartInfoProperties()
    {
        private PropertyValues UseShellExecute { get; set; } = PropertyValues.NotSet;
        private PropertyValues RedirectStandardError { get; set; } = PropertyValues.NotSet;
        private PropertyValues RedirectStandardInput { get; set; } = PropertyValues.NotSet;
        private PropertyValues RedirectStandardOutput { get; set; } = PropertyValues.NotSet;

        public readonly bool IsRedirecting
            => ((RedirectStandardError | RedirectStandardInput | RedirectStandardOutput) & PropertyValues.True) is not PropertyValues.None;

        /// <summary>
        /// The shell can be used to start the process, either because UseShellExecute can be true or because
        /// it can keep its default value, which is true on .NET Framework.
        /// </summary>
        public readonly bool CanUseShellExecute
            => (UseShellExecute & (PropertyValues.NotSet | PropertyValues.True)) is not PropertyValues.None;

        public readonly bool IsUseShellExecuteNotSet => UseShellExecute is PropertyValues.NotSet;

        /// <summary>
        /// Replaces the values the property can have, for an assignment that is always executed.
        /// </summary>
        public void Set(string propertyName, PropertyValues values) => Update(propertyName, values, replaceExistingValues: true);

        /// <summary>
        /// Adds a value the property can have, for an assignment that may not be executed.
        /// </summary>
        public void AddPossibleValues(string propertyName, PropertyValues values) => Update(propertyName, values, replaceExistingValues: false);

        private void Update(string propertyName, PropertyValues values, bool replaceExistingValues)
        {
            switch (propertyName)
            {
                case "UseShellExecute":
                    UseShellExecute = Merge(UseShellExecute, values, replaceExistingValues);
                    break;

                case "RedirectStandardError":
                    RedirectStandardError = Merge(RedirectStandardError, values, replaceExistingValues);
                    break;

                case "RedirectStandardInput":
                    RedirectStandardInput = Merge(RedirectStandardInput, values, replaceExistingValues);
                    break;

                case "RedirectStandardOutput":
                    RedirectStandardOutput = Merge(RedirectStandardOutput, values, replaceExistingValues);
                    break;
            }

            static PropertyValues Merge(PropertyValues currentValues, PropertyValues newValues, bool replaceExistingValues)
                => replaceExistingValues ? newValues : currentValues | newValues;
        }
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly INamedTypeSymbol? _processStartInfoSymbol = compilation.GetTypeByMetadataName("System.Diagnostics.ProcessStartInfo");

        private readonly INamedTypeSymbol? _processSymbol = compilation.GetTypeByMetadataName("System.Diagnostics.Process");

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
                if (properties.CanUseShellExecute)
                {
                    // Redirecting standard input or output while UseShellExecute is not explicitly set to false
                    context.ReportDiagnostic(SetToFalseWhenRedirectingOutput, operation);
                }
            }
            else if (properties.IsUseShellExecuteNotSet)
            {
                // Constructing ProcessStartInfo without setting UseShellExecute
                context.ReportDiagnostic(UseShellExecuteMustBeExplicitlySet, operation);
            }
        }

        private ProcessStartInfoProperties GetProperties(IObjectCreationOperation operation)
        {
            var result = new ProcessStartInfoProperties();
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
                var root = GetRootOperation(operation);
                var assignments = GetPropertyAssignments(root, operation, target);

                // The configuration that matters is the one the instance has when the process is started
                var startOperation = FindProcessStart(root, operation, target);

                // The assignments of a nested function are executed when the function is invoked, so they cannot
                // be ordered with the start of the process
                if (startOperation is not null && assignments.Any(item => IsInNestedFunction(item.Assignment, operation)))
                {
                    startOperation = null;
                }

                foreach (var (propertyReference, assignment) in assignments)
                {
                    // The assignments that follow the start of the process cannot change its configuration
                    if (startOperation is not null && assignment.Syntax.SpanStart > startOperation.Syntax.SpanStart)
                        continue;

                    var value = GetAssignedValue(assignment.Value);
                    if (startOperation is null || IsAlwaysExecutedBefore(assignment, startOperation))
                    {
                        result.Set(propertyReference.Property.Name, value);
                    }
                    else
                    {
                        // The assignment may not be executed, so the property can also keep its previous values
                        result.AddPossibleValues(propertyReference.Property.Name, value);
                    }
                }
            }

            return result;
        }

        private List<(IPropertyReferenceOperation PropertyReference, ISimpleAssignmentOperation Assignment)> GetPropertyAssignments(IOperation root, IObjectCreationOperation operation, ISymbol target)
        {
            var result = new List<(IPropertyReferenceOperation, ISimpleAssignmentOperation)>();
            foreach (var descendant in root.Descendants())
            {
                // Only the assignments that follow the creation apply to the created instance
                if (descendant is ISimpleAssignmentOperation { Target: IPropertyReferenceOperation { Instance: { } instance } propertyReference } assignment
                    && descendant.Syntax.SpanStart > operation.Syntax.SpanStart
                    && propertyReference.Property.ContainingType.IsEqualTo(_processStartInfoSymbol)
                    && target.IsEqualTo(GetReferencedSymbol(instance)))
                {
                    result.Add((propertyReference, assignment));
                }
            }

            return result;
        }

        /// <summary>
        /// Finds the first call to Process.Start that uses the created instance, as this is where its
        /// configuration is used.
        /// </summary>
        private IOperation? FindProcessStart(IOperation root, IObjectCreationOperation operation, ISymbol target)
        {
            foreach (var descendant in root.Descendants())
            {
                if (descendant.Syntax.SpanStart <= operation.Syntax.SpanStart)
                    continue;

                if (descendant is not IInvocationOperation invocation || !IsProcessStartInvocation(invocation))
                    continue;

                if (!invocation.Arguments.Any(argument => target.IsEqualTo(GetReferencedSymbol(argument.Value))))
                    continue;

                // A nested function is invoked at an unknown time, which can be before the operations that precede it
                if (IsInNestedFunction(invocation, operation))
                    continue;

                // The descendants are enumerated in source order, so this is the first start of the process
                return invocation;
            }

            return null;
        }

        /// <summary>
        /// Indicates whether the operation is executed every time the process is started. This is an approximation
        /// as it only relies on the position of the operations and on the operations that can skip them.
        /// </summary>
        private static bool IsAlwaysExecutedBefore(IOperation operation, IOperation use)
        {
            if (operation.Syntax.SpanStart > use.Syntax.SpanStart)
                return false;

            var child = operation;
            foreach (var ancestor in operation.Ancestors())
            {
                if (ancestor.Syntax.Span.Contains(use.Syntax.Span))
                {
                    // The operations are in the same branch when the branch that contains the operation also contains the use
                    return !CanBeSkipped(ancestor) || child.Syntax.Span.Contains(use.Syntax.Span);
                }

                if (CanBeSkipped(ancestor))
                    return false;

                child = ancestor;
            }

            return true;
        }

        private static bool IsInNestedFunction(IOperation operation, IObjectCreationOperation creation)
        {
            foreach (var ancestor in operation.Ancestors())
            {
                if (ancestor.Syntax.Span.Contains(creation.Syntax.Span))
                    return false;

                if (ancestor is IAnonymousFunctionOperation or ILocalFunctionOperation)
                    return true;
            }

            return false;
        }

        private static bool CanBeSkipped(IOperation operation) => operation is
            IConditionalOperation or
            ILoopOperation or
            ISwitchOperation or
            ISwitchCaseOperation or
            ISwitchExpressionOperation or
            ISwitchExpressionArmOperation or
            ICatchClauseOperation or
            IConditionalAccessOperation or
            ICoalesceOperation or
            ICoalesceAssignmentOperation or
            IAnonymousFunctionOperation or
            ILocalFunctionOperation or
            IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr };

        private static PropertyValues GetAssignedValue(IOperation operation) => operation.ConstantValue switch
        {
            { HasValue: true, Value: true } => PropertyValues.True,
            { HasValue: true, Value: false } => PropertyValues.False,
            _ => PropertyValues.Unknown,
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
