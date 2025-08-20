using System.Collections.Immutable;
using WebUI.Types;

namespace WebUI.Domain;

public class Event(ChampionshipId championshipId, Types.EventId eventId)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public Types.EventId EventId { get; } = eventId;

    public TrackId TrackId { get; set; }

    public State State { get; private set; }

    public ImmutableArray<SessionId> Schedule { get; set; } = [];

    public ImmutableArray<EventParticipantResult> Results { get; private set; } = [];

    public void Start()
    {
        if (!CanStart(this))
            throw new InvalidOperationException("Event cannot be started");

        State = State.Running;
    }

    public void Finish(IEnumerable<EventParticipantResult> results)
    {
        if (!CanFinish(this))
            throw new InvalidOperationException("Event cannot be finished");

        State = State.Finished;
        Results = [.. results];
    }

    public static bool CanStart(Event @event) => @event.State == State.NotStarted && @event.Schedule.Length > 0;

    public static bool CanFinish(Event @event) => @event.State == State.Running;
}

public class EventParticipantResult(DriverId driverId, ushort position, TimeSpan totalTime, ushort awardedPoints)
{
    public DriverId DriverId { get; } = driverId;
    public ushort Position { get; } = position;
    public TimeSpan TotalTime { get; } = totalTime;
    public ushort AwardedPoints { get; } = awardedPoints;
}
