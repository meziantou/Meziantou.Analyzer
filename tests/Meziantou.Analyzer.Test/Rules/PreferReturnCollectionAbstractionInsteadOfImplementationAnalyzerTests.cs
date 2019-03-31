using System.Collections.Generic;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class PreferReturnCollectionAbstractionInsteadOfImplementationAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<PreferReturnCollectionAbstractionInsteadOfImplementationAnalyzer>();
        }

        private static IEnumerable<object[]> ReturnTypeValues
        {
            get
            {
                yield return new object[] { "private", "List<int>", true };
                yield return new object[] { "private", "List<int>", true };
                yield return new object[] { "public", "Task<List<int>>", false };
                yield return new object[] { "public", "List<int>", false };
                yield return new object[] { "protected", "List<int>", false };
                yield return new object[] { "private protected", "List<int>", true };
                yield return new object[] { "public", "string", true };
            }
        }

        private static IEnumerable<object[]> ParametersTypeValues
        {
            get
            {
                yield return new object[] { "private", "List<int>", true };
                yield return new object[] { "public", "List<int>", false };
                yield return new object[] { "protected", "List<int>", false };
                yield return new object[] { "private protected", "List<int>", true };
                yield return new object[] { "public", "string", true };
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public async System.Threading.Tasks.Task FieldsAsync(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + type + @" _a;
}");

            if (isValid)
            {
                project.ShouldNotReportDiagnostic();
            }
            else
            {
                project.ShouldReportDiagnostic(line: 5, column: 5);
            }

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public async System.Threading.Tasks.Task DelegatesAsync(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" delegate
    " + type + @" A();
}");

            if (isValid)
            {
                project.ShouldNotReportDiagnostic();
            }
            else
            {
                project.ShouldReportDiagnostic(line: 5, column: 5);
            }

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ParametersTypeValues), DynamicDataSourceType.Property)]
        public async System.Threading.Tasks.Task Delegates_ParametersAsync(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" delegate void A(
    " + type + @" p);
}");

            if (isValid)
            {
                project.ShouldNotReportDiagnostic();
            }
            else
            {
                project.ShouldReportDiagnostic(line: 5, column: 5);
            }

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public async System.Threading.Tasks.Task IndexersAsync(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + type + @" this[int value] => throw null;
}");


            if (isValid)
            {
                project.ShouldNotReportDiagnostic();
            }
            else
            {
                project.ShouldReportDiagnostic(line: 5, column: 5);
            }

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ParametersTypeValues), DynamicDataSourceType.Property)]
        public async System.Threading.Tasks.Task Indexers_ParametersAsync(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" int this[
    " + type + @" value] => throw null;
}");


            if (isValid)
            {
                project.ShouldNotReportDiagnostic();
            }
            else
            {
                project.ShouldReportDiagnostic(line: 5, column: 5);
            }

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public async System.Threading.Tasks.Task PropertiesAsync(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + type + @" A => throw null;
}");

            if (isValid)
            {
                project.ShouldNotReportDiagnostic();
            }
            else
            {
                project.ShouldReportDiagnostic(line: 5, column: 5);
            }

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public async System.Threading.Tasks.Task MethodsAsync(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + type + @" A() => throw null;
}");

            if (isValid)
            {
                project.ShouldNotReportDiagnostic();
            }
            else
            {
                project.ShouldReportDiagnostic(line: 5, column: 5);
            }

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(ParametersTypeValues), DynamicDataSourceType.Property)]
        public async System.Threading.Tasks.Task Methods_ParametersAsync(string visibility, string type, bool isValid)
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" void A(
    " + type + @" p) => throw null;
}");

            if (isValid)
            {
                project.ShouldNotReportDiagnostic();
            }
            else
            {
                project.ShouldReportDiagnostic(line: 5, column: 5);
            }

            await project.ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task PrivateContainerAsync()
        {
            const string SourceCode = @"using System.Collections.Generic;
internal class Test
{
    public delegate List<int> B();
    public List<int> _a;
    protected List<int> _b;
    public List<int> A() => throw null;
}";
            await CreateProjectBuilder()
                 .WithSourceCode(SourceCode)
                 .ShouldNotReportDiagnostic()
                 .ValidateAsync();
        }
    }
}
