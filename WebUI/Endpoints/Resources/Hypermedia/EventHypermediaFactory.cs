using WebUI.Model.Hypermedia;

namespace WebUI.Endpoints.Resources.Hypermedia;

public class EventHypermediaFactory(GenerateHypermediaUri generateHypermediaUri) : HypermediaResourceFactory<EventResource>
{
    protected override Hyperlink GetCanonicalSelf(EventResource resource)
        => new(generateHypermediaUri(nameof(EventEndpoints.FindEventById), Params(resource)));

    protected override IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(EventResource resource)
    {
        yield return new("championship", new(generateHypermediaUri(nameof(ChampionshipEndpoints.FindChampionshipById), new { championshipId = resource.ChampionshipId.ToString("BASE36") })));
        yield return new("sessions", new(generateHypermediaUri(nameof(SessionEndpoints.ListSessions), new { championshipId = resource.ChampionshipId.ToString("BASE36"), eventId = resource.EventId.ToString("BASE36") })));
        yield return new("track", new(generateHypermediaUri(nameof(TrackEndpoints.FindTrackById), new { championshipId = resource.ChampionshipId.ToString("BASE36"), trackId = resource.TrackId.ToString("BASE36") })));
    }

    protected override IEnumerable<KeyValuePair<string, Model.Hypermedia.Action>> GetActions(EventResource resource)
    {
        yield return new("update", new("PUT", generateHypermediaUri(nameof(EventEndpoints.UpdateEventById), Params(resource))));
    }

    static object Params(EventResource resource) => new
    {
        championshipId = resource.ChampionshipId.ToString("BASE36"),
        eventId = resource.EventId.ToString("BASE36")
    };
}

public class EventsCollectionHypermediaFactory(GenerateHypermediaUri generateHypermediaUri) : HypermediaCollectionFactory<EventResourceCollection, EventResource>(new EventHypermediaFactory(generateHypermediaUri))
{
    protected override Hyperlink GetCanonicalSelf(EventResourceCollection resource)
        => new(generateHypermediaUri(nameof(EventEndpoints.ListEvents), new
        {
            championshipId = resource.ChampionshipId.ToString("BASE36")
        }));

    protected override IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(EventResourceCollection resource)
    {
        yield return new("find", new(generateHypermediaUri(nameof(EventEndpoints.FindEventById), new
        {
            championshipId = resource.ChampionshipId.ToString("BASE36"),
            eventId = "{eventId}"
        }))
        { IsTemplated = true });
    }

    protected override IEnumerable<KeyValuePair<string, Model.Hypermedia.Action>> GetActions(EventResourceCollection resource)
    {
        yield return new("create", new("POST", generateHypermediaUri(nameof(EventEndpoints.CreateEvent), new
        {
            championshipId = resource.ChampionshipId.ToString("BASE36")
        })));
    }
}