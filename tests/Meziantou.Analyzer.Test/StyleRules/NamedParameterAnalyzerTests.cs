using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.StyleRules
{
    [TestClass]
    public class NamedParameterAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new NamedParameterAnalyzer();

        [TestMethod]
        public void EmptyString_ShouldNotReportDiagnosticForEmptyString()
        {
            var test = "";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Task_ConfigureAwait_ShouldNotReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public async System.Threading.Tasks.Task Test()
    {
        await System.Threading.Tasks.Task.Run(()=>{}).ConfigureAwait(false);
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Task_T_ConfigureAwait_ShouldNotReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public async System.Threading.Tasks.Task Test()
    {
        await System.Threading.Tasks.Task.Run(() => 10).ConfigureAwait(true);
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NamedParameter_ShouldNotReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        object.Equals(objA: true, "");
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void True_ShouldReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        object.Equals(true, "");
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "MA0003",
                Message = "Name the parameter to improve the readability of the code",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 23)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void False_ShouldReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        object.Equals(false, "");
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "MA0003",
                Message = "Name the parameter to improve the readability of the code",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 23)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void Null_ShouldReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        object.Equals(null, "");
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "MA0003",
                Message = "Name the parameter to improve the readability of the code",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 23)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void MethodBaseInvoke_FirstArg_ShouldNotReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        typeof(TypeName).GetMethod("").Invoke(null, new object[0])
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void MethodBaseInvoke_ShouldReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        typeof(TypeName).GetMethod("""").Invoke(null, null);
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "MA0003",
                Message = "Name the parameter to improve the readability of the code",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 53)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void Attribute_ShouldNotReportDiagnostic()
        {
            var test = @"
[assembly: SkipNamedAttribute(""TypeName"", ""Test"")]
internal class SkipNamedAttribute : System.Attribute
{
    public SkipNamedAttribute(string typeName, string methodName) { }
}

class TypeName
{
    public void Test(string name) => Test(null);
    public void Test2(string name) => Test2(null);
}";

            var expected = new DiagnosticResult
            {
                Id = "MA0003",
                Message = "Name the parameter to improve the readability of the code",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 11, column: 45)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void AttributeWithWildcard_ShouldNotReportDiagnostic()
        {
            var test = @"
[assembly: SkipNamedAttribute(""TypeName"", ""*"")]
internal class SkipNamedAttribute : System.Attribute
{
    public SkipNamedAttribute(string typeName, string methodName) { }
}

class TypeName
{
    public void Test(string name) => Test(null);
    public void Test2(string name) => Test2(null);
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void MSTestAssert_ShouldNotReportDiagnostic()
        {
            var test = @"
namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    public class Assert
    {
        public static void AreEqual(object expected, object actual) { }
    }
}

class TypeName
{
    public void Test() => Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(null, true);
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NunitAssert_ShouldNotReportDiagnostic()
        {
            var test = @"
namespace NUnit.Framework
{
    public class Assert
    {
        public static void AreEqual(object expected, object actual) { }
    }
}

class TypeName
{
    public void Test() => NUnit.Framework.Assert.AreEqual(null, true);
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void XunitAssert_ShouldNotReportDiagnostic()
        {
            var test = @"
namespace Xunit
{
    public class Assert
    {
        public static void AreEqual(object expected, object actual) { }
    }
}

class TypeName
{
    public void Test() => Xunit.Assert.AreEqual(null, true);
}";

            VerifyCSharpDiagnostic(test);
        }
    }
}
