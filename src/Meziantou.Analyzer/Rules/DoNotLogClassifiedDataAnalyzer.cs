using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            StructuredLogFieldAttributeSymbol = compilation.GetBestTypeByMetadataName("Meziantou.Analyzer.Annotations.StructuredLogFieldAttribute");

            DataClassificationAttributeSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute");
        }

        public INamedTypeSymbol? StructuredLogFieldAttributeSymbol { get; private set; }

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
                foreach (var argument in operation.Arguments)
                {
                    var parameter = argument.Parameter;
                    if (parameter is null)
                        continue;

                    if (parameter.Equals(argumentsParameter, SymbolEqualityComparer.Default))
                    {
                        if (argument.ArgumentKind == ArgumentKind.ParamArray && argument.Value is IArrayCreationOperation arrayCreation && arrayCreation.Initializer is not null)
                        {
                            ValidateDataClassification(context, arrayCreation.Initializer.ElementValues);
                        }
                    }
                }
            }
        }

        private void ValidateDataClassification(DiagnosticReporter diagnosticReporter, IEnumerable<IOperation> operations)
        {
            foreach (var operation in operations)
            {
                ValidateDataClassification(diagnosticReporter, operation);
            }
        }

        private void ValidateDataClassification(DiagnosticReporter diagnosticReporter, IOperation operation)
        {
            ValidateDataClassification(diagnosticReporter, operation, operation, DataClassificationAttributeSymbol!);

            static void ValidateDataClassification(DiagnosticReporter diagnosticReporter, IOperation operation, IOperation reportOperation, INamedTypeSymbol dataClassificationAttributeSymbol)
            {
                operation = operation.UnwrapConversionOperations();
                if (operation is IParameterReferenceOperation { Parameter: var parameter })
                {
                    if (parameter.HasAttribute(dataClassificationAttributeSymbol, inherits: true) || parameter.Type.HasAttribute(dataClassificationAttributeSymbol, inherits: true))
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
                    ValidateDataClassification(diagnosticReporter, arrayElementReferenceOperation.ArrayReference, reportOperation, dataClassificationAttributeSymbol);
                }
            }
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
