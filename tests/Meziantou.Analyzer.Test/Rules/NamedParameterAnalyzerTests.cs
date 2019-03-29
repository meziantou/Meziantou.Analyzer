using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class NamedParameterAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new NamedParameterAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new NamedParameterFixer();
        protected override string ExpectedDiagnosticId => "MA0003";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Info;
        protected override string ExpectedDiagnosticMessage => "Name the parameter to improve the readability of the code";

        [TestMethod]
        public void EmptyString_ShouldNotReportDiagnosticForEmptyString()
        {
            var project = new ProjectBuilder();
            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Task_ConfigureAwait_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TypeName
{
    public async System.Threading.Tasks.Task Test()
    {
        await System.Threading.Tasks.Task.Run(()=>{}).ConfigureAwait(false);
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Task_T_ConfigureAwait_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TypeName
{
    public async System.Threading.Tasks.Task Test()
    {
        await System.Threading.Tasks.Task.Run(() => 10).ConfigureAwait(true);
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void NamedParameter_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        object.Equals(objA: true, """");
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void True_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        var a = string.Compare("""", """", true);
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 40);
            VerifyDiagnostic(project, expected);

            var fix = @"
class TypeName
{
    public void Test()
    {
        var a = string.Compare("""", """", ignoreCase: true);
    }
}";

            VerifyFix(project, fix);
        }

        [TestMethod]
        public void False_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        object.Equals(false, """");
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Null_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        object.Equals(null, """");
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MethodBaseInvoke_FirstArg_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        typeof(TypeName).GetMethod("""").Invoke(null, new object[0]);
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MethodBaseInvoke_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        typeof(TypeName).GetMethod("""").Invoke(null, null);
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 53);
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        typeof(TypeName).GetMethod("""").Invoke(null, parameters: null);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void Attribute_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
[assembly: SkipNamedAttribute(""TypeName"", ""Test"")]
internal class SkipNamedAttribute : System.Attribute
{
    public SkipNamedAttribute(string typeName, string methodName) { }
}

class TypeName
{
    public void Test(string name) => Test(null);
    public void Test2(string name) => Test2(null);
}");

            var expected = CreateDiagnosticResult(line: 11, column: 45);
            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void AttributeWithWildcard_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
[assembly: SkipNamedAttribute(""TypeName"", ""*"")]
internal class SkipNamedAttribute : System.Attribute
{
    public SkipNamedAttribute(string typeName, string methodName) { }
}

class TypeName
{
    public void Test(string name) => Test(null);
    public void Test2(string name) => Test2(null);
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MSTestAssert_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddMSTestApi()
                  .WithSourceCode(@"
class TypeName
{
    public void Test() => Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(null, true);
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void NunitAssert_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddNUnitApi()
                  .WithSourceCode(@"
class TypeName
{
    public void Test() => NUnit.Framework.Assert.AreEqual(null, true);
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void XunitAssert_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddXUnitApi()
                  .WithSourceCode(@"
class TypeName
{
    public void Test() => Xunit.Assert.AreEqual(null, true);
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Ctor_ShouldUseTheRightParameterName()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        new TypeName(null);
    }

    TypeName(string a) { }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 22);
            VerifyDiagnostic(project, expected);

            var fix = @"
class TypeName
{
    public void Test()
    {
        new TypeName(a: null);
    }

    TypeName(string a) { }
}";

            VerifyFix(project, fix);
        }

        [TestMethod]
        public void PropertyBuilder_IsUnicode_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TypeName
{
    public async System.Threading.Tasks.Task Test()
    {
        new Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<int>().IsUnicode(false);
    }
}

namespace Microsoft.EntityFrameworkCore.Metadata.Builders
{
    public class PropertyBuilder<TProperty>
    {
        public bool IsUnicode(bool value) => throw null;
    }
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Task_FromResult_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        _ = System.Threading.Tasks.Task.FromResult(true);
    }
}
");

            VerifyDiagnostic(project);
        }
    }
}
