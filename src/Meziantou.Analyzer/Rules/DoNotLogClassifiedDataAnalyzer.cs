using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotLogClassifiedDataAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotLogClassifiedData,
        title: "Do not log symbols decorated with DataClassificationAttribute directly",
        messageFormat: "Do not log symbols decorated with DataClassificationAttribute directly",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotLogClassifiedData));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            var ctx = new AnalyzerContext(context.Compilation);
            if (!ctx.IsValid)
                return;

            context.RegisterOperationAction(ctx.AnalyzeInvocationDeclaration, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            LoggerSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
            if (LoggerSymbol is null)
                return;

            LoggerExtensionsSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.LoggerExtensions");
            LoggerMessageSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.LoggerMessage");
            DataClassificationAttributeSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute");
        }

        public INamedTypeSymbol? LoggerSymbol { get; }
        public INamedTypeSymbol? LoggerExtensionsSymbol { get; }
        public INamedTypeSymbol? LoggerMessageSymbol { get; }

        public INamedTypeSymbol? DataClassificationAttributeSymbol { get; }

        public bool IsValid => DataClassificationAttributeSymbol is not null && LoggerSymbol is not null;

        public void AnalyzeInvocationDeclaration(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation.TargetMethod.ContainingType.IsEqualTo(LoggerExtensionsSymbol) && FindLogParameters(operation.TargetMethod, out var argumentsParameter))
            {
                var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(operation.Syntax.SyntaxTree);
                var reportTypesWithDataClassification = GetReportTypesWithDataClassificationConfiguration(options);

                foreach (var argument in operation.Arguments)
                {
                    var parameter = argument.Parameter;
                    if (parameter is null)
                        continue;

                    if (parameter.Equals(argumentsParameter, SymbolEqualityComparer.Default))
                    {
                        if (argument.ArgumentKind == ArgumentKind.ParamArray && argument.Value is IArrayCreationOperation arrayCreation && arrayCreation.Initializer is not null)
                        {
                            ValidateDataClassification(context, arrayCreation.Initializer.ElementValues, reportTypesWithDataClassification);
                        }
                    }
                }
            }
        }

        private static bool GetReportTypesWithDataClassificationConfiguration(AnalyzerConfigOptions options)
        {
            if (options.TryGetValue(RuleIdentifiers.DoNotLogClassifiedData + ".report_types_with_data_classification_attributes", out var value))
            {
                if (bool.TryParse(value, out var result))
                    return result;
            }

            return true; // Default to true (enabled by default)
        }

        private void ValidateDataClassification(DiagnosticReporter diagnosticReporter, IEnumerable<IOperation> operations, bool reportTypesWithDataClassification)
        {
            foreach (var operation in operations)
            {
                ValidateDataClassification(diagnosticReporter, operation, reportTypesWithDataClassification);
            }
        }

        private void ValidateDataClassification(DiagnosticReporter diagnosticReporter, IOperation operation, bool reportTypesWithDataClassification)
        {
            ValidateDataClassification(diagnosticReporter, operation, operation, DataClassificationAttributeSymbol!, reportTypesWithDataClassification);

            static void ValidateDataClassification(DiagnosticReporter diagnosticReporter, IOperation operation, IOperation reportOperation, INamedTypeSymbol dataClassificationAttributeSymbol, bool reportTypesWithDataClassification)
            {
                operation = operation.UnwrapConversionOperations();
                if (operation is IParameterReferenceOperation { Parameter: var parameter })
                {
                    if (parameter.HasAttribute(dataClassificationAttributeSymbol, inherits: true) || parameter.Type.HasAttribute(dataClassificationAttributeSymbol, inherits: true))
                    {
                        diagnosticReporter.ReportDiagnostic(Rule, reportOperation);
                    }
                    else if (reportTypesWithDataClassification && TypeContainsMembersWithDataClassification(parameter.Type, dataClassificationAttributeSymbol))
                    {
                        diagnosticReporter.ReportDiagnostic(Rule, reportOperation);
                    }
                }
                else if (operation is IPropertyReferenceOperation { Property: var property })
                {
                    if (property.HasAttribute(dataClassificationAttributeSymbol, inherits: true) || property.ContainingType.HasAttribute(dataClassificationAttributeSymbol, inherits: true))
                    {
                        diagnosticReporter.ReportDiagnostic(Rule, reportOperation);
                    }
                }
                else if (operation is IFieldReferenceOperation { Field: var field })
                {
                    if (field.HasAttribute(dataClassificationAttributeSymbol, inherits: true) || field.ContainingType.HasAttribute(dataClassificationAttributeSymbol, inherits: true))
                    {
                        diagnosticReporter.ReportDiagnostic(Rule, reportOperation);
                    }
                }
                else if (operation is IArrayElementReferenceOperation arrayElementReferenceOperation)
                {
                    ValidateDataClassification(diagnosticReporter, arrayElementReferenceOperation.ArrayReference, reportOperation, dataClassificationAttributeSymbol, reportTypesWithDataClassification);
                }
                else if (reportTypesWithDataClassification)
                {
                    // Check if the operation's type contains members with DataClassificationAttribute
                    var type = operation.Type;
                    if (type is not null)
                    {
                        // Early exit for value types (enums, structs) from well-known namespaces
                        if (type.IsValueType && type is INamedTypeSymbol namedType)
                        {
                            var ns = namedType.ContainingNamespace?.ToDisplayString();
                            if (ns is not null && (ns.StartsWith("System.", StringComparison.Ordinal) || ns == "System"))
                            {
                                return;
                            }
                        }

                        if (TypeContainsMembersWithDataClassification(type, dataClassificationAttributeSymbol))
                        {
                            diagnosticReporter.ReportDiagnostic(Rule, reportOperation);
                        }
                    }
                }
            }
        }

        private static bool TypeContainsMembersWithDataClassification(ITypeSymbol type, INamedTypeSymbol dataClassificationAttributeSymbol)
        {
            if (type is null)
                return false;

            // Don't check primitive types, strings, or common system types
            if (type.SpecialType != SpecialType.None)
                return false;

            // Check all members (properties and fields)
            foreach (var member in type.GetMembers())
            {
                if (member is IPropertySymbol property)
                {
                    if (property.HasAttribute(dataClassificationAttributeSymbol, inherits: true))
                        return true;
                }
                else if (member is IFieldSymbol field)
                {
                    if (field.HasAttribute(dataClassificationAttributeSymbol, inherits: true))
                        return true;
                }
            }

            return false;
        }

        private static bool FindLogParameters(IMethodSymbol methodSymbol, out IParameterSymbol? arguments)
        {
            IParameterSymbol? message = null;
            arguments = null;
            foreach (var parameter in methodSymbol.Parameters)
            {
                if (parameter.Type.IsString() &&
                    (string.Equals(parameter.Name, "message", StringComparison.Ordinal) ||
                    string.Equals(parameter.Name, "messageFormat", StringComparison.Ordinal) ||
                    string.Equals(parameter.Name, "formatString", StringComparison.Ordinal)))
                {
                    message = parameter;
                }
                // When calling logger.BeginScope("{Param}") generic overload would be selected
                else if (parameter.Type.SpecialType == SpecialType.System_String &&
                    methodSymbol.Name is "BeginScope" &&
                    parameter.Name is "state")
                {
                    message = parameter;
                }
                else if (parameter.IsParams &&
                    parameter.Name is "args")
                {
                    arguments = parameter;
                }
            }

            return message is not null;
        }
    }
}
