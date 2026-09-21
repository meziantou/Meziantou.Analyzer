using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The position of a navigator, as the banned syntax rule reports it.
/// </summary>
internal interface IBannedSyntaxNavigator
{
    /// <summary>The span of the node or of the tokens of the attribute the navigator is positioned on.</summary>
    TextSpan Span { get; }

    /// <summary>
    /// The name to report, such as <c>GotoStatement</c> or <c>operation:Invocation/@TargetMethod</c>. It is null
    /// when the position is not reportable, such as the document that contains the elements.
    /// </summary>
    string? ReportName { get; }
}
