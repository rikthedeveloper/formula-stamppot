using WebUI.Endpoints.Resources.Interfaces;

namespace WebUI.Model.Hypermedia;

public abstract class HypermediaResourceFactory
{
    public virtual HypermediaResource GetHypermedia(object resource)
        => new(resource,
            GetLinks(resource).Prepend(new("self", GetCanonicalSelf(resource))).ToDictionary(),
            GetActions(resource).ToDictionary(),
            GetMetadata(resource));

    protected abstract Hyperlink GetCanonicalSelf(object resource);

    protected virtual IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(object resource)
    {
        yield break;
    }

    protected virtual IEnumerable<KeyValuePair<string, Action>> GetActions(object resource)
    {
        yield break;
    }

    protected virtual Metadata? GetMetadata(object resource)
    {
        Metadata? metadata = null;
        if (resource is IVersioned versioned)
        {
            metadata ??= new();
            metadata.Version = versioned.Version;
        }

        return metadata;
    }
}

public abstract class HypermediaResourceFactory<TResource> : HypermediaResourceFactory
    where TResource : class
{
    public virtual HypermediaResource<TResource> GetHypermedia(TResource resource)
        => new(resource,
            GetLinks(resource).Prepend(new("self", GetCanonicalSelf(resource))).ToDictionary(),
            GetActions(resource).ToDictionary(),
            GetMetadata(resource));

    protected abstract Hyperlink GetCanonicalSelf(TResource resource);

    protected virtual IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(TResource resource)
    {
        yield break;
    }

    protected virtual IEnumerable<KeyValuePair<string, Action>> GetActions(TResource resource)
    {
        yield break;
    }

    protected virtual Metadata? GetMetadata(TResource resource)
        => base.GetMetadata(resource);

    public override HypermediaResource GetHypermedia(object resource)
        => GetHypermedia(resource as TResource ?? throw new ArgumentException("The resource was not of the correct type", nameof(resource)));

    protected override Hyperlink GetCanonicalSelf(object resource)
        => GetCanonicalSelf(resource as TResource ?? throw new ArgumentException("The resource was not of the correct type", nameof(resource)));

    protected override IEnumerable<KeyValuePair<string, Hyperlink>> GetLinks(object resource)
        => GetLinks(resource as TResource ?? throw new ArgumentException("The resource was not of the correct type", nameof(resource)));

    protected override IEnumerable<KeyValuePair<string, Action>> GetActions(object resource)
        => GetActions(resource as TResource ?? throw new ArgumentException("The resource was not of the correct type", nameof(resource)));

    protected override Metadata? GetMetadata(object resource)
        => GetMetadata(resource as TResource ?? throw new ArgumentException("The resource was not of the correct type", nameof(resource)));
}