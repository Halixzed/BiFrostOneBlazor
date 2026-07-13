// Watches for user interaction while the "Learn More" PDF overlay is open, and tells Blazor to
// close it after a period of inactivity so a kiosk left showing the PDF returns to the 3D scene
// on its own. Kept as a plain script (not part of the BlazorThreeJS bundle patches) since it has
// nothing to do with three.js.

const EVENTS = ["click", "touchstart", "mousemove", "keydown", "scroll", "wheel"];

let timeoutHandle = null;
let dotNetRef = null;
let iframeWindow = null;

function resetTimer(timeoutMs) {
    if (timeoutHandle) {
        clearTimeout(timeoutHandle);
    }
    timeoutHandle = setTimeout(() => {
        const ref = dotNetRef;
        window.portfolioStopPdfWatchdog();
        ref && ref.invokeMethodAsync("ClosePdfFromWatchdog");
    }, timeoutMs);
}

window.portfolioStartPdfWatchdog = function (dotNetObjectRef, timeoutMs, iframeElement) {
    window.portfolioStopPdfWatchdog();
    dotNetRef = dotNetObjectRef;

    const handler = () => resetTimer(timeoutMs);
    window.__portfolioPdfWatchdogHandler = handler;
    EVENTS.forEach((name) => document.addEventListener(name, handler));

    // The PDF renders inside a same-origin iframe, whose own document doesn't bubble events up
    // to the parent document - without this, scrolling/reading the PDF itself wouldn't count as
    // activity and it'd time out from under someone still reading it.
    if (iframeElement) {
        const attachToIframe = () => {
            try {
                iframeWindow = iframeElement.contentWindow;
                EVENTS.forEach((name) => iframeWindow.document.addEventListener(name, handler));
            } catch {
                // Ignore - if it's ever not same-origin, activity inside the iframe just won't count.
            }
        };
        if (iframeElement.contentDocument && iframeElement.contentDocument.readyState === "complete") {
            attachToIframe();
        } else {
            iframeElement.addEventListener("load", attachToIframe, { once: true });
        }
    }

    resetTimer(timeoutMs);
};

window.portfolioStopPdfWatchdog = function () {
    if (timeoutHandle) {
        clearTimeout(timeoutHandle);
        timeoutHandle = null;
    }
    const handler = window.__portfolioPdfWatchdogHandler;
    if (handler) {
        EVENTS.forEach((name) => document.removeEventListener(name, handler));
        if (iframeWindow) {
            try {
                EVENTS.forEach((name) => iframeWindow.document.removeEventListener(name, handler));
            } catch {
                // Ignore.
            }
        }
    }
    window.__portfolioPdfWatchdogHandler = null;
    iframeWindow = null;
    dotNetRef = null;
};
