(function () {
    'use strict';
    window.BioGamaMarkdown = {
        render: function (text) {
            return window.DOMPurify.sanitize(window.marked.parse(text || ''), {
                USE_PROFILES: { html: true }
            });
        }
    };
})();
