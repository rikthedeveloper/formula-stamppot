import { navigate } from 'system/navigation';

async function handleAnchorClick(event: PointerEvent) {
    if (event.ctrlKey || event.shiftKey || event.altKey || event.metaKey)
        return;

    if (event.target instanceof HTMLAnchorElement) {
        const anchor = event.target;
        if (anchor.target && anchor.target !== '_self')
            return;

        if (anchor.href && anchor.href.startsWith(window.location.origin)) {
            event.preventDefault();
            const url = new URL(anchor.href);
            await navigate(url);
        }
    }
}

function handleAnchorRemoved(anchor: HTMLElement) {
    anchor.removeEventListener('click', handleAnchorClick);
}

function handleAnchorAdded(anchor: HTMLElement) {
    anchor.addEventListener('click', handleAnchorClick);
}


function handleMutation(mutation: MutationRecord) {
    function isElement(node: Node): node is HTMLElement {
        return node.nodeType === Node.ELEMENT_NODE && node instanceof HTMLElement;
    }

    function applyToAnchor(fn: (el: HTMLElement) => void): (node: Node) => void {
        return node => {
            if (isElement(node)) {
                if (node instanceof HTMLAnchorElement) {
                    fn(node);
                } else {
                    node.querySelectorAll('a').forEach(fn);
                }
            }
        }
    }

    mutation.removedNodes.forEach(applyToAnchor(handleAnchorRemoved));
    mutation.addedNodes.forEach(applyToAnchor(handleAnchorAdded));
}

addEventListener('popstate', async event => {
    if (event.state) {
        await navigate(new URL(document.location.href));
    }
});

const observer = new MutationObserver(mutations => mutations.forEach(handleMutation));
const main = document.querySelector('main');
if (main) {
    observer.observe(main, { childList: true, subtree: true });
    main.querySelectorAll('a').forEach(handleAnchorAdded);
}
window.history.pushState({}, '', location.href);