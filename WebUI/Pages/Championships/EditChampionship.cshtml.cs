using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebUI.Domain;
using WebUI.Domain.ObjectStore;
using WebUI.Endpoints.Internal.Specifications;
using WebUI.Types;

namespace WebUI.Pages.Championships;

public class EditChampionshipModel(IReadOnlyObjectStore objectStore) : PageModel
{
    readonly IReadOnlyObjectStore _objectStore = objectStore;

    public ObjectRecord<Championship> Championship { get; private set; } = null!;

    public async Task<IActionResult> OnGet(ChampionshipId championshipId, CancellationToken cancellationToken)
    {
        var championship = await _objectStore.Championships.FindAsync([new ChampionshipIdSpecification(championshipId)], cancellationToken: cancellationToken);
        if (championship == null)
        {
            return NotFound();
        }
        Championship = championship;
        return Page();
    }
}
