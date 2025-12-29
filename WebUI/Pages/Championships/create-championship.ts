import { navigate, onLoad } from "system/navigation";
import { CreateChampionshipRequest, createChampionship, ProblemDetails, ValidationProblemDetails, Championship } from "api/formula-discord";
import { FormManager, form } from "form";

let formMgr: FormManager;

async function onSubmitChampionship(e: SubmitEvent) {
    const nameInput: HTMLInputElement | null = formMgr.field('name');
    const championship: CreateChampionshipRequest = new CreateChampionshipRequest(nameInput.value);
    const res = await createChampionship(championship);
    if (res instanceof ValidationProblemDetails) {
        formMgr.setValidationState(res.toFormError());
    } else if (res instanceof ProblemDetails) {
        formMgr.setError(res.toFormError());
    } else if (res.data instanceof Championship) {
        const linkTpl = document.head.querySelector("link[rel='linktemplate']#edit-championship");
        if (!linkTpl || !linkTpl.hasAttribute('href'))
            throw new Error('Edit championship link template not found');

        const urlTpl = decodeURIComponent(linkTpl.getAttribute('href')!).replace(':championshipId', res.data.championshipId);
        await navigate(new URL(urlTpl, location.origin));
    }
}

onLoad(async () => {
    formMgr = form('form#create-championship');
    formMgr.onSubmit(onSubmitChampionship);
});