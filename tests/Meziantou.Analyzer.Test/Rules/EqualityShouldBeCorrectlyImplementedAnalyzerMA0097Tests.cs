using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class EqualityShouldBeCorrectlyImplementedAnalyzerMA0097Tests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<EqualityShouldBeCorrectlyImplementedAnalyzer>()
                .WithCodeFixProvider<EqualityShouldBeCorrectlyImplementedFixer>();
        }

        [Fact]
        public async Task BaseClassImplementsOperators()
        {
            var originalCode = @"using System;
abstract class Test : IComparable
{
    public int CompareTo(object other) => 0;
    public override bool Equals(object? obj) => true;
    public override int GetHashCode() => 0;
    public static bool operator <(Test a, Test b) => true;
    public static bool operator <=(Test a, Test b) => true;
    public static bool operator >(Test a, Test b) => true;
    public static bool operator >=(Test a, Test b) => true;
    public static bool operator ==(Test a, Test b) => true;
    public static bool operator !=(Test a, Test b) => true;
}

class InheritedTest : Test // should be ok as the operators are implemented in the base class
{
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ValidateAsync();
        }
    }
}
