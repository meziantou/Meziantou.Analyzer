using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public class DoNotUseZeroToInitializeAnEnumValueTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseZeroToInitializeAnEnumValue>();
        }

        [Fact]
        public async Task Assignation()
        {
            const string SourceCode = @"
enum MyEnum { A = 0, B = 1 }

class Test
{
    void A()
    {
        MyEnum a = [|0|];
        a = [|0|];
        MyEnum b = (MyEnum)0;
        b = (MyEnum)0;
        MyEnum c = MyEnum.A;
        MyEnum d = MyEnum.B;
        MyEnum e = default;
        long f = 0;
        long g = (long)0;
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task MethodInvocation()
        {
            const string SourceCode = @"
enum MyEnum { A = 0, B = 1 }

class Test
{
    void A(MyEnum a)
    {
        A([|0|]);
        A((MyEnum)0);
        A(MyEnum.A);
        A(MyEnum.B);
        A(default);
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task OptionalParameter()
        {
            const string SourceCode = @"
enum MyEnum { A = 0, B = 1 }
class Test
{
    void A(MyEnum a = MyEnum.A)
    {
        A();
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

    }
}
