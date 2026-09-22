using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The position of a navigator, as the banned syntax rule reports it.
/// </summary>
internal interface IBannedSyntaxNavigator
{
    /// <summary>
    /// The spans to report the position on. A node, an operation, or the tokens of an attribute have a single span,
    /// whereas a symbol has one per declaration in the file, such as the parts of a partial type.
    /// </summary>
    ImmutableArray<TextSpan> ReportSpans { get; }

    /// <summary>
    /// The name to report, such as <c>GotoStatement</c> or <c>operation:Invocation/@TargetMethod</c>. It is null
    /// when the position is not reportable, such as the document that contains the elements.
    /// </summary>
    string? ReportName { get; }
}
