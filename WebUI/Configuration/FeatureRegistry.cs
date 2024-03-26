using System.Collections.Immutable;
using System.Reflection;
using WebUI.Types;

namespace WebUI.Configuration;

public record class FeatureRegistration(
    Type Feature,
    Type? TrackData = null,
    Type? DriverData = null,
    Type? TeamData = null);

public class FeatureRegistry(IDictionary<Type, FeatureRegistration> registrations)
{
    readonly ImmutableDictionary<Type, FeatureRegistration> _registrations = registrations.ToImmutableDictionary();

    public FeatureRegistration? Get<TFeature>()
        where TFeature : FeatureBase
        => Get(typeof(TFeature));

    public FeatureRegistration? Get(Type featureType)
        => _registrations.GetValueOrDefault(featureType);

    public IReadOnlyDictionary<Type, FeatureRegistration> Registrations => _registrations;
}

public class FeatureRegistryBuilder
{
    readonly Dictionary<Type, FeatureRegistration> _registrations = [];

    public FeatureRegistry Build() => new(_registrations);

    public FeatureRegistryBuilder Register<TFeature>()
        where TFeature : IFeature
    {
        _registrations.Add(typeof(TFeature), new FeatureRegistration(
            typeof(TFeature),
            typeof(TFeature).GetInterface(nameof(IFeatureWithTrackData)) != null 
                ? (Type?)typeof(TFeature).GetProperty(nameof(IFeatureWithTrackData.TrackDataType), BindingFlags.Static | BindingFlags.Public)!.GetValue(null) 
                : null,
            typeof(TFeature).GetInterface(nameof(IFeatureWithDriverData)) != null
                ? (Type?)typeof(TFeature).GetProperty(nameof(IFeatureWithDriverData.DriverDataType), BindingFlags.Static | BindingFlags.Public)!.GetValue(null)
                : null,
            typeof(TFeature).GetInterface(nameof(IFeatureWithTeamData)) != null
                ? (Type?)typeof(TFeature).GetProperty(nameof(IFeatureWithTeamData.TeamDataType), BindingFlags.Static | BindingFlags.Public)!.GetValue(null)
                : null));
        return this;
    }
}

public static class ServiceCollectionExtensions
{
    public static FeatureRegistryBuilder ConfigureFeatures(this IServiceCollection services)
    {
        services.AddSingleton(sp => sp.GetRequiredService<FeatureRegistryBuilder>().Build());
        var featureConfigurationBuilder = new FeatureRegistryBuilder();
        services.AddSingleton(featureConfigurationBuilder);
        return featureConfigurationBuilder;
    }
}