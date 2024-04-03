using Microsoft.Extensions.DependencyInjection.Extensions;
using WebUI.Domain.ObjectStore.Internal;
using WebUI.Domain.ObjectStore.Internal.Migration;

namespace WebUI.Domain.ObjectStore;

public static class ServiceCollectionExtensions
{
    public static ObjectStoreBuilder AddObjectStore(this IServiceCollection services)
    {
        // Register the default in-memory SQLite provider when the provider is not configured
        services.TryAddSingleton<IDbConnectionProvider>(new ServiceProviderLifetimeDbConnectionProvider());
        services.TryAddSingleton(sp => sp.GetRequiredService<IDbConnectionProvider>().GetConnection());
        services.AddTransient<IObjectStore, DefaultObjectStore>();
        services.AddTransient<DataMigrator>();
        services.AddHostedService<DataMigrator>();
        services.AddOptions<ObjectStoreCollectionOptions>();
        services.AddOptions<ObjectStoreJsonOptions>();
        return new ObjectStoreBuilder(services);
    }
}
