# Fuzz tests

The fuzz tests run all the analyzers and the code fixers on C# code that [libFuzzer](https://llvm.org/docs/LibFuzzer.html) mutates,
starting from the code snippets of the tests. [SharpFuzz](https://github.com/Metalnem/sharpfuzz) instruments `Meziantou.Analyzer.dll`
and `Meziantou.Analyzer.CodeFixers.dll`, so libFuzzer keeps the mutations that execute new branches of the rules.

The harness reports these defects:

| Kind | Description |
|------|-------------|
| `AnalyzerException` | An analyzer or a diagnostic suppressor throws (AD0001) |
| `CodeFixException` | A code fixer throws when it registers its code fixes, or when one of them is computed or applied |
| `CodeFixBreaksCode` | The input compiles without errors, but a code fix introduces compiler errors |
| `CodeFixDoesNotChangeCode` | A code fixer registers a code fix that does not change the code |
| `Crash` | The process crashes, e.g. on a stack overflow |
| `Timeout` | An analyzer or a code fixer does not complete within 60 seconds (twice the libFuzzer timeout) |

The exceptions thrown by Roslyn without a frame of the analyzers in their stack trace (`RoslynException`), such as the formatter failing on the invalid code
of an input, and the exceptions of the harness itself (`HarnessException`) are saved, but no issue is opened for them.
The analyzers do not run when the compiler throws on the input (`[assembly:(async()=>`), as the compilation then hangs when it is used again.
This is saved as a `RoslynException`, instead of a `Timeout` of the analyzer that happens to run.
The code fixers do not run when the syntax tree of the input does not contain the whole text, which the parser can produce for some invalid code
(`r e implicit operator`), as the workspace services they use, such as `SymbolFinder` and `Renamer`, throw on such a document.

All the rules are enabled, including the ones disabled by default, and the code is compiled with the .NET and ASP.NET Core
assemblies of the harness, the libraries referenced by the project file, and the source generators of the .NET SDK.

Each defect has a signature, computed from its kind, the analyzer or the code fixer, the exception, and the frames of the analyzer.
The findings are saved in `findings/<signature>/`: the smallest inputs that reproduce them (`input-*.cs`), the input minimized by libFuzzer
(`minimized.cs`), and the details of the finding produced by each input (`input-*.json`, `minimized.json`, and `finding.json` for the first one).

## Weekly run

The [fuzz workflow](../../.github/workflows/fuzz.yml) fuzzes for 4 hours every Sunday, starting from the snippets of the tests and the corpus of the
previous run. It then opens an issue labeled `fuzzing` for each new finding, at most 50 per run. The signature is in the body of the issue, so:

- a finding with an open issue, or with an issue closed as not planned, is not reported again
- a finding with an issue closed as completed is reported in a new issue, as a regression

The workflow can be started manually with a shorter duration, and without opening issues. The findings, the corpus, and the logs are attached to the run.

## Reproducing a finding

The `repro` command runs the analyzers and the code fixers on files, on any OS, and prints the findings with their details:

```shell
dotnet run --project tests/Meziantou.Analyzer.Fuzz --configuration Release -- repro input.cs
```

`--only <type>` runs only some analyzers or code fixers (full type names, separated by commas), and `--no-fixes` does not run the code fixers.
A folder runs all its files, for instance the corpus of a run or the snippets of the tests:

```shell
dotnet run --project tests/Meziantou.Analyzer.Fuzz --configuration Release -- corpus --tests tests/Meziantou.Analyzer.Test --output artifacts/fuzz-seeds --dictionary artifacts/fuzz-seeds/csharp.dict
dotnet run --project tests/Meziantou.Analyzer.Fuzz --configuration Release -- repro artifacts/fuzz-seeds
```

## Fuzzing locally

libFuzzer requires Linux, or WSL on Windows. Install SharpFuzz and [libfuzzer-dotnet](https://github.com/Metalnem/libfuzzer-dotnet/releases), then run:

```shell
dotnet tool install --global SharpFuzz.CommandLine
pwsh tests/Meziantou.Analyzer.Fuzz/eng/run.ps1 -LibFuzzer ./libfuzzer-dotnet-ubuntu -DurationMinutes 30
```

The script publishes the harness to `artifacts/fuzz/bin`, instruments the analyzers, and starts the campaign:

1. One libFuzzer worker per core, sharing the corpus. Each worker starts with a part of the snippets of the tests.
2. The analyzers save their findings without stopping libFuzzer. When an input crashes the process or does not complete, libFuzzer stops,
   the campaign finds the responsible analyzer or code fixer by replaying the input with groups of them, disables it, and restarts the worker.
3. The corpus is merged in `artifacts/fuzz/corpus-merged`, to pass to the next campaign with `-PreviousCorpus`.
4. libFuzzer minimizes the input of each finding, except the crashes and the timeouts. This only works on x64: libfuzzer-dotnet reports
   the findings with `__builtin_trap()`, which raises `SIGTRAP` on Arm64, and libFuzzer does not handle this signal.

`pwsh tests/Meziantou.Analyzer.Fuzz/eng/report.ps1 -FindingsDirectory artifacts/fuzz/findings -DryRun` prints the issues without creating them.

## Not covered yet

- The options of the rules (`.editorconfig`), which use their default values
- The Fix All providers and the refactorings
- The Roslyn versions other than the default one
- The analysis of generated code (`MEZIANTOU_ANALYZER_GENERATED_CODE`)
