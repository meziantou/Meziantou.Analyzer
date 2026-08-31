using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.MergeIsPatternChecksAnalyzer,
    Meziantou.Analyzer.Rules.MergeIsPatternChecksFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MergeIsPatternChecksAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task LogicalOr_ConstantPattern()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = 0;
            _ = [|value is 1 || value is 2|];
            """;
        test.FixedCode = """
            var value = 0;
            _ = value is 1 or 2;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LogicalOr_EnumPattern()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = (System.DayOfWeek)0;
            _ = [|value is System.DayOfWeek.Monday || value is System.DayOfWeek.Tuesday|];
            """;
        // A qualified enum member in a pattern is parsed as a type, whereas the fixer reuses the constant
        // the compiler binds it to, so the shape of the tree cannot be compared with the parsed one
        test.CodeActionValidationMode = CodeActionValidationMode.None;
        test.FixedCode = """
            var value = (System.DayOfWeek)0;
            _ = value is System.DayOfWeek.Monday or System.DayOfWeek.Tuesday;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LogicalAnd_EnumPattern()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = (System.DayOfWeek)0;
            _ = [|value is System.DayOfWeek.Monday && value is System.DayOfWeek.Tuesday|];
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LogicalAnd_NotPattern()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = (System.DayOfWeek)0;
            _ = [|value is System.DayOfWeek.Monday && value is not System.DayOfWeek.Tuesday|];
            """;
        // A qualified enum member in a pattern is parsed as a type, whereas the fixer reuses the constant
        // the compiler binds it to, so the shape of the tree cannot be compared with the parsed one
        test.CodeActionValidationMode = CodeActionValidationMode.None;
        test.FixedCode = """
            var value = (System.DayOfWeek)0;
            _ = value is System.DayOfWeek.Monday and not System.DayOfWeek.Tuesday;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LogicalAnd_ParenthesizeOrPattern()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = MyEnum.Value1;
            _ = [|value is (MyEnum.Value1 or MyEnum.Value2) && value is not MyEnum.Value2|];

            enum MyEnum { Value1, Value2 }
            """;
        test.FixedCode = """
            var value = MyEnum.Value1;
            _ = value is (MyEnum.Value1 or MyEnum.Value2) and not MyEnum.Value2;

            enum MyEnum { Value1, Value2 }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LogicalOr_ParenthesizeAndPattern()
    {
        var test = CreateTest();
        test.TestCode = """
            byte marker = 0;
            _ = [|marker is 0x01 || marker is >= 0xD0 and <= 0xD7|];
            """;
        test.FixedCode = """
            byte marker = 0;
            _ = marker is 0x01 or (>= 0xD0 and <= 0xD7);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DifferentExpressions_DoNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            var value1 = MyEnum.Value1;
            var value2 = MyEnum.Value2;
            _ = value1 is MyEnum.Value1 || value2 is MyEnum.Value2;

            enum MyEnum { Value1, Value2 }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Indexer_SameArgument_NotPatternWithDeclaration_DoNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            var cList = new System.Collections.Generic.List<object?> { "" };
            _ = cList[0] is not null || cList[0] is not string cy;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LogicalOr_DeclarationPattern_DoNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            object? value = "";
            _ = value is string text || value is null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AlreadyMerged_DoNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = MyEnum.Value1;
            _ = value is MyEnum.Value1 or MyEnum.Value2;

            enum MyEnum { Value1, Value2 }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Parameter()
    {
        var test = CreateTest();
        test.TestCode = """
            static bool M(int value) => [|value is 1 || value is 2|];
            """;
        test.FixedCode = """
            static bool M(int value) => value is 1 or 2;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariable()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = 0;
            _ = [|value is 1 || value is 2|];
            """;
        test.FixedCode = """
            var value = 0;
            _ = value is 1 or 2;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_ExplicitAndImplicitThis()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = new Sample().M();

            class Sample
            {
                private int _value;
                public bool M() => [|_value is 1 || this._value is 2|];
            }
            """;
        test.FixedCode = """
            _ = new Sample().M();

            class Sample
            {
                private int _value;
                public bool M() => _value is 1 or 2;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_NameAndThisFieldName()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = new Sample().M();

            class Sample
            {
                private int fieldName;
                public bool M() => [|fieldName is 1 || this.fieldName is 2|];
            }
            """;
        test.FixedCode = """
            _ = new Sample().M();

            class Sample
            {
                private int fieldName;
                public bool M() => fieldName is 1 or 2;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariable_HidesField_DoNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = new Sample().M();

            class Sample
            {
                private int value;

                public bool M()
                {
                    var value = 0;
                    return value is 1 || this.value is 2;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_ExplicitAndImplicitThis()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = new Sample().M();

            class Sample
            {
                private int Value { get; set; }
                public bool M() => [|Value is 1 || this.Value is 2|];
            }
            """;
        test.FixedCode = """
            _ = new Sample().M();

            class Sample
            {
                private int Value { get; set; }
                public bool M() => Value is 1 or 2;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_DifferentInstances_DoNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            var a = new Sample();
            var b = new Sample();
            _ = a.Value is 1 || b.Value is 2;

            class Sample
            {
                public int Value { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Indexer_DifferentArguments_DoNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            var text = "";
            _ = text[0] is 'a' || text[1] is 'b';
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrimaryConstructorParameter()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            _ = new Sample(1).M();

            class Sample(int value)
            {
                public bool M() => [|value is 1 || value is 2|];
            }
            """;
        test.FixedCode = """
            _ = new Sample(1).M();

            class Sample(int value)
            {
                public bool M() => value is 1 or 2;
            }
            """;

        return test.RunAsync();
    }
}
