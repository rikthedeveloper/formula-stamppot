using System.Runtime.Serialization;
using WebUI.Types;

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