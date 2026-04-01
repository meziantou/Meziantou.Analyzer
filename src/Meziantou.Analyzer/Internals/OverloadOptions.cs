using System;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Internals;

internal record struct OverloadOptions(
    bool IncludeObsoleteMembers = false,
    bool AllowOptionalParameters = false,
    bool IncludeExtensionsMethods = false,
    SyntaxNode? SyntaxNode = null,
    bool AllowNumericConversion = true,
    bool AllowParamsToNonParamsCompatibility = true,
    bool AllowInModifierCompatibility = true,
    bool AllowInterfaceConversions = true,
    Func<IMethodSymbol, bool>? ShouldCheckMethod = null);
