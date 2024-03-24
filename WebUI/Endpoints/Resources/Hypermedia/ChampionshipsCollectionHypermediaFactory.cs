using WebUI.Model.Hypermedia;

namespace WebUI.Endpoints.Resources.Hypermedia;

public class ChampionshipsCollectionHypermediaFactory(GenerateHypermediaUri generateHypermediaUri)
    : HypermediaCollectionFactory<ResourceCollection<ChampionshipResource>, ChampionshipResource>(new ChampionshipHypermediaFactory(generateHypermediaUri))
{
    protected override Hyperlink GetCanonicalSelf(ResourceCollection<ChampionshipResource> resource)
        => new(generateHypermediaUri(nameof(ChampionshipEndpoints.ListChampionships)));

    protected override IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(ResourceCollection<ChampionshipResource> resource)
    {
        yield return new("find", new(generateHypermediaUri(nameof(ChampionshipEndpoints.FindChampionshipById), new { championshipId = "{championshipId}" })) { IsTemplated = true });
    }

    protected override IEnumerable<KeyValuePair<string, Model.Hypermedia.Action>> GetActions(ResourceCollection<ChampionshipResource> resource)
    {
        yield return new("create", new("POST", generateHypermediaUri(nameof(ChampionshipEndpoints.CreateChampionship))));
    }
}
