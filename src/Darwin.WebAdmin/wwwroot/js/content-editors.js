(function () {
    window.darwinAdmin = window.darwinAdmin || {};

    const toolbar = [
        [{ header: [1, 2, 3, false] }],
        ['bold', 'italic', 'underline', 'strike'],
        [{ list: 'ordered' }, { list: 'bullet' }],
        ['link', 'image', 'video'],
        ['blockquote', 'code-block'],
        [{ align: [] }],
        ['clean']
    ];

    function syncEditorValue(editorEl) {
        const editorHidden = editorEl?.parentElement?.querySelector('textarea');
        if (editorHidden && editorEl.__quill) {
            editorHidden.value = editorEl.__quill.root.innerHTML;
        }
    }

    function bindSubmit(form, selector, dataKey) {
        if (!form || form.dataset[dataKey] === 'true') {
            return;
        }

        form.addEventListener('submit', function () {
            form.querySelectorAll(selector).forEach(function (editorEl) {
                syncEditorValue(editorEl);
            });
        }, true);
        form.dataset[dataKey] = 'true';
    }

    function initEditors(root, selector, options) {
        const scope = root || document;
        const editors = scope.querySelectorAll?.(selector) || [];
        if (editors.length === 0) {
            return;
        }

        if (!window.Quill) {
            if (options.notLoadedMessage) {
                console.error(options.notLoadedMessage);
            }
            return;
        }

        editors.forEach(function (el) {
            if (el.dataset.quillInitialized === 'true') {
                return;
            }

            const hidden = el.parentElement.querySelector('textarea');
            const quillOptions = {
                theme: 'snow',
                placeholder: options.placeholder,
                modules: {
                    toolbar: options.toolbar
                }
            };

            const quill = new Quill(el, quillOptions);
            if (hidden && hidden.value) {
                quill.root.innerHTML = hidden.value;
            }

            bindSubmit(el.closest('form'), selector, options.submitDataKey);
            el.__quill = quill;
            quill.on('text-change', function () {
                syncEditorValue(el);
            });
            syncEditorValue(el);
            el.dataset.quillInitialized = 'true';
        });
    }

    function resolveConfigRoot(scope, id) {
        if (scope?.nodeType === Node.ELEMENT_NODE && scope.id === id) {
            return scope;
        }

        return scope?.querySelector?.(`#${id}`) || document.getElementById(id);
    }

    function pageToolbar(root) {
        const dataset = root?.dataset || {};
        const uploadUrl = dataset.pageImageUploadUrl || '';
        const uploadFailedMessage = dataset.pageImageUploadFailed || '';
        const uploadFailedError = dataset.pageEditorUploadFailedError || 'Upload failed';

        return {
            container: toolbar,
            handlers: {
                image: function () {
                    const quill = this.quill;
                    const input = document.createElement('input');
                    input.type = 'file';
                    input.accept = 'image/*';
                    input.onchange = async function () {
                        const file = input.files[0];
                        if (!file || !uploadUrl) {
                            return;
                        }

                        const formData = new FormData();
                        formData.append('file', file);

                        try {
                            const token = root.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
                            const headers = token ? { RequestVerificationToken: token } : {};
                            const resp = await fetch(uploadUrl, {
                                method: 'POST',
                                headers,
                                body: formData
                            });

                            if (!resp.ok) {
                                throw new Error(uploadFailedError);
                            }

                            const json = await resp.json();
                            const range = quill.getSelection(true);
                            quill.insertEmbed(range.index, 'image', json.url);
                            quill.setSelection(range.index + 1);
                        } catch {
                            alert(uploadFailedMessage);
                        }
                    };
                    input.click();
                }
            }
        };
    }

    window.darwinAdmin.initPageEditors = function (root) {
        const scope = root || document;
        const configRoot = resolveConfigRoot(scope, 'page-editor-shell');
        const dataset = configRoot?.dataset || {};
        initEditors(scope, '[data-page-quill-editor="true"]', {
            placeholder: dataset.pageEditorPlaceholder || '',
            notLoadedMessage: dataset.pageEditorQuillNotLoaded || '',
            toolbar: pageToolbar(configRoot),
            submitDataKey: 'pageQuillSubmitBound'
        });
    };

    window.darwinAdmin.initProductEditors = function (root) {
        const scope = root || document;
        const configRoot = resolveConfigRoot(scope, 'product-editor-shell');
        const dataset = configRoot?.dataset || {};
        initEditors(scope, '[data-quill-product-editor="true"]', {
            placeholder: dataset.productDescriptionPlaceholder || '',
            toolbar: toolbar,
            submitDataKey: 'quillSubmitBound'
        });
    };

    function initContentEditors(root) {
        window.darwinAdmin.initPageEditors(root);
        window.darwinAdmin.initProductEditors(root);
    }

    document.addEventListener('DOMContentLoaded', function () {
        initContentEditors(document);
    });

    document.body.addEventListener('htmx:afterSwap', function (event) {
        initContentEditors(event.target);
    });
})();
