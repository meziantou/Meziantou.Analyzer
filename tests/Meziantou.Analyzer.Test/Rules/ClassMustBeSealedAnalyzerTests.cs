using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class ClassMustBeSealedAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<ClassMustBeSealedAnalyzer>()
                .WithCodeFixProvider<ClassMustBeSealedFixer>();
        }

        [Fact]
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

        [Fact]
        public async Task Inherited_Diagnostic()
        {
            const string SourceCode = @"
class Test
{
}

class [||]Test2 : Test
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

        [Fact]
        public async Task ImplementInterface_Diagnostic()
        {
            const string SourceCode = @"
interface ITest
{
}

class [||]Test : ITest
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

        [Fact]
        public async Task StaticMethodAndConstField_NotReported()
        {
            const string SourceCode = @"
public class Test
{
    const int a = 10;
    static void A() { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task StaticMethodAndConstFieldWithEditorConfigTrue_Diagnostic()
        {
            const string SourceCode = @"
public class [||]Test
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
                  .WithEditorConfig("MA0053.public_class_should_be_sealed = true")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task GenericBaseClass()
        {
            const string SourceCode = @"
internal class Base<T>
{
}

internal sealed class Child : Base<int>
{
}";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
