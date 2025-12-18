import { navigate, onLoad } from "system/navigation";
import { CreateChampionshipRequest, createChampionship, ProblemDetails, ValidationProblemDetails, Championship, Hypermedia } from "api/formula-discord";

function getValidationUlId(member: string) {
    return `validation-messages-${member}`;
}

async function onSubmitChampionship(e: PointerEvent) {
    e.preventDefault();
    // TODO: Create a form API
    const form = document.querySelector('form#create-championship');
    if (!form)
        throw new Error('Form not found');

    // Remove any previous form-level error
    const prevFormError = form.querySelector('.form-error');
    if (prevFormError)
        prevFormError.remove();

    // Remove form-level aria-describedby if present
    form.removeAttribute('aria-describedby');

    // Remove all existing .validation-messages elements
    const prevMessages = form.querySelectorAll('.validation-messages');
    prevMessages.forEach(ul => ul.remove());

    // Remove aria-describedby and aria-invalid from all inputs
    const allInputs = form.querySelectorAll('input');
    allInputs.forEach(input => {
        input.setCustomValidity("");
        input.removeAttribute('aria-invalid');
        input.removeAttribute('aria-describedby');
    });

    const nameInput: HTMLInputElement | null = form.querySelector('input[for="name"]');
    if (!nameInput)
        throw new Error('Name input not found');

    const championship: CreateChampionshipRequest = new CreateChampionshipRequest(nameInput.value);
    const res = await createChampionship(championship);
    if (res instanceof ProblemDetails) {
        if (res instanceof ValidationProblemDetails) {
            for (const [member, messages] of Object.entries(res.validationMessages)) {
                const input: HTMLInputElement | null = form.querySelector(`input[for="${member}"]`);
                if (input) {
                    // Set custom validity
                    const errorText = messages.map(m => m.message).join("\n");
                    input.setCustomValidity(errorText);
                    // Create a new ul for validation messages
                    const ul = document.createElement('ul');
                    ul.className = 'validation-messages';
                    ul.id = getValidationUlId(member);
                    ul.setAttribute('role', 'alert');
                    for (const m of messages) {
                        const li = document.createElement('li');
                        li.textContent = m.message;
                        ul.appendChild(li);
                    }
                    // Insert after the input
                    if (input.nextSibling) {
                        input.parentNode?.insertBefore(ul, input.nextSibling);
                    } else {
                        input.parentNode?.appendChild(ul);
                    }

                    // Set ARIA attributes
                    input.setAttribute('aria-invalid', 'true');
                    input.setAttribute('aria-describedby', ul.id);
                }
            }
        }

        // Show form-level error at the top
        const errorDiv = document.createElement('div');
        errorDiv.className = 'form-error';
        errorDiv.setAttribute('role', 'alert');
        errorDiv.id = 'form-error';
        errorDiv.textContent = res.detail || res.title || "An error occurred.";
        form.insertBefore(errorDiv, form.firstChild);
        // Set form aria-describedby to point to the error
        form.setAttribute('aria-describedby', errorDiv.id);
    } else if (res.data instanceof Championship) {
        const linkTpl = document.head.querySelector("link[rel='linktemplate']#edit-championship");
        if (!linkTpl || !linkTpl.hasAttribute('href'))
            throw new Error('Edit championship link template not found');

        const urlTpl = decodeURIComponent(linkTpl.getAttribute('href')!).replace(':championshipId', res.data.championshipId);
        await navigate(new URL(urlTpl, location.origin));
    }
}

onLoad(async () => {
    const submitBtn: HTMLButtonElement | null = document.querySelector('button#submit-championship');
    if (!submitBtn)
        throw new Error('Submit button not found');
    submitBtn?.addEventListener('click', onSubmitChampionship);
});