using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DebuggerDisplayAttributeShouldContainValidExpressionsAnalyzer : DiagnosticAnalyzer
{
    private static readonly char[] MemberSeparators = [',', '(', '.', '['];

    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DebuggerDisplayAttributeShouldContainValidExpressions,
        title: "DebuggerDisplay must contain valid members",
        messageFormat: "Member '{0}' does not exist",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DebuggerDisplayAttributeShouldContainValidExpressions));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var attributeSymbol = context.Compilation.GetBestTypeByMetadataName("System.Diagnostics.DebuggerDisplayAttribute");
            if (attributeSymbol is null)
                return;

            context.RegisterSymbolAction(context => AnalyzeNamedType(context, attributeSymbol), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol attributeSymbol)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        foreach (var attribute in symbol.GetAttributes())
        {
            if (!attribute.AttributeClass.IsEqualTo(attributeSymbol))
                continue;

            if (attribute.ConstructorArguments is [{ Kind: TypedConstantKind.Primitive, IsNull: false, Type.SpecialType: SpecialType.System_String, Value: string value }, ..])
            {
                var members = ParseMembers(value.AsSpan());
                if (members is not null)
                {
                    foreach (var member in members)
                    {
                        if (!MemberExists(symbol, member))
                        {
                            context.ReportDiagnostic(Rule, attribute, member);
                            return;
                        }
                    }
                }
            }
        }

        static bool MemberExists(INamedTypeSymbol? symbol, string name)
        {
            while (symbol is not null)
            {
                if (!symbol.GetMembers(name).IsEmpty)
                    return true;

                symbol = symbol.BaseType;
            }

            return false;
        }

        static List<string>? ParseMembers(ReadOnlySpan<char> value)
        {
            List<string>? result = null;

            while (!value.IsEmpty)
            {
                var startIndex = value.IndexOf('{');
                if (startIndex < 0)
                    break;

                value = value[(startIndex + 1)..];
                var endIndex = value.IndexOf('}');
                if (endIndex < 0)
                    break;

                var member = value[..endIndex];


                static string GetMemberName(ReadOnlySpan<char> member)
                {
                    var index = member.IndexOfAny(MemberSeparators);
                    if (index < 0)
                        return member.ToString();

                    return member[..index].ToString();
                }

                result ??= [];
                result.Add(GetMemberName(member));

                value = value[(endIndex + 1)..];
            }

            return result;
        }
    }
}
