using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DoNotDeclareStaticMembersOnGenericTypesTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new DoNotDeclareStaticMembersOnGenericTypes();
        protected override string ExpectedDiagnosticId => "MA0018";
        protected override string ExpectedDiagnosticMessage => "Do not declare static members on generic types";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void StaticMembers()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test<T>
{
    static string field;
    static string Prop => throw null;
    static string Method() => throw null;

    string field2;
    string Prop2 => throw null;
    string Method2() => throw null;
}

class Test
{
    static string field;
    static string Prop => throw null;
    static string Method() => throw null;

    string field2;
    string Prop2 => throw null;
    string Method2() => throw null;
}");
            var expected = new[]
            {
                CreateDiagnosticResult(line: 4, column: 19),
                CreateDiagnosticResult(line: 5, column: 5),
                CreateDiagnosticResult(line: 6, column: 5),
            };
            VerifyDiagnostic(project, expected);
        }

    }
}
