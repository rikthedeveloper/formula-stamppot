using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Utils;
using WebUI.Domain;
using WebUI.Domain.ObjectStore;
using WebUI.Types;

namespace WebUI.Pages.Championships;

public class ChampionshipsListModel(IReadOnlyObjectStore objectStore) : PageModel
{
    readonly IReadOnlyObjectStore _objectStore = objectStore;

    public IList<ObjectRecord<Championship>> Championships { get; private set; } = [];

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var championships = await _objectStore.Championships.ListAsync(cancellationToken: cancellationToken);
        Championships = championships.ToList(); 
        return Page();
    }

    public string Base36Id(ChampionshipId championshipId) => ConvertLongBase36.Encode(championshipId);
}
