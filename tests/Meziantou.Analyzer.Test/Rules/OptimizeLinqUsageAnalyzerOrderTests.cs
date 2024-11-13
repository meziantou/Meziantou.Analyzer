using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerOrderTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.Net8_0)
            .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: RuleIdentifiers.OptimizeEnumerable_UseOrder)
            .WithCodeFixProvider<OptimizeLinqUsageFixer>();
    }

    [Fact]
    public async Task IEnumerable_Order_net5()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net5_0)
              .WithSourceCode("""
                  using System.Collections.Generic;
                  using System.Linq;
                  class Test
                  {
                      public Test()
                      {
                          IEnumerable<string> query = null;
                          query.OrderBy(x => x);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task IEnumerable_Order_LambdaNotValid()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net5_0)
              .WithSourceCode("""
                  using System.Collections.Generic;
                  using System.Linq;
                  class Test
                  {
                      public Test()
                      {
                          IEnumerable<string> query = null;
                          query.OrderBy(x => true);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task IEnumerable_Order_LambdaReferenceAnotherParameter()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net5_0)
              .WithSourceCode("""
                  using System.Collections.Generic;
                  using System.Linq;
                  class Test
                  {
                      public Test(int a)
                      {
                          IEnumerable<string> query = null;
                          query.OrderBy(x => a);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task IEnumerable_Order()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Generic;
                  using System.Linq;
                  class Test
                  {
                      public Test()
                      {
                          IEnumerable<string> query = null;
                          query.[|OrderBy|](x => x);
                      }
                  }
                  """)
              .ShouldReportDiagnosticWithMessage("Use 'Order' instead of 'OrderBy'")
              .ShouldFixCodeWith("""
                  using System.Collections.Generic;
                  using System.Linq;
                  class Test
                  {
                      public Test()
                      {
                          IEnumerable<string> query = null;
                          query.Order();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task IEnumerable_OrderDescending()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Generic;
                  using System.Linq;
                  class Test
                  {
                      public Test()
                      {
                          IEnumerable<string> query = null;
                          query.[|OrderByDescending|](x => x);
                      }
                  }
                  """)
              .ShouldReportDiagnosticWithMessage("Use 'OrderDescending' instead of 'OrderByDescending'")
              .ShouldFixCodeWith("""
                  using System.Collections.Generic;
                  using System.Linq;
                  class Test
                  {
                      public Test()
                      {
                          IEnumerable<string> query = null;
                          query.OrderDescending();
                      }
                  }
                  """)
              .ValidateAsync();
    }
}
