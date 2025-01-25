using SqlKata;
using System.Diagnostics.CodeAnalysis;
using WebUI.Domain.ObjectStore;
using WebUI.Domain.ObjectStore.Internal;

namespace WebUI.Endpoints.Internal.Specifications;

public class VersionMatchSpecification(ObjectVersion version) : ISpecification
{
    public VersionMatchSpecification(string version)
        : this(new(version)) { }

    public ObjectVersion Version { get; } = version;

    public Query Apply(Query query) => query.Where(DefaultCollectionFields.Version, Version.ToString());

    public static bool TryParse(string? value, [MaybeNullWhen(returnValue: false)] out VersionMatchSpecification @out)
    {
        if (ObjectVersion.TryParse(value, out var version))
        {
            @out = new(version);
            return true;
        }

        @out = null;
        return false;
    }
}
