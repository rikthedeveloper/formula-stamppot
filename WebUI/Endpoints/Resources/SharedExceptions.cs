using WebUI.Types;
using EventId = WebUI.Types.EventId;

namespace WebUI.Endpoints.Resources;

public abstract class ChampionshipException(ChampionshipId championshipId, string message, Exception? innerException) : ApplicationException(message, innerException)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
}

public class InvalidChampionshipException(ChampionshipId championshipId) : ChampionshipException(championshipId, _errorMessage, null)
{
    const string _errorMessage = "The specified Championship does not exist.";
}

public class OptimisticConcurrencyException : ApplicationException
{
    const string _errorMessage = "The specified object was changed by another command.";

    public OptimisticConcurrencyException()
    {
    }
}

public abstract class TrackException(ChampionshipId championshipId, TrackId trackId, string message, Exception? innerException) : ApplicationException(message, innerException)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public TrackId TrackId { get; } = trackId;
}

public class InvalidTrackException(ChampionshipId championshipId, TrackId trackId) : TrackException(championshipId, trackId, _errorMessage, null)
{
    const string _errorMessage = "The specified Track does not exist.";
}

public abstract class DriverException(ChampionshipId championshipId, DriverId driverId, string message, Exception? innerException) : ApplicationException(message, innerException)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public DriverId DriverId { get; } = driverId;
}

public class InvalidDriverException(ChampionshipId championshipId, DriverId driverId) : DriverException(championshipId, driverId, _errorMessage, null)
{
    const string _errorMessage = "The specified Driver does not exist.";
}

public abstract class EventException(ChampionshipId championshipId, EventId eventId, string message, Exception? innerException) : ApplicationException(message, innerException)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public EventId EventId { get; } = eventId;
}

public class InvalidEventException(ChampionshipId championshipId, EventId eventId) : EventException(championshipId, eventId, _errorMessage, null)
{
    const string _errorMessage = "The specified Event does not exist.";
}

public abstract class SessionException(ChampionshipId championshipId, EventId eventId, SessionId sessionId, string message, Exception? innerException) : ApplicationException(message, innerException)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public EventId EventId{ get; } = eventId;
    public SessionId SessionId { get; } = sessionId;
}

public class InvalidSessionException(ChampionshipId championshipId, EventId eventId, SessionId sessionId) : SessionException(championshipId, eventId, sessionId, _errorMessage, null)
{
    const string _errorMessage = "The specified Session does not exist.";
}