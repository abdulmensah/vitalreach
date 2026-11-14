const slideshows = new WeakMap();
const interval = 8000;

export function start(element, component) {
    dispose(element);
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)');
    const state = { paused: motion.matches, visible: true, timer: null, disposed: false };
    const clear = () => { window.clearTimeout(state.timer); state.timer = null; };
    const schedule = () => {
        clear();
        if (state.disposed || state.paused || document.hidden || !state.visible) return;
        state.timer = window.setTimeout(async () => {
            try { await component.invokeMethodAsync('AdvanceAutomatically'); }
            catch { state.paused = true; }
            schedule();
        }, interval);
    };
    const pause = () => {
        if (state.paused) return;
        state.paused = true;
        clear();
        component.invokeMethodAsync('PauseFromBrowser').catch(() => {});
    };
    const onMotion = () => { if (motion.matches) pause(); };
    // Keyboard entry stops rotation. Only an explicit Play action resumes it.
    const onFocus = event => {
        if (!element.contains(event.relatedTarget) && !event.target.closest('.mission-rotation-toggle')) pause();
    };
    motion.addEventListener('change', onMotion);
    element.addEventListener('focusin', onFocus);
    document.addEventListener('visibilitychange', schedule);
    const observer = new IntersectionObserver(entries => {
        state.visible = entries[0].isIntersecting;
        schedule();
    }, { threshold: 0.15 });
    observer.observe(element);
    state.setPaused = paused => { state.paused = paused; schedule(); };
    state.dispose = () => {
        state.disposed = true;
        clear();
        observer.disconnect();
        motion.removeEventListener('change', onMotion);
        element.removeEventListener('focusin', onFocus);
        document.removeEventListener('visibilitychange', schedule);
    };
    slideshows.set(element, state);
    schedule();
    return state.paused;
}

export function setPaused(element, paused) { slideshows.get(element)?.setPaused(paused); }
export function dispose(element) {
    slideshows.get(element)?.dispose();
    slideshows.delete(element);
}
