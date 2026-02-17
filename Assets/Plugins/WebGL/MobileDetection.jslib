mergeInto(LibraryManager.library, {
    IsMobileBrowser: function() {
        // Enhanced mobile detection that includes iPads with iPadOS 13+
        var isMobile = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent) ||
                       (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1) || // iPad with iPadOS 13+
                       ('ontouchstart' in window); // Any touch-capable device
        
        return isMobile;
    }
});
