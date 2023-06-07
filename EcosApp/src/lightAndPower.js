class LightAndPower {
    constructor() {
        console.log("**** construct LightAndPower");

        this.__rgbStripesIpAddr = 'ws://192.168.178.66:81';
        this.__powerPlugIpAddr = 'ws://192.168.178.62:81';

        this.__installed = false;

        this.__ctrlIdMorning = "rgbColorMorning";
        this.__ctrlIdNoon = "rgbColorNoon";
        this.__ctrlIdAfternoon = "rgbColorAfternoon";
        this.__ctrlIdNight = "rgbColorNight";

        this.__dialogName = "dialogLightAndPower";
        this.__storageNameGeometry = this.__dialogName + "_geometry";
        this.__fieldNamePowerStates = this.__dialogName + "_powerStates";

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

        self.__c1DelayTimeout = 0;
        self.__c2DelayTimeout = 0;
        self.__c3DelayTimeout = 0;
        self.__c4DelayTimeout = 0;

        const ctrlIds = [
            this.__ctrlIdMorning,
            this.__ctrlIdNoon,
            this.__ctrlIdAfternoon,
            this.__ctrlIdNight
        ];

        let i;
        const iMax = ctrlIds.length;
        for (i = 0; i < iMax; ++i) {
            const c = $('#' + ctrlIds[i]);
            c.colorpicker({
                stop: function (ev, data) {
                    c.css({
                        "background-color": data.css,
                        "color": data.css
                    });
                }
            });
            var bgCss = c.val();
            c.css({
                "background-color": "#" + bgCss,
                "color": "#" + bgCss,
                "text-align": "left"
            });
            c.w2field('text');
        }

        $('#dialogLightAndPower input[type=eu-time]').w2field('time', { format: 'h24' });

        const geometry = this.__windowGeometry.recent();

        $("#" + this.__dialogName).dialog({
            height: 430,//geometry.height,
            width: 450, //geometry.width,
            left: geometry.left,
            top: geometry.top,
            resizable: false,
            closeOnEscape: false,
            autoOpen: options.autoOpen,
            resize: function (event, ui) {
                // TBD
            },
            resizeStop: function (event, ui) {
                //self.__windowGeometry.save(ui.position, ui.size);
                console.log(ui);
            },
            dragStop: function (event, ui) {
                self.__windowGeometry.save(ui.position,
                    {
                        //width: event.target.clientWidth,
                        //height: event.target.clientHeight
                    });
            },
            close: function (event, ui) {
                // TBD
            }
        });

        this.__applyRecentPowerSwitchStates(true);

        this.__installed = true;
    }

    __storePowerSwitchStates() {
        let states = {
            "in1": $('#powerChkIn1').is(":checked"),
            "in2": $('#powerChkIn2').is(":checked"),
            "in3": $('#powerChkIn3').is(":checked"),
            "in4": $('#powerChkIn4').is(":checked")
        };

        $.localStorage.setItem(this.__fieldNamePowerStates, JSON.stringify(states));
    }

    __applyRecentPowerSwitchStates(initCtrls = false) {
        const self = this;

        const c1 = $('#powerChkIn1');
        const c1delay = $('#powerChkIn1DelaySecs');

        const c2 = $('#powerChkIn2');
        const c2delay = $('#powerChkIn2DelaySecs');

        const c3 = $('#powerChkIn3');
        const c3delay = $('#powerChkIn3DelaySecs');

        const c4 = $('#powerChkIn4');
        const c4delay = $('#powerChkIn4DelaySecs');

        if (initCtrls === true) {

            c1.w2field('checkbox');
            c1.change(function () {
                self.__storePowerSwitchStates();
                self.__updatePowerDelayState(c1, c1delay);

                try {
                    const spinnerIcon = $('#powerChkIn1DelaySpinner');
                    const delayValue = parseInt(c1delay.val());
                    if (c1.is(":checked")) {
                        if (self.__c1DelayTimeout === 0) {

                            spinnerIcon.show();
                            self.__c1DelayTimeout = setTimeout(function () {
                                self.__sendPowerSwitch();
                                clearTimeout(self.__c1DelayTimeout);
                                self.__c1DelayTimeout = 0;
                                spinnerIcon.hide();
                            }, delayValue * 1000);
                        }
                    } else {
                        spinnerIcon.hide();
                        self.__sendPowerSwitch();
                        if (self.__c1DelayTimeout !== 0) {
                            clearTimeout(self.__c1DelayTimeout);
                            self.__c1DelayTimeout = 0;
                        }
                    }
                }
                catch (e) { }
            });

            $('#powerChkIn1DelaySecs').w2field('int', {
                autoFormat: true, min: 0, max: 30, silent: false
            });

            c2.w2field('checkbox');
            c2.change(function () {
                self.__storePowerSwitchStates();
                self.__updatePowerDelayState(c2, c2delay);

                try {
                    const spinnerIcon = $('#powerChkIn2DelaySpinner');
                    const delayValue = parseInt(c2delay.val());
                    if (c2.is(":checked")) {
                        if (self.__c2DelayTimeout === 0) {

                            spinnerIcon.show();
                            self.__c2DelayTimeout = setTimeout(function () {
                                self.__sendPowerSwitch();
                                clearTimeout(self.__c2DelayTimeout);
                                self.__c2DelayTimeout = 0;
                                spinnerIcon.hide();
                            }, delayValue * 1000);
                        }
                    } else {
                        spinnerIcon.hide();
                        self.__sendPowerSwitch();
                        if (self.__c2DelayTimeout !== 0) {
                            clearTimeout(self.__c2DelayTimeout);
                            self.__c2DelayTimeout = 0;
                        }
                    }
                }
                catch (e) { }
            });
            $('#powerChkIn2DelaySecs').w2field('int', {
                autoFormat: true, min: 0, max: 30, silent: false
            });

            c3.w2field('checkbox');
            c3.change(function () {
                self.__storePowerSwitchStates();
                self.__updatePowerDelayState(c3, c3delay);

                try {
                    const spinnerIcon = $('#powerChkIn3DelaySpinner');
                    const delayValue = parseInt(c3delay.val());
                    if (c3.is(":checked")) {
                        if (self.__c3DelayTimeout === 0) {

                            spinnerIcon.show();
                            self.__c3DelayTimeout = setTimeout(function () {
                                self.__sendPowerSwitch();
                                clearTimeout(self.__c3DelayTimeout);
                                self.__c3DelayTimeout = 0;
                                spinnerIcon.hide();
                            }, delayValue * 1000);
                        }
                    } else {
                        spinnerIcon.hide();
                        self.__sendPowerSwitch();
                        if (self.__c3DelayTimeout !== 0) {
                            clearTimeout(self.__c3DelayTimeout);
                            self.__c3DelayTimeout = 0;
                        }
                    }
                }
                catch (e) { }
            });
            $('#powerChkIn3DelaySecs').w2field('int', {
                autoFormat: true, min: 0, max: 30
            });

            c4.w2field('checkbox');
            c4.change(function () {
                self.__storePowerSwitchStates();
                self.__updatePowerDelayState(c4, c4delay);

                try {
                    const spinnerIcon = $('#powerChkIn4DelaySpinner');
                    const delayValue = parseInt(c4delay.val());
                    if (c4.is(":checked")) {
                        if (self.__c4DelayTimeout === 0) {

                            spinnerIcon.show();
                            self.__c4DelayTimeout = setTimeout(function () {
                                self.__sendPowerSwitch();
                                clearTimeout(self.__c4DelayTimeout);
                                self.__c4DelayTimeout = 0;
                                spinnerIcon.hide();
                            }, delayValue * 1000);
                        }
                    } else {
                        spinnerIcon.hide();
                        self.__sendPowerSwitch();
                        if (self.__c4DelayTimeout !== 0) {
                            clearTimeout(self.__c4DelayTimeout);
                            self.__c4DelayTimeout = 0;
                        }
                    }
                }
                catch (e) { }
            });
            $('#powerChkIn4DelaySecs').w2field('int', {
                autoFormat: true, min: 0, max: 30
            });

            $('#rgbColorMorningRadio').w2field('radio');
            $('#rgbColorNoonRadio').w2field('radio');
            $('#rgbColorAfternoonRadio').w2field('radio');
            $('#rgbColorNightRadio').w2field('radio');

            $('#rgbColorMorningRadio').change(function () {
                self.__sendRgbStripe();
            });
            $('#rgbColorNoonRadio').change(function () {
                self.__sendRgbStripe();
            });
            $('#rgbColorAfternoonRadio').change(function () {
                self.__sendRgbStripe();
            });
            $('#rgbColorNightRadio').change(function () {
                self.__sendRgbStripe();
            });
        }

        const nativeStates = $.localStorage.getItem(this.__fieldNamePowerStates);
        let states = JSON.parse(nativeStates);
        if (typeof states === "undefined" || states == null) {
            states = {
                "in1": false,
                "in2": false,
                "in3": false,
                "in4": false
            };
        } else {
            c1.prop('checked', states.in1);
            c2.prop('checked', states.in2);
            c3.prop('checked', states.in3);
            c4.prop('checked', states.in3);
        }

        self.__updatePowerDelayState(c1, c1delay);
        self.__updatePowerDelayState(c2, c2delay);
        self.__updatePowerDelayState(c3, c3delay);
        self.__updatePowerDelayState(c4, c4delay);
    }

    __updatePowerDelayState(chkCtrl, radioCtrl) {
        const state = chkCtrl.is(':checked');
        radioCtrl.attr("disabled", state);
    }

    __sendRgbStripe() {
        const self = this;
        const dataToSend = self.__getRgbStripesObject();

        self.__trigger('relayCommand',
            {
                'mode': 'websocket',
                'target': this.__rgbStripesIpAddr,
                'contentType': 'application/json',
                'data': dataToSend
            });
    }

    __getRgbStripesObject() {
        let rgbW = { r: 255, g: 255, b: 255, w: 1023 };
        if ($('#rgbColorMorningRadio').is(":checked") === true) {
            rgbW = hexToRgbA($('#rgbColorMorning').val());
            rgbW.w = 200;
        } else if ($('#rgbColorNoonRadio').is(":checked") === true) {
            rgbW = hexToRgbA($('#rgbColorNoon').val());
            rgbW.w = 1023;
        } else if ($('#rgbColorAfternoonRadio').is(":checked") === true) {
            rgbW = hexToRgbA($('#rgbColorAfternoon').val());
            rgbW.w = 500;
        } else if ($('#rgbColorNightRadio').is(":checked") === true) {
            rgbW = hexToRgbA($('#rgbColorNight').val());
            rgbW.w = 100;
        }

        return rgbW;    
    }

    __sendPowerSwitch() {
        const self = this;
        const dataToSend = {
            "in1": $('#powerChkIn1').is(":checked"),
            "in2": $('#powerChkIn2').is(":checked"),
            "in3": $('#powerChkIn3').is(":checked"),
            "in4": $('#powerChkIn4').is(":checked"),

            //"in1delay": parseInt($('#powerChkIn1DelaySecs').val()),
            //"in2delay": parseInt($('#powerChkIn2DelaySecs').val()),
            //"in3delay": parseInt($('#powerChkIn3DelaySecs').val()),
            //"in4delay": parseInt($('#powerChkIn4DelaySecs').val())
        };

        self.__trigger('relayCommand',
            {
                'mode': 'websocket',
                'target': this.__powerPlugIpAddr,
                'contentType': 'application/json',
                'data': dataToSend
            });
    }
}