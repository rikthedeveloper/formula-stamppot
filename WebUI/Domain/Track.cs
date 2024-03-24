using WebUI.Types;

namespace WebUI.Domain;

public class Track(ChampionshipId championshipId, TrackId trackId)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public TrackId TrackId { get; } = trackId;
    public string Name { get; set; } = string.Empty;
    public Distance Length { get; set; } = Distance.Zero;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
