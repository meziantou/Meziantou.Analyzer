using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public class PublicRecordAnalyzerTest
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<PublicRecordAnalyzer>();
        }

        [Fact]
        public async Task GivenPublicRecord_CodeFix_ReturnsPublicRecord()
        {
            var violation = @"
using System.Runtime;
public record [||]SomeRecord(int Id);";

            var fix = @"
using System.Runtime;
public sealed record SomeRecord(int Id);";

            await CreateProjectBuilder()
                .WithSourceCode(violation)
                .WithCodeFixProvider<PublicRecordCodeFixer>()
                .ShouldReportDiagnosticWithMessage("Public record 'SomeRecord' should be annotated with 'sealed'.")
                .ShouldFixCodeWith(fix)
                .WithTargetFramework(TargetFramework.Net8_0)
#if CSHARP10_OR_GREATER
                .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
#endif
                .ValidateAsync();
        }
    }
}