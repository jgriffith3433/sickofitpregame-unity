/**
 * Unity WebGL JavaScript Plugin for Umami Analytics
 * This file bridges Unity C# code with the website's JavaScript analytics functions.
 * 
 * IMPORTANT: Make sure these functions are defined on your website:
 * window.trackGameStart(), window.trackLevel1Start(), etc.
 * 
 * If the functions don't exist, this will log a warning instead of crashing.
 */

mergeInto(LibraryManager.library, {

    // ============================================================
    // Game Events
    // ============================================================
    
    trackGameStart: function() {
        if (typeof window.trackGameStart === 'function') {
            window.trackGameStart();
        } else {
            console.warn('[Unity Analytics] window.trackGameStart is not defined');
        }
    },

    trackGameOver: function() {
        if (typeof window.trackGameOver === 'function') {
            window.trackGameOver();
        } else {
            console.warn('[Unity Analytics] window.trackGameOver is not defined');
        }
    },

    // ============================================================
    // Level Start Events
    // ============================================================
    
    trackLevel1Start: function() {
        if (typeof window.trackLevel1Start === 'function') {
            window.trackLevel1Start();
        } else {
            console.warn('[Unity Analytics] window.trackLevel1Start is not defined');
        }
    },

    trackLevel2Start: function() {
        if (typeof window.trackLevel2Start === 'function') {
            window.trackLevel2Start();
        } else {
            console.warn('[Unity Analytics] window.trackLevel2Start is not defined');
        }
    },

    trackLevel3Start: function() {
        if (typeof window.trackLevel3Start === 'function') {
            window.trackLevel3Start();
        } else {
            console.warn('[Unity Analytics] window.trackLevel3Start is not defined');
        }
    },

    trackLevel4Start: function() {
        if (typeof window.trackLevel4Start === 'function') {
            window.trackLevel4Start();
        } else {
            console.warn('[Unity Analytics] window.trackLevel4Start is not defined');
        }
    },

    // ============================================================
    // Level Complete Events
    // ============================================================
    
    trackLevel1Complete: function() {
        if (typeof window.trackLevel1Complete === 'function') {
            window.trackLevel1Complete();
        } else {
            console.warn('[Unity Analytics] window.trackLevel1Complete is not defined');
        }
    },

    trackLevel2Complete: function() {
        if (typeof window.trackLevel2Complete === 'function') {
            window.trackLevel2Complete();
        } else {
            console.warn('[Unity Analytics] window.trackLevel2Complete is not defined');
        }
    },

    trackLevel3Complete: function() {
        if (typeof window.trackLevel3Complete === 'function') {
            window.trackLevel3Complete();
        } else {
            console.warn('[Unity Analytics] window.trackLevel3Complete is not defined');
        }
    },

    trackLevel4Complete: function() {
        if (typeof window.trackLevel4Complete === 'function') {
            window.trackLevel4Complete();
        } else {
            console.warn('[Unity Analytics] window.trackLevel4Complete is not defined');
        }
    },

    // ============================================================
    // Video Start Events
    // ============================================================
    
    trackVideo1Start: function() {
        if (typeof window.trackVideo1Start === 'function') {
            window.trackVideo1Start();
        } else {
            console.warn('[Unity Analytics] window.trackVideo1Start is not defined');
        }
    },

    trackVideo2Start: function() {
        if (typeof window.trackVideo2Start === 'function') {
            window.trackVideo2Start();
        } else {
            console.warn('[Unity Analytics] window.trackVideo2Start is not defined');
        }
    },

    trackVideo3Start: function() {
        if (typeof window.trackVideo3Start === 'function') {
            window.trackVideo3Start();
        } else {
            console.warn('[Unity Analytics] window.trackVideo3Start is not defined');
        }
    },

    trackVideo4Start: function() {
        if (typeof window.trackVideo4Start === 'function') {
            window.trackVideo4Start();
        } else {
            console.warn('[Unity Analytics] window.trackVideo4Start is not defined');
        }
    },

    // ============================================================
    // Video Complete Events
    // ============================================================
    
    trackVideo1Complete: function() {
        if (typeof window.trackVideo1Complete === 'function') {
            window.trackVideo1Complete();
        } else {
            console.warn('[Unity Analytics] window.trackVideo1Complete is not defined');
        }
    },

    trackVideo2Complete: function() {
        if (typeof window.trackVideo2Complete === 'function') {
            window.trackVideo2Complete();
        } else {
            console.warn('[Unity Analytics] window.trackVideo2Complete is not defined');
        }
    },

    trackVideo3Complete: function() {
        if (typeof window.trackVideo3Complete === 'function') {
            window.trackVideo3Complete();
        } else {
            console.warn('[Unity Analytics] window.trackVideo3Complete is not defined');
        }
    },

    trackVideo4Complete: function() {
        if (typeof window.trackVideo4Complete === 'function') {
            window.trackVideo4Complete();
        } else {
            console.warn('[Unity Analytics] window.trackVideo4Complete is not defined');
        }
    },

    // ============================================================
    // Video Skip Events
    // ============================================================
    
    trackVideo1Skip: function() {
        if (typeof window.trackVideo1Skip === 'function') {
            window.trackVideo1Skip();
        } else {
            console.warn('[Unity Analytics] window.trackVideo1Skip is not defined');
        }
    },

    trackVideo2Skip: function() {
        if (typeof window.trackVideo2Skip === 'function') {
            window.trackVideo2Skip();
        } else {
            console.warn('[Unity Analytics] window.trackVideo2Skip is not defined');
        }
    },

    trackVideo3Skip: function() {
        if (typeof window.trackVideo3Skip === 'function') {
            window.trackVideo3Skip();
        } else {
            console.warn('[Unity Analytics] window.trackVideo3Skip is not defined');
        }
    },

    trackVideo4Skip: function() {
        if (typeof window.trackVideo4Skip === 'function') {
            window.trackVideo4Skip();
        } else {
            console.warn('[Unity Analytics] window.trackVideo4Skip is not defined');
        }
    },

    // ============================================================
    // Player Death Event (with string parameter)
    // ============================================================
    
    trackPlayerDeath: function(causePtr) {
        var cause = UTF8ToString(causePtr);
        
        if (typeof window.trackPlayerDeath === 'function') {
            window.trackPlayerDeath(cause);
        } else {
            console.warn('[Unity Analytics] window.trackPlayerDeath is not defined');
        }
    }

});
