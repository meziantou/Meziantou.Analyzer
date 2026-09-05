**Any code you commit SHOULD compile, and new and existing tests related to the change SHOULD pass.**

You MUST make your best effort to ensure your changes satisfy those criteria before committing. If for any reason you were unable to build or test the changes, you MUST report that. You MUST NOT claim success unless all builds and tests pass as described above.

Do not complete without checking the relevant code builds and relevant tests still pass after the last edits you make. Do not simply assume that your changes fix test failures you see, actually build and run those tests again to confirm.
Also, always run `dotnet run --project src/DocumentationGenerator` to update the markdown documentation after modifying analyzer code or documentation comments. Note that this command returns a non-zero exit code if any markdown files were changed.
After running the command, review the changes made to the markdown files and ensure they are accurate and appropriate. If you make any changes to the markdown files, you MUST re-run the command to verify that no further changes are necessary.

You MUST follow all code-formatting and naming conventions defined in [`.editorconfig`](/.editorconfig).

In addition to the rules enforced by `.editorconfig`, you SHOULD:

- Prefer file-scoped namespace declarations and single-line using directives.
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.
- Prefer `?.` if applicable (e.g. `scope?.Dispose()`).
- Use `ObjectDisposedException.ThrowIf` where applicable.
- When adding new unit tests, strongly prefer to add them to existing test code files rather than creating new code files.
- When running tests, if possible use filters and check test run counts, or look at test logs, to ensure they actually ran.
- Do not finish work with any tests commented out or disabled that were not previously commented out or disabled.
- Do not update `global.json` file
- When writing tests, do not emit "Act", "Arrange" or "Assert" comments.
- There should be no trailing whitespace in any lines.
- Add a blank line before XML documentation comments (`///`) when they follow other code (methods, properties, fields, etc.).

## Documenting equivalent or similar rules

When a rule in this analyzer is equivalent to or similar to a rule in another analyzer (e.g., Roslyn IDE rules, CA rules, SonarQube rules), document the relationship in [`docs/comparison-with-other-analyzers.md`](/docs/comparison-with-other-analyzers.md):

- **Equivalent rules**: Add an entry to the "Equivalent rules" table (two columns: external rule | MA rule). Do not add a note to the individual rule's documentation file.
- **Similar rules**: Add an entry to the "Similar rules" table (three columns: external rule | MA rule | explanation of differences).

Do NOT add equivalence/similarity notes directly to individual rule documentation files (e.g., `docs/Rules/MA0158.md`).

## Maintaining Meziantou.Analyzer.Annotations

When you change any file under `src/Meziantou.Analyzer.Annotations`, you MUST:

- Update [`src/Meziantou.Analyzer.Annotations/README.md`](/src/Meziantou.Analyzer.Annotations/README.md) if the package behavior, exposed attributes, or usage guidance changed.
- Bump `<Version>` in [`src/Meziantou.Analyzer.Annotations/Meziantou.Analyzer.Annotations.csproj`](/src/Meziantou.Analyzer.Annotations/Meziantou.Analyzer.Annotations.csproj).

## Implementing Roslyn analyzers

- When creating a new rule, create a new constant in `src/Meziantou.Analyzer/RuleIdentifiers.cs` using the name of the new rule. The value must be unique and incremented from the last rule.
- When updating an existing rule, update the corresponding documentation file under `docs/Rules/` to reflect the change.
- The analyzers must be under `src/Meziantou.Analyzer/Rules/`
- The code fixers must be under `src/Meziantou.Analyzer.CodeFixers/Rules`
- The tests must be under `tests/Meziantou.Analyzer.Test/Rules`

The analyzer must use `IOperation` or `ISymbol` to analyze the content. Only fallback to `SyntaxNode` when the other ways are not supported.

### Generated code

The analyzers analyze generated code, so they must call `context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics)` in `Initialize`, and the diagnostics located in generated code are filtered when they are reported. The filtering is done by `GeneratedCodeReporting`, which sets the `DiagnosticReporter.CanReportDiagnostic` filter of `Meziantou.Framework.Roslyn` from a module initializer, so the rules must report their diagnostics with the `ReportDiagnostic` extension methods of the analysis contexts or with a `DiagnosticReporter`; `BannedSymbols.txt` makes reporting directly on a Roslyn context a compilation error, as it would bypass the filter. Add `customTags: [GeneratedCodeReporting.ReportInGeneratedCodeTag]` to the descriptor of the rules that must report in generated code by default, such as the rules whose subject is the generated file itself, and run `dotnet run --project src/DocumentationGenerator` to update the list in [`docs/generated-code.md`](/docs/generated-code.md).

Code snippets in tests must use raw string literals (`"""`) and must be minimized to only include the necessary code to reproduce the issue. Avoid including unnecessary code that does not contribute to the test case.
When reporting a diagnostic, the snippet must use the `[|code|]` syntax or `{|id:code|}` syntax. Do not explicitly indicates lines or columns.

