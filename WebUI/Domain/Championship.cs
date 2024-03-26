using WebUI.Types;

namespace WebUI.Domain;

public class Championship(ChampionshipId championshipId)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public string Name { get; set; } = string.Empty;
}
