using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotRaiseReservedExceptionTypeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotRaiseReservedExceptionType,
            title: "Do not raise reserved exception type",
            messageFormat: "'{0}' is a reserved exception type",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotRaiseReservedExceptionType));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var compilation = ctx.Compilation;
                var reservedExceptionTypes = new List<INamedTypeSymbol>();
                reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.AccessViolationException"));
                reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.BadImageFormatException"));
                reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.CannotUnloadAppDomainException"));
                reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.DataMisalignedException"));
                reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.ExecutionEngineException"));
                reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.IndexOutOfRangeException"));
                reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.InvalidProgramException"));
                reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.NullReferenceException"));
                reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.OutOfMemoryException"));
                reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.StackOverflowException"));

                if (reservedExceptionTypes.Any())
                {
                    ctx.RegisterOperationAction(_ => Analyze(_, reservedExceptionTypes), OperationKind.Throw);
                }
            });
        }

        private static void Analyze(OperationAnalysisContext context, IEnumerable<INamedTypeSymbol> reservedExceptionTypes)
        {
            var operation = (IThrowOperation)context.Operation;
            if (operation == null || operation.Exception == null)
                return;

            var exceptionType = operation.Exception.GetActualType();
            if (reservedExceptionTypes.Any(type => exceptionType.IsEqualTo(type) || exceptionType.InheritsFrom(type)))
            {
                context.ReportDiagnostic(s_rule, operation, exceptionType.ToDisplayString());
            }
        }
    }
}
