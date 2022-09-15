using System.Collections.Immutable;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseImplicitCultureSensitiveToStringAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_stringConcatRule = new(
        RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString,
        title: "Do not use implicit culture-sensitive ToString",
        messageFormat: "Do not use implicit culture-sensitive ToString",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString));

    private static readonly DiagnosticDescriptor s_stringInterpolationRule = new(
        RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation,
        title: "Do not use implicit culture-sensitive ToString in interpolated strings",
        messageFormat: "Do not use implicit culture-sensitive ToString in interpolated strings",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation));

    private static readonly DiagnosticDescriptor s_objectToStringRule = new(
        RuleIdentifiers.DoNotUseCultureSensitiveObjectToString,
        title: "Do not use culture-sensitive object.ToString",
        messageFormat: "Do not use culture-sensitive object.ToString",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseCultureSensitiveObjectToString));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_stringConcatRule, s_stringInterpolationRule, s_objectToStringRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeBinaryOperation, OperationKind.Binary);
        context.RegisterOperationAction(AnalyzeInterpolatedString, OperationKind.InterpolatedString);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var operation = (IInvocationOperation)context.Operation;
        if (IsExcludedMethod(context, s_objectToStringRule, operation))
            return;

        if (operation.TargetMethod.Name == "ToString" && operation.TargetMethod.ContainingType.IsObject() && operation.TargetMethod.Parameters.Length == 0)
        {
            if (operation.Instance != null && operation.Instance.Type.IsObject())
            {
                context.ReportDiagnostic(s_objectToStringRule, operation);
            }
        }
    }

    private static void AnalyzeBinaryOperation(OperationAnalysisContext context)
    {
        var operation = (IBinaryOperation)context.Operation;
        if (operation.OperatorKind != BinaryOperatorKind.Add)
            return;

        if (!operation.Type.IsString())
            return;

        if (operation.ConstantValue.HasValue)
            return;

        if (IsExcludedMethod(context, s_stringConcatRule, operation))
            return;

        if (!IsValidOperand(operation.LeftOperand))
        {
            context.ReportDiagnostic(s_stringConcatRule, operation.LeftOperand);
        }

        if (!IsValidOperand(operation.RightOperand))
        {
            context.ReportDiagnostic(s_stringConcatRule, operation.RightOperand);
        }
    }

    private static void AnalyzeInterpolatedString(OperationAnalysisContext context)
    {
        // Check if parent is InterpolatedString.Invariant($"") or conversion to string?
        var operation = (IInterpolatedStringOperation)context.Operation;

        if (operation.ConstantValue.HasValue)
            return;

        if (IsExcludedMethod(context, s_stringInterpolationRule, operation))
            return;

        var parent = operation.Parent;
        if (parent is IConversionOperation conversionOperation)
        {
            // `FormattableString _ = $""` is valid whereas `string _ = $""` may not be
            if (conversionOperation.Type.IsEqualTo(context.Compilation.GetBestTypeByMetadataName("System.FormattableString")))
                return;
        }

        foreach (var part in operation.Parts.OfType<IInterpolationOperation>())
        {
            var expression = part.Expression;
            var type = expression.Type;
            if (expression == null || type == null)
                continue;

            if (IsFormattableType(context.Compilation, type) && !IsConstantPositiveNumber(expression))
            {
                context.ReportDiagnostic(s_stringInterpolationRule, part);
            }
        }
    }

    private static bool IsExcludedMethod(OperationAnalysisContext context, DiagnosticDescriptor descriptor, IOperation operation)
    {
        // ToString show culture-sensitive data by default
        if (operation?.GetContainingMethod()?.Name == "ToString")
        {
            return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, descriptor.Id + ".exclude_tostring_methods", defaultValue: true);
        }

        return false;
    }

    private static bool IsValidOperand(IOperation operand)
    {
        // Implicit conversion from a type number
        if (operand is null)
            return true;

        if (operand is IConversionOperation conversion && conversion.IsImplicit && conversion.Type.IsObject() && conversion.Operand.Type != null)
        {
            if (IsFormattableType(operand.SemanticModel!.Compilation, conversion.Operand.Type) && !IsConstantPositiveNumber(conversion.Operand))
                return false;
        }

        return true;
    }

    private static bool IsFormattableType(Compilation compilation, ITypeSymbol typeSymbol)
    {
        var iformattableSymbol = compilation.GetBestTypeByMetadataName("System.IFormattable");
        if (typeSymbol.Implements(iformattableSymbol))
        {
            if (typeSymbol.IsEnumeration())
                return false;

            if (typeSymbol.SpecialType == SpecialType.System_Byte)
                return false;

            if (typeSymbol.SpecialType == SpecialType.System_Char)
                return false;

            if (typeSymbol.SpecialType == SpecialType.System_UInt16)
                return false;

            if (typeSymbol.SpecialType == SpecialType.System_UInt32)
                return false;

            if (typeSymbol.SpecialType == SpecialType.System_UInt64)
                return false;

            if (typeSymbol.IsEqualTo(compilation.GetBestTypeByMetadataName("System.UInt128")))
                return false;
            
            if (typeSymbol.IsEqualTo(compilation.GetBestTypeByMetadataName("System.UIntPtr")))
                return false;
            
            if (typeSymbol.IsEqualTo(compilation.GetBestTypeByMetadataName("System.TimeSpan")))
                return false;

            if (typeSymbol.IsEqualTo(compilation.GetBestTypeByMetadataName("System.Guid")))
                return false;

            if (typeSymbol.IsOrInheritFrom(compilation.GetBestTypeByMetadataName("System.Version")))
                return false;

            if (typeSymbol.IsEqualTo(compilation.GetBestTypeByMetadataName("System.Windows.FontStretch")))
                return false;

            if (typeSymbol.IsOrInheritFrom(compilation.GetBestTypeByMetadataName("System.Windows.Media.Brush")))
                return false;

            return true;
        }

        return false;
    }

    // Only negative numbers are culture-sensitive (negative sign)
    // For instance, https://source.dot.net/#System.Private.CoreLib/Int32.cs,8d6f2d8bc0589463
    private static bool IsConstantPositiveNumber(IOperation operation)
    {
        if (operation.Type != null && operation.ConstantValue.HasValue)
        {
            var constantValue = operation.ConstantValue.Value;
            bool? result = operation.Type.SpecialType switch
            {
                SpecialType.System_Byte => true,
                SpecialType.System_SByte => (sbyte)constantValue! >= 0,
                SpecialType.System_Int16 => (short)constantValue! >= 0,
                SpecialType.System_Int32 => (int)constantValue! >= 0,
                SpecialType.System_Int64 => (long)constantValue! >= 0,
                SpecialType.System_UInt16 => true,
                SpecialType.System_UInt32 => true,
                SpecialType.System_UInt64 => true,
                _ => null,
            };
            if (result.HasValue)
                return result.Value;
        }

        if (operation is IMemberReferenceOperation memberReferenceOperation)
        {
            if (memberReferenceOperation.Member.Name == "Count")
                return true;

            if (memberReferenceOperation.Member.Name == "Length")
                return true;

            if (memberReferenceOperation.Member.Name == "LongthLength")
                return true;
        }
        else if (operation is IInvocationOperation invocationOperation)
        {
            if (invocationOperation.TargetMethod.Name == "Count")
                return true;

            if (invocationOperation.TargetMethod.Name == "LongCount")
                return true;
        }

        return false;
    }
}
