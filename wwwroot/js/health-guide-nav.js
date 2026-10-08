(() => {
    const mobile = window.matchMedia('(max-width: 600px)');
    let pendingFrame = false;
    const updateCurrentSection = () => {
        pendingFrame = false;
        document.querySelectorAll('[data-health-guide-nav]').forEach(nav => {
            const links = [...nav.querySelectorAll('.health-guide-nav-links a')];
            let current = null;
            for (const link of links) {
                const section = document.getElementById(new URL(link.href).hash.slice(1));
                if (!section) continue;
                // Account for the space added by the expanded mobile topic panel.
                const panelHeight = mobile.matches ? nav.querySelector('.health-guide-nav-panel').getBoundingClientRect().height : 0;
                const readingLine = parseFloat(getComputedStyle(section).scrollMarginTop) + panelHeight + 24;
                const bounds = section.getBoundingClientRect();
                if (bounds.top <= readingLine && bounds.bottom > readingLine) current = link;
            }
            links.forEach(link => {
                if (link === current) link.setAttribute('aria-current', 'location');
                else link.removeAttribute('aria-current');
            });
            const label = nav.querySelector('.health-guide-nav-toggle > span');
            const text = current ? `Explore: ${current.textContent.trim().replace(/\s+/g, ' ').replace(/^(\d+)\s*/, '$1 · ')}` : 'Explore this page';
            if (label.textContent !== text) label.textContent = text;
        });
    };
    const scheduleUpdate = () => {
        if (pendingFrame) return;
        pendingFrame = true;
        window.requestAnimationFrame(updateCurrentSection);
    };
    const setExpanded = (nav, expanded, restoreFocus = false) => {
        nav.classList.toggle('is-expanded', expanded);
        const toggle = nav.querySelector('.health-guide-nav-toggle');
        toggle.setAttribute('aria-expanded', String(expanded));
        nav.querySelector('.health-guide-nav-panel').inert = mobile.matches && !expanded;
        if (restoreFocus) toggle.focus();
        scheduleUpdate();
    };
    const initialize = () => document.querySelectorAll('[data-health-guide-nav]').forEach(nav => {
        nav.classList.add('is-ready');
        setExpanded(nav, false);
        scheduleUpdate();
    });
    document.addEventListener('click', event => {
        if (!(event.target instanceof Element)) return;
        const nav = event.target.closest('[data-health-guide-nav]');
        if (!nav) return;
        if (event.target.closest('.health-guide-nav-toggle')) {
            setExpanded(nav, !nav.classList.contains('is-expanded'));
        } else if (event.target.closest('a') && mobile.matches) {
            setExpanded(nav, false, true);
        }
    });
    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape' || !mobile.matches) return;
        const nav = document.querySelector('[data-health-guide-nav].is-expanded');
        if (nav) { event.preventDefault(); setExpanded(nav, false, true); }
    });
    mobile.addEventListener('change', initialize);
    window.addEventListener('scroll', scheduleUpdate, { passive: true });
    window.addEventListener('resize', scheduleUpdate, { passive: true });
    window.addEventListener('pageshow', scheduleUpdate);
    window.addEventListener('hashchange', scheduleUpdate);
    document.addEventListener('transitionend', event => {
        if (event.target instanceof Element && event.target.matches('.health-guide-nav-panel')) scheduleUpdate();
    });
    initialize();
    const connect = () => {
        if (window.Blazor?.addEventListener) window.Blazor.addEventListener('enhancedload', initialize);
        else window.setTimeout(connect, 25);
    };
    connect();
})();
