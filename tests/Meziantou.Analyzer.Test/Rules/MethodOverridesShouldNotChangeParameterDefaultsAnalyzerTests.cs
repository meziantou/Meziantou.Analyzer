using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class MethodOverridesShouldNotChangeParameterDefaultsAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<MethodOverridesShouldNotChangeParameterDefaultsAnalyzer>();
        }

        [TestMethod]
        public async Task Interface_ExplicitImplementation()
        {
            const string SourceCode = @"
interface ITest
{
    void A(int a = 0);
}

class Test : ITest
{
    void ITest.A(int a) { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Interface_SameValue()
        {
            const string SourceCode = @"
interface ITest
{
    void A(int a = 0);
}

class Test : ITest
{
    public void A(int a = 0) { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Override_SameValue()
        {
            const string SourceCode = @"
class Test
{
    public virtual void A(int a = 0, int b = 1) { }
}

class TestDerived : Test
{
    public override void A(int a = 0, int b = 1) { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Override_DifferentValue()
        {
            const string SourceCode = @"
class Test
{
    public virtual void A(int a = 0, int b = 1) { }
}

class TestDerived : Test
{
    public override void A(int [||]a = 1, int [||]b = 2) { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Method overrides should not change parameter defaults (original: '0'; current: '1')")
                  .ShouldReportDiagnosticWithMessage("Method overrides should not change parameter defaults (original: '1'; current: '2')")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task New_DifferentValue()
        {
            const string SourceCode = @"
class Test
{
    public virtual void A(int a = 0, int b = 1) { }
}

class TestDerived : Test
{
    public void A(int a = 1, int b = 2) { } // no override
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Override_DifferentValue_OriginalParameterHasNoDefault()
        {
            const string SourceCode = @"
class Test
{
    public virtual void A(int a) { }
}

class TestDerived : Test
{
    public override void A(int [||]a = 1) { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Method overrides should not change parameter defaults (original: <no default value>; current: '1')")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Override_DifferentValue_OverrideParameterHasNoDefault()
        {
            const string SourceCode = @"
class Test
{
    public virtual void A(int a = 0) { }
}

class TestDerived : Test
{
    public override void A(int [||]a) { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Method overrides should not change parameter defaults (original: '0'; current: <no default value>)")
                  .ValidateAsync();
        }
    }
}
