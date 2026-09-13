namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptimizeStartsWithAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.OptimizeStartsWith,
        title: "Optimize string method usage",
        messageFormat: "Use an overload with char instead of string",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeStartsWith));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            StringComparisonSymbol = compilation.GetTypeByMetadataName("System.StringComparison");
            if (StringComparisonSymbol is not null)
            {
                StringComparison_Ordinal = StringComparisonSymbol.GetMembers(nameof(StringComparison.Ordinal)).FirstOrDefault();
                StringComparison_CurrentCulture = StringComparisonSymbol.GetMembers(nameof(StringComparison.CurrentCulture)).FirstOrDefault();
#pragma warning disable RS0030 // Do not use banned APIs
                StringComparison_InvariantCulture = StringComparisonSymbol.GetMembers(nameof(StringComparison.InvariantCulture)).FirstOrDefault();
#pragma warning restore RS0030
            }

            EnumerableOfTSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");

            var stringSymbol = compilation.GetSpecialType(SpecialType.System_String);
            if (stringSymbol is not null)
            {
                foreach (var method in stringSymbol.GetMembers(nameof(string.StartsWith)).OfType<IMethodSymbol>())
                {
                    if (!method.IsStatic && method.Parameters.Length == 1 && method.Parameters[0].Type.IsChar())
                    {
                        StartsWith_Char = method;
                        break;
                    }
                }

                foreach (var method in stringSymbol.GetMembers(nameof(string.EndsWith)).OfType<IMethodSymbol>())
                {
                    if (!method.IsStatic && method.Parameters.Length == 1 && method.Parameters[0].Type.IsChar())
                    {
                        EndsWith_Char = method;
                        break;
                    }
                }

                foreach (var method in stringSymbol.GetMembers(nameof(string.Replace)).OfType<IMethodSymbol>())
                {
                    if (!method.IsStatic && method.Parameters.Length == 2 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsChar())
                    {
                        Replace_Char_Char = method;
                        break;
                    }
                }

                foreach (var method in stringSymbol.GetMembers(nameof(string.IndexOf)).OfType<IMethodSymbol>())
                {
                    if (method.IsStatic)
                        continue;

                    if (method.Parameters.Length == 1 && method.Parameters[0].Type.IsChar())
                    {
                        IndexOf_Char = method;
                    }
                    else if (method.Parameters.Length == 2 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsInt32())
                    {
                        IndexOf_Char_Int32 = method;
                    }
                    else if (method.Parameters.Length == 3 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsInt32() && method.Parameters[2].Type.IsInt32())
                    {
                        IndexOf_Char_Int32_Int32 = method;
                    }
                    else if (method.Parameters.Length == 2 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsEqualTo(StringComparisonSymbol))
                    {
                        IndexOf_Char_StringComparison = method;
                    }
                }

                foreach (var method in stringSymbol.GetMembers(nameof(string.LastIndexOf)).OfType<IMethodSymbol>())
                {
                    if (method.IsStatic)
                        continue;

                    if (method.Parameters.Length == 1 && method.Parameters[0].Type.IsChar())
                    {
                        LastIndexOf_Char = method;
                    }
                    else if (method.Parameters.Length == 2 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsInt32())
                    {
                        LastIndexOf_Char_Int32 = method;
                    }
                    else if (method.Parameters.Length == 3 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsInt32() && method.Parameters[2].Type.IsInt32())
                    {
                        LastIndexOf_Char_Int32_Int32 = method;
                    }
                    else if (method.Parameters.Length == 2 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsEqualTo(StringComparisonSymbol))
                    {
                        LastIndexOf_Char_StringComparison = method;
                    }
                }

                foreach (var method in stringSymbol.GetMembers(nameof(string.Join)).OfType<IMethodSymbol>())
                {
                    if (!method.IsStatic)
                        continue;

                    if (method.Parameters.Length == 2 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Object })
                    {
                        Join_Char_ObjectArray = method;
                    }
                    else if (method.Parameters.Length == 2 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_String })
                    {
                        Join_Char_StringArray = method;
                    }
                    else if (method.Parameters.Length == 2 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type is INamedTypeSymbol symbol && symbol.ConstructedFrom.IsEqualTo(EnumerableOfTSymbol))
                    {
                        Join_Char_IEnumerableT = method;
                    }
                    else if (method.Parameters.Length == 4 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_String } && method.Parameters[2].Type.IsInt32() && method.Parameters[3].Type.IsInt32())
                    {
                        Join_Char_StringArray_Int32_Int32 = method;
                    }
                }
            }
        }

        public IMethodSymbol? StartsWith_Char { get; set; }
        public IMethodSymbol? EndsWith_Char { get; set; }

        public IMethodSymbol? Replace_Char_Char { get; set; }

        public IMethodSymbol? IndexOf_Char { get; set; }
        public IMethodSymbol? IndexOf_Char_Int32 { get; set; }
        public IMethodSymbol? IndexOf_Char_Int32_Int32 { get; set; }
        public IMethodSymbol? IndexOf_Char_StringComparison { get; set; }

        public IMethodSymbol? LastIndexOf_Char { get; set; }
        public IMethodSymbol? LastIndexOf_Char_Int32 { get; set; }
        public IMethodSymbol? LastIndexOf_Char_Int32_Int32 { get; set; }
        public IMethodSymbol? LastIndexOf_Char_StringComparison { get; set; }

        public IMethodSymbol? Join_Char_ObjectArray { get; set; }
        public IMethodSymbol? Join_Char_IEnumerableT { get; set; }
        public IMethodSymbol? Join_Char_StringArray { get; set; }
        public IMethodSymbol? Join_Char_StringArray_Int32_Int32 { get; set; }

        public INamedTypeSymbol? StringComparisonSymbol { get; set; }
        public ISymbol? StringComparison_Ordinal { get; set; }
        public ISymbol? StringComparison_CurrentCulture { get; set; }
        public ISymbol? StringComparison_InvariantCulture { get; set; }

        public INamedTypeSymbol? EnumerableOfTSymbol { get; set; }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation.TargetMethod.ContainingType.IsString())
            {
                if (operation.TargetMethod.Name is "StartsWith")
                {
                    if (StartsWith_Char is null)
                        return;

                    // string.StartsWith(string, StringComparison)
                    if (IsStringSearchWithStringComparison(operation.TargetMethod, int32ParameterCount: 0) &&
                        IsOrdinalArgument(operation, parameterOrdinal: 1) &&
                        GetSingleCharStringArgument(operation, parameterOrdinal: 0) is { } value)
                    {
                        context.ReportDiagnostic(Rule, value);
                    }
                }
                else if (operation.TargetMethod.Name is "EndsWith")
                {
                    if (EndsWith_Char is null)
                        return;

                    // string.EndsWith(string, StringComparison)
                    if (IsStringSearchWithStringComparison(operation.TargetMethod, int32ParameterCount: 0) &&
                        IsOrdinalArgument(operation, parameterOrdinal: 1) &&
                        GetSingleCharStringArgument(operation, parameterOrdinal: 0) is { } value)
                    {
                        context.ReportDiagnostic(Rule, value);
                    }
                }
                else if (operation.TargetMethod.Name is "Replace")
                {
                    if (Replace_Char_Char is null)
                        return;

                    if (GetSingleCharStringArgument(operation, parameterOrdinal: 0) is null || GetSingleCharStringArgument(operation, parameterOrdinal: 1) is null)
                        return;

                    // string.Replace(string, string) or string.Replace(string, string, StringComparison)
                    if (operation.TargetMethod.Parameters.Length == 2 ||
                        (operation.TargetMethod.Parameters.Length == 3 && IsOrdinalArgument(operation, parameterOrdinal: 2)))
                    {
                        // Improve the error message as the rule is reported on the method
                        context.ReportDiagnostic(Rule, ImmutableDictionary<string, string?>.Empty, operation, DiagnosticInvocationReportOptions.ReportOnMember);
                    }
                }
                else if (operation.TargetMethod.Name is "IndexOf")
                {
                    AnalyzeIndexOf(context, operation, IndexOf_Char, IndexOf_Char_Int32, IndexOf_Char_Int32_Int32, IndexOf_Char_StringComparison);
                }
                else if (operation.TargetMethod.Name is "LastIndexOf")
                {
                    AnalyzeIndexOf(context, operation, LastIndexOf_Char, LastIndexOf_Char_Int32, LastIndexOf_Char_Int32_Int32, LastIndexOf_Char_StringComparison);
                }
                else if (operation.TargetMethod.Name is "Join" && operation.TargetMethod.IsStatic)
                {
                    if (operation.Arguments.Length > 1)
                    {
                        if (operation.Arguments[0].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } })
                        {
                            var secondParameterType = operation.TargetMethod.Parameters[1].Type;
                            switch (operation.Arguments.Length)
                            {
                                case 2:
                                    if (Join_Char_ObjectArray is not null && secondParameterType is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Object })
                                    {
                                        context.ReportDiagnostic(Rule, operation.Arguments[0].Value);
                                        return;
                                    }

                                    if (Join_Char_StringArray is not null && secondParameterType is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_String })
                                    {
                                        context.ReportDiagnostic(Rule, operation.Arguments[0].Value);
                                        return;
                                    }

                                    if (Join_Char_IEnumerableT is not null && secondParameterType is INamedTypeSymbol symbol && symbol.ConstructedFrom.IsEqualTo(EnumerableOfTSymbol))
                                    {
                                        context.ReportDiagnostic(Rule, operation.Arguments[0].Value);
                                        return;
                                    }

                                    break;

                                case 4:
                                    if (Join_Char_StringArray_Int32_Int32 is not null)
                                    {
                                        context.ReportDiagnostic(Rule, operation.Arguments[0].Value);
                                        return;
                                    }

                                    break;
                            }
                        }
                    }
                }
            }
        }

        private void AnalyzeIndexOf(OperationAnalysisContext context, IInvocationOperation operation, IMethodSymbol? charOverload, IMethodSymbol? charInt32Overload, IMethodSymbol? charInt32Int32Overload, IMethodSymbol? charStringComparisonOverload)
        {
            if (GetSingleCharStringArgument(operation, parameterOrdinal: 0) is not { } value)
                return;

            // The overloads without a StringComparison parameter are culture-sensitive, so they cannot be replaced
            // with the char overloads, which are ordinal. e.g. IndexOf(string, int) or IndexOf(string, int, int)
            var method = operation.TargetMethod;
            var shouldReport = method.Parameters.Length switch
            {
                // (string, StringComparison)
                2 when IsStringSearchWithStringComparison(method, int32ParameterCount: 0) => charStringComparisonOverload is not null || (charOverload is not null && IsOrdinalArgument(operation, parameterOrdinal: 1)),

                // (string, int, StringComparison)
                3 when IsStringSearchWithStringComparison(method, int32ParameterCount: 1) => charInt32Overload is not null && IsOrdinalArgument(operation, parameterOrdinal: 2),

                // (string, int, int, StringComparison)
                4 when IsStringSearchWithStringComparison(method, int32ParameterCount: 2) => charInt32Int32Overload is not null && IsOrdinalArgument(operation, parameterOrdinal: 3),

                _ => false,
            };

            if (shouldReport)
            {
                context.ReportDiagnostic(Rule, value);
            }
        }

        // Match (string, int32 x int32ParameterCount, StringComparison)
        private bool IsStringSearchWithStringComparison(IMethodSymbol method, int int32ParameterCount)
        {
            var parameters = method.Parameters;
            if (parameters.Length != int32ParameterCount + 2)
                return false;

            if (!parameters[0].Type.IsString())
                return false;

            for (var i = 1; i <= int32ParameterCount; i++)
            {
                if (!parameters[i].Type.IsInt32())
                    return false;
            }

            return parameters[parameters.Length - 1].Type.IsEqualTo(StringComparisonSymbol);
        }

        private bool IsOrdinalArgument(IInvocationOperation operation, int parameterOrdinal)
        {
            return GetArgument(operation, parameterOrdinal) is { Parameter: { } parameter, Value.ConstantValue: { HasValue: true, Value: (int)StringComparison.Ordinal } }
                && parameter.Type.IsEqualTo(StringComparisonSymbol);
        }

        private static IOperation? GetSingleCharStringArgument(IInvocationOperation operation, int parameterOrdinal)
        {
            if (GetArgument(operation, parameterOrdinal) is { Parameter.Type.SpecialType: SpecialType.System_String, Value: { ConstantValue: { HasValue: true, Value: string { Length: 1 } } } value })
                return value;

            return null;
        }

        private static IArgumentOperation? GetArgument(IInvocationOperation operation, int parameterOrdinal)
        {
            foreach (var argument in operation.Arguments)
            {
                if (argument.Parameter?.Ordinal == parameterOrdinal)
                    return argument;
            }

            return null;
        }
    }
}
