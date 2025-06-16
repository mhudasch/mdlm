namespace Verticular.DownloadManager;

internal class DefaultProtocolProviderFactory : IProtocolProviderFactory
{
    private readonly Dictionary<string, Func<string, IProtocolProvider>> protocolHandlers = [];

    public DefaultProtocolProviderFactory AddProvider(string scheme, IProtocolProvider provider)
        => this.AddProvider(scheme, new Func<string, IProtocolProvider>((_) => provider));

    public DefaultProtocolProviderFactory AddProvider(string scheme, Func<string, IProtocolProvider> providerFactory)
    {
        this.protocolHandlers.Add(scheme, providerFactory);
        return this;
    }

    public IProtocolProvider? Create(ResourceLocation location)
    {
        var scheme = location.ResourceIdentifier.Scheme;
        if (this.protocolHandlers.TryGetValue(scheme, out var protocolProviderFactory))
        {
            return protocolProviderFactory(scheme);
        }
        return null;
    }
}
