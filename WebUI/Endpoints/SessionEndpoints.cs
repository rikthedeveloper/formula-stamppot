using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using WebUI.Domain;
using WebUI.Domain.ObjectStore;
using WebUI.Endpoints.Internal;
using WebUI.Endpoints.Internal.Specifications;
using WebUI.Endpoints.Resources;
using WebUI.Endpoints.Resources.Interfaces;
using WebUI.Types;
using WebUI.Validation;
using EventId = WebUI.Types.EventId;

namespace WebUI.Endpoints;

public class SessionResourceCollection(ChampionshipId championshipId, EventId eventId, IEnumerable<SessionResource> items) : ResourceCollection<SessionResource>(items)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public EventId EventId { get; } = eventId;

    public SessionResourceCollection(EventRouteParameters routeParameters, IEnumerable<SessionResource> items)
        : this(routeParameters.ChampionshipId, routeParameters.EventId, items)
    {
    }
}

public class SessionResource(Session session, string version) : IVersioned
{
    public ChampionshipId ChampionshipId { get; } = session.ChampionshipId;
    public EventId EventId { get; } = session.EventId;
    public SessionId SessionId { get; } = session.SessionId;
    public string Version { get; } = version;
    public string Name { get; } = session.Name;
    public ushort LapCount { get; } = session.LapCount;
    public State State { get; } = session.State;
    public IImmutableList<SessionParticipant> Participants { get; } = session.Participants.Select(sp => new SessionParticipant(sp)).ToImmutableList();
    public FeatureCollection Features { get; } = session.Features;
    public ushort ElapsedLaps { get; } = session.ElapsedLaps;
    public IImmutableDictionary<ushort, LapResult> LapResults { get; } = session.LapResults.ToImmutableDictionary(kvp => kvp.Key, kvp => new LapResult(kvp.Value));

    [JsonIgnore]
    public bool IsPreviousSessionFinished { get; } = session.PreviousSessionHasFinished;

    public class SessionParticipant(Domain.SessionParticipant sessionParticipant)
    {
        public DriverId DriverId { get; } = sessionParticipant.DriverId;
        public ushort Position { get; } = sessionParticipant.StartingPosition;
        public SessionParticipantResult? Result { get; } = sessionParticipant.Result is not null ? new(sessionParticipant.Result) : null;
    }

    public class SessionParticipantResult(Domain.SessionResult participantResult)
    {
        public ushort Position { get; } = participantResult.Position;
        public TimeSpan TotalTime { get; } = participantResult.TotalTime;
    }

    public class LapResult(Domain.LapResult lapResult)
    {
        public IImmutableDictionary<DriverId, ParticipantLapResult> Results { get; } = lapResult.Results.ToImmutableDictionary(kvp => kvp.Key, kvp => new ParticipantLapResult(kvp.Value));
    }

    public class ParticipantLapResult(Domain.ParticipantLapResult participantLapResult)
    {
        public ushort Position { get; } = participantLapResult.Position;
        public TimeSpan TotalTime { get; } = participantLapResult.TotalTime;
        public TimeSpan LapTime { get; } = participantLapResult.LapTime;

    }
}

[UseValidator]
public partial class SessionChangeBody
{
    public string Name { get; set; } = string.Empty;
    public ushort LapCount { get; set; } = 0;

    public void Apply(Session session)
    {
        session.Name = Name;
        session.LapCount = LapCount;
    }

    static partial void ConfigureValidator(AbstractValidator<SessionChangeBody> validator)
    {
        validator.RuleFor(d => d.Name).NotEmpty();
        validator.RuleFor(d => d.LapCount).GreaterThan((ushort)0);
    }
}

public class SessionProgressChangeBody : Filters.IValidator
{
    public ushort ElapsedLaps { get; set; } = 0;

    static readonly Validator _validator = new();
    public async Task<ValidationResult> ValidateAsync() => await _validator.ValidateAsync(this);