### Code fixer best practice: validate before registering

In `RegisterCodeFixesAsync`, validate **all** conditions that could prevent the fix from being applied **before** calling `context.RegisterCodeFix`. Do not register a code fix whose action would return the document unchanged.

**Wrong** — registers the fix without validating whether it can be applied:
```csharp
public override async Task RegisterCodeFixesAsync(CodeFixContext context)
{
    var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
    var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
    if (nodeToFix is null)
        return;

    context.RegisterCodeFix(CodeAction.Create(title, ct => FixAsync(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
}

private static async Task<Document> FixAsync(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
{
    if (nodeToFix is not BinaryExpressionSyntax binaryExpression)
        return document; // Fix not applied — but it was already shown to the user!

    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
    var mySymbol = semanticModel!.Compilation.GetBestTypeByMetadataName("System.SomeType");
    if (mySymbol is null)
        return document; // Fix not applied — but it was already shown to the user!
    // ...
}
```

**Correct** — validates all conditions first, then registers the fix:
```csharp
public override async Task RegisterCodeFixesAsync(CodeFixContext context)
{
    var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
    var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
    if (nodeToFix is not BinaryExpressionSyntax binaryExpression)
        return;

    var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
    if (semanticModel is null)
        return;

    if (semanticModel.Compilation.GetBestTypeByMetadataName("System.SomeType") is null)
        return;

    context.RegisterCodeFix(CodeAction.Create(title, ct => FixAsync(context.Document, binaryExpression, ct), equivalenceKey: title), context.Diagnostics);
}

private static async Task<Document> FixAsync(Document document, BinaryExpressionSyntax binaryExpression, CancellationToken cancellationToken)
{
    var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
    // ... fix logic — all preconditions are guaranteed to hold
    return editor.GetChangedDocument();
}
```

## Testing with different Roslyn versions

This project supports multiple versions of Roslyn to ensure compatibility with different versions of Visual Studio and the .NET SDK. The supported Roslyn versions are the ones that have a project per version (see the project layout below):

- `roslyn4.8` - Roslyn 4.8.0
- `roslyn4.14` - Roslyn 4.14.0
- `roslyn5.0` - Roslyn 5.0.0
- `roslyn5.6` - Roslyn 5.6.0
- `roslyn5.9` - Roslyn 5.9.0 (the default version, used by the projects that are not specific to a Roslyn version)

The default version must always be the latest supported one, and the CI fails if it is not.

