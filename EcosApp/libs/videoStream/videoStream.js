(function ($) {

    $.fn.videoStream = function(options) {

        var __isInstalled = false;
        var __ctx = this;
        var __img = null;
        var __imgLbl = null;
        var __imgCaption = null;

        var settings = $.extend({
            width: 320,
            height: 240,
            fps: 10,
            locale: "de-DE",
            caption: ""
        }, options);

        if (__isInstalled === true)
            return this;

        __install();
        
        return this;

        function __install() {
            __ctx.resizable({
                ghost: false
            });
            __ctx.draggable();

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

            setInterval(function () {
                __updateImage();
            }, 1000 / settings.fps);

            __isInstalled = true;
        }
        
        function __updateImage() {
            if (typeof __img === "undefined") return;
            if (__img == null) return;
            __img.get(0).src = settings.url + "?t=" + Math.random();
            const parentRect = __img.parent().get(0).getBoundingClientRect();
            __img.css({
                width: parentRect.width + "px",
                height: parentRect.height + "px"
            });
            const sdt = new Date().toLocaleString("de-DE");
            __imgLbl.html(sdt);
        }

    } // $.fn.videoStream

}(jQuery));