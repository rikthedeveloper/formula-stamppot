using Microsoft.AspNetCore.Mvc.RazorPages;
using WebUI.Domain.ObjectStore;
using WebUI.Endpoints;

namespace WebUI.Pages.ChampionshipsList;

public class ChampionshipsListModel(IReadOnlyObjectStore objectStore) : PageModel
{
    readonly IReadOnlyObjectStore _objectStore = objectStore;

    public IList<ChampionshipResource> Championships { get; private set; } = [];

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var championships = await _objectStore.Championships.ListAsync(cancellationToken: cancellationToken);
        Championships = championships.Select(c => new ChampionshipResource(c, c.Version)).ToList();
    }
}
