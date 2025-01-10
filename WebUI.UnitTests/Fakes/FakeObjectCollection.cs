using System.Reflection;
using WebUI.Domain;
using WebUI.Domain.ObjectStore;
using WebUI.Endpoints.Internal.Specifications;

namespace WebUI.UnitTests.Fakes;

public class FakeObjectCollection<T> : IObjectCollection<T> where T : class
{
    private readonly List<ObjectRecord<T>> _records;

    public FakeObjectCollection(IEnumerable<ObjectRecord<T>> records)
    {
        _records = records.ToList();
    }

    public Task<IEnumerable<ObjectRecord<T>>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ObjectRecord<T>>>(_records);
    }

    public Task<IEnumerable<ObjectRecord<T>>> ListAsync(ISpecification[] specification, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_records.Where(RecordSatisfies(specification)));
    }

    public IAsyncEnumerable<ObjectRecord<T>> StreamAsync(CancellationToken cancellationToken = default)
    {
        return _records.ToAsyncEnumerable();
    }

    public IAsyncEnumerable<ObjectRecord<T>> StreamAsync(ISpecification[] specification, CancellationToken cancellationToken = default)
    {
        return _records.ToAsyncEnumerable();
    }

    public Task<ObjectRecord<T>?> FindAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_records.FirstOrDefault());
    }

    public Task<ObjectRecord<T>?> FindAsync(ISpecification[] specification, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_records.FirstOrDefault(RecordSatisfies(specification)));
    }

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_records.Any());
    }

    public Task<bool> ExistsAsync(ISpecification[] specification, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_records.Any(RecordSatisfies(specification)));
    }

    public Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)_records.Count);
    }

    public Task<long> CountAsync(ISpecification[] specification, CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)_records.Count(RecordSatisfies(specification)));
    }

    public Task<int> InsertAsync(T model, CancellationToken cancellationToken = default)
    {
        _records.Add(new ObjectRecord<T>(model, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new ObjectVersion(FakeObjectStore.DefaultObjectVersion)));
        return Task.FromResult(1);
    }

    public Task<int> UpdateAsync(ISpecification[] specification, T model, CancellationToken cancellationToken = default)
    {
        var recordToUpdate = _records.FirstOrDefault(RecordSatisfies(specification));
        if (recordToUpdate != null)
        {
            if (specification.OfType<VersionMatchSpecification>().Any())
            {
                var versionSpec = specification.OfType<VersionMatchSpecification>().First();
                if (recordToUpdate.Version != versionSpec.Version)
                    return Task.FromResult(0);
            }

            _records[_records.IndexOf(recordToUpdate)] = new ObjectRecord<T>(model, recordToUpdate.Created, DateTimeOffset.UtcNow, new ObjectVersion(FakeObjectStore.DefaultObjectVersion));
            return Task.FromResult(1);
        }
        return Task.FromResult(0);
    }

    static Func<ObjectRecord<T>, bool> RecordSatisfies(ISpecification[] specifications)
    {
        return (ObjectRecord<T> record) =>
        {
            foreach (var specification in specifications)
            {
                var specHandlerType = typeof(ISpecificationHandler<,>);
                var argsCount = specHandlerType.GenericTypeArguments.Length;
                var expectedSpecificationType = specHandlerType.MakeGenericType(specification.GetType(), record.GetType());
                var handler = specificationHandlers.FirstOrDefault(h => h.GetType().IsAssignableTo(expectedSpecificationType));
                if (handler == null)
                    continue;
                var handlerMethod = handler.GetType().GetMethod(nameof(ISpecificationHandler<ISpecification, object>.SpecificationSatisfies), [specification.GetType(), record.GetType()])!;
                if (!(bool)handlerMethod.Invoke(handler, [specification, record])!)
                    return false;
            }
            return true;
        };
    }

    static IList<object> specificationHandlers = [
        new VersionMatchSpecificationHandler(),
        new ChampionshipIdSpecificationHandler(),
        new DriverIdSpecificationHandler(),
        new EventIdSpecificationHandler(),
        new SessionIdSpecificationHandler(),
        new TrackIdSpecificationHandler()
    ];
}

