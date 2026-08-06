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
            var doNotIgnoreAttributeSymbol = compilationContext.Compilation.GetBestTypeByMetadataName("Meziantou.Analyzer.Annotations.DoNotIgnoreAttribute");

            var streamSymbol = compilationContext.Compilation.GetBestTypeByMetadataName("System.IO.Stream");
            var textReaderSymbol = compilationContext.Compilation.GetBestTypeByMetadataName("System.IO.TextReader");
            var binaryReaderSymbol = compilationContext.Compilation.GetBestTypeByMetadataName("System.IO.BinaryReader");

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, doNotIgnoreAttributeSymbol, streamSymbol, textReaderSymbol, binaryReaderSymbol),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol? doNotIgnoreAttributeSymbol,
        INamedTypeSymbol? streamSymbol,
        INamedTypeSymbol? textReaderSymbol,
        INamedTypeSymbol? binaryReaderSymbol)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        // Check out parameters with [DoNotIgnore]
        if (doNotIgnoreAttributeSymbol is not null)
        {
            foreach (var argument in invocation.Arguments)
            {
                if (argument.Parameter is { RefKind: RefKind.Out } outParam
                    && argument.Value is IDiscardOperation
                    && outParam.HasAttribute(doNotIgnoreAttributeSymbol))
                {
                    var message = GetMessage(outParam.GetAttributes(), doNotIgnoreAttributeSymbol);
                    context.ReportDiagnostic(OutParameterRule, invocation,
                        outParam.Name, targetMethod.Name, message is null ? "" : ": " + message);
                }
            }
        }

        // Check return value
        if (!IsReturnValueIgnored(invocation))
            return;

        // Check attribute on return value
        if (doNotIgnoreAttributeSymbol is not null)
        {
            var returnAttrs = targetMethod.GetReturnTypeAttributes();
            foreach (var attr in returnAttrs)
            {
                if (attr.AttributeClass is not null && attr.AttributeClass.IsOrInheritFrom(doNotIgnoreAttributeSymbol))
                {
                    var message = GetMessageFromAttributeData(attr);
                    context.ReportDiagnostic(ReturnValueRule, invocation,
                        targetMethod.Name, message is null ? "" : ": " + message);
                    return;
                }
            }
        }

        // Check built-in CLR list
        if (IsBuiltInMethod(targetMethod, streamSymbol, textReaderSymbol, binaryReaderSymbol))
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

    private static bool IsBuiltInMethod(
        IMethodSymbol method,
        INamedTypeSymbol? streamSymbol,
        INamedTypeSymbol? textReaderSymbol,
        INamedTypeSymbol? binaryReaderSymbol)
    {
        var containingType = method.ContainingType;

        if (streamSymbol is not null && containingType.IsOrInheritFrom(streamSymbol))
        {
            return method.Name is
                nameof(System.IO.Stream.Read) or
                "ReadAsync" or
                nameof(System.IO.Stream.ReadByte) or
                "ReadAtLeast" or
                "ReadAtLeastAsync";
        }

        if (textReaderSymbol is not null && containingType.IsOrInheritFrom(textReaderSymbol))
        {
            return method.Name is
                nameof(System.IO.TextReader.Read) or
                "ReadAsync" or
                "ReadLineAsync";
        }

        if (binaryReaderSymbol is not null && containingType.IsOrInheritFrom(binaryReaderSymbol))
        {
            return method.Name is
                nameof(System.IO.BinaryReader.Read) or
                nameof(System.IO.BinaryReader.ReadBoolean) or
                nameof(System.IO.BinaryReader.ReadByte) or
                nameof(System.IO.BinaryReader.ReadBytes) or
                nameof(System.IO.BinaryReader.ReadChar) or
                nameof(System.IO.BinaryReader.ReadChars) or
                nameof(System.IO.BinaryReader.ReadDecimal) or
                nameof(System.IO.BinaryReader.ReadDouble) or
                "ReadHalf" or
                nameof(System.IO.BinaryReader.ReadInt16) or
                nameof(System.IO.BinaryReader.ReadInt32) or
                nameof(System.IO.BinaryReader.ReadInt64) or
                nameof(System.IO.BinaryReader.ReadSByte) or
                nameof(System.IO.BinaryReader.ReadSingle) or
                nameof(System.IO.BinaryReader.ReadString) or
                nameof(System.IO.BinaryReader.ReadUInt16) or
                nameof(System.IO.BinaryReader.ReadUInt32) or
                nameof(System.IO.BinaryReader.ReadUInt64);
        }

        return false;
    }

    private static string? GetMessage(ImmutableArray<AttributeData> attributes, INamedTypeSymbol doNotIgnoreAttributeSymbol)
    {
        foreach (var attr in attributes)
        {
            if (attr.AttributeClass is not null && attr.AttributeClass.IsOrInheritFrom(doNotIgnoreAttributeSymbol))
                return GetMessageFromAttributeData(attr);
        }

        return null;
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
