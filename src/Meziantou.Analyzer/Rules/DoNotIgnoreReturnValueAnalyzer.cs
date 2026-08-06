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
        private INamedTypeSymbol? StringSymbol { get; } = compilation.GetSpecialType(SpecialType.System_String);
        private INamedTypeSymbol? IImmutableDictionarySymbol { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Immutable.IImmutableDictionary`2");
        private INamedTypeSymbol? IImmutableListSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Immutable.IImmutableList`1");
        private INamedTypeSymbol? IImmutableQueueSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Immutable.IImmutableQueue`1");
        private INamedTypeSymbol? IImmutableSetSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Immutable.IImmutableSet`1");
        private INamedTypeSymbol? IImmutableStackSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Immutable.IImmutableStack`1");
        private INamedTypeSymbol? ImmutableArraySymbol { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Immutable.ImmutableArray");
        private INamedTypeSymbol? ImmutableArrayBuilderSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1+Builder")
            ?? compilation.GetBestTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1.Builder");

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

            if (StringSymbol is not null && containingType.IsEqualTo(StringSymbol))
            {
                return method.Name is
                    nameof(string.ToUpper) or
                    nameof(string.ToLower) or
                    nameof(string.Trim) or
                    nameof(string.TrimEnd) or
                    nameof(string.TrimStart) or
                    nameof(string.ToUpperInvariant) or
                    nameof(string.ToLowerInvariant) or
                    nameof(string.Clone) or
                    nameof(string.Format) or
                    nameof(string.Concat) or
                    nameof(string.Copy) or
                    nameof(string.Insert) or
                    nameof(string.Join) or
                    nameof(string.Normalize) or
                    nameof(string.Remove) or
                    nameof(string.Replace) or
                    nameof(string.Split) or
                    nameof(string.PadLeft) or
                    nameof(string.PadRight) or
                    nameof(string.Substring);
            }

            if (IImmutableDictionarySymbol is not null && (containingType.ImplementsGenericInterface(IImmutableDictionarySymbol) || containingType.OriginalDefinition.IsEqualTo(IImmutableDictionarySymbol)))
            {
                return method.Name is "Clear" or "Add" or "AddRange" or "SetItem" or "SetItems" or "RemoveRange" or "Remove" or "Contains" or "TryGetKey";
            }

            if (IImmutableListSymbol is not null && (containingType.ImplementsGenericInterface(IImmutableListSymbol) || containingType.OriginalDefinition.IsEqualTo(IImmutableListSymbol)))
            {
                return method.Name is "Clear" or "IndexOf" or "LastIndexOf" or "Add" or "AddRange" or "Insert" or "InsertRange" or "Remove" or "RemoveAll" or "RemoveRange" or "RemoveAt" or "SetItem" or "Replace";
            }

            if (IImmutableQueueSymbol is not null && (containingType.ImplementsGenericInterface(IImmutableQueueSymbol) || containingType.OriginalDefinition.IsEqualTo(IImmutableQueueSymbol)))
            {
                return method.Name is "Clear" or "Peek" or "Enqueue" or "Dequeue";
            }

            if (IImmutableSetSymbol is not null && (containingType.ImplementsGenericInterface(IImmutableSetSymbol) || containingType.OriginalDefinition.IsEqualTo(IImmutableSetSymbol)))
            {
                return method.Name is "Clear" or "Contains" or "Add" or "Remove" or "TryGetValue" or "Intersect" or "Except" or "SymmetricExcept" or "Union" or "SetEquals" or "IsProperSubsetOf" or "IsProperSupersetOf" or "IsSubsetOf" or "IsSupersetOf" or "Overlaps";
            }

            if (IImmutableStackSymbol is not null && (containingType.ImplementsGenericInterface(IImmutableStackSymbol) || containingType.OriginalDefinition.IsEqualTo(IImmutableStackSymbol)))
            {
                return method.Name is "Clear" or "Push" or "Pop" or "Peek";
            }

            if (ImmutableArraySymbol is not null && containingType.IsEqualTo(ImmutableArraySymbol))
            {
                return method.Name is "Create" or "CreateRange" or "CreateBuilder" or "ToImmutableArray" or "BinarySearch";
            }

            if (ImmutableArrayBuilderSymbol is not null && containingType.OriginalDefinition.IsEqualTo(ImmutableArrayBuilderSymbol))
            {
                return method.Name is "IndexOf" or "LastIndexOf";
            }

            return IsTryParseMethod(method);
        }

        private static bool IsTryParseMethod(IMethodSymbol method)
        {
            return method.Name.StartsWith("TryParse", StringComparison.Ordinal) &&
                method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                method.Parameters.Length >= 2 &&
                method.Parameters[method.Parameters.Length - 1].RefKind != RefKind.None;
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
