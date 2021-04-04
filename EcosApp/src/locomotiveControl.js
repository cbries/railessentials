/**
 * Supported events:
 * speedChanged : { sender: this, data : { oid: INT, speedstep: INT } }
 * functionChanged : { sender: this, data : { oid : INT, fncIdx: INT }}
 * speedChanged : { sender: this, data : { oid : INT, speedstep: STRING }}
 * directionChanged : { sender: this, data : { oid : INT }}
 */

const constLocomotiveControlBaseName = "locomotiveCtrl_";
const constLocomotiveMinSpeed = 0;

class LocomotiveControl {
    constructor() {
        console.log("**** construct LocomotiveControl");
        this.__tplMain = "./components/dialogLocomotiveControl/locomotiveControl.html";
        this.__baseDlgId = constLocomotiveControlBaseName;
        this.__dlgId = null;
        this.__dlgElement = null;
        this.__dlgIsClosed = true;

        this.__windowGeometry = null;
        this.__locomotiveRecord = null;

        this.__speedTrigger = true;

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

    updateSpeedState(dataLocomotive) {
        if (this.__dlgIsClosed) return;
        const sliderEl = this.__dlgElement.find(".locomotiveSpeedometer");
        if (!sliderEl) return;
        this.__speedTrigger = false;
        sliderEl.slider("value", dataLocomotive.speedstep);
        this.__speedTrigger = true;
        $('.locomotiveSpeedstepValue').html(dataLocomotive.speedstep);
    }

    updateDirectionState(dataLocomotive) {
        if (this.__dlgIsClosed) return;
        const self = this;
        const oid = dataLocomotive.objectId;
        const ctrl = $('#locomotiveCtrl_' + oid);
        const cmdBackward = ctrl.find('.cmdLocBackward');
        const cmdForward = ctrl.find('.cmdLocForward');
        cmdBackward.css({ "color": "black" });
        cmdForward.css({ "color": "black" });
        if (dataLocomotive.direction === 1) // backward
            cmdBackward.css({ "color": "green" });
        else if (dataLocomotive.direction === 0) // forward
            cmdForward.css({ "color": "green" });
    }

    updateFunctionStates(dataLocomotive) {
        const oid = dataLocomotive.objectId;
        const nrOfFunctions = dataLocomotive.nrOfFunctions;
        const funcset = dataLocomotive.funcset;
        const funcdesc = dataLocomotive.funcdesc;

        let usedFnc = 0;
        for (let i = 0; i < funcdesc.length; ++i) {
            const fncState = funcset[i];
            const fncDesc = funcdesc[i];
            if (fncDesc.type === 0)
                continue;

            const className = 'fncCommand_' + oid + "_" + i;
            const inputEl = $('button.' + className);

            this.__changeButtonStyle(inputEl, fncState === "1");

            ++usedFnc;
            if (usedFnc >= nrOfFunctions)
                break;
        }
    }

    __changeButtonStyle(buttonEl, state) {
        if (state) {
            buttonEl.css({
                "font-weight": "bold",
                "color": "green"
            });
        } else {
            buttonEl.css({
                "font-weight": "bold",
                "color": "red"
            });
        }
    }

    open(locomotiveRecord) {
        const self = this;
        this.__locomotiveRecord = locomotiveRecord;

        const name = this.__locomotiveRecord.name;
        const speedstep = this.__locomotiveRecord.speedstep;
        const oid = this.__locomotiveRecord.oid;
        const noOfFunctions = this.__locomotiveRecord.noOfFunctions;
        const funcset = this.__locomotiveRecord.funcset;
        const funcdesc = this.__locomotiveRecord.funcdesc;
        
        this.__dlgId = this.__baseDlgId + oid;
        this.__dlgSelector = '#' + this.__dlgId;

        this.__windowGeometry = new WindowGeometryStorage(this.__dlgId);

        const recentDialog = $(this.__dlgSelector);
        if (recentDialog.length > 0)
            return false;

        this.__dlgIsClosed = false;

        const tplLocomotiveControl = $('div[id=tplLocomotiveControl]');
        let ctrl = tplLocomotiveControl.clone();
        this.__dlgElement = ctrl;
        ctrl.attr("id", this.__dlgId);
        ctrl.attr("title", name);
        ctrl.addClass("locomotiveControlMain");
        
        ctrl.dialog({
            height: 350,
            resizable: false,
            autoOpen: false,
            resizeStop: function (event, ui) {
                self.__windowGeometry.save(ui.position, ui.size);
            },
            dragStop: function (event, ui) {
                self.__windowGeometry.save(ui.position,
                    {
                        width: event.target.clientWidth,
                        height: event.target.clientHeight
                    });
            },
            close: function (event, ui) {
                ctrl.remove();

                self.__trigger('dialogClosed',
                    {
                        instance: this
                    });
            }
        });

        const speedometer = ctrl.find(".locomotiveSpeedometer");

        speedometer.slider({
            orientation: "vertical",
            range: "min",
            min: constLocomotiveMinSpeed,
            max: self.__locomotiveRecord.speedstepMax,
            value: speedstep,
            slide: function(event, ui) {
                $('.locomotiveSpeedstepValue').html(ui.value);
            },
            stop: function (event, ui) {
                if (!self.__speedTrigger) return;
                const oid = self.__locomotiveRecord.oid;
                self.__trigger('speedChanged',
                    {
                        oid: oid,
                        speedstep: ui.value
                    });
            }
        });

        $('.locomotiveSpeedstepValue').html(speedstep);

        const fakeId = "img_" + this.__dlgId;
        const locImage = ctrl.find(".locomotiveImage");
        locImage.attr("id", fakeId);
        locImage.attr("src", "./images/noimage32x32.png");
        locImage.attr("alt", name);
        loadLocomotiveImageIntoHtml(fakeId, name);

        const handleFunctionClick = (function (ev) {
            const fnc = $(ev.currentTarget).data("function");
            const oid = self.__locomotiveRecord.oid;
            self.__trigger('functionChanged',
                {
                    oid: oid,
                    fncIdx: fnc,
                    timestamp: Date.now()
                });
        });

        const handleSpeedClick = (function (ev) {
            const speedstep = $(ev.currentTarget).data("speedstep");
            const oid = self.__locomotiveRecord.oid;
            self.__trigger('speedChanged',
                {
                    oid: oid,
                    speedstep: speedstep,
                    timestamp: Date.now()
                });
        });

        const globalCss = {
            "font-size": "14px",
            "font-weight": "bold",
            width: "50px",
            padding: "2px",
            margin: "2px"
        };

        const allBtns = ctrl.find('button').not("button[data-function]");
        allBtns.each(function () {
            $(this).button();
            if ($(this).data("speedstep")) {
                $(this).css(globalCss);
                $(this).click(handleSpeedClick);
            } else if ($(this).data("direction")) {
                $(this).css(globalCss);
                $(this).css({
                    width: "25px",
                    display: "inline-block",
                    margin: "-3px",
                    "margin-bottom": "2px"
                });
                $(this).click(function () {
                    const oid = self.__locomotiveRecord.oid;
                    self.__trigger('directionChanged',
                        {
                            oid: oid,
                            force: $(this).data("direction")
                        });
                });
            }
        });

        const fncAr = getArrayOfFunctions2(oid, noOfFunctions, funcset, funcdesc);

        const target = ctrl.find('td.locomotiveCommandField');
        let usedBtn = 0;
        for (let i = 0; i < funcset.length; ++i) {
            const fncState = fncAr[usedBtn];
            const fncDesc = funcdesc[i];
            if (fncDesc.type === 0)
                continue;

            const className = 'fncCommand_' + oid + "_" + i;

            let btn = $('<button>').text("F" + i);
            btn.addClass("locomotiveCommand");
            btn.addClass(className);
            btn.data("function", i);
            let fncDescHuman = ListOfFunctionDescription[fncDesc.type];
            if (typeof fncDescHuman === "undefined" || fncDescHuman == null)
                fncDescHuman = "Function";
            btn.data("tipso-title", fncDescHuman);
            btn.button();
            btn.css(globalCss);
            this.__changeButtonStyle(btn, fncState.state);
            btn.click(handleFunctionClick);
            btn.appendTo(target);

            btn.tipso({
                size: 'tiny',
                speed: 100,
                delay: 250,
                useTitle: true,
                width: "auto",
                background: '#333333',
                titleBackground: '#333333'
            });

            ++usedBtn;
            if (usedBtn >= noOfFunctions)
                break;  
        }

        ctrl.dialog("open");

        return true;
    }
}