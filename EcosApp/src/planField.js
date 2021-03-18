"use strict";

class Planfield {

    /**
     * 
     * @param {any} options : {
     * isEditMode [bool] }
     */
    constructor(options = {}) {
        console.log("**** construct Planfield");
        this.planfieldElement = null;
        this.isEditMode = false;
        if (typeof options.isEditMode !== "undefined")
            this.isEditMode = options.isEditMode;
        this.currentSelection = null;
        this.currentSelectionMoved = false;
        this.currentSelectionMovedItem = null;
        this.options = options;
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

    setEditMode(state) {
        const self = this;
        this.isEditMode = state;
        this.clearCtrlSelection();
        if (state) {
            this.planfieldElement.addClass("planFieldBackground");
        } else {
            this.planfieldElement.removeClass("planFieldBackground");
        }

        let j;
        const jMax = window.textfieldElementInstances.length;
        for (j = 0; j < jMax; ++j) {
            window.textfieldElementInstances[j].setEditMode(state);
        }

        const ctrls = $('div.ctrlItem');
        let i;
        const iMax = ctrls.length;
        for (i = 0; i < iMax; ++i) {
            const c = ctrls[i];
            if (c == null) continue;
            if (state) {
                this.initDragFor($(c));
            } else {
                this.removeDragFor($(c));
            }
        }
    }

    __updatePlanfieldGeometryPixel(size = {}) {
        if (typeof size.w === "undefined")
            size.w = $(window).width();
        if (typeof size.h === "undefined")
            size.h = $(window).height();
        this.__updatePlanfieldGeometry(
            parseInt(size.w / constItemWidth),
            parseInt(size.h / constItemHeight));
    }

    __updatePlanfieldGeometry(itemX, itemY) {
        const planField = $('div#planField');
        planField.css("width", "calc(" + itemX + " * " + constItemWidth + "px + 1px)");
        planField.css("height", "calc(" + itemY + " * " + constItemHeight + "px + 1px)");
        return planField;
    }

    isCtrlSelected(jqueryElement) {
        if (typeof jqueryElement === "undefined") return false;
        if (jqueryElement == null) return false;
        if (!(jqueryElement instanceof jQuery))
            jqueryElement = $(jqueryElement);
        const newCtrlImg = jqueryElement.find("img");
        if (typeof newCtrlImg === "undefined") return false;
        if (newCtrlImg == null) return false;
        return newCtrlImg.hasClass("selected");
    }

    __getCtrl(item, ctrls) {
        const x = item.x;
        const y = item.y;
        const xpx = x * constItemWidth + (constItemWidth / 2);
        const ypx = y * constItemHeight + (constItemHeight / 2);
        const ctrl = getCtrlOfPosition(xpx, ypx, ctrls);
        return $(ctrl);
    }

    clearRouteUserHighlight() {
        const self = this;
        const ctrls = $('div.ctrlItem');
        if (typeof ctrls === "undefined") return;
        if (ctrls == null) return;
        if (ctrls.length === 0) return;
        for (let i = 0; i < ctrls.length; ++i) {
            const itm = ctrls[i];
            if (itm == null) continue;
            $(itm).removeClass('highlightRoute');
        }
    }

    activateRouteUserHighlight(elements) {
        const self = this;
        const ctrls = $('div.ctrlItem');

        let i;
        let iMax = elements.tracks.length;
        for (i = 0; i < iMax; ++i) {
            const jqCtrl = self.__getCtrl(elements.tracks[i], ctrls);
            jqCtrl.addClass('highlightRoute');
        }

        iMax = elements.sensors.length;
        for (i = 0; i < iMax; ++i) {
            const jqCtrl = self.__getCtrl(elements.sensors[i], ctrls);
            jqCtrl.addClass('highlightRoute');
        }

        iMax = elements.signals.length;
        for (i = 0; i < iMax; ++i) {
            const jqCtrl = self.__getCtrl(elements.signals[i], ctrls);
            jqCtrl.addClass('highlightRoute');
        }

        iMax = elements.switches.length;
        for (i = 0; i < iMax; ++i) {
            const jqCtrl = self.__getCtrl(elements.switches[i], ctrls);
            jqCtrl.addClass('highlightRoute');
        }
    }

    activateRouteVisualization(route) {
        const allTrackElements = [];
        Array.prototype.push.apply(allTrackElements, route.tracks);
        Array.prototype.push.apply(allTrackElements, route.sensors);
        Array.prototype.push.apply(allTrackElements, route.signals);
        Array.prototype.push.apply(allTrackElements, route.switches);

        const self = this;
        const ctrls = $('div.ctrlItem');

        let i;
        const iMax = allTrackElements.length;
        for (i = 0; i < iMax; ++i) {
            const el = allTrackElements[i];
            if (el == null) continue;
            const jqCtrl = self.__getCtrl(el, ctrls);
            const img = jqCtrl.find("img");
            if (typeof img === "undefined" || img == null) continue;
            if (img.length === 0) continue;
            let imgsrc = img.attr("src");
            if (imgsrc.includes("-route.png")) continue;
            imgsrc = imgsrc.replace(".png", "-route.png");
            img.attr("src", imgsrc);
        }
    }

    deativateRouteVisualization(route) {
        const allTrackElements = [];
        Array.prototype.push.apply(allTrackElements, route.tracks);
        Array.prototype.push.apply(allTrackElements, route.sensors);
        Array.prototype.push.apply(allTrackElements, route.signals);
        Array.prototype.push.apply(allTrackElements, route.switches);

        const self = this;
        const ctrls = $('div.ctrlItem');

        let i;
        const iMax = allTrackElements.length;
        for (i = 0; i < iMax; ++i) {
            const el = allTrackElements[i];
            if (el == null) continue;
            const jqCtrl = self.__getCtrl(el, ctrls);
            const img = jqCtrl.find("img");
            if (typeof img === "undefined" || img == null) continue;
            if (img.length === 0) continue;
            let imgsrc = img.attr("src");
            if (!imgsrc.includes("-route.png")) continue;
            imgsrc = imgsrc.replace("-route.png", ".png");
            img.attr("src", imgsrc);
        }
    }

    clearCtrlSelection() {
        const ctrls = $('div.ctrlItem');
        if (typeof ctrls === "undefined") return;
        if (ctrls == null) return;
        if (ctrls.length === 0) return;
        let i;
        const iMax = ctrls.length;
        for (i = 0; i < iMax; ++i) {
            const itm = ctrls[i];
            if (itm == null) continue;
            if (!this.isCtrlSelected(itm)) continue;
            this.unselectCtrl($(itm));
        }
    }

    unselectCtrl(jqueryElement) {
        if (typeof jqueryElement === "undefined") return;
        if (jqueryElement == null) return;

        let newCtrlImg = jqueryElement.find("img");
        if (typeof newCtrlImg === "undefined")
            newCtrlImg = null;

        if (newCtrlImg == null) return;

        newCtrlImg.removeClass("selected");
        this.editMenubar.hideEditMenu();
        this.currentSelection = null;
    }

    selectCtrl(jqueryElement, options = {}) {
        if (typeof jqueryElement === "undefined") return;
        if (jqueryElement == null) return;

        this.clearCtrlSelection();

        const newCtrlImg = jqueryElement.find("img");
        if (typeof newCtrlImg !== "undefined" && newCtrlImg != null)
            newCtrlImg.addClass("selected");

        this.currentSelection = jqueryElement;
        if (typeof options.startEditMode === "undefined")
            options.startEditMode = true;
        if (options.startEditMode) {
            this.editMenubar.setCurrentSelection(jqueryElement);
            this.editMenubar.updateEditMenuPositionAndEvents(jqueryElement);
            this.editMenubar.showEditMenu();
        }
    }

    generateUniqueItemIdentifier(themeId) {
        const idarray = $('div.ctrlItem[id]')
            .map(function () { return this.id; })
            .get();
        let newId = "Item";
        if (themeId) {
            if (themeId >= 10 && themeId <= 25)
                newId = "TK";
            else if (themeId >= 50 && themeId <= 75)
                newId = "SW";
            else if (themeId >= 100 && themeId <= 125)
                newId = "SE";
            else if (themeId >= 150 && themeId <= 175)
                newId = "BK";
            else if (themeId >= 200 && themeId <= 225)
                newId = "FB";
        }
        const idarrayLength = idarray.length;
        for (let i = 0; i < idarrayLength; ++i) {
            const localId = newId + "_" + i;
            if (!idarray.includes(localId))
                return localId;
        }
        return newId + "_" + idarrayLength;
    }

    install() {
        const self = this;

        self.__firstRun = true;

        window.ctrlClickSpinnerCounter = 0;

        $(window).resize(function () {
            self.__updatePlanfieldGeometryPixel();
        });

        this.editMenubar = new EditMenuBar(this.options);

        this.__updatePlanfieldGeometryPixel();
        this.planfieldElement = $('div#planField');
        this.planfieldElement.click(function (ev) {
            if (self.currentSelectionMoved) {
                ev.preventDefault();
                ev.stopPropagation();
                self.currentSelectionMoved = false;
                return;
            }

            const ctrl = getCtrlOfEvent($(this), ev, $('div.ctrlItem'));
            if (ctrl != null) {
                const jsonData = ctrl.data(constDataThemeItemObject);
                if (typeof jsonData !== "undefined" && jsonData !== null && jsonData.clickable) {

                    //
                    // show click spinner for some seconds
                    //
                    if (!self.isEditMode) {
                        const spinner = $('div.lds-facebookOriginal');
                        const spinnerClone = spinner.clone();
                        spinnerClone.removeClass("lds-facebookOriginal");
                        spinnerClone.addClass("lds-facebook");
                        const _clickId = "lfd-facebookClickId" + window.ctrlClickSpinnerCounter;
                        spinnerClone.addClass(_clickId);
                        ++window.ctrlClickSpinnerCounter;
                        spinnerClone.css({
                            top: ev.clientY - (constItemHeight / 2),
                            left: ev.clientX - (constItemWidth / 2)
                        });
                        spinnerClone.insertAfter(spinner);
                        if (spinnerClone.is(":visible")) {
                            // ignore
                        } else {
                            spinnerClone.show();
                            const idToRemove = _clickId;
                            var ctrlToRemove = $('div.' + idToRemove);
                            setTimeout(function () {
                                ctrlToRemove.hide();
                                ctrlToRemove.remove();
                            }, 500);
                        }

                        self.__trigger('clicked',
                            {
                                ctrlId: ctrl.attr('id'),
                                coord: getElementCoord(self.planfieldElement, ev)
                            });
                    }
                }
            } else {
                if (self.isEditMode) {
                    const tb = window.toolbox;
                    if (tb.recentItem) {
                        self.createControlWithEv(tb.recentItem, ev);
                    }
                }
            }
        });

        this.planfieldElement.get(0).ondragover = ev => {
            if (!self.isEditMode) return;
            ev.preventDefault();
        }
        this.planfieldElement.get(0).ondrop = ev => {
            if (!this.isEditMode) return;
            self.createControlOnDrop(ev, { editMode: true });
        }

        $(document).keyup(function (e) {
            if (e.keyCode === 27) // esc
                self.clearCtrlSelection();
        });
    }

    __initBlockDrop(jqueryEl) {
        if (typeof jqueryEl === "undefined") return;
        if (jqueryEl === null) return;

        const self = this;
        const childs = jqueryEl.find("img");
        let i;
        const iMax = childs.length;
        for (i = 0; i < iMax; ++i) {
            $(childs[i]).droppable({
                accept: "div.locomotiveInfo",
            //    drop: function (event, ui) {
            //    }
            });
        }

        jqueryEl.get(0).ondragover = function (ev) {
            ev.preventDefault();
        };
        jqueryEl.get(0).ondrop = function (ev) {
            try {
                ev.preventDefault();
                const data = ev.dataTransfer.getData("text");
                const jsonData = JSON.parse(data);
                const cmd = {
                    mode: 'assignToBlock',
                    oid: jsonData.recid,
                    coord: getThemeJsonData(jqueryEl).coord
                };

                self.__trigger("assignToBlock", cmd);
                const srcGrid = w2ui[jsonData.sourceGrid];
                srcGrid.selectNone();
                srcGrid.unselect(jsonData.recid);
            }
            catch (ev) {
                // ignore
            }
        };
    }

    /**
     * 
     * @param {any} feedbacks -- data of fbevents.json
     */
    updateFeedbacks(feedbacks) {
        const self = this;
        const noOfFeedbacks = feedbacks.data.length;
        const blockCtrls = $('div.ctrlItemBlock');
        self.__removeAllDisabledFlags(blockCtrls);
        if (noOfFeedbacks === 0) return;
        if (typeof blockCtrls === "undefined") return;
        if (blockCtrls == null) return;
        if (blockCtrls.length === 0) return;

        let i = 0; 
        for (; i < noOfFeedbacks; ++i) {
            const fbData = feedbacks.data[i];
            if (typeof fbData === "undefined") continue;
            if (fbData == null) continue;

            try {
                const enabled = fbData.Settings.BlockEnabled;
                if (typeof enabled === "undefined" || enabled == null || enabled === true) continue;

                const blockCtrl = self.__getBlockCtrlOf(fbData, blockCtrls);
                if (blockCtrl == null) continue;
                blockCtrl.addClass("blockDisabledState");
            }
            catch(err) {
                // ignore
            }
        }
    }

    __getBlockCtrlOf(fbData, blockCtrls) {
        const blockId = fbData.BlockId;
        for (let i = 0; i < blockCtrls.length; ++i) {
            const block = $(blockCtrls[i]);
            const data = block.data(constDataThemeItemObject);
            const blockId = data.identifier;
            if (fbData.BlockId.startsWith(blockId))
                return block;
        }
        return null;
    }

    __removeAllDisabledFlags(blockCtrls) {
        if (typeof blockCtrls === "undefined" || blockCtrls == null || blockCtrls.length === 0) return;
        let i = 0; 
        const iMax = blockCtrls.length;
        for (; i < iMax; ++i) {
            const fb = blockCtrls[i];
            if (typeof fb === "undefined" || fb == null) continue;
            $(fb).removeClass("blockDisabledState");
        }
    }

    /**
     * 
     * @param {any} feedbacks -- ECoS data
     */
    updateEcosFeedbacks(feedbacks) {
        const self = this;
        const noOfFeedbacks = feedbacks.length;
        const fbCtrls = $('div.ctrlItemFeedback');
        const changes = [];
        let portOffset = 0;

        const recentFeedback = self.__recentFeedbacksData;
        if (typeof recentFeedback === "undefined" || recentFeedback == null)
            self.__recentFeedbacksData = feedbacks;

        for (let fbIdx = 0; fbIdx < noOfFeedbacks; ++fbIdx) {
            const fb = feedbacks[fbIdx];
            if (typeof fb === "undefined") continue;
            if (fb == null) continue;

            if (fb === self.__recentFeedbacksData[fbIdx] && self.__firstRun === false)
                continue;

            for (let jj = 15; jj >= 0; --jj) {
                const addr = portOffset + jj + 1;
                const fbItem = getFbByEcosAddr(fbCtrls, addr);
                if (typeof fbItem === "undefined") continue;
                if (fbItem === null) continue;
                
                let newImgEnding = "";
                const mask = 1 << jj;
                if ((fb.stateOriginal & mask) !== 0) {
                    newImgEnding = "-on";
                } else {
                    newImgEnding = "-off";
                }

                const img = fbItem.planItem.find("img");
                if (!img) continue;
                const imgsrc = img.attr("src");
                const isRouteSelected = imgsrc.includes("-route");
                const dirpath = getDirpathOf(imgsrc);
                let newimgfname = fbItem.themeData.basename;
                newimgfname = newimgfname.replace("-on", "");
                newimgfname = newimgfname.replace("-off", "");
                if (isRouteSelected === true)
                    newImgEnding += "-route";
                const newImgSrc = dirpath + newimgfname + newImgEnding + ".png";
                if (imgsrc !== newImgSrc) {
                    changes.push({
                        "ctrl": img,
                        "src": newImgSrc + "?r=" + Math.random()
                    });
                }
            }

            portOffset += fb.ports;
        }

        for (let j = 0; j < changes.length; ++j) {
            changes[j].ctrl.attr("src", changes[j].src);
        }

        self.__firstRun = false;
    }

    /**
     * 
     * @param {any} accessories -- ECoS data
     */
    updateEcosAccessories(accessories) {
        const self = this;
        let noOfAccessories = accessories.length;
        let accCtrls = $('div.ctrlItemAccessory');

        for (var i = 0; i < noOfAccessories; ++i) {
            const acc = accessories[i];
            if (typeof acc === "undefined") continue;
            if (acc == null) continue;
            if (!acc.hasChanged) continue;

            //console.log("Acc: " + acc.name1 + " "
            //    + "State(" + acc.state + ") "
            //    + "Addrext(" + acc.addrext + ") "
            //    + "oid(" + acc.objectId + ")");

            const accCtrl = getAccByEcosAddr(accCtrls, acc.addr);
            if (accCtrl == null) continue;
            let noOfAddresses = 0;
            if (accCtrl.ecosAddresses[0].ecosAddrValid === true) noOfAddresses++;
            if (accCtrl.ecosAddresses[1].ecosAddrValid === true) noOfAddresses++;
            if (noOfAddresses === 0)
                continue;

            // general stuff
            const img = accCtrl.planItem.find("img");
            if (!img) continue;
            const imgsrc = img.attr("src");
            const dirpath = getDirpathOf(imgsrc);
            const themeId = accCtrl.themeData.editor.themeId;
            let newimgfname = accCtrl.themeData.basename;

            // individual stuff
            switch (noOfAddresses) {
                case 1:
                    {
                        let inverse = false;
                        if (accCtrl.ecosAddresses[0].ecosAddrValid === true) {
                            inverse = accCtrl.ecosAddresses[0].inverse;
                        } else if (accCtrl.ecosAddresses[1].ecosAddrValid === true) {
                            inverse = accCtrl.ecosAddresses[1].inverse;
                        }

                        var targetState = acc.state === 1;
                        if (inverse === true)
                            targetState = !targetState;

                        if (isDecoupler(themeId)) {
                            if (targetState === false) {
                                newimgfname += "-on";
                            } else if (targetState === true) {
                                newimgfname += "";
                            }
                        } else if (isSignal(themeId)) {
                            newimgfname = newimgfname.replace("-r", "");
                            newimgfname = newimgfname.replace("-g", "");
                            if (targetState === false) {
                                newimgfname += "-g";
                            } else if (targetState === true) {
                                newimgfname += "-r";
                            }
                        } else if (isAccessory(themeId)) {
                            if (targetState === false) {
                                newimgfname += "-on";
                            } else if (targetState === true) {
                                newimgfname += "-off";
                            }
                        } else if (isSwitchOrAccessory(themeId)) {
                            if (targetState === false) {
                                //newimgfname += ".png";
                            } else if (targetState === true) {
                                newimgfname += "-t";
                            }
                        }

                        let currentSrc = img.attr("src");
                        let isOcc = false;
                        let isRoute = false;
                        isOcc = currentSrc.includes("-occ");
                        isRoute = currentSrc.includes("-route");

                        if (isOcc) newimgfname += "-occ";
                        else if (isRoute) newimgfname += "-route";

                        newimgfname += ".png";

                        img.attr("src", dirpath + newimgfname);
                    }
                    break;

                case 2:
                    {
                        const ctrl = accCtrl.planItem;
                        const addrs = accCtrl.ecosAddresses;
                        const key0 = addrs[0].ecosAddr;
                        const key1 = addrs[1].ecosAddr;
                        const accAddr = acc.addr;

                        let recent = ctrl.data("recentState");
                        if (typeof recent === "undefined") {
                            recent = {};
                            recent[key0] = "straight";
                            recent[key1] = "straight";
                        }

                        if (acc.state === 0) { // straight/green
                            recent[accAddr] = "straight";
                        } else if (acc.state === 1) { // turn/red
                            recent[accAddr] = "turn";
                        }

                        let s0 = recent[key0];
                        let s1 = recent[key1];

                        // keep the new target state
                        // do not store the inverted state !!
                        const originalRecent = recent;
                        ctrl.data("recentState", originalRecent);

                        if (addrs[0].ecosAddr === accAddr) {
                            if (addrs[0].inverse === true) {
                                if (s0 === "turn") s0 = "straight";
                                else if (s0 === "straight") s0 = "turn";
                            }
                            recent[accAddr] = s0;
                        }

                        if (addrs[1].ecosAddr === accAddr) {
                            if (addrs[1].inverse === true) {
                                if (s1 === "turn") s1 = "straight";
                                else if (s1 === "straight") s1 = "turn";
                            }
                            recent[accAddr] = s1;
                        }

                        // TODO distinguish isSignal and isSwitchOrAccessory 
                        // TODO distinguish isSignal and isSwitchOrAccessory 
                        // TODO distinguish isSignal and isSwitchOrAccessory 

                        if (s0 === "straight" && s1 === "straight") {
                            //newimgfname += ".png";
                        } else if (s0 === "turn" && s1 === "turn") {
                            newimgfname += "-t";
                        } else if (s0 === "turn" && s1 === "straight") {
                            newimgfname += "-tl";
                        } else if (s0 === "straight" && s1 === "turn") {
                            newimgfname += "-tr";
                        }

                        const currentSrc = img.attr("src");
                        const isOcc = currentSrc.includes("-occ");
                        const isRoute = currentSrc.includes("-route");

                        if (isOcc) newimgfname += "-occ";
                        else if (isRoute) newimgfname += "-route";

                        newimgfname += ".png";

                        img.attr("src", dirpath + newimgfname);
                    }
                    break;
            }

        }
    }

    applyAddressToAccessory(identifier, address) {
        if (typeof identifier === "undefined" || identifier == null) return;
        if (typeof address === "undefined" || address == null) return;
        const accCtrls = $('div.ctrlItemAccessory[id="' + identifier + '"]')
        if (typeof accCtrls === "undefined" || accCtrls == null || accCtrls.length === 0) return;
        const c = $(accCtrls[0]);
        const data = c.data(constDataThemeItemObject);
        data.addresses = address;
        c.data(constDataThemeItemObject, data);
    }

    createControlOnDrop(ev, options = {}) {
        const self = this;
        ev.preventDefault();
        if (ev.dataTransfer) {
            const data = ev.dataTransfer.getData("text/plain");
            const jsonData = JSON.parse(data);
            this.createControlWithEv(jsonData, ev, options);
        }
    }

    createControlWithEv(jsonData, ev, options = {}) {
        const self = this;
        const coord = getElementCoord(self.planfieldElement, ev);
        if (jsonData != null && typeof jsonData.editor !== "undefined") {
            const editorData = jsonData.editor;
            const coordOffsetX = parseInt(editorData.offsetX / constItemWidth);
            const coordOffsetY = parseInt(editorData.offsetY / constItemHeight);
            coord.x -= coordOffsetX;
            coord.y -= coordOffsetY;
        }

        this.createControl(jsonData, coord, options);
    }

    createControl(jsonData, coord, options = {}) {
        const self = this;

        var knownControls = $('div.ctrlItem[id]');
        var knownCtrl = getCtrlOfCoord(coord.x, coord.y, knownControls);
        if (typeof knownCtrl !== "undefined" && knownCtrl != null) {
            return;
        }

        if (jsonData == null) return;

        var pixelCoord = coord2pixel(coord);

        if (typeof options.editMode === "undefined")
            options.editMode = true;

        var data = JSON.stringify(jsonData);
        data = window.btoa(data);

        var dimensions = jsonData.dimensions;
        if (typeof dimensions === "undefined")
            dimensions = [{ w: 1, h: 1 }];

        const w = dimensions[0].w * constItemWidth;
        const h = dimensions[0].h * constItemHeight;

        if (typeof jsonData.editor === "undefined")
            jsonData.editor = {};
        if (typeof jsonData.editor.themeDimIdx === "undefined")
            jsonData.editor.themeDimIdx = 0;

        var newId = "";
        if (typeof jsonData.identifier !== "undefined" && this.__isInitialization) {
            newId = jsonData.identifier;
            if (newId.length == 0)
                newId = this.generateUniqueItemIdentifier(jsonData.editor.themeId);
        } else {
            newId = this.generateUniqueItemIdentifier(jsonData.editor.themeId);
        }

        var newCtrl = null;

        if (jsonData.editor.themeId === 1010) {
            // textfield element

            var innerHtml = null;
            var fontSizeValue = null;
            var sizeInit = null;
            if (jsonData.editor.innerHtml)
                innerHtml = jsonData.editor.innerHtml;
            if (jsonData.editor.outerHtml)
                fontSizeValue = $(jsonData.editor.outerHtml).css("font-size");
            if (jsonData.editor.size)
                sizeInit = jsonData.editor.size;

            var textfieldElement = new TextfieldElement();
            textfieldElement.install({
                fontSize: fontSizeValue,
                uniqueId: jsonData.identifier
            });

            window.textfieldElementInstances.push(textfieldElement);

            newCtrl = textfieldElement.__element;
            newCtrl.addClass("ctrlItem");
            newCtrl.attr("draggable", true);
            newCtrl.css({
                left: pixelCoord.x + "px",
                top: pixelCoord.y + "px",
                width: w + "px",
                height: h + "px",
                "z-index": 0
            });
            newCtrl.data(constDataThemeItemObject, jsonData);
            newCtrl.data(constDataThemeDimensionIndex, jsonData.editor.themeDimIdx);

            var targetEl = newCtrl.find('.elEditor');
            if (jsonData.editor.innerHtml && innerHtml !== null) {
                targetEl.html(innerHtml);
            }
            if (jsonData.editor.outerHtml && fontSizeValue !== null) {
                targetEl.css("font-size", fontSizeValue);
            }
            if (jsonData.editor.size && sizeInit !== null) {
                targetEl.css({
                    width: sizeInit.width,
                    height: sizeInit.height
                });
                targetEl.parent().css({
                    width: sizeInit.width,
                    height: sizeInit.height
                });
            }

            if (!this.__isInitialization)
                textfieldElement.setEditMode(true);
        }
        else {
            // general approach to create track element

            newCtrl = $('<div>')
                .addClass("ctrlItem")
                .attr("id", newId)
                .css({
                    left: pixelCoord.x + "px",
                    top: pixelCoord.y + "px",
                    width: w + "px",
                    height: h + "px"
                });
            newCtrl.data(constDataThemeItemObject, jsonData);
            newCtrl.data(constDataThemeDimensionIndex, jsonData.editor.themeDimIdx);

            var themeId = jsonData.editor.themeId;
            if (isBlock(themeId)) {
                newCtrl.addClass("ctrlItemBlock");
            } else if (isSwitchOrAccessory(themeId)
                || isAccessory(themeId)
                || isSignal(themeId)
                || isDecoupler(themeId)) {
                newCtrl.addClass("ctrlItemAccessory");
            }
            else if (isFeedback(themeId)) {
                newCtrl.addClass("ctrlItemFeedback");
            }

            var icon = "";
            if (jsonData.active && jsonData.active.default)
                icon = jsonData.active.default + ".png";
            else
                icon = jsonData.basename + ".png";

            var newImg = $('<img>', {})
                .attr({
                    //"data-tipso-title": it.name,
                    //"data-tipso": it.name,
                    src: "theme/" + window.settings.themeName + "/" + icon
                });

            newImg.appendTo(newCtrl);

            //
            // Label for the Element
            //
            var labelOffset = getLabelOffsetBy(jsonData);
            if (labelOffset !== null) {
                labelOffset.display = "none";
                var newLbl = $('<div>', {})
                    .css(labelOffset)
                    .html(
                        jsonData.identifier
                        //+ ", " +
                        //jsonData.editor.themeId
                    );
                newLbl.addClass("elementLabel");
                newLbl.appendTo(newCtrl);
            }
        }

        if (newCtrl === null) {
            return;
        }

        //
        // init drop for block
        //
        if (isBlock(jsonData.editor.themeId)) {
            this.__initBlockDrop(newCtrl);
        }

        newCtrl.appendTo(this.planfieldElement);

        newCtrl.click(function (ev) {
            const isTextFieldCtrl = $(this).hasClass("elEditorRoot");
            if (isTextFieldCtrl) return;

            if (self.isEditMode) {
                if (!self.currentSelectionMoved) {
                    if (self.isCtrlSelected(newCtrl)) {
                        self.unselectCtrl(newCtrl);
                    } else {
                        self.clearCtrlSelection();
                        self.selectCtrl(newCtrl);
                    }
                }
            }
        });

        newCtrl.mouseover(function (ev) {
            const themeItemJson = $(this).data(constDataThemeItemObject);

            var coord = themeItemJson.coord;
            if (!coord)
                coord = getElementCoord(self.planfieldElement, ev);

            const ctrlId = $(this).attr("id");

            const infoText = themeItemJson.name
                + " (" + coord.x + ", " + coord.y + ")"
                + " &mdash; " + ctrlId;

            const targetEl = $('#statusBar div.ctrlInfo');
            targetEl.html(infoText);
        });

        newCtrl.mouseleave(function (ev) {
            const targetEl = $('#statusBar div.ctrlInfo');
            targetEl.html("");
        });

        if (this.isEditMode)
            this.initDragFor(newCtrl);

        if (options.editMode) {
            var isTextFieldCtrl = newCtrl.hasClass("elEditorRoot");
            if (isTextFieldCtrl)
                ; // TODO enable edit mode but only with "remove" command
            else
                this.selectCtrl(newCtrl);
        }
        this.editMenubar.setCurrentSelection(newCtrl);

        this.editMenubar.rot(-1, newCtrl, this.__isInitialization);

        this.__trigger('controlCreated',
            {
                instance: newCtrl
            });

        if (!this.__isInitialization)
            window.serverHandling.sendControl(newCtrl);
    }

    removeDragFor(jqueryEl) {
        if (jqueryEl === null) return;
        const elmnt = jqueryEl.get(0);
        elmnt.ondragstart = null;
    }

    initDragFor(jqueryEl) {
        var elmnt = jqueryEl.get(0);
        elmnt.ondragstart = dragStart;
        var ctx = jqueryEl.parent();

        const self = this;

        var deltaCoord = null;

        function dragStart(e) {
            if (!self.isEditMode)
                return;

            e = e || window.event;
            e.preventDefault();
            e.stopPropagation();

            self.currentSelectionMoved = false;
            self.currentSelectionMovedItem = null;

            const isTextFieldCtrl = $(this).hasClass("elEditorRoot");
            if (isTextFieldCtrl) {
                const elToSelect0 = $(this);

                self.currentSelectionMoved = true;
                self.currentSelectionMovedItem = elToSelect0;

                self.selectCtrl(elToSelect0, {
                    startEditMode: false
                });
            } else {
                const elToSelect1 = $(e.target).parent();

                self.currentSelectionMoved = true;
                self.currentSelectionMovedItem = elToSelect1;

                self.selectCtrl(elToSelect1);
            }

            document.onmouseup = closeDragElement;
            document.onmousemove = elementDrag;

            const coord = getElementCoord(ctx, e);
            const parent = $(e.target).parent();
            const coordItemStart = {
                x: Math.floor(parent.get(0).offsetLeft / constItemWidth),
                y: Math.floor(parent.get(0).offsetTop / constItemHeight)
            }

            deltaCoord = {
                x: Math.floor(coord.x - coordItemStart.x),
                y: Math.floor(coord.y - coordItemStart.y)
            };
        }

        function elementDrag(e) {
            if (!self.isEditMode)
                return;

            e = e || window.event;
            e.preventDefault();
            e.stopPropagation();

            const coord = getElementCoord(ctx, e);
            var top = ((coord.y - deltaCoord.y) * constItemHeight);
            var left = ((coord.x - deltaCoord.x) * constItemWidth);

            const isTextFieldCtrl = self.currentSelection.hasClass("elEditorRoot");
            if (isTextFieldCtrl) { // Ries: I do not know why, but this helps for dragging.
                top = Math.floor(coord.y * constItemHeight);
                left = Math.floor(coord.x * constItemWidth);
            }

            elmnt.style.top = top + "px";
            elmnt.style.left = left + "px";

            if (isTextFieldCtrl) {
                // do not open edit editMenubar
            } else {
                self.editMenubar.updateEditMenuPositionAndEvents(self.currentSelection);
            }
        }

        function closeDragElement(ev) {
            if (!self.isEditMode)
                return;

            document.onmouseup = null;
            document.onmousemove = null;

            if (!self.__isInitialization) {
                window.serverHandling.removeControl(self.currentSelectionMovedItem.attr("id"));
                window.serverHandling.sendControl(self.currentSelectionMovedItem);
            }
            self.currentSelectionMovedItem = null;
        }
    }

    highlightAccessory(accessoryIdentifier, state) {
        const self = this;
        const id = accessoryIdentifier;
        const ctrls = $('div.ctrlItem[id]');
        for (let i = 0; i < ctrls.length; ++i) {
            const ctrl = ctrls[i];
            if (!ctrl) continue;
            const jsonData = $(ctrl).data(constDataThemeItemObject);
            if (jsonData == null) continue;
            if (jsonData.identifier === accessoryIdentifier) {
                if (state)
                    $(ctrl).addClass("highlightAccessory");
                else
                    $(ctrl).removeClass("highlightAccessory");

                return;
            }
        }
    }

    initFromServer(metamodel) {
        const planField = metamodel.planField;

        this.__isInitialization = true;

        for (let key in planField) {
            if (planField.hasOwnProperty(key)) {
                const itemCtrl = planField[key];
                this.createControl(itemCtrl, itemCtrl.coord, { editMode: false });
            }
        }

        this.__isInitialization = false;
    }
}