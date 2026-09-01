(() => {
    const buttonId = "scroll-to-top";

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
            const isContactInquiry = window.location.pathname === "/contact"
                && new URLSearchParams(window.location.search).has("message");

            if (isContactInquiry) {
                document.getElementById("contact-content")?.scrollIntoView({ block: "start", behavior: "instant" });
            } else if (!window.location.hash) {
                window.scrollTo({ top: 0, left: 0, behavior: "instant" });
            }

            bindButton();
            updateButton();
        });
    };

    window.addEventListener("scroll", updateButton, { passive: true });
    window.addEventListener("pageshow", updateButton);
    Blazor.addEventListener("enhancedload", resetPageScroll);
    bindButton();
})();
