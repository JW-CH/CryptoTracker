namespace cryptotracker.worker.Helpers;

public class SimpleHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient();
    }
}