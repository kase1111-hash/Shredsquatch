mergeInto(LibraryManager.library, {

    GetBrowserInfo: function() {
        var userAgent = navigator.userAgent;
        var bufferSize = lengthBytesUTF8(userAgent) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(userAgent, buffer, bufferSize);
        return buffer;
    },

    CheckWebGL2Support: function() {
        try {
            var canvas = document.createElement('canvas');
            return !!(canvas.getContext('webgl2'));
        } catch (e) {
            return false;
        }
    },

    IsMobileDevice: function() {
        return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    },

    SetFullscreen: function(fullscreen) {
        var canvas = document.querySelector('canvas');
        if (fullscreen) {
            if (canvas.requestFullscreen) {
                canvas.requestFullscreen();
            } else if (canvas.webkitRequestFullscreen) {
                canvas.webkitRequestFullscreen();
            } else if (canvas.mozRequestFullScreen) {
                canvas.mozRequestFullScreen();
            }
        } else {
            if (document.exitFullscreen) {
                document.exitFullscreen();
            } else if (document.webkitExitFullscreen) {
                document.webkitExitFullscreen();
            } else if (document.mozCancelFullScreen) {
                document.mozCancelFullScreen();
            }
        }
    },

    GetPerformanceMemory: function() {
        if (performance && performance.memory) {
            return performance.memory.usedJSHeapSize / (1024 * 1024);
        }
        return -1;
    },

    ShowBrowserAlert: function(messagePtr) {
        var message = UTF8ToString(messagePtr);
        alert(message);
    },

    CopyToClipboard: function(textPtr) {
        var text = UTF8ToString(textPtr);
        navigator.clipboard.writeText(text).catch(function(err) {
            console.error('Failed to copy: ', err);
        });
    },

    OpenExternalLink: function(urlPtr) {
        var url = UTF8ToString(urlPtr);
        window.open(url, '_blank');
    }

});
