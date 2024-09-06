using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Immutable;
using WebUI.Domain;
using WebUI.Domain.ObjectStore;
using WebUI.Endpoints.Internal.Specifications;
using WebUI.Endpoints.Resources;
using WebUI.Endpoints.Resources.Interfaces;
using WebUI.Filters;
using WebUI.Types;
using EventId = WebUI.Types.EventId;

namespace WebUI.Endpoints;

public class SessionResourceCollection(ChampionshipId championshipId, EventId eventId, IEnumerable<SessionResource> items) : ResourceCollection<SessionResource>(items)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public EventId EventId { get; } = eventId;
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

public class SessionChangeBody : IValidator2
{
    public string Name { get; set; } = string.Empty;
    public ushort LapCount { get; set; } = 0;

    public void Apply(Session session)
    {
        session.Name = Name;
        session.LapCount = LapCount;
    }

    static readonly Validator _validator = new();
    public async Task<ValidationResult> ValidateAsync() => await _validator.ValidateAsync(this);

    class Validator : AbstractValidator<SessionChangeBody>
    {
        public Validator()
        {
            RuleFor(d => d.Name).NotEmpty();
            RuleFor(d => d.LapCount).GreaterThan((ushort)0);
        }
    }
}

public class SessionProgressChangeBody : IValidator2
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