    class Validator : AbstractValidator<SessionProgressChangeBody>
    {
        public Validator()
        {
            RuleFor(d => d.ElapsedLaps).GreaterThan((ushort)0);
        }
    }
}

public class SessionStateChangeBody : Filters.IValidator
{
    public State State { get; set; } = State.NotStarted;

    static readonly Validator _validator = new();
    public async Task<ValidationResult> ValidateAsync() => await _validator.ValidateAsync(this);

    class Validator : AbstractValidator<SessionStateChangeBody>
    {
        public Validator()
        {
            RuleFor(d => d.State).IsInEnum();
        }
    }
}

public static class SessionEndpoints
{
    public static RouteGroupBuilder MapSessions(this IEndpointRouteBuilder endpoints)
    {
        var groupBuilder = endpoints.MapGroup("championships/{championshipId}/events/{eventId}/sessions").WithTags("Sessions");
        groupBuilder.MapGet("/", ListSessions).WithName(nameof(ListSessions));
        groupBuilder.MapPost("/", CreateSession).WithName(nameof(CreateSession));
        groupBuilder.MapGet("/{sessionId}", FindSessionById).WithName(nameof(FindSessionById));
        groupBuilder.MapPut("/{sessionId}", UpdateSessionById).WithName(nameof(UpdateSessionById));
        groupBuilder.MapPut("/{sessionId}/state", UpdateSessionStateById).WithName(nameof(UpdateSessionStateById));
        groupBuilder.MapPut("/{sessionId}/progress", UpdateSessionProgressById).WithName(nameof(UpdateSessionProgressById));
        return groupBuilder;
    }

