using WebUI.Model;
using WebUI.Model.Hypermedia;

namespace WebUI.Endpoints.Resources.Hypermedia;

public class ApiInfoHypermediaFactory(GenerateHypermediaUri generateHypermediaUri) : HypermediaResourceFactory<ApiInfoResource>
{
    protected override Hyperlink GetCanonicalSelf(ApiInfoResource resource)
        => new(generateHypermediaUri(nameof(ApiInfoEndpoints.GetApiInfo)));

    protected override IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(ApiInfoResource resource)
    {
        yield return new("championships", new(generateHypermediaUri(nameof(ChampionshipEndpoints.ListChampionships))));
    }
}
