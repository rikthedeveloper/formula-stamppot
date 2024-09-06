using WebUI.Model.Hypermedia;

namespace WebUI.Endpoints.Resources.Hypermedia;

public class SessionHypermediaFactory(GenerateHypermediaUri generateHypermediaUri) : HypermediaResourceFactory<SessionResource>
{
    protected override Hyperlink GetCanonicalSelf(SessionResource resource)
        => new(generateHypermediaUri(nameof(SessionEndpoints.FindSessionById), Params(resource)));

    protected override IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(SessionResource resource)
    {
        yield return new("championship", new(generateHypermediaUri(nameof(ChampionshipEndpoints.FindChampionshipById), new { ChampionshipId = resource.ChampionshipId.ToString("BASE36") })));
        yield return new("event", new(generateHypermediaUri(nameof(ChampionshipEndpoints.FindChampionshipById), new { 
            championshipId = resource.ChampionshipId.ToString("BASE36"), 
            eventId = resource.EventId.ToString("BASE36")
        })));
    }

    protected override IEnumerable<KeyValuePair<string, Model.Hypermedia.Action>> GetActions(SessionResource resource)
    {
        yield return new("update", new("PUT", generateHypermediaUri(nameof(SessionEndpoints.UpdateSessionById), Params(resource))));
        if (resource.State == Types.State.NotStarted)
            yield return new("start", new("POST", generateHypermediaUri(nameof(SessionEndpoints.UpdateSessionStateById), Params(resource))));

        if (resource.State == Types.State.Running && resource.LapCount == resource.ElapsedLaps)
            yield return new("finish", new("POST", generateHypermediaUri(nameof(SessionEndpoints.UpdateSessionStateById), Params(resource))));

        if (resource.State == Types.State.Running && resource.ElapsedLaps < resource.LapCount)
            yield return new("progress", new("POST", generateHypermediaUri(nameof(SessionEndpoints.UpdateSessionProgressById), Params(resource))));
    }

    static object Params(SessionResource resource) => new
    {
        championshipId = resource.ChampionshipId.ToString("BASE36"),
        eventId = resource.EventId.ToString("BASE36"),
        sessionId = resource.SessionId.ToString("BASE36")
    };
}

public class SessionsCollectionHypermediaFactory(GenerateHypermediaUri generateHypermediaUri) : HypermediaCollectionFactory<SessionResourceCollection, SessionResource>(new SessionHypermediaFactory(generateHypermediaUri))
{
    protected override Hyperlink GetCanonicalSelf(SessionResourceCollection resource)
        => new(generateHypermediaUri(nameof(SessionEndpoints.ListSessions), new
        {
            championshipId = resource.ChampionshipId.ToString("BASE36"),
            eventId = resource.EventId.ToString("BASE36"),
        }));

    protected override IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(SessionResourceCollection resource)
    {
        yield return new("find", new(generateHypermediaUri(nameof(SessionEndpoints.FindSessionById), new
        {
            championshipId = resource.ChampionshipId.ToString("BASE36"),
            eventId = resource.EventId.ToString("BASE36"),
            sessionId = "{sessionId}"
        }))
        { IsTemplated = true });
    }

    protected override IEnumerable<KeyValuePair<string, Model.Hypermedia.Action>> GetActions(SessionResourceCollection resource)
    {
        yield return new("create", new("POST", generateHypermediaUri(nameof(SessionEndpoints.CreateSession), new
        {
            championshipId = resource.ChampionshipId.ToString("BASE36"),
            eventId = resource.EventId.ToString("BASE36"),
        })));
    }
}