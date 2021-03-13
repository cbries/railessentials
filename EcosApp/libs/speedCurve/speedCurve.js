(function ($) {

    $.fn.speedCurve = function (options) {

        var __isInstalled = false;

        this.refresh = function () {
            __redrawSpeedDots(false);
            __realignLines();
            return this;
        };

        this.getData = function () {
            return __getData();
        };

        var speedCfgs = {
            "dcc14": { speedsteps: 14, extraWidthForSteps: 2, noobyWidth: 4, noobyHeight: 4, deltaShow: 1 },
            "dcc28": { speedsteps: 28, extraWidthForSteps: 2, noobyWidth: 4, noobyHeight: 4, deltaShow: 2 },
            "dcc128": { speedsteps: 128, extraWidthForSteps: 2, noobyWidth: 4, noobyHeight: 4, deltaShow: 4 },
            "mm14": { speedsteps: 14, extraWidthForSteps: 2, noobyWidth: 4, noobyHeight: 4, deltaShow: 1 },
            "mm27": { speedsteps: 27, extraWidthForSteps: 0, noobyWidth: 4, noobyHeight: 4, deltaShow: 2 },
            "mm128": { speedsteps: 128, extraWidthForSteps: 2, noobyWidth: 4, noobyHeight: 4, deltaShow: 5 }
        };

        var settings = $.extend({
            speedMode: "dcc28",
            width: 750,
            height: 200,
            speedTimeMaxDefault: 5,
            speedStepMaxDefault: null,
            preloadData: [],
            onChanged: null
        }, options);

        var __speedMode = speedCfgs[settings.speedMode];
        var ctxContainer = this;
        var __mouseDown = false;

        if (settings.speedStepMaxDefault == null)
            settings.speedStepMaxDefault = parseInt(__speedMode.speedsteps / 2);

        __install();
        __initDiagram();

        __isInstalled = true;

        function __createControls() {
            ctxContainer.append(
                '<div class="speedCurveRoot"></div>' +
                '<div class="speedCurveControls">' +
                'Preloads:' +
                '<input type="button" id="cmdSpeedRestore" value="Restore">' +
                '<input type="button" id="cmdSpeedLinear" value="Linear">' +
                '<input type="button" id="cmdSpeedExponentialEsu" value="Exponential ESU">' +
                '<input type="button" id="cmdSpeedExponentialLenz" value="Exponential Lenz">' +
                '<input type="checkbox" class="chkLabelShow" name="chkLabelShow">' +
                '<label for="chkLabelShow" style="padding-bottom: 1px;"> Show Labels</label>' +
                '<div style="padding-top: 10px;">' +
                'Speedstep (max): <select id="cmbSpeedMax"></select>' +
                '&nbsp;&nbsp;Time (max): <select id="cmbSpeedTimeMax"></select>' +
                '</div>' +
                '</div>');
        }

        function __install() {
            window.addEventListener("resize", function () {
                __redrawSpeedDots(false);
                __realignLines();
            });

            __createControls();

            __chkLabelShow = ctxContainer.find('.chkLabelShow');
            __speedCurveRoot = ctxContainer.find('.speedCurveRoot');
            __speedCurveRoot.get(0).onmousedown = function () { __mouseDown = true; }
            __speedCurveRoot.get(0).onmouseup = function () { __mouseDown = false; }
        }

        function __initDiagram() {
            ctxContainer.css({
                width: settings.width + "px",
                height: settings.height + "px"
            });

            const targetGraph = ctxContainer.find('.speedCurveRoot');

            //
            // add horizontally / vertically line for better recognization
            //
            __lineSpeed = $('<div>') // horizontally
                .css({ width: settings.width + "px" })
                .addClass('speedCurveLineSpeed');
            __lineSpeed.appendTo(__speedCurveRoot);

            __lineTime = $('<div>') // vertically
                .css({ height: settings.height + "px" })
                .addClass('speedCurveLineTime');
            __lineTime.appendTo(__speedCurveRoot);

            //
            // add noobys
            //
            const xsteps = settings.width / __speedMode.speedsteps;
            let elementsToAppend = $();
            for (let i = 0; i < __speedMode.speedsteps; ++i) {
                const nooby = $("<div>")
                    .css({
                        width: __speedMode.noobyWidth + "px",
                        height: __speedMode.noobyHeight + "px"
                    })
                    .data("index", i)
                    .data("xsteps", xsteps)
                    .addClass("nooby_" + i)
                    .addClass("nooby");

                const noobySpeedLbl = $('<div>').addClass('noobySpeedLbl').html("");
                noobySpeedLbl.appendTo(nooby);

                let txtTime = "";
                const noobyTimeLbl = $('<div>').addClass('noobyTimeLbl').html(txtTime);
                noobyTimeLbl.appendTo(nooby);

                elementsToAppend = elementsToAppend.add(nooby);
            }
            targetGraph.append(elementsToAppend);
            __redrawSpeedDots();

            const selectSpeedCtrl = ctxContainer.find('select#cmbSpeedMax');
            for (let i = 0; i < __speedMode.speedsteps; ++i) {
                const opt = $('<option>', { value: i }).html(i);
                opt.appendTo(selectSpeedCtrl);
            }
            selectSpeedCtrl.change(function (ev) {
                const v = $(this).val();
                settings.speedStepMaxDefault = parseInt(v);
                __highlightMaxSpeed(v);
                __realignLines();
            });
            selectSpeedCtrl.val(settings.speedStepMaxDefault);

            const selectTimeCtrl = ctxContainer.find('select#cmbSpeedTimeMax');
            for (let i = 0; i < __speedMode.speedsteps; ++i) {
                if (i === settings.speedTimeMaxDefault) {
                    const opt = $('<option>', { value: i, selected: '' }).html(i + "s");
                    opt.appendTo(selectTimeCtrl);
                } else {
                    const opt = $('<option>', { value: i }).html(i + "s");
                    opt.appendTo(selectTimeCtrl);
                }
            }
            selectTimeCtrl.change(function () {
                settings.speedTimeMaxDefault = $(this).val();
                __realignLines();
            });

            __speedCurveRoot.mouseleave(function () {
                __mouseDown = false;
            });

            __speedCurveRoot.mousemove(function (ev) {
                ev.preventDefault();
                const coord = __getMouseCoordRelativeTo(this, ev);
                __recentMouseMoveCoord = coord;
                if (__mouseDown === false) return;
                __handleMouseClickMove(coord);
            });

            __speedCurveRoot.click(function () {
                __handleMouseClickMove(__recentMouseMoveCoord);
            });

            const cmdSpeedRestore = ctxContainer.find('#cmdSpeedRestore');
            const cmdSpeedLinear = ctxContainer.find("#cmdSpeedLinear");
            const cmdSpeedExponentialEsu = ctxContainer.find("#cmdSpeedExponentialEsu");
            const cmdSpeedExponentialLenz = ctxContainer.find("#cmdSpeedExponentialLenz")

            cmdSpeedRestore.click(function () { __preloadData(settings.preloadData); });
            cmdSpeedLinear.click(function () { __preloadLinear(); });
            cmdSpeedExponentialEsu.click(function () { __preloadExponential(0); });
            cmdSpeedExponentialLenz.click(function () { __preloadExponential(1); });
            __chkLabelShow.click(function () {
                __realignLines();
            });

            __realignLines();

            if (typeof settings.preloadData === "string") {
                if (settings.preloadData === "esu")
                    __preloadExponential(0);
                else if (settings.preloadData === "lenz")
                    __preloadExponential(1);
            } else {
                if (settings.preloadData.length == 0) {
                    __preloadExponential(0);
                } else {
                    __preloadData(settings.preloadData);
                }
            }
        }

        function __realignLines() {
            const speedCurveRoot = ctxContainer.find('.speedCurveRoot');

            const maxSpeed = settings.speedStepMaxDefault;
            const maxTime = settings.speedTimeMaxDefault;

            const lineSpeed = ctxContainer.find('.speedCurveLineSpeed');
            const lineTime = ctxContainer.find('.speedCurveLineTime');

            // align speed line
            const speedNooby = ctxContainer.find('.nooby_' + maxSpeed);
            let currentY = parseInt(speedNooby.css("top").replace("px", ""));
            if (currentY < 0) currentY = currentY * -1.0;
            const lineY = settings.height - currentY;
            lineSpeed.css({ "top": lineY });

            // align time stuff
            let currentX = parseInt(speedNooby.css("left").replace("px", ""));
            currentX += __speedMode.noobyWidth / 2;
            currentX += maxSpeed * __speedMode.noobyWidth;
            lineTime.css({
                "left": currentX
            });

            const rect = speedCurveRoot.get(0).getBoundingClientRect();
            const bottom = rect.top + rect.height;

            //
            // hide time labels which are higher as the selected max time value
            //
            const elements = ctxContainer.find('.nooby');

            let istep = 1;
            if (elements.length > 32)
                istep = 10;

            const chkLabelShow = ctxContainer.find('.chkLabelShow');
            const isShowChecked = chkLabelShow.is(":checked");
            const stepTime = maxTime / maxSpeed / istep;
            let counterTime = 0;
            for (let i = 0; i < elements.length; i++) {
                const el = $(elements[i]);
                const elTimeLbl = el.find('.noobyTimeLbl');
                const elSpeedLbl = el.find('.noobySpeedLbl');

                elTimeLbl.hide();
                elSpeedLbl.hide();

                let t = counterTime.toString();
                t = t.substr(0, 3);
                elTimeLbl.html(t + "s");

                const parentRect = el.parent().get(0).getBoundingClientRect();
                const elRect = el.get(0).getBoundingClientRect();
                const y = parentRect.bottom - elRect.bottom;
                const factor = settings.height / __speedMode.speedsteps;
                const speed = (1 / factor) * y;
                elSpeedLbl.html(speed);

                elTimeLbl.removeClass("timeHighlight");
                elSpeedLbl.removeClass("speedHighlight");

                if (speed > maxSpeed) {
                    elTimeLbl.hide();
                    elSpeedLbl.hide();
                    el.data("speed", maxSpeed);
                    el.data("timeStep", maxSpeed);
                } else if (i === maxSpeed) {
                    elTimeLbl.addClass("timeHighlight");
                    elSpeedLbl.addClass("speedHighlight");
                    elSpeedLbl.html(maxSpeed);
                    el.data("speed", speed);
                    el.data("timeStep", counterTime);
                    elTimeLbl.show();
                    elSpeedLbl.show();
                } else {
                    if (i % istep === 0) {
                        if (isShowChecked == true) elTimeLbl.show();
                        else elTimeLbl.hide();
                        if (isShowChecked == true) elSpeedLbl.show();
                        else elSpeedLbl.hide();
                    }
                    el.data("speed", speed);
                    el.data("timeStep", counterTime);
                }

                for (let j = 0; j < istep; ++j)
                    counterTime += stepTime;
            }

            if (settings.onChanged != null && __isInstalled == true)
                settings.onChanged({ data: __getData() });
        }

        function __highlightMaxSpeed(idx) {
            const elements = ctxContainer.find('.nooby');
            const iidx = parseInt(idx);
            for (let i = 0; i < elements.length; ++i) {
                const el = $(elements[i]);
                el.removeClass("noobyMax");
                if (i === iidx)
                    el.addClass("noobyMax");
            }
        }

        function __preloadData(data = []) {
            if (typeof data === "string") {
                if (settings.preloadData === "esu")
                    __preloadExponential(0);
                else if (settings.preloadData === "lenz")
                    __preloadExponential(1);
            } else {
                if (typeof data !== "undefined" && data != null && data.length > 0)
                    __preloadExponential(-1, data);
                else
                    __preloadExponential(-1, settings.preloadData);
            }
        }

        function __preloadLinear() {
            const elements = ctxContainer.find('.nooby');
            const speedsteps = __speedMode.speedsteps;
            const ystep = settings.height / speedsteps;
            for (let i = 0; i < speedsteps; ++i) {
                const el = $(elements[i]);
                const y = (i * ystep);
                __setY(el, y);
            }

            __realignLines();
        }

        function __preloadExponential(esuLenz, data = []) {
            const elements = ctxContainer.find('.nooby');
            const speedsteps = __speedMode.speedsteps;
            const deltaStep = __speedMode.deltaShow;

            const preloadEsu28 = [
                0,
                2.000, 5.763, 9.573, 13.480, 17.536, 21.794, 26.306, 31.127, 36.309,
                41.909, 47.980, 54.578, 61.759, 69.578, 78.091, 87.353, 97.423, 108.356,
                120.208, 133.037, 146.900, 161.854, 177.956, 195.264, 213.836, 233.728,
                255.000
            ];

            const preloadLenz28 = [
                0,
                2.000, 4.591, 7.786, 11.562, 15.905, 20.809, 26.266, 32.272, 38.822,
                45.914, 53.543, 61.706, 70.402, 79.628, 89.382, 99.662, 110.465, 121.792,
                133.639, 146.005, 158.889, 172.290, 186.206, 200.636, 215.579, 231.034,
                247.000
            ];

            let values = data;
            if (esuLenz === 0) values = preloadEsu28;
            else if (esuLenz === 1) values = preloadLenz28;

            const maxI = values.length;

            for (let i = 0, elIdx = 0; i < maxI - 1; ++i, elIdx += deltaStep) {
                const v0 = values[i];
                const v1 = values[i + 1];

                let maxSub = elIdx + deltaStep;
                if (i === maxI - 2) {
                    maxSub = speedsteps;
                }

                const el_delta = maxSub - elIdx;
                const v_delta = v1 - v0;
                const factor = v_delta / el_delta;

                for (let j = elIdx, multi = 0; j < maxSub; ++j, ++multi) {
                    const v_ = v0 + (factor * multi);
                    const ell = $(elements[j]);
                    const y = (settings.height / 255) * v_;

                    __setY(ell, y);
                }
            }

            __realignLines();
        }

        function __getOffset() {
            return {
                top: settings.height,
                left: 0
            }
        }

        function __setY(el, y) {
            if (y < 0) return;
            if (y > settings.height) return;
            const yoffset = __speedMode.noobyHeight;
            el.css({ top: -y - yoffset + "px" });
        }

        function __redrawSpeedDots(initMode = true) {
            const offset = __getOffset();
            const elements = ctxContainer.find('.nooby');
            for (let i = 0; i < elements.length; ++i) {
                const el = $(elements[i]);
                const xsteps = el.data("xsteps");
                const left = offset.left + parseInt(i * xsteps) - (i * __speedMode.noobyWidth);

                if (initMode === true) {
                    el.css({
                        top: offset.top + "px",
                        left: left + "px"
                    });
                } else {
                    el.css({
                        left: left + "px"
                    });
                }
            }
        }

        function __handleMouseClickMove(coord) {
            const steps = settings.width / __speedMode.speedsteps;
            const idx = __getIndexByWidth(coord.x, settings.width, steps);
            if (idx < 0) return;
            const noobyEl = ctxContainer.find('.nooby_' + idx);
            if (typeof noobyEl === "undefined" || noobyEl == null || noobyEl.length === 0)
                return;
            const y = coord.y;
            __setY(noobyEl, y);
            __realignLines();
        }

        function __getMouseCoordRelativeTo(rootElement, ev) {
            const evX = ev.pageX;
            const evY = ev.pageY;
            const rect = rootElement.getBoundingClientRect();
            const parentOffsetTop = rect.top;
            const parentOffsetLeft = rect.left;
            const parentHeight = rect.height;
            const coord = {
                x: evX - parentOffsetLeft,
                y: (parentHeight - (evY - parentOffsetTop)),
                topPage: evX - parentOffsetLeft,
                leftPage: parentHeight - (evY - parentOffsetTop)
            };
            return coord;
        }

        function __getIndexByWidth(x, width, step) {
            if (x < 0) return -1;
            if (x > width) return -1;
            let idx = 0;
            for (let i = 0; i < width; i += step) {
                const lowX = i;
                const highX = lowX + step;
                if (x >= lowX && x < highX)
                    return idx;
                ++idx;
            }
            return -1;
        }

        function __getData() {
            const selectSpeedCtrl = ctxContainer.find('select#cmbSpeedMax');
            const selectTimeCtrl = ctxContainer.find('select#cmbSpeedTimeMax');

            const maxSpeed = parseInt(selectSpeedCtrl.val());
            const maxTime = parseInt(selectTimeCtrl.val());

            const elements = ctxContainer.find('.nooby');
            let elAr = [];
            for (let i = 0; i < elements.length; ++i) {
                const el = $(elements[i]);
                let speed = el.data("speed");
                let timeStep = el.data("timeStep");

                if (speed < 0) speed = 0;
                if (timeStep < 0) timeStep = 0;

                const itm = {
                    speed: speed,
                    timeStep: timeStep
                };
                elAr.push(itm);
            }

            let data = {
                maxSpeed: maxSpeed,
                maxTime: maxTime,
                steps: elAr
            };

            return data;
        }

        return this;

    } // $.fn.speedCurve

}(jQuery));
