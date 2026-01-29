using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;

namespace BackgroundWork;

public partial class App : Application
{
    private static readonly SocketsHttpHandler HttpHandler = new()
    {
        PooledConnectionLifetime = TimeSpan.FromSeconds(15),
    };

    private static readonly HttpClient _httpClient = new(HttpHandler)
    {
        Timeout = TimeSpan.FromSeconds(5),
    };
    
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }
    
    internal static void NetLog(TraceLevel level, int threadId, string? str, Exception? exception = null)
    {
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            using var stringContent = new StringContent(
                JsonConvert.SerializeObject(
                    new
                    {
                        level,
                        threadId,
                        str,
                        exception = exception?.ToString(),
                    }),
                Encoding.UTF8,
                "application/json");
            await _httpClient.PostAsync(new Uri("http://10.2.0.26:8080/log"), stringContent).ConfigureAwait(false);
        });
    }
}