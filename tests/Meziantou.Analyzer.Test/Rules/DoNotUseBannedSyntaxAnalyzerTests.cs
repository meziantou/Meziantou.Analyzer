using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseBannedSyntaxAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseBannedSyntaxAnalyzerTests
{
    // The analyzer also reports the invalid entries (MA0241), so the markup must use the first descriptor (MA0240)
    private static AnalyzerTest CreateTest(string bannedSyntaxes)
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", bannedSyntaxes));
        return test;
    }

    private static DiagnosticResult Diagnostic(int markupKey, string name, string message) =>
        new DiagnosticResult("MA0240", DiagnosticSeverity.Warning).WithLocation(markupKey).WithArguments(name, message);

    [Fact]
    public Task NoFile_NoDiagnostic()
    {
        var test = new AnalyzerTest
        {
            TestCode = """
                class Sample
                {
                    void M()
                    {
                        goto label;
                        label:
                        return;
                    }
                }
                """,
        };

        return test.RunAsync();
    }

    [Fact]
    public Task OtherFileName_NoDiagnostic()
    {
        var test = new AnalyzerTest();
        test.TestState.AdditionalFiles.Add(("BannedSymbols.txt", "GotoStatement"));
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    goto label;
                    label:
                    return;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Kind_DefaultMessage()
    {
        var test = CreateTest("GotoStatement");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#0:goto label;|}
                    label:
                    return;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "GotoStatement", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Kind_CustomMessage()
    {
        var test = CreateTest("GotoStatement; Use structured control flow instead");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#0:goto label;|}
                    label:
                    return;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "GotoStatement", ": Use structured control flow instead"));

        return test.RunAsync();
    }

    [Fact]
    public Task KindWithDescendantAxis_Diagnostic()
    {
        var test = CreateTest("//GotoStatement");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    [|goto label;|]
                    label:
                    return;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleLines_CommentsAndBlankLines()
    {
        var test = CreateTest("""
            # Control flow

            GotoStatement; Do not use goto
                LockStatement ; Use System.Threading.Lock
            """);
        test.TestCode = """
            class Sample
            {
                void M(object o)
                {
                    {|#0:lock (o) { }|}
                    {|#1:goto label;|}
                    label:
                    return;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "LockStatement", ": Use System.Threading.Lock"));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "GotoStatement", ": Do not use goto"));

        return test.RunAsync();
    }

    [Fact]
    public Task NestedNodes_Diagnostic()
    {
        var test = CreateTest("ConditionalExpression");
        test.TestCode = """
            class Sample
            {
                int M(bool a, bool b) => [|a ? [|b ? 1 : 2|] : 3|];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleFiles_Diagnostic()
    {
        var test = CreateTest("GotoStatement");
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.Shared.txt", "LockStatement"));
        test.TestCode = """
            class Sample
            {
                void M(object o)
                {
                    [|lock (o) { }|]
                    [|goto label;|]
                    label:
                    return;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SameSyntaxBannedTwice_ReportedOncePerMessage()
    {
        var test = CreateTest("""
            GotoStatement; First
            //GotoStatement; First
            GotoStatement; Second
            """);
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#0:goto label;|}
                    label:
                    return;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "GotoStatement", ": First"));
        test.ExpectedDiagnostics.Add(Diagnostic(0, "GotoStatement", ": Second"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_PrimaryConstructor_CustomMessage()
    {
        var test = CreateTest("//ClassDeclaration/ParameterList; Do not use primary constructors");
        test.TestCode = """
            class Sample{|#0:(int value)|}
            {
                Sample() : this(0) { }

                void M(int value) { }
            }

            class Other { }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "ParameterList", ": Do not use primary constructors"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_Predicate_ReportsSelectedNode()
    {
        var test = CreateTest("//ClassDeclaration[ParameterList]");
        test.TestCode = """
            [|class Sample(int value)
            {
            }|]

            class Other { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_Union_Diagnostic()
    {
        var test = CreateTest("//ClassDeclaration/ParameterList | //StructDeclaration/ParameterList");
        test.TestCode = """
            class Sample[|(int value)|];

            struct S[|(int value)|];

            record R(int Value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_NestedNodes_Diagnostic()
    {
        var test = CreateTest("//ConditionalExpression//ConditionalExpression");
        test.TestCode = """
            class Sample
            {
                int M(bool a, bool b) => a ? [|b ? 1 : 2|] : 3;
                int N(bool a) => a ? 1 : 2;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_TokenAttribute_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[@Identifier='Banned']");
        test.TestCode = """
            class Sample
            {
                [|void Banned() { }|]
                void Allowed() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_TokenListAttribute_ReportsTokens()
    {
        var test = CreateTest("//MethodDeclaration/@Modifiers[contains(., 'async')]");
        test.TestCode = """
            class Sample
            {
                {|#0:public async|} System.Threading.Tasks.Task A() => await System.Threading.Tasks.Task.Yield();
                public void B() { }
                void C() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "MethodDeclaration/@Modifiers", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemicolonInStringLiteral_IsNotASeparator()
    {
        var test = CreateTest("""//StringLiteralExpression[@Token='";"']; Use a char""");
        test.TestCode = """
            class Sample
            {
                string A() => {|#0:";"|};
                string B() => ",";
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "StringLiteralExpression", ": Use a char"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_ParentAxis_Diagnostic()
    {
        var test = CreateTest("//ReturnStatement/parent::Block/parent::MethodDeclaration/@Identifier");
        test.TestCode = """
            class Sample
            {
                int [|A|]()
                {
                    return 0;
                }

                void B() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SiblingAxes_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[preceding-sibling::FieldDeclaration and following-sibling::FieldDeclaration]/@Identifier");
        test.TestCode = """
            class Sample
            {
                int a;
                void [|A|]() { }
                int b;
                void B() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_Count_Diagnostic()
    {
        var test = CreateTest("//*[count(ParameterList/Parameter) > 5]/@Identifier");
        test.TestCode = """
            class Sample
            {
                void [|A|](int a, int b, int c, int d, int e, int f) { }
                void B(int a, int b, int c, int d, int e) { }

                [|Sample|](int a, int b, int c, int d, int e, int f) { }

                void M()
                {
                    void [|Local|](int a, int b, int c, int d, int e, int f) { }
                }
            }

            delegate void [|D|](int a, int b, int c, int d, int e, int f);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_NoMatch_NoDiagnostic()
    {
        var test = CreateTest("//ClassDeclaration/ParameterList");
        test.TestCode = """
            class Sample
            {
                void M() { }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("GotoStatment")]
    [InlineData("//GotoStatment")]
    [InlineData("gotostatement")]
    [InlineData("None")]
    [InlineData("//ClassDeclaration[")]
    [InlineData("count(//ClassDeclaration)")]
    [InlineData("8743")]
    [InlineData("//ClassDeclaration[unknown-function()]")]
    public Task InvalidEntry_ReportedInFile(string query)
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", $$"""
            # Comment
            GotoStatement
            {|#0:{{query}}; message|}
            """));
        test.TestCode = """
            class Sample
            {
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0));

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidEntry_ValidEntriesAreApplied()
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            {|#0:UnknownKind|}
            GotoStatement
            """));
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#1:goto label;|}
                    label:
                    return;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("UnknownKind", "'UnknownKind' is not a member of SyntaxKind"));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "GotoStatement", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticTypeMetadataName_Diagnostic()
    {
        var test = CreateTest("//AddExpression/*[@semantic:TypeMetadataName='System.Nullable`1']");
        test.TestCode = """
            class Sample
            {
                int? M(int? a, int? b) => [|a|] + [|b|];
                int N(int a, int b) => a + b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticTypeDocumentationId_Diagnostic()
    {
        var test = CreateTest("//AddExpression/*[@semantic:TypeDocumentationId='T:System.Nullable`1']");
        test.TestCode = """
            class Sample
            {
                int? M(int? a, int? b) => [|a|] + [|b|];
                int N(int a, int b) => a + b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticTypeReferenceId_Diagnostic()
    {
        var test = CreateTest("//ObjectCreationExpression[@semantic:TypeReferenceId='System.Text.StringBuilder']");
        test.TestCode = """
            class Sample
            {
                object A() => [|new System.Text.StringBuilder()|];
                object B() => new System.Exception();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticTypeReferenceId_IncludesTypeArguments()
    {
        var test = CreateTest("//ObjectCreationExpression[@semantic:TypeReferenceId='System.Collections.Generic.List{System.String}']");
        test.TestCode = """
            class Sample
            {
                object A() => [|new System.Collections.Generic.List<string>()|];
                object B() => new System.Collections.Generic.List<int>();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticConvertedType_Diagnostic()
    {
        var test = CreateTest("//*[@semantic:TypeReferenceId='System.Int32' and @semantic:ConvertedTypeReferenceId='System.Object']; No boxing");
        test.TestCode = """
            class Sample
            {
                object A(int value) => {|#0:value|};
                int B(int value) => value;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "IdentifierName", ": No boxing"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticSymbol_Diagnostic()
    {
        var test = CreateTest("//InvocationExpression[@semantic:Symbol='System.Console.WriteLine']");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    [|System.Console.WriteLine("a")|];
                    System.Console.Write("b");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticSymbolDocumentationId_DistinguishesOverloads()
    {
        var test = CreateTest("//InvocationExpression[@semantic:SymbolDocumentationId='M:System.Console.WriteLine(System.String)']");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    [|System.Console.WriteLine("a")|];
                    System.Console.WriteLine(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticSymbolDocumentationId_OmitsTheReturnType()
    {
        // The id is the one of the XML documentation file, which has no return type but for the conversion operators
        var test = CreateTest("//InvocationExpression[@semantic:SymbolDocumentationId='M:System.String.Substring(System.Int32)']");
        test.TestCode = """
            class Sample
            {
                void M(string value)
                {
                    [|value.Substring(1)|];
                    value.Substring(1, 2);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticSymbolDocumentationId_ConversionOperatorKeepsTheReturnType()
    {
        var test = CreateTest("//ConversionOperatorDeclaration[@semantic:SymbolDocumentationId='M:Sample.op_Implicit(Sample)~System.Int32']");
        test.TestCode = """
            class Sample
            {
                [|public static implicit operator int(Sample value) => 0;|]
                public static explicit operator string(Sample value) => "";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticReturnTypeOfADeclaration_Diagnostic()
    {
        // The return type is the only child of a method declaration that has a type
        var test = CreateTest("//MethodDeclaration/*[@semantic:TypeMetadataName='System.Threading.Tasks.Task`1']");
        test.TestCode = """
            class Sample
            {
                [|System.Threading.Tasks.Task<int>|] A() => null;
                System.Threading.Tasks.Task B() => null;
                int C(System.Threading.Tasks.Task<int> task) => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticReturnTypeOfACall_Diagnostic()
    {
        // The type of an invocation is the return type of the method it calls
        var test = CreateTest("//InvocationExpression[@semantic:TypeReferenceId='System.Int32']");
        test.TestCode = """
            class Sample
            {
                int A() => 1;
                void B() { }

                void M()
                {
                    [|A()|];
                    B();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticReturnType_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:ReturnTypeReferenceId='System.Void']");
        test.TestCode = """
            class Sample
            {
                [|void A() { }|]
                int B() => 1;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticReturnTypeMetadataName_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:ReturnTypeMetadataName='System.Threading.Tasks.Task`1']");
        test.TestCode = """
            class Sample
            {
                [|System.Threading.Tasks.Task<int> A() => null;|]
                System.Threading.Tasks.Task B() => null;
                int C(System.Threading.Tasks.Task<int> task) => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticReturnTypeDocumentationId_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:ReturnTypeDocumentationId='T:System.Threading.Tasks.Task`1']");
        test.TestCode = """
            class Sample
            {
                [|System.Threading.Tasks.Task<int> A() => null;|]
                System.Threading.Tasks.Task B() => null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticReturnTypeCombinedWithAToken_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[@Identifier='Handle' and @semantic:ReturnTypeReferenceId='System.Void']");
        test.TestCode = """
            class Sample
            {
                [|void Handle() { }|]
                int Handle(int value) => value;
                void Other() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticReturnTypeOfACalledMethod_Diagnostic()
    {
        var test = CreateTest("//InvocationExpression[@semantic:ReturnTypeReferenceId='System.Int32']");
        test.TestCode = """
            class Sample
            {
                int A() => 1;
                void B() { }

                void M()
                {
                    [|A()|];
                    B();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticTypeReferenceId_TupleIgnoresTheElementNames()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:ReturnTypeReferenceId='System.ValueTuple{System.Int32,System.String}']");
        test.TestCode = """
            class Sample
            {
                [|(int x, string y) A() => (1, "a");|]
                [|(int a, string b) B() => (1, "a");|]
                (int a, int b) C() => (1, 2);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticTypeMetadataName_TupleIsAValueTuple()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:ReturnTypeMetadataName='System.ValueTuple`2']");
        test.TestCode = """
            class Sample
            {
                [|(int x, string y) A() => (1, "a");|]
                [|(int a, string b) B() => (1, "a");|]
                int C() => 1;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticSymbolKind_Diagnostic()
    {
        var test = CreateTest("//IdentifierName[@semantic:SymbolKind='Field']");
        test.TestCode = """
            class Sample
            {
                private int _field = 0;
                private int Property { get; set; }

                int M() => [|_field|] + Property;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticContainingType_Diagnostic()
    {
        var test = CreateTest("//InvocationExpression[@semantic:ContainingTypeMetadataName='System.Console']");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    [|System.Console.Write("a")|];
                    M();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticContainingTypeDocumentationId_Diagnostic()
    {
        var test = CreateTest("//InvocationExpression[@semantic:ContainingTypeDocumentationId='T:System.Console']");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    [|System.Console.Write("a")|];
                    M();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticDeclaredSymbol_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:Symbol='Sample.Banned']");
        test.TestCode = """
            class Sample
            {
                [|void Banned() { }|]
                void Allowed() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticConstantValue_Diagnostic()
    {
        var test = CreateTest("//*[@semantic:ConstantValue='0']");
        test.TestCode = """
            class Sample
            {
                int A() => [|0|];
                int B() => 1;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticHasConstantValue_NullConstant()
    {
        var test = CreateTest("//*[@semantic:HasConstantValue and not(@semantic:ConstantValue)]");
        test.TestCode = """
            class Sample
            {
                const string A = [|null|];
                const string B = "a";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticAttribute_ReportsNodeAndPrefixedName()
    {
        var test = CreateTest("//ObjectCreationExpression/@semantic:TypeReferenceId");
        test.TestCode = """
            class Sample
            {
                object A() => {|#0:new System.Text.StringBuilder()|};
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "ObjectCreationExpression/@semantic:TypeReferenceId", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticNegativePredicate_Diagnostic()
    {
        var test = CreateTest("//ObjectCreationExpression[@semantic:TypeReferenceId!='System.Text.StringBuilder']");
        test.TestCode = """
            class Sample
            {
                object A() => new System.Text.StringBuilder();
                object B() => [|new System.Exception()|];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticAndSyntacticEntries_BothReported()
    {
        var test = CreateTest("""
            GotoStatement; Use structured control flow instead
            //ClassDeclaration/ParameterList; Do not use primary constructors
            //InvocationExpression[@semantic:ContainingTypeMetadataName='System.Console']; Use the logger
            """);
        test.TestCode = """
            class Sample{|#0:(int value)|}
            {
                void M()
                {
                    {|#1:goto label;|}
                    label:
                    {|#2:System.Console.Write("a")|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "ParameterList", ": Do not use primary constructors"));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "GotoStatement", ": Use structured control flow instead"));
        test.ExpectedDiagnostics.Add(Diagnostic(2, "InvocationExpression", ": Use the logger"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticAttributesOnEveryNodeKind_NoDiagnostic()
    {
        var test = CreateTest("//*[@semantic:TypeReferenceId='Does.Not.Exist' or @semantic:Symbol='Does.Not.Exist' or @semantic:HasConstantValue='no']");
        test.TestCode = """
            using System;
            using System.Linq;
            using Alias = System.Collections.Generic.List<int>;
            using static System.Math;

            namespace Demo;

            [AttributeUsage(AttributeTargets.All)]
            sealed class MarkerAttribute : Attribute { }

            /// <summary>Documented <see cref="Sample.M(int)"/>.</summary>
            record Point(int X, int Y)
            {
                public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
            }

            [Marker]
            unsafe class Sample<T> where T : struct
            {
                private event EventHandler? Changed;
                private int[] _values = [1, 2, 3];

                public int this[int index] => _values[index];

                ~Sample() { }

                public int M(int value)
                {
                    var query = from v in _values where v > 0 orderby v select v * 2;
                    var (a, b) = (1, "text");
                    var anonymous = new { a, b };
                    Func<int, int> lambda = static x => x + 1;

                    int Local(int x) => checked(x * 2);

                    if (b is string { Length: > 0 } s and not null)
                    {
                        Console.WriteLine(s);
                    }

                    lock (_values)
                    {
                        using var disposable = new System.IO.MemoryStream();
                    }

                    switch (value)
                    {
                        case 0:
                            goto end;
                        default:
                            break;
                    }

                end:
                    Changed?.Invoke(this, EventArgs.Empty);
                    var alias = new Alias();
                    return Local(anonymous.a) + query.Count() + alias.Count + (int)Abs(-1d);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidEntry_UnknownSemanticAttribute_Reported()
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            {|#0://ClassDeclaration[@semantic:Typo='a']|}
            """));
        test.TestCode = """
            class Sample
            {
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("//ClassDeclaration[@semantic:Typo='a']", "'Typo' is not a valid semantic attribute"));

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidEntry_UnknownNamespacePrefix_Reported()
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            {|#0://ClassDeclaration[@unknown:Type='a']|}
            """));
        test.TestCode = """
            class Sample
            {
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticNameInStringLiteral_IsNotValidated()
    {
        var test = CreateTest("//MethodDeclaration[@Identifier='semantic:Typo']");
        test.TestCode = """
            class Sample
            {
                void Allowed() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SameFileAddedTwice_ReportedOnce()
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            {|#0:UnknownKind|}
            GotoStatement
            """));
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            UnknownKind
            GotoStatement
            """));
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#1:goto label;|}
                    label:
                    return;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("UnknownKind", "'UnknownKind' is not a member of SyntaxKind"));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "GotoStatement", ""));

        return test.RunAsync();
    }
}
