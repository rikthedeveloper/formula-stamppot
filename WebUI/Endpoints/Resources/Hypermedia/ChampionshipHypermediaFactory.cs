using WebUI.Endpoints;
using WebUI.Model.Hypermedia;
using WebUI.Types;

namespace WebUI.Endpoints.Resources.Hypermedia;

public class ChampionshipHypermediaFactory(GenerateHypermediaUri generateHypermediaUri) : HypermediaResourceFactory<ChampionshipResource>
{
    protected override Hyperlink GetCanonicalSelf(ChampionshipResource resource)
        => Hyperlink(nameof(ChampionshipEndpoints.FindChampionshipById), resource);

    protected override IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(ChampionshipResource resource)
    {
        yield return new("tracks", Hyperlink(nameof(TrackEndpoints.ListTracks), resource));
    }

    protected override IEnumerable<KeyValuePair<string, Model.Hypermedia.Action>> GetActions(ChampionshipResource resource)
    {
        yield return new("update", Action("PUT", nameof(ChampionshipEndpoints.UpdateChampionshipById), resource));
    }

    Hyperlink Hyperlink(string routeName, ChampionshipResource resource) => new(generateHypermediaUri(routeName, Params(resource)));
    Model.Hypermedia.Action Action(string method, string routeName, ChampionshipResource resource) => new(method, generateHypermediaUri(routeName, Params(resource)));

    static object Params(ChampionshipResource resource) => new { championshipId = resource.ChampionshipId.ToString("BASE36") };
}