    public static async Task<IResult> CreateSession(
        [AsParameters] EventRouteParameters routeParameters,
        SessionChangeBody change,
        [FromServices] IObjectStore objectStore,
        [FromServices] GenerateId generateId,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken))
            throw new InvalidChampionshipException(routeParameters);

        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        var @event = await transaction.Events.FindAsync([routeParameters.EventIdSpecification()], cancellationToken)
            ?? throw new InvalidEventException(routeParameters);

        SessionId sessionId = new(generateId());
        var session = new Session(routeParameters.ChampionshipId, routeParameters.EventId, sessionId);
        change.Apply(session);
        var previousSession = await transaction.Sessions.FindAsync([routeParameters.SessionIdSpecification(@event.Object.Schedule.LastOrDefault())], cancellationToken);
        session.PreviousSessionHasFinished = previousSession is not { Object.State: not State.Finished };
        @event.Object.Schedule = @event.Object.Schedule.Add(sessionId);

        await transaction.Sessions.InsertAsync(session, cancellationToken);
        await transaction.Events.UpdateAsync([routeParameters.EventIdSpecification()], @event.Object, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var createdSession = await objectStore.Sessions.FindAsync([routeParameters.SessionIdSpecification(sessionId)], cancellationToken)
            ?? throw new InvalidSessionException(routeParameters, sessionId);

        return Results.CreatedAtRoute(nameof(FindSessionById), routeParameters.ToRouteValues(sessionId), new SessionResource(createdSession, createdSession.Version));
    }

    public static async Task<IResult> ListSessions(
        [AsParameters] EventRouteParameters routeParameters,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Events.ExistsAsync([routeParameters.EventIdSpecification()], cancellationToken))
            throw new InvalidEventException(routeParameters);

        var sessions = await objectStore.Sessions.ListAsync([routeParameters.EventIdSpecification()], cancellationToken);
        return Results.Ok(new SessionResourceCollection(routeParameters, sessions.Select(d => new SessionResource(d, d.Version))));
    }

    public static async Task<IResult> FindSessionById(
        [AsParameters] SessionRouteParameters routeParameters,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Events.ExistsAsync([routeParameters.EventIdSpecification()], cancellationToken))
            throw new InvalidEventException(routeParameters);

        var session = await objectStore.Sessions.FindAsync([routeParameters.SessionIdSpecification()], cancellationToken)
            ?? throw new InvalidSessionException(routeParameters);

        return Results.Ok(new SessionResource(session, session.Version));
    }

    public static async Task<IResult> UpdateSessionById(
        [AsParameters] SessionRouteParameters routeParameters,
        [FromBody] SessionChangeBody change,
        [FromHeader(Name = "If-Match")] VersionMatchSpecification versionMatchSpecification,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        var session = await transaction.Sessions.FindAsync([routeParameters.SessionIdSpecification()], cancellationToken)
            ?? throw new InvalidSessionException(routeParameters);

        change.Apply(session.Object);
        if (await transaction.Sessions.UpdateAsync([routeParameters.SessionIdSpecification(), versionMatchSpecification], session.Object, cancellationToken) == 0)
        {
            throw new OptimisticConcurrencyException();
        }

        await transaction.CommitAsync(cancellationToken);
        var updatedObject = await objectStore.Sessions.FindAsync([routeParameters.SessionIdSpecification()], cancellationToken)
            ?? throw new InvalidSessionException(routeParameters);
        return Results.Ok(new SessionResource(updatedObject, updatedObject.Version));
    }

    public static async Task<IResult> UpdateSessionStateById(
        [AsParameters] SessionRouteParameters routeParameters,
        [FromBody] SessionStateChangeBody stateChange,
        [FromHeader(Name = "If-Match")] VersionMatchSpecification versionSpecification,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        var session = await transaction.Sessions.FindAsync([routeParameters.SessionIdSpecification(), versionSpecification], cancellationToken)
            ?? throw new InvalidSessionException(routeParameters);
        var championship = await transaction.Championships.FindAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken)
            ?? throw new InvalidChampionshipException(routeParameters);

        var @event = await transaction.Events.FindAsync([routeParameters.EventIdSpecification()], cancellationToken)
            ?? throw new InvalidEventException(routeParameters);

        if (stateChange.State == State.Running)
        {
            if (!session.Object.CanStart())
                throw new SessionScheduleConflictException(routeParameters, @event.Object.Schedule.TakeWhile(s => s != routeParameters.SessionId).Last());

            var participantDrivers = await transaction.Drivers.ListAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken);
            session.Object.Start(championship.Object.Features, participantDrivers.Select((d, i) => new SessionParticipant(d.Object.DriverId, (ushort)(i + 1), d.Object.Data)).ToImmutableList());
        }

        if (stateChange.State == State.Finished)
        {
            if (!session.Object.CanFinish())
                throw new InvalidSessionStateException(routeParameters, []);

            session.Object.Finish();

            var nextSessionId = @event.Object.Schedule
                .SkipWhile(s => s != routeParameters.SessionId)
                .Skip(1)
                .FirstOrDefault();

            if (nextSessionId != default && await transaction.Sessions.FindAsync([routeParameters.SessionIdSpecification(nextSessionId)], cancellationToken) is ObjectRecord<Session> nextSession)
            {
                nextSession.Object.PreviousSessionHasFinished = true;
                await transaction.Sessions.UpdateAsync([routeParameters.SessionIdSpecification(nextSessionId)], nextSession.Object, cancellationToken);
            }
        }

        if (await transaction.Sessions.UpdateAsync([routeParameters.SessionIdSpecification(), versionSpecification], session.Object, cancellationToken) == 0)
            throw new OptimisticConcurrencyException();

        await transaction.CommitAsync(cancellationToken);

        var updatedObject = await objectStore.Sessions.FindAsync([routeParameters.SessionIdSpecification()], cancellationToken)
            ?? throw new InvalidSessionException(routeParameters);
        return Results.Ok(new SessionResource(updatedObject, updatedObject.Version));
    }

    public static async Task<IResult> UpdateSessionProgressById(
        [AsParameters] SessionRouteParameters routeParameters,
        [FromBody] SessionProgressChangeBody progressChange,
        [FromHeader(Name = "If-Match")] VersionMatchSpecification versionSpecification,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);

        (var session, _, _, _) = await transaction.Sessions.FindAsync([routeParameters.SessionIdSpecification(), versionSpecification], cancellationToken)
            ?? throw new InvalidSessionException(routeParameters);

        session.CanProgressToOrFail(progressChange.ElapsedLaps);
        session.LapResults.TryGetValue(session.ElapsedLaps, out var lastLapResult);

        var lapResults = new List<LapResult>();
        for (var currentElapsedLaps = session.ElapsedLaps; currentElapsedLaps < progressChange.ElapsedLaps; currentElapsedLaps++)
        {
            var rand = new Random();
            var driverScores = new List<(DriverId driverId, TimeSpan lapTime, TimeSpan totalTime)>();
            foreach (var participant in session.Participants)
            {
                var currentTotalTime = lastLapResult?.Results[participant.DriverId].TotalTime ?? TimeSpan.FromMilliseconds(0);
                var driverResults = session.Features.Apply(session, participant, lastLapResult, rand).ToArray();
                var lapTime = driverResults.Length != 0 // IEnumerable<T>.Sum fails on empty collections, so use a default in that situation.
                    ? TimeSpan.FromMilliseconds(driverResults.Sum(dr => dr.Result.TotalMilliseconds))
                    : TimeSpan.Zero;

                driverScores.Add((participant.DriverId, lapTime, currentTotalTime + lapTime));
            }

            var scores = driverScores
                .OrderBy(ds => ds.totalTime)
                .Aggregate(ImmutableDictionary<DriverId, ParticipantLapResult>.Empty, (current, next) => current.Add(next.driverId, new((ushort)(current.Count + 1), next.totalTime, next.lapTime)));

            lastLapResult = new LapResult(scores);
            lapResults.Add(lastLapResult);
        }

        session.Progress(progressChange.ElapsedLaps, lapResults);

        if (await transaction.Sessions.UpdateAsync([routeParameters.SessionIdSpecification(), versionSpecification], session, cancellationToken) == 0)
            throw new OptimisticConcurrencyException();

        await transaction.CommitAsync(cancellationToken);
        (session, _, _, var version) = await objectStore.Sessions.FindAsync([routeParameters.SessionIdSpecification()], cancellationToken)
            ?? throw new InvalidSessionException(routeParameters);
        return Results.Ok(new SessionResource(session, version));
    }
}

