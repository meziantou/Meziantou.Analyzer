using System.Collections.Immutable;
using System.Diagnostics;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DebuggerDisplayAttributeShouldContainValidExpressionsAnalyzer : DiagnosticAnalyzer
{
    private static readonly Dictionary<string, SpecialType> CSharpKeywordToTypeName = new(StringComparer.Ordinal)
    {
        ["bool"] = SpecialType.System_Boolean,
        ["byte"] = SpecialType.System_Byte,
        ["sbyte"] = SpecialType.System_SByte,
        ["char"] = SpecialType.System_Char,
        ["decimal"] = SpecialType.System_Decimal,
        ["double"] = SpecialType.System_Double,
        ["float"] = SpecialType.System_Single,
        ["int"] = SpecialType.System_Int32,
        ["uint"] = SpecialType.System_UInt32,
        ["nint"] = SpecialType.System_IntPtr,
        ["nuint"] = SpecialType.System_UIntPtr,
        ["long"] = SpecialType.System_Int64,
        ["ulong"] = SpecialType.System_UInt64,
        ["short"] = SpecialType.System_Int16,
        ["ushort"] = SpecialType.System_UInt16,
        ["object"] = SpecialType.System_Object,
        ["string"] = SpecialType.System_String,
    };

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
    }

    private static void ValidateValue(SymbolAnalysisContext context, INamedTypeSymbol symbol, AttributeData attribute, string value)
    {
        var expressions = ExtractExpressions(value.AsSpan());
        if (expressions is null)
            return;

        foreach (var expression in expressions)
        {
            if (string.IsNullOrWhiteSpace(expression))
                continue;

            var expressionSyntax = SyntaxFactory.ParseExpression(expression);
            foreach (var memberPath in ExtractMemberPaths(expressionSyntax))
            {
                if (!IsValid(context.Compilation, symbol, memberPath, out var invalidMember))
                {
                    context.ReportDiagnostic(Rule, attribute, invalidMember);
                    break;
                }
            }
        }
    }

    private static List<List<string>> ExtractMemberPaths(ExpressionSyntax expressionSyntax)
    {
        var paths = new List<List<string>>();
        ExtractMemberPaths(paths, expressionSyntax);
        return paths;

        static void ExtractMemberPaths(List<List<string>> results, ExpressionSyntax expressionSyntax)
        {
            var path = new List<string>();

            while (expressionSyntax is not null)
            {
                switch (expressionSyntax)
                {
                    case ParenthesizedExpressionSyntax parenthesizedExpression:
                        // Check if parentheses are necessary
                        // (Instance).Member => Instance.Member
                        // ((Instance + value)).Member => stop evaluating
                        if (path.Count is 0)
                        {
                            expressionSyntax = parenthesizedExpression.Expression;
                        }
                        else
                        {
                            if (parenthesizedExpression.Expression is MemberAccessExpressionSyntax or IdentifierNameSyntax or ParenthesizedExpressionSyntax)
                            {
                                expressionSyntax = parenthesizedExpression.Expression;
                            }
                            else
                            {
                                return;
                            }
                        }

                        break;

                    case IdentifierNameSyntax identifierName:
                        path.Insert(0, identifierName.Identifier.ValueText);
                        results.Add(path);
                        return;

                    case MemberAccessExpressionSyntax memberAccessExpression:
                        path.Insert(0, memberAccessExpression.Name.Identifier.ValueText);
                        expressionSyntax = memberAccessExpression.Expression;
                        break;

                    case InvocationExpressionSyntax invocationExpression:
                        foreach (var argument in invocationExpression.ArgumentList.Arguments)
                        {
                            ExtractMemberPaths(results, argument.Expression);
                        }

                        path.Clear(); // Clear the path because we don't know the return type
                        expressionSyntax = invocationExpression.Expression;
                        break;

                    case BinaryExpressionSyntax binaryExpression:
                        ExtractMemberPaths(results, binaryExpression.Left);
                        ExtractMemberPaths(results, binaryExpression.Right);
                        return;

                    case PrefixUnaryExpressionSyntax unaryExpression:
                        ExtractMemberPaths(results, unaryExpression.Operand);
                        return;

                    case PostfixUnaryExpressionSyntax unaryExpression:
                        ExtractMemberPaths(results, unaryExpression.Operand);
                        return;

                    case ElementAccessExpressionSyntax elementAccess:
                        foreach (var argument in elementAccess.ArgumentList.Arguments)
                        {
                            ExtractMemberPaths(results, argument.Expression);
                        }

                        path.Clear(); // Clear the path because we don't know the return type
                        expressionSyntax = elementAccess.Expression;
                        break;

                    default:
                        return;
                }
            }

            results.Add(path);
        }
    }

    private static bool IsValid(Compilation compilation, ISymbol rootSymbol, List<string> syntax, out string? invalidMember)
    {
        if (syntax.Count is 0)
        {
            invalidMember = null;
            return true;
        }

        var firstMember = syntax[0];
        var current = FindSymbol(compilation, rootSymbol, firstMember) ?? FindGlobalSymbol(compilation, firstMember);
        if (current is null)
        {
            invalidMember = firstMember;
            return false;
        }

        foreach (var member in syntax.Skip(1))
        {
            if (current is ITypeSymbol { TypeKind: TypeKind.TypeParameter })
            {
                // Cannot continue analysis easily
                invalidMember = null;
                return true;
            }

            var next = FindSymbol(compilation, current, member);
            if (next is null)
            {
                invalidMember = member;
                return false;
            }

            current = next;
        }

        invalidMember = null;
        return true;

        static ISymbol? FindSymbol(Compilation compilation, ISymbol parent, string name)
        {
            if (name is "ToString" or "GetHashCode")
            {
                compilation.GetSpecialType(SpecialType.System_Object).GetMembers(name).FirstOrDefault();
            }

            if (parent is INamespaceOrTypeSymbol typeSymbol)
            {
                if (typeSymbol.GetAllMembers(name).FirstOrDefault() is { } member)
                {
                    if (member is INamespaceOrTypeSymbol)
                        return member;

                    return member.GetSymbolType();
                }
            }

            return null;
        }

        static ISymbol? FindGlobalSymbol(Compilation compilation, string name)
        {
            if (CSharpKeywordToTypeName.TryGetValue(name, out var specialType))
                return compilation.GetSpecialType(specialType);

            return compilation.GlobalNamespace.GetMembers(name).FirstOrDefault();
        }
    }

    private static List<string>? ExtractExpressions(ReadOnlySpan<char> value)
    {
        List<string>? result = null;

        while (!value.IsEmpty)
        {
            var startIndex = IndexOf(value, '{');
            if (startIndex < 0)
                break;

            value = value[(startIndex + 1)..];
            var endIndex = IndexOf(value, '}');
            if (endIndex < 0)
                break;

            var expression = value[..endIndex];
            result ??= [];
            result.Add(expression.ToString());

            value = value[(endIndex + 1)..];
        }

        return result;

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
    }
}