public class SessionStateChangeBody : IValidator2
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
        ChampionshipId championshipId,
        EventId eventId,
        SessionChangeBody change,
        [FromServices] IObjectStore objectStore,
        [FromServices] GenerateId generateId,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([new ChampionshipIdSpecification(championshipId)], cancellationToken))
        {
            throw new InvalidChampionshipException(championshipId);
        }

        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        var @event = await transaction.Events.FindAsync([new EventIdSpecification(championshipId, eventId)], cancellationToken)
            ?? throw new InvalidEventException(championshipId, eventId);

        SessionId sessionId = new(generateId());
        var session = new Session(championshipId, eventId, sessionId);
        change.Apply(session);
        @event.Object.Schedule = @event.Object.Schedule.Add(sessionId);

        await transaction.Sessions.InsertAsync(session, cancellationToken);
        await transaction.Events.UpdateAsync([new EventIdSpecification(championshipId, eventId)], @event.Object, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var createdSession = await objectStore.Sessions.FindAsync([new SessionIdSpecification(championshipId, eventId, sessionId)], cancellationToken)
            ?? throw new InvalidSessionException(championshipId, eventId, sessionId);

        return Results.CreatedAtRoute(nameof(FindSessionById), new
        {
            championshipId = championshipId.ToString("BASE36"),
            sessionId = sessionId.ToString("BASE36")
        }, new SessionResource(createdSession, createdSession.Version));
    }

    public static async Task<IResult> ListSessions(
        ChampionshipId championshipId,
        EventId eventId,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Events.ExistsAsync([new EventIdSpecification(championshipId, eventId)], cancellationToken))
        {
            throw new InvalidEventException(championshipId, eventId);
        }

        var sessions = await objectStore.Sessions.ListAsync([new EventIdSpecification(championshipId, eventId)], cancellationToken);
        return Results.Ok(new SessionResourceCollection(championshipId, eventId, sessions.Select(d => new SessionResource(d, d.Version))));
    }

    public static async Task<IResult> FindSessionById(
        ChampionshipId championshipId,
        EventId eventId,
        SessionId sessionId,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([new EventIdSpecification(championshipId, eventId)], cancellationToken))
        {
            throw new InvalidChampionshipException(championshipId);
        }

        var session = await objectStore.Sessions.FindAsync([new SessionIdSpecification(championshipId, eventId, sessionId)], cancellationToken)
            ?? throw new InvalidSessionException(championshipId, eventId, sessionId);

        return Results.Ok(new SessionResource(session, session.Version));
    }

    public static async Task<IResult> UpdateSessionById(
        ChampionshipId championshipId,
        EventId eventId,
        SessionId sessionId,
        [FromBody] SessionChangeBody change,
        [FromHeader(Name = "If-Match")] string version,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        version = version.Trim('"'); // HTTP header MUST have quotation marks, but we don't want them here
        var sessionIdSpecification = new SessionIdSpecification(championshipId, eventId, sessionId);
        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        var session = await transaction.Sessions.FindAsync([sessionIdSpecification], cancellationToken)
            ?? throw new InvalidSessionException(championshipId, eventId, sessionId);

        change.Apply(session.Object);
        if (await transaction.Sessions.UpdateAsync([sessionIdSpecification, new VersionMatchSpecification(version)], session.Object, cancellationToken) == 0)
        {
            throw new OptimisticConcurrencyException();
        }

        await transaction.CommitAsync(cancellationToken);
        var updatedObject = await objectStore.Sessions.FindAsync([sessionIdSpecification], cancellationToken)
            ?? throw new InvalidSessionException(championshipId, eventId, sessionId);
        return Results.Ok(new SessionResource(updatedObject, updatedObject.Version));
    }

    public static async Task<IResult> UpdateSessionStateById(
        ChampionshipId championshipId,
        EventId eventId,
        SessionId sessionId,
        [FromBody] SessionStateChangeBody stateChange,
        [FromHeader(Name = "If-Match")] string version,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        version = version.Trim('"'); // HTTP header MUST have quotation marks, but we don't want them here
        var sessionIdSpecification = new SessionIdSpecification(championshipId, eventId, sessionId);
        var versionSpecification = new VersionMatchSpecification(version);
        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        var session = await transaction.Sessions.FindAsync([sessionIdSpecification, versionSpecification], cancellationToken)
            ?? throw new InvalidSessionException(championshipId, eventId, sessionId);
        var championship = await transaction.Championships.FindAsync([new ChampionshipIdSpecification(championshipId)], cancellationToken)
            ?? throw new InvalidChampionshipException(championshipId);

        if (stateChange.State == State.Running)
        {
            var participantDrivers = await transaction.Drivers.ListAsync([new ChampionshipIdSpecification(championshipId)], cancellationToken);
            session.Object.Start(championship.Object.Features, participantDrivers.Select((d, i) => new SessionParticipant(d.Object.DriverId, (ushort)(i + 1), d.Object.Data)).ToImmutableList());
        }

        if (stateChange.State == State.Finished)
        {
            session.Object.Finish();
        }

        if (await transaction.Sessions.UpdateAsync([sessionIdSpecification, versionSpecification], session.Object, cancellationToken) == 0)
            throw new OptimisticConcurrencyException();

        await transaction.CommitAsync(cancellationToken);

        var updatedObject = await objectStore.Sessions.FindAsync([sessionIdSpecification], cancellationToken)
            ?? throw new InvalidSessionException(championshipId, eventId, sessionId);
        return Results.Ok(new SessionResource(updatedObject, updatedObject.Version));
    }

    public static async Task<IResult> UpdateSessionProgressById(
        ChampionshipId championshipId,
        EventId eventId,
        SessionId sessionId,
        [FromBody] SessionProgressChangeBody progressChange,
        [FromHeader(Name = "If-Match")] string matchVersion,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        matchVersion = matchVersion.Trim('"'); // HTTP header MUST have quotation marks, but we don't want them here
        var sessionIdSpecification = new SessionIdSpecification(championshipId, eventId, sessionId);
        var versionSpecification = new VersionMatchSpecification(matchVersion);
        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);

        (var session, _, _, _) = await transaction.Sessions.FindAsync([sessionIdSpecification, versionSpecification], cancellationToken)
            ?? throw new InvalidSessionException(championshipId, eventId, sessionId);

        session.CanProgressToOrFail(progressChange.ElapsedLaps);
        session.LapResults.TryGetValue(session.ElapsedLaps, out var lastlapResult);

        var lapResults = new List<LapResult>();
        for (var currentElapsedLaps = session.ElapsedLaps; currentElapsedLaps < progressChange.ElapsedLaps; currentElapsedLaps++)
        {
            var rand = new Random();
            var driverScores = new List<(DriverId driverId, TimeSpan lapTime, TimeSpan totalTime)>();
            foreach (var participant in session.Participants)
            {
                var currentTotalTime = lastlapResult?.Results[participant.DriverId].TotalTime ?? TimeSpan.FromMilliseconds(0);
                var driverResults = session.Features.Apply(session, participant, lastlapResult, rand).ToArray();
                var lapTime = driverResults.Length != 0 // IEnumerable<T>.Sum fails on empty collections, so use a default in that situation.
                    ? TimeSpan.FromMilliseconds(driverResults.Sum(dr => dr.Result.TotalMilliseconds))
                    : TimeSpan.Zero;

                driverScores.Add((participant.DriverId, lapTime, currentTotalTime + lapTime));
            }

            var scores = driverScores
                .OrderBy(ds => ds.totalTime)
                .Aggregate(ImmutableDictionary<DriverId, ParticipantLapResult>.Empty, (current, next) => current.Add(next.driverId, new((ushort)(current.Count + 1), next.totalTime, next.lapTime)));

            lastlapResult = new LapResult(scores);
            lapResults.Add(lastlapResult);
        }

        session.Progress(progressChange.ElapsedLaps, lapResults);

        if (await transaction.Sessions.UpdateAsync([sessionIdSpecification, versionSpecification], session, cancellationToken) == 0)
            throw new OptimisticConcurrencyException();

        await transaction.CommitAsync(cancellationToken);
        (session, _, _, var version) = await objectStore.Sessions.FindAsync([sessionIdSpecification], cancellationToken)
            ?? throw new InvalidSessionException(championshipId, eventId, sessionId);
        return Results.Ok(new SessionResource(session, version));
    }
}

public class InvalidSessionStateException(ChampionshipId championshipId, EventId eventId, SessionId sessionId, State[] validStates) : SessionException(championshipId, eventId, sessionId, _errorMessage, null)
{
    const string _errorMessage = "The requested operation is not valid for the specified Session's state.";
    public State[] ValidStates { get; } = validStates;
}

public class InvalidSessionStateChangeException(ChampionshipId championshipId, EventId eventId, SessionId sessionId, State requestedState, State[] validStates) : SessionException(championshipId, eventId, sessionId, _errorMessage, null)
{
    const string _errorMessage = "The given state transition is not valid for the specified Session.";
    public State RequestedState { get; } = requestedState;
    public State[] ValidStates { get; } = validStates;
}