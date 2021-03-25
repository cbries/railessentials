(function ($) {

    $.fn.videoStream = function(options) {

        var __isInstalled = false;
        var __ctx = this;
        var __img = null;
        var __imgLbl = null;
        var __imgCaption = null;
        var __timeoutId = -1;
        var __failureCounter = 0;

        const defaultFps = 10;

        var settings = $.extend({
            width: 320,
            height: 240,
            fps: defaultFps,
            locale: "de-DE",
            caption: "",
            stopOnFailure: false,
            stopAfterFailuresFps: 2 * defaultFps,
            moved: null
        }, options);

        if (__isInstalled === true)
            return this;

        __install();
        
        return this;

        function __install() {
            __ctx.resizable({
                ghost: false,
                stop: function (event, ui) {
                    if (typeof settings.moved === "undefined") return;
                    if (settings.moved == null) return;
                    settings.moved({
                        url: __img.get(0).src,
                        x: ui.position.left,
                        y: ui.position.top,
                        w: ui.size.width,
                        h: ui.size.height
                    });
                }
            });
            __ctx.draggable({
                stop: function (event, ui) {
                    if (typeof settings.moved === "undefined") return;
                    if (settings.moved == null) return;
                    settings.moved({
                        url: __img.get(0).src,
                        x: ui.position.left,
                        y: ui.position.top,
                        w: parseInt(__ctx.css("width").replace("px", "")),
                        h: parseInt(__ctx.css("height").replace("px", ""))
                    });
                }
            });

            __ctx.css({
                width: settings.width + "px",
                height: settings.height + "px"
            });

            __img = $('<img>', {
                width: settings.width,
                height: settings.height,
                src: settings.url
            });
            __img.appendTo(__ctx);

            __imgLbl = $('<div>').addClass("lblDateTime");
            const sdt = new Date().toLocaleString("de-DE");
            __imgLbl.html(sdt);
            __imgLbl.appendTo(__ctx);

            if(typeof settings.caption !== "undefined" && settings.caption != null && settings.caption.length > 0) {
                __imgCaption = $('<div>').addClass("lblCaption");
                __imgCaption.html(settings.caption);
                __imgCaption.appendTo(__ctx);
            }

            __tryLoadOfStream();

            __isInstalled = true;
        }

        function __showErrorImage() {
            __img.get(0).src =
                    "data:image/png;base64,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";
        }

        function __updateImage() {
            let newImg = new Image;
            newImg.onload = function () {
                if (typeof __img === "undefined") return;
                if (__img == null) return;
                __img.get(0).src = this.src;
                const parentRect = __img.parent().get(0).getBoundingClientRect();
                __img.css({
                    width: parentRect.width + "px",
                    height: parentRect.height + "px"
                });
                const sdt = new Date().toLocaleString("de-DE");
                __imgLbl.html(sdt);
                __imgLbl.show();
                __imgCaption.show()();
                __tryLoadOfStream();
            }
            newImg.onerror = function () {
                if(settings.stopOnFailure === true) {
                    __failureCounter++;
                    if (__failureCounter >= settings.stopAfterFailuresFps) {
                        console.log("Refresh of Stream stopped: " + __failureCounter + " >= " + settings.stopAfterFailuresFps);
                        clearTimeout(__timeoutId);
                        __timeoutId = -1;
                        __showErrorImage();
                        return;
                    }
                }
                if (typeof __img === "undefined") return;
                if (__img == null) return;
                __showErrorImage();
                __imgLbl.hide();
                __imgCaption.hide();
                __tryLoadOfStream();
            }
            newImg.src = settings.url + "?t=" + Math.random();
        }

        function __tryLoadOfStream() {
            clearTimeout(__timeoutId);
            __timeoutId = setTimeout(function () {
                __updateImage();
            }, 1000 / settings.fps);
        }

    } // $.fn.videoStream

}(jQuery));