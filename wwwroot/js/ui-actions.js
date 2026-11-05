(function () {
    'use strict';
    function setCourseImageFallback(image, failed) {
        if (image?.matches?.('img[data-course-image]')) image.hidden = failed;
    }
    document.addEventListener('error', function (event) {
        setCourseImageFallback(event.target, true);
    }, true);
    document.addEventListener('load', function (event) {
        setCourseImageFallback(event.target, false);
    }, true);
    document.querySelectorAll('img[data-course-image]').forEach(function (image) {
        if (image.complete && image.naturalWidth === 0) setCourseImageFallback(image, true);
    });
    document.addEventListener('click', function (event) {
        const button = event.target.closest('[data-quantity-step], [data-print], [data-my-orders], [data-analysis-type], [data-copy-response]');
        if (!button) return;
        if (button.hasAttribute('data-quantity-step')) {
            const form = button.closest('form');
            const input = form.querySelector('input[name="quantity"]');
            if (button.dataset.quantityStep === 'down') input.stepDown();
            else input.stepUp();
            form.requestSubmit();
        } else if (button.hasAttribute('data-print')) {
            window.print();
        } else if (button.hasAttribute('data-my-orders')) {
            window.location.assign('/Orders/MyOrders');
        } else if (button.hasAttribute('data-analysis-type')) {
            void window.generarAnalisis(button.dataset.analysisType);
        } else if (button.hasAttribute('data-copy-response')) {
            void window.copiarRespuesta();
        }
    });
    document.addEventListener('submit', function (event) {
        const form = event.target;
        if (form.hasAttribute('data-confirm') && !window.confirm(form.dataset.confirm)) event.preventDefault();
    });
})();
