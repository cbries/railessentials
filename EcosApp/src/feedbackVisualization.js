class FeedbackVisualization {
    constructor() {
        console.log("**** construct FeedbackVisualization");

        this.__installed = false;

        this.__dialogName = "dialogFeedbackVisualization";
        this.__storageNameGeometry = this.__dialogName + "_geometry";

        this.__windowGeometry = new WindowGeometryStorage(this.__dialogName);

        this.__initEventHandling();
    }

    __initEventHandling() {
        this.__events = new Events();
    }

    on(eventName, callback) {
        this.__events.on(eventName, callback);
    }

    __trigger(eventName, dataObject) {
        this.__events.triggerHandler(eventName, {
            sender: this,
            data: dataObject
        });
    }

    show() {
        const el = $('#' + this.__dialogName);
        this.__windowGeometry.showWithGeometry(el, false);

        if (this.__installed) {
            el.dialog("open");
        } else {
            this.install({ autoOpen: true });
            el.dialog("open");
        }

        if(typeof this.__recentSensorData !== "undefined" && this.__recentSensorData != null)
            this.updateFeedbackSensors(this.__recentSensorData);
    }

    isShown() {
        try {
            return $('#' + this.__dialogName).dialog('isOpen');
        } catch (error) {
            // ignore
        }
        return false;
    }

    install(options = {}) {
        const self = this;
        const state = this.isShown();
        if (state) return;

        if (this.__installed) return;

        if (typeof options === "undefined" || typeof options.autoOpen === "undefined") {
            options.autoOpen = false;
        }

        const geometry = this.__windowGeometry.recent();

        $("#" + this.__dialogName).dialog({
            left: geometry.left,
            top: geometry.top,
            resizable: false,
            closeOnEscape: false,
            autoOpen: options.autoOpen,
            resizeStop: function (event, ui) {
                self.__windowGeometry.save(ui.position, ui.size);
            },
            dragStop: function (event, ui) {
                self.__windowGeometry.save(ui.position, { width: 0, height: 0 });
            },
            close: function (event, ui) {
                // TBD
            }
        });

        this.__installed = true;
    }

    updateFeedbackSensors(feedbackSensors) {
        const self = this;
        self.__recentSensorData = feedbackSensors;
        if (self.isShown() === false) return;

        let startIndex = 0;
        let i;
        const iMax = feedbackSensors.length;
        for (i = 0; i < iMax; ++i) {
            const sensor = feedbackSensors[i];
            self.__addPort(i, startIndex, sensor.ports);
            startIndex += sensor.ports;
        }

        let fbIdx;
        const fbIdxMax = feedbackSensors.length;
        for (fbIdx = 0; fbIdx < fbIdxMax; ++fbIdx) {
            const fb = feedbackSensors[fbIdx];
            if (typeof fb === "undefined") continue;
            if (fb == null) continue;

            for (let j = 15; j >= 0; --j) {
                const accessorId = "div#feedbackPort_" + fbIdx + "_" + j;
                const fbCtrl = $(accessorId);
                const mask = 1 << j;
                if ((fb.stateOriginal & mask) !== 0) {
                    // on / red
                    fbCtrl.removeClass('feedbackStateGreen').addClass('feedbackStateRed');
                } else {
                    // off / green
                    fbCtrl.removeClass('feedbackStateRed').addClass('feedbackStateGreen');
                }
            }

            startIndex += fb.ports;
        }

        const targetCtrl = $('div.feedbackOuterWrapper');
        targetCtrl.css({
            height: (feedbackSensors.length * 45) + "px"
        });

        // feedbackVisualization.js:77 {width: 471, height: 323}
        const dlgCtrl = $("#" + this.__dialogName);
        dlgCtrl.dialog("option", "width", 490);
        dlgCtrl.dialog("option", "height", feedbackSensors.length * 50 + 30);
    }

    __addPort(portIdx, startIndex, len) {
        const targetCtrl = $('div.feedbackOuterWrapper');
        const newFeedbackPortId = "feedbackPort_" + portIdx;
        const c = $('div#' + newFeedbackPortId);
        if (c !== "undefined" && c != null && c.length > 0)
            c.remove();
        const feedbackPort = $('<div>')
            .attr("id", newFeedbackPortId)
            .css({ "width": "470px" })
            .addClass('feedbackPort');

        const portNo = $('<div>')
            .css({
                display: "block",
                float: "left",
                "padding-top": "4px",
                "padding-right": "5px"
            })
            .html("Port " + (portIdx + 1));

        feedbackPort.append(portNo);

        for (let i = 0; i < len; ++i) {
            const newId = "feedbackPort_" + portIdx + "_" + (len - i - 1);
            const newStateCtrl = $('<div>')
                .attr('id', newId)
                .attr('data-tipso-title', "Port(" + (portIdx + 1) + ")  Index(" + (len - i - 1) + ")  EcosAddr(" + (startIndex + (len - i - 1)) + ")")
                .addClass('feedbackStateRed');
            feedbackPort.append(newStateCtrl);

            newStateCtrl.tipso({
                size: 'tiny',
                speed: 100,
                delay: 250,
                useTitle: true,
                width: "auto",
                background: '#333333',
                titleBackground: '#333333'
            });
        }

        targetCtrl.append(feedbackPort);

        let left0 = "";
        let top0 = "";
        if (len === 16) {
            left0 = "440px";
            top0 = "-20px";
        } else if (len === 8) {
            left0 = "233px";
            top0 = "-20px";
        }

        // addr labels
        const lblRight = $('<div>').css({
            position: "relative",
            "font-size": "0.75em",
            "left": left0,
            "top": top0,
            "width": "20px",
            "height": "20px",
            "overflow": "visible",
            "text-align": "right"
        }).html(startIndex);

        const lblLeft = $('<div>').css({
            position: "relative",
            "font-size": "0.75em",
            "left": "50px",
            "width": "20px",
            "height": "20px",
            "overflow": "visible"
        }).html(startIndex + len - 1);

        feedbackPort.append(lblLeft);
        feedbackPort.append(lblRight);
    }
}