﻿using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DoNotUseStringGetHashCodeAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseStringGetHashCodeAnalyzer>()
                .WithCodeFixProvider<DoNotUseStringGetHashCodeFixer>();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task GetHashCode_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        ""a"".GetHashCode();
        System.StringComparer.Ordinal.GetHashCode(""a"");
        new object().GetHashCode();
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        System.StringComparer.Ordinal.GetHashCode(""a"");
        System.StringComparer.Ordinal.GetHashCode(""a"");
        new object().GetHashCode();
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 9)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }
    }
}
