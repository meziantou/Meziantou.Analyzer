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

## Implementing Roslyn analyzers

- When creating a new rule, create a new constant in `src/Meziantou.Analyzer/RuleIdentifiers.cs` using the name of the new rule. The value must be unique and incremented from the last rule.
- The analyzers must be under `src/Meziantou.Analyzer/Rules/`
- The code fixers must be under `src/Meziantou.Analyzer.CodeFixers/Rules`
- The tests must be under `tests/Meziantou.Analyzer.Test/Rules`

The analyzer must use `IOperation` or `ISymbol` to analyze the content. Only fallback to `SyntaxNode` when the other ways are not supported.

Code snippets in tests must use raw string literals (`"""`) and must be minimized to only include the necessary code to reproduce the issue. Avoid including unnecessary code that does not contribute to the test case.
When reporting a diagnostic, the snippet must use the `[|code|]` syntax or `{|id:code|}` syntax. Do not explicitly indicates lines or columns.

## Testing with different Roslyn versions

This project supports multiple versions of Roslyn to ensure compatibility with different versions of Visual Studio and the .NET SDK. The supported Roslyn versions are configured in `Directory.Build.targets`:

- `roslyn4.2` - Roslyn 4.2.0 (C# 9, C# 10)
- `roslyn4.4` - Roslyn 4.4.0 (C# 9, C# 10, C# 11)
- `roslyn4.6` - Roslyn 4.6.0 (C# 9, C# 10, C# 11)
- `roslyn4.8` - Roslyn 4.8.0 (C# 9, C# 10, C# 11, C# 12)
- `roslyn4.14` - Roslyn 4.14.0 (C# 9, C# 10, C# 11, C# 12, C# 13)
- `default` - Roslyn 5.0.0 (latest, C# 9, C# 10, C# 11, C# 12, C# 13, C# 14)

### Building with a specific Roslyn version

To build the project with a specific Roslyn version, use the `/p:RoslynVersion` MSBuild property:

```bash
dotnet build /p:RoslynVersion=roslyn4.2
dotnet build /p:RoslynVersion=roslyn4.14
dotnet build # Uses default (latest) version
```

### Running tests with a specific Roslyn version

To run tests with a specific Roslyn version, use the `/p:RoslynVersion` MSBuild property:

```bash
# Test with a specific Roslyn version
dotnet test /p:RoslynVersion=roslyn4.2
dotnet test /p:RoslynVersion=roslyn4.4
dotnet test /p:RoslynVersion=roslyn4.6
dotnet test /p:RoslynVersion=roslyn4.8
dotnet test /p:RoslynVersion=roslyn4.14

# Test with default (latest) Roslyn version
dotnet test
```

You can also filter tests to run only specific test classes or methods:

```bash
# Run only tests from a specific test class
dotnet test /p:RoslynVersion=roslyn4.2 --filter "FullyQualifiedName~UseRegexSourceGeneratorAnalyzerTests"

# Run a specific test method
dotnet test /p:RoslynVersion=roslyn4.2 --filter "FullyQualifiedName~UseRegexSourceGeneratorAnalyzerTests.NewRegex_Options"
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

The CI pipeline (`.github/workflows/ci.yml`) automatically tests with all supported Roslyn versions as part of the `build_and_test` job. All Roslyn versions must pass before a PR can be merged.
