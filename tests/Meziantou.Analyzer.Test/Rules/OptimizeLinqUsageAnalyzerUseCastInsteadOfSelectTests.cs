using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class OptimizeLinqUsageAnalyzerUseCastInsteadOfSelectTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: RuleIdentifiers.OptimizeEnumerable_CastInsteadOfSelect)
                .WithCodeFixProvider<OptimizeLinqUsageFixer>();
        }

        [Theory]
        [InlineData("enumerable.[|Select|](dt => (BaseType)dt)",
                    "enumerable.Cast<BaseType>()")]
        [InlineData("Enumerable.[|Select|](enumerable, dt => dt as Test.BaseType).Where(x => x != null)",
                    "Enumerable.Cast<Test.BaseType>(enumerable).Where(x => x != null)")]
        [InlineData("System.Linq.Enumerable.Empty<DerivedType>().[|Select|](dt => (Gen.IList<string>)dt)",
                                "Enumerable.Empty<DerivedType>().Cast<Gen.IList<string>>()")]
        public async Task OptimizeLinq_WhenSelectorReturnsCastElement_ReplacesSelectByCast(string selectInvocation, string expectedReplacement)
        {
            var originalCode = $@"using System.Linq;
using Gen = System.Collections.Generic;
class Test
{{
    class BaseType {{ public string Name {{ get; set; }} }}
    class DerivedType : BaseType {{}}

    public Test()
    {{
        var enumerable = System.Linq.Enumerable.Empty<DerivedType>();
        {selectInvocation};
    }}
}}";
            var modifiedCode = $@"using System.Linq;
using Gen = System.Collections.Generic;
class Test
{{
    class BaseType {{ public string Name {{ get; set; }} }}
    class DerivedType : BaseType {{}}

    public Test()
    {{
        var enumerable = System.Linq.Enumerable.Empty<DerivedType>();
        {expectedReplacement};
    }}
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("enumerable.Select(dt => dt.Name)")]
        [InlineData("enumerable.Select(dt => (object)dt.Name)")]
        public async Task OptimizeLinq_WhenSelectorDoesNotReturnCastElement_NoDiagnosticReported(string selectInvocation)
        {
            var originalCode = $@"using System.Linq;
class Test
{{
    class BaseType {{ public string Name {{ get; set; }} }}
    class DerivedType : BaseType {{}}

    public Test()
    {{
        var enumerable = System.Linq.Enumerable.Empty<DerivedType>();
        {selectInvocation};
    }}
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ValidateAsync();
        }
    }
}
