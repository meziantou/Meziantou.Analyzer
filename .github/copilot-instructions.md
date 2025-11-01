**Any code you commit SHOULD compile, and new and existing tests related to the change SHOULD pass.**

You MUST make your best effort to ensure your changes satisfy those criteria before committing. If for any reason you were unable to build or test the changes, you MUST report that. You MUST NOT claim success unless all builds and tests pass as described above.

Do not complete without checking the relevant code builds and relevant tests still pass after the last edits you make. Do not simply assume that your changes fix test failures you see, actually build and run those tests again to confirm.
Also, always run `dotnet run --project src/DocumentationGenerator` to update the markdown documentation after modifying analyzer code or documentation comments.
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