The version of the `Microsoft.CodeAnalysis.*` packages is not listed anywhere: it is derived from `RoslynVersion` in the `Directory.Build.props` of the repository root (`roslyn4.8` uses `4.8.0`). The `ROSLYN_*_OR_GREATER` and `CSHARP*_OR_GREATER` constants are not defined by this repository: they come from the [`Meziantou.Framework.Roslyn`](https://github.com/meziantou/Meziantou.Framework/blob/main/src/Meziantou.Framework.Roslyn/readme.md) package, which derives them from the version of the referenced `Microsoft.CodeAnalysis.*` packages. Note that this package considers `CSHARP15_OR_GREATER` to start at Roslyn 5.6, while the `closed` types and the union types are only supported from Roslyn 5.9: guard those with `ROSLYN_5_9_OR_GREATER`. What remains version specific in `Directory.Build.targets` is the warnings to disable: the `nullable` warnings are only reported for `DefaultRoslynVersion`, as the nullable annotations of the Roslyn APIs change from one version to another. A project named after a version that does not exist fails the restore, since no `Microsoft.CodeAnalysis.*` package matches the derived version.

### Project layout

There is one project per Roslyn version for the analyzer, the code fixers, and the tests:

- `src/Meziantou.Analyzer/Meziantou.Analyzer.roslyn<version>.csproj`
- `src/Meziantou.Analyzer.CodeFixers/Meziantou.Analyzer.CodeFixers.roslyn<version>.csproj`
- `tests/Meziantou.Analyzer.Test/Meziantou.Analyzer.Test.roslyn<version>.csproj`

Those project files are empty: the `RoslynVersion` property is derived from the name of the project file by the `Directory.Build.props` of the repository root, and everything else is defined in the `Directory.Build.props` of their folder, which is shared by all the projects of the folder. They use `TreatAsLocalProperty="RoslynVersion"` so that `/p:RoslynVersion` cannot override the version they are named after. Each test project references the analyzer and the code fixers of its own Roslyn version, using `$(RoslynVersion)` in the `Directory.Build.props` of the test folder.

There is no development project that would not be tied to a Roslyn version: `DocumentationGenerator` is not specific to a Roslyn version, so its `RoslynVersion` is the default one and it references `Meziantou.Analyzer.$(RoslynVersion).csproj`. When adding a file or a package reference, update the `Directory.Build.props` of the folder so that all Roslyn versions get it.

`Meziantou.Analyzer.Pack.csproj` discovers the `roslyn<version>` projects with a wildcard and references them with `ReferenceOutputAssembly="false" PrivateAssets="all"`, so building or packing it builds all Roslyn versions in the right order without adding them as NuGet package dependencies. Its `AddAnalyzersToPackage` target then asks each project for the assembly it produces (`GetTargetPath`) and packs it under `analyzers/dotnet/<roslyn version>/cs`. Adding support for a new Roslyn version therefore only requires adding its three projects, and the `DefaultRoslynVersion` of the `Directory.Build.props` of the repository root when the new version is the latest one. The `list_test_projects` job of the CI fails if a version is missing one of its projects, or if the default version is not the latest one.

### Output folders

The repository uses the [artifacts output layout](https://learn.microsoft.com/en-us/dotnet/core/sdk/artifacts-output), enabled in the `Directory.Build.props` of the repository root. There is no `bin` or `obj` folder next to the projects: the build output is in `artifacts/bin/<project name>/<configuration>_<target framework>`, the intermediate output in `artifacts/obj/<project name>/<configuration>_<target framework>`, and the NuGet packages in `artifacts/package/<configuration>`. This is what keeps the outputs of the Roslyn-specific projects separate, as they share their source folder.

### Building with a specific Roslyn version

To build with a specific Roslyn version, build the project of that version:

```bash
# Build a specific Roslyn version
dotnet build src/Meziantou.Analyzer/Meziantou.Analyzer.roslyn4.8.csproj
dotnet build src/Meziantou.Analyzer/Meziantou.Analyzer.roslyn4.14.csproj

# Build every Roslyn version, as all the projects are in the solution
dotnet build
```

### Running tests with a specific Roslyn version

To run tests with a specific Roslyn version, run the test project of that version:

```bash
# Test with a specific Roslyn version
dotnet test tests/Meziantou.Analyzer.Test/Meziantou.Analyzer.Test.roslyn4.8.csproj
dotnet test tests/Meziantou.Analyzer.Test/Meziantou.Analyzer.Test.roslyn4.14.csproj
dotnet test tests/Meziantou.Analyzer.Test/Meziantou.Analyzer.Test.roslyn5.0.csproj
dotnet test tests/Meziantou.Analyzer.Test/Meziantou.Analyzer.Test.roslyn5.6.csproj
dotnet test tests/Meziantou.Analyzer.Test/Meziantou.Analyzer.Test.roslyn5.9.csproj

# Test with every Roslyn version, as all the test projects are in the solution
dotnet test
```

You can also filter tests to run only specific test classes or methods:

```bash
# Run only tests from a specific test class
dotnet test tests/Meziantou.Analyzer.Test/Meziantou.Analyzer.Test.roslyn4.8.csproj --filter "FullyQualifiedName~UseRegexSourceGeneratorAnalyzerTests"

# Run a specific test method
dotnet test tests/Meziantou.Analyzer.Test/Meziantou.Analyzer.Test.roslyn4.8.csproj --filter "FullyQualifiedName~UseRegexSourceGeneratorAnalyzerTests.NewRegex_Options"
```

### Limiting the number of concurrent tests

Every test compiles code with Roslyn, so running one test per CPU thread uses a lot of memory. [`tests/Meziantou.Analyzer.Test/testconfig.json`](/tests/Meziantou.Analyzer.Test/testconfig.json) limits the number of tests running at the same time in a test project (`xUnit.maxParallelThreads`). It is shared by all the Roslyn versions, as they are in the same folder, and `Microsoft.Testing.Platform.MSBuild` copies it to the output folder of each project as `Meziantou.Analyzer.Test.testconfig.json`.

The test projects still run in parallel when they run together, which multiplies the memory usage by the number of projects. Use `--max-parallel-test-modules` to limit them:

```bash
# Run at most 2 test projects at the same time
dotnet test --max-parallel-test-modules 2
```

### When to test with multiple Roslyn versions

You SHOULD test with multiple Roslyn versions when:

- Making changes that affect analyzer or code fixer functionality
- Making changes to the test infrastructure (e.g., `ProjectBuilder` helpers)
- Making changes that use Roslyn APIs or language features that may behave differently across versions
- Making changes that involve conditional compilation based on Roslyn version (e.g., `#if CSHARP11_OR_GREATER`)

You do NOT need to test with multiple Roslyn versions when:

- Making documentation-only changes
- Making changes to build scripts or CI configuration (unless they affect version-specific builds)

### CI and Roslyn versions

The CI pipeline (`.github/workflows/ci.yml`) automatically tests with all supported Roslyn versions as part of the `build_and_test` job. Its matrix is not hardcoded: the `list_test_projects` job first checks that every Roslyn version has an analyzer, a code fixer and a test project and that the default version is the latest one, then discovers the test projects of the `tests/Meziantou.Analyzer.Test` folder, and `build_and_test` runs one job per discovered project. All Roslyn versions must pass before a PR can be merged.
