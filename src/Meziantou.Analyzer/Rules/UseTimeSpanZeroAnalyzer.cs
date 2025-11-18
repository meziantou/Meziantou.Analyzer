using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseTimeSpanZeroAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseTimeSpanZero,
        title: "Use TimeSpan.Zero instead of TimeSpan.FromXXX(0)",
        messageFormat: "Use TimeSpan.Zero instead of TimeSpan.{0}(0)",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseTimeSpanZero));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var timeSpanType = compilationContext.Compilation.GetBestTypeByMetadataName("System.TimeSpan");
            if (timeSpanType is null)
                return;

            compilationContext.RegisterOperationAction(context => AnalyzeInvocationOperation(context, timeSpanType), OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocationOperation(OperationAnalysisContext context, INamedTypeSymbol timeSpanType)
    {
        var operation = (IInvocationOperation)context.Operation;
        
        // Check if the method is a static method on System.TimeSpan
        if (!operation.TargetMethod.IsStatic)
            return;

        if (!operation.TargetMethod.ContainingType.IsEqualTo(timeSpanType))
            return;

        // Check if it's one of the From methods
        var methodName = operation.TargetMethod.Name;
        if (!IsTimeSpanFromMethod(methodName))
            return;

        // Check if the method has exactly one argument
        if (operation.Arguments.Length != 1)
            return;

        // Check if the argument is a constant value of 0
        var argument = operation.Arguments[0];
        if (!argument.Value.ConstantValue.HasValue)
            return;

        var constantValue = argument.Value.ConstantValue.Value;
        if (!IsZero(constantValue))
            return;

        context.ReportDiagnostic(Rule, operation, methodName);
    }

    private static bool IsTimeSpanFromMethod(string methodName)
    {
        return methodName is "FromDays" or "FromHours" or "FromMinutes" or "FromSeconds" or "FromMilliseconds" or "FromMicroseconds" or "FromTicks";
    }

    private static bool IsZero(object? value)
    {
        return value switch
        {
            int i => i == 0,
            long l => l == 0,
            double d => d == 0.0,
            float f => f == 0.0f,
            decimal dec => dec == 0m,
            byte b => b == 0,
            sbyte sb => sb == 0,
            short s => s == 0,
            ushort us => us == 0,
            uint ui => ui == 0,
            ulong ul => ul == 0,
            _ => false,
        };
    }
}
