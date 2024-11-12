using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotCallVirtualMethodInConstructorAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotCallVirtualMethodInConstructorAnalyzer>();
    }

    [Fact]
    public async Task CtorWithVirtualCall()
    {
        const string SourceCode = @"
class Test
{
    Test()
    {
        [||]A();
    }

    public virtual void A() { }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CtorWithAbstractCall()
    {
        const string SourceCode = @"
abstract class Test
{
    Test()
    {
        [||]A();
    }

    public abstract void A();
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CtorWithNoVirtualCall()
    {
        const string SourceCode = @"
class Test
{
    Test()
    {
        A();
    }

    public void A() { }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CtorWithVirtualCallOnAnotherInstance()
    {
        const string SourceCode = @"
class Test
{
    Test()
    {
        var test = new Test();
        test.A();
    }

    public virtual void A() { }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CtorWithVirtualPropertyAssignment()
    {
        const string SourceCode = @"
class Test
{
    Test()
    {
        [||]A = 10;
    }

    public virtual int A { get; set; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CtorWithVirtualPropertyAssignmentOnAnotherInstance()
    {
        const string SourceCode = @"
class Test
{
    Test()
    {
        var test = new Test();
        test.A = 10;
    }

    public virtual int A { get; set; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CtorWithVirtualPropertyGet()
    {
        const string SourceCode = @"
class Test
{
    Test()
    {
        _ = [||]A;
    }

    public virtual int A => 10;
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CtorWithOverridedMethod()
    {
        const string SourceCode = @"
class Base
{
    public virtual void A() { }
}

class Test : Base
{
    Test()
    {
        [||]A();
    }

    public override void A() { }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CtorWithVirtualPropertyReferenceInNameOf()
    {
        const string SourceCode = @"
class Test
{
    Test()
    {
        _ = nameof(A);
    }

    public virtual int A => 10;
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AssignVirtualEvent()
    {
        const string SourceCode = @"
class Test
{
    protected virtual event System.Action SampleEvent;
    
    Test()
    {
        [||]SampleEvent += A;
    }

    public void A() { }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AssignEvent()
    {
        const string SourceCode = @"
class Test
{
    protected event System.Action SampleEvent;
    
    Test()
    {
        SampleEvent += A;
    }

    public void A() { }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task VirtualDelegate()
    {
        const string SourceCode = @"
class Test
{
    Test()
    {
        System.Action a = A;
    }

    public virtual void A() { }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task VirtualDelegate2()
    {
        const string SourceCode = @"
class Test
{
    Test()
    {
        System.Action a = () => A();
    }

    public virtual void A() { }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CtorWithVirtualGetOnlyPropertyAssignment()
    {
        const string SourceCode = @"
class Test
{
    Test()
    {
        A = 10;
    }

    public virtual int A { get; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
