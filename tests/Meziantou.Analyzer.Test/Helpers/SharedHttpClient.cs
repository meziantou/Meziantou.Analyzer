using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.Analyzer.Test.Helpers;
internal static class SharedHttpClient
{
    public static HttpClient Instance { get; } = CreateHttpClient();

    public static async Task<bool> IsUrlAccessible(this HttpClient httpClient, Uri url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static HttpClient CreateHttpClient()
    {
#if NETCOREAPP2_1_OR_GREATER
        var socketHandler = new SocketsHttpHandler()
        {
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            PooledConnectionLifetime = TimeSpan.FromMinutes(1),
        };
#else
        var socketHandler = new HttpClientHandler();
#endif

        return new HttpClient(new HttpRetryMessageHandler(socketHandler), disposeHandler: true);
    }
    private sealed class HttpRetryMessageHandler : DelegatingHandler
    {
        public HttpRetryMessageHandler(HttpMessageHandler handler)
            : base(handler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            const int MaxRetries = 5;
            for (var i = 1; ; i++)
            {
                HttpResponseMessage result = null;
                try
                {
                    result = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    if (!IsLastAttempt(i) && ((int)result.StatusCode >= 500 || result.StatusCode is System.Net.HttpStatusCode.RequestTimeout))
                    {
                        result.Dispose();
                    }
                    else
                    {
                        return result;
                    }
                }
                catch (HttpRequestException)
                {
                    result?.Dispose();
                    if (IsLastAttempt(i))
                        throw;
                }

                await Task.Delay(100 * i, cancellationToken).ConfigureAwait(false);

                static bool IsLastAttempt(int i) => i >= MaxRetries;
            }
        }
    }
}
