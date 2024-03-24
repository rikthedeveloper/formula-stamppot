using SqlKata;
using WebUI.Domain.ObjectStore;
using WebUI.Domain.ObjectStore.Internal;

namespace WebUI.Endpoints.Internal.Specifications;

public class VersionMatchSpecification(ObjectVersion version) : ISpecification
{
    public VersionMatchSpecification(string version)
        : this(new(version)) { }

    public ObjectVersion Version { get; } = version;

    public Query Apply(Query query) => query.Where(DefaultCollectionFields.Version, Version.ToString());
}
