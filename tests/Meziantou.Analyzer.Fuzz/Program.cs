using Meziantou.Analyzer.Fuzz;

// libFuzzer starts the harness without arguments, and sends the inputs through shared memory
if (args.Length is 0)
{
    FuzzTarget.RunLibFuzzer();
    return 0;
}

// The assemblies are instrumented when the harness runs from the output of eng/run.ps1,
// and the instrumentation writes to a buffer that only libFuzzer allocates
SharpFuzzTrace.EnsureInitialized();

var arguments = CommandLineArguments.Parse(args.AsSpan(1));
return args[0] switch
{
    "repro" => await ReproCommand.RunAsync(arguments),
    "triage" => await Triage.RunCommandAsync(arguments),
    "corpus" => CorpusExtractor.RunCommand(arguments),
    "campaign" => await Campaign.RunAsync(arguments),
    _ => PrintUsage(),
};

static int PrintUsage()
{
    Console.Error.WriteLine("""
        Usage:
          Meziantou.Analyzer.Fuzz
              Run under libFuzzer (see eng/run.ps1)
          Meziantou.Analyzer.Fuzz repro [--only <type>[,<type>...]] [--no-fixes] <file|directory>...
              Run the analyzers and the code fixers on the inputs and print the findings
          Meziantou.Analyzer.Fuzz triage --kind <crash|timeout> --input <file> [--log <file>] [--timeout <seconds>]
              Find the rule that crashes the process or does not complete
          Meziantou.Analyzer.Fuzz corpus --tests <directory> --output <directory> --dictionary <file> [--shards <count>]
              Extract the code snippets of the tests to use as the seed corpus
          Meziantou.Analyzer.Fuzz campaign --libfuzzer <path> --tests <directory> --output <directory> [--duration <minutes>] [--workers <count>]
                                           [--timeout <seconds>] [--previous-corpus <directory>] [--merge-minutes <minutes>] [--minimize-minutes <minutes>]
              Fuzz with several libFuzzer workers, triage the crashes and minimize the findings
        """);
    return 2;
}