interface ISpecificationHandler<TSpecification, TObject> where TSpecification : ISpecification
{
    bool SpecificationSatisfies(TSpecification specification, TObject record);
}

class VersionMatchSpecificationHandler : ISpecificationHandler<VersionMatchSpecification, ObjectRecord>
{
    public bool SpecificationSatisfies(VersionMatchSpecification specification, ObjectRecord record) => record.Version == specification.Version;
}

class ChampionshipIdSpecificationHandler : ISpecificationHandler<ChampionshipIdSpecification, ObjectRecord<Championship>>,
    ISpecificationHandler<ChampionshipIdSpecification, ObjectRecord<Driver>>,
    ISpecificationHandler<ChampionshipIdSpecification, ObjectRecord<Event>>,
    ISpecificationHandler<ChampionshipIdSpecification, ObjectRecord<Session>>,
    ISpecificationHandler<ChampionshipIdSpecification, ObjectRecord<Team>>,
    ISpecificationHandler<ChampionshipIdSpecification, ObjectRecord<Track>>
{
    public bool SpecificationSatisfies(ChampionshipIdSpecification specification, ObjectRecord<Championship> record) => record.Object.ChampionshipId == specification.ChampionshipId;
    public bool SpecificationSatisfies(ChampionshipIdSpecification specification, ObjectRecord<Driver> record) => record.Object.ChampionshipId == specification.ChampionshipId;
    public bool SpecificationSatisfies(ChampionshipIdSpecification specification, ObjectRecord<Event> record) => record.Object.ChampionshipId == specification.ChampionshipId;
    public bool SpecificationSatisfies(ChampionshipIdSpecification specification, ObjectRecord<Session> record) => record.Object.ChampionshipId == specification.ChampionshipId;
    public bool SpecificationSatisfies(ChampionshipIdSpecification specification, ObjectRecord<Team> record) => record.Object.ChampionshipId == specification.ChampionshipId;
    public bool SpecificationSatisfies(ChampionshipIdSpecification specification, ObjectRecord<Track> record) => record.Object.ChampionshipId == specification.ChampionshipId;
}

class DriverIdSpecificationHandler : ChampionshipIdSpecificationHandler, ISpecificationHandler<DriverIdSpecification, ObjectRecord<Driver>>
{
    public bool SpecificationSatisfies(DriverIdSpecification specification, ObjectRecord<Driver> record)
        => base.SpecificationSatisfies(new(specification.ChampionshipId), record) && record.Object.DriverId == specification.DriverId;
}

class EventIdSpecificationHandler : ChampionshipIdSpecificationHandler, ISpecificationHandler<EventIdSpecification, ObjectRecord<Event>>,
    ISpecificationHandler<EventIdSpecification, ObjectRecord<Session>>
{
    public bool SpecificationSatisfies(EventIdSpecification specification, ObjectRecord<Event> record)
        => base.SpecificationSatisfies(new(specification.ChampionshipId), record) && record.Object.EventId == specification.EventId;
    public bool SpecificationSatisfies(EventIdSpecification specification, ObjectRecord<Session> record)
        => base.SpecificationSatisfies(new(specification.ChampionshipId), record) && record.Object.EventId == specification.EventId;
}

class SessionIdSpecificationHandler : ChampionshipIdSpecificationHandler, ISpecificationHandler<SessionIdSpecification, ObjectRecord<Session>>
{
    public bool SpecificationSatisfies(SessionIdSpecification specification, ObjectRecord<Session> record)
        => base.SpecificationSatisfies(new(specification.ChampionshipId), record) && record.Object.SessionId == specification.SessionId;
}

class TrackIdSpecificationHandler : ChampionshipIdSpecificationHandler, ISpecificationHandler<TrackIdSpecification, ObjectRecord<Track>>
{
    public bool SpecificationSatisfies(TrackIdSpecification specification, ObjectRecord<Track> record)
        => base.SpecificationSatisfies(new(specification.ChampionshipId), record) && record.Object.TrackId == specification.TrackId;
}
