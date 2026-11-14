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

The fuzzer also finds bugs of the compiler. They cannot be fixed in this repository, so they are added as draft items to a
[GitHub project](https://github.com/users/meziantou/projects/6) instead of issues, to be reported to Roslyn later:

| Kind | Description |
|------|-------------|
| `CompilerException` | The compiler throws when it computes the diagnostics of the input, before any analyzer runs (`[assembly:(async()=>`) |
| `CompilerTimeout` | The compiler does not complete on the input, even without any analyzer (`void value(int a = value == null) { }`) |

The analyzers do not run when the compiler throws on the input, as the compilation then hangs when it is used again. This is saved as a `CompilerException`,
instead of a `Timeout` of the analyzer that happens to run. In the same way, the input of a timeout is compiled without any analyzer before looking for
the analyzer that does not complete, so a hang of the compiler is saved as a `CompilerTimeout`. All the `CompilerTimeout` findings have the same signature,
as there is no stack trace to tell them apart.

The exceptions thrown by Roslyn without a frame of the analyzers in their stack trace while an analyzer or a code fixer runs (`RoslynException`), and
the exceptions of the harness itself (`HarnessException`), are saved, but no issue is opened for them. Most `RoslynException` findings are the formatter
or the simplifier failing on the invalid code of an input, which is full of syntax errors, even before a code fix changes it. A `NullReferenceException`
thrown by Roslyn is also a `RoslynException`, even when an analyzer or a code fixer is in its stack trace, as the analyzer only called a Roslyn API that
fails on the input (e.g. `SymbolFinder` on `namespace[(async delegate`).

The signature of a `RoslynException` or a `CompilerException` does not use the analyzer or the code fixer that was running, as the same bug can be found
while any of them runs. It uses the message of the assertions of Roslyn (`Contract.ThrowIfFalse`), which contains their file and their line, and the first
two frames of Roslyn for the other exceptions. For instance, the formatter fails on many invalid inputs with `Unexpected false - file IndentBlockOperation.cs line 38`,
whatever the code fixer and the formatting rule, so this is a single finding.
The code fixers do not run when the syntax tree of the input does not contain the whole text, which the parser can produce for some invalid code
(`r e implicit operator`), as the workspace services they use, such as `SymbolFinder` and `Renamer`, throw on such a document.

All the rules are enabled, including the ones disabled by default, and the code is compiled with the .NET and ASP.NET Core
assemblies of the harness, the libraries referenced by the project file (Entity Framework Core and xUnit), and the source
generators of the .NET SDK.

Each defect has a signature, computed from its kind, the analyzer or the code fixer, the exception, and the frames of the analyzer (or of Roslyn).
The findings are saved in `findings/<signature>/`: the smallest inputs that reproduce them (`input-*.cs`), the input minimized by libFuzzer
(`minimized.cs`), and the details of the finding produced by each input (`input-*.json`, `minimized.json`, and `finding.json` for the first one).

A libFuzzer worker runs many inputs in the same process, so a finding can depend on what the previous inputs left in the process, such as
the shared state of Roslyn, instead of on the input alone. These findings do not reproduce, and are not defects of the rules. So, after fuzzing,
the inputs of each finding are replayed with `repro`, each one in a new process, the smallest first, until one of them reproduces the finding.
The inputs that do not reproduce it are renamed to `not-reproduced-*.cs` (and `not-reproduced-*.json`): they are kept for the investigation,
but they are neither minimized nor reported, and no issue is opened for a finding that none of its inputs reproduces. The crashes and the timeouts
are already replayed in a new process when they are triaged. The `verify` command does the same on a folder of findings, such as the
`fuzz-findings` artifact of a run:

```shell
dotnet run --project tests/Meziantou.Analyzer.Fuzz --configuration Release -- verify --findings fuzz-findings
```

## Weekly run

The [fuzz workflow](../../.github/workflows/fuzz.yml) fuzzes for 4 hours every Sunday, starting from the snippets of the tests and the corpus of the
previous run. It then opens an issue labeled `fuzzing` and `bug` for each new finding, at most 50 per run, except for the bugs of the compiler, which are added to the project. The signature is in the body of the issue, so:

- a finding with an open issue, or with an issue closed as not planned, is not reported again
- a finding with an issue closed as completed is reported in a new issue, as a regression, when the fuzzed commit contains the commit that
  closed the issue (the merge commit of its pull request). A run of an older commit, such as a branch that does not have the fix yet, still finds
  the defect, so the finding is not reported. When the closing commit is unknown, for instance for an issue closed manually, the finding is reported.

The draft items of the project also contain the signature, and a finding with an item is not added again, whatever its status.
So, move an item to another status instead of deleting or archiving it, which would add it again on the next run.
The token of the workflow cannot access the projects of a user, so the workflow uses the `FUZZ_PROJECT_TOKEN` secret, a token with the `project` scope.
Without it, the bugs of the compiler are not reported.

The workflow can be started manually with a shorter duration, and without opening issues. The findings, the corpus, and the logs are attached to the run.

## Reproducing a finding

The `repro` command runs the analyzers and the code fixers on files, on any OS, and prints the findings with their details:

```shell
dotnet run --project tests/Meziantou.Analyzer.Fuzz --configuration Release -- repro input.cs
```

`--only <type>` runs only some analyzers or code fixers (full type names, separated by commas), `--no-fixes` does not run the code fixers, and `--no-analyzers` only compiles the files.
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
4. The inputs of the findings are replayed in new processes, and the ones that do not reproduce their finding are set aside.
5. libFuzzer minimizes the input of each finding, except the crashes and the timeouts. This only works on x64: libfuzzer-dotnet reports
   the findings with `__builtin_trap()`, which raises `SIGTRAP` on Arm64, and libFuzzer does not handle this signal.

`pwsh tests/Meziantou.Analyzer.Fuzz/eng/report.ps1 -FindingsDirectory artifacts/fuzz/findings -DryRun` prints the issues without creating them.
With `-Repository meziantou/Meziantou.Analyzer -CommitSha <commit>`, it also skips the findings that are already known, as the weekly run does.

## Not covered yet

- The options of the rules (`.editorconfig`), which use their default values
- The Fix All providers and the refactorings
- The Roslyn versions other than the default one
- The analysis of generated code (`MEZIANTOU_ANALYZER_GENERATED_CODE`)
- The rules specific to the libraries that the project file does not reference (Moq, MSTest, Newtonsoft.Json, NUnit, Serilog, ...)
