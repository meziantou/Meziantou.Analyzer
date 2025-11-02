using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Internals;

internal record struct OverloadOptions(bool IncludeObsoleteMembers = false, bool AllowOptionalParameters = false, bool IncludeExtensionsMethods = false, SyntaxNode? SyntaxNode = null);
