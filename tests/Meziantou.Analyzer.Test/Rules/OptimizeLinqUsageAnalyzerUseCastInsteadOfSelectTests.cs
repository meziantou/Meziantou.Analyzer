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
        [InlineData("source.[|Select|](dt => (BaseType)dt)",
                    "source.Cast<BaseType>()")]
        [InlineData("Enumerable.[|Select|](source, dt => (Test.BaseType)dt).FirstOrDefault()",
                    "Enumerable.Cast<Test.BaseType>(source).FirstOrDefault()")]
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
        var source = System.Linq.Enumerable.Empty<DerivedType>();
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
        var source = System.Linq.Enumerable.Empty<DerivedType>();
        {expectedReplacement};
    }}
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("source.Select(dt => dt.Name)")]            // No cast
        [InlineData("source.Select(dt => (object)dt.Name)")]    // Cast of property, not of element itself
        [InlineData("source.Select(dt => dt as BaseType)")]     // 'as' operator -> Could be replaced by OfType<>
        public async Task OptimizeLinq_WhenSelectorDoesNotReturnCastElement_NoDiagnosticReported(string selectInvocation)
        {
            var originalCode = $@"using System.Linq;
class Test
{{
    class BaseType {{ public string Name {{ get; set; }} }}
    class DerivedType : BaseType {{}}

    public Test()
    {{
        var source = System.Linq.Enumerable.Empty<DerivedType>();
        {selectInvocation};
    }}
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ValidateAsync();
        }
    }
}
