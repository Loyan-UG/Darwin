(function () {
    function renderTemplate(template, prefix, optionIndex, valueIndex, valueHtml) {
        return template.innerHTML
            .replaceAll('__prefix__', prefix)
            .replaceAll('__i__', optionIndex.toString())
            .replaceAll('__j__', valueIndex.toString())
            .replaceAll('__value__', valueHtml || '');
    }

    function initOptionsEditor(root) {
        const scope = root || document;
        const editor = scope.querySelector?.('#options-editor') || (scope.id === 'options-editor' ? scope : null);
        if (!editor || editor.dataset.optionsEditorBound === 'true') {
            return;
        }

        const optionsContainer = editor.querySelector('#options-container');
        const addOptionBtn = editor.querySelector('#btnAddOption');
        const optionTemplate = editor.querySelector('#addOnOptionTemplate');
        const valueTemplate = editor.querySelector('#addOnValueTemplate');
        if (!optionsContainer || !addOptionBtn || !optionTemplate || !valueTemplate) {
            return;
        }

        const prefix = editor.getAttribute('data-options-prefix') || 'Options';
        const nextOptionIndex = function () {
            let max = -1;
            optionsContainer.querySelectorAll('.option-item').forEach(function (item) {
                const idx = parseInt(item.getAttribute('data-index') || '-1', 10);
                if (!Number.isNaN(idx) && idx > max) max = idx;
            });
            return max + 1;
        };
        const renderValue = function (optionIndex, valueIndex) {
            return renderTemplate(valueTemplate, prefix, optionIndex, valueIndex);
        };
        const renderOption = function (optionIndex) {
            return renderTemplate(optionTemplate, prefix, optionIndex, 0, renderValue(optionIndex, 0));
        };

        addOptionBtn.addEventListener('click', function () {
            optionsContainer.insertAdjacentHTML('beforeend', renderOption(nextOptionIndex()));
        });

        optionsContainer.addEventListener('click', function (event) {
            const target = event.target.closest('button');
            if (!target) return;

            if (target.hasAttribute('data-remove-option')) {
                target.closest('.option-item')?.remove();
                return;
            }

            if (target.hasAttribute('data-add-value')) {
                const option = target.closest('.option-item');
                if (!option) return;

                const optionIndex = parseInt(option.getAttribute('data-index'), 10);
                const valuesContainer = option.querySelector('.values-container');
                if (!valuesContainer || Number.isNaN(optionIndex)) return;

                const valueIndex = valuesContainer.querySelectorAll('.value-item').length;
                valuesContainer.insertAdjacentHTML('beforeend', renderValue(optionIndex, valueIndex));
                return;
            }

            if (target.hasAttribute('data-remove-value')) {
                target.closest('.value-item')?.remove();
            }
        });

        editor.dataset.optionsEditorBound = 'true';
    }

    document.addEventListener('change', function (event) {
        const toggle = event.target.closest('[data-addon-toggle-all]');
        if (toggle) {
            const scope = toggle.closest('[data-addon-selection-scope]') || document;
            scope.querySelectorAll('[data-addon-row-check]').forEach(function (checkbox) {
                checkbox.checked = toggle.checked;
            });
            return;
        }

        const variant = event.target.closest('[data-addon-variant-selection]');
        if (!variant) {
            return;
        }

        const shell = variant.closest('#add-on-group-attach-variants-shell');
        const container = shell ? shell.querySelector('#selected-container') : document.getElementById('selected-container');
        const id = variant.getAttribute('data-id');
        if (!container || !id) {
            return;
        }

        const existing = container.querySelector('input[type="hidden"][name="SelectedVariantIds"][value="' + id + '"]');
        if (variant.checked && !existing) {
            const hidden = document.createElement('input');
            hidden.type = 'hidden';
            hidden.name = 'SelectedVariantIds';
            hidden.value = id;
            hidden.dataset.bindId = id;
            container.appendChild(hidden);
        } else if (!variant.checked && existing) {
            existing.remove();
        }
    });

    document.addEventListener('input', function (event) {
        const input = event.target.closest('[data-addon-currency-uppercase]');
        if (input) {
            input.value = input.value.toUpperCase();
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        initOptionsEditor(document);
    });

    document.body.addEventListener('htmx:afterSwap', function (event) {
        initOptionsEditor(event.target);
    });
})();
