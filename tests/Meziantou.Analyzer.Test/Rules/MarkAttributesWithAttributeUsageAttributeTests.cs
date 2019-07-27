using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class MarkAttributesWithAttributeUsageAttributeTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<MarkAttributesWithAttributeUsageAttributeAnalyzer>()
                .WithCodeFixProvider<MarkAttributesWithAttributeUsageAttributeFixer>();
        }

        [Fact]
        public async System.Threading.Tasks.Task ClassInheritsFromAttribute_MissingAttribute_ShouldReportErrorAsync()
        {
            const string SourceCode = "class [||]TestAttribute : System.Attribute { }";

            const string CodeFix = @"[System.AttributeUsage(System.AttributeTargets.All)]
class TestAttribute : System.Attribute { }";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task ClassDoesNotInheritsFromAttribute_ShouldNotReportErrorAsync()
        {
            await CreateProjectBuilder()
                  .WithSourceCode("class TestAttribute : System.Object { }")
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task ClassHasAttribute_ShouldNotReportErrorAsync()
        {
            const string SourceCode = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = false, Inherited = true)]
class TestAttribute : System.Attribute { }";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
