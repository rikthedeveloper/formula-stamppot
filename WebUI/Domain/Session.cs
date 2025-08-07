using System.Collections.Immutable;
using WebUI.Endpoints.Resources;
using WebUI.Model.StartingOrderStrategies;
using WebUI.Types;
using EventId = WebUI.Types.EventId;

namespace WebUI.Domain;

public class Session(ChampionshipId championshipId, EventId eventId, SessionId sessionId)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public EventId EventId { get; } = eventId;
    public SessionId SessionId { get; } = sessionId;

    public string Name { get; set; } = string.Empty;
    public ushort LapCount { get; set; } = 0;

    public State State { get; private set; } = State.NotStarted;
    public ushort ElapsedLaps { get; private set; } = 0;

    public FeatureCollection Features { get; private set; } = new();
    public IStartingOrderStrategy StartingOrderStrategy { get; private set; } = new NoopStartingOrderStrategy();
    public IImmutableList<IPointsSystem> PointsSystems { get; private set; } = [];
    public IImmutableList<SessionParticipant> Participants { get; private set; } = [];
    public IImmutableDictionary<ushort, LapResult> LapResults { get; private set; } = ImmutableDictionary<ushort, LapResult>.Empty;

    public void Start(FeatureCollection features, IImmutableList<IPointsSystem> pointsSystems, IImmutableList<SessionParticipant> participants)
    {
        if (State != State.NotStarted)
            throw new InvalidSessionStateChangeException(ChampionshipId, EventId, SessionId, State.Running, GetValidStateChanges());

        State = State.Running;
        Features = features;
        PointsSystems = pointsSystems;
        Participants = participants;
    }

    public void Progress(ushort elapsedLaps, IEnumerable<LapResult> results)
    {
        if (State != State.Running)
            throw new InvalidSessionStateException(ChampionshipId, EventId, SessionId, State, [State.Running]);

        if (elapsedLaps < ElapsedLaps || elapsedLaps > LapCount)
            throw new InvalidSessionProgressChangeException(ChampionshipId, EventId, SessionId, elapsedLaps, (ushort)(ElapsedLaps + 1), LapCount);

        if (results.Count() != (elapsedLaps - ElapsedLaps))
            throw new ArgumentException("The number of results must match the number of laps progressed.");

        LapResults = LapResults.AddRange(results.Select((r, i) => new KeyValuePair<ushort, LapResult>((ushort)(ElapsedLaps + i + 1), r)));
        ElapsedLaps = elapsedLaps;
    }

    public void Finish()
    {
        if (State != State.Running)
            throw new InvalidSessionStateChangeException(ChampionshipId, EventId, SessionId, State, GetValidStateChanges());

        if (ElapsedLaps != LapCount)
            throw new InvalidSessionProgressException(ChampionshipId, EventId, SessionId, ElapsedLaps, LapCount, LapCount);

        State = State.Finished;
        var finalLapResult = LapResults.Values.Last();
        foreach (var participant in Participants)
        {
            var participantLapResult = finalLapResult.Results[participant.DriverId];
            var awardedPoints = ushort.CreateTruncating(PointsSystems.Sum(ps => ps.GetPoints(participantLapResult, LapResults)));
            participant.Result = new SessionResult(participantLapResult.Position, participantLapResult.TotalTime, awardedPoints);
        }
    }

    State[] GetValidStateChanges()
    {
        if (State == State.NotStarted)
            return [State.Running];
        if (State == State.Running && ElapsedLaps != LapCount)
            return [];
        if (State == State.Running && ElapsedLaps == LapCount)
            return [State.Finished];
        if (State == State.Finished)
            return [];

        throw new InvalidOperationException($"Unexpected session state: {State}");
    }
}

public class TrackInfo(TrackId trackId, Distance length)
{
    public TrackId TrackId { get; } = trackId;
    public Distance Length { get; } = length;

    public FeatureDataCollection<IFeatureTrackData> Data { get; } = new();
}

public class SessionParticipant(DriverId driverId, ushort startingPosition, FeatureDataCollection<IFeatureDriverData> data)
{
    public DriverId DriverId { get; } = driverId;
    public ushort StartingPosition { get; } = startingPosition;
    public SessionResult? Result { get; set; } = null;

    public FeatureDataCollection<IFeatureDriverData> Data { get; } = data;
}

public class SessionResult(ushort position, TimeSpan totalTime, ushort awardedPoints)
{
    public ushort Position { get; } = position;
    public TimeSpan TotalTime { get; } = totalTime;
    public ushort AwardedPoints { get; } = awardedPoints;
}

public class LapResult(IImmutableDictionary<DriverId, ParticipantLapResult> results)
{
    public IImmutableDictionary<DriverId, ParticipantLapResult> Results { get; } = results;
}

public class ParticipantLapResult(ushort position, TimeSpan totalTime, TimeSpan lapTime)
{
    public ushort Position { get; } = position;
    public TimeSpan TotalTime { get; } = totalTime;
    public TimeSpan LapTime { get; } = lapTime;
}

public class InvalidSessionStateException(ChampionshipId championshipId, EventId eventId, SessionId sessionId, State currentState, State[] validStates) 
    : SessionException(championshipId, eventId, sessionId, _errorMessage, null)
{
    const string _errorMessage = "The requested operation is not valid for the specified Session's state.";
    public State CurrentState { get; } = currentState;
    public State[] ValidStates { get; } = validStates;
}

public class InvalidSessionStateChangeException(ChampionshipId championshipId, EventId eventId, SessionId sessionId, State requestedState, State[] validStates) 
    : SessionException(championshipId, eventId, sessionId, _errorMessage, null)
{
    const string _errorMessage = "The given state transition is not valid for the specified Session.";
    public State RequestedState { get; } = requestedState;
    public State[] ValidStates { get; } = validStates;
}

public class InvalidSessionProgressException(ChampionshipId championshipId, EventId eventId, SessionId sessionId, ushort currentProgress, ushort minimumProgress, ushort maximumProgress)
    : SessionException(championshipId, eventId, sessionId, _errorMessage, null)
{
    const string _errorMessage = "The requested operation is not valid for the specified Session's progress.";
    public ushort CurrentProgress { get; } = currentProgress;
    public ushort MinimumProgress { get; } = minimumProgress;
    public ushort MaximumProgress { get; } = maximumProgress;
}

public class InvalidSessionProgressChangeException(ChampionshipId championshipId, EventId eventId, SessionId sessionId, ushort requestedProgress, ushort minimumProgress, ushort maximumProgress) 
    : SessionException(championshipId, eventId, sessionId, _errorMessage, null)
{
    const string _errorMessage = "The given progress is not valid for the specified Session.";
    public ushort RequestedProgress { get; } = requestedProgress;
    public ushort MinimumProgress { get; } = minimumProgress;
    public ushort MaximumProgress { get; } = maximumProgress;
}