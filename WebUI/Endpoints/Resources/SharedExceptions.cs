using WebUI.Endpoints.Internal;
using WebUI.Types;
using EventId = WebUI.Types.EventId;

namespace WebUI.Endpoints.Resources;

public abstract class ChampionshipException(ChampionshipId championshipId, string message, Exception? innerException) : ApplicationException(message, innerException)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;

    public ChampionshipException(ChampionshipRouteParameters routeParameters, string message, Exception? innerException)
        : this(routeParameters.ChampionshipId, message, innerException)
    { }
}

public class InvalidChampionshipException(ChampionshipId championshipId) : ChampionshipException(championshipId, _errorMessage, null)
{
    const string _errorMessage = "The specified Championship does not exist.";

    public InvalidChampionshipException(ChampionshipRouteParameters routeParameters)
        : this(routeParameters.ChampionshipId)
    { }
}

public class OptimisticConcurrencyException() : ApplicationException(_errorMessage)
{
    const string _errorMessage = "The specified object was changed by another command.";
}

public abstract class TrackException(ChampionshipId championshipId, TrackId trackId, string message, Exception? innerException) : ApplicationException(message, innerException)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public TrackId TrackId { get; } = trackId;

    public TrackException(TrackRouteParameters routeParameters, string message, Exception? innerException)
        : this(routeParameters.ChampionshipId, routeParameters.TrackId, message, innerException)
    { }

    public TrackException(ChampionshipRouteParameters routeParameters, TrackId trackId, string message, Exception? innerException)
        : this(routeParameters.ChampionshipId, trackId, message, innerException)
    { }
}

public class InvalidTrackException(ChampionshipId championshipId, TrackId trackId) : TrackException(championshipId, trackId, _errorMessage, null)
{
    const string _errorMessage = "The specified Track does not exist.";

    public InvalidTrackException(TrackRouteParameters routeParameters)
        : this(routeParameters.ChampionshipId, routeParameters.TrackId)
    { }

    public InvalidTrackException(ChampionshipRouteParameters routeParameters, TrackId trackId)
        : this(routeParameters.ChampionshipId, trackId)
    { }
}

public abstract class DriverException(ChampionshipId championshipId, DriverId driverId, string message, Exception? innerException) : ApplicationException(message, innerException)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public DriverId DriverId { get; } = driverId;

    public DriverException(DriverRouteParameters routeParameters, string message, Exception? innerException)
        : this(routeParameters.ChampionshipId, routeParameters.DriverId, message, innerException)
    { }

    public DriverException(ChampionshipRouteParameters routeParameters, DriverId driverId, string message, Exception? innerException)
        : this(routeParameters.ChampionshipId, driverId, message, innerException)
    { }
}

public class InvalidDriverException(ChampionshipId championshipId, DriverId driverId) : DriverException(championshipId, driverId, _errorMessage, null)
{
    const string _errorMessage = "The specified Driver does not exist.";

    public InvalidDriverException(DriverRouteParameters routeParameters)
        : this(routeParameters.ChampionshipId, routeParameters.DriverId)
    { }

    public InvalidDriverException(ChampionshipRouteParameters routeParameters, DriverId driverId)
        : this(routeParameters.ChampionshipId, driverId)
    { }
}

public abstract class EventException(ChampionshipId championshipId, EventId eventId, string message, Exception? innerException) : ApplicationException(message, innerException)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public EventId EventId { get; } = eventId;

    public EventException(EventRouteParameters routeParameters, string message, Exception? innerException)
        : this(routeParameters.ChampionshipId, routeParameters.EventId, message, innerException)
    { }

    public EventException(ChampionshipRouteParameters routeParameters, EventId eventId, string message, Exception? innerException)
        : this(routeParameters.ChampionshipId, eventId, message, innerException)
    { }
}

public class InvalidEventException(ChampionshipId championshipId, EventId eventId) : EventException(championshipId, eventId, _errorMessage, null)
{
    const string _errorMessage = "The specified Event does not exist.";

    public InvalidEventException(EventRouteParameters routeParameters)
        : this(routeParameters.ChampionshipId, routeParameters.EventId)
    { }

    public InvalidEventException(ChampionshipRouteParameters routeParameters, EventId eventId)
        : this(routeParameters.ChampionshipId, eventId)
    { }
}

public abstract class SessionException(ChampionshipId championshipId, EventId eventId, SessionId sessionId, string message, Exception? innerException) : ApplicationException(message, innerException)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public EventId EventId{ get; } = eventId;
    public SessionId SessionId { get; } = sessionId;

    public SessionException(SessionRouteParameters routeParameters, string message, Exception? innerException)
        : this(routeParameters.ChampionshipId, routeParameters.EventId, routeParameters.SessionId, message, innerException)
    { }

    public SessionException(EventRouteParameters routeParameters, SessionId sessionId, string message, Exception? innerException)
        : this(routeParameters.ChampionshipId, routeParameters.EventId, sessionId, message, innerException)
    { }
}

public class InvalidSessionException(ChampionshipId championshipId, EventId eventId, SessionId sessionId) : SessionException(championshipId, eventId, sessionId, _errorMessage, null)
{
    const string _errorMessage = "The specified Session does not exist.";

    public InvalidSessionException(SessionRouteParameters routeParameters) 
        : this(routeParameters.ChampionshipId, routeParameters.EventId, routeParameters.SessionId)
    { }

    public InvalidSessionException(EventRouteParameters routeParameters, SessionId sessionId)
        : this(routeParameters.ChampionshipId, routeParameters.EventId, sessionId)
    { }
}