using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Internals;

internal record struct OverloadOptions(
    bool IncludeObsoleteMembers = false,
    bool AllowOptionalParameters = false,
    bool IncludeExtensionsMethods = false,
    SyntaxNode? SyntaxNode = null,
    bool DisableNumericConversion = false,
    bool DisableParamsToNonParamsCompatibility = false,
    bool DisableInModifierCompatibility = false,
    bool DisableInterfaceConversions = false);
