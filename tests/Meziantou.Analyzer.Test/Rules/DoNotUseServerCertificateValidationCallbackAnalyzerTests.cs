using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class DoNotUseServerCertificateValidationCallbackAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseServerCertificateValidationCallbackAnalyzer>();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task ServicePointManager_ServerCertificateValidationCallbackAsync()
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

        [TestMethod]
        public async System.Threading.Tasks.Task HttpClientHandler_ServerCertificateCustomValidationCallbackAsync()
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
