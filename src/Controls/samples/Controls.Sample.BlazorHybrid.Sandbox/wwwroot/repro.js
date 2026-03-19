window.safeAreaRepro = (() => {
    let dotNetRef = null;
    let timeoutHandle = null;
    let resizeHandler = null;
    let orientationHandler = null;

    function readEnv(propertyName, envName) {
        const probe = document.createElement('div');
        probe.style.position = 'absolute';
        probe.style.visibility = 'hidden';
        probe.style.pointerEvents = 'none';
        probe.style[propertyName] = `env(${envName}, 0px)`;
        document.body.appendChild(probe);

        const value = getComputedStyle(probe)[propertyName];
        probe.remove();
        return value;
    }

    function capture(trigger) {
        const safeAreaDiv = document.querySelector('.status-bar-safe-area');
        const sidebar = document.querySelector('.repro-sidebar');

        return {
            Trigger: trigger,
            OrientationType: window.screen.orientation?.type ?? 'unknown',
            OrientationAngle: Number(window.screen.orientation?.angle ?? window.orientation ?? 0),
            SafeAreaInsetTop: readEnv('paddingTop', 'safe-area-inset-top'),
            SafeAreaInsetLeft: readEnv('paddingLeft', 'safe-area-inset-left'),
            SafeAreaInsetRight: readEnv('paddingRight', 'safe-area-inset-right'),
            SafeAreaInsetBottom: readEnv('paddingBottom', 'safe-area-inset-bottom'),
            SafeAreaDivDisplay: safeAreaDiv ? getComputedStyle(safeAreaDiv).display : 'missing',
            SafeAreaDivHeight: safeAreaDiv ? getComputedStyle(safeAreaDiv).height : 'missing',
            SidebarPaddingLeft: sidebar ? getComputedStyle(sidebar).paddingLeft : 'missing',
            WindowInnerWidth: window.innerWidth,
            WindowInnerHeight: window.innerHeight
        };
    }

    async function publish(trigger) {
        if (!dotNetRef) {
            return;
        }

        try {
            await dotNetRef.invokeMethodAsync('UpdateMetrics', capture(trigger));
        } catch (error) {
            console.error('safeAreaRepro publish failed', error);
        }
    }

    function schedule(trigger) {
        clearTimeout(timeoutHandle);
        timeoutHandle = window.setTimeout(() => publish(trigger), 200);
    }

    return {
        start(ref) {
            dotNetRef = ref;

            resizeHandler = () => schedule('resize');
            orientationHandler = () => schedule('orientation-change');

            window.addEventListener('resize', resizeHandler);
            window.screen.orientation?.addEventListener?.('change', orientationHandler);

            schedule('initial');
        },

        captureNow() {
            schedule('manual-refresh');
        },

        stop() {
            clearTimeout(timeoutHandle);

            if (resizeHandler) {
                window.removeEventListener('resize', resizeHandler);
            }

            if (orientationHandler) {
                window.screen.orientation?.removeEventListener?.('change', orientationHandler);
            }

            resizeHandler = null;
            orientationHandler = null;
            dotNetRef = null;
        }
    };
})();
