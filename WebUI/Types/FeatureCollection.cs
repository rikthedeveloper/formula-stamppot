using System.Collections;
using System.Collections.Immutable;

namespace WebUI.Types;

// Features are the extension points of Formula Stamppot
// On each cycle, each Feature gets called at which point it processes its own result
// Each feature can define required data objects that need to be registered to a Driver, Team, or Track

public interface IFeature
{
    bool Enabled { get; }
}

public interface IFeatureWithDriverData : IFeature
{
    static abstract Type? DriverDataType { get; }
}
public interface IFeatureWithTeamData : IFeature
{
    static abstract Type? TeamDataType { get; }
}
public interface IFeatureWithTrackData : IFeature
{
    static abstract Type? TrackDataType { get; }
}

public abstract record class FeatureBase(bool Enabled) : IFeature;

public interface IFeatureData { }

public class FeatureCollection : IEnumerable<IFeature>
{
    readonly ImmutableDictionary<Type, IFeature> _features;

    public FeatureCollection()
        : this(ImmutableDictionary.Create<Type, IFeature>())
    {
    }

    public FeatureCollection(IEnumerable<IFeature> features)
        : this(features.ToImmutableDictionary(f => f.GetType()))
    {
    }

    public FeatureCollection(IDictionary<Type, IFeature> features)
        : this(features.ToImmutableDictionary())
    {
    }

    FeatureCollection(ImmutableDictionary<Type, IFeature> features)
    {
        _features = features;
    }

    public T? Get<T>()
        where T : IFeature
        => (T?)_features.GetValueOrDefault(typeof(T));

    public FeatureCollection Set<T>(T feature)
        where T : IFeature
    {
        var type = typeof(T);
        if (_features.ContainsKey(type))
            return new(_features.SetItem(type, feature));

        return new(_features.Add(type, feature));
    }

    public IEnumerator<IFeature> GetEnumerator() => _features.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
