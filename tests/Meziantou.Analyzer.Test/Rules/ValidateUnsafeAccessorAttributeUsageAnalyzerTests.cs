using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public class ValidateUnsafeAccessorAttributeUsageAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.Net8_0)
            .WithAnalyzer<ValidateUnsafeAccessorAttributeUsageAnalyzer>();
    }

    [Fact]
    public async Task NotExternStaticMethod()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Runtime.CompilerServices;
                  class Sample
                  {
                      [System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod)]
                      void [||]A() { }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LocalFunction_WithoutNameParameter()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Runtime.CompilerServices;
                  class Sample
                  {
                      void A()
                      {
                          // Local function name are mangle by the compiler, so the Name property is required
                          [UnsafeAccessor(UnsafeAccessorKind.Field)]
                          extern static ref int [||]B(System.Version a);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LocalFunction_WithNameProperty()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Runtime.CompilerServices;
                  class Sample
                  {
                      void A()
                      {
                          [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_Major")]
                          extern static ref int B(System.Version a);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Field_TooManyParameters()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Runtime.CompilerServices;
                  class Sample
                  {
                      void A()
                      {
                          [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_Major")]
                          extern static ref int [||]B(System.Version a, int b);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Field_ReturnVoid()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Runtime.CompilerServices;
                  class Sample
                  {
                      void A()
                      {
                          [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_Major")]
                          extern static void [||]B(System.Version a);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Field_DoesNotReturnByRef()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Runtime.CompilerServices;
                  class Sample
                  {
                      void A()
                      {
                          [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_Major")]
                          extern static int [||]B(System.Version a);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Field_FirstParameterNotByRefForStruct()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Runtime.CompilerServices;
                  class Sample
                  {
                      void A()
                      {
                          [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_Major")]
                          extern static ref int [||]B(System.Int32 a);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Field_FirstParameterByRefForStruct()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Runtime.CompilerServices;
                  class Sample
                  {
                      void A()
                      {
                          [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_Major")]
                          extern static ref int B(ref System.Int32 a);
                      }
                  }
                  """)
              .ValidateAsync();
    }
}
