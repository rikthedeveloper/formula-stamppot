namespace WebUI.Domain.ObjectStore.Internal;

public class ObjectStoreBuilder(IServiceCollection services)
{
    readonly IServiceCollection _services = services;

    public ObjectStoreBuilder UseInMemoryDb()
    {
        _services.AddSingleton<IDbConnectionProvider>(new ServiceProviderLifetimeDbConnectionProvider());
        _services.AddSingleton(sp => sp.GetRequiredService<IDbConnectionProvider>().GetConnection());
        return this;
    }

    public ObjectStoreBuilder UseFile(string connectionString)
    {
        _services.AddTransient<IDbConnectionProvider>(_ => new ScopedDbConnectionProvider(connectionString));
        _services.AddTransient(sp => sp.GetRequiredService<IDbConnectionProvider>().GetConnection());
        return this;
    }

    public ObjectStoreBuilder ConfigureJson(Action<ObjectStoreJsonOptions> configure)
    {
        _services.Configure(configure);
        return this;
    }

    public ObjectStoreBuilder ConfigureCollection(Action<ObjectStoreCollectionOptions> configure)
    {
        _services.Configure(configure);
        return this;
    }
}
