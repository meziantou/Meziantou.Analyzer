using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseServerCertificateValidationCallbackAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseServerCertificateValidationCallbackAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task ServicePointManager_ServerCertificateValidationCallbackAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    {|MA0039:System.Net.ServicePointManager.ServerCertificateValidationCallback|} += (sender, certification, chain, sslPolicyErrors) => throw null;
                }
            }

            namespace System.Net
            {
                public class ServicePointManager
                {
                    public static System.Net.Security.RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }
                }
            }

            namespace System.Net.Security
            {
                public delegate bool RemoteCertificateValidationCallback(object sender, object certificate, object chain, object sslPolicyErrors);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HttpClientHandler_ServerCertificateCustomValidationCallbackAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var handler = new System.Net.Http.HttpClientHandler();
                    {|MA0039:handler.ServerCertificateCustomValidationCallback|} += (sender, certification, chain, sslPolicyErrors) => throw null;
                }
            }

            namespace System.Net.Http
            {
                public class HttpClientHandler
                {
                    public Func<object, object, object, object, bool> ServerCertificateCustomValidationCallback { get; set; }
                }
            }
            """;

        return test.RunAsync();
    }
}
