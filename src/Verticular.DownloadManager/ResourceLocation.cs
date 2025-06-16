namespace Verticular.DownloadManager;

internal readonly record struct ResourceLocation
{
    public string? Username { get; }
    public string? Password { get; }
    public bool Authenticate { get; }
    public Uri ResourceIdentifier { get; }

    public ResourceLocation(string uri)
    {
        this.Username = null;
        this.Password = null;
        this.Authenticate = false;
        this.ResourceIdentifier = new Uri(uri);
    }

    public ResourceLocation(string uri, string username, string password)
    {
        this.Username = username;
        this.Password = password;
        this.Authenticate = true;
        this.ResourceIdentifier = new Uri(uri);
    }
}
