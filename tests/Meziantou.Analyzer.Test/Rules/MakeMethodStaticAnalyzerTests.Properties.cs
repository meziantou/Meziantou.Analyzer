﻿using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class MakeMethodStaticAnalyzerTests_Properties : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new MakeMethodStaticAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new MakeMethodStaticFixer();
        protected override string ExpectedDiagnosticId => "MA0041";
        protected override string ExpectedDiagnosticMessage => "Make property static";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Info;

        [TestMethod]
        public void ExpressionBody()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    int A => throw null;
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 9));
            VerifyFix(project, @"
class TestClass
{
    static int A => throw null;
}
");
        }

        [TestMethod]
        public void AccessInstanceProperty_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    int A => TestProperty;

    public int TestProperty { get; }
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AccessInstanceMethodInLinqQuery_Where_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    int A { get; set; }
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AccessStaticProperty()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    public int A => TestProperty;

    public static int TestProperty => 0;
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 16));
            VerifyFix(project, @"
class TestClass
{
    public static int A => TestProperty;

    public static int TestProperty => 0;
}
");
        }

        [TestMethod]
        public void AccessStaticMethod()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    int A => TestMethod();

    public static int TestMethod() => 0;
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 9));
        }

        [TestMethod]
        public void AccessStaticField()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    int A => _a;

    static int _a;
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 9));

            var fix = @"
class TestClass
{
    static int A => _a;

    static int _a;
}
";
            VerifyFix(project, fix);
        }

        [TestMethod]
        public void AccessInstanceField()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    int A => _a;

    public int _a;
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MethodImplementAnInterface()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass : ITest
{
    public int A { get; }
}

interface ITest
{
    int A { get; }
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MethodExplicitlyImplementAnInterface()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass : ITest
{
    int ITest.A { get; }
}

interface ITest
{
    int A { get; }
}
");

            VerifyDiagnostic(project);
        }
    }
}
