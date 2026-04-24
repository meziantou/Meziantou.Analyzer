using System.Collections.Immutable;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseAnOverloadThatHasMidpointRoundingAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseAnOverloadThatHasMidpointRounding,
        title: "Use an overload with a MidpointRounding argument",
        messageFormat: "Use an overload with a MidpointRounding argument",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAnOverloadThatHasMidpointRounding));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var midpointRoundingSymbol = context.Compilation.GetBestTypeByMetadataName("System.MidpointRounding");
            if (midpointRoundingSymbol is null)
                return;

            var ifloatingPointSymbol = context.Compilation.GetBestTypeByMetadataName("System.Numerics.IFloatingPoint`1");
            var mathSymbol = context.Compilation.GetBestTypeByMetadataName("System.Math");
            var mathFSymbol = context.Compilation.GetBestTypeByMetadataName("System.MathF");
            if (ifloatingPointSymbol is null && mathSymbol is null && mathFSymbol is null)
                return;

            context.RegisterOperationAction(context => AnalyzeInvocation(context, midpointRoundingSymbol, ifloatingPointSymbol, mathSymbol, mathFSymbol), OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol midpointRoundingSymbol,
        INamedTypeSymbol? ifloatingPointSymbol,
        INamedTypeSymbol? mathSymbol,
        INamedTypeSymbol? mathFSymbol)
    {
        var operation = (IInvocationOperation)context.Operation;
        var method = operation.TargetMethod;
        if (!IsRoundMethodWithoutMidpointRounding(method, midpointRoundingSymbol))
            return;

        if (method.ContainingType.IsEqualTo(mathSymbol) || method.ContainingType.IsEqualTo(mathFSymbol))
        {
            context.ReportDiagnostic(Rule, operation);
            return;
        }

        if (method.ContainingType.SpecialType is SpecialType.System_Decimal)
        {
            context.ReportDiagnostic(Rule, operation);
            return;
        }

        if (IsIFloatingPointRoundMethod(method, midpointRoundingSymbol, ifloatingPointSymbol) ||
            IsIFloatingPointRoundImplementation(method, midpointRoundingSymbol, ifloatingPointSymbol))
        {
            context.ReportDiagnostic(Rule, operation);
        }
    }

    private static bool IsRoundMethodWithoutMidpointRounding(IMethodSymbol method, INamedTypeSymbol midpointRoundingSymbol)
    {
        return method.Name is "Round" &&
               !method.Parameters.Any(parameter => parameter.Type.IsEqualTo(midpointRoundingSymbol));
    }

    private static bool IsIFloatingPointRoundMethod(IMethodSymbol method, INamedTypeSymbol midpointRoundingSymbol, INamedTypeSymbol? ifloatingPointSymbol)
    {
        if (ifloatingPointSymbol is null)
            return false;

        return IsRoundMethodWithoutMidpointRounding(method, midpointRoundingSymbol) &&
               method.ContainingType.OriginalDefinition.IsEqualTo(ifloatingPointSymbol);
    }

    private static bool IsIFloatingPointRoundImplementation(IMethodSymbol method, INamedTypeSymbol midpointRoundingSymbol, INamedTypeSymbol? ifloatingPointSymbol)
    {
        if (ifloatingPointSymbol is null || method.ContainingType is null)
            return false;

        foreach (var explicitImplementation in method.ExplicitInterfaceImplementations)
        {
            if (IsIFloatingPointRoundMethod(explicitImplementation, midpointRoundingSymbol, ifloatingPointSymbol))
                return true;
        }

        foreach (var interfaceType in method.ContainingType.AllInterfaces)
        {
            if (!interfaceType.OriginalDefinition.IsEqualTo(ifloatingPointSymbol))
                continue;

            foreach (var interfaceMethod in interfaceType.GetMembers(method.Name).OfType<IMethodSymbol>())
            {
                if (!IsIFloatingPointRoundMethod(interfaceMethod, midpointRoundingSymbol, ifloatingPointSymbol))
                    continue;

                var implementation = method.ContainingType.FindImplementationForInterfaceMember(interfaceMethod);
                if (implementation is IMethodSymbol implementationMethod && implementationMethod.OriginalDefinition.IsEqualTo(method.OriginalDefinition))
                    return true;
            }
        }

        return false;
    }
}
