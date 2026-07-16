// <input type="color">'s input/change events have historically been unreliable across some
// mobile/tablet browsers' native color-picker sheets - Blazor Server's own event handling relies
// on a single delegated listener at the document root, and on some devices that delegation never
// sees these events fire at all (the swatch updates natively, but nothing reaches the server).
// Attaching native listeners directly to the element sidesteps that delegation path entirely.

window.portfolioWireColorPicker = function (element, dotNetRef) {
    if (!element || element.__portfolioColorWired) {
        return;
    }
    element.__portfolioColorWired = true;

    const notify = () => dotNetRef.invokeMethodAsync("OnColorPickerChanged", element.value);
    element.addEventListener("input", notify);
    element.addEventListener("change", notify);
};
