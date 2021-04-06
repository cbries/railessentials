const ErrorHandlerLevel = {
    Info: 1,
    Error: 2
}

class ErrorHandler {
    constructor() {
        console.log("**** construct ErrorHandler");
        this.__overlay = $('div.overlay');;
        this.__overlayText = $('div.overlay div.overlayText');
        this.setLevel(ErrorHandler.Info);
    }

    setLevel(level) {
        if (typeof level === "undefined")
            level = ErrorHandlerLevel.Info;
        let colorClass = "infoOverlay";
        switch (level) {
            case ErrorHandlerLevel.Info:
                colorClass = "infoOverlay";
                break;
            case ErrorHandlerLevel.Error:
                colorClass = "errorOverlay";
                break;
        }
        this.__overlayText.removeClass("infoOverlay");
        this.__overlayText.removeClass("errorOverlay");
        this.__overlayText.addClass(colorClass);
    }

    setText(msg, forceMessage = false) {
        const o = this.__overlay;
        const ot = this.__overlayText;

        if (o.is(":visible")) {
            if (forceMessage)
                ot.html(msg);
        } else {
            o.show();
            ot.html(msg);
        }
    }

    hide() {
        const o = this.__overlay;
        const ot = this.__overlayText;

        o.hide();
        ot.html("");
    }
}