public class InvalidSessionStateException(ChampionshipId championshipId, EventId eventId, SessionId sessionId, State[] validStates) : SessionException(championshipId, eventId, sessionId, _errorMessage, null)
{
    const string _errorMessage = "The requested operation is not valid for the specified Session's state.";
    public State[] ValidStates { get; } = validStates;

    public InvalidSessionStateException(SessionRouteParameters routeParameters, State[] validStates)
        : this(routeParameters.ChampionshipId, routeParameters.EventId, routeParameters.SessionId, validStates)
    { }
}

public class InvalidSessionStateChangeException(ChampionshipId championshipId, EventId eventId, SessionId sessionId, State requestedState, State[] validStates) : SessionException(championshipId, eventId, sessionId, _errorMessage, null)
{
    const string _errorMessage = "The given state transition is not valid for the specified Session.";
    public State RequestedState { get; } = requestedState;
    public State[] ValidStates { get; } = validStates;
}

public class SessionScheduleConflictException(ChampionshipId championshipId, EventId eventId, SessionId sessionId, SessionId conflictingSession) : SessionException(championshipId, eventId, sessionId, _errorMessage, null)
{
    const string _errorMessage = "The Session cannot be started while the previous scheduled Session is not yet Finished";
    public SessionId ConflictingSession { get; } = conflictingSession;

    public SessionScheduleConflictException(SessionRouteParameters routeParameters, SessionId conflictingSession)
        : this(routeParameters.ChampionshipId, routeParameters.EventId, routeParameters.SessionId, conflictingSession)
    { }
}