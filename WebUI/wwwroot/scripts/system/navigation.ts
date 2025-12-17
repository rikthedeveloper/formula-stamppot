export type HookFn = () => void | Promise<void>;

function createScriptElement(inactiveScript: HTMLScriptElement): HTMLScriptElement {
    let scriptElement = document.createElement('script');
    scriptElement.type = inactiveScript.type || 'text/javascript';
    scriptElement.async = inactiveScript.async || false;
    scriptElement.defer = inactiveScript.defer || false;
    scriptElement.crossOrigin = inactiveScript.crossOrigin || null;
    scriptElement.nonce = inactiveScript.nonce || undefined;
    if (inactiveScript.src !== undefined) {
        scriptElement.src = inactiveScript.src;
    }
    if (inactiveScript.textContent) {
        scriptElement.appendChild(document.createTextNode(inactiveScript.innerText));
    }
    return scriptElement;
}

async function appendScript(host: Element, script: HTMLScriptElement): Promise<void> {
    return new Promise((resolve, reject) => {
        script = host.appendChild(script);
        script.addEventListener('load', () => resolve())
        script.addEventListener('error', () => reject());
    });
}

async function ajax(url: URL): Promise<Document> {
    const resp = await fetch(url.href, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    });
    const document = new DOMParser().parseFromString(await resp.text(), 'text/html');
    return document;
}

class Hook {
    private functions: HookFn[] = [];

    register(hook: HookFn) {
        if (hook === undefined || hook === null) {
            throw new Error('Hook cannot be null or undefined');
        }

        if (this.functions.includes(hook)) {
            throw new Error('Hook already registered');
        }

        this.functions.push(hook);
    }

    async fire() {
        for (const hook of this.functions) {
            const promise = hook();
            if (promise instanceof Promise) {
                await promise;
            }
        }
    }
}

class Component {
    private _onLoad: Hook = new Hook();
    private _onUnload: Hook = new Hook();
    constructor(private path: string) { }

    get Path() {
        return this.path;
    }

    get onLoad() {
        return this._onLoad;
    }

    get onUnload() {
        return this._onUnload;
    }
}

class ComponentStore {
    private components: Map<string, Component> = new Map();

    constructor(
        private _currentComponent: () => string
    ) { }
    /**
     * Gets the current component, based on the set component key.
     */
    get current(): Component | undefined {
        const key = this._currentComponent();
        return this.components.get(key);
    }

    beforeLoadComponent(key: string): void {
        if (!this.components.has(key)) {
            this.components.set(key, new Component(key));
        }
        document.querySelector('main')!.setAttribute('route', key);
    }
}

class AjaxNavigator {
    private readonly _routeHost = document.querySelector('[route]');
    private readonly _scriptHost = document.querySelector('script:last-of-type')!.parentElement;
    readonly componentStore = new ComponentStore(() => this._routeHost!.getAttribute('route')!);

    async navigate(url: URL) {
        const html = await ajax(url);
        const previousComponent = this.componentStore.current;
        await previousComponent!.onUnload.fire();
        this.componentStore.beforeLoadComponent(url.pathname);
        window.history.pushState({}, '', url.pathname + url.search + url.hash);

        for (const element of html.head.children) {
            if (element instanceof HTMLTitleElement) {
                document.title = element.textContent;
            } else {
                const existingElement = document.head.querySelector(`${element.tagName}#${element.id}`);
                if (existingElement) {
                    document.head.replaceChild(element, existingElement);
                } else {
                    document.head.appendChild(element);
                }
            }
        }

        this._routeHost!.replaceChildren(...html.body.querySelectorAll(':scope > :not(script)'));
        const scriptElements = Array.from(<NodeListOf<HTMLScriptElement>>html.body.querySelectorAll(':scope > script'));
        await Promise.all(scriptElements.map(createScriptElement).map(se => appendScript(this._scriptHost!, se)));
        this.componentStore.current!.onLoad.fire();
    }
}

const navigator: AjaxNavigator = new AjaxNavigator();
navigator.componentStore.beforeLoadComponent(location.pathname);

addEventListener('DOMContentLoaded', async event => {
    await navigator.componentStore.current!.onLoad.fire();
});

export async function navigate(url: URL) {
    return await navigator.navigate(url);
}

export function onLoad(hook: HookFn) {
    navigator.componentStore.current!.onLoad.register(hook);
}

export function onUnload(hook: HookFn) {
    navigator.componentStore.current!.onUnload.register(hook);
}
