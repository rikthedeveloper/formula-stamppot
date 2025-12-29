import { onLoad } from "navigation";
import { UpdateChampionshipRequest, updateChampionship, ProblemDetails, ValidationProblemDetails, Championship, Hypermedia } from "api/formula-discord";
import { FormManager, form } from "form";

let formMgr: FormManager;

async function onSubmitChampionship(e: SubmitEvent) {
    const championshipIdInput = formMgr.field('championshipId');
    const versionInput = formMgr.field('version');
    const nameInput = formMgr.field('name');
    const championship: UpdateChampionshipRequest = new UpdateChampionshipRequest(nameInput.value);
    const res = await updateChampionship(championshipIdInput.value, versionInput.value, championship);
    if (res instanceof ValidationProblemDetails) {
        formMgr.setValidationState(res.toFormError());
    } else if (res instanceof ProblemDetails) {
        formMgr.setError(res.toFormError());
    } else if (res instanceof Hypermedia && res.data instanceof Championship) {
        versionInput.value = res.version;
    }
}

onLoad(async () => {
    formMgr = form('form#edit-championship');
    formMgr.onSubmit(onSubmitChampionship);
});