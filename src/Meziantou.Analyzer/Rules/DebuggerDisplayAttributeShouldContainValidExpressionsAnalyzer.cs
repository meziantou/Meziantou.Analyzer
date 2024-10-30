using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DebuggerDisplayAttributeShouldContainValidExpressionsAnalyzer : DiagnosticAnalyzer
{
    private static readonly char[] MemberSeparators = [',', '(', '.', '[', ' '];

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

            {
                if (attribute.ConstructorArguments is [{ Kind: TypedConstantKind.Primitive, Value: string value }, ..])
                {
                    ValidateValue(context, symbol, attribute, value);
                }
            }

            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key is nameof(DebuggerDisplayAttribute.Name) or nameof(DebuggerDisplayAttribute.Type) && argument.Value is { Kind: TypedConstantKind.Primitive, Value: string value2 })
                {
                    ValidateValue(context, symbol, attribute, value2);
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

            static int IndexOf(ReadOnlySpan<char> value, char c)
            {
                var skipped = 0;
                while (!value.IsEmpty)
                {
                    var index = value.IndexOfAny(c, '\\');
                    if (index < 0)
                        return -1;

                    if (value[index] == c)
                        return index + skipped;

                    if (index + 1 < value.Length)
                    {
                        skipped += index + 2;
                        value = value[(index + 2)..];
                    }
                    else
                    {
                        return -1;
                    }
                }

                return -1;
            }

            while (!value.IsEmpty)
            {
                var startIndex = IndexOf(value, '{');
                if (startIndex < 0)
                    break;

                value = value[(startIndex + 1)..];
                var endIndex = IndexOf(value, '}');
                if (endIndex < 0)
                    break;

                var member = value[..endIndex];
                result ??= [];
                result.Add(GetMemberName(member));

                value = value[(endIndex + 1)..];
            }

            return result;

            static string GetMemberName(ReadOnlySpan<char> member)
            {
                var index = member.IndexOfAny(MemberSeparators);
                if (index < 0)
                    return member.ToString();

                return member[..index].ToString();
            }
        }

        static void ValidateValue(SymbolAnalysisContext context, INamedTypeSymbol symbol, AttributeData attribute, string value)
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
}
