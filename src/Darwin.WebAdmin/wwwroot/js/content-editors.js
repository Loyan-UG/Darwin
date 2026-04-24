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

    function bindSubmit(form, selector, dataKey) {
        if (!form || form.dataset[dataKey] === 'true') {
            return;
        }

        form.addEventListener('submit', function () {
            form.querySelectorAll(selector).forEach(function (editorEl) {
                const editorHidden = editorEl.parentElement.querySelector('textarea');
                if (editorHidden && editorEl.__quill) {
                    editorHidden.value = editorEl.__quill.root.innerHTML;
                }
            });
        });
        form.dataset[dataKey] = 'true';
    }

    function initEditors(root, selector, options) {
        const scope = root || document;
        if (!window.Quill) {
            if (options.notLoadedMessage) {
                console.error(options.notLoadedMessage);
            }
            return;
        }

        scope.querySelectorAll(selector).forEach(function (el) {
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
            el.dataset.quillInitialized = 'true';
        });
    }

    function pageToolbar(root) {
        const uploadUrl = root.dataset.pageImageUploadUrl || '';
        const uploadFailedMessage = root.dataset.pageImageUploadFailed || '';
        const uploadFailedError = root.dataset.pageEditorUploadFailedError || 'Upload failed';

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
                            const resp = await fetch(uploadUrl, {
                                method: 'POST',
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
        const configRoot = scope.querySelector?.('#page-editor-shell') || document.getElementById('page-editor-shell') || scope;
        initEditors(scope, '[data-page-quill-editor="true"]', {
            placeholder: configRoot.dataset.pageEditorPlaceholder || '',
            notLoadedMessage: configRoot.dataset.pageEditorQuillNotLoaded || '',
            toolbar: pageToolbar(configRoot),
            submitDataKey: 'pageQuillSubmitBound'
        });
    };

    window.darwinAdmin.initProductEditors = function (root) {
        const scope = root || document;
        const configRoot = scope.querySelector?.('#product-editor-shell') || document.getElementById('product-editor-shell') || scope;
        initEditors(scope, '[data-quill-product-editor="true"]', {
            placeholder: configRoot.dataset.productDescriptionPlaceholder || '',
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
