using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class ReplaceEnumToStringWithNameofAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<ReplaceEnumToStringWithNameofAnalyzer>()
                .WithCodeFixProvider<ReplaceEnumToStringWithNameofFixer>();
        }

        [TestMethod]
        public async Task ConstantEnumValueToString()
        {
            const string SourceCode = @"
class Test
{
    void A()
    {
        _ = [|]MyEnum.A.ToString();
    }
}

enum MyEnum
{
    A,
}";

            const string CodeFix = @"
class Test
{
    void A()
    {
        _ = nameof(MyEnum.A);
    }
}

enum MyEnum
{
    A,
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task EnumVariableToString()
        {
            const string SourceCode = @"
class Test
{
    void A()
    {
        var a = MyEnum.A;
        _ = a.ToString();
    }
}

enum MyEnum
{
    A,
}";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
