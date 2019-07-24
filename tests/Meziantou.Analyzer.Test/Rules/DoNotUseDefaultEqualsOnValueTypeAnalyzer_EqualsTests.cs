using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class DoNotUseDefaultEqualsOnValueTypeAnalyzer_EqualsTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseDefaultEqualsOnValueTypeAnalyzer>(id: "MA0065");
        }

        [TestMethod]
        public async Task Equals_DefaultImplementation()
        {
            const string SourceCode = @"
struct Test
{
}

class Sample
{
    public void A()
    {
        _ = [||]new Test().Equals(new Test());
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task ObjectEquals_DefaultImplementation()
        {
            const string SourceCode = @"
struct Test
{
    public void A()
    {
        _ = [||]Equals(new Test());
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Equals_Override()
        {
            const string SourceCode = @"
struct Test
{
    public override bool Equals(object o) => throw null;
    public override int GetHashCode() => throw null;
}

class Sample
{
    public void A()
    {
        _ = new Test().Equals(new Test());
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task GetHashCode_DefaultImplementation()
        {
            const string SourceCode = @"
struct Test
{
}

class Sample
{
    public void A()
    {
        _ = [||]new Test().GetHashCode();
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task GetHashCode_Override()
        {
            const string SourceCode = @"
struct Test
{
    public override bool Equals(object o) => throw null;
    public override int GetHashCode() => throw null;
}

class Sample
{
    public void A()
    {
        _ = new Test().GetHashCode();
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task GetHashCode_Enum()
        {
            const string SourceCode = @"
enum Test
{
    A,
    B,
}

class Sample
{
    public void A()
    {
        _ = Test.A.GetHashCode();
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }


        [TestMethod]
        public async Task GetHashCode_EnumVariable()
        {
            const string SourceCode = @"
enum Test
{
    A,
    B,
}

class Sample
{
    public void A()
    {
        var a = Test.A;
        _ = a.GetHashCode();
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
