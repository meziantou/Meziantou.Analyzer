using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class DoNotUseEqualityComparerDefaultOfStringAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseEqualityComparerDefaultOfStringAnalyzer>()
                .WithCodeFixProvider<DoNotUseEqualityComparerDefaultOfStringFixer>();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestAsync()
        {
            const string SourceCode = @"using System.Collections.Generic;
class Test
{
    internal void Sample()
    {
        _ = EqualityComparer<int>.Default.Equals(0, 0);
        _ = [||]EqualityComparer<string>.Default.Equals(null, null);
    }
}
";
            const string CodeFix = @"using System.Collections.Generic;
class Test
{
    internal void Sample()
    {
        _ = EqualityComparer<int>.Default.Equals(0, 0);
        _ = System.StringComparer.Ordinal.Equals(null, null);
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }
    }
}
