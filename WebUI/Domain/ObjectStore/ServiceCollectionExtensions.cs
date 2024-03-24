using Microsoft.Extensions.DependencyInjection.Extensions;
using WebUI.Domain.ObjectStore.Internal;
using WebUI.Domain.ObjectStore.Internal.Migration;

namespace WebUI.Domain.ObjectStore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddObjectStore(this IServiceCollection services)
        => AddObjectStoreInternal(services, null);

    public static IServiceCollection AddObjectStore(this IServiceCollection services, Action<ObjectStoreOptions> configure)
        => AddObjectStoreInternal(services, configure);

    static IServiceCollection AddObjectStoreInternal(IServiceCollection services, Action<ObjectStoreOptions>? configure)
    {
        var opts = new ObjectStoreOptions(services);
        configure?.Invoke(opts);
        services.AddSingleton(opts);
        // Register the default in-memory SQLite provider when the provider is not configured
        services.TryAddSingleton<IDbConnectionProvider>(new ServiceProviderLifetimeDbConnectionProvider());
        services.TryAddSingleton(sp => sp.GetRequiredService<IDbConnectionProvider>().GetConnection());
        services.AddTransient<IObjectStore, DefaultObjectStore>();
        services.AddTransient<DataMigrator>();
        services.AddHostedService<DataMigrator>();
        return services;
    }
}
