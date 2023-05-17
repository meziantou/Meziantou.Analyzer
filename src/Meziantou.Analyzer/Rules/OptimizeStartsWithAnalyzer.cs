using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptimizeStartsWithAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.OptimizeStartsWith,
        title: "Optimize string method usage",
        messageFormat: "Use an overload with char instead of string",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeStartsWith));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            StringComparisonSymbol = compilation.GetBestTypeByMetadataName("System.StringComparison");
            if (StringComparisonSymbol != null)
            {
                StringComparison_Ordinal = StringComparisonSymbol.GetMembers(nameof(StringComparison.Ordinal)).FirstOrDefault();
                StringComparison_CurrentCulture = StringComparisonSymbol.GetMembers(nameof(StringComparison.CurrentCulture)).FirstOrDefault();
                StringComparison_InvariantCulture = StringComparisonSymbol.GetMembers(nameof(StringComparison.InvariantCulture)).FirstOrDefault();
            }

            // StartsWith methods
            var stringSymbol = compilation.GetSpecialType(SpecialType.System_String);
            if (stringSymbol != null)
            {
                foreach (var method in stringSymbol.GetMembers(nameof(string.StartsWith)).OfType<IMethodSymbol>())
                {
                    if (!method.IsStatic && method.Parameters.Length == 1 && method.Parameters[0].Type.IsChar())
                    {
                        StartsWith_Char = method;
                        break;
                    }
                }

                foreach (var method in stringSymbol.GetMembers(nameof(string.EndsWith)).OfType<IMethodSymbol>())
                {
                    if (!method.IsStatic && method.Parameters.Length == 1 && method.Parameters[0].Type.IsChar())
                    {
                        EndsWith_Char = method;
                        break;
                    }
                }

                foreach (var method in stringSymbol.GetMembers(nameof(string.Replace)).OfType<IMethodSymbol>())
                {
                    if (!method.IsStatic && method.Parameters.Length == 2 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsChar())
                    {
                        Replace_Char_Char = method;
                        break;
                    }
                }

                foreach (var method in stringSymbol.GetMembers(nameof(string.IndexOf)).OfType<IMethodSymbol>())
                {
                    if (method.IsStatic)
                        continue;

                    if (method.Parameters.Length == 1 && method.Parameters[0].Type.IsChar())
                    {
                        IndexOf_Char = method;
                    }
                    else if (method.Parameters.Length == 2 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsInt32())
                    {
                        IndexOf_Char_Int32 = method;
                    }
                    else if (method.Parameters.Length == 3 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsInt32() && method.Parameters[2].Type.IsInt32())
                    {
                        IndexOf_Char_Int32_Int32 = method;
                    }
                    else if (method.Parameters.Length == 2 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsEqualTo(StringComparisonSymbol))
                    {
                        IndexOf_Char_StringComparison = method;
                    }
                }

                foreach (var method in stringSymbol.GetMembers(nameof(string.LastIndexOf)).OfType<IMethodSymbol>())
                {
                    if (method.IsStatic)
                        continue;

                    if (method.Parameters.Length == 1 && method.Parameters[0].Type.IsChar())
                    {
                        LastIndexOf_Char = method;
                    }
                    else if (method.Parameters.Length == 2 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsInt32())
                    {
                        LastIndexOf_Char_Int32 = method;
                    }
                    else if (method.Parameters.Length == 3 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsInt32() && method.Parameters[2].Type.IsInt32())
                    {
                        LastIndexOf_Char_Int32_Int32 = method;
                    }
                    else if (method.Parameters.Length == 2 && method.Parameters[0].Type.IsChar() && method.Parameters[1].Type.IsEqualTo(StringComparisonSymbol))
                    {
                        LastIndexOf_Char_StringComparison = method;
                    }
                }
            }
        }

        public IMethodSymbol? StartsWith_Char { get; set; }
        public IMethodSymbol? EndsWith_Char { get; set; }
        public IMethodSymbol? Replace_Char_Char { get; set; }
        public IMethodSymbol? IndexOf_Char { get; set; }
        public IMethodSymbol? IndexOf_Char_Int32 { get; set; }
        public IMethodSymbol? IndexOf_Char_Int32_Int32 { get; set; }
        public IMethodSymbol? IndexOf_Char_StringComparison { get; set; }
        
        public IMethodSymbol? LastIndexOf_Char { get; set; }
        public IMethodSymbol? LastIndexOf_Char_Int32 { get; set; }
        public IMethodSymbol? LastIndexOf_Char_Int32_Int32 { get; set; }
        public IMethodSymbol? LastIndexOf_Char_StringComparison { get; set; }

        public INamedTypeSymbol? StringComparisonSymbol { get; set; }
        public ISymbol? StringComparison_Ordinal { get; set; }
        public ISymbol? StringComparison_CurrentCulture { get; set; }
        public ISymbol? StringComparison_InvariantCulture { get; set; }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation.TargetMethod.IsEqualTo(StartsWith_Char) || operation.TargetMethod.IsEqualTo(EndsWith_Char))
                return;

            if (operation.TargetMethod.ContainingType.IsString())
            {
                if (operation.TargetMethod.Name is "StartsWith")
                {
                    if (StartsWith_Char == null)
                        return;

                    if (operation.Arguments.Length == 2)
                    {
                        if (operation.Arguments[0].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } } &&
                            operation.Arguments[1].Value is { ConstantValue: { HasValue: true, Value: (int)StringComparison.Ordinal } })
                        {
                            context.ReportDiagnostic(s_rule, operation.Arguments[0]);
                        }
                    }
                }
                else if (operation.TargetMethod.Name is "EndsWith")
                {
                    if (EndsWith_Char == null)
                        return;

                    if (operation.Arguments.Length == 2)
                    {
                        if (operation.Arguments[0].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } } &&
                            operation.Arguments[1].Value is { ConstantValue: { HasValue: true, Value: (int)StringComparison.Ordinal } })
                        {
                            context.ReportDiagnostic(s_rule, operation.Arguments[0]);
                        }
                    }
                }
                else if (operation.TargetMethod.Name is "Replace")
                {
                    if (Replace_Char_Char == null)
                        return;

                    if (operation.Arguments.Length == 2)
                    {
                        if (operation.Arguments[0].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } } &&
                            operation.Arguments[1].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } })
                        {
                            context.ReportDiagnostic(s_rule, operation);
                        }
                    }
                    else if (operation.Arguments.Length == 3)
                    {
                        if (operation.Arguments[0].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } } &&
                            operation.Arguments[1].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } } &&
                            operation.Arguments[2].Value is { ConstantValue: { HasValue: true, Value: (int)StringComparison.Ordinal } })
                        {
                            context.ReportDiagnostic(s_rule, operation);
                        }
                    }
                }
                else if (operation.TargetMethod.Name is "IndexOf")
                {
                    if (operation.Arguments.Length == 2)
                    {
                        if (IndexOf_Char != null)
                        {
                            if (operation.Arguments[0].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } } &&
                                operation.Arguments[1].Value is { ConstantValue: { HasValue: true, Value: (int)StringComparison.Ordinal } })
                            {
                                context.ReportDiagnostic(s_rule, operation);
                                return;
                            }
                        }

                        if(IndexOf_Char_StringComparison != null)
                        {
                            if (operation.Arguments[0].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } } &&
                                operation.Arguments[1].Value.Type.IsEqualTo(StringComparisonSymbol))
                            {
                                context.ReportDiagnostic(s_rule, operation);
                                return;
                            }
                        }
                    }
                    else if (operation.Arguments.Length == 3)
                    {
                        if (IndexOf_Char_Int32 == null)
                            return;

                        if (operation.Arguments[0].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } } &&
                            operation.Arguments[1].Value.Type.IsInt32() &&
                            operation.Arguments[2].Value is { ConstantValue: { HasValue: true, Value: (int)StringComparison.Ordinal } })
                        {
                            context.ReportDiagnostic(s_rule, operation);
                        }
                    }
                    else if (operation.Arguments.Length == 4)
                    {
                        if (IndexOf_Char_Int32_Int32 == null)
                            return;


                        if (operation.Arguments[0].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } } &&
                            operation.Arguments[1].Value.Type.IsInt32() &&
                            operation.Arguments[2].Value.Type.IsInt32() &&
                            operation.Arguments[3].Value is { ConstantValue: { HasValue: true, Value: (int)StringComparison.Ordinal } })
                        {
                            context.ReportDiagnostic(s_rule, operation);
                        }
                    }
                }
                else if (operation.TargetMethod.Name is "LastIndexOf")
                {
                    if (operation.Arguments.Length == 2)
                    {
                        if (LastIndexOf_Char != null)
                        {
                            if (operation.Arguments[0].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } } &&
                                operation.Arguments[1].Value is { ConstantValue: { HasValue: true, Value: (int)StringComparison.Ordinal } })
                            {
                                context.ReportDiagnostic(s_rule, operation);
                                return;
                            }
                        }

                        if(LastIndexOf_Char_StringComparison != null)
                        {
                            if (operation.Arguments[0].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } } &&
                                operation.Arguments[1].Value.Type.IsEqualTo(StringComparisonSymbol))
                            {
                                context.ReportDiagnostic(s_rule, operation);
                                return;
                            }
                        }
                    }
                    else if (operation.Arguments.Length == 3)
                    {
                        if (LastIndexOf_Char_Int32 == null)
                            return;


                        if (operation.Arguments[0].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } } &&
                            operation.Arguments[1].Value.Type.IsInt32() &&
                            operation.Arguments[2].Value is { ConstantValue: { HasValue: true, Value: (int)StringComparison.Ordinal } })
                        {
                            context.ReportDiagnostic(s_rule, operation);
                        }
                    }
                    else if (operation.Arguments.Length == 4)
                    {
                        if (LastIndexOf_Char_Int32_Int32 == null)
                            return;


                        if (operation.Arguments[0].Value is { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: string { Length: 1 } } } &&
                            operation.Arguments[1].Value.Type.IsInt32() &&
                            operation.Arguments[2].Value.Type.IsInt32() &&
                            operation.Arguments[3].Value is { ConstantValue: { HasValue: true, Value: (int)StringComparison.Ordinal } })
                        {
                            context.ReportDiagnostic(s_rule, operation);
                        }
                    }
                }
            }
        }
    }
}
