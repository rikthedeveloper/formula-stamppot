using WebUI.Model.Hypermedia;

namespace WebUI.Endpoints.Resources.Hypermedia;

public class DriverHypermediaFactory(GenerateHypermediaUri generateHypermediaUri) : HypermediaResourceFactory<DriverResource>
{
    protected override Hyperlink GetCanonicalSelf(DriverResource resource)
        => new(generateHypermediaUri(nameof(DriverEndpoints.FindDriverById), Params(resource)));

    protected override IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(DriverResource resource)
    {
        yield return new("championship", new(generateHypermediaUri(nameof(ChampionshipEndpoints.FindChampionshipById), new { ChampionshipId = resource.ChampionshipId.ToString("BASE36") })));
    }

    protected override IEnumerable<KeyValuePair<string, Model.Hypermedia.Action>> GetActions(DriverResource resource)
    {
        yield return new("update", new("PUT", generateHypermediaUri(nameof(DriverEndpoints.UpdateDriverById), Params(resource))));
    }

    static object Params(DriverResource resource) => new
    {
        championshipId = resource.ChampionshipId.ToString("BASE36"),
        driverId = resource.DriverId.ToString("BASE36")
    };
}

public class DriversCollectionHypermediaFactory(GenerateHypermediaUri generateHypermediaUri) : HypermediaCollectionFactory<DriverResourceCollection, DriverResource>(new DriverHypermediaFactory(generateHypermediaUri))
{
    protected override Hyperlink GetCanonicalSelf(DriverResourceCollection resource)
        => new(generateHypermediaUri(nameof(DriverEndpoints.ListDrivers), new
        {
            championshipId = resource.ChampionshipId.ToString("BASE36")
        }));

    protected override IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(DriverResourceCollection resource)
    {
        yield return new("find", new(generateHypermediaUri(nameof(DriverEndpoints.FindDriverById), new
        {
            championshipId = resource.ChampionshipId.ToString("BASE36"),
            driverId = "{driverId}"
        }))
        { IsTemplated = true });
    }

    protected override IEnumerable<KeyValuePair<string, Model.Hypermedia.Action>> GetActions(DriverResourceCollection resource)
    {
        yield return new("create", new("POST", generateHypermediaUri(nameof(DriverEndpoints.CreateDriver), new
        {
            championshipId = resource.ChampionshipId.ToString("BASE36")
        })));
    }
}