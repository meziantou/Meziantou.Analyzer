using System.Globalization;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventSourceImplementationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor EventIdMustBePositiveRule = CreateRule(
        RuleIdentifiers.EventSourceEventIdMustBePositive,
        title: "The event id of an EventSource must be greater than zero",
        messageFormat: "The event id must be greater than zero");

    private static readonly DiagnosticDescriptor DuplicateEventIdRule = CreateRule(
        RuleIdentifiers.EventSourceDuplicateEventId,
        title: "The event id of an EventSource is already used by another event",
        messageFormat: "The event id '{0}' is already used by the event '{1}'");

    private static readonly DiagnosticDescriptor DuplicateEventNameRule = CreateRule(
        RuleIdentifiers.EventSourceDuplicateEventName,
        title: "The event name of an EventSource is already used by another event",
        messageFormat: "The event name '{0}' is already used by another event");

    private static readonly DiagnosticDescriptor EventMethodMustNotBeStaticRule = CreateRule(
        RuleIdentifiers.EventSourceEventMethodMustNotBeStatic,
        title: "An EventSource event method must not be static",
        messageFormat: "An event method must not be static");

    private static readonly DiagnosticDescriptor EventMethodMustNotBeAnExplicitInterfaceImplementationRule = CreateRule(
        RuleIdentifiers.EventSourceEventMethodMustNotBeAnExplicitInterfaceImplementation,
        title: "An EventSource event method must not be an explicit interface implementation",
        messageFormat: "An event method must not be an explicit interface implementation");

    private static readonly DiagnosticDescriptor AbstractTypeMustNotDeclareEventMethodsRule = CreateRule(
        RuleIdentifiers.EventSourceAbstractTypeMustNotDeclareEventMethods,
        title: "An abstract EventSource must not declare event methods",
        messageFormat: "An abstract EventSource must not declare event methods");

    private static readonly DiagnosticDescriptor MismatchedEventIdRule = CreateRule(
        RuleIdentifiers.EventSourceMismatchedEventId,
        title: "The event id written by an EventSource event method must match its [Event] attribute",
        messageFormat: "'{0}' is called with the event id '{1}' but the method declares the event id '{2}'");

    private static readonly DiagnosticDescriptor MismatchedPayloadRule = CreateRule(
        RuleIdentifiers.EventSourceMismatchedPayload,
        title: "The payload written by an EventSource event method must match its parameters",
        messageFormat: "'{0}' writes {1} payload item(s) but the event method declares {2} payload parameter(s)");

    private static readonly DiagnosticDescriptor MismatchedPayloadOrderRule = CreateRule(
        RuleIdentifiers.EventSourceMismatchedPayloadOrder,
        title: "The payload written by an EventSource event method must use the order of its parameters",
        messageFormat: "'{0}' must write the payload parameters of the event method in the order they are declared");

    private static readonly DiagnosticDescriptor MissingRelatedActivityIdParameterRule = CreateRule(
        RuleIdentifiers.EventSourceMissingRelatedActivityIdParameter,
        title: "An EventSource event method writing a related activity id must declare it as its first parameter",
        messageFormat: "The first parameter of an event method calling '{0}' must be a 'Guid' named 'relatedActivityId'");

    private static readonly DiagnosticDescriptor UnsupportedParameterTypeRule = CreateRule(
        RuleIdentifiers.EventSourceUnsupportedParameterType,
        title: "The parameter type of an EventSource event method is not supported",
        messageFormat: "The type '{0}' is not supported by EventSource");

    private static DiagnosticDescriptor CreateRule(string ruleIdentifier, string title, string messageFormat) => new(
        ruleIdentifier,
        title: title,
        messageFormat: messageFormat,
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(ruleIdentifier));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        EventIdMustBePositiveRule,
        DuplicateEventIdRule,
        DuplicateEventNameRule,
        EventMethodMustNotBeStaticRule,
        EventMethodMustNotBeAnExplicitInterfaceImplementationRule,
        AbstractTypeMustNotDeclareEventMethodsRule,
        MismatchedEventIdRule,
        MismatchedPayloadRule,
        MismatchedPayloadOrderRule,
        MissingRelatedActivityIdParameterRule,
        UnsupportedParameterTypeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (!analyzerContext.IsValid)
                return;

            ctx.RegisterSymbolStartAction(analyzerContext.AnalyzeNamedType, SymbolKind.NamedType);
        });
    }

    private sealed class AnalyzerContext
    {
        // The Microsoft.Diagnostics.Tracing.EventSource NuGet package is a copy of the BCL implementation in another namespace
        private static readonly string[] Namespaces = ["System.Diagnostics.Tracing", "Microsoft.Diagnostics.Tracing"];

        private readonly ImmutableArray<INamedTypeSymbol> _eventSourceSymbols;
        private readonly ImmutableArray<INamedTypeSymbol> _eventAttributeSymbols;
        private readonly ImmutableArray<INamedTypeSymbol> _nonEventAttributeSymbols;
        private readonly ImmutableArray<INamedTypeSymbol> _eventSourceSettingsSymbols;
        private readonly INamedTypeSymbol? _guidSymbol;

        public AnalyzerContext(Compilation compilation)
        {
            _eventSourceSymbols = GetSymbols(compilation, "EventSource");
            _eventAttributeSymbols = GetSymbols(compilation, "EventAttribute");
            _nonEventAttributeSymbols = GetSymbols(compilation, "NonEventAttribute");
            _eventSourceSettingsSymbols = GetSymbols(compilation, "EventSourceSettings");
            _guidSymbol = compilation.GetBestTypeByMetadataName("System.Guid");
        }

        public bool IsValid => !_eventSourceSymbols.IsEmpty;

        private static ImmutableArray<INamedTypeSymbol> GetSymbols(Compilation compilation, string typeName)
        {
            var result = ImmutableArray.CreateBuilder<INamedTypeSymbol>(Namespaces.Length);
            foreach (var ns in Namespaces)
            {
                var symbol = compilation.GetBestTypeByMetadataName(ns + "." + typeName);
                if (symbol is not null)
                {
                    result.Add(symbol);
                }
            }

            return result.ToImmutable();
        }

        private static bool IsAnyOf(ITypeSymbol? symbol, ImmutableArray<INamedTypeSymbol> expectedSymbols)
        {
            foreach (var expectedSymbol in expectedSymbols)
            {
                if (symbol.IsEqualTo(expectedSymbol))
                    return true;
            }

            return false;
        }

        public bool IsEventSourceBaseType(ITypeSymbol? symbol) => IsAnyOf(symbol, _eventSourceSymbols);

        public bool IsEventSourceSettings(ITypeSymbol? symbol) => IsAnyOf(symbol, _eventSourceSettingsSymbols);

        public bool InheritsFromEventSource(ITypeSymbol? symbol)
        {
            if (symbol is null)
                return false;

            foreach (var eventSourceSymbol in _eventSourceSymbols)
            {
                if (symbol.InheritsFrom(eventSourceSymbol))
                    return true;
            }

            return false;
        }

        public bool IsEventSourceOrDerived(ITypeSymbol? symbol) => IsEventSourceBaseType(symbol) || InheritsFromEventSource(symbol);

        private static AttributeData? GetAttribute(ISymbol symbol, ImmutableArray<INamedTypeSymbol> attributeSymbols)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (IsAnyOf(attribute.AttributeClass, attributeSymbols))
                    return attribute;
            }

            return null;
        }

        /// <summary>
        /// Determines if the method is considered as an event by the runtime, using the same rules as
        /// <c>EventSource.CreateManifestAndDescriptors</c>.
        /// </summary>
        public bool IsEventMethod(IMethodSymbol method, out AttributeData? eventAttribute)
        {
            eventAttribute = null;
            if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation))
                return false;

            // The [Event] attribute takes precedence over everything else, including [NonEvent]
            eventAttribute = GetAttribute(method, _eventAttributeSymbols);
            if (eventAttribute is not null)
                return true;

            if (GetAttribute(method, _nonEventAttributeSymbols) is not null)
                return false;

            // Without the [Event] attribute, the runtime ignores the methods that do not return void and the virtual methods.
            // Non-public methods are only ignored by these rules, as reporting them would be too noisy.
            return method is { IsStatic: false, ReturnsVoid: true, IsVirtual: false, IsOverride: false, IsAbstract: false, DeclaredAccessibility: Accessibility.Public };
        }

        public bool IsRelatedActivityIdParameter(IParameterSymbol parameter)
            => parameter.Type.IsEqualTo(_guidSymbol) && string.Equals(parameter.Name, "relatedActivityId", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines if the type can be written to the manifest of an event source, using the same rules as
        /// <c>ManifestBuilder.GetTypeName</c>.
        /// </summary>
        public bool IsSupportedParameterType(ITypeSymbol type)
        {
            if (type.TypeKind is TypeKind.Enum && type is INamedTypeSymbol { EnumUnderlyingType: not null } enumType)
            {
                type = enumType.EnumUnderlyingType;
            }

            if (type.SpecialType is SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte
                or SpecialType.System_Char or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64
                or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double
                or SpecialType.System_String or SpecialType.System_DateTime or SpecialType.System_IntPtr)
            {
                return true;
            }

            if (type.IsEqualTo(_guidSymbol))
                return true;

            // byte[] and byte* are written as a binary blob
            return type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Byte }
                or IPointerTypeSymbol { PointedAtType.SpecialType: SpecialType.System_Byte };
        }

        public void AnalyzeNamedType(SymbolStartAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (symbol.TypeKind is not TypeKind.Class || symbol.IsStatic)
                return;

            if (!InheritsFromEventSource(symbol))
                return;

            var typeContext = new EventSourceTypeContext(this, symbol);
            context.RegisterOperationBlockAction(typeContext.AnalyzeOperationBlock);
            context.RegisterSymbolEndAction(typeContext.ReportTypeDiagnostics);
        }
    }

    private sealed class EventSourceTypeContext(AnalyzerContext analyzerContext, INamedTypeSymbol symbol)
    {
        // Written by the operation block actions, which run before the symbol end action
        private volatile bool _useSelfDescribingEvents;

        public void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
        {
            AttributeData? eventAttribute = null;
            var eventMethod = !symbol.IsAbstract && context.OwningSymbol is IMethodSymbol method && analyzerContext.IsEventMethod(method, out eventAttribute) ? method : null;

            foreach (var block in context.OperationBlocks)
            {
                foreach (var operation in block.DescendantsAndSelf())
                {
                    // EventSourceSettings is provided to the base constructor, and it may enable self-describing
                    // events, in which case the payload is not validated against a manifest
                    if (analyzerContext.IsEventSourceSettings(operation.Type))
                    {
                        _useSelfDescribingEvents = true;
                    }

                    if (eventMethod is not null && operation is IInvocationOperation invocation)
                    {
                        AnalyzeWriteEventInvocation(context, eventMethod, eventAttribute, invocation);
                    }
                }
            }
        }

        private void AnalyzeWriteEventInvocation(OperationBlockAnalysisContext context, IMethodSymbol eventMethod, AttributeData? eventAttribute, IInvocationOperation invocation)
        {
            var targetMethod = invocation.TargetMethod;
            if (targetMethod.Name is not ("WriteEvent" or "WriteEventCore" or "WriteEventWithRelatedActivityId" or "WriteEventWithRelatedActivityIdCore"))
                return;

            // Custom overloads can be declared by a "Utility EventSource"
            if (!analyzerContext.IsEventSourceOrDerived(targetMethod.ContainingType))
                return;

            var arguments = GetArguments(invocation);
            if (arguments is null || arguments.Count is 0)
                return;

            if (TryGetEventId(eventAttribute, out var declaredEventId) && declaredEventId > 0 &&
                arguments[0].ConstantValue is { HasValue: true, Value: int writtenEventId } && writtenEventId != declaredEventId)
            {
                context.ReportDiagnostic(MismatchedEventIdRule, arguments[0], targetMethod.Name, writtenEventId.ToString(CultureInfo.InvariantCulture), declaredEventId.ToString(CultureInfo.InvariantCulture));
            }

            // The runtime removes the first parameter from the payload when it is the related activity id
            var payloadParameters = eventMethod.Parameters;
            var hasRelatedActivityIdParameter = payloadParameters.Length > 0 && analyzerContext.IsRelatedActivityIdParameter(payloadParameters[0]);
            if (hasRelatedActivityIdParameter)
            {
                payloadParameters = payloadParameters.RemoveAt(0);
            }

            if (targetMethod.Name is "WriteEventWithRelatedActivityId" or "WriteEventWithRelatedActivityIdCore" && !hasRelatedActivityIdParameter)
            {
                context.ReportDiagnostic(MissingRelatedActivityIdParameterRule, invocation, targetMethod.Name);
                return;
            }

            if (targetMethod.Name is "WriteEventCore" or "WriteEventWithRelatedActivityIdCore")
            {
                AnalyzeEventDataCount(context, invocation, payloadParameters.Length);
                return;
            }

            AnalyzePayloadArguments(context, invocation, arguments, payloadParameters);
        }

        private static void AnalyzeEventDataCount(OperationBlockAnalysisContext context, IInvocationOperation invocation, int payloadParameterCount)
        {
            foreach (var argument in invocation.Arguments)
            {
                if (argument.Parameter is null || !string.Equals(argument.Parameter.Name, "eventDataCount", StringComparison.Ordinal))
                    continue;

                if (argument.Value.ConstantValue is { HasValue: true, Value: int eventDataCount } && eventDataCount != payloadParameterCount)
                {
                    context.ReportDiagnostic(MismatchedPayloadRule, argument, invocation.TargetMethod.Name, eventDataCount.ToString(CultureInfo.InvariantCulture), payloadParameterCount.ToString(CultureInfo.InvariantCulture));
                }

                return;
            }
        }

        private static void AnalyzePayloadArguments(OperationBlockAnalysisContext context, IInvocationOperation invocation, List<IOperation> arguments, ImmutableArray<IParameterSymbol> payloadParameters)
        {
            // Skip the event id, and the related activity id which is not part of the payload
            var firstPayloadArgument = string.Equals(invocation.TargetMethod.Name, "WriteEventWithRelatedActivityId", StringComparison.Ordinal) ? 2 : 1;
            var payloadArgumentCount = arguments.Count - firstPayloadArgument;
            if (payloadArgumentCount < 0)
                return;

            if (payloadArgumentCount != payloadParameters.Length)
            {
                context.ReportDiagnostic(MismatchedPayloadRule, invocation, invocation.TargetMethod.Name, payloadArgumentCount.ToString(CultureInfo.InvariantCulture), payloadParameters.Length.ToString(CultureInfo.InvariantCulture));
                return;
            }

            // Only report the order when every argument is a parameter of the method, as the other expressions
            // cannot be matched to a parameter
            var isSameOrder = true;
            for (var i = 0; i < payloadParameters.Length; i++)
            {
                if (UnwrapConversions(arguments[firstPayloadArgument + i]) is not IParameterReferenceOperation parameterReference)
                    return;

                isSameOrder &= parameterReference.Parameter.IsEqualTo(payloadParameters[i]);
            }

            if (!isSameOrder)
            {
                context.ReportDiagnostic(MismatchedPayloadOrderRule, invocation, invocation.TargetMethod.Name);
            }
        }

        private static IOperation UnwrapConversions(IOperation operation)
        {
            while (operation is IConversionOperation conversion)
            {
                operation = conversion.Operand;
            }

            return operation;
        }

        /// <summary>
        /// Gets the arguments of the invocation in the order of the parameters, the ones of the <c>params</c> parameter
        /// being expanded. Returns <see langword="null"/> when the arguments cannot be matched to the parameters.
        /// </summary>
        private static List<IOperation>? GetArguments(IInvocationOperation invocation)
        {
            var result = new List<IOperation>(invocation.Arguments.Length);
            foreach (var argument in invocation.Arguments)
            {
                if (argument.Parameter?.IsParams is true)
                {
                    // An array can be provided instead of the expanded arguments
                    if (argument is not { ArgumentKind: ArgumentKind.ParamArray, Value: IArrayCreationOperation { Initializer: not null } arrayCreation })
                        return null;

                    foreach (var elementValue in arrayCreation.Initializer.ElementValues)
                    {
                        result.Add(elementValue);
                    }
                }
                else if (argument.ArgumentKind is ArgumentKind.Explicit)
                {
                    result.Add(argument.Value);
                }
                else
                {
                    return null;
                }
            }

            return result;
        }

        public void ReportTypeDiagnostics(SymbolAnalysisContext context)
        {
            var validateParameterTypes = !_useSelfDescribingEvents && analyzerContext.IsEventSourceBaseType(symbol.BaseType);
            var eventIds = new Dictionary<int, string>();
            var eventNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var member in symbol.GetMembers())
            {
                if (member is not IMethodSymbol method || !analyzerContext.IsEventMethod(method, out var eventAttribute))
                    continue;

                if (symbol.IsAbstract)
                {
                    // The events must be declared by the derived types, as the runtime only looks at the methods declared by the type itself
                    if (eventAttribute is not null)
                    {
                        context.ReportDiagnostic(AbstractTypeMustNotDeclareEventMethodsRule, GetLocation(eventAttribute, method, context.CancellationToken));
                    }

                    continue;
                }

                if (eventAttribute is not null)
                {
                    if (method.IsStatic)
                    {
                        context.ReportDiagnostic(EventMethodMustNotBeStaticRule, method);
                        continue;
                    }

                    if (method.MethodKind is MethodKind.ExplicitInterfaceImplementation)
                    {
                        context.ReportDiagnostic(EventMethodMustNotBeAnExplicitInterfaceImplementationRule, method);
                    }

                    if (TryGetEventId(eventAttribute, out var eventId))
                    {
                        if (eventId <= 0)
                        {
                            context.ReportDiagnostic(EventIdMustBePositiveRule, GetLocation(eventAttribute, method, context.CancellationToken));
                            continue;
                        }

                        if (eventIds.TryGetValue(eventId, out var otherEventName))
                        {
                            context.ReportDiagnostic(DuplicateEventIdRule, GetLocation(eventAttribute, method, context.CancellationToken), eventId.ToString(CultureInfo.InvariantCulture), otherEventName);
                        }
                        else
                        {
                            eventIds.Add(eventId, method.Name);
                        }
                    }
                }

                if (!eventNames.Add(method.Name))
                {
                    context.ReportDiagnostic(DuplicateEventNameRule, method, method.Name);
                }

                if (validateParameterTypes)
                {
                    foreach (var parameter in method.Parameters)
                    {
                        if (!analyzerContext.IsSupportedParameterType(parameter.Type))
                        {
                            context.ReportDiagnostic(UnsupportedParameterTypeRule, parameter, parameter.Type.ToDisplayString());
                        }
                    }
                }
            }
        }

        private static Location GetLocation(AttributeData attribute, ISymbol fallbackSymbol, CancellationToken cancellationToken)
            => attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? fallbackSymbol.Locations[0];

        private static bool TryGetEventId(AttributeData? attribute, out int eventId)
        {
            if (attribute is { ConstructorArguments: [{ Kind: TypedConstantKind.Primitive, Value: int value }, ..] })
            {
                eventId = value;
                return true;
            }

            eventId = 0;
            return false;
        }
    }
}
