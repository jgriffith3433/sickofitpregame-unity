mergeInto(LibraryManager.library, {
    OpenURLInCurrentWindow: function(urlPtr) {
        var url = UTF8ToString(urlPtr);
        window.location.href = url;
    }
});
