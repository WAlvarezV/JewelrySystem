window.docLightbox = {
    init: function (containerSelector) {
        const container = document.querySelector(containerSelector);
        if (!container) return;

        // In-document links (e.g. [Ver Proceso 2](#proceso-2-creacion-joyas)).
        // Blazor would resolve "#id" against the base URI and navigate away from the page, so
        // they are intercepted here and turned into an in-place scroll. Bound once per container.
        if (!container.dataset.docAnchorsBound) {
            container.dataset.docAnchorsBound = 'true';
            container.addEventListener('click', function (event) {
                const link = event.target.closest('a[href^="#"]');
                if (!link || !container.contains(link)) return;

                const anchor = decodeURIComponent(link.getAttribute('href').substring(1));
                if (!anchor) return;

                event.preventDefault();
                event.stopPropagation();
                window.docLightbox.scrollToSection(containerSelector, anchor);
            });
        }

        const images = container.querySelectorAll('img');
        images.forEach(img => {
            img.style.cursor = 'zoom-in';
            img.style.transition = 'transform 0.2s, box-shadow 0.2s';
            
            img.onclick = function () {
                const overlay = document.createElement('div');
                overlay.style.position = 'fixed';
                overlay.style.top = '0';
                overlay.style.left = '0';
                overlay.style.width = '100vw';
                overlay.style.height = '100vh';
                overlay.style.backgroundColor = 'rgba(15, 23, 42, 0.85)';
                overlay.style.backdropFilter = 'blur(4px)';
                overlay.style.zIndex = '99999';
                overlay.style.display = 'flex';
                overlay.style.alignItems = 'center';
                overlay.style.justifyContent = 'center';
                overlay.style.cursor = 'zoom-out';

                const fullImg = document.createElement('img');
                fullImg.src = img.src;
                fullImg.alt = img.alt || 'Zoomed image';
                fullImg.style.maxWidth = '92vw';
                fullImg.style.maxHeight = '92vh';
                fullImg.style.borderRadius = '12px';
                fullImg.style.boxShadow = '0 25px 50px -12px rgba(0, 0, 0, 0.5)';
                fullImg.style.objectFit = 'contain';

                overlay.appendChild(fullImg);
                overlay.onclick = function () {
                    document.body.removeChild(overlay);
                };

                document.body.appendChild(overlay);
            };
        });
    },

    scrollToSection: function (containerSelector, sectionAnchor) {
        if (!sectionAnchor) return;

        const container = document.querySelector(containerSelector);
        if (!container) return;

        const targetEl = container.querySelector('#' + CSS.escape(sectionAnchor)) || document.getElementById(sectionAnchor);
        if (!targetEl) return;

        // If target is inside or is a <details>, open it and collapse others
        const parentDetails = targetEl.closest('details') || (targetEl.tagName.toLowerCase() === 'details' ? targetEl : null);
        
        if (parentDetails) {
            const allDetails = container.querySelectorAll('details');
            allDetails.forEach(d => {
                if (d === parentDetails) {
                    d.open = true;
                } else {
                    d.open = false;
                }
            });
        }

        setTimeout(() => {
            targetEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }, 150);
    }
};
