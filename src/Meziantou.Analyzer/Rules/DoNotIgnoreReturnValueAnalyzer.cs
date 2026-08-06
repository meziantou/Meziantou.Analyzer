using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotIgnoreReturnValueAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor ReturnValueRule = new(
        RuleIdentifiers.DoNotIgnoreReturnValue,
        title: "The return value of the method should be used",
        messageFormat: "The return value of '{0}' should be used{1}",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotIgnoreReturnValue));

    private static readonly DiagnosticDescriptor OutParameterRule = new(
        RuleIdentifiers.DoNotIgnoreReturnValue,
        title: "The return value of the method should be used",
        messageFormat: "The out parameter '{0}' of '{1}' should not be discarded{2}",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotIgnoreReturnValue));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ReturnValueRule, OutParameterRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var analyzerContext = new AnalyzerContext(compilationContext.Compilation);
            compilationContext.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            compilationContext.RegisterOperationAction(analyzerContext.AnalyzeArgument, OperationKind.Argument);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private INamedTypeSymbol? DoNotIgnoreAttributeSymbol { get; } = compilation.GetBestTypeByMetadataName("Meziantou.Analyzer.Annotations.DoNotIgnoreAttribute");
        private INamedTypeSymbol? PureAttributeSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Diagnostics.Contracts.PureAttribute")
            ?? compilation.GetBestTypeByMetadataName("JetBrains.Annotations.PureAttribute");
        private INamedTypeSymbol? StreamSymbol { get; } = compilation.GetBestTypeByMetadataName("System.IO.Stream");
        private INamedTypeSymbol? TextReaderSymbol { get; } = compilation.GetBestTypeByMetadataName("System.IO.TextReader");
        private INamedTypeSymbol? BinaryReaderSymbol { get; } = compilation.GetBestTypeByMetadataName("System.IO.BinaryReader");

        public void AnalyzeArgument(OperationAnalysisContext context)
        {
            if (DoNotIgnoreAttributeSymbol is null)
                return;

            var argument = (IArgumentOperation)context.Operation;
            if (argument.Parameter is not { RefKind: RefKind.Out } outParam)
                return;

            if (argument.Value is not IDiscardOperation)
                return;

            if (!outParam.HasAttribute(DoNotIgnoreAttributeSymbol))
                return;

            var methodName = argument.Parent is IInvocationOperation inv ? inv.TargetMethod.Name : "?";
            var attr = outParam.GetAttribute(DoNotIgnoreAttributeSymbol);
            var message = attr is not null ? GetMessageFromAttributeData(attr) : null;
            context.ReportDiagnostic(OutParameterRule, argument,
                outParam.Name, methodName, message is null ? "" : ": " + message);
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var targetMethod = invocation.TargetMethod;

            if (targetMethod.ReturnsVoid)
                return;

            // Check return value
            if (!IsReturnValueIgnored(invocation))
                return;

            // Check attribute on return value
            if (DoNotIgnoreAttributeSymbol is not null)
            {
                var attr = targetMethod.GetReturnTypeAttribute(DoNotIgnoreAttributeSymbol);
                if (attr is not null)
                {
                    var message = GetMessageFromAttributeData(attr);
                    context.ReportDiagnostic(ReturnValueRule, invocation,
                        targetMethod.Name, message is null ? "" : ": " + message);
                    return;
                }
            }

            // Check [Pure] attribute on the method
            if (PureAttributeSymbol is not null && targetMethod.HasAttribute(PureAttributeSymbol))
            {
                context.ReportDiagnostic(ReturnValueRule, invocation, targetMethod.Name, "");
                return;
            }

            // Check built-in CLR list
            if (IsBuiltInMethod(targetMethod))
            {
                context.ReportDiagnostic(ReturnValueRule, invocation, targetMethod.Name, "");
            }
        }

        private static bool IsReturnValueIgnored(IInvocationOperation invocation)
        {
            var parent = invocation.Parent;
            if (parent is IAwaitOperation)
            {
                parent = parent.Parent;
            }

            return parent is null or IBlockOperation or IExpressionStatementOperation;
        }

        private bool IsBuiltInMethod(IMethodSymbol method)
        {
            var containingType = method.ContainingType;

            if (StreamSymbol is not null && containingType.IsOrInheritFrom(StreamSymbol))
            {
                return method.Name is
                    nameof(System.IO.Stream.Read) or
                    "ReadAsync" or
                    "ReadAtLeast" or
                    "ReadAtLeastAsync";
            }

            if (TextReaderSymbol is not null && containingType.IsOrInheritFrom(TextReaderSymbol))
            {
                return method.Name is
                    nameof(System.IO.TextReader.Read) or
                    "ReadAsync";
            }

            if (BinaryReaderSymbol is not null && containingType.IsOrInheritFrom(BinaryReaderSymbol))
            {
                return method.Name is nameof(System.IO.BinaryReader.Read);
            }

            return false;
        }

        private static string? GetMessageFromAttributeData(AttributeData attr)
        {
            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg.Key == "Message" && namedArg.Value.Value is string msg)
                    return msg;
            }

            return null;
        }
    }
}
