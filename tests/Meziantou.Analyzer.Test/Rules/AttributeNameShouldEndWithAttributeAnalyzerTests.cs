using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class AttributeNameShouldEndWithAttributeAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<AttributeNameShouldEndWithAttributeAnalyzer>();
        }

        [TestMethod]
        public async Task NameEndsWithAttribute()
        {
            const string SourceCode = @"
class CustomAttribute : System.Attribute
{
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task NameDoesNotEndWithAttribute()
        {
            const string SourceCode = @"
class [|]CustomAttr : System.Attribute
{
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

    }
}
