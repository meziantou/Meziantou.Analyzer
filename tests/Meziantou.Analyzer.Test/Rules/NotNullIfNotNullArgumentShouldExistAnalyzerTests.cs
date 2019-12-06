using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class NotNullIfNotNullArgumentShouldExistAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<NotNullIfNotNullArgumentShouldExistAnalyzer>();
        }

        [Fact]
        public async Task ParameterDoesNotExist()
        {
            const string SourceCode = @"
class Test
{
    [[|System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute(""unknown"")|]]
    public void A(string a) { }
}

namespace System.Diagnostics.CodeAnalysis
{
    public class NotNullIfNotNullAttribute : System.Attribute
    {
        public NotNullIfNotNullAttribute (string parameterName) => throw null;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
        
        [Fact]
        public async Task ParameterExists()
        {
            const string SourceCode = @"
class Test
{
    [System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute(""a"")]
    public void A(string a) { }
}

namespace System.Diagnostics.CodeAnalysis
{
    public class NotNullIfNotNullAttribute : System.Attribute
    {
        public NotNullIfNotNullAttribute (string parameterName) => throw null;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

    }
}
