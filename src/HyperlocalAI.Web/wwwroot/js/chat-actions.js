window.localMindChat = {
    copyText: async function (text) {
        await navigator.clipboard.writeText(text);
    },
    shareText: async function (title, text) {
        const t = title || 'LocalMind AI';
        if (navigator.share) {
            try {
                await navigator.share({ title: t, text: text });
                return true;
            } catch (e) {
                if (e && e.name === 'AbortError') return false;
            }
        }
        await navigator.clipboard.writeText(text);
        return true;
    }
};
