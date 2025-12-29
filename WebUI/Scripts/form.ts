import { routeUnloadSignal } from "./navigation.js";

export type FormError = {
    message: string;
}

export type ValidationResults = FormError & {
    results: {
        [fieldName: string]: string[];
    };
};

class FieldRef {
    private _errorsElement: HTMLUListElement | null = null;

    constructor(
        public name: string,
        public element: HTMLInputElement
    ) {
    }

    setErrors(errors: string[]) {
        // Remove existing errors element if any
        this._errorsElement?.remove();

        if (errors.length === 0) {
            this._errorsElement = null;
            this.element.setCustomValidity("");
            this.element.removeAttribute('aria-invalid');
            this.element.removeAttribute('aria-describedby');
            return;
        }

        // Create a new ul for validation messages
        this._errorsElement = document.createElement('ul');
        this._errorsElement.className = 'validation-messages';
        this.element.id = 'validation-messages-' + this.name;
        this._errorsElement.setAttribute('role', 'alert');
        for (const error of errors) {
            const li = document.createElement('li');
            li.textContent = error;
            this._errorsElement.appendChild(li);
        }

        // Insert after the input
        if (this.element.nextSibling) {
            this.element.parentNode!.insertBefore(this._errorsElement, this.element.nextSibling);
        } else {
            this.element.parentNode!.appendChild(this._errorsElement);
        }
    }
}

export class FormManager {
    private _fields = new Map<string, FieldRef>();
    private _errorMessageElement: HTMLDivElement | null = null;

    constructor(public formElement: HTMLFormElement) {

    }

    field(name: string): HTMLInputElement;
    field(name: string, optional: boolean): HTMLInputElement | null;
    field(name: string, optional: boolean = false): HTMLInputElement | null {
        const cachedField = this._fields.get(name);
        if (cachedField) {
            return cachedField.element;
        }

        const input = this.formElement.querySelector(`input[name="${name}"]`);
        if (!input) {
            if (optional)
                return null;
            throw new Error(`Input field "${name}" not found`);
        }

        if (!(input instanceof HTMLInputElement)) {
            if (optional)
                return null;
            throw new Error(`Field "${name}" is not an input element`);
        }

        this._fields.set(name, new FieldRef(name, input));
        return input;
    }

    onSubmit(handler: (e: SubmitEvent) => void, opts?: {
        removeOnRouteChange: boolean;
        signal: AbortSignal;
    }) {
        function onSubmitHandlerWrapper(e: SubmitEvent) {
            e.preventDefault();
            handler(e);
        }

        const handlerOpts: AddEventListenerOptions = {};
        if (opts && (opts.removeOnRouteChange || opts.removeOnRouteChange === undefined) && !opts.signal) {
            handlerOpts.signal = routeUnloadSignal;
        }
        else if (opts && opts.signal) {
            handlerOpts.signal = opts.signal;
        }

        this.formElement.addEventListener('submit', onSubmitHandlerWrapper, handlerOpts);
    }

    setError(error: FormError) {
        if (this._errorMessageElement) {
            this._errorMessageElement.remove();
            this.formElement.removeAttribute('aria-describedby');
        }

        if (error && error.message) {
            this._errorMessageElement = document.createElement('div');
            this._errorMessageElement.className = 'form-error';
            this._errorMessageElement.setAttribute('role', 'alert');
            this._errorMessageElement.id = 'form-error';
            this._errorMessageElement.textContent = error.message;
            this.formElement.insertBefore(this._errorMessageElement, this.formElement.firstChild);
            this.formElement.setAttribute('aria-describedby', this._errorMessageElement.id);
        }
    }

    setValidationState(state: ValidationResults) {
        this.setError(state);

        for (const fieldRef of this._fields.values()) {
            fieldRef.setErrors([]);
        }

        for (const [fieldName, result] of Object.entries(state.results)) {
            const fieldRef = this._fields.get(fieldName);
            if (!fieldRef)
                continue;

            fieldRef.setErrors(result);
        }
    }
}

export function form(selector: string) {
    const form = document.querySelector(selector);
    if (!form)
        throw new Error('Form not found');

    if (!(form instanceof HTMLFormElement))
        throw new Error('Element is not a form');

    return new FormManager(form);
}