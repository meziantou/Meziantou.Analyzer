﻿using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class UseRegexTimeoutAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseRegexTimeoutAnalyzer>();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task IsMatch_MissingTimeout_ShouldReportErrorAsync()
        {
            const string SourceCode = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        [||]Regex.IsMatch(""test"", ""[a-z]+"");
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task IsMatch_WithTimeout_ShouldNotReportErrorAsync()
        {
            const string SourceCode = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        Regex.IsMatch(""test"", ""[a-z]+"", RegexOptions.ExplicitCapture, System.TimeSpan.FromSeconds(1));
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Ctor_MissingTimeout_ShouldReportErrorAsync()
        {
            const string SourceCode = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        [||]new Regex(""[a-z]+"");
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Ctor_WithTimeout_ShouldNotReportErrorAsync()
        {
            const string SourceCode = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        new Regex(""[a-z]+"", RegexOptions.ExplicitCapture, System.TimeSpan.FromSeconds(1));
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task NonRegexCtor_ShouldNotReportErrorAsync()
        {
            const string SourceCode = @"
class TestClass
{
    void Test()
    {
        new System.Exception("""");
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
