using System.Collections;
using System.Collections.Immutable;
using WebUI.Domain;

namespace WebUI.Types;

// Features are the extension points of Formula Stamppot
// On each cycle, each Feature gets called at which point it processes its own result
// Each feature can define required data objects that need to be registered to a Driver, Team, or Track

public interface IFeature
{
    bool Enabled { get; }
    FeatureResult Apply(Session session, SessionParticipant participant, LapResult? previousLap, Random random);
}

public record class FeatureResult(TimeSpan Result);

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

public abstract record class FeatureBase(bool Enabled) : IFeature 
{
    public abstract FeatureResult Apply(Session session, SessionParticipant participant, LapResult? previousLap, Random random);
}

public interface IFeatureData { }
public interface IFeatureDriverData : IFeatureData { }
public interface IFeatureTeamData : IFeatureData { }
public interface IFeatureTrackData : IFeatureData { }

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

    public IEnumerable<FeatureResult> Apply(Session session, SessionParticipant participant, LapResult? previousLap, Random random)
    {
        foreach (var feature in this)
            if (feature.Enabled)
                yield return feature.Apply(session, participant, previousLap, random);
    }

    public IEnumerator<IFeature> GetEnumerator() => _features.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class FeatureDataCollection<TFeatureData> : IEnumerable<TFeatureData> 
    where TFeatureData : IFeatureData
{
    readonly ImmutableDictionary<Type, TFeatureData> _features;

    public FeatureDataCollection()
        : this(ImmutableDictionary.Create<Type, TFeatureData>())
    {
    }

    public FeatureDataCollection(IEnumerable<TFeatureData> data)
        : this(data.ToImmutableDictionary(data => data.GetType()))
    {
    }

    public FeatureDataCollection(IDictionary<Type, TFeatureData> features)
        : this(features.ToImmutableDictionary())
    {
    }

    protected FeatureDataCollection(ImmutableDictionary<Type, TFeatureData> features)
    {
        _features = features;
    }

    public T? Get<T>()
        where T : TFeatureData
        => (T?)_features.GetValueOrDefault(typeof(T));

    public IEnumerator<TFeatureData> GetEnumerator() => _features.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}