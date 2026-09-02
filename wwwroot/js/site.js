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

    const updateProductCarousel = carousel => {
        const track = carousel?.querySelector("[data-carousel-track]");
        const cards = track ? [...track.children] : [];
        if (!track || cards.length === 0) return 0;

        const cardLeft = card => card.offsetLeft - track.offsetLeft;
        const activeIndex = cards.reduce((closest, card, index) =>
            Math.abs(cardLeft(card) - track.scrollLeft) < Math.abs(cardLeft(cards[closest]) - track.scrollLeft)
                ? index
                : closest, 0);
        const status = carousel.querySelector("[data-carousel-status]");
        const dots = [...carousel.querySelectorAll("[data-carousel-index]")];

        if (status) status.textContent = `${activeIndex + 1} / ${cards.length}`;
        dots.forEach((dot, index) => {
            const current = index === activeIndex;
            dot.classList.toggle("active", current);
            if (current) dot.setAttribute("aria-current", "true");
            else dot.removeAttribute("aria-current");
        });
        const previous = carousel.querySelector("[data-carousel-prev]");
        const next = carousel.querySelector("[data-carousel-next]");
        if (previous) previous.disabled = activeIndex === 0;
        if (next) next.disabled = activeIndex === cards.length - 1;
        return activeIndex;
    };

    const moveProductCarousel = (carousel, index) => {
        const track = carousel.querySelector("[data-carousel-track]");
        const cards = [...track.children];
        const target = Math.max(0, Math.min(cards.length - 1, index));
        track.scrollTo({ left: cards[target].offsetLeft - track.offsetLeft, behavior: "smooth" });
    };

    const bindProductCarousels = () => {
        document.querySelectorAll("[data-product-carousel]").forEach(updateProductCarousel);
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
            bindProductCarousels();
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
    document.addEventListener("click", event => {
        const control = event.target.closest("[data-carousel-prev], [data-carousel-next], [data-carousel-index]");
        const carousel = control?.closest("[data-product-carousel]");
        if (!control || !carousel) return;

        const activeIndex = updateProductCarousel(carousel);
        if (control.matches("[data-carousel-prev]")) moveProductCarousel(carousel, activeIndex - 1);
        else if (control.matches("[data-carousel-next]")) moveProductCarousel(carousel, activeIndex + 1);
        else moveProductCarousel(carousel, Number(control.dataset.carouselIndex));
    });
    document.addEventListener("scroll", event => {
        const track = event.target.closest?.("[data-carousel-track]");
        if (track) window.requestAnimationFrame(() => updateProductCarousel(track.closest("[data-product-carousel]")));
    }, true);
    document.addEventListener("submit", event => {
        if (window.location.pathname === "/shop" && event.target.matches("form.shop-search"))
            shopScrollPosition = window.scrollY;
    });
    bindButton();
    bindProductCarousels();

    const connectEnhancedNavigation = () => {
        if (window.Blazor?.addEventListener) {
            window.Blazor.addEventListener("enhancedload", resetPageScroll);
            return;
        }

        window.setTimeout(connectEnhancedNavigation, 25);
    };

    connectEnhancedNavigation();
})();
