using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;
using System.Threading.Tasks;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class NamedParameterAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<NamedParameterAnalyzer>()
                .WithCodeFixProvider<NamedParameterFixer>();
        }

        [Fact]

        public async Task Task_ConfigureAwait_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public async System.Threading.Tasks.Task Test()
    {
        await System.Threading.Tasks.Task.Run(()=>{}).ConfigureAwait(false);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Task_T_ConfigureAwait_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public async System.Threading.Tasks.Task Test()
    {
        await System.Threading.Tasks.Task.Run(() => 10).ConfigureAwait(true);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task NamedParameter_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        object.Equals(objA: true, """");
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task True_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var a = string.Compare("""", """", [||]true);
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        var a = string.Compare("""", """", ignoreCase: true);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task False_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        object.Equals(false, """");
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Null_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        object.Equals(null, """");
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task MethodBaseInvoke_FirstArg_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        typeof(TypeName).GetMethod("""").Invoke(null, new object[0]);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task MethodBaseInvoke_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        typeof(TypeName).GetMethod("""").Invoke(null, [||]null);
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        typeof(TypeName).GetMethod("""").Invoke(null, parameters: null);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task MSTestAssert_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test() => Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(null, true);
}";
            await CreateProjectBuilder()
                  .AddMSTestApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task NunitAssert_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test() => NUnit.Framework.Assert.AreEqual(null, true);
}";
            await CreateProjectBuilder()
                  .AddNUnitApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task XunitAssert_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test() => Xunit.Assert.AreEqual(null, true);
}";
            await CreateProjectBuilder()
                  .AddXUnitApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Ctor_ShouldUseTheRightParameterName()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        new TypeName([||]null);
    }

    TypeName(string a) { }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        new TypeName(a: null);
    }

    TypeName(string a) { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task PropertyBuilder_IsUnicode_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
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
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Task_FromResult_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        _ = System.Threading.Tasks.Task.FromResult(true);
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Expression_ShouldNotReportDiagnostic()
        {

            await CreateProjectBuilder()
                  .WithSourceCode(@"
using System.Linq;
using System.Collections.Generic;

class Test
{
    public Test()
    {
        IEnumerable<string> query = null;
        query.Where(x => M([||]false));
    }

    static bool M(bool a) => false;
}
")
                  .ValidateAsync();

            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        IQueryable<string> query = null;
        query.Where(x => M(false));
    }

    static bool M(bool a) => false;
}
")
                  .ValidateAsync();
        }

        [Fact]
        public async Task Expression_ShouldNotReportDiagnostic2()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
using System;
using System.Linq;
using System.Linq.Expressions;

class Test
{
    public Test()
    {
        Mock<ITest> mock = null;
        mock.Setup(x => x.M(false));
    }

    static bool M(bool a) => false;
}

interface ITest
{
    bool M(bool a);
}

class Mock<T>
{
    public void Setup<TResult>(Expression<Func<T, TResult>> expression) => throw null;
}
")
                  .ValidateAsync();
        }
    }
}
