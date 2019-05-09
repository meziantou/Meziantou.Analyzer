using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class ClassMustBeSealedAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<ClassMustBeSealedAnalyzer>()
                .WithCodeFixProvider<ClassMustBeSealedFixer>();
        }

        [TestMethod]
        public async Task AbstractClass_NoDiagnostic()
        {
            const string SourceCode = @"
abstract class AbstractClass
{
    static void A() { }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Inherited_Diagnostic()
        {
            const string SourceCode = @"
class Test
{
}

class [|]Test2 : Test
{
}
";

            const string CodeFix = @"
class Test
{
}

sealed class Test2 : Test
{
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task ImplementInterface_Diagnostic()
        {
            const string SourceCode = @"
interface ITest
{
}

class [|]Test : ITest
{
}
";
            const string CodeFix = @"
interface ITest
{
}

sealed class Test : ITest
{
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task StaticMethodAndConstField_Diagnostic()
        {
            const string SourceCode = @"
public class [|]Test
{
    const int a = 10;
    static void A() { }
}";
            const string CodeFix = @"
public sealed class Test
{
    const int a = 10;
    static void A() { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic()
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }
    }
}
