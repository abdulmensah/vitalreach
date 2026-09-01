(() => {
    const buttonId = "scroll-to-top";
    let previousPathname = window.location.pathname;
    let shopScrollPosition = null;

    const updateButton = () => {
        document.getElementById(buttonId)?.classList.toggle("visible", window.scrollY > 500);
    };

    const bindButton = () => {
        const button = document.getElementById(buttonId);
        if (!button || button.dataset.bound === "true") return;

        button.dataset.bound = "true";
        button.addEventListener("click", () => window.scrollTo({ top: 0, left: 0, behavior: "smooth" }));
        updateButton();
    };

    const resetPageScroll = () => {
        window.requestAnimationFrame(() => {
            const currentPathname = window.location.pathname;
            const isContactInquiry = window.location.pathname === "/contact"
                && new URLSearchParams(window.location.search).has("message");
            const isShopFilterChange = currentPathname === "/shop" && previousPathname === currentPathname;

            if (isContactInquiry) {
                document.getElementById("contact-content")?.scrollIntoView({ block: "start", behavior: "instant" });
            } else if (isShopFilterChange) {
                const position = shopScrollPosition ?? window.scrollY;
                window.setTimeout(() => {
                    window.scrollTo({ top: position, left: 0, behavior: "instant" });
                    updateButton();
                }, 0);
            } else if (!window.location.hash) {
                window.scrollTo({ top: 0, left: 0, behavior: "instant" });
            }

            shopScrollPosition = null;
            previousPathname = currentPathname;
            bindButton();
            updateButton();
        });
    };

    window.addEventListener("scroll", updateButton, { passive: true });
    window.addEventListener("pageshow", updateButton);
    document.addEventListener("click", event => {
        const link = event.target.closest("a[href]");
        if (!link || window.location.pathname !== "/shop") return;

        const target = new URL(link.href, window.location.href);
        if (target.origin === window.location.origin && target.pathname === "/shop")
            shopScrollPosition = window.scrollY;
    });
    document.addEventListener("submit", event => {
        if (window.location.pathname === "/shop" && event.target.matches("form.shop-search"))
            shopScrollPosition = window.scrollY;
    });
    bindButton();

    const connectEnhancedNavigation = () => {
        if (window.Blazor?.addEventListener) {
            window.Blazor.addEventListener("enhancedload", resetPageScroll);
            return;
        }

        window.setTimeout(connectEnhancedNavigation, 25);
    };

    connectEnhancedNavigation();
})();
