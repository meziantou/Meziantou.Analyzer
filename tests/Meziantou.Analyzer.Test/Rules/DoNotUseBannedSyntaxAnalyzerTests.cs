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
        Diagnostic(markupKey, DiagnosticSeverity.Warning, name, message);

    private static DiagnosticResult Diagnostic(int markupKey, DiagnosticSeverity severity, string name, string message) =>
        new DiagnosticResult("MA0240", severity).WithLocation(markupKey).WithArguments(name, message);

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
    public Task Query_SemanticTypeIsValueType_Diagnostic()
    {
        var test = CreateTest("//*[@semantic:TypeIsValueType='true' and @semantic:ConvertedTypeMetadataName='System.Object']; No boxing");
        test.TestCode = """
            class Sample
            {
                object A(int value) => {|#0:value|};
                object B(string value) => value;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "IdentifierName", ": No boxing"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticTypeIsValueType_False()
    {
        var test = CreateTest("//ObjectCreationExpression[@semantic:TypeIsValueType='false']");
        test.TestCode = """
            class Sample
            {
                object A() => [|new System.Text.StringBuilder()|];
                object B() => new System.DateTime();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticTypeSpecialType_Diagnostic()
    {
        var test = CreateTest("//EqualsExpression/*[@semantic:TypeSpecialType='System_String']; Use string.Equals");
        test.TestCode = """
            class Sample
            {
                bool A(string a, string b) => {|#0:a|} == {|#1:b|};
                bool B(int a, int b) => a == b;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "IdentifierName", ": Use string.Equals"));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "IdentifierName", ": Use string.Equals"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticTypeSpecialType_NotExposedForOtherTypes()
    {
        var test = CreateTest("//ObjectCreationExpression[not(@semantic:TypeSpecialType)]");
        test.TestCode = """
            class Sample
            {
                object A() => [|new System.Text.StringBuilder()|];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticTypeNullableAnnotation_Diagnostic()
    {
        var test = CreateTest("//IdentifierName[@semantic:TypeNullableAnnotation='Annotated']");
        test.TestCode = """
            #nullable enable
            class Sample
            {
                string M(string? a, string b) => [|a|] ?? b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticTypeNullableAnnotation_NotExposedWhenNullableIsDisabled()
    {
        var test = CreateTest("//IdentifierName[@semantic:TypeNullableAnnotation]");
        test.TestCode = """
            #nullable disable
            class Sample
            {
                string M(string a) => a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticDeclaredAccessibility_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:DeclaredAccessibility='Public']");
        test.TestCode = """
            class Sample
            {
                [|public void A() { }|]
                private void B() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticDeclaredAccessibility_PrivateProtected()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:DeclaredAccessibility='ProtectedAndInternal']");
        test.TestCode = """
            class Sample
            {
                [|private protected void A() { }|]
                protected internal void B() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticIsStatic_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:IsStatic='true']");
        test.TestCode = """
            class Sample
            {
                [|static void A() { }|]
                void B() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticIsStatic_False()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:IsStatic='false']");
        test.TestCode = """
            class Sample
            {
                static void A() { }
                [|void B() { }|]
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticContainingSymbol_Diagnostic()
    {
        var test = CreateTest("//IdentifierName[@semantic:ContainingSymbol='Sample.M']");
        test.TestCode = """
            class Sample
            {
                private int _field;

                int M()
                {
                    int local = 0;
                    return [|local|] + _field;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticContainingSymbolDocumentationId_Diagnostic()
    {
        var test = CreateTest("//IdentifierName[@semantic:ContainingSymbolDocumentationId='M:Sample.M']");
        test.TestCode = """
            class Sample
            {
                private int _field;

                int M()
                {
                    int local = 0;
                    return [|local|] + _field;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticContainingSymbolKind_Diagnostic()
    {
        var test = CreateTest("//ClassDeclaration[@semantic:ContainingSymbolKind='Namespace']");
        test.TestCode = """
            [|class Outer
            {
                class Inner { }
            }|]
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

    [Theory]
    [InlineData("error", DiagnosticSeverity.Error)]
    [InlineData("warning", DiagnosticSeverity.Warning)]
    [InlineData("suggestion", DiagnosticSeverity.Info)]
    [InlineData("info", DiagnosticSeverity.Info)]
    [InlineData("silent", DiagnosticSeverity.Hidden)]
    [InlineData("hidden", DiagnosticSeverity.Hidden)]
    [InlineData("Error", DiagnosticSeverity.Error)]
    public Task Severity_CustomMessage(string severity, DiagnosticSeverity expectedSeverity)
    {
        var test = CreateTest($"GotoStatement;{severity}; Use structured control flow instead");
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
        test.ExpectedDiagnostics.Add(Diagnostic(0, expectedSeverity, "GotoStatement", ": Use structured control flow instead"));

        return test.RunAsync();
    }

    [Fact]
    public Task Severity_EmptyMessage()
    {
        var test = CreateTest("GotoStatement;error;");
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
        test.ExpectedDiagnostics.Add(Diagnostic(0, DiagnosticSeverity.Error, "GotoStatement", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Severity_Query()
    {
        var test = CreateTest("//ClassDeclaration/ParameterList;error;Do not use primary constructors");
        test.TestCode = """
            class Sample{|#0:(int value)|}
            {
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, DiagnosticSeverity.Error, "ParameterList", ": Do not use primary constructors"));

        return test.RunAsync();
    }

    [Fact]
    public Task Severity_None_NoDiagnostic()
    {
        var test = CreateTest("""
            GotoStatement;none;Use structured control flow instead
            """);
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
    public Task Severity_None_OtherEntriesAreApplied()
    {
        var test = CreateTest("""
            GotoStatement;none;
            LockStatement;error;
            """);
        test.TestCode = """
            class Sample
            {
                private readonly object _lock = new object();

                void M()
                {
                    {|#0:lock (_lock) { }|}
                    goto label;
                    label:
                    return;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, DiagnosticSeverity.Error, "LockStatement", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Severity_None_InvalidQueryIsStillReported()
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            {|#0:UnknownKind;none;|}
            """));
        test.TestCode = """
            class Sample
            {
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("UnknownKind", "'UnknownKind' is not a member of SyntaxKind"));

        return test.RunAsync();
    }

    [Fact]
    public Task Severity_EditorConfigOverridesTheSeverityOfTheEntries()
    {
        var test = CreateTest("GotoStatement;suggestion;");
        test.TestState.SetConfiguration("dotnet_diagnostic.MA0240.severity", "error");
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
        test.ExpectedDiagnostics.Add(Diagnostic(0, DiagnosticSeverity.Error, "GotoStatement", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Severity_SameSyntaxBannedWithTwoSeverities_BothReported()
    {
        var test = CreateTest("""
            GotoStatement;error;
            //GotoStatement;warning;
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
        test.ExpectedDiagnostics.Add(Diagnostic(0, DiagnosticSeverity.Error, "GotoStatement", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(0, DiagnosticSeverity.Warning, "GotoStatement", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidEntry_UnknownSeverity_Reported()
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            {|#0:GotoStatement;warnign;Use structured control flow instead|}
            """));
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
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("GotoStatement", "'warnign' is not a valid severity"));

        return test.RunAsync();
    }

    [Fact]
    public Task MessageContainingSeparator_IsNotASeverity()
    {
        var test = CreateTest("GotoStatement; Do not use goto; use structured control flow instead");
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
        test.ExpectedDiagnostics.Add(Diagnostic(0, "GotoStatement", ": Do not use goto; use structured control flow instead"));

        return test.RunAsync();
    }

    [Fact]
    public Task MessageStartingWithASingleWordFollowedBySeparator_IsASeverity()
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            {|#0:GotoStatement;Avoid;it is not readable|}
            """));
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
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("GotoStatement", "'Avoid' is not a valid severity"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticTypeName_Diagnostic()
    {
        var test = CreateTest("//ObjectCreationExpression[@semantic:TypeName='StringBuilder']");
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
    public Task Query_SemanticConvertedTypeName_Diagnostic()
    {
        var test = CreateTest("//*[@semantic:TypeName='Int32' and @semantic:ConvertedTypeName='Object']");
        test.TestCode = """
            class Sample
            {
                object A(int value) => [|value|];
                int B(int value) => value;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticReturnTypeName_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:ReturnTypeName='Task']");
        test.TestCode = """
            class Sample
            {
                [|System.Threading.Tasks.Task<int> A() => null;|]
                int B() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticContainingTypeName_Diagnostic()
    {
        var test = CreateTest("//InvocationExpression[@semantic:ContainingTypeName='Console']");
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
    public Task Query_SemanticSymbolName_Diagnostic()
    {
        // The short name of the symbol selects every overload, whatever the containing type
        var test = CreateTest("//InvocationExpression[@semantic:SymbolName='WriteLine']");
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
    public Task Query_SemanticContainingSymbolName_Diagnostic()
    {
        var test = CreateTest("//IdentifierName[@semantic:ContainingSymbolName='M']");
        test.TestCode = """
            class Sample
            {
                private int _field;

                int M()
                {
                    int local = 0;
                    return [|local|] + _field;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_Kind_Diagnostic()
    {
        var test = CreateTest("//operation:Invocation");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    [|System.Console.WriteLine("a")|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_Boxing_Diagnostic()
    {
        var test = CreateTest("//operation:Conversion[@IsImplicit='true' and @TypeMetadataName='System.Object']; No boxing");
        test.TestCode = """
            class Sample
            {
                object A(int value) => {|#0:value|};
                int B(int value) => value;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Conversion", ": No boxing"));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_TargetMethod_Diagnostic()
    {
        var test = CreateTest("//operation:Invocation[@TargetMethod='System.Console.WriteLine']");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#0:System.Console.WriteLine("a")|};
                    System.Console.Write("b");
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Invocation", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_TargetMethodName_Diagnostic()
    {
        // The short name of the symbol, whatever the containing type
        var test = CreateTest("//operation:Invocation[@TargetMethodName='WriteLine']");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#0:System.Console.WriteLine("a")|};
                    System.Console.Write("b");
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Invocation", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_TargetMethodDocumentationId_DistinguishesOverloads()
    {
        var test = CreateTest("//operation:Invocation[@TargetMethodDocumentationId='M:System.Console.WriteLine(System.String)']");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#0:System.Console.WriteLine("a")|};
                    System.Console.WriteLine(1);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Invocation", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_TargetMethodIsStatic_Diagnostic()
    {
        var test = CreateTest("//operation:Invocation[@TargetMethodIsStatic='true']");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#0:System.Console.Write("a")|};
                    M();
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Invocation", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_EnumAttribute_Diagnostic()
    {
        var test = CreateTest("//operation:Binary[@OperatorKind='Add']");
        test.TestCode = """
            class Sample
            {
                int A(int a, int b) => {|#0:a + b|};
                int B(int a, int b) => a - b;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Binary", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_LoopKind_Diagnostic()
    {
        var test = CreateTest("//operation:Loop[@LoopKind='While']");
        test.TestCode = """
            class Sample
            {
                void M(bool condition)
                {
                    {|#0:while (condition)
                    {
                    }|}

                    foreach (var value in new int[0])
                    {
                    }
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Loop", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_BooleanAttribute_Diagnostic()
    {
        var test = CreateTest("//operation:Invocation[@IsVirtual='true']");
        test.TestCode = """
            class Sample
            {
                void M(object value)
                {
                    {|#0:value.ToString()|};
                    System.Console.Write("a");
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Invocation", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_SymbolArrayAttribute_Diagnostic()
    {
        // The symbols of a property that returns several of them are joined by a space
        var test = CreateTest("//operation:Block[@Locals='value']");
        test.TestCode = """
            class Sample
            {
                void M()
                {|#0:{
                    int value = 0;
                }|}
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Block", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_ConstantValue_Diagnostic()
    {
        var test = CreateTest("//operation:Literal[@HasConstantValue='true' and @ConstantValue='0']");
        test.TestCode = """
            class Sample
            {
                int A() => {|#0:0|};
                int B() => 1;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Literal", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_Attribute_ReportedWithItsName()
    {
        var test = CreateTest("//operation:Invocation/@TargetMethod");
        test.TestCode = """
            class Sample
            {
                void M() => {|#0:System.Console.Write("a")|};
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Invocation/@TargetMethod", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_FieldInitializer_Diagnostic()
    {
        // The initializers are roots of the operations, like the bodies of the methods
        var test = CreateTest("//operation:FieldInitializer");
        test.TestCode = """
            class Sample
            {
                private int _value {|#0:= 1|};
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:FieldInitializer", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_AndSyntaxEntryOnTheSameSpan_BothReported()
    {
        var test = CreateTest("""
            //InvocationExpression; Syntax
            //operation:Invocation; Operation
            """);
        test.TestCode = """
            class Sample
            {
                void M() => {|#0:System.Console.Write("a")|};
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "InvocationExpression", ": Syntax"));
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Invocation", ": Operation"));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_Severity()
    {
        var test = CreateTest("//operation:Invocation;error;Use the logger");
        test.TestCode = """
            class Sample
            {
                void M() => {|#0:System.Console.Write("a")|};
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, DiagnosticSeverity.Error, "operation:Invocation", ": Use the logger"));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_NoMatch_NoDiagnostic()
    {
        var test = CreateTest("//operation:Lock");
        test.TestCode = """
            class Sample
            {
                void M() => System.Console.Write("a");
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidEntry_SemanticAttributeInAnOperationQuery_Reported()
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            {|#0://operation:Invocation[@semantic:SymbolName='a']|}
            """));
        test.TestCode = """
            class Sample
            {
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("//operation:Invocation[@semantic:SymbolName='a']", "A query cannot use both the 'semantic' and the 'operation' prefixes"));

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidEntry_UnknownOperationKind_Reported()
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            {|#0://operation:Invokation|}
            """));
        test.TestCode = """
            class Sample
            {
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("//operation:Invokation", "'Invokation' is not a kind of operation"));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_Wildcard_IsNotValidatedAsAKind()
    {
        var test = CreateTest("//operation:*[@TargetMethodName='Write']");
        test.TestCode = """
            class Sample
            {
                void M() => {|#0:System.Console.Write("a")|};
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Invocation", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_AttributesOnEveryOperationKind_NoDiagnostic()
    {
        // Every operation of the file is visited, and the attributes of every kind of operation are computed
        var test = CreateTest("//operation:*[@TypeReferenceId='Does.Not.Exist' or @TargetMethod='Does.Not.Exist' or @HasConstantValue='no' or @Locals='Does.Not.Exist']");
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
    public Task Operation_ChildOperations_Diagnostic()
    {
        // The operands of a binary operation are its child elements
        var test = CreateTest("//operation:Binary[@OperatorKind='Equals']/*[@TypeSpecialType='System_String']");
        test.TestCode = """
            class Sample
            {
                bool A(string a, string b) => {|#0:a|} == {|#1:b|};
                bool B(int a, int b) => a == b;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:ParameterReference", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "operation:ParameterReference", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_PropertyReference_Diagnostic()
    {
        var test = CreateTest("//operation:PropertyReference[@PropertyDocumentationId='P:System.DateTime.Now']");
        test.TestCode = """
            class Sample
            {
                System.DateTime A() => {|#0:System.DateTime.Now|};
                System.DateTime B() => System.DateTime.UtcNow;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:PropertyReference", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_ForEachLoop_Diagnostic()
    {
        var test = CreateTest("//operation:Loop[@LoopKind='ForEach']");
        test.TestCode = """
            class Sample
            {
                void M(int[] values)
                {
                    {|#0:foreach (var value in values)
                    {
                    }|}

                    for (var i = 0; i < 1; i++)
                    {
                    }
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Loop", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_IsImplicitFalse_ExcludesTheOperationsTheCompilerInserts()
    {
        var test = CreateTest("//operation:ParameterReference[@IsImplicit='false']");
        test.TestCode = """
            class Sample
            {
                object A(int value) => {|#0:value|};
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:ParameterReference", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidEntry_ObsoleteAliasOfAnOperationKind_Reported()
    {
        // 'BinaryOperator' is an alias of 'Binary', and the elements are named after the current name
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            {|#0://operation:BinaryOperator|}
            """));
        test.TestCode = """
            class Sample
            {
                int M(int a, int b) => a + b;
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("//operation:BinaryOperator", "'BinaryOperator' is not a kind of operation"));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_UnaryKind_Diagnostic()
    {
        // 'Unary' is the other kind that has an alias
        var test = CreateTest("//operation:Unary[@OperatorKind='Not']");
        test.TestCode = """
            class Sample
            {
                bool A(bool value) => {|#0:!value|};
                bool B(bool value) => value;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Unary", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SyntaxFunction_ContinuesOnTheSyntaxTree()
    {
        // The nodes of the syntax tree are reported with the name of their kind
        var test = CreateTest("syntax(//operation:Invocation[@TargetMethodName='Write'])//StringLiteralExpression");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    System.Console.Write({|#0:"a"|});
                    System.Console.WriteLine("b");
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "StringLiteralExpression", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SyntaxFunction_SelectsTheNodeOfTheOperation()
    {
        var test = CreateTest("syntax(//operation:Invocation)");
        test.TestCode = """
            class Sample
            {
                void M() => {|#0:System.Console.Write("a")|};
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "InvocationExpression", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SyntaxFunction_InAPredicate()
    {
        // 'syntax(.)' is the node of the operation the predicate is evaluated on
        var test = CreateTest("//operation:Invocation[syntax(.)//InterpolatedStringExpression]; Do not interpolate");
        test.TestCode = """
            class Sample
            {
                void M(int value)
                {
                    {|#0:System.Console.Write($"a{value}")|};
                    System.Console.Write("b");
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Invocation", ": Do not interpolate"));

        return test.RunAsync();
    }

    [Fact]
    public Task SyntaxFunction_ReachesTheSyntaxThatHasNoOperation()
    {
        // The verbatim strings are only visible in the syntax tree, as an operation has the value of the literal
        var test = CreateTest("syntax(//operation:Invocation)//StringLiteralExpression/@Token[starts-with(., '@')]");
        test.TestCode = """"
            class Sample
            {
                void M()
                {
                    System.Console.Write({|#0:@"a"|});
                    System.Console.Write("b");
                }
            }
            """";
        test.ExpectedDiagnostics.Add(Diagnostic(0, "StringLiteralExpression/@Token", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SyntaxFunction_OperationsSharingASyntaxNode_ReportedOnce()
    {
        // The conversion and its operand have the same syntax node
        var test = CreateTest("syntax(//operation:Conversion | //operation:ParameterReference)");
        test.TestCode = """
            class Sample
            {
                object A(int value) => {|#0:value|};
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "IdentifierName", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SyntaxFunction_NoOperation_NoDiagnostic()
    {
        var test = CreateTest("syntax(//operation:Lock)//StringLiteralExpression");
        test.TestCode = """
            class Sample
            {
                void M() => System.Console.Write("a");
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidEntry_UnknownFunctionInAnOperationQuery_Reported()
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            {|#0:unknown-function(//operation:Invocation)|}
            """));
        test.TestCode = """
            class Sample
            {
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0));

        return test.RunAsync();
    }
}
