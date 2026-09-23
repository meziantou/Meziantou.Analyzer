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
    public Task Query_SemanticTypeKind_Diagnostic()
    {
        var test = CreateTest("//Parameter/*[@semantic:TypeKind='Interface']; Do not take an interface");
        test.TestCode = """
            class Sample
            {
                void A({|#0:System.IDisposable|} value) { }
                void B(string value) { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "QualifiedName", ": Do not take an interface"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticTypeKind_Enum()
    {
        var test = CreateTest("//ArrowExpressionClause//IdentifierName[@semantic:TypeKind='Enum']");
        test.TestCode = """
            enum Color { Red }

            class Sample
            {
                object A(Color color) => [|color|];
                object B(int value) => value;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticContainingTypeKind_Diagnostic()
    {
        var test = CreateTest("//InvocationExpression[@semantic:ContainingTypeKind='Interface']");
        test.TestCode = """
            class Sample
            {
                void A(System.IDisposable value) => [|value.Dispose()|];
                void B(string value) => value.ToString();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticIsAbstract_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:IsAbstract='true']");
        test.TestCode = """
            abstract class Sample
            {
                [|protected abstract void A();|]
                protected virtual void B() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticIsVirtual_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:IsVirtual='true']");
        test.TestCode = """
            class Sample
            {
                [|protected virtual void A() { }|]
                protected void B() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticIsOverrideAndIsSealed_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:IsOverride='true' and @semantic:IsSealed='true']");
        test.TestCode = """
            class Base
            {
                protected virtual void A() { }
                protected virtual void B() { }
            }

            class Sample : Base
            {
                [|protected sealed override void A() { }|]
                protected override void B() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticIsAsync_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[@semantic:IsAsync='true']");
        test.TestCode = """
            using System.Threading.Tasks;

            class Sample
            {
                [|async Task A() { await Task.Yield(); }|]
                Task B() => Task.CompletedTask;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticIsAsync_NotExposedForTheSymbolsThatAreNotAMethod()
    {
        // The modifiers that only a method has are not exposed for the other symbols, so the attribute is not present
        var test = CreateTest("//VariableDeclarator[not(@semantic:IsAsync)]");
        test.TestCode = """
            class Sample
            {
                int [|_value|];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticIsExtensionMethod_Diagnostic()
    {
        var test = CreateTest("//InvocationExpression[@semantic:IsExtensionMethod='true']");
        test.TestCode = """
            static class Extensions
            {
                public static int Twice(this int value) => value * 2;

                public static int Once(int value) => value;
            }

            class Sample
            {
                int A(int value) => [|value.Twice()|];
                int B(int value) => Extensions.Once(value);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticArity_Diagnostic()
    {
        var test = CreateTest("//InvocationExpression[@semantic:Arity='2']; Too many type arguments");
        test.TestCode = """
            class Sample
            {
                void A() => {|#0:M<int, string>()|};
                void B() => M<int>();

                void M<T>() { }
                void M<T1, T2>() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "InvocationExpression", ": Too many type arguments"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticArity_NamedType()
    {
        var test = CreateTest("//ClassDeclaration[@semantic:Arity='1']");
        test.TestCode = """
            [|class Generic<T> { }|]

            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticRefKind_Parameter()
    {
        var test = CreateTest("//Parameter[@semantic:RefKind='Out']");
        test.TestCode = """
            class Sample
            {
                void M(int a, ref int b, [|out int c|]) => c = 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticRefKind_In()
    {
        // RefReadOnly is an alias of In, so the name is always In
        var test = CreateTest("//*[self::Parameter or self::VariableDeclarator][@semantic:RefKind='In']");
        test.TestCode = """
            class Sample
            {
                void M([|in int a|], int b)
                {
                    ref readonly int [|c = ref a|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticIsParams_Diagnostic()
    {
        var test = CreateTest("//Parameter[@semantic:IsParams='true']");
        test.TestCode = """
            class Sample
            {
                void M(int a, [|params int[] b|]) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticIsOptional_Diagnostic()
    {
        var test = CreateTest("//Parameter[@semantic:IsOptional='true']");
        test.TestCode = """
            class Sample
            {
                void M(int a, [|int b = 0|]) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticIsConst_FieldsAndLocals()
    {
        var test = CreateTest("//VariableDeclarator[@semantic:IsConst='true']");
        test.TestCode = """
            class Sample
            {
                const int [|A = 1|];
                static readonly int B = 2;

                void M()
                {
                    const int [|c = 3|];
                    int d = 4;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticIsReadOnly_Field()
    {
        var test = CreateTest("//VariableDeclarator[@semantic:IsReadOnly='true']");
        test.TestCode = """
            class Sample
            {
                readonly int [|_a|];
                int _b;
                const int C = 1;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemanticParameterModifiers_NotExposedForTheSymbolsThatAreNotAParameter()
    {
        // The modifiers that only a parameter has are not exposed for a field, and the ones of a field not for a parameter
        var test = CreateTest("//VariableDeclarator[not(@semantic:IsParams)] | //Parameter[not(@semantic:IsConst) and not(@semantic:IsReadOnly)]");
        test.TestCode = """
            class Sample
            {
                int [|_value|];

                void M([|int value|]) { }
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
    public Task Operation_SymbolArrayDocumentationId_Diagnostic()
    {
        // The documentation ids of a property that returns several symbols are joined by a space
        var test = CreateTest("//operation:FieldInitializer[contains(@InitializedFieldsDocumentationId, 'F:Sample._value')]");
        test.TestCode = """
            class Sample
            {
                int _value [|= 0|];
                int _other;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_TypeArrayAttribute_Diagnostic()
    {
        // A property that returns several types exposes the names a type has, not the ones of the other symbols
        var test = CreateTest("//operation:DynamicMemberReference[@TypeArgumentsMetadataName='System.Int32']");
        test.TestCode = """
            class Sample
            {
                void A(dynamic value) => [|value.M<int>|]();
                void B(dynamic value) => value.M<string>();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_ConversionIsUserDefined_Diagnostic()
    {
        var test = CreateTest("//operation:Conversion[@ConversionIsUserDefined='true']; Do not use the implicit operators");
        test.TestCode = """
            class Sample
            {
                public static implicit operator int(Sample value) => 0;

                int A(Sample value) => {|#0:value|};
                int B(short value) => value;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Conversion", ": Do not use the implicit operators"));

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_ConversionMethod_Diagnostic()
    {
        var test = CreateTest("//operation:Conversion[@ConversionMethod='Sample.op_Implicit']");
        test.TestCode = """
            class Sample
            {
                public static implicit operator int(Sample value) => 0;

                int A(Sample value) => [|value|];
                int B(short value) => value;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_ConversionIsNumeric_Diagnostic()
    {
        var test = CreateTest("//operation:Conversion[@ConversionIsNumeric='true' and @ConversionIsImplicit='true']");
        test.TestCode = """
            class Sample
            {
                long A(int value) => [|value|];
                object B(string value) => value;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_TargetMethodModifiers_Diagnostic()
    {
        var test = CreateTest("//operation:Invocation[@TargetMethodIsAbstract='true']");
        test.TestCode = """
            abstract class Base
            {
                public abstract void A();

                public void B() { }
            }

            class Sample
            {
                void M(Base value)
                {
                    [|value.A()|];
                    value.B();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_TargetMethodArity_Diagnostic()
    {
        var test = CreateTest("//operation:Invocation[@TargetMethodArity='2']");
        test.TestCode = """
            class Sample
            {
                void A() => [|M<int, string>()|];
                void B() => M<int>();

                void M<T>() { }
                void M<T1, T2>() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_ParameterRefKind_Diagnostic()
    {
        var test = CreateTest("//operation:ParameterReference[@ParameterRefKind='Ref']");
        test.TestCode = """
            class Sample
            {
                int M(ref int a, int b) => [|a|] + b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_ParameterIsParams_Diagnostic()
    {
        var test = CreateTest("//operation:ParameterReference[@ParameterIsParams='true']");
        test.TestCode = """
            class Sample
            {
                int M(int a, params int[] b) => a + [|b|].Length;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_ParameterIsOptional_Diagnostic()
    {
        var test = CreateTest("//operation:Argument[@ParameterIsOptional='true']");
        test.TestCode = """
            class Sample
            {
                void A() => M(1, [|2|]);

                void M(int a, int b = 0) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_FieldIsConstAndIsReadOnly_Diagnostic()
    {
        var test = CreateTest("//operation:FieldReference[@FieldIsConst='true' or @FieldIsReadOnly='true']");
        test.TestCode = """
            class Sample
            {
                const int A = 1;
                readonly int _b = 2;
                int _c = 3;

                int M() => [|A|] + [|_b|] + _c;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operation_LocalIsConst_Diagnostic()
    {
        var test = CreateTest("//operation:LocalReference[@LocalIsConst='true']");
        test.TestCode = """
            class Sample
            {
                int M()
                {
                    const int a = 1;
                    int b = 2;
                    return [|a|] + b;
                }
            }
            """;

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
    [Fact]
    public Task Query_AnyAttribute_Diagnostic()
    {
        var test = CreateTest("//GotoStatement[@*]");
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
    public Task Query_AnySemanticAttribute_Diagnostic()
    {
        var test = CreateTest("//InvocationExpression[@semantic:SymbolName='WriteLine' and @*]");
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
    public Task Query_AttributeAxis_Diagnostic()
    {
        var test = CreateTest("//MethodDeclaration[attribute::Identifier='Banned']");
        test.TestCode = """
            class Sample
            {
                [|void Banned()
                {
                }|]

                void Allowed()
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_AttributeAxisWildcard_Diagnostic()
    {
        var test = CreateTest("//GotoStatement[count(attribute::*) > 0]");
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
    public Task Query_SeveralEntriesShareTheSameNode_Diagnostic()
    {
        var test = CreateTest("""
            //MethodDeclaration[@Identifier='First']
            //MethodDeclaration[@semantic:ReturnTypeReferenceId='System.Int32']
            //InvocationExpression[@semantic:SymbolName='WriteLine']
            """);
        test.TestCode = """
            class Sample
            {
                [|void First()
                {
                    [|System.Console.WriteLine("a")|];
                }|]

                [|int Second() => 0;|]
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_Method_ReportedOnTheName()
    {
        var test = CreateTest("//symbol:Method[@Name='Execute']");
        test.TestCode = """
            class Sample
            {
                void {|#0:Execute|}() { }
                void Other() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_MembersAreChildrenOfTheirType()
    {
        var test = CreateTest("//symbol:NamedType[@Name='A']/symbol:Method");
        test.TestCode = """
            class A
            {
                void {|#0:M|}() { }
            }

            class B
            {
                void N() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_QualifiedNamespace()
    {
        var test = CreateTest("//symbol:Namespace[@Name='A']/symbol:Namespace[@Name='B']/symbol:NamedType");
        test.TestCode = """
            namespace A.B
            {
                class {|#0:C|} { }
            }

            namespace A
            {
                class D { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_FileScopedNamespace()
    {
        var test = CreateTest("//symbol:Namespace[@Name='A']/symbol:Namespace[@Name='B']/symbol:NamedType");
        test.TestCode = """
            namespace A.B;

            class {|#0:C|} { }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_TypesOfTheGlobalNamespaceAreRoots()
    {
        var test = CreateTest("/symbol:NamedType");
        test.TestCode = """
            class {|#0:A|} { }

            namespace N
            {
                class B { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_TypeKind()
    {
        var test = CreateTest("//symbol:NamedType[@TypeKind='Interface']");
        test.TestCode = """
            interface {|#0:ISample|} { }
            class Sample { }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_IsRecord()
    {
        var test = CreateTest("//symbol:NamedType[@IsRecord='true']");
        test.TestCode = """
            record {|#0:Sample|} { }
            class Other { }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_AccessibilityAndModifiers()
    {
        var test = CreateTest("//symbol:Method[@DeclaredAccessibility='Public' and @IsStatic='true']");
        test.TestCode = """
            public class Sample
            {
                public static void {|#0:A|}() { }
                public void B() { }
                private static void C() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_AsyncVoid()
    {
        var test = CreateTest("//symbol:Method[@IsAsync='true' and @ReturnsVoid='true']");
        test.TestCode = """
            class Sample
            {
                async void {|#0:A|}() { await System.Threading.Tasks.Task.Yield(); }
                async System.Threading.Tasks.Task B() { await System.Threading.Tasks.Task.Yield(); }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_DocumentationId()
    {
        var test = CreateTest("//symbol:Method[@DocumentationId='M:Sample.Execute(System.Int32)']");
        test.TestCode = """
            class Sample
            {
                void {|#0:Execute|}(int value) { }
                void Execute(string value) { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_ConstantValue()
    {
        var test = CreateTest("//symbol:Field[@ConstantValue='42']");
        test.TestCode = """
            class Sample
            {
                const int {|#0:A|} = 42;
                int B = 42;
                const int C = 1;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Field", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_ParameterType()
    {
        var test = CreateTest("//symbol:Parameter[@TypeSpecialType='System_Int32']");
        test.TestCode = """
            class Sample
            {
                void M(int {|#0:a|}, string b) { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Parameter", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_ParameterRefKind_In()
    {
        var test = CreateTest("//symbol:Parameter[@RefKind='In']");
        test.TestCode = """
            class Sample
            {
                void M(in int {|#0:a|}, ref int b) { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Parameter", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_Attribute_ReportedOnTheLocationOfTheSymbol()
    {
        var test = CreateTest("//symbol:Method/@Name[. = 'Execute']");
        test.TestCode = """
            class Sample
            {
                void {|#0:Execute|}() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method/@Name", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_AccessorsAreChildrenOfTheirProperty()
    {
        var test = CreateTest("//symbol:Property/symbol:Method[@MethodKind='PropertyGet']");
        test.TestCode = """
            class Sample
            {
                int Value { {|#0:get|}; set; }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_Locals()
    {
        var test = CreateTest("//symbol:Method[@Name='M']/symbol:Local");
        test.TestCode = """
            class Sample
            {
                void M(object value)
                {
                    var {|#0:a|} = 1;
                    foreach (var {|#1:item|} in new int[0]) { }
                    if (value is int {|#2:b|}) { }
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Local", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "symbol:Local", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(2, "symbol:Local", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_LocalsOfALocalFunction()
    {
        var test = CreateTest("//symbol:Method[@MethodKind='LocalFunction']/symbol:Local");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    var a = 1;
                    void Local()
                    {
                        var {|#0:b|} = 2;
                    }
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Local", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_ParametersOfALambda()
    {
        var test = CreateTest("//symbol:Method[@Name='M']/symbol:Method[@MethodKind='AnonymousFunction']/symbol:Parameter");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    System.Func<int, int> f = {|#0:x|} => x;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Parameter", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_ExplicitDefaultValue()
    {
        var test = CreateTest("//symbol:Parameter[@ExplicitDefaultValue='1']");
        test.TestCode = """
            class Sample
            {
                void M(int {|#0:a|} = 1, int b = 2, int c = 0) { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Parameter", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_RecordPositionalProperty()
    {
        var test = CreateTest("syntax(//symbol:NamedType/symbol:Property)");
        test.TestCode = """
            record Sample({|#0:int Value|});
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "Parameter", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_ImplicitlyDeclaredSymbols_NotExposed()
    {
        // A record has synthesized members, and a type without constructor has a default one
        var test = CreateTest("//symbol:Method");
        test.TestCode = """
            record Sample
            {
                public void {|#0:M|}() { }
            }

            class Other { }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_PartialTypeInSeveralFiles_OnlyTheMembersOfTheFile()
    {
        var test = CreateTest("//symbol:NamedType[count(symbol:Method) = 1]/symbol:Method");
        test.TestState.Sources.Add(("File1.cs", """
            partial class Sample
            {
                void {|#0:A|}() { }
            }
            """));
        test.TestState.Sources.Add(("File2.cs", """
            partial class Sample
            {
                void {|#1:B|}() { }
            }
            """));
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_PartialTypeDeclaredTwiceInAFile_ReportedOnEachDeclaration()
    {
        var test = CreateTest("//symbol:NamedType");
        test.TestCode = """
            partial class {|#0:Sample|} { }
            partial class {|#1:Sample|} { }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_SyntaxFunction_ReturnsTheDeclaration()
    {
        var test = CreateTest("syntax(//symbol:Method[@Name='M'])");
        test.TestCode = """
            class Sample
            {
                {|#0:void M() { }|}
                void N() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "MethodDeclaration", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_SyntaxFunction_Field()
    {
        var test = CreateTest("syntax(//symbol:Field)");
        test.TestCode = """
            class Sample
            {
                int {|#0:a|}, {|#1:b = 1|};
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "VariableDeclarator", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "VariableDeclarator", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_SyntaxFunction_PartialTypeReturnsEachDeclaration()
    {
        var test = CreateTest("syntax(//symbol:NamedType)");
        test.TestCode = """
            {|#0:partial class Sample { }|}
            {|#1:partial class Sample { }|}
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "ClassDeclaration", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "ClassDeclaration", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolTree_SyntaxFunction_InAPredicate()
    {
        var test = CreateTest("//symbol:Method[syntax(.)//GotoStatement]; Do not use goto");
        test.TestCode = """
            class Sample
            {
                void {|#0:A|}()
                {
                    goto end;
                end:
                    return;
                }

                void B() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ": Do not use goto"));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolFunction_ReturnsTheSymbolOfADeclaration()
    {
        var test = CreateTest("symbol(//ClassDeclaration[@Identifier='A'])/symbol:Method");
        test.TestCode = """
            class A
            {
                void {|#0:M|}() { }
            }

            class B
            {
                void N() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolFunction_InAPredicate()
    {
        var test = CreateTest("//MethodDeclaration[symbol(.)/symbol:Parameter[@RefKind='Out']]");
        test.TestCode = """
            class Sample
            {
                {|#0:void A(out int value) => value = 0;|}
                void B(int value) { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "MethodDeclaration", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolFunction_ReferencesToALocal_ReturnedOnce()
    {
        // The references to a symbol declared in another assembly, such as Console.WriteLine, return nothing
        var test = CreateTest("symbol(//IdentifierName)");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    var {|#0:a|} = 1;
                    System.Console.WriteLine(a + a);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Local", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolFunction_ReferenceToAGenericMethod_ReturnsItsDeclaration()
    {
        var test = CreateTest("symbol(//GenericName)");
        test.TestCode = """
            class Sample
            {
                void {|#0:M|}<T>() { }
                void N() => M<int>();
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolFunction_SelfAxis()
    {
        var test = CreateTest("symbol(//IdentifierName)[self::symbol:Local]");
        test.TestCode = """
            class Sample
            {
                int field;

                void M()
                {
                    var {|#0:a|} = field;
                    var b = a;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Local", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolFunction_PartialType_ReturnedOnce()
    {
        var test = CreateTest("//CompilationUnit[count(symbol(ClassDeclaration)) = 1]");
        test.TestCode = """
            {|#0:partial class Sample { }
            partial class Sample { }|}
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "CompilationUnit", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolFunction_AttributesOfTheSymbol()
    {
        var test = CreateTest("//MethodDeclaration[symbol(.)[@IsAsync='true']]/@Identifier");
        test.TestCode = """
            class Sample
            {
                async System.Threading.Tasks.Task {|#0:A|}() => await System.Threading.Tasks.Task.Yield();
                System.Threading.Tasks.Task B() => System.Threading.Tasks.Task.CompletedTask;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "MethodDeclaration/@Identifier", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SymbolFunction_RoundTripKeepsTheSemanticAttributes()
    {
        var test = CreateTest("symbol(//ClassDeclaration)[syntax(.)//IdentifierName[@semantic:SymbolName='WriteLine']]");
        test.TestCode = """
            class {|#0:A|}
            {
                void M() => System.Console.WriteLine();
            }

            class B
            {
                void M() => System.Console.Write("");
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SyntaxFunction_WithoutPrefix_EvaluatedOnTheOperations()
    {
        var test = CreateTest("syntax(//*)[self::InvocationExpression]");
        test.TestCode = """
            class Sample
            {
                void M() => {|#0:System.Console.Write("a")|};
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "InvocationExpression", ""));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("//symbol:Type", "'Type' is not a kind of symbol")]
    [InlineData("//symbol:Label", "'Label' is not a kind of symbol")]
    [InlineData("//symbol:Method[@semantic:Symbol='a']", "A query cannot use both the 'symbol' and the 'semantic' prefixes")]
    [InlineData("//symbol:Method | //operation:Invocation", "A query cannot use both the 'symbol' and the 'operation' prefixes")]
    [InlineData("symbol(//operation:Invocation)", "A query using the 'symbol' function cannot use the 'operation' prefix")]
    [InlineData("syntax(//*)[@semantic:Symbol='a']", "A query using the 'syntax' function without the 'symbol' prefix is evaluated on the operations, so it cannot use the 'semantic' prefix")]
    public Task InvalidEntry_SymbolQuery_Reported(string query, string message)
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", "{|#0:" + query + "|}"));
        test.TestCode = """
            class Sample
            {
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0).WithArguments(query, message));

        return test.RunAsync();
    }

    [Fact]
    public Task Implements_Symbol()
    {
        var test = CreateTest("//symbol:NamedType[implements('System.IDisposable')]");
        test.TestCode = """
            using System;

            class {|#0:A|} : IDisposable { public void Dispose() { } }
            class {|#1:B|} : A { }
            interface {|#2:I|} : IDisposable { }
            class C { }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "symbol:NamedType", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(2, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Implements_TypeItselfIsExcluded()
    {
        var test = CreateTest("//IdentifierName[implements('System.IDisposable')]");
        test.TestCode = """
            class Sample
            {
                System.IDisposable a;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("//ObjectCreationExpression[implements('System.IDisposable')]", "ObjectCreationExpression")]
    [InlineData("//operation:ObjectCreation[implements('T:System.IDisposable')]", "operation:ObjectCreation")]
    public Task Implements_TypeOfTheExpression(string query, string name)
    {
        var test = CreateTest(query);
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    _ = {|#0:new System.IO.MemoryStream()|};
                    _ = new object();
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, name, ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Implements_TypeParameterConstraint()
    {
        var test = CreateTest("//symbol:Parameter[implements('System.IDisposable')]");
        test.TestCode = """
            class Sample
            {
                void M<T, U>(T {|#0:a|}, U b) where T : System.IDisposable { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Parameter", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task InheritsFrom_BaseClasses()
    {
        var test = CreateTest("//symbol:NamedType[inherits-from('N.Base')]");
        test.TestCode = """
            namespace N;

            class Base { }
            class {|#0:Derived|} : Base { }
            class {|#1:Derived2|} : Derived { }
            interface I { }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task InheritsFrom_InterfaceIsNotABaseClass()
    {
        var test = CreateTest("//symbol:NamedType[inherits-from('System.IDisposable')]");
        test.TestCode = """
            class Sample : System.IDisposable { public void Dispose() { } }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InheritsFrom_NestedType()
    {
        var test = CreateTest("//symbol:NamedType[inherits-from('Outer+Base')]");
        test.TestCode = """
            class Outer
            {
                public class Base { }
            }

            class {|#0:Derived|} : Outer.Base { }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task IsAssignableTo_GenericDefinition()
    {
        var test = CreateTest("//symbol:Parameter[is-assignable-to('System.Collections.Generic.IEnumerable`1')]");
        test.TestCode = """
            using System.Collections.Generic;

            class Sample
            {
                void M(List<int> {|#0:a|}, IEnumerable<string> {|#1:b|}, string {|#2:c|}, int d) { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Parameter", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "symbol:Parameter", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(2, "symbol:Parameter", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task IsAssignableTo_ReferenceId_SelectsTheTypeArguments()
    {
        var test = CreateTest("//symbol:Parameter[is-assignable-to('System.Collections.Generic.IEnumerable{System.String}')]");
        test.TestCode = """
            using System.Collections.Generic;

            class Sample
            {
                void M(List<string> {|#0:a|}, List<int> b, string[] {|#1:c|}) { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Parameter", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "symbol:Parameter", ""));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.String", "MetadataName")]
    [InlineData("T:System.String", "DocumentationDeclarationId")]
    [InlineData("System.String", "DocumentationReferenceId")]
    public Task IsAssignableTo_ExplicitFormat(string name, string format)
    {
        var test = CreateTest($"//symbol:Parameter[is-assignable-to('{name}', '{format}')]");
        test.TestCode = """
            class Sample
            {
                void M(string {|#0:a|}, object b) { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Parameter", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task IsAssignableTo_ExplicitFormatDoesNotMatchTheOtherFormats()
    {
        var test = CreateTest("//symbol:Parameter[is-assignable-to('T:System.String', 'MetadataName')]");
        test.TestCode = """
            class Sample
            {
                void M(string a) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasAttribute_DerivedAttributeClass()
    {
        var test = CreateTest("//symbol:Method[has-attribute('BaseAttribute')]");
        test.TestCode = """
            using System;

            class BaseAttribute : Attribute { }
            class DerivedAttribute : BaseAttribute { }

            class Sample
            {
                [Base] void {|#0:A|}() { }
                [Derived] void {|#1:B|}() { }
                void C() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("//InvocationExpression[has-attribute('T:System.ObsoleteAttribute')]", "InvocationExpression")]
    [InlineData("//operation:Invocation[has-attribute('System.ObsoleteAttribute')]", "operation:Invocation")]
    public Task HasAttribute_CalledMethod(string query, string name)
    {
        var test = CreateTest(query);
        test.TestCode = """
            class Sample
            {
                [System.Obsolete]
                void Old() { }

                void New() { }

                void M()
                {
                    {|#0:Old()|};
                    New();
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, name, ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Attributes_ConstructorArgument()
    {
        var test = CreateTest("//symbol:Method[attributes()[@AttributeClassName='ObsoleteAttribute']/ConstructorArgument[@Position='0' and @Value='Use N']]");
        test.TestCode = """
            using System;

            class Sample
            {
                [Obsolete("Use N")] void {|#0:M|}() { }
                [Obsolete("Other")] void O() { }
                void N() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Attributes_EmptyStringHasNoValue()
    {
        var test = CreateTest("//symbol:Method[attributes()/ConstructorArgument[@IsNull='false' and not(@Value)]]");
        test.TestCode = """
            using System;

            class Sample
            {
                [Obsolete("")] void {|#0:M|}() { }
                [Obsolete(null)] void N() { }
                [Obsolete("a")] void O() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Attributes_NamedArgument()
    {
        var test = CreateTest("//symbol:NamedType[attributes()/NamedArgument[@Name='AllowMultiple' and @Value='true']]");
        test.TestCode = """
            using System;

            [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
            class {|#0:AAttribute|} : Attribute { }

            [AttributeUsage(AttributeTargets.All)]
            class BAttribute : Attribute { }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Attributes_ArrayItems()
    {
        var test = CreateTest("//symbol:Method[attributes()/ConstructorArgument/Item[@Value='2']]");
        test.TestCode = """
            using System;

            class MyAttribute : Attribute
            {
                public MyAttribute(params int[] values) { }
            }

            class Sample
            {
                [My(1, 2)] void {|#0:M|}() { }
                [My(3)] void N() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Attributes_TypeArgument()
    {
        var test = CreateTest("//symbol:Method[attributes()/ConstructorArgument[@Kind='Type' and @Value='System.String']]");
        test.TestCode = """
            using System;

            class MyAttribute : Attribute
            {
                public MyAttribute(Type type) { }
            }

            class Sample
            {
                [My(typeof(string))] void {|#0:M|}() { }
                [My(typeof(int))] void N() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Attributes_ReportedOnTheAttribute()
    {
        var test = CreateTest("attributes(//symbol:Method)[@AttributeClassMetadataName='System.ObsoleteAttribute']");
        test.TestCode = """
            using System;

            class Sample
            {
                [{|#0:Obsolete|}, CLSCompliant(false)]
                void M() { }

                [{|#1:Obsolete("a")|}]
                void N() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "AttributeData", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "AttributeData", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Attributes_SyntaxFunction()
    {
        var test = CreateTest("syntax(attributes(//symbol:Method))");
        test.TestCode = """
            class Sample
            {
                [{|#0:System.Obsolete|}]
                void M() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "Attribute", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Attributes_FromMetadata_NotReported()
    {
        var test = CreateTest("attributes(//IdentifierName[@Identifier='Obsolete' and @semantic:SymbolName='ObsoleteAttribute'])");
        test.TestCode = """
            class Sample
            {
                [System.Obsolete]
                void M() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ContainingAssembly_Name()
    {
        var test = CreateTest("//InvocationExpression[starts-with(containing-assembly(), 'System.')]");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#0:System.Console.WriteLine()|};
                    N();
                }

                void N() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "InvocationExpression", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task ContainingAssembly_Test_IgnoresTheCase()
    {
        var test = CreateTest("//operation:Invocation[containing-assembly('system.console')]");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#0:System.Console.WriteLine()|};
                    N();
                }

                void N() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Invocation", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task IsFromCurrentAssembly()
    {
        var test = CreateTest("//InvocationExpression[not(is-from-current-assembly())]");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#0:System.Console.WriteLine()|};
                    N();
                }

                void N() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "InvocationExpression", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task IsFromCurrentAssembly_Symbols()
    {
        var test = CreateTest("//symbol:NamedType[is-from-current-assembly()]");
        test.TestCode = """
            class {|#0:Sample|}
            {
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SemanticFunction_OnTheSyntaxOfAnOperation()
    {
        var test = CreateTest("syntax(//operation:Invocation)[has-attribute('System.ObsoleteAttribute')]");
        test.TestCode = """
            class Sample
            {
                [System.Obsolete]
                void Old() { }

                void M()
                {
                    {|#0:Old()|};
                    M();
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "InvocationExpression", ""));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("//GotoStatement[contains(file-path(), '/Migrations/')]")]
    [InlineData("//GotoStatement[substring(file-path(), string-length(file-path()) - 8) = 'Legacy.cs']")]
    public Task FilePath_SyntaxQuery(string query)
    {
        var test = CreateTest(query);
        test.TestState.Sources.Add(("/src/Migrations/Legacy.cs", """
            class A
            {
                void M()
                {
                    {|#0:goto end;|}
                end:
                    return;
                }
            }
            """));
        test.TestState.Sources.Add(("/src/B.cs", """
            class B
            {
                void M()
                {
                    goto end;
                end:
                    return;
                }
            }
            """));
        test.ExpectedDiagnostics.Add(Diagnostic(0, "GotoStatement", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task FilePath_SymbolQuery()
    {
        var test = CreateTest("//symbol:NamedType[not(contains(file-path(), '/Tests/'))]");
        test.TestState.Sources.Add(("/src/Tests/A.cs", """
            class A { }
            """));
        test.TestState.Sources.Add(("/src/B.cs", """
            class {|#0:B|} { }
            """));
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("//symbol:Method[overrides('M:System.Object.ToString')]")]
    [InlineData("//symbol:Method[overrides('System.Object.ToString')]")]
    public Task Overrides_Transitively(string query)
    {
        var test = CreateTest(query);
        test.TestCode = """
            class A
            {
                public override string {|#0:ToString|}() => "";
            }

            class B : A
            {
                public override string {|#1:ToString|}() => "";
                public override int GetHashCode() => 0;
            }

            class C
            {
                public new string ToString() => "";
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Overrides_DocumentationIdSelectsASingleOverload()
    {
        var test = CreateTest("//MethodDeclaration[overrides('M:System.Object.Equals(System.Object)')]/@Identifier");
        test.TestCode = """
            class Sample
            {
                public override bool {|#0:Equals|}(object obj) => false;
                public bool Equals(Sample other) => false;
                public override int GetHashCode() => 0;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "MethodDeclaration/@Identifier", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Overrides_Property()
    {
        var test = CreateTest("//symbol:Property[overrides('P:A.Value')]");
        test.TestCode = """
            abstract class A
            {
                public abstract int Value { get; }
            }

            class B : A
            {
                public override int {|#0:Value|} => 0;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Property", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task ImplementsMember_ImplicitAndExplicit()
    {
        var test = CreateTest("//symbol:Method[implements-member('M:System.IDisposable.Dispose')]");
        test.TestCode = """
            using System;

            class A : IDisposable
            {
                public void {|#0:Dispose|}() { }
            }

            class B : IDisposable
            {
                void IDisposable.{|#1:Dispose|}() { }
                public void Dispose(bool disposing) { }
            }

            class C
            {
                public void Dispose() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task ImplementsMember_GenericInterface()
    {
        var test = CreateTest("//symbol:Method[implements-member('System.IEquatable`1.Equals')]");
        test.TestCode = """
            class Sample : System.IEquatable<Sample>
            {
                public bool {|#0:Equals|}(Sample other) => false;
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task ImplementsMember_Invocation()
    {
        var test = CreateTest("//operation:Invocation[implements-member('M:System.IDisposable.Dispose')]");
        test.TestCode = """
            class Sample
            {
                void M(System.IO.MemoryStream stream)
                {
                    {|#0:stream.Dispose()|};
                    stream.Flush();
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:Invocation", ""));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("//InvocationExpression[containing-namespace('System.IO')]")]
    [InlineData("//InvocationExpression[starts-with(containing-namespace(), 'System.I')]")]
    public Task ContainingNamespace(string query)
    {
        var test = CreateTest(query);
        test.TestCode = """
            namespace N;

            class Sample
            {
                void M()
                {
                    {|#0:System.IO.File.Exists("")|};
                    System.Console.WriteLine();
                    System.Collections.Generic.EqualityComparer<int>.Default.GetHashCode(0);
                    M();
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "InvocationExpression", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task ContainingNamespace_GlobalNamespaceIsEmpty()
    {
        var test = CreateTest("//symbol:NamedType[containing-namespace() = '']");
        test.TestCode = """
            class {|#0:A|} { }

            namespace N
            {
                class B { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:NamedType", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task IsExternallyVisible()
    {
        var test = CreateTest("//symbol:Method[is-externally-visible()]");
        test.TestCode = """
            public class A
            {
                public void {|#0:M1|}() { }
                protected void {|#1:M2|}() { }
                protected internal void {|#2:M3|}() { }
                private protected void M4() { }
                internal void M5() { }
                void M6() { }
            }

            internal class B
            {
                public void M() { }
            }

            public class C
            {
                private class D
                {
                    public void M() { }
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Method", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "symbol:Method", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(2, "symbol:Method", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task IsExternallyVisible_Parameter()
    {
        var test = CreateTest("//symbol:Parameter[is-externally-visible()]");
        test.TestCode = """
            public class A
            {
                public void M(int {|#0:a|}) { }
                internal void N(int b) { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Parameter", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task IsCaptured_Symbols()
    {
        var test = CreateTest("//symbol:*[is-captured()]");
        test.TestCode = """
            using System;

            class Sample
            {
                void M(int {|#0:a|}, int b)
                {
                    var {|#1:c|} = 0;
                    var {|#2:d|} = b;
                    Func<int> f = () => a + c;
                    int Local() => d;
                    Func<int, int> g = x => x;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Parameter", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "symbol:Local", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(2, "symbol:Local", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task IsCaptured_LocalFunction()
    {
        var test = CreateTest("//symbol:Local[is-captured()]");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    var {|#0:a|} = 0;
                    var b = 0;
                    int Local() => a;
                    static int Static(int b) => b;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Local", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task IsCaptured_References()
    {
        var test = CreateTest("//operation:LocalReference[is-captured()]");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    var a = 0;
                    System.Func<int> f = () => {|#0:a|};
                    {|#1:a|}++;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "operation:LocalReference", ""));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "operation:LocalReference", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task IsCaptured_NestedLambda()
    {
        var test = CreateTest("//symbol:Local[is-captured()]");
        test.TestCode = """
            using System;

            class Sample
            {
                Func<Func<int>> F = () =>
                {
                    var {|#0:a|} = 0;
                    var b = 0;
                    return () => a;
                };
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Local", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task IsCaptured_TopLevelStatements()
    {
        var test = CreateTest("//symbol:Local[is-captured()]");
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            var {|#0:a|} = 0;
            var b = 0;
            System.Func<int> f = () => a;
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "symbol:Local", ""));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("//symbol:NamedType[implements('System.IDisposable', 'Typo')]", "'Typo' is not a valid type name format. The valid formats are 'MetadataName', 'DocumentationDeclarationId' and 'DocumentationReferenceId'")]
    [InlineData("//ClassDeclaration[has-attribute(concat('a', 'b'), \"typo\")]", "'typo' is not a valid type name format. The valid formats are 'MetadataName', 'DocumentationDeclarationId' and 'DocumentationReferenceId'")]
    public Task InvalidEntry_UnknownTypeNameFormat_Reported(string query, string message)
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", "{|#0:" + query + "|}"));
        test.TestCode = """
            class Sample
            {
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0).WithArguments(query, message));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("//symbol:NamedType[implements()]")]
    [InlineData("//symbol:NamedType[implements('a', 'MetadataName', 'b')]")]
    [InlineData("//symbol:NamedType[containing-assembly('a', 'b')]")]
    [InlineData("//symbol:NamedType[is-from-current-assembly('a')]")]
    [InlineData("attributes(//symbol:NamedType, //symbol:Method)")]
    [InlineData("//symbol:Method[overrides()]")]
    [InlineData("//symbol:Method[implements-member('a', 'b')]")]
    [InlineData("//symbol:Method[containing-namespace('a', 'b')]")]
    [InlineData("//symbol:Method[is-externally-visible('a')]")]
    [InlineData("//symbol:Local[is-captured(.)]")]
    [InlineData("//GotoStatement[file-path('a')]")]
    public Task InvalidEntry_SemanticFunctionArgumentCount_Reported(string query)
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", "{|#0:" + query + "|}"));
        test.TestCode = """
            class Sample
            {
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0241", DiagnosticSeverity.Warning).WithLocation(0));

        return test.RunAsync();
    }
}
