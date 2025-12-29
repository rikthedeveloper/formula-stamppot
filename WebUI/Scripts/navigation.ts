type HookFn = () => void | Promise<void>;

async function appendScript(host: Element, script: HTMLScriptElement): Promise<void> {
    let scriptElement = document.createElement('script');
    scriptElement.type = script.type || 'text/javascript';
    scriptElement.async = script.async || false;
    scriptElement.defer = script.defer || false;
    scriptElement.crossOrigin = script.crossOrigin || null;
    scriptElement.nonce = script.nonce || undefined;
    if (script.src !== undefined) {
        scriptElement.src = script.src;
    }
    if (script.textContent) {
        scriptElement.appendChild(document.createTextNode(script.innerText));
    }

    return new Promise((resolve, reject) => {
        scriptElement = host.appendChild(scriptElement);
        scriptElement.addEventListener('load', () => resolve())
        scriptElement.addEventListener('error', () => reject());
    });
}

async function ajax(url: URL): Promise<Document> {
    const resp = await fetch(url.href, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    });
    const document = new DOMParser().parseFromString(await resp.text(), 'text/html');
    return document;
}

async function handleAnchorClick(event: PointerEvent) {
    if (event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.shiftKey || event.altKey || event.metaKey)
        return;

    if (!event.target || !(event.target instanceof Element))
        return;

    const anchor = event.target.closest("a[href]");
    if (!anchor || !(anchor instanceof HTMLAnchorElement))
        return;

    if (anchor.target && anchor.target !== '_self' || anchor.hasAttribute('download'))
        return;

    if (anchor.href && anchor.href.startsWith(window.location.origin)) {
        event.preventDefault();
        const url = new URL(anchor.href);
        await navigate(url);
    }
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
            const promises = [];
            const hookResult = hook();
            if (hookResult instanceof Promise) {
                promises.push(hookResult);
            }

            await Promise.all(promises);
        }
    }
}

class Component {
    private _onLoad: Hook = new Hook();
    private _onUnload: Hook = new Hook();
    private _abort = new AbortController();

    constructor(private _path: string) { }

    get path() {
        return this._path;
    }

    get onLoad() {
        return this._onLoad;
    }

    get onUnload() {
        return this._onUnload;
    }

    get unloadSignal() {
        return this._abort.signal;
    }

    abort() {
        this._abort.abort('unload');
        this._abort = new AbortController();
    }
}

class AjaxNavigator {
    private _components: Map<string, Component> = new Map();
    private _currentComponent: Component;

    constructor(
        private routeHost: Element,
        private scriptHost: Element
    ) {
        const key = location.pathname;
        this._currentComponent = new Component(key);
        this._components.set(key, this._currentComponent);
        this.routeHost.setAttribute('route', key);

        const routesElement = document.head.querySelector('script[type="routes"]');
        const initialRoutes = JSON.parse(routesElement?.textContent || '{}');
        routes = { ...routes, ...initialRoutes };
        routesElement?.remove();

        document.addEventListener('DOMContentLoaded', async event => {
            this._currentComponent?.onLoad.fire();
        }, { once: true });
    }

    async navigate(url: URL) {
        // Load the new page via AJAX
        const html = await ajax(url);

        // Fire unload hooks for the previous component
        await this._currentComponent!.onUnload.fire();
        this._currentComponent!.abort();

        // Update the current component
        const key = url.pathname;
        let currentComponent = this._components.get(key);
        if (!currentComponent) {
            currentComponent = new Component(key);
            this._components.set(key, currentComponent);
        }
        this._currentComponent = currentComponent;
        routeUnloadSignal = this._currentComponent.unloadSignal;

        this.routeHost.setAttribute('route', key);
        window.history.pushState({}, '', url.pathname + url.search + url.hash);

        // Update the document head
        for (const element of html.head.children) {
            if (element instanceof HTMLTitleElement) {
                document.title = element.textContent;
            } else if (element instanceof HTMLScriptElement && element.type == 'routes') {
                const newRoutes = JSON.parse(element.textContent || '{}');
                routes = { ...routes, ...newRoutes };
            } else {
                const existingElement = document.head.querySelector(`${element.tagName}#${element.id}`);
                if (existingElement) {
                    document.head.replaceChild(element, existingElement);
                } else {
                    document.head.appendChild(element);
                }
            }
        }

        // Update the route host and script host
        this.routeHost.replaceChildren(...html.body.querySelectorAll(':scope > :not(script)'));
        const scriptElements = Array.from(<NodeListOf<HTMLScriptElement>>html.body.querySelectorAll(':scope > script'));
        await Promise.all(scriptElements.map(se => appendScript(this.scriptHost, se)));

        // Fire load hooks for the new component
        this._currentComponent.onLoad.fire();
    }

    onLoad(hook: HookFn) {
        this._currentComponent?.onLoad.register(hook);
    }

    onUnload(hook: HookFn) {
        this._currentComponent?.onUnload.register(hook);
    }
}

export let routeUnloadSignal: AbortSignal;
export let routes: { [key: string]: string } = {};

const routeHost = document.querySelector('main')!;
const scriptHost = document.querySelector('script:last-of-type')!.parentElement!;
const navigator = new AjaxNavigator(routeHost, scriptHost);
document.body.addEventListener('click', handleAnchorClick);

addEventListener('popstate', async event => {
    if (event.state) {
        await navigate(new URL(document.location.href));
    }
});

window.history.pushState({}, '', location.href);

export async function navigate(url: URL) {
    return await navigator.navigate(url);
}

export function onLoad(hook: HookFn) {
    navigator.onLoad(hook);
}

export function onUnload(hook: HookFn) {
    navigator.onUnload(hook);
}

export function route(name: string, params?: { [key: string]: string | number }): URL {
    const routeTemplate = routes[name];
    if (!routeTemplate) {
        throw new Error(`Route '${name}' not found`);
    }

    let urlStr = routeTemplate;
    if (params) {
        for (const [key, value] of Object.entries(params)) {
            urlStr = urlStr.replace(key.startsWith(':') ? key : `:${key}`, encodeURIComponent(value.toString()));
        }
    }
    return new URL(urlStr, location.origin);
}

export async function navigateToRoute(name: string, params?: { [key: string]: string | number }) {
    return await navigate(route(name, params));
}