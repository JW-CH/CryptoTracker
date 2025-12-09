namespace cryptotracker.worker.Helpers;

public class SimpleHttpClientFactory : IHttpClientFactory, IDisposable
{
    private readonly HttpClient _sharedClient = new();
    public HttpClient CreateClient(string name)
    {
        return _sharedClient;
    }
    public void Dispose()
    {
        _sharedClient.Dispose();
    }
}