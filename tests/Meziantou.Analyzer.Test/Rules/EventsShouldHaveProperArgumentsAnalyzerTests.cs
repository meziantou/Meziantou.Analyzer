using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EventsShouldHaveProperArgumentsAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<EventsShouldHaveProperArgumentsAnalyzer>();
    }

    [Fact]
    public async Task InvalidArguments_InstanceEvent_ConditionalAccess()
    {
        const string SourceCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    MyEvent?.Invoke([|null|], EventArgs.Empty);
                }
            }
            """;
        const string Fix = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    MyEvent?.Invoke(this, EventArgs.Empty);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithCodeFixProvider<EventsShouldHaveProperArgumentsFixer>()
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ValidArguments_InstanceEvent()
    {
        const string SourceCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    MyEvent.Invoke(this, EventArgs.Empty);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InvalidSender_Instance()
    {
        const string SourceCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    MyEvent.Invoke([|null|], EventArgs.Empty);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InvalidSender_Static()
    {
        const string SourceCode = """
            using System;
            class Test
            {
                public static event EventHandler MyEvent;
            
                void OnEvent()
                {
                    MyEvent.Invoke([|this|], EventArgs.Empty);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InvalidEventArgs()
    {
        const string SourceCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    MyEvent.Invoke(this, [|null|]);
                }
            }
            """;
        const string Fix = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    MyEvent.Invoke(this, EventArgs.Empty);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithCodeFixProvider<UseEventArgsEmptyFixer>()
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task InvalidEventArgs_NamedArgument()
    {
        const string SourceCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    MyEvent.Invoke(this, e: [|null|]);
                }
            }
            """;
        const string Fix = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    MyEvent.Invoke(this, e: EventArgs.Empty);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithCodeFixProvider<UseEventArgsEmptyFixer>()
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task EventIsStoredInVariable()
    {
        const string SourceCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    var ev = MyEvent;
                    if (ev != null)
                    {
                        ev.Invoke(this, [|null|]);
                    }
                }
            }
            """;
        const string Fix = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    var ev = MyEvent;
                    if (ev != null)
                    {
                        ev.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithCodeFixProvider<UseEventArgsEmptyFixer>()
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task EventIsStoredInVariableInVariable()
    {
        const string SourceCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    var a = MyEvent;
                    var ev = a;
                    if (ev != null)
                    {
                        ev.Invoke(this, [|null|]);
                    }
                }
            }
            """;
        const string Fix = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    var a = MyEvent;
                    var ev = a;
                    if (ev != null)
                    {
                        ev.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithCodeFixProvider<UseEventArgsEmptyFixer>()
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task EventIsStoredInVariableAndConditionalAccess()
    {
        const string SourceCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    var ev = MyEvent;
                    ev?.Invoke(this, [|null|]);
                }
            }
            """;
        const string Fix = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;
            
                void OnEvent()
                {
                    var ev = MyEvent;
                    ev?.Invoke(this, EventArgs.Empty);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithCodeFixProvider<UseEventArgsEmptyFixer>()
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }
}
