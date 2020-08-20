using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotUseServerCertificateValidationCallbackAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseServerCertificateValidationCallbackAnalyzer>();
        }

        [Fact]
        public async Task ServicePointManager_ServerCertificateValidationCallbackAsync()
        {
            const string SourceCode = @"
class Test
{
    void A()
    {
        [||]System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certification, chain, sslPolicyErrors) => throw null;
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
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task HttpClientHandler_ServerCertificateCustomValidationCallbackAsync()
        {
            const string SourceCode = @"
class Test
{
    void A()
    {
        var handler = new System.Net.Http.HttpClientHandler();
        [||]handler.ServerCertificateCustomValidationCallback += (sender, certification, chain, sslPolicyErrors) => throw null;
    }
}

namespace System.Net.Http
{
    public class HttpClientHandler
    {
        public Func<object, object, object, object, bool> ServerCertificateCustomValidationCallback { get; set; }
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
