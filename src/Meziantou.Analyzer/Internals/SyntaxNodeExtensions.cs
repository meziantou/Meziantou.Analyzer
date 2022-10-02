using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Internals;

internal static class SyntaxNodeExtensions
{
	public static bool ContainsUsingStatement(this BlockSyntax block)
	{
		return block.Statements.OfType<LocalDeclarationStatementSyntax>().Any(l => l.UsingKeyword.IsKind(SyntaxKind.UsingKeyword));
	}

	public static SyntaxNode? GetNextSibling(this SyntaxNode statementInBlock)
	{
        if (statementInBlock.Parent is not BlockSyntax block)
        {
            return null;
        }

        var numberOfStatements = block.Statements.Count;
        for (var i = 0; i < numberOfStatements; i++)
        {
            if(block.Statements[i].Equals(statementInBlock) && i < numberOfStatements - 1)
            {
                return block.Statements[i + 1];
            }
        }

        return null;
	}

	public static bool HasParent(this SyntaxNode node, SyntaxKind kind)
	{
		var parent = node.Parent;
		while (parent is not null)
		{
			if (parent.IsKind(kind))
			{
				return true;
			}

			parent = parent.Parent;
		}

		return false;
	}

	public static bool IsNextStatementReturnStatement(this SyntaxNode node)
	{
		return node.Parent?.GetNextSibling()?.IsKind(SyntaxKind.ReturnStatement) == true;
	}
}
