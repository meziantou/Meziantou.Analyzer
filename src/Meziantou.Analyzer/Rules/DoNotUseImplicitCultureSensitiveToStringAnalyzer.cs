using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotUseImplicitCultureSensitiveToStringAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_stringConcatRule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString,
            title: "Do not use implicit culture-sensitive ToString",
            messageFormat: "Do not use implicit culture-sensitive ToString",
            RuleCategories.Design,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString));

        private static readonly DiagnosticDescriptor s_stringInterpolationRule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation,
            title: "Do not use implicit culture-sensitive ToString",
            messageFormat: "Do not use implicit culture-sensitive ToString",
            RuleCategories.Design,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_stringConcatRule, s_stringInterpolationRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(AnalyzeBinaryOperation, OperationKind.Binary);
            context.RegisterOperationAction(AnalyzeInterpolatedString, OperationKind.InterpolatedString);
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

            var parent = operation.Parent;
            if (parent is IConversionOperation conversionOperation)
            {
                // `FormattableString _ = $""` is valid whereas `string _ = $""` may not be
                if (conversionOperation.Type.IsEqualTo(context.Compilation.GetTypeByMetadataName("System.FormattableString")))
                    return;
            }

            foreach (var part in operation.Parts.OfType<IInterpolationOperation>())
            {
                var expression = part.Expression;
                var type = expression.Type;
                if (IsFormattableType(context.Compilation, type))
                {
                    context.ReportDiagnostic(s_stringInterpolationRule, part);
                }
            }
        }

        private static bool IsValidOperand(IOperation operand)
        {
            // Implicit conversion from a type number
            if (operand is null)
                return true;

            if (operand is IConversionOperation conversion && conversion.IsImplicit && conversion.Type.IsObject())
            {
                if (IsFormattableType(operand.SemanticModel.Compilation, conversion.Operand.Type) && !IsConstantPositiveNumber(conversion.Operand))
                    return false;
            }

            return true;
        }

        private static bool IsFormattableType(Compilation compilation, ITypeSymbol typeSymbol)
        {
            var iformattableSymbol = compilation.GetTypeByMetadataName("System.IFormattable");
            if (typeSymbol.Implements(iformattableSymbol))
            {
                if (typeSymbol.IsEnumeration())
                    return false;

                if (typeSymbol.SpecialType == SpecialType.System_Byte)
                    return false;

                if (typeSymbol.SpecialType == SpecialType.System_UInt16)
                    return false;

                if (typeSymbol.SpecialType == SpecialType.System_UInt32)
                    return false;

                if (typeSymbol.SpecialType == SpecialType.System_UInt64)
                    return false;

                if (typeSymbol.IsEqualTo(compilation.GetTypeByMetadataName("System.TimeSpan")))
                    return false;

                if (typeSymbol.IsEqualTo(compilation.GetTypeByMetadataName("System.Guid")))
                    return false;

                if (typeSymbol.IsEqualTo(compilation.GetTypeByMetadataName("System.Windows.FontStretch")))
                    return false;

                if (typeSymbol.IsOrInheritFrom(compilation.GetTypeByMetadataName("System.Windows.Media.Brush")))
                    return false;

                return true;
            }

            return false;
        }

        // Only negative numbers are culture-sensitive (negative sign)
        // For instance, https://source.dot.net/#System.Private.CoreLib/Int32.cs,8d6f2d8bc0589463
        private static bool IsConstantPositiveNumber(IOperation operation)
        {
            if (!operation.ConstantValue.HasValue)
                return false;

            var constantValue = operation.ConstantValue.Value;
            return operation.Type.SpecialType switch
            {
                SpecialType.System_Byte => (byte)constantValue >= 0,
                SpecialType.System_SByte => (sbyte)constantValue >= 0,
                SpecialType.System_Int16 => (short)constantValue >= 0,
                SpecialType.System_Int32 => (int)constantValue >= 0,
                SpecialType.System_Int64 => (long)constantValue >= 0,
                SpecialType.System_Single => (float)constantValue >= 0,
                SpecialType.System_Double => (double)constantValue >= 0,
                SpecialType.System_Decimal => (decimal)constantValue >= 0,
                SpecialType.System_UInt16 => true,
                SpecialType.System_UInt32 => true,
                SpecialType.System_UInt64 => true,
                _ => false,
            };
        }
    }
}
