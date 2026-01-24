using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AvoidUnusedNonPublicMembersAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<AvoidUnusedNonPublicMembersAnalyzer>();
    }

    [Fact]
    public async Task PublicMethod_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                public void PublicMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ProtectedMethod_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                protected void ProtectedMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ProtectedInternalMethod_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                protected internal void ProtectedInternalMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedPrivateMethod_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private void [|UnusedMethod|]() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedInternalMethod_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                internal void [|UnusedMethod|]() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedPrivateProtectedMethod_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private protected void [|UnusedMethod|]() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivateMethod_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private void UsedMethod() { }
                
                public void PublicMethod()
                {
                    UsedMethod();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RecursiveMethod_NotUsedExternally_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int [|Factorial|](int n)
                {
                    if (n <= 1) return 1;
                    return n * Factorial(n - 1);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RecursiveMethod_UsedExternally_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Factorial(int n)
                {
                    if (n <= 1) return 1;
                    return n * Factorial(n - 1);
                }

                public int Compute(int n) => Factorial(n);
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MutuallyRecursiveMethods_NotUsedExternally_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private bool IsEven(int n)
                {
                    if (n == 0) return true;
                    return IsOdd(n - 1);
                }

                private bool IsOdd(int n)
                {
                    if (n == 0) return false;
                    return IsEven(n - 1);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MethodReferenceSelf_NotUsedExternally_ReportsDiagnostic()
    {
        const string SourceCode = """
            using System;

            public class Sample
            {
                private void [|DoWork|]()
                {
                    Action action = DoWork;
                    action();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedPrivateField_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int [|_unusedField|];
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivateField_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _usedField;
                
                public int GetValue() => _usedField;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ConstField_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private const int [|ConstField|] = 42;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedPrivateProperty_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int UnusedProperty { [|get|]; [|set|]; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyInitializer_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int UnusedProperty { [|get|]; [|set|]; } = 0;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyInitializer_UsesPrivateField_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private static int _defaultValue = 42;
                private int UnusedProperty { [|get|]; [|set|]; } = _defaultValue;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyInitializer_UsesPrivateMethod_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int UnusedProperty { [|get|]; [|set|]; } = GetDefaultValue();
                
                private static int GetDefaultValue() => 42;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyInitializer_UsesPrivateConst_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private const int DefaultValue = 42;
                private int UnusedProperty { [|get|]; [|set|]; } = DefaultValue;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyInitializer_UsesPrivateProperty_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private static int DefaultValue => 42;
                private int UnusedProperty { [|get|]; [|set|]; } = DefaultValue;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticPropertyInitializer_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private static int UnusedProperty { [|get|]; [|set|]; } = 0;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GetOnlyPropertyWithInitializer_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int UnusedProperty { [|get|]; } = 0;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivatePropertyGetter_ReportsUnusedSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; [|set|]; }
                
                public int GetValue() => Property;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivatePropertySetter_ReportsUnusedGetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { [|get|]; set; }
                
                public void SetValue(int value) => Property = value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivatePropertyBoth_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public int GetValue() => Property;
                public void SetValue(int value) => Property = value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PublicPropertyWithPrivateSetter_UsedSetter_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                public int Property { get; private set; }
                
                public Sample()
                {
                    Property = 42;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PublicPropertyWithPrivateSetter_UnusedSetter_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                public int Property { get; [|private set|]; }
                
                public Sample()
                {
                    Property = 42;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedPrivateEvent_ReportsDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private event EventHandler [|UnusedEvent|];
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivateEvent_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private event EventHandler UsedEvent;
                
                public void Subscribe(EventHandler handler) => UsedEvent += handler;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedPrivateEventWithExplicitAddRemove_ReportsDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private EventHandler _handler;
                
                private event EventHandler [|UnusedEvent|]
                {
                    add { _handler += value; }
                    remove { _handler -= value; }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivateEventWithExplicitAdd_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private EventHandler _handler;

                // Event must have add and remove accessors, so do report only when both are unused
                private event EventHandler UsedEvent
                {
                    add { _handler += value; }
                    remove { _handler -= value; }
                }
                
                public void Subscribe(EventHandler handler) => UsedEvent += handler;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivateEventWithExplicitRemove_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private EventHandler _handler;
                
                // Event must have add and remove accessors, so do report only when both are unused
                private event EventHandler UsedEvent
                {
                    add { _handler += value; }
                    remove { _handler -= value; }
                }
                
                public void Unsubscribe(EventHandler handler) => UsedEvent -= handler;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivateEventWithExplicitAddRemove_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private EventHandler _handler;
                
                private event EventHandler UsedEvent
                {
                    add { _handler += value; }
                    remove { _handler -= value; }
                }
                
                public void Subscribe(EventHandler handler) => UsedEvent += handler;
                public void Unsubscribe(EventHandler handler) => UsedEvent -= handler;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticConstructor_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                static Sample() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Finalizer_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                ~Sample() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PublicOverriddenMethod_NoDiagnostic()
    {
        const string SourceCode = """
            public class Base
            {
                public virtual void Method() { }
            }
            
            internal class Derived : Base
            {
                public override void Method() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalInterfaceImplementation_NoDiagnostic()
    {
        const string SourceCode = """
            internal interface IService
            {
                void [|DoWork|]();
            }
            
            internal class ServiceImpl : IService
            {
                public void DoWork() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalInterfaceImplementation_MemberUsed_NoDiagnostic()
    {
        const string SourceCode = """
            internal interface IService
            {
                void DoWork();
            }
            
            internal class ServiceImpl : IService
            {
                public void DoWork() { }
            }
            
            public class Consumer
            {
                public void Use(IService service)
                {
                    service.DoWork();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalInterfaceImplementation_ImplementationMemberUsedDirectly_ReportsDiagnostic()
    {
        const string SourceCode = """
            internal interface IService
            {
                void [|DoWork()|];
            }
            
            internal class ServiceImpl : IService
            {
                public void DoWork() { }
            }
            
            public class Consumer
            {
                public void Use(ServiceImpl service)
                {
                    service.DoWork();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalInterfaceExplicitImplementation_Diagnostic()
    {
        const string SourceCode = """
            internal interface IService
            {
                void [|DoWork|]();
            }
            
            internal class ServiceImpl : IService
            {
                void IService.DoWork() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PublicInterfaceImplementation_NoDiagnostic()
    {
        const string SourceCode = """
            public interface ISample
            {
                bool IsSupported();
                bool Value { get; }
                event System.EventHandler Changed;
            }
            
            internal sealed class Sample : ISample
            {
                public bool IsSupported() => throw null;
                public bool Value => throw null;
                public event System.EventHandler Changed;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitInterfaceImplementation_NoDiagnostic()
    {
        const string SourceCode = """
            public interface IService
            {
                void DoWork();
            }
            
            internal class ServiceImpl : IService
            {
                void IService.DoWork() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnmanagedCallersOnlyMethod_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Runtime.InteropServices;
            
            public class Sample
            {
                [UnmanagedCallersOnly]
                private static void NativeCallback() { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnitTestMethod_XUnit_NoDiagnostic()
    {
        const string SourceCode = """
            using Xunit;
            
            public class TestClass
            {
                [Fact]
                private void TestMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .AddXUnitApi()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnitTestMethod_NUnit_NoDiagnostic()
    {
        const string SourceCode = """
            using NUnit.Framework;
            
            [TestFixture]
            public class TestClass
            {
                [Test]
                private void TestMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .AddNUnitApi()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnitTestMethod_MSTest_NoDiagnostic()
    {
        const string SourceCode = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            
            [TestClass]
            public class TestClass
            {
                [TestMethod]
                private void TestMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .AddMSTestApi()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ComImportClass_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            using System.Runtime.InteropServices;
            
            [ComImport]
            [Guid("00000000-0000-0000-0000-000000000000")]
            internal interface IComInterface
            {
                void UnusedMethod();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ComVisibleClass_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Runtime.InteropServices;
            
            [ComVisible(true)]
            internal class ComVisibleClass
            {
                private void UnusedMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ComRegisterFunctionAttribute_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Runtime.InteropServices;
            
            public class ComServer
            {
                [ComRegisterFunction]
                private static void RegisterFunction(System.Type type) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ComUnregisterFunctionAttribute_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Runtime.InteropServices;
            
            public class ComServer
            {
                [ComUnregisterFunction]
                private static void UnregisterFunction(System.Type type) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    // nameof is a usage because it's often use for reflection-safe member access
    [Fact]
    public async Task Nameof_Method_NotConsideredUsage()
    {
        const string SourceCode = """
            public class Sample
            {
                private void UnusedMethod() { }
                
                public string GetName() => nameof(UnusedMethod);
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Nameof_Field_NotConsideredUsage()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _f;
                
                public string GetName() => nameof(_f);
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Nameof_Property_NotConsideredUsage()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Prop { get; set; }
                
                public string GetName() => nameof(Prop);
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_MembersAreUsed()
    {
        const string SourceCode = """
            using System.Diagnostics;
            
            [DebuggerDisplay("{DebugValue}")]
            public class Sample
            {
                private string DebugValue => "test";
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_MethodCall()
    {
        const string SourceCode = """
            using System.Diagnostics;
            
            [DebuggerDisplay("{GetDebugValue()}")]
            public class Sample
            {
                private string GetDebugValue() => "test";
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_NameProperty()
    {
        const string SourceCode = """
            using System.Diagnostics;
            
            [DebuggerDisplay("Value", Name = "{DisplayName}")]
            public class Sample
            {
                private string DisplayName => "test";
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_TypeProperty()
    {
        const string SourceCode = """
            using System.Diagnostics;
            
            [DebuggerDisplay("{Value}", Type = "{TypeName}")]
            public class Sample
            {
                private string TypeName => "CustomType";
                private int Value => 42;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_WithFormatting()
    {
        const string SourceCode = """
            using System.Diagnostics;
            
            [DebuggerDisplay("{Value,nq}")]
            public class Sample
            {
                private string Value => "test";
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_ComplexExpression()
    {
        const string SourceCode = """
            using System.Diagnostics;
            
            [DebuggerDisplay("{Value + 1}")]
            public class Sample
            {
                private int Value => 42;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_NestedMemberAccess()
    {
        const string SourceCode = """
            using System.Diagnostics;
            
            [DebuggerDisplay("{Child.Value}")]
            public class Sample
            {
                private Child Child => new();
            }
            
            public class Child
            {
                public int Value => 42;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    // DynamicallyAccessedMembersAttribute tests
    //
    // DynamicallyAccessedMembersAttribute is used to annotate parameters, fields, properties, or return values
    // of type System.Type (or string representing a type name) to indicate that the code may access members
    // of that type via reflection at runtime.
    //
    // Key behaviors:
    // 1. When applied to a TYPE: Members matching the specified flags should be considered potentially used
    //    (e.g., [DynamicallyAccessedMembers(All)] on a class means all members may be accessed via reflection)
    // 2. When applied to a Type PARAMETER/FIELD/PROPERTY/RETURN VALUE: This is a contract annotation for the linker
    //    It does NOT automatically mark members of unrelated types as used - it only affects types that flow
    //    through that annotated location at runtime.
    //
    // For this analyzer, we focus on case 1: when the attribute is applied directly to a type declaration,
    // members of that type matching the specified flags should not be reported as unused.

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnType_AllMembers_NoDiagnostic()
    {
        // When DynamicallyAccessedMembersAttribute with All is applied to a type,
        // all members of that type may be accessed via reflection, so none should be reported as unused.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            internal class Sample
            {
                private void PrivateMethod() { }
                private int _privateField;
                private int PrivateProperty { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnType_NonPublicMethods_NoDiagnostic()
    {
        // When NonPublicMethods is specified, private/internal methods should not be reported as unused.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
            internal class Sample
            {
                private void PrivateMethod() { }
                internal void InternalMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnType_NonPublicFields_NoDiagnostic()
    {
        // When NonPublicFields is specified, private/internal fields should not be reported as unused.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
            internal class Sample
            {
                private int _privateField;
                internal int _internalField;
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnType_NonPublicProperties_NoDiagnostic()
    {
        // When NonPublicProperties is specified, private/internal properties should not be reported as unused.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicProperties)]
            internal class Sample
            {
                private int PrivateProperty { get; set; }
                internal int InternalProperty { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnType_NonPublicConstructors_NoDiagnostic()
    {
        // When NonPublicConstructors is specified, private/internal constructors should not be reported as unused.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            internal class Sample
            {
                private Sample() { }
                internal Sample(int value) { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnType_NonPublicEvents_NoDiagnostic()
    {
        // When NonPublicEvents is specified, private/internal events should not be reported as unused.
        const string SourceCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicEvents)]
            internal class Sample
            {
                private event EventHandler PrivateEvent;
                internal event EventHandler InternalEvent;
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnType_PublicMethods_ReportsPrivateMethod()
    {
        // When only PublicMethods is specified, private methods are NOT covered and should be reported.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            internal class Sample
            {
                private void [|PrivateMethod|]() { }
                public void PublicMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnType_CombinedFlags_NoDiagnostic()
    {
        // Combined flags should preserve all matching members.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.NonPublicFields)]
            internal class Sample
            {
                private void PrivateMethod() { }
                private int _privateField;
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnTypeParameter_FlowsToType_NoDiagnostic()
    {
        // When a Type is passed to a method with DynamicallyAccessedMembers on the parameter,
        // the members of that specific type matching the flags should be considered used.
        const string SourceCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            public class Factory
            {
                public static object? Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
                {
                    return Activator.CreateInstance(type, nonPublic: true);
                }
            }
            
            internal class Sample
            {
                private Sample() { }
            }
            
            public class Consumer
            {
                public void Use()
                {
                    // typeof(Sample) flows to the annotated parameter, so Sample's private constructor is used
                    Factory.Create(typeof(Sample));
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnGenericTypeParameter_FlowsToType_NoDiagnostic()
    {
        // When a type is used as a generic argument with DynamicallyAccessedMembers constraint,
        // the members of that type matching the flags should be considered used.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            public class Factory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)] T> where T : new()
            {
                public T Create() => new T();
            }
            
            internal class Sample
            {
                internal Sample() { }
            }
            
            public class Consumer
            {
                public void Use()
                {
                    // Sample is used as generic argument, so Sample's constructors matching the flags are used
                    var factory = new Factory<Sample>();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnField_TypeFlows_NoDiagnostic()
    {
        // When a Type field is annotated with DynamicallyAccessedMembers and a specific type is assigned,
        // the members of that type matching the flags should be considered used.
        const string SourceCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            public class TypeHolder
            {
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
                public Type HeldType;
            }
            
            internal class Sample
            {
                private void PrivateMethod() { }
            }
            
            public class Consumer
            {
                public void Use()
                {
                    var holder = new TypeHolder();
                    holder.HeldType = typeof(Sample);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnProperty_TypeFlows_NoDiagnostic()
    {
        // When a Type property is annotated with DynamicallyAccessedMembers,
        // types assigned to it have their matching members considered used.
        const string SourceCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            public class TypeHolder
            {
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicProperties)]
                public Type HeldType { get; set; }
            }
            
            internal class Sample
            {
                private int PrivateProperty { get; set; }
            }
            
            public class Consumer
            {
                public void Use()
                {
                    var holder = new TypeHolder { HeldType = typeof(Sample) };
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    // TODO intermethod - dataflow analysis (JsonSerializer.Serialize<T>(data))
    // https://github.com/dotnet/runtime/blob/main/src/tools/illink/src/ILLink.RoslynAnalyzer/DataFlow/BasicBlockExtensions.cs

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnReturnValue_TypeFlows_NoDiagnostic()
    {
        // When a method has DynamicallyAccessedMembers on return value,
        // the returned type's matching members should be considered used.
        const string SourceCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            public class TypeProvider
            {
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
                public Type GetType() => typeof(Sample);
            }
            
            internal class Sample
            {
                private void PrivateMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnPropertySetterParameter_TypeFlows_NoDiagnostic()
    {
        // When a property setter has [param: DynamicallyAccessedMembers],
        // types assigned to the property have their matching members considered used.
        const string SourceCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            public class TypeHolder
            {
                private Type _type;
                
                public Type HeldType
                {
                    get => _type;
                    [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
                    set => _type = value;
                }
            }
            
            internal class Sample
            {
                private void PrivateMethod() { }
            }
            
            public class Consumer
            {
                public void Use()
                {
                    var holder = new TypeHolder();
                    holder.HeldType = typeof(Sample);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnIndexerSetterParameter_TypeFlows_NoDiagnostic()
    {
        // When an indexer setter has [param: DynamicallyAccessedMembers],
        // types assigned via the indexer have their matching members considered used.
        const string SourceCode = """
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            
            public class TypeRegistry
            {
                private Dictionary<string, Type> _types = new();
                
                public Type this[string key]
                {
                    get => _types[key];
                    [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
                    set => _types[key] = value;
                }
            }
            
            internal class Sample
            {
                private Sample() { }
            }
            
            public class Consumer
            {
                public void Use()
                {
                    var registry = new TypeRegistry();
                    registry["sample"] = typeof(Sample);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnPropertyGetterReturn_TypeFlows_NoDiagnostic()
    {
        // When a property getter has [return: DynamicallyAccessedMembers],
        // the returned type's matching members should be considered used.
        const string SourceCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            public class TypeProvider
            {
                [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
                public Type ProvidedType => typeof(Sample);
            }
            
            internal class Sample
            {
                private int _privateField;
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnNestedType_NoDiagnostic()
    {
        // Nested types with DynamicallyAccessedMembers should also have their members preserved.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            public class Outer
            {
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
                private class Inner
                {
                    private void PrivateMethod() { }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_None_ReportsDiagnostic()
    {
        // When None is specified, no members are preserved by this attribute.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.None)]
            internal class Sample
            {
                private void [|PrivateMethod|]() { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_NonPublicNestedTypes_NoDiagnostic()
    {
        // When NonPublicNestedTypes is specified, private/internal nested types should not be reported.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
            internal class Sample
            {
                private class PrivateNested { }
                internal class InternalNested { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    // Positive and negative match tests - verifying that flags only preserve matching member types

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_NonPublicMethods_ReportsField()
    {
        // NonPublicMethods does NOT preserve fields - field should be reported.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
            internal class Sample
            {
                private void PrivateMethod() { }
                private int [|_privateField|];
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_NonPublicFields_ReportsMethod()
    {
        // NonPublicFields does NOT preserve methods - method should be reported.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
            internal class Sample
            {
                private void [|PrivateMethod|]() { }
                private int _privateField;
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_NonPublicProperties_ReportsMethodAndField()
    {
        // NonPublicProperties does NOT preserve methods or fields - both should be reported.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicProperties)]
            internal class Sample
            {
                private void [|PrivateMethod|]() { }
                private int [|_privateField|];
                private int PrivateProperty { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_NonPublicConstructors_ReportsMethodAndField()
    {
        // NonPublicConstructors does NOT preserve methods or fields - both should be reported.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            internal class Sample
            {
                private Sample() { }
                private void [|PrivateMethod|]() { }
                private int [|_privateField|];
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_NonPublicEvents_ReportsMethodAndField()
    {
        // NonPublicEvents does NOT preserve methods or fields - both should be reported.
        const string SourceCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicEvents)]
            internal class Sample
            {
                private event EventHandler PrivateEvent;
                private void [|PrivateMethod|]() { }
                private int [|_privateField|];
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_NonPublicNestedTypes_ReportsMethodAndField()
    {
        // NonPublicNestedTypes does NOT preserve methods or fields - both should be reported.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
            internal class Sample
            {
                private class PrivateNested { }
                private void [|PrivateMethod|]() { }
                private int [|_privateField|];
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_PublicFields_ReportsPrivateField()
    {
        // PublicFields does NOT preserve private fields - private field should be reported.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
            internal class Sample
            {
                public int PublicField;
                private int [|_privateField|];
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_PublicProperties_ReportsPrivateProperty()
    {
        // PublicProperties does NOT preserve private properties - private property should be reported.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
            internal class Sample
            {
                public int PublicProperty { get; set; }
                private int PrivateProperty { [|get;|] [|set;|] }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_PublicConstructors_ReportsPrivateConstructor()
    {
        // PublicConstructors does NOT preserve private constructors - private constructor should be reported.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            internal class Sample
            {
                public Sample() { }
                private [|Sample(int value)|] { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_PublicEvents_ReportsPrivateEvent()
    {
        // PublicEvents does NOT preserve private events - private event should be reported.
        const string SourceCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
            internal class Sample
            {
                public event EventHandler PublicEvent;
                private event EventHandler [|PrivateEvent|];
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_PublicNestedTypes_ReportsPrivateNestedType()
    {
        // PublicNestedTypes does NOT preserve private nested types - private nested type should be reported.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes)]
            internal class Sample
            {
                public class PublicNested { }
                private class [|PrivateNested|] { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_CombinedFlags_ReportsNonMatchingMembers()
    {
        // Combined flags should preserve only matching members - non-matching should be reported.
        const string SourceCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.NonPublicFields)]
            internal class Sample
            {
                private void PrivateMethod() { }
                private int _privateField;
                private int PrivateProperty { [|get;|] [|set;|] }
                private event EventHandler [|PrivateEvent|];
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_InternalMethod_WithNonPublicMethods_NoDiagnostic()
    {
        // NonPublicMethods includes internal methods (not just private).
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
            internal class Sample
            {
                private void PrivateMethod() { }
                internal void InternalMethod() { }
                private protected void PrivateProtectedMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_InternalField_WithNonPublicFields_NoDiagnostic()
    {
        // NonPublicFields includes internal fields (not just private).
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
            internal class Sample
            {
                private int _privateField;
                internal int _internalField;
                private protected int _privateProtectedField;
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_StaticMembers_WithNonPublicMethods_NoDiagnostic()
    {
        // NonPublicMethods includes static methods.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
            internal class Sample
            {
                private static void PrivateStaticMethod() { }
                private void PrivateInstanceMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_StaticFields_WithNonPublicFields_NoDiagnostic()
    {
        // NonPublicFields includes static fields.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
            internal class Sample
            {
                private static int _privateStaticField;
                private int _privateInstanceField;
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_WithoutAttribute_ReportsDiagnostic()
    {
        // Without the attribute, unused members should be reported.
        const string SourceCode = """
            internal class Sample
            {
                private void [|PrivateMethod|]() { }
                private int [|_privateField|];
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_Interfaces_NoDiagnostic()
    {
        // Interfaces flag should preserve interfaces implemented by the type (not directly testable for unused members).
        // This test ensures no false positives when Interfaces is specified.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
            internal class Sample
            {
                private void [|PrivateMethod|]() { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_AllPublicMembers_ReportsPrivateMembers()
    {
        // PublicMethods | PublicFields | PublicProperties | PublicConstructors | PublicEvents | PublicNestedTypes
        // does NOT preserve private members.
        const string SourceCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.PublicFields |
                DynamicallyAccessedMemberTypes.PublicProperties |
                DynamicallyAccessedMemberTypes.PublicConstructors |
                DynamicallyAccessedMemberTypes.PublicEvents |
                DynamicallyAccessedMemberTypes.PublicNestedTypes)]
            internal class Sample
            {
                public void PublicMethod() { }
                public int PublicField;
                public int PublicProperty { get; set; }
                public Sample() { }
                public event EventHandler PublicEvent;
                public class PublicNested { }
                
                private void [|PrivateMethod|]() { }
                private int [|_privateField|];
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_AllNonPublicMembers_NoDiagnostic()
    {
        // All non-public flags combined should preserve all non-public members.
        const string SourceCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.NonPublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicFields |
                DynamicallyAccessedMemberTypes.NonPublicProperties |
                DynamicallyAccessedMemberTypes.NonPublicConstructors |
                DynamicallyAccessedMemberTypes.NonPublicEvents |
                DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
            internal class Sample
            {
                private Sample(int value) { }
                private void PrivateMethod() { }
                private int _privateField;
                private int PrivateProperty { get; set; }
                private event EventHandler PrivateEvent;
                private class PrivateNested { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_InheritedFromBaseClass_NoDiagnostic()
    {
        // When base class has DynamicallyAccessedMembers, derived class members may still be reported
        // unless they override/implement something from the base.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
            internal class BaseClass
            {
                private void BasePrivateMethod() { }
            }
            
            internal class DerivedClass : BaseClass
            {
                private void [|DerivedPrivateMethod|]() { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_OnDerivedClass_NoDiagnostic()
    {
        // When derived class has DynamicallyAccessedMembers, its members should be preserved.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            internal class BaseClass
            {
                private void [|BasePrivateMethod|]() { }
            }
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
            internal class DerivedClass : BaseClass
            {
                private void DerivedPrivateMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_ConstField_WithNonPublicFields_NoDiagnostic()
    {
        // NonPublicFields should include const fields.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
            internal class Sample
            {
                private const int PrivateConst = 42;
                private readonly int _privateReadonly = 1;
                private static readonly int _privateStaticReadonly = 2;
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_PropertyAccessors_WithNonPublicProperties_NoDiagnostic()
    {
        // NonPublicProperties should preserve both getter and setter of private properties.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicProperties)]
            internal class Sample
            {
                private int GetOnlyProperty { get; }
                private int SetOnlyProperty { set { } }
                private int GetSetProperty { get; set; }
                private int InitProperty { get; init; }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_Indexer_WithNonPublicProperties_NoDiagnostic()
    {
        // NonPublicProperties should include indexers.
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicProperties)]
            internal class Sample
            {
                private int this[int index] => index;
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_Indexer_WithNonPublicMethods_ReportsDiagnostic()
    {
        // NonPublicMethods does NOT include indexers (they are properties).
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
            internal class Sample
            {
                private int this[int index] => [|index|];
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicallyAccessedMembersAttribute_ExplicitEventAccessors_WithNonPublicEvents_NoDiagnostic()
    {
        // NonPublicEvents should preserve events with explicit add/remove accessors.
        const string SourceCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicEvents)]
            internal class Sample
            {
                private EventHandler _handler;
                
                private event EventHandler PrivateEvent
                {
                    add { _handler += value; }
                    remove { _handler -= value; }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task JsonPropertyNameAttribute_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Text.Json.Serialization;
            
            public class Sample
            {
                [JsonPropertyName("value")]
                private int Value { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task JsonIncludeAttribute_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Text.Json.Serialization;
            
            public class Sample
            {
                [JsonInclude]
                private int Value { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task JsonIgnoreAttribute_ReportsDiagnostic()
    {
        const string SourceCode = """
            using System.Text.Json.Serialization;
            
            public class Sample
            {
                [JsonIgnore]
                private int [|Value|] { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task XmlElementAttribute_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Xml.Serialization;
            
            public class Sample
            {
                [XmlElement]
                private int Value { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NewtonsoftJsonPropertyAttribute_NoDiagnostic()
    {
        const string SourceCode = """
            using Newtonsoft.Json;
            
            public class Sample
            {
                [JsonProperty]
                private int Value { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .AddNuGetReference("Newtonsoft.Json", "13.0.1", "lib/netstandard2.0/")
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NewtonsoftJsonPropertyAttribute_WithName_NoDiagnostic()
    {
        const string SourceCode = """
            using Newtonsoft.Json;
            
            public class Sample
            {
                [JsonProperty("value")]
                private int Value { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .AddNuGetReference("Newtonsoft.Json", "13.0.1", "lib/netstandard2.0/")
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DataMemberAttribute_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Runtime.Serialization;
            
            [DataContract]
            public class Sample
            {
                [DataMember]
                private int Value { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DeconstuctMethod_Unused_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Point
            {
                public int X { get; }
                public int Y { get; }
                
                public Point(int x, int y) => (X, Y) = (x, y);
                
                private void [|Deconstruct|](out int x, out int y)
                {
                    x = X;
                    y = Y;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GetEnumerator_Unused_ReportsDiagnostic()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            
            public class CustomCollection
            {
                private List<int> _items = new();
                
                private IEnumerator<int> [|GetEnumerator|]() => _items.GetEnumerator();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GetAwaiter_Unused_ReportsDiagnostic()
    {
        const string SourceCode = """
            using System.Runtime.CompilerServices;
            
            public class CustomAwaitable
            {
                private CustomAwaiter [|GetAwaiter|]() => new CustomAwaiter();
            }
            
            public class CustomAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public void GetResult() { }
                public void OnCompleted(System.Action continuation) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GetAwaiter_UsedInAwait_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            
            internal class CustomAwaitable
            {
                internal CustomAwaiter GetAwaiter() => new CustomAwaiter();
            }
            
            internal class CustomAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public void GetResult() { }
                public void OnCompleted(Action continuation) { }
            }
            
            public class Consumer
            {
                public async Task UseAsync()
                {
                    await new CustomAwaitable();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DisposeMethod_Unused_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Resource
            {
                private void [|Dispose|]() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DisposeMethod_UsedInUsing_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            internal class Resource : IDisposable
            {
                internal void Dispose() { }
                void IDisposable.Dispose() => Dispose();
            }
            
            public class Consumer
            {
                public void Use()
                {
                    using (var r = new Resource())
                    {
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DisposeAsyncMethod_Unused_ReportsDiagnostic()
    {
        const string SourceCode = """
            using System.Threading.Tasks;
            
            public class Resource
            {
                private ValueTask [|DisposeAsync|]() => ValueTask.CompletedTask;
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DisposeAsyncMethod_UsedInAwaitUsing_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            using System.Threading.Tasks;
            
            internal class Resource : IAsyncDisposable
            {
                internal ValueTask DisposeAsync() => ValueTask.CompletedTask;
                ValueTask IAsyncDisposable.DisposeAsync() => DisposeAsync();
            }
            
            public class Consumer
            {
                public async Task UseAsync()
                {
                    await using (var r = new Resource())
                    {
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExpressionTree_PropertyUsage()
    {
        const string SourceCode = """
            using System;
            using System.Linq.Expressions;
            
            public class Sample
            {
                private int Value { get; }
                
                public Expression<Func<Sample, int>> GetExpression()
                {
                    return s => s.Value;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExpressionTree_MethodUsage()
    {
        const string SourceCode = """
            using System;
            using System.Linq.Expressions;
            
            public class Sample
            {
                private int GetValue() => 42;
                
                public Expression<Func<Sample, int>> GetExpression()
                {
                    return s => s.GetValue();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReflectionUsage_TypeOfGetMethod()
    {
        const string SourceCode = """
            using System;
            using System.Reflection;
            
            public class Sample
            {
                private void PrivateMethod() { }
                
                public MethodInfo GetMethod()
                {
                    return typeof(Sample).GetMethod("PrivateMethod");
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReflectionUsage_TypeOfGetProperty()
    {
        const string SourceCode = """
            using System;
            using System.Reflection;
            
            public class Sample
            {
                private int PrivateProperty { get; set; }
                
                public PropertyInfo GetProperty()
                {
                    return typeof(Sample).GetProperty("PrivateProperty");
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReflectionUsage_TypeOfGetField()
    {
        const string SourceCode = """
            using System;
            using System.Reflection;
            
            public class Sample
            {
                private int _privateField;
                
                public FieldInfo GetField()
                {
                    return typeof(Sample).GetField("_privateField");
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedInForEach_MarkIteratorMethodsAsUsed()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            
            public class CustomCollection
            {
                private List<int> _items = new();
                
                public IEnumerator<int> GetEnumerator() => _items.GetEnumerator();
                
                public void Iterate()
                {
                    foreach (var item in this)
                    {
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedInUsing_MarkDisposeAsUsed()
    {
        const string SourceCode = """
            using System;
            
            public class Resource : IDisposable
            {
                private void Dispose() { }
                void IDisposable.Dispose() => Dispose();
            }
            
            public class Consumer
            {
                public void Use()
                {
                    using (var r = new Resource())
                    {
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedInUsingDeclaration_MarkDisposeAsUsed()
    {
        const string SourceCode = """
            using System;
            
            internal class Resource : IDisposable
            {
                internal void Dispose() { }
                void IDisposable.Dispose() => Dispose();
            }
            
            public class Consumer
            {
                public void Use()
                {
                    using var r = new Resource();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedInDeconstruction_MarkDeconstructAsUsed()
    {
        const string SourceCode = """
            public class Point
            {
                public int X { get; }
                public int Y { get; }
                
                public Point(int x, int y) => (X, Y) = (x, y);
                
                public void Deconstruct(out int x, out int y)
                {
                    x = X;
                    y = Y;
                }
            }
            
            public class Consumer
            {
                public void Use()
                {
                    var p = new Point(1, 2);
                    var (x, y) = p;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DeconstructExtensionMethod_UsedInDeconstruction_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Point
            {
                public int X { get; }
                public int Y { get; }
                
                public Point(int x, int y) => (X, Y) = (x, y);
            }
            
            internal static class PointExtensions
            {
                public static void Deconstruct(this Point point, out int x, out int y)
                {
                    x = point.X;
                    y = point.Y;
                }
            }
            
            public class Consumer
            {
                public void Use()
                {
                    var p = new Point(1, 2);
                    var (x, y) = p;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalDeconstruct_UsedInDeconstruction_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Point
            {
                public int X { get; }
                public int Y { get; }
                
                public Point(int x, int y) => (X, Y) = (x, y);
                
                internal void Deconstruct(out int x, out int y)
                {
                    x = X;
                    y = Y;
                }
            }
            
            public class Consumer
            {
                public void Use()
                {
                    var p = new Point(1, 2);
                    var (x, y) = p;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedInAwait_MarkAwaiterMethodsAsUsed()
    {
        const string SourceCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            
            public class CustomAwaitable
            {
                public CustomAwaiter GetAwaiter() => new CustomAwaiter();
            }
            
            public class CustomAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public void GetResult() { }
                public void OnCompleted(Action continuation) { }
            }
            
            public class Consumer
            {
                public async Task UseAsync()
                {
                    await new CustomAwaitable();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalAwaitable_UsedInAwait_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            
            internal class CustomAwaitable
            {
                internal CustomAwaiter GetAwaiter() => new CustomAwaiter();
            }
            
            internal class CustomAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public void GetResult() { }
                public void OnCompleted(Action continuation) { }
            }
            
            public class Consumer
            {
                public async Task UseAsync()
                {
                    await new CustomAwaitable();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UserDefinedOperator_Used_NoDiagnostic()
    {
        const string SourceCode = """
            public class Vector
            {
                public int X { get; }
                public int Y { get; }
                
                public Vector(int x, int y) => (X, Y) = (x, y);
                
                public static Vector operator +(Vector a, Vector b) => new Vector(a.X + b.X, a.Y + b.Y);
            }
            
            public class Consumer
            {
                public int Sum(Vector a, Vector b) => (a + b).X;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalUserDefinedOperator_Used_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Vector
            {
                public int X { get; }
                public int Y { get; }
                
                public Vector(int x, int y) => (X, Y) = (x, y);
                
                public static Vector operator +(Vector a, Vector b) => new Vector(a.X + b.X, a.Y + b.Y);
            }
            
            public class Consumer
            {
                public int Sum(Vector a, Vector b) => (a + b).X;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalUserDefinedOperator_NotUsed_ReportsDiagnostic()
    {
        const string SourceCode = """
            internal class Vector
            {
                public static Vector [|operator +(Vector a, Vector b)|] => throw null;
            }
            
            public class Consumer
            {
                public int Sum(Vector a, Vector b) => throw null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PublicConversionOperator_Used_NoDiagnostic()
    {
        const string SourceCode = """
            public class Wrapper
            {
                public int Value { get; }
                
                public Wrapper(int value) => Value = value;
                
                public static implicit operator int(Wrapper w) => w.Value;
            }
            
            public class Consumer
            {
                public int GetValue(Wrapper w) => w;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalConversionOperator_Used_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Wrapper
            {
                public int Value { get; }
                
                public Wrapper(int value) => Value = value;
                
                public static implicit operator int(Wrapper w) => w.Value;
            }
            
            public class Consumer
            {
                public int GetValue()
                {
                    var w = new Wrapper(42);
                    return w; // implicit conversion
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalConversionOperator_NotUsed_ReportsDiagnostic()
    {
        const string SourceCode = """
            internal class Wrapper
            {
                public int Value { get; }
                
                public Wrapper(int value) => Value = value;
                
                public static implicit operator [|int|](Wrapper w) => w.Value;
            }
            
            public class Consumer
            {
                public int GetValue()
                {
                    var a  = new Wrapper(42);
                    return 0;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalExplicitConversionOperator_Used_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Wrapper
            {
                public int Value { get; }
                
                public Wrapper(int value) => Value = value;
                
                public static explicit operator int(Wrapper w) => w.Value;
            }
            
            public class Consumer
            {
                public int GetValue()
                {
                    var w = new Wrapper(42);
                    return (int)w; // explicit conversion
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalExplicitConversionOperator_NotUsed_ReportsDiagnostic()
    {
        const string SourceCode = """
            internal class Wrapper
            {
                public int Value { get; }
                
                public Wrapper(int value) => Value = value;
                
                public static explicit operator [|int|](Wrapper w) => w.Value;
            }
            
            public class Consumer
            {
                public int GetValue()
                {
                    var w = new Wrapper(42);
                    return 0;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CompilerGeneratedBackingField_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                public int AutoProperty { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PartialMethod_ReportsDiagnostic()
    {
        const string SourceCode = """
            public partial class Sample
            {
                partial void [|OnSomething|]();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PartialMethodWithImplementation_NoDiagnostic()
    {
        const string SourceCode = """
            public partial class Sample
            {
                partial void [|OnSomething|]();
                partial void [|OnSomething|]() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PartialMethodWithImplementationUsed_NoDiagnostic()
    {
        const string SourceCode = """
            public partial class Sample
            {
                partial void OnSomething();
                partial void OnSomething() { }
                
                public void Use() => OnSomething();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task TopLevelStatements_EntryPoint_NoDiagnostic()
    {
        const string SourceCode = """
            System.Console.WriteLine("Hello");
            """;
        await CreateProjectBuilder()
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MainMethod_EntryPoint_NoDiagnostic()
    {
        const string SourceCode = """
            public class Program
            {
                private static void Main(string[] args)
                {
                    System.Console.WriteLine("Hello");
                }
            }
            """;
        await CreateProjectBuilder()
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyCompoundAssignment_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void Increment() => Property += 1;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyIncrement_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void Increment() => Property++;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyDecrement_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void Decrement() => Property--;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyPostfixIncrement_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public int Increment()
                {
                    return Property++;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyPrefixIncrement_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public int Increment()
                {
                    return ++Property;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldIncrement_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void Increment() => _field++;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldDecrement_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void Decrement() => _field--;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldPrefixIncrement_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public int Increment() => ++_field;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldPostfixIncrement_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public int Increment() => _field++;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyCoalesceAssignment_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private string Property { get; set; }
                
                public void SetDefault(string value) => Property ??= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldCoalesceAssignment_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private string _field;
                
                public void SetDefault(string value) => _field ??= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyCoalesceAssignment_OnlyGetter_ReportsUnusedSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private string Property { get; [|set|]; }
                
                public void SetDefault(string value)
                {
                    var temp = Property ?? value;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyAddAssignment_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void Add(int value) => Property += value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertySubtractAssignment_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void Subtract(int value) => Property -= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyMultiplyAssignment_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void Multiply(int value) => Property *= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyDivideAssignment_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void Divide(int value) => Property /= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyModuloAssignment_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void Modulo(int value) => Property %= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyBitwiseAndAssignment_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void BitwiseAnd(int value) => Property &= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyBitwiseOrAssignment_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void BitwiseOr(int value) => Property |= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyBitwiseXorAssignment_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void BitwiseXor(int value) => Property ^= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyLeftShiftAssignment_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void LeftShift(int value) => Property <<= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyRightShiftAssignment_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void RightShift(int value) => Property >>= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldAddAssignment_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void Add(int value) => _field += value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldSubtractAssignment_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void Subtract(int value) => _field -= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldMultiplyAssignment_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void Multiply(int value) => _field *= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldDivideAssignment_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void Divide(int value) => _field /= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldModuloAssignment_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void Modulo(int value) => _field %= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldBitwiseAndAssignment_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void BitwiseAnd(int value) => _field &= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldBitwiseOrAssignment_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void BitwiseOr(int value) => _field |= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldBitwiseXorAssignment_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void BitwiseXor(int value) => _field ^= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldLeftShiftAssignment_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void LeftShift(int value) => _field <<= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldRightShiftAssignment_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void RightShift(int value) => _field >>= value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyCompoundAssignment_GetOnlyProperty_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { [|get|]; }
                
                public void Add(int value)
                {
                    // This should fail to compile, but if it somehow doesn't, getter should be reported
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldCompoundAssignment_WithMethodCall_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void AddResult() => _field += GetValue();
                
                private int GetValue() => 10;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyCompoundAssignment_WithMethodCall_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Property { get; set; }
                
                public void AddResult() => Property += GetValue();
                
                private int GetValue() => 10;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldIncrement_InForLoop_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _counter;
                
                public void Loop()
                {
                    for (_counter = 0; _counter < 10; _counter++)
                    {
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyIncrement_InForLoop_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Counter { get; set; }
                
                public void Loop()
                {
                    for (Counter = 0; Counter < 10; Counter++)
                    {
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FieldCompoundAssignment_InExpression_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _value;
                
                public int Calculate() => (_value += 5) * 2;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyCompoundAssignment_InExpression_UsesGetterAndSetter()
    {
        const string SourceCode = """
            public class Sample
            {
                private int Value { get; set; }
                
                public int Calculate() => (Value += 5) * 2;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MethodDelegate_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private void Handler() { }
                
                public Action GetHandler() => Handler;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AddMethod_Unused_ReportsDiagnostic()
    {
        const string SourceCode = """
            using System.Collections;
            using System.Collections.Generic;
            
            public class CustomCollection : IEnumerable<int>
            {
                private List<int> _items = new();
                
                private void [|Add|](int item) => _items.Add(item);
                
                public IEnumerator<int> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AddMethod_UsedInCollectionInitializer_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Collections;
            using System.Collections.Generic;
            
            internal class CustomCollection : IEnumerable<int>
            {
                private List<int> _items = new();
                
                public void Add(int item) => _items.Add(item);
                
                public IEnumerator<int> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            
            public class Consumer
            {
                public void Use()
                {
                    CustomCollection collection = new() { 1, 2, 3 };
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedPrivateIndexer_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int this[int index] => [|index|];
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivateIndexer_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int this[int index] => index;
                
                public int GetValue(int index) => this[index];
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericMethod_Used_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private T GetDefault<T>() => default;
                
                public int GetDefaultInt() => GetDefault<int>();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MethodUsedViaRefParameter_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _value;
                
                private void SetValue(ref int value) => value = 42;
                
                public void DoWork()
                {
                    SetValue(ref _value);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NestedType_UnusedMethod_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Outer
            {
                private class Inner
                {
                    private void [|UnusedMethod|]() { }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GeneratedCode_NoDiagnosticReported()
    {
        const string SourceCode = """
            public class Sample
            {
                private void UnusedMethod() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode("Test.g.cs", SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MethodUsedFromDifferentPartOfClass_NoDiagnostic()
    {
        const string SourceCode = """
            public partial class Sample
            {
                private void PrivateMethod() { }
            }
            
            public partial class Sample
            {
                public void PublicMethod() => PrivateMethod();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalMethodInInternalClass_UsedFromDifferentClass_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Helper
            {
                internal void DoWork() { }
            }
            
            public class Consumer
            {
                public void Use()
                {
                    new Helper().DoWork();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task RecordWithProperty_NoDiagnostic()
    {
        const string SourceCode = """
            public record Sample(int Value);
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RecordStructWithProperty_NoDiagnostic()
    {
        const string SourceCode = """
            public record struct Sample(int Value);
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalRecordWithUsedProperty_NoDiagnostic()
    {
        const string SourceCode = """
            internal record Sample(int Value);
            
            public class Consumer
            {
                public int GetValue(Sample sample) => sample.Value;
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalRecordWithUnusedProperty_ReportsDiagnostic()
    {
        const string SourceCode = """
            internal record Sample([|int Value|]);
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task AsyncMethod_UsedWithAwait_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Threading.Tasks;
            
            public class Sample
            {
                private async Task DoWorkAsync() => await Task.Delay(100);
                
                public async Task PublicMethodAsync() => await DoWorkAsync();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassMember_InUnitTestClass_NoDiagnostic()
    {
        const string SourceCode = """
            using Xunit;
            
            public class TestClass
            {
                private int _field;
                
                [Fact]
                public void Test()
                {
                    _field = 1;
                }
            }
            """;
        await CreateProjectBuilder()
              .AddXUnitApi()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateField_InitializedInConstructor_UsedInMethod_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private readonly int _value;
                
                public Sample(int value) => _value = value;
                
                public int GetValue() => _value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateField_OnlyInitialized_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private readonly int _value;
                
                public Sample(int value) => _value = value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateField_WithInlineInitializer_NeverRead_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int [|_value|] = 42;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateField_WithInlineInitializer_IsRead_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _value = 42;
                
                public int GetValue() => _value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateReadonlyField_WithInlineInitializer_NeverRead_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private readonly int [|_value|] = 42;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticPrivateField_WithInlineInitializer_NeverRead_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private static int [|_value|] = 42;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticPrivateField_WithInlineInitializer_IsRead_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private static int _value = 42;
                
                public static int GetValue() => _value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateField_InitializedWithMethodCall_NeverRead_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int [|_value|] = GetInitialValue();
                
                private static int GetInitialValue() => 42;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateField_InitializedWithMethodCall_IsRead_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _value = GetInitialValue();
                
                private static int GetInitialValue() => 42;
                
                public int GetValue() => _value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultipleFields_WithInitializers_SomeUsed_ReportsDiagnosticForUnused()
    {
        const string SourceCode = """
            public class Sample
            {
                private static int _used = 1;
                private static int [|_unused|] = 2;
                private static int _alsoUsed = 3;
                
                public int GetSum() => _used + _alsoUsed;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateField_InitializedWithAnotherField_BothUsed_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private static int _base = 10;
                private static int _derived = _base * 2;
                
                public int GetDerived() => _derived;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateField_WithComplexInitializer_NeverRead_ReportsDiagnostic()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            
            public class Sample
            {
                private List<int> [|_items|] = new List<int> { 1, 2, 3 };
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateField_WithComplexInitializer_IsRead_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            
            public class Sample
            {
                private List<int> _items = new List<int> { 1, 2, 3 };
                
                public int GetCount() => _items.Count;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task EventWithAdd_UsedBySubscription_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private event EventHandler Changed;
                
                public void Subscribe(EventHandler handler)
                {
                    Changed += handler;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task EventWithRaise_Used_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private event EventHandler Changed;
                
                public void OnChanged()
                {
                    Changed?.Invoke(this, EventArgs.Empty);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticPrivateMethod_UsedStatically_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private static void PrivateMethod() { }
                
                public static void PublicMethod() => PrivateMethod();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticPrivateField_UsedStatically_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private static int _field;
                
                public static int GetValue() => _field;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MethodUsedInLambda_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            using System.Linq;
            
            public class Sample
            {
                private bool Filter(int x) => x > 0;
                
                public int[] GetPositive(int[] items) => items.Where(Filter).ToArray();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MethodUsedAsMethodGroup_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private void Handler(object sender, EventArgs e) { }

                public void Subscribe(EventHandler handler)
                {
                    handler += Handler;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MethodUsedAsMethodGroup_LinqWhere_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Linq;
            
            public class Sample
            {
                private bool IsValid(int value) => value > 0;
                
                public int[] Filter(int[] items) => items.Where(IsValid).ToArray();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ForEach_ExtensionGetEnumerator_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            
            public class CustomCollection
            {
                public int[] Items { get; } = new[] { 1, 2, 3 };
            }
            
            internal static class CustomCollectionExtensions
            {
                public static IEnumerator<int> GetEnumerator(this CustomCollection collection)
                {
                    return ((IEnumerable<int>)collection.Items).GetEnumerator();
                }
            }
            
            public class Consumer
            {
                public void Iterate(CustomCollection collection)
                {
                    foreach (var item in collection)
                    {
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ForEach_ExtensionCSharp14GetEnumerator_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            
            public class CustomCollection
            {
                public int[] Items { get; } = new[] { 1, 2, 3 };
            }
            
            internal static class CustomCollectionExtensions
            {
                extension (CustomCollection collection)
                {
                    public IEnumerator<int> GetEnumerator()
                    {
                        return ((IEnumerable<int>)collection.Items).GetEnumerator();
                    }
                }
            }
            
            public class Consumer
            {
                public void Iterate(CustomCollection collection)
                {
                    foreach (var item in collection)
                    {
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ForEach_GetEnumeratorExplicitImplementation_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Collections;
            using System.Collections.Generic;
            
            public class CustomCollection : IEnumerable
            {
                public int[] Items { get; } = new[] { 1, 2, 3 };

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((IEnumerable<int>)Items).GetEnumerator();
                }
            }
            
            public class Consumer
            {
                public void Iterate(CustomCollection collection)
                {
                    foreach (var item in collection)
                    {
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Await_ExtensionGetAwaiter_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            
            public class CustomAwaitable
            {
            }
            
            internal class CustomAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public void GetResult() { }
                public void OnCompleted(Action continuation) { }
            }
            
            internal static class CustomAwaitableExtensions
            {
                public static CustomAwaiter GetAwaiter(this CustomAwaitable awaitable) => new CustomAwaiter();
            }
            
            public class Consumer
            {
                public async Task UseAsync()
                {
                    await new CustomAwaitable();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ForEach_StaticGetEnumerator_ReportsDiagnostic()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            
            public class CustomCollection
            {
                private static IEnumerator<int> [|GetEnumerator|]() => new List<int>().GetEnumerator();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Await_StaticGetAwaiter_ReportsDiagnostic()
    {
        const string SourceCode = """
            using System;
            using System.Runtime.CompilerServices;
            
            public class CustomAwaitable
            {
                private static CustomAwaiter [|GetAwaiter|]() => new CustomAwaiter();
            }
            
            public class CustomAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public void GetResult() { }
                public void OnCompleted(Action continuation) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CollectionBuilderAttribute_MethodNotUsed_ReportsDiagnostic()
    {
        const string SourceCode = """
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            
            [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
            internal class MyCollection<T> : IEnumerable<T>
            {
                private readonly T[] _items;
                
                public MyCollection(T[] items) => _items = items;
                
                public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
            }
            
            internal static class MyCollectionBuilder
            {
                internal static MyCollection<T> [|Create|]<T>(ReadOnlySpan<T> items) => new MyCollection<T>(items.ToArray());
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericClass_Constructor_Used_NoDiagnostic()
    {
        const string SourceCode = """            
            internal class MyCollection<T>
            {
                public MyCollection() { }
            }
            
            public class Consumer
            {
                public void UseCollectionBuilder()
                {
                    _ = new MyCollection<int>();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CollectionBuilderAttribute_MethodIsUsed_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            
            [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
            internal class MyCollection<T> : IEnumerable<T>
            {
                private readonly T[] _items;
                
                public MyCollection(T[] items) => _items = items;
                
                public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
            }
            
            internal static class MyCollectionBuilder
            {
                internal static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => new MyCollection<T>(items.ToArray());
            }

            public class Consumer
            {
                public void UseCollectionBuilder()
                {
                    MyCollection<int> collection = [ 1, 2, 3 ];
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CollectionInitializer_MethodIsUsed_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            
            internal class MyCollection<T> : IEnumerable<T>
            {
                private readonly T[] _items;
                
                public void Add(T item) { }

                public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
            }
            
            public class Consumer
            {
                public void UseCollectionInitializer()
                {
                    var collection = new MyCollection<int>() { 1, 2, 3 };
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InlineArray_BackingField_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Runtime.CompilerServices;

            // Inline array are not reported because they are handled by MA0182
            [InlineArray(10)]
            internal struct Buffer
            {
                private int _element;
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_WithLiteralDefault_UsedInCall_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private void Method(int value = 42) { }
                
                public void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_WithLiteralDefault_NotUsed_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private void [|Method|](int value = 42) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_DefaultValueCallsPrivateMethod_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private static int GetDefaultValue() => 42;
                
                private void Method(int value = 42) { }
                
                public void Use()
                {
                    Method(GetDefaultValue());
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_DefaultValueReferencesPrivateConst_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private const int DefaultValue = 42;
                
                private void Method(int value = DefaultValue) { }
                
                public void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_DefaultValueReferencesUnusedPrivateConst_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private const int [|UnusedValue|] = 99;
                private const int DefaultValue = 42;
                
                private void Method(int value = DefaultValue) { }
                
                public void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_MultipleParameters_SomeWithDefaults_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private void Method(int a, int b = 10, int c = 20) { }
                
                public void Use()
                {
                    Method(1);
                    Method(1, 2);
                    Method(1, 2, 3);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_WithNullDefault_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private void Method(string value = null) { }
                
                public void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_WithDefaultExpression_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private void Method(int value = default) { }
                
                public void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InConstructor_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private Sample(int value = 42) { }
                
                public static Sample Create() => new Sample();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InConstructor_Unused_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private [|Sample|](int value = 42) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_WithEnumDefault_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private void Method(StringComparison comparison = StringComparison.Ordinal) { }
                
                public void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_DefaultReferencesPrivateEnum_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private enum Mode
                {
                    Default,
                    [|Advanced|],
                }
                
                private void Method(Mode mode = Mode.Default) { }
                
                public void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_CalledWithNamedArguments_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private void Method(int a = 1, int b = 2, int c = 3) { }
                
                public void Use()
                {
                    Method(c: 5);
                    Method(b: 4, a: 3);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InOverloadedMethod_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private void Method(int value) { }
                private void Method(int value, string name = "default") { }
                
                public void Use()
                {
                    Method(1);
                    Method(1, "custom");
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InOverloadedMethod_WithOverloadPriority_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                [System.Runtime.CompilerServices.OverloadResolutionPriority(-1)]
                private void [|Method|](int value) { }
                private void Method(int value, string name = "default") { }
                
                public void Use()
                {
                    Method(1);
                    Method(1, "custom");
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.Net9_0)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InInterfaceImplementation_NoDiagnostic()
    {
        const string SourceCode = """
            public interface IService
            {
                void DoWork(int value = 42);
            }
            
            internal class ServiceImpl : IService
            {
                public void DoWork(int value = 42) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_DefaultValueExpression_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private const int BaseValue = 10;
                
                private void Method(int value = BaseValue * 2) { }
                
                public void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_MultipleDefaults_OnlyOneUsed_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private const int Default1 = 10;
                private const int [|Default2|] = 20;
                
                private void Method(int value = Default1) { }
                
                public void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_CallerMemberName_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Runtime.CompilerServices;
            
            public class Sample
            {
                private void Method([CallerMemberName] string memberName = null) { }
                
                public void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_CallerFilePath_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Runtime.CompilerServices;
            
            public class Sample
            {
                private void Method([CallerFilePath] string filePath = null) { }
                
                public void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_CallerLineNumber_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Runtime.CompilerServices;
            
            public class Sample
            {
                private void Method([CallerLineNumber] int lineNumber = 0) { }
                
                public void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_WithStringInterpolation_NotSupported()
    {
        const string SourceCode = """
            public class Sample
            {
                private void Method(string value = "default") { }
                
                public void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InStaticMethod_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private static void Method(int value = 42) { }
                
                public static void Use()
                {
                    Method();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InExtensionMethod_NoDiagnostic()
    {
        const string SourceCode = """
            internal static class Extensions
            {
                internal static void Method(this string value, int count = 1) { }
            }
            
            public class Sample
            {
                public void Use()
                {
                    "test".Method();
                    "test".Method(2);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InLocalFunction_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                public void Use()
                {
                    LocalMethod();
                    
                    void LocalMethod(int value = 42) { }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InDelegate_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private delegate void Handler(int value = 42);
                
                private void Method(int value = 42) { }
                
                public void Use()
                {
                    Handler handler = Method;
                    handler();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InLambda_NotUsed_ReportsDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private Func<int, int> [|GetMultiplier|]()
                {
                    return (int factor = 2) => factor * 10;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InLambda_WithConst_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private const int DefaultMultiplier = 10;
                
                private Func<int, int> GetMultiplier()
                {
                    return (int factor = DefaultMultiplier) => factor * 2;
                }
                
                public void Use()
                {
                    var multiplier = GetMultiplier();
                    var result = multiplier(0);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InLambda_AssignedToDelegate_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private delegate int Calculator(int a, int b = 10);
                
                private Calculator GetCalculator()
                {
                    return (int a, int b = 10) => a + b;
                }
                
                public void Use()
                {
                    Calculator calc = GetCalculator();
                    var result1 = calc(5);
                    var result2 = calc(5, 15);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InLambda_StoredInField_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private Func<int, int> _calculator = (int value = 5) => value * 2;
                
                public int Calculate()
                {
                    return _calculator(0);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InAsyncLambda_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            using System.Threading.Tasks;
            
            public class Sample
            {
                private Func<int, Task<int>> GetAsyncCalculator()
                {
                    return async (int value = 42) => 
                    {
                        await Task.Delay(100);
                        return value * 2;
                    };
                }
                
                public async Task Use()
                {
                    var calc = GetAsyncCalculator();
                    var result = await calc(0);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InEventHandlerLambda_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private event EventHandler<int> ValueChanged;
                
                private void Subscribe()
                {
                    ValueChanged += (object sender = null, int value = 0) => 
                        Console.WriteLine(value);
                }
                
                public void Use()
                {
                    Subscribe();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptionalParameter_InGenericLambda_NoDiagnostic()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private Func<T, T> CreateIdentity<T>(T defaultValue)
                {
                    return (T value = default) => defaultValue;
                }
                
                public void Use()
                {
                    var identity = CreateIdentity(42);
                    var result = identity(0);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_DirectAccess_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void UseRef()
                {
                    ref var value = ref _field;
                    value = 42;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_InRefReturn_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public ref int GetFieldRef() => ref _field;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefReadonlyField_InRefReturn_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public ref readonly int GetFieldRef() => ref _field;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_MultipleReferences_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field1;
                private int _field2;
                
                public void UseRefs()
                {
                    ref var value1 = ref _field1;
                    ref var value2 = ref _field2;
                    value1 = value2;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_InLocalFunction_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void UseRef()
                {
                    GetRef() = 42;
                    
                    ref int GetRef() => ref _field;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_PassedToRefParameter_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void UseRef()
                {
                    Modify(ref _field);
                }
                
                private void Modify(ref int value)
                {
                    value = 42;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefReadonlyField_PassedToInParameter_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void UseRef()
                {
                    Process(in _field);
                }
                
                private void Process(in int value)
                {
                    var temp = value;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefProperty_AccessGetter_MarksGetterAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _backingField;
                
                private ref int Property => ref _backingField;
                
                public void UseRef()
                {
                    Property = 42;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefReadonlyProperty_AccessGetter_MarksGetterAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _backingField;
                
                private ref readonly int Property => ref _backingField;
                
                public void UseRef()
                {
                    var value = Property;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_InSpanConstructor_MarksFieldAsUsed()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private int _field;
                
                public Span<int> GetSpan()
                {
                    return new Span<int>(ref _field);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_InRefStruct_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public ref struct RefStruct
            {
                private int _field;
                
                public ref int GetRef() => ref _field;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_AssignedToRefLocal_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void UseRef()
                {
                    ref int reference = ref _field;
                    reference = 42;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefReadonlyField_AssignedToRefReadonlyLocal_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void UseRef()
                {
                    ref readonly int reference = ref _field;
                    var value = reference;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_InTernaryExpression_MarksFieldsAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field1;
                private int _field2;
                
                public ref int GetConditionalRef(bool condition)
                {
                    return ref condition ? ref _field1 : ref _field2;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_InForEachRef_MarksFieldAsUsed()
    {
        const string SourceCode = """
            using System;
            
            public class Sample
            {
                private int[] _array = new int[10];
                
                public void UseRef()
                {
                    foreach (ref var item in _array.AsSpan())
                    {
                        item = 42;
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.Net10_0)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_UsedInOutParameter_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public void UseRef()
                {
                    Initialize(out _field);
                }
                
                private void Initialize(out int value)
                {
                    value = 42;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_InIndexer_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public ref int this[int index]
                {
                    get => ref _field;
                }
                
                public void UseRef()
                {
                    this[0] = 42;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_ChainedRefReturns_MarksAllFieldsAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field1;
                private int _field2;
                
                public ref int GetRef1() => ref _field1;
                public ref int GetRef2() => ref _field2;
                
                public void UseRefs()
                {
                    GetRef1() = GetRef2();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_InSwitchExpression_MarksAllFieldsAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field1;
                private int _field2;
                private int _field3;
                
                public ref int GetRefByIndex(int index)
                {
                    return ref index switch
                    {
                        0 => ref _field1,
                        1 => ref _field2,
                        _ => ref _field3
                    };
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_AssignmentTarget_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public ref int GetRef() => ref _field;
                
                public void UseRef()
                {
                    GetRef() = 42;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_IncrementThroughRef_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public ref int GetRef() => ref _field;
                
                public void UseRef()
                {
                    GetRef()++;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefField_CompoundAssignmentThroughRef_MarksFieldAsUsed()
    {
        const string SourceCode = """
            public class Sample
            {
                private int _field;
                
                public ref int GetRef() => ref _field;
                
                public void UseRef()
                {
                    GetRef() += 10;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LocalFunction_Unused_NoDiagnostic()
    {
        // Unused local functions should not be reported by this analyzer
        const string SourceCode = """
            public class Sample
            {
                public void Method()
                {
                    void UnusedLocalFunction() { }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    // Windows Forms Designer Property Grid Support Methods
    // 
    // The Windows Forms designer uses special method naming conventions to provide design-time
    // functionality for properties in the property grid:
    // 
    // - "bool ShouldSerializeXXX()" determines whether a property named XXX should be serialized
    //   to code by the designer. The designer calls this method to decide if the property value
    //   differs from its default and should be written to the designer-generated code.
    // 
    // - "void ResetXXX()" resets a property named XXX to its default value. The designer adds
    //   a "Reset" context menu item to the property grid when this method exists.
    // 
    // These methods are not directly called in user code but are invoked by the Windows Forms
    // designer infrastructure at design time, so they should not be reported as unused when
    // there is a matching property with the same name (case-sensitive).

    [Fact]
    public async Task ShouldSerializeMethod_WithMatchingProperty_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                public int Value { get; set; }
                
                private bool ShouldSerializeValue() => Value != 0;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ShouldSerializeMethod_WithoutMatchingProperty_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private bool [|ShouldSerializeValue|]() => false;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ShouldSerializeMethod_CaseSensitive_ReportsDiagnostic()
    {
        // The method name must match the property name exactly (case-sensitive)
        // for compatibility with legacy FxCop implementation
        const string SourceCode = """
            public class Sample
            {
                public int Value { get; set; }
                
                private bool [|ShouldSerializevalue|]() => false;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ResetMethod_WithMatchingProperty_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                public int Value { get; set; }
                
                private void ResetValue() => Value = 0;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ResetMethod_WithoutMatchingProperty_ReportsDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                private void [|ResetValue|]() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ResetMethod_CaseSensitive_ReportsDiagnostic()
    {
        // The method name must match the property name exactly (case-sensitive)
        // for compatibility with legacy FxCop implementation
        const string SourceCode = """
            public class Sample
            {
                public int Value { get; set; }
                
                private void [|Resetvalue|]() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ShouldSerializeAndResetMethods_WithMatchingProperty_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                public int Value { get; set; }
                
                private bool ShouldSerializeValue() => Value != 0;
                
                private void ResetValue() => Value = 0;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ShouldSerializeMethod_WithMatchingField_ReportsDiagnostic()
    {
        // These methods only work with properties, not fields
        const string SourceCode = """
            public class Sample
            {
                public int Value;
                
                private bool [|ShouldSerializeValue|]() => Value != 0;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ResetMethod_WithMatchingField_ReportsDiagnostic()
    {
        // These methods only work with properties, not fields
        const string SourceCode = """
            public class Sample
            {
                public int Value;
                
                private void [|ResetValue|]() => Value = 0;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ShouldSerializeMethod_WithParameters_ReportsDiagnostic()
    {
        // ShouldSerializeXXX() must be parameterless
        const string SourceCode = """
            public class Sample
            {
                public int Value { get; set; }
                
                private bool [|ShouldSerializeValue|](int x) => false;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ResetMethod_WithParameters_ReportsDiagnostic()
    {
        // ResetXXX() must be parameterless
        const string SourceCode = """
            public class Sample
            {
                public int Value { get; set; }
                
                private void [|ResetValue|](int x) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ShouldSerializeMethod_WrongReturnType_ReportsDiagnostic()
    {
        // ShouldSerializeXXX() must return bool
        const string SourceCode = """
            public class Sample
            {
                public int Value { get; set; }
                
                private int [|ShouldSerializeValue|]() => 0;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ShouldSerializeAndResetMethods_MultipleProperties_NoDiagnostic()
    {
        const string SourceCode = """
            public class Sample
            {
                public int Value1 { get; set; }
                public string Value2 { get; set; }
                public bool Value3 { get; set; }
                
                private bool ShouldSerializeValue1() => Value1 != 0;
                private void ResetValue1() => Value1 = 0;
                
                private bool ShouldSerializeValue2() => !string.IsNullOrEmpty(Value2);
                private void ResetValue2() => Value2 = null;
                
                private bool ShouldSerializeValue3() => Value3;
                private void ResetValue3() => Value3 = false;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ShouldSerializeMethod_InternalProperty_NoDiagnostic()
    {
        // These methods work with properties of any accessibility
        const string SourceCode = """
            internal class Sample
            {
                internal int Value { get; set; }
                
                private bool ShouldSerializeValue() => Value != 0;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ResetMethod_StaticProperty_ReportsDiagnostic()
    {
        // These methods work only with instance properties, not static
        const string SourceCode = """
            public class Sample
            {
                public static int Value { get; set; }
                
                private void [|ResetValue|]() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ShouldSerializeMethod_StaticMethod_WithInstanceProperty_ReportsDiagnostic()
    {
        // The methods must be instance methods
        const string SourceCode = """
            public class Sample
            {
                public int Value { get; set; }
                
                private static bool [|ShouldSerializeValue|]() => false;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
