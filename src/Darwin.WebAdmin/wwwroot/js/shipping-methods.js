(function () {
    window.darwinAdmin = window.darwinAdmin || {};

    function renumberShippingRateRows() {
        const rows = document.querySelectorAll('#shipping-rate-rows [data-rate-row]');
        rows.forEach(function (row, index) {
            row.querySelectorAll('input').forEach(function (input) {
                const currentName = input.getAttribute('name');
                if (!currentName) return;
                input.setAttribute('name', currentName.replace(/Rates\[\d+\]/, 'Rates[' + index + ']'));
                if (currentName.endsWith('.SortOrder')) {
                    input.value = index;
                }
            });
        });
    }

    window.darwinAdmin.addShippingRateRow = function () {
        const template = document.getElementById('shipping-rate-row-template');
        const tbody = document.getElementById('shipping-rate-rows');
        if (!template || !tbody) return;

        const index = tbody.querySelectorAll('[data-rate-row]').length;
        const html = template.innerHTML.replaceAll('__index__', index.toString());
        tbody.insertAdjacentHTML('beforeend', html);
        renumberShippingRateRows();
    };

    window.darwinAdmin.removeShippingRateRow = function (button) {
        const row = button.closest('[data-rate-row]');
        const tbody = document.getElementById('shipping-rate-rows');
        if (!row || !tbody) return;

        row.remove();
        if (!tbody.querySelector('[data-rate-row]')) {
            window.darwinAdmin.addShippingRateRow();
        } else {
            renumberShippingRateRows();
        }
    };

    window.darwinAdmin.initShippingMethodForm = function () {
        renumberShippingRateRows();
    };

    document.addEventListener('click', function (event) {
        const addButton = event.target.closest('[data-shipping-rate-add]');
        if (addButton) {
            window.darwinAdmin.addShippingRateRow();
            return;
        }

        const removeButton = event.target.closest('[data-shipping-rate-remove]');
        if (removeButton) {
            window.darwinAdmin.removeShippingRateRow(removeButton);
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        window.darwinAdmin.initShippingMethodForm();
    });

    document.body.addEventListener('htmx:afterSwap', function () {
        window.darwinAdmin.initShippingMethodForm();
    });
})();
