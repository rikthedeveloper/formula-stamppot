import { navigateToRoute, onLoad } from "navigation";
import { CreateChampionshipRequest, createChampionship, ProblemDetails, ValidationProblemDetails, Championship } from "api/formula-discord";
import { FormManager, form } from "form";

let formMgr: FormManager;

async function onSubmitChampionship() {
    const nameInput: HTMLInputElement | null = formMgr.field('name');
    const championship: CreateChampionshipRequest = new CreateChampionshipRequest(nameInput.value);
    const res = await createChampionship(championship);
    if (res instanceof ValidationProblemDetails) {
        formMgr.setValidationState(res.toFormError());
    } else if (res instanceof ProblemDetails) {
        formMgr.setError(res.toFormError());
    } else if (res.data instanceof Championship) {
        await navigateToRoute('edit-championship', { championshipId: res.data.championshipId });
    }
}

onLoad(async () => {
    formMgr = form('form#create-championship');
    formMgr.onSubmit(onSubmitChampionship);
});