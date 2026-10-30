using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.FloatingPointToDecimalConversionAnalyzer,
    Meziantou.Analyzer.Rules.FloatingPointToDecimalConversionFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class FloatingPointToDecimalConversionAnalyzerTests
{
    [Fact]
    public Task Valid()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Dummy
            {
                void Test(int i, long l, decimal m)
                {
                    _ = 2.3m;
                    _ = (decimal)1;
                    _ = (decimal)i;
                    _ = (double)m;
                    _ = (float)2.3m;
                    _ = new decimal(1);
                    _ = new decimal(l);
                    _ = System.Convert.ToDecimal(1);
                    _ = System.Convert.ToDecimal("2.3");
                    _ = decimal.CreateChecked(1);
                    _ = double.CreateChecked(2.3m);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Cast_DoubleLiteral()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Dummy
            {
                decimal Test() => {|MA0244:(decimal)2.3|};
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                decimal Test() => 2.3m;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Cast_Suffixes()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Dummy
            {
                void Test()
                {
                    _ = {|MA0244:(decimal)14.1f|};
                    _ = {|MA0244:(decimal)14.1F|};
                    _ = {|MA0244:(decimal)2.3d|};
                    _ = {|MA0244:(decimal)2.3D|};
                    _ = {|MA0244:(decimal)1d|};
                    _ = {|MA0244:(decimal)1e-3|};
                    _ = {|MA0244:(decimal)1E+3d|};
                    _ = {|MA0244:(decimal)1_000.5|};
                    _ = {|MA0244:(decimal).5|};
                }
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = 14.1m;
                    _ = 14.1m;
                    _ = 2.3m;
                    _ = 2.3m;
                    _ = 1m;
                    _ = 1e-3m;
                    _ = 1E+3m;
                    _ = 1_000.5m;
                    _ = .5m;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Cast_Negative()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Dummy
            {
                void Test(decimal value)
                {
                    _ = {|MA0244:(decimal)-0.5|};
                    _ = {|MA0244:(decimal)(-0.5)|};
                    _ = {|MA0244:(decimal)(-(0.5))|};
                    _ = value-{|MA0244:(decimal)-0.5|};
                    _ = value - {|MA0244:(decimal)-0.5|};
                }
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                void Test(decimal value)
                {
                    _ = -0.5m;
                    _ = -0.5m;
                    _ = -0.5m;
                    _ = value- -0.5m;
                    _ = value - -0.5m;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Cast_Parenthesized()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Dummy
            {
                string Test() => ({|MA0244:(decimal)(2.3)|}).ToString();
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                string Test() => (2.3m).ToString();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Cast_NullableDecimal()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Dummy
            {
                void Test()
                {
                    var value = {|MA0244:(decimal?)2.3|};
                }
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                void Test()
                {
                    var value = (decimal?)2.3m;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Cast_ConstDeclaration()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Dummy
            {
                const decimal Value = {|MA0244:(decimal)2.3|};
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                const decimal Value = 2.3m;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Cast_NonLiteralConstant_NoFix()
    {
        var test = new CodeFixTest();
        var sourceCode = """
            class Dummy
            {
                const double Rate = 0.23;

                void Test()
                {
                    _ = {|MA0244:(decimal)Rate|};
                    _ = {|MA0244:(decimal)(1.0 / 3)|};
                    _ = {|MA0244:(decimal)(float)2.3|};
                    _ = {|MA0244:(decimal)+2.3|};
                }
            }
            """;
        test.TestCode = sourceCode;
        test.FixedCode = sourceCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Constructor()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Dummy
            {
                void Test()
                {
                    _ = {|MA0244:new decimal(123.4567)|};
                    _ = {|MA0244:new decimal(14.1f)|};
                    decimal value = {|MA0244:new(2.3)|};
                    _ = {|MA0244:new decimal(-1.5)|}.ToString();
                }
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = 123.4567m;
                    _ = 14.1m;
                    decimal value = 2.3m;
                    _ = (-1.5m).ToString();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConvertToDecimal()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;

            class Dummy
            {
                void Test()
                {
                    _ = {|MA0244:Convert.ToDecimal(0.1)|};
                    _ = {|MA0244:Convert.ToDecimal(0.1f)|};
                }
            }
            """;
        test.FixedCode = """
            using System;

            class Dummy
            {
                void Test()
                {
                    _ = 0.1m;
                    _ = 0.1m;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericMath()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Dummy
            {
                void Test()
                {
                    _ = {|MA0244:decimal.CreateChecked(2.3)|};
                    _ = {|MA0244:decimal.CreateSaturating(2.3f)|};
                    _ = {|MA0244:decimal.CreateTruncating<double>(2.3)|};
                }
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = 2.3m;
                    _ = 2.3m;
                    _ = 2.3m;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FixAll()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Dummy
            {
                decimal[] Test() => [{|MA0244:(decimal)1.1|}, {|MA0244:new decimal(2.2)|}, {|MA0244:System.Convert.ToDecimal(3.3)|}];
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                decimal[] Test() => [1.1m, 2.2m, 3.3m];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonConstant()
    {
        var test = new CodeFixTest();
        var sourceCode = """
            using System;

            class Dummy
            {
                void Test(double d, float f, double? nd)
                {
                    _ = {|MA0245:(decimal)d|};
                    _ = {|MA0245:(decimal)f|};
                    _ = {|MA0245:(decimal?)nd|};
                    _ = {|MA0245:(decimal)nd|};
                    _ = {|MA0245:new decimal(d)|};
                    _ = {|MA0245:Convert.ToDecimal(f)|};
                    _ = {|MA0245:decimal.CreateChecked(d)|};
                }
            }
            """;
        test.TestCode = sourceCode;
        test.FixedCode = sourceCode;

        return test.RunAsync();
    }

    [Fact]
    public Task XunitInlineData()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddXunitV3();
        var sourceCode = """
            using Xunit;

            public class Tests
            {
                [Theory]
                [InlineData({|MA0244:2.3|}, 2.3, "2.3", 1)]
                [InlineData({|MA0244:-2.3f|}, 2.3, "2.3", 1)]
                [InlineData(null, 2.3, "2.3", 1)]
                public void Test(decimal? value, double d, decimal s, decimal i)
                {
                }
            }
            """;
        test.TestCode = sourceCode;
        test.FixedCode = sourceCode;

        return test.RunAsync();
    }

    [Fact]
    public Task XunitInlineData_ExplicitArray()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddXunitV3();
        var sourceCode = """
            using Xunit;

            public class Tests
            {
                [Theory]
                [InlineData(new object[] { 1, {|MA0244:2.3|} })]
                public void Test(int a, decimal b)
                {
                }
            }
            """;
        test.TestCode = sourceCode;
        test.FixedCode = sourceCode;

        return test.RunAsync();
    }

    [Fact]
    public Task NUnitTestCase()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddNUnit();
        var sourceCode = """
            using NUnit.Framework;

            public class Tests
            {
                [TestCase({|MA0244:2.3|})]
                [TestCase(1, {|MA0244:2.3|})]
                [TestCase(1, 2, {|MA0244:2.3|})]
                [TestCase(1, 2, 3, {|MA0244:2.3|})]
                public void Test(decimal a, decimal b = 0, decimal c = 0, decimal d = 0)
                {
                }
            }
            """;
        test.TestCode = sourceCode;
        test.FixedCode = sourceCode;

        return test.RunAsync();
    }

    [Fact]
    public Task MSTestDataRow()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddMSTest();
        var sourceCode = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public class Tests
            {
                [TestMethod]
                [DataRow({|MA0244:2.3|})]
                [DataRow({|MA0244:2.3|}, {|MA0244:4.5|})]
                public void Test(decimal a, decimal b = 0)
                {
                }
            }
            """;
        test.TestCode = sourceCode;
        test.FixedCode = sourceCode;

        return test.RunAsync();
    }

    [Fact]
    public Task TestDataAttribute_DerivedAttribute()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddNUnit();
        var sourceCode = """
            using NUnit.Framework;

            public class Tests
            {
                [CustomTestCase({|MA0244:2.3|})]
                public void Test(decimal value)
                {
                }
            }

            public sealed class CustomTestCaseAttribute(params object[] arguments) : TestCaseAttribute(arguments);
            """;
        test.TestCode = sourceCode;
        test.FixedCode = sourceCode;

        return test.RunAsync();
    }

    [Fact]
    public Task TestDataAttribute_ParamsParameter()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddXunitV3();
        test.TestCode = """
            using Xunit;

            public class Tests
            {
                [Theory]
                [InlineData(2.3, 4.5)]
                public void Test(params decimal[] values)
                {
                }
            }
            """;

        return test.RunAsync();
    }
}
