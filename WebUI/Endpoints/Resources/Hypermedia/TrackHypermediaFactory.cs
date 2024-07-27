using WebUI.Model.Hypermedia;

namespace WebUI.Endpoints.Resources.Hypermedia;

public class TrackHypermediaFactory(GenerateHypermediaUri generateHypermediaUri) : HypermediaResourceFactory<TrackResource>
{
    protected override Hyperlink GetCanonicalSelf(TrackResource resource)
        => new(generateHypermediaUri(nameof(TrackEndpoints.FindTrackById), Params(resource)));

    protected override IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(TrackResource resource)
    {
        yield return new("championship", new(generateHypermediaUri(nameof(ChampionshipEndpoints.FindChampionshipById), new { ChampionshipId = resource.ChampionshipId.ToString("BASE36") })));
    }

    protected override IEnumerable<KeyValuePair<string, Model.Hypermedia.Action>> GetActions(TrackResource resource)
    {
        yield return new("update", new("PUT", generateHypermediaUri(nameof(TrackEndpoints.UpdateTrackById), Params(resource))));
    }

    static object Params(TrackResource resource) => new
    {
        championshipId = resource.ChampionshipId.ToString("BASE36"),
        trackId = resource.TrackId.ToString("BASE36")
    };
}

public class TracksCollectionHypermediaFactory(GenerateHypermediaUri generateHypermediaUri) : HypermediaCollectionFactory<TrackResourceCollection, TrackResource>(new TrackHypermediaFactory(generateHypermediaUri))
{
    protected override Hyperlink GetCanonicalSelf(TrackResourceCollection resource)
        => new(generateHypermediaUri(nameof(TrackEndpoints.ListTracks), new
        {
            championshipId = resource.ChampionshipId.ToString("BASE36")
        }));

    protected override IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(TrackResourceCollection resource)
    {
        yield return new("find", new(generateHypermediaUri(nameof(TrackEndpoints.FindTrackById), new
        {
            championshipId = resource.ChampionshipId.ToString("BASE36"),
            trackId = "{trackId}"
        }))
        { IsTemplated = true });
    }

    protected override IEnumerable<KeyValuePair<string, Model.Hypermedia.Action>> GetActions(TrackResourceCollection resource)
    {
        yield return new("create", new("POST", generateHypermediaUri(nameof(TrackEndpoints.CreateTrack), new
        {
            championshipId = resource.ChampionshipId.ToString("BASE36")
        })));
    }
}