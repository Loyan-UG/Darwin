(function () {
    window.darwinAdmin = window.darwinAdmin || {};

    window.darwinAdmin.initBootstrapUi = function (root) {
        const scope = root || document;
        scope.querySelectorAll('[data-bs-toggle="popover"]').forEach(function (el) {
            bootstrap.Popover.getOrCreateInstance(el);
        });
        scope.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            bootstrap.Tooltip.getOrCreateInstance(el);
        });
    };

    window.darwinAdmin.refreshAlerts = function (url) {
        const targetUrl = url || document.body.dataset.alertsUrl;
        if (!targetUrl || !window.htmx) {
            return;
        }

        htmx.ajax('GET', targetUrl, '#alerts-container');
    };

    window.darwinAdmin.hideModal = function (modalId) {
        const modalEl = document.getElementById(modalId);
        if (!modalEl || !window.bootstrap) {
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.hide();
    };

    window.darwinAdmin.hideAddressModal = function () {
        window.darwinAdmin.hideModal('addressEditModal');
    };

    window.darwinAdmin.initUserEditScreen = function (root) {
        if (!root) return;

        const addAddressTitle = root.dataset.addAddressTitle || 'Address';
        const editAddressTitle = root.dataset.editAddressTitle || 'Address';

        const modal = root.querySelector('#addressEditModal');
        if (modal && !modal.dataset.addressModalBound) {
            modal.dataset.addressModalBound = 'true';

            modal.addEventListener('show.bs.modal', function (event) {
                const btn = event.relatedTarget;
                if (!btn) return;

                const mode = btn.getAttribute('data-mode') || 'create';
                const form = modal.querySelector('#addressEditForm');
                if (!form) return;

                const idInput = modal.querySelector('#addrId');
                const rvInput = modal.querySelector('#addrRowVersion');
                const userInput = modal.querySelector('#addrUserId');
                const title = modal.querySelector('#addressEditModalLabel');
                const action = btn.getAttribute('data-action') || '';

                form.setAttribute('action', action);
                form.setAttribute('hx-post', action);

                if (userInput) {
                    userInput.value = btn.getAttribute('data-userid') || '';
                }

                const removeName = function (el) { if (el) el.removeAttribute('name'); };
                const setName = function (el, name) { if (el) el.setAttribute('name', name); };
                const setValue = function (name, value) {
                    const input = form.querySelector(`input[name="${name}"]`);
                    if (input) input.value = value;
                };
                const setChecked = function (name, value) {
                    const input = form.querySelector(`input[name="${name}"]`);
                    if (input) input.checked = value;
                };

                if (mode === 'create') {
                    if (idInput) { idInput.value = ''; removeName(idInput); }
                    if (rvInput) { rvInput.value = ''; removeName(rvInput); }

                    setValue('FullName', '');
                    setValue('Company', '');
                    setValue('Street1', '');
                    setValue('Street2', '');
                    setValue('PostalCode', '');
                    setValue('City', '');
                    setValue('State', '');
                    setValue('CountryCode', 'DE');
                    setValue('PhoneE164', '');
                    setChecked('IsDefaultBilling', false);
                    setChecked('IsDefaultShipping', false);

                    if (title) title.textContent = addAddressTitle;
                } else {
                    if (idInput) { idInput.value = btn.getAttribute('data-id') || ''; setName(idInput, 'Id'); }
                    if (rvInput) { rvInput.value = btn.getAttribute('data-rowversion') || ''; setName(rvInput, 'RowVersion'); }

                    setValue('FullName', btn.getAttribute('data-fullname') || '');
                    setValue('Company', btn.getAttribute('data-company') || '');
                    setValue('Street1', btn.getAttribute('data-street1') || '');
                    setValue('Street2', btn.getAttribute('data-street2') || '');
                    setValue('PostalCode', btn.getAttribute('data-postalcode') || '');
                    setValue('City', btn.getAttribute('data-city') || '');
                    setValue('State', btn.getAttribute('data-state') || '');
                    setValue('CountryCode', btn.getAttribute('data-countrycode') || 'DE');
                    setValue('PhoneE164', btn.getAttribute('data-phonee164') || '');
                    setChecked('IsDefaultBilling', btn.getAttribute('data-defaultbilling') === 'true');
                    setChecked('IsDefaultShipping', btn.getAttribute('data-defaultshipping') === 'true');

                    if (title) title.textContent = editAddressTitle;
                }
            });
        }

        const countryCode = root.querySelector('#addrCountryCode');
        if (countryCode && !countryCode.dataset.uppercaseBound) {
            countryCode.dataset.uppercaseBound = 'true';
            countryCode.addEventListener('input', function () {
                this.value = this.value.toUpperCase();
            });
        }
    };

    window.darwinAdmin.configureConfirmDeleteModal = function (event) {
        const modalEl = event.target;
        if (!modalEl || modalEl.id !== 'confirmDeleteModal') {
            return;
        }

        const button = event.relatedTarget;
        const form = document.getElementById('confirmDeleteForm');
        if (!button || !form) {
            return;
        }

        const nameSpan = document.getElementById('confirmDeleteName');
        const idInput = document.getElementById('confirmDeleteId');
        const rowVersionInput = document.getElementById('confirmDeleteRowVersion');
        const userIdInput = form.querySelector('input[name="userId"]');
        const actionUrl = button.getAttribute('data-action');
        const hxTarget = button.getAttribute('data-hx-target');
        const hxSwap = button.getAttribute('data-hx-swap') || 'innerHTML';

        if (nameSpan) nameSpan.textContent = button.getAttribute('data-name') || '';
        if (idInput) idInput.value = button.getAttribute('data-id') || '';
        if (rowVersionInput) rowVersionInput.value = button.getAttribute('data-rowversion') || '';
        if (userIdInput) userIdInput.value = button.getAttribute('data-userid') || '';

        if (actionUrl) {
            form.setAttribute('action', actionUrl);
        } else {
            form.removeAttribute('action');
        }

        if (hxTarget && actionUrl) {
            form.setAttribute('hx-post', actionUrl);
            form.setAttribute('hx-target', hxTarget);
            form.setAttribute('hx-swap', hxSwap);
        } else {
            form.removeAttribute('hx-post');
            form.removeAttribute('hx-target');
            form.removeAttribute('hx-swap');
        }
    };

    document.addEventListener('DOMContentLoaded', function () {
        window.darwinAdmin.initBootstrapUi(document);
        window.darwinAdmin.initUserEditScreen(document.getElementById('user-edit-editor-shell'));
    });

    document.addEventListener('show.bs.modal', window.darwinAdmin.configureConfirmDeleteModal);

    document.addEventListener('change', function (event) {
        const switcher = event.target.closest('[data-culture-switcher]');
        if (switcher && switcher.form) {
            switcher.form.submit();
        }
    });

    document.addEventListener('click', function (event) {
        const passkeyButton = event.target.closest('[data-passkey-preparing-label]');
        if (passkeyButton) {
            alert(passkeyButton.getAttribute('data-passkey-preparing-label') || '');
        }
    });

    document.body.addEventListener('htmx:configRequest', function (event) {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';
        if (token) {
            event.detail.headers.RequestVerificationToken = token;
        }
    });

    document.body.addEventListener('htmx:afterSwap', function (event) {
        window.darwinAdmin.initBootstrapUi(event.target);
        window.darwinAdmin.initUserEditScreen(event.target.querySelector?.('#user-edit-editor-shell') || event.target);
    });

    document.body.addEventListener('htmx:afterRequest', function (event) {
        if (!event.detail || !event.detail.successful || !event.detail.elt) {
            return;
        }

        if (event.detail.elt.hasAttribute('data-refresh-alerts')) {
            window.darwinAdmin.refreshAlerts();
        }

        if (event.detail.elt.hasAttribute('data-hide-address-modal') && window.darwinAdmin.hideAddressModal) {
            window.darwinAdmin.hideAddressModal();
        }

        if (event.detail.elt.id === 'confirmDeleteForm') {
            window.darwinAdmin.refreshAlerts();
            window.darwinAdmin.hideModal('confirmDeleteModal');
        }
    });
})();
