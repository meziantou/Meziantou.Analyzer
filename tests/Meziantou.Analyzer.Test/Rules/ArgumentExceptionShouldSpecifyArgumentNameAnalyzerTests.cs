using System.ComponentModel;
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class ArgumentExceptionShouldSpecifyArgumentNameAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<ArgumentExceptionShouldSpecifyArgumentNameAnalyzer>();
        }

        [TestMethod]
        public async Task ArgumentNameIsSpecified_ShouldNotReportError()
        {
            var sourceCode = @"
class Sample
{
    string Prop
    {
        get { throw null; }
        set { throw new System.ArgumentNullException(nameof(value)); }
    }

    string this[int index]
    {
        get { throw new System.ArgumentNullException(nameof(index)); }
        set { throw new System.ArgumentNullException(nameof(index)); }
    }

    Sample(string test)
    {
        throw new System.Exception();
        throw new System.ArgumentException(""message"", ""test"");
        throw new System.ArgumentException(""message"", nameof(test));
        throw new System.ArgumentNullException(nameof(test));
    }

    void Test(string test)
    {
        throw new System.Exception();
        throw new System.ArgumentException(""message"", ""test"");
        throw new System.ArgumentException(""message"", nameof(test));
        throw new System.ArgumentNullException(nameof(test));
        throw new System.ComponentModel.InvalidEnumArgumentException(nameof(test), 0, typeof(System.Enum));

        void LocalFunction(string a)
        {
            throw new System.ArgumentNullException(nameof(a));
        }
    }
}";

            await CreateProjectBuilder()
                  .AddReference(typeof(InvalidEnumArgumentException))
                  .WithSourceCode(sourceCode)
                  .ShouldNotReportDiagnostic()
                  .ValidateAsync();
        }
        
        [TestMethod]
        public async Task ArgumentNameDoesNotMatchAParameter_Properties_ShouldReportError()
        {
            const string SourceCode = @"
class TestAttribute
{
    string Prop
    {
        get { throw null; }
        set { throw new System.ArgumentNullException(""unknown""); }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 21, message: "'unknown' is not a valid parameter name")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task ArgumentNameDoesNotMatchAParameter_Methods_ShouldReportError()
        {
            const string SourceCode = @"
class TestAttribute
{
    void Test(string test)
    {
        throw new System.ArgumentException(""message"", ""unknown"");
    }  
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 15, message: "'unknown' is not a valid parameter name")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task OverloadWithoutParameterName_Properties_ShouldReportError()
        {
            const string SourceCode = @"
class TestAttribute
{
    string Prop
    {
        get { throw null; }
        set { throw new System.ArgumentNullException(); }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 21, message: "Use an overload of 'System.ArgumentNullException' with the parameter name")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task OverloadWithoutParameterName_Methods_ShouldReportError()
        {
            const string SourceCode = @"
class TestAttribute
{
    void Test(string test)
    {
        throw new System.ArgumentException(""message"");
    }    
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 15, message: "Use an overload of 'System.ArgumentException' with the parameter name")
                  .ValidateAsync();
        }
    }
}
