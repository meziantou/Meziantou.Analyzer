using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotUseImplicitCultureSensitiveToStringAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseImplicitCultureSensitiveToStringAnalyzer>();
        }

        [Theory]
        [InlineData("\"\"", "-1")]
        [InlineData("\"abc\"", "-1")]
        [InlineData("\"abc\"", "(sbyte)-1")]
        [InlineData("\"abc\"", "(short)-1")]
        [InlineData("\"abc\"", "(int)-1")]
        [InlineData("\"abc\"", "(long)-1")]
        [InlineData("\"abc\"", "(float)-1")]
        [InlineData("\"abc\"", "(double)-1")]
        [InlineData("\"abc\"", "(decimal)-1")]
        [InlineData("\"abc\"", "default(System.DateTime)")]
        [InlineData("\"abc\"", "default(System.DateTimeOffset)")]
        [InlineData("\"abc\"", "default(System.FormattableString)")]
        [InlineData("\"abc\"", "default(System.Numerics.BigInteger)")]
        [InlineData("\"abc\"", "default(System.Numerics.Complex)")]
        [InlineData("\"abc\"", "default(System.Numerics.Vector2)")]
        [InlineData("\"abc\"", "default(System.Numerics.Vector3)")]
        [InlineData("\"abc\"", "default(System.Numerics.Vector4)")]
        [InlineData("\"abc\"", "default(System.Numerics.Vector<int>)")]
        public async Task ConcatDiagnostic(string left, string right)
        {
            var sourceCode = @"
class Test
{
    void A() { _ = " + left + " + [|" + right + @"|]; }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();

            var invertedSourceCode = @"
class Test
{
    void A() { _ = [|" + right + "|] + " + left + @"; }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(invertedSourceCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("\"abc\"", "'d'")]
        [InlineData("\"abc\"", "\"def\"")]
        [InlineData("\"abc\"", "(byte)1")]
        [InlineData("\"abc\"", "(ushort)1")]
        [InlineData("\"abc\"", "1u")]
        [InlineData("\"abc\"", "1ul")]
        [InlineData("\"abc\"", "(sbyte)1")]
        [InlineData("\"abc\"", "(short)1")]
        [InlineData("\"abc\"", "1")]
        [InlineData("\"abc\"", "1L")]
        [InlineData("\"abc\"", "1f")]
        [InlineData("\"abc\"", "1d")]
        [InlineData("\"abc\"", "1m")]
        [InlineData("\"abc\"", "new System.Guid()")]
        [InlineData("\"abc\"", "new System.Uri(\"\")")]
        [InlineData("\"abc\"", "new System.TimeSpan()")]
        [InlineData("\"abc\"", @"$""test{new System.Uri("""")}""")]
        public async Task ConcatNoDiagnostic(string left, string right)
        {
            var sourceCode = @"
class Test
{
    void A() { _ = " + left + " + " + right + @"; }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();

            var invertedSourceCode = @"
class Test
{
    void A() { _ = " + right + " + " + left + @"; }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(invertedSourceCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("abc[|{(sbyte)-1}|]")]
        [InlineData("abc[|{(short)-1}|]")]
        [InlineData("abc[|{(int)-1}|]")]
        [InlineData("abc[|{(long)-1}|]")]
        [InlineData("abc[|{(float)-1}|]")]
        [InlineData("abc[|{(double)-1}|]")]
        [InlineData("abc[|{(decimal)-1}|]")]
        [InlineData(@"test[|{new int[0].Min()}|]")]
        public async Task InterpolatedStringDiagnostic(string content)
        {
            var sourceCode = @"using System.Linq;
class Test
{
    void A() { string str = $""" + content + @"""; }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("abc{\"def\"}")]
        [InlineData("abc{'a'}")]
        [InlineData("abc{(byte)1}")]
        [InlineData("abc{(ushort)1}")]
        [InlineData("abc{(uint)1}")]
        [InlineData("abc{(ulong)1}")]
        [InlineData(@"test{new System.Uri("""")}")]
        [InlineData(@"test{new int[0].Length}")]
        [InlineData(@"test{new int[0].Count()}")]
        [InlineData(@"test{new System.Collections.Generic.List<int>().Count}")]
        public async Task InterpolatedStringNoDiagnostic(string content)
        {
            var sourceCode = @"using System.Linq;
class Test
{
    void A() { string str = $""" + content + @"""; }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task FormattableString()
        {
            var sourceCode = @"
class Test
{
    void A() { System.FormattableString a = $""abc{-1}""; }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task FormattableString_Invariant()
        {
            var sourceCode = @"
class Test
{
    void A() { string a = System.FormattableString.Invariant($""abc{1}""); }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task StringConcatFormattableString()
        {
            var sourceCode = @"
class Test
{
    void A() { var a = ""abc"" + $""[|{-1}|]""; }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }
    }
}
