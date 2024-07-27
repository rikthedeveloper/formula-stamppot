using WebUI.Types;

namespace WebUI.Domain;

public class Team(ChampionshipId championshipId, TeamId teamId)
{ 
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public TeamId TeamId { get; } = teamId;

    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Color Color { get; set; } = new();
}
