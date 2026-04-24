(function () {
    document.addEventListener('click', async function (event) {
        const button = event.target.closest('[data-copy-media-url]');
        if (!button) {
            return;
        }

        const url = button.getAttribute('data-url') || '';
        if (!url || !navigator.clipboard) {
            return;
        }

        await navigator.clipboard.writeText(url);
        const copyLabel = button.getAttribute('data-copy-label') || button.textContent;
        button.textContent = button.getAttribute('data-copied-label') || copyLabel;
        window.setTimeout(function () {
            button.textContent = copyLabel;
        }, 1500);
    });
})();
