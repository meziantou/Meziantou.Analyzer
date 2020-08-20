using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class AvoidLockingOnPubliclyAccessibleInstanceAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<AvoidLockingOnPubliclyAccessibleInstanceAnalyzer>();
        }

        [Fact]
        public async Task LockThis()
        {
            const string SourceCode = @"
class Test
{
    void A()
    {
        lock ([||]this) {}
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task LockTypeof()
        {
            const string SourceCode = @"
class Test
{
    void A()
    {
        lock ([||]typeof(Test))
        {
            throw null;
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task LockVariableOfTypeSystemType()
        {
            const string SourceCode = @"
class Test
{
    void A()
    {
        System.Type type = null;
        lock ([||]type) {}
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task LockPubliclyAccessibleField()
        {
            const string SourceCode = @"
public class Test
{
    public string TestField;
    void A()
    {
        lock ([||]TestField) {}
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task LockPrivateFieldShouldNotReport()
        {
            const string SourceCode = @"
public class Test
{
    private string TestField;
    void A()
    {
        lock (TestField) {}
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task LockVariableOfTypeStringShouldNotReport()
        {
            const string SourceCode = @"
public class Test
{
    private string TestField;
    void A()
    {
        string test = """";
        lock (test) {}
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
