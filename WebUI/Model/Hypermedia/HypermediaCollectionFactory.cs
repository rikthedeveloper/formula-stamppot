using WebUI.Endpoints.Resources;

namespace WebUI.Model.Hypermedia;

public abstract class HypermediaCollectionFactory<TCollection, TResource>(HypermediaResourceFactory<TResource> innerHypermediaFactory)
    : HypermediaResourceFactory
    where TCollection : ResourceCollection<TResource>
    where TResource : class
{
    public virtual HypermediaResource<ResourceCollection<HypermediaResource<TResource>>> GetHypermedia(TCollection resource)
        => new(new ResourceCollection<HypermediaResource<TResource>>(resource.Items.Select(innerHypermediaFactory.GetHypermedia)) { Total = resource.Total },
            GetLinks(resource).Prepend(new("self", GetCanonicalSelf(resource))).ToDictionary(),
            GetActions(resource).ToDictionary(),
            GetMetadata(resource));

    protected abstract Hyperlink GetCanonicalSelf(TCollection resource);

    protected virtual IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(TCollection resource)
    {
        yield break;
    }

    protected virtual IEnumerable<KeyValuePair<string, Action>> GetActions(TCollection resource)
    {
        yield break;
    }

    protected virtual Metadata? GetMetadata(TCollection resource)
        => base.GetMetadata(resource);

    public override HypermediaResource GetHypermedia(object resource)
        => GetHypermedia(resource as TCollection ?? throw new ArgumentException("The resource was not of the correct type", nameof(resource)));

    protected override Hyperlink GetCanonicalSelf(object resource)
        => GetCanonicalSelf(resource as TCollection ?? throw new ArgumentException("The resource was not of the correct type", nameof(resource)));

    protected override IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(object resource)
        => GetLinks(resource as TCollection ?? throw new ArgumentException("The resource was not of the correct type", nameof(resource)));

    protected override IEnumerable<KeyValuePair<string, Action>> GetActions(object resource)
        => GetActions(resource as TCollection ?? throw new ArgumentException("The resource was not of the correct type", nameof(resource)));

    protected override Metadata? GetMetadata(object resource)
        => GetMetadata(resource as TCollection ?? throw new ArgumentException("The resource was not of the correct type", nameof(resource)));
}
