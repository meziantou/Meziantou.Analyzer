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
                    "source.Cast<BaseType>().FirstOrDefault()")]
        [InlineData("System.Linq.Enumerable.Empty<DerivedType>().[|Select|](dt => (Gen.IList<string>)dt)",
                                "Enumerable.Empty<DerivedType>().Cast<Gen.IList<string>>()")]
        [InlineData("Enumerable.Range(0, 1).[|Select<int, object>|](i => i)",
                    "Enumerable.Range(0, 1).Cast<object>()")]
        [InlineData("source.[|Select|](i => (object?)i)",
                    "source.Cast<object?>()",
                    true)]
        [InlineData("source.[|Select|](i => (object)i)",
                    "source.Cast<object>()",
                    true)]
        [InlineData("source.[|Select<DerivedType, object?>|](i => i)",
                    "source.Cast<object?>()",
                    true)]
        [InlineData("source.[|Select<DerivedType, object>|](i => i)",
                    "source.Cast<object>()",
                    true)]
        public async Task OptimizeLinq_WhenSelectorReturnsCastElement_ReplacesSelectByCast(
            string selectInvocation,
            string expectedReplacement,
            bool enableNullable = false)
        {
            var originalCode = $@"#nullable {(enableNullable ? "enable" : "disable")}
using System.Linq;
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
            var modifiedCode = $@"#nullable {(enableNullable ? "enable" : "disable")}
using System.Linq;
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
        [InlineData("source.Select(dt => dt as BaseType)")]     // 'as' operator should not be replaced by Cast<>
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
