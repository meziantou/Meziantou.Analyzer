using Microsoft.CodeAnalysis.CSharp;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStaticLambdaAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseStaticLambdaAnalyzer>()
            .WithCodeFixProvider<UseStaticLambdaFixer>()
            .WithTargetFramework(TargetFramework.Net9_0);
    }

    [Fact]
    public async Task LambdaWithoutCapture()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Generic;
                  class C
                  {
                      void M(List<int> list) => list.Sort([|(x, y) => x.CompareTo(y)|]);
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System.Collections.Generic;
                  class C
                  {
                      void M(List<int> list) => list.Sort(static (x, y) => x.CompareTo(y));
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task SimpleLambdaWithoutCapture()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void M() => _ = (Func<int, int>)([|x => x + 1|]);
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      void M() => _ = (Func<int, int>)(static x => x + 1);
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AnonymousMethodWithoutCapture()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void M() => _ = (Func<int>)([|delegate { return 1; }|]);
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      void M() => _ = (Func<int>)(static delegate { return 1; });
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AsyncLambdaWithoutCapture()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  using System.Threading.Tasks;
                  class C
                  {
                      void M() => _ = (Func<Task<int>>)([|async () => await Task.FromResult(1)|]);
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  using System.Threading.Tasks;
                  class C
                  {
                      void M() => _ = (Func<Task<int>>)(static async () => await Task.FromResult(1));
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultilineLambdaKeepsIndentation()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Generic;
                  class C
                  {
                      void M(List<int> list)
                      {
                          list.Sort(
                              [|(x, y) => x.CompareTo(y)|]);
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System.Collections.Generic;
                  class C
                  {
                      void M(List<int> list)
                      {
                          list.Sort(
                              static (x, y) => x.CompareTo(y));
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LambdaInExpressionTree()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  using System.Linq.Expressions;
                  class C
                  {
                      void M() => _ = (Expression<Func<int, int>>)([|x => x + 1|]);
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  using System.Linq.Expressions;
                  class C
                  {
                      void M() => _ = (Expression<Func<int, int>>)(static x => x + 1);
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LambdaUsingStaticMemberAndConstant()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      const int Constant = 1;
                      static int Field;
                      void M() => _ = (Func<int>)([|() => Field + Constant + Compute()|]);
                      static int Compute() => 0;
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      const int Constant = 1;
                      static int Field;
                      void M() => _ = (Func<int>)(static () => Field + Constant + Compute());
                      static int Compute() => 0;
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LambdaUsingNameofOfLocal()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void M(int parameter) => _ = (Func<string>)([|() => nameof(parameter)|]);
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      void M(int parameter) => _ = (Func<string>)(static () => nameof(parameter));
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NestedLambdaCapturingLocalOfTheOuterLambda()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void M() => _ = (Func<int, Func<int>>)([|x => { var y = x; return () => y; }|]);
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      void M() => _ = (Func<int, Func<int>>)(static x => { var y = x; return () => y; });
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LambdaUsingStaticLocalFunction()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void M()
                      {
                          static int Local() => 1;
                          _ = (Func<int>)([|() => Local()|]);
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      void M()
                      {
                          static int Local() => 1;
                          _ = (Func<int>)(static () => Local());
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LambdaDeclaringItsOwnLocalFunction()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void M() => _ = (Func<int>)([|() => { int Local() => 1; return Local(); }|]);
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      void M() => _ = (Func<int>)(static () => { int Local() => 1; return Local(); });
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AlreadyStatic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Generic;
                  class C
                  {
                      void M(List<int> list) => list.Sort(static (x, y) => x.CompareTo(y));
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CaptureLocalVariable()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void M(int value) => _ = (Func<int>)(() => value);
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CaptureInstanceField()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      int _field;
                      void M() => _ = (Func<int>)(() => _field);
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CaptureThisUsingInstanceMethod()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      int Compute() => 1;
                      void M() => _ = (Func<int>)(() => Compute());
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CapturePrimaryConstructorParameter()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C(int value)
                  {
                      void M() => _ = (Func<int>)(() => value);
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NestedLambdaCapturingLocalOfTheEnclosingMethod()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void M(int value) => _ = (Func<int, Func<int>>)(x => () => value);
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReferenceNonStaticLocalFunction()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void M()
                      {
                          int Local() => 1;
                          _ = (Func<int>)(() => Local());
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReferenceNonStaticLocalFunctionAsMethodGroup()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void M()
                      {
                          int Local() => 1;
                          _ = (Func<Func<int>>)(() => Local);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task QueryExpression()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Generic;
                  using System.Linq;
                  class C
                  {
                      void M(IEnumerable<int> items) => _ = from item in items where item > 0 select item;
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LambdaCapturingRangeVariable()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  using System.Collections.Generic;
                  using System.Linq;
                  class C
                  {
                      void M(IEnumerable<int> items) => _ = from item in items select (Func<int>)(() => item);
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CSharp8()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(LanguageVersion.CSharp8)
              .WithSourceCode("""
                  using System.Collections.Generic;
                  class C
                  {
                      void M(List<int> list) => list.Sort((x, y) => x.CompareTo(y));
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LambdaWithAttribute()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void M() => _ = (Func<int, int>)([|[Obsolete] (int x) => x|]);
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      void M() => _ = (Func<int, int>)([Obsolete] static (int x) => x);
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultipleLambdasWithoutCapture()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void M()
                      {
                          _ = (Func<int>)([|() => 1|]);
                          _ = (Func<int>)([|() => 2|]);
                      }
                  }
                  """)
              .ShouldBatchFixCodeWith("""
                  using System;
                  class C
                  {
                      void M()
                      {
                          _ = (Func<int>)(static () => 1);
                          _ = (Func<int>)(static () => 2);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NestedLambdaInsideCapturingLambda()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void M(int value) => _ = (Func<Func<int>>)(() => value > 0 ? [|() => 1|] : null);
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      void M(int value) => _ = (Func<Func<int>>)(() => value > 0 ? static () => 1 : null);
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task TopLevelStatements()
    {
        await CreateProjectBuilder()
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
              .WithSourceCode("""
                  using System;
                  var value = 1;
                  _ = (Func<int>)([|() => 1|]);
                  _ = (Func<int>)(() => value);
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  var value = 1;
                  _ = (Func<int>)(static () => 1);
                  _ = (Func<int>)(() => value);
                  """)
              .ValidateAsync();
    }

#if CSHARP14_OR_GREATER
    [Fact]
    public async Task CaptureThisUsingFieldKeyword()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      public int P
                      {
                          get
                          {
                              Func<int> f = () => field;
                              return f();
                          }
                      }
                  }
                  """)
              .ValidateAsync();
    }
#endif
}
