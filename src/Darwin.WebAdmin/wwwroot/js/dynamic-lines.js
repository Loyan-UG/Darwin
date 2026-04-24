(function () {
    document.addEventListener('click', function (event) {
        const addButton = event.target.closest('[data-dynamic-lines-add]');
        if (addButton) {
            const containerSelector = addButton.getAttribute('data-dynamic-lines-container');
            const templateSelector = addButton.getAttribute('data-dynamic-lines-template');
            const rowSelector = addButton.getAttribute('data-dynamic-lines-row') || '.line-row';
            const container = containerSelector ? document.querySelector(containerSelector) : null;
            const template = templateSelector ? document.querySelector(templateSelector) : null;
            if (!container || !template) {
                return;
            }

            const index = container.querySelectorAll(rowSelector).length;
            container.insertAdjacentHTML('beforeend', template.innerHTML.replaceAll('__index__', index.toString()));
            return;
        }

        const removeButton = event.target.closest('[data-dynamic-lines-remove]');
        if (removeButton) {
            removeButton.closest(removeButton.getAttribute('data-dynamic-lines-row') || '.line-row')?.remove();
        }
    });
})();
