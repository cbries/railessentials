/**
 * Supported events:
 * locomotiveDoubleClick : { sender: this, data : Record ROW }
 * functionChanged : { sender: this, data : { oid : INT, fncIdx: INT, timestamp: Date.now() }}
 */
class Locomotives {
    constructor() {
        console.log("**** construct Locomotives");

        this.winLocomotives = null;
        this.gridLocomotives = null;
        this.__locomotiveImages = {};
        this.__recentEcosData = null;
        this.__installed = false;

        this.__gridName = "gridLocomotives";
        this.__dialogName = "dialogLocomotives";
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
        this.__windowGeometry.showWithGeometry(el);

        if (this.__installed) {
            el.dialog("open");
        } else {
            this.install({ autoOpen: true });
            el.dialog("open");
        }

        this.updateLocomotives(this.__recentEcosData);

        w2ui[this.__gridName].refresh();
    }

    isShown() {
        try {
            return $('#' + this.__dialogName).dialog('isOpen');
        } catch (error) {
            // ignore
        }
        return false;
    }

    __handleLocomotiveFunctionToggle(ev) {
        //console.log(ev);
    }

    __renderImage(record) {
        const fakeId = "loadLocomotivesImage_" + record.oid;
        loadLocomotiveImageIntoHtml(fakeId, record.name);
        const innerHtml = '<div style="height: 100%; width: 100%; white-space: nowrap; text-align: center; padding-top: 3px;">' +
            '<span style="display: inline-block;height: 100%;vertical-align: middle;"></span>' +
            '<img id="'
            + fakeId
            + '" ' +
            'style="height: 16px; width: 16px; margin: auto;" ' +
            'src="./images/noimage32x32.png" />'
            + '</div>';
        return innerHtml;
    }

    __renderFunctions(record, index, column_index) {
        // record.noOfFunctions
        // record.funcset
        // record.funcdesc

        const maxFnc = record.noOfFunctions;
        if (maxFnc == 0)
            return "<b>&mdash;</b>";

        const availableFuncsFromLeftToRight = window.getArrayOfFunctions(record);

        let html = "";
        $(availableFuncsFromLeftToRight).each(function () {
            const data = this;

            let fncDescription = ListOfFunctionDescription[data.type];
            if (typeof fncDescription === "undefined")
                fncDescription = "Unknown";

            const evData = { oid: data.oid, fncIdx: data.idx, timestamp: Date.now() };
            const evDataCleaned = window._htmlEntities(JSON.stringify(evData));
            const className = 'fncCommand_' + data.oid + "_" + data.idx;
            const onClick = 'onClick="window.__eventTrigger(\'functionChanged\', \'' + evDataCleaned + '\');"';
            const checked = data.state ? "checked" : "";
            html += '<input class="' + className + '" type="checkbox" ' + checked + ' ' + onClick + ' title="' + fncDescription + '">';
        });

        return html;
    }

    getLocomotiveRecord(oid) {
        if (oid <= 0) return null;
        if (!w2ui[this.__gridName]) return null;
        const grid = w2ui[this.__gridName];
        const rec = grid.find({ oid: oid });
        if (typeof rec !== "undefined" && rec != null && rec.length > 0) {
            const recAccessor = rec[0];
            return grid.get(recAccessor);
        }
        return null;
    }

    install(options = {}) {
        const self = this;
        const state = this.isShown();
        if (state) return;

        if (typeof options.autoOpen === "undefined")
            options.autoOpen = false;

        this.__gridRowDoubleBlickCount = 0;
        this.__expandedPreferences = {};
        const geometry = this.__windowGeometry.recent();

        $("#" + this.__dialogName).dialog({
            height: geometry.height,
            width: geometry.width,
            left: geometry.left,
            top: geometry.top,
            closeOnEscape: false,
            autoOpen: options.autoOpen,
            resizeStop: function (event, ui) {
                self.__windowGeometry.save(ui.position, ui.size);
            },
            dragStop: function (event, ui) {
                self.__windowGeometry.save(ui.position,
                    {
                        width: event.target.clientWidth,
                        height: event.target.clientHeight
                    });
            }
        });

        if (!w2ui[this.__gridName]) {
            const targetGridEl = $('#' + this.__gridName);
            targetGridEl.w2grid({
                name: this.__gridName,
                header: "Locomotives",
                multiSelect: false,
                show: {
                    //lineNumbers: true,
                    toolbar: true,
                    //header: true,
                    footer: true,
                    toolbarAdd: false,
                    toolbarDelete: false,
                    toolbarEdit: false,
                    toolbarSave: false,
                },
                dragRow: true,
                textSearch: 'contains',
                recid: 'oid', // rename "recid" to "oid"
                searches: [
                    { field: 'name', caption: 'Name', type: 'text' }
                ],
                sortData: [{ field: 'name', direction: 'asc' }],
                columns: [
                    { field: 'oid', caption: 'Object ID', size: '10%', sortable: true },
                    {
                        field: 'locImage',
                        caption: 'Photo',
                        size: '10%',
                        style: 'text-align: center',
                        render: self.__renderImage
                    },
                    { field: 'name', caption: 'Name', size: '30%', sortable: true, editable: { type: 'text' } },
                    { field: 'protocol', caption: 'Protocol', size: '10%', sortable: true },
                    { field: 'addr', caption: 'Address', size: '10%' },
                    { field: 'speed', caption: 'Speed', size: '10%', hidden: true },
                    { field: 'speedstep', caption: 'Speedstep', size: '10%', hidden: false },
                    { field: 'speedstepMax', caption: 'Speedstep (max)', size: '10%', hidden: false },
                    {
                        field: 'funcset',
                        caption: 'Functions',
                        size: '15%',
                        render: self.__renderFunctions
                    },
                    { field: 'funcdesc', caption: 'Func.-Desc.', hidden: true },
                    { field: 'noOfFunctions', caption: 'No. of Functions', hidden: true },
                    {
                        field: 'locked',
                        caption: 'Locked',
                        size: '10%',
                        style: 'text-align: center',
                        editable: {
                            type: 'checkbox',
                            style: 'text-align: center'
                        }
                    },
                    { field: 'speedLevel1', caption: 'Level1', size: '1%', hidden: true },
                    { field: 'speedLevel2', caption: 'Level2', size: '1%', hidden: true },
                    { field: 'speedLevel3', caption: 'Level3', size: '1%', hidden: true },
                    { field: 'speedLevel4', caption: 'Level4', size: '1%', hidden: true },
                ],
                records: [],
                onExpand: function (event) {
                    if (self.__gridRowDoubleBlickCount !== 0) {
                        --self.__gridRowDoubleBlickCount;
                        event = event || window.event;
                        event.preventDefault();
                        event.stopPropagation();
                        return;
                    }

                    const elGrid = w2ui[self.__gridName];
                    const rec = elGrid.get(event.recid);
                    const data = self.__getExpandedHtml(rec);
                    data.recid = event.recid;
                    $('#' + event.box_id).html(data.renderHtml);

                    self.__expandedPreferences[event.recid] = true;

                    event.onComplete = function () {
                        const chkIds = data.checkboxIds;
                        let activateChkEvents = [];
                        for (let j = 0; j < chkIds.length; ++j) {
                            const chkCtrl = $('#' + chkIds[j]);
                            if (typeof chkCtrl === "undefined" || chkCtrl == null) continue;
                            chkCtrl.w2field('checkbox');
                            activateChkEvents.push(chkCtrl);
                        }

                        const currentBlockCtrl = $('#' + data.blockCurrentId);
                        currentBlockCtrl.w2field('list', { items: [] });

                        // apply css changes to render the content correct
                        $('div#locomotivesData_' + event.recid + ' #' + data.selectId + ' + span').css({
                            "margin-left": "10px",
                        });
                        $('div#locomotivesData_' + event.recid + ' #' + data.selectId + ' + span span.selection span.select2-selection').css({
                            "border-radius": "3px",
                            "border": "1px solid #cacaca"
                        });

                        self.__loadRecentStatesFor(event.recid);

                        currentBlockCtrl.change(function () { self.__saveLocomotiveSettings(data); });
                        for (let i = 0; i < activateChkEvents.length; ++i) {
                            activateChkEvents[i].change(function () {
                                self.__saveLocomotiveSettings(data);
                            });
                        }

                        for (let j = 0; j < data.speedLevel.length; ++j) {
                            const selectLevel = data.speedLevel[j];
                            if (typeof selectLevel === "undefined") continue;
                            if (selectLevel == null) continue;
                            const ctrl = $('#' + selectLevel);
                            ctrl.change(function (ev) {
                                const id = $(this).attr('id');
                                const idx = id.lastIndexOf("_");
                                const speedLevel = id.substr(0, idx);
                                const locOid = id.substr(idx + 1);

                                //console.log("speedLevel: " + speedLevel);
                                //console.log("locOid: " + locOid);
                                //console.log(this.value);

                                self.__trigger("setting",
                                    {
                                        "mode": "locomotive",
                                        "cmd": "speedLevel",
                                        "value": {
                                            "oid": locOid,
                                            "level": speedLevel,
                                            "value": parseInt(this.value)
                                        }
                                    });
                            });
                        }

                        const cmdSpeedCurve = $('#cmdSpeedCurveContainer_' + event.recid);
                        cmdSpeedCurve.w2field('button');
                        cmdSpeedCurve.click(function () {
                            const locOid = event.recid;
                            self.__showSpeedCurveDialog(locOid);
                        });

                        self.__resizePreferences();
                    }
                },
                onCollapse: function (event) {
                    self.__expandedPreferences[event.recid] = false;
                },
                onDblClick: function (event) {
                    self.__gridRowDoubleBlickCount++;

                    const elGrid = w2ui[self.__gridName];
                    const columnIdx = event.column;
                    const c = elGrid.columns[columnIdx];
                    if (c.field === "name") {
                        event.stopPropagation();
                        return;
                    }

                    const rec = elGrid.get(event.recid);
                    self.__trigger('locomotiveDoubleClick', rec);
                },
                onSelect: function(ev) {
                    bringToFront(self.__dialogName);
                }
            });
        }

        const gridTb = w2ui[self.__gridName].toolbar;
        if (gridTb) {
            gridTb.add({
                type: 'button',
                id: 'cmdSave',
                text: 'Save ',
                img: 'fas fa-save',
                tooltip: function (item) {
                    return 'Applies all changes to the server.';
                },
                onClick: function (ev) {
                    const elGrid = w2ui[self.__gridName];
                    const changes = elGrid.getChanges();
                    for (let i = 0; i < changes.length; ++i) {
                        const change = changes[i];

                        const recid = change.recid;
                        const row = elGrid.get(recid);
                        const oid = row.oid;

                        const locName = change.name;
                        if (typeof locName !== "undefined" && locName != null) {
                            row.name = locName;
                            elGrid.refreshCell(row, 'name');
                            self.__trigger("setting",
                                {
                                    "mode": "locomotive",
                                    "cmd": "rename",
                                    "value": {
                                        "oid": oid,
                                        "name": locName
                                    }
                                });
                        }

                        const isLocked = change.locked;
                        if (typeof isLocked !== "undefined" && isLocked != null) {
                            row.locked = isLocked;
                            elGrid.refreshCell(row, 'locked');
                            self.__trigger("setting",
                                {
                                    "mode": "locomotive",
                                    "cmd": "lock",
                                    "value": {
                                        "oid": oid,
                                        "locked": isLocked
                                    }
                                });
                        }
                    }

                    self.__cleanupChangedState();
                }
            });
        }

        this.__installed = true;
    }

    __getLocomotiveEcosData(oid) {
        const self = this;
        if (typeof self.__recentEcosData === "undefined" || self.__recentEcosData == null)
            return null;
        const localOid = parseInt(oid);
        for (let i = 0; i < self.__recentEcosData.length; ++i) {
            const loc = self.__recentEcosData[i];
            if (loc == null) continue;
            if (loc.objectId === localOid)
                return loc;
        }
        return null;
    }

    __showSpeedCurveDialog(locOid) {
        const self = this;

        if (!w2ui.formSpeedCurve) {
            $().w2form({
                name: 'formSpeedCurve',
                style: 'border: 0px; background-color: white;',
                formHTML:
                    '<div class="w2ui-page page-0">' +
                    '    <div id="formSpeedCurveInstance" class="speedCurveDesign"></div>' +
                    '</div>' +
                    '<div class="w2ui-buttons">' +
                    '    <button class="w2ui-btn" name="cancel">Cancel</button>' +
                    '    <button class="w2ui-btn" name="save">Save</button>' +
                    '</div>',
                fields: [],
                record: {},
                actions: {
                    "save": function () {
                        const data = self.__speedCurveInstance.getData();
                        const oid = data.objectId;
                        delete data.objectId;
                        self.__saveLocomotiveSpeedCurve(oid, data);
                        w2popup.close();
                    },
                    "cancel": function () {
                        w2popup.close();
                    }
                }
            });
        }

        $().w2popup('open', {
            title: 'SpeedCurve',
            body: '<div id="form" style="width: 100%; height: 100%;"></div>',
            style: 'padding: 0px 0px 0px 0px; padding-left: 5px; background-color: white;',
            width: 775,
            height: 400,
            showMax: false,
            onToggle: function (event) {
                $(w2ui.foo.box).hide();
                event.onComplete = function () {
                    $(w2ui.foo.box).show();
                    w2ui.foo.resize();
                }
            },
            onOpen: function (event) {
                event.onComplete = function () {
                    $('#w2ui-popup #form').w2render('formSpeedCurve');

                    const locEsuData = self.__getLocomotiveEcosData(locOid);

                    let speedModeProtocol = "dcc128";
                    let speedMax = 55;
                    let timeMax = 15;

                    if (locEsuData != null) {
                        if (locEsuData.protocol === "DCC128"
                            || locEsuData.protocol === "MM128"
                            || locEsuData.protocol === "MFX") {

                            speedModeProtocol = "dcc128";
                            speedMax = 55;
                            timeMax = 15;

                        } else if (locEsuData.protocol === "MMFKT"
                            || locEsuData.protocol === "MM14"
                            || locEsuData.protocol === "DCC14") {

                            speedModeProtocol = "dcc14";
                            speedMax = 7;
                            timeMax = 5;

                        } else if (locEsuData.protocol === "MM27" || locEsuData.protocol === "DCC28") {

                            speedModeProtocol = "dcc28";
                            speedMax = 7;
                            timeMax = 5;

                        }
                    }

                    const recentLocomotiveData = self.__getLocomotiveOfRecentData(locOid);
                    let preloadData = "esu";
                    
                    if (recentLocomotiveData != null && recentLocomotiveData.SpeedCurve != null) {
                        if (typeof recentLocomotiveData.SpeedCurve.steps !== "undefined" &&
                            recentLocomotiveData.SpeedCurve.steps != null) {
                            preloadData = [];
                            for (let ii = 0; ii < recentLocomotiveData.SpeedCurve.steps.length; ++ii) {
                                preloadData.push(recentLocomotiveData.SpeedCurve.steps[ii].speed);
                            }
                        }
                        if (typeof recentLocomotiveData.SpeedCurve.maxSpeed !== "undefined") {
                            speedMax = recentLocomotiveData.SpeedCurve.maxSpeed;
                        }
                        if (typeof recentLocomotiveData.SpeedCurve.maxTime !== "undefined") {
                            timeMax = recentLocomotiveData.SpeedCurve.maxTime;
                        }
                    }

                    self.__speedCurveInstance = $('#w2ui-popup #formSpeedCurveInstance').speedCurve({
                        objectId: locOid,
                        speedMode: speedModeProtocol,
                        speedStepMaxDefault: speedMax,
                        speedTimeMaxDefault: timeMax,
                        height: 220,
                        preloadData: preloadData
                    });
                    self.__speedCurveInstance.refresh();
                }
            }
        });
    }

    __resizePreferences() {
        const obj = this.__expandedPreferences;
        $.each(obj, function (recid, state) {
            if (state === true) {
                try {
                    const h = $('div#locomotivesData_' + recid).get(0).getBoundingClientRect().height;
                    $('div#grid_gridLocomotives_frec_' + recid + '_expanded').css({
                        height: h + "px"
                    });
                }
                catch (err) {

                }
            }
        });
    }

    __saveLocomotiveSpeedCurve(oid, speedCurveData) {
        if (typeof speedCurveData === "undefined" || speedCurveData == null)
            return;

        this.__trigger('setting',
            {
                'mode': 'locomotive',
                'cmd': 'speedCurve',
                'value': {
                    oid: oid,
                    data: speedCurveData
                }
            });
    }

    __saveLocomotiveSettings(locomotiveData) {
        const chkStates = {};
        const chkIds = locomotiveData.checkboxIds;
        for (let j = 0; j < chkIds.length; ++j) {
            const chkCtrl = $('#' + chkIds[j]);
            if (typeof chkCtrl === "undefined" || chkCtrl == null)
                continue;
            const chkId = $(chkCtrl).attr('name');
            const chkState = $(chkCtrl).is(":checked");
            chkStates[chkId] = chkState;
        }

        const recid = locomotiveData.recid;

        this.__trigger('setting',
            {
                'mode': 'locomotive',
                'cmd': 'locomotiveData',
                'value': {
                    oid: recid,
                    checkboxSettings: chkStates
                }
            });
    }

    __cleanupChangedState() {
        const self = this;
        $('#' + this.__dialogName + ' td.w2ui-grid-data').each(function () {
            $(this).removeClass('w2ui-changed');
        });
    }

    updateLocomotives2(locomotivesData) {
        const self = this;
        const elGrid = w2ui[self.__gridName];
        const data = locomotivesData.data;

        this.__recentLocomotivesData = data;

        for (let key in data) {
            if (data.hasOwnProperty(key)) {
                const oid = parseInt(key);
                const row = elGrid.get(oid);
                if (typeof row === "undefined" || row == null)
                    continue;

                if (row.locked !== data[oid].IsLocked) {
                    row.locked = data[oid].IsLocked;
                    elGrid.refreshCell(oid, 'locked');
                }
            }
        }

        self.__cleanupChangedState();
    }

    updateLocomotives(ecosDataLocomotives) {
        const self = this;
        self.__recentEcosData = ecosDataLocomotives;
        if (typeof ecosDataLocomotives === "undefined" || ecosDataLocomotives == null)
            return;

        const elGrid = w2ui[self.__gridName];

        let listOfObjectsToAdd = [];

        let i;
        const iMax = this.__recentEcosData.length;
        for (i = 0; i < iMax; ++i) {
            const ecosObj = this.__recentEcosData[i];
            if (typeof ecosObj === "undefined" || ecosObj == null) continue;

            const oid = ecosObj.objectId;
            const name = ecosObj.name;
            const protocol = ecosObj.protocol;
            const addr = ecosObj.addr;
            const speed = ecosObj.speed;
            const speedstep = ecosObj.speedstep;
            const speedstepMax = ecosObj.speedstepMax;
            const funcset = ecosObj.funcset;
            const funcdesc = ecosObj.funcdesc;
            const noOfFunctions = ecosObj.nrOfFunctions;
            
            const rec = elGrid.find({ oid: oid });
            if (rec.length <= 0) {
                //
                // add new record
                //
                listOfObjectsToAdd.push(
                    {
                        oid: oid,
                        name: name,
                        protocol: protocol,
                        addr: addr,
                        speed: speed,
                        speedstep: speedstep,
                        speedstepMax: speedstepMax,
                        noOfFunctions: noOfFunctions,
                        locked: false,
                        funcset: funcset,
                        funcdesc: funcdesc
                    }
                );
            } else {
                //
                // update record
                //
                const row = elGrid.get(oid);

                const getCtrlDlg = (function () {
                    const locCtrlSelector = constLocomotiveControlBaseName + oid;
                    const locCtrlInstance = window.findLocomotivesDlg(locCtrlSelector);
                    return locCtrlInstance;
                });
                const locCtrlInstance = getCtrlDlg();

                if (row.name !== name) {
                    row.name = name;
                    elGrid.refreshCell(oid, 'name');
                }

                if (row.addr !== addr) {
                    row.addr = addr;
                    elGrid.refreshCell(oid, 'addr');
                }

                if (row.speedstep !== speedstep) {
                    row.speedstep = speedstep;
                    elGrid.refreshCell(oid, 'speedstep');

                    // IMPORTANT: call after update of the row
                    if (locCtrlInstance)
                        locCtrlInstance.updateSpeedState(ecosObj);
                }

                if (row.noOfFunctions !== noOfFunctions) {
                    row.noOfFunctions = noOfFunctions;
                    elGrid.refreshCell(oid, 'noOfFunctions');
                }

                if (row.funcset !== funcset) {
                    row.funcset = funcset;
                    elGrid.refreshCell(oid, 'funcset');

                    // IMPORTANT: call after update of the row
                    if (locCtrlInstance)
                        locCtrlInstance.updateFunctionStates(ecosObj);
                }

                // REMARK: will always be called, on any update
                // TODO probably we should improve this part for performance
                if (locCtrlInstance)
                    locCtrlInstance.updateDirectionState(ecosObj);
            }
        }

        if (listOfObjectsToAdd.length > 0)
            elGrid.add(listOfObjectsToAdd);
    }

    __getCurrentSpeedLevelsOf(oid, maxSpeedstep) {
        const self = this;
        const locData = self.__getLocomotiveOfRecentData(oid);
        try {
            const level1Value = locData.speedLevels.level1;
            const level2Value = locData.speedLevels.level2;
            const level3Value = locData.speedLevels.level3;
            const level4Value = locData.speedLevels.level4;

            return {
                "level1": parseInt(level1Value),
                "level2": parseInt(level2Value),
                "level3": parseInt(level3Value),
                "level4": parseInt(level4Value)
            };
        }
        catch(err) {
            return {
                "level1": parseInt(maxSpeedstep * 0.1),
                "level2": parseInt(maxSpeedstep * 0.3),
                "level3": parseInt(maxSpeedstep * 0.5),
                "level4": parseInt(maxSpeedstep * 0.6)
            };
        }
    }

    __getExpandedHtml(rec) {
        const self = this;
        const recid = rec.oid;
        const locomotiveDataId = "locomotivesData_" + recid;

        // start of div area
        let html = '<div id="' + locomotiveDataId + '" style="padding: 5px; width: 100%; float: left;">';

        const dataOptions = self.__getCheckboxOptions('Options:', recid);
        const dataTypes = self.__getCheckboxTypes('Types:', recid);

        html += dataOptions.html;
        html += dataTypes.html;

        //
        // speed levels, speedstepMax
        //
        const speedLevels = self.__getCurrentSpeedLevelsOf(recid, rec.speedstepMax);
        let hl1 = "", hl2 = "", hl3 = "", hl4 = "";
        for (let counter = 0; counter < rec.speedstepMax; ++counter) {
            let select1 = "";
            let select2 = "";
            let select3 = "";
            let select4 = "";

            if (counter === speedLevels.level1) select1 = "selected";
            if (counter === speedLevels.level2) select2 = "selected";
            if (counter === speedLevels.level3) select3 = "selected";
            if (counter === speedLevels.level4) select4 = "selected";
            
            hl1 += '<option value="' + counter + '" ' + select1 + '>' + counter + '</option>\n';
            hl2 += '<option value="' + counter + '" ' + select2 + '>' + counter + '</option>\n';
            hl3 += '<option value="' + counter + '" ' + select3 + '>' + counter + '</option>\n';
            hl4 += '<option value="' + counter + '" ' + select4 + '>' + counter + '</option>\n';
        }
        const speedLevelIds = [
            'speedLevel1_' + recid,
            'speedLevel2_' + recid,
            'speedLevel3_' + recid,
            'speedLevel4_' + recid
        ];
        html += '<div class="w2ui-field">';
        html += '<label style="float: left;">Speed Levels:</label>';
        html += '<div style="padding: 2px; margin-top: 3px; margin-left: 5px;">';
        html += ' &nbsp; Level1: <select style="height: 20px;" name="speedLevel1" id="' + speedLevelIds[0] + '">' + hl1 + '</select>';
        html += ' Level2: <select style="height: 20px;" name="speedLevel2" id="' + speedLevelIds[1] + '">' + hl2 + '</select>';
        html += ' Level3: <select style="height: 20px;" name="speedLevel3" id="' + speedLevelIds[2] + '">' + hl3 + '</select>';
        html += ' Level4: <select style="height: 20px;" name="speedLevel4" id="' + speedLevelIds[3] + '">' + hl4 + '</select>';
        html += '</div>';
        html += '</div>';

        //
        // SpeedCurve
        //
        html += '<div class="w2ui-field">';
        html += '<label>Speed Curve:</label>';
        html += '<input type="button" id="cmdSpeedCurveContainer_' + recid + '" ' +
                    'value="Modify" ' +
                    'style="padding: 2px; margin-top: 3px; margin-left: 5px;">';
        html += '</div > ';
       

        // end of div area
        html += '</div>';

        html += '</div>';

        let chkIds = [];
        chkIds = chkIds.concat(dataOptions.checkboxIds);
        chkIds = chkIds.concat(dataTypes.checkboxIds);

        return {
            renderId: locomotiveDataId,
            checkboxIds: chkIds,
            speedLevel: speedLevelIds,
            renderHtml: html
        };
    }

    __getCtrlIdsForOptionsAndTypes(recid) {
        return {
            chkIdDirection: 'locomotiveChkDirection_' + recid,
            chkIdMainline: 'locomotiveChkMainline_' + recid,

            chkIdOthers: 'locomotiveChkTypeOthers_' + recid,
            chkIdLocal: 'locomotiveChkTypeLocal_' + recid,
            chkIdIntercity: 'locomotiveChkTypeIntercity_' + recid,
            chkIdFreight: 'locomotiveChkTypeFreight_' + recid,
            chkIdShunting: 'locomotiveChkTypeShunting_' + recid,
            chkIdRegional: 'locomotiveChkTypeRegional_' + recid,
            chkIdBranchLine: 'locomotiveChkTypeBranchLine_' + recid,
            chkIdBranchLineFreight: 'locomotiveChkTypeBranchLineFreight_' + recid
        };
    }

    __getCheckboxOptions(lblTxt, recid) {
        let html = '';
        html += '<div class="w2ui-field">';
        html += '<label>' + lblTxt + '</label>';
        html += '<div style="padding-top: 7px;">';

        const cssLabel = 'style="word-wrap:break-word; font-weight: normal; margin-right: 10px;"';
        const cssInput = 'style="vertical-align: middle;"';

        const ids = this.__getCtrlIdsForOptionsAndTypes(recid);

        html += '<label ' + cssLabel + '><input name="OptionDirection" id="' + ids.chkIdDirection + '" type="checkbox" ' + cssInput + '> Allow change direction </label>';
        /**
         * currently disabled "Wait" and "Mainline" -- not really supported in the moment 
         */
        //html += '<label ' + cssLabel + '><input name="OptionMainline" id="' + ids.chkIdMainline + '" type="checkbox" ' + cssInput + '> Mainline </label>';

        html += '</div>';
        html += '</div>';
        return {
            html: html,
            checkboxIds: [
                ids.chkIdDirection,
                ids.chkIdMainline,
            ]
        };
    }

    __getCheckboxTypes(lblTxt, recid) {
        let html = '';
        html += '<div class="w2ui-field">';
        html += '<label>' + lblTxt + '</label>';
        html += '<div style="padding-top: 7px;">';

        const cssLabel = 'style="word-wrap:break-word; font-weight: normal; margin-right: 10px;"';
        const cssInput = 'style="vertical-align: middle;"';

        const ids = this.__getCtrlIdsForOptionsAndTypes(recid);

        html += '<label ' + cssLabel + '><input name="TypeOthers" id="' + ids.chkIdOthers + '" type="checkbox" ' + cssInput + '> Others </label>';
        html += '<label ' + cssLabel + '><input name="TypeLocal" id="' + ids.chkIdLocal + '" type="checkbox" ' + cssInput + '> Local </label>'; // Nahverkehr
        html += '<label ' + cssLabel + '><input name="TypeIntercity" id="' + ids.chkIdIntercity + '" type="checkbox" ' + cssInput + '> Intercity </label>'; // Fernverkehr
        html += '<label ' + cssLabel + '><input name="TypeFreight" id="' + ids.chkIdFreight + '" type="checkbox" ' + cssInput + '> Freight </label>'; // Fracht
        html += '<label ' + cssLabel + '><input name="TypeShunting" id="' + ids.chkIdShunting + '" type="checkbox" ' + cssInput + '> Shunting </label>'; // Rangieren
        html += '<label ' + cssLabel + '><input name="TypeRegional" id="' + ids.chkIdRegional + '" type="checkbox" ' + cssInput + '> Regional </label>'; // Regionalzug
        html += '<label ' + cssLabel + '><input name="TypeBranchLine" id="' + ids.chkIdBranchLine + '" type="checkbox" ' + cssInput + '> Branch Line </label>'; // Nebenbahn
        html += '<label ' + cssLabel + '><input name="TypeBranchLineFreight" id="' + ids.chkIdBranchLineFreight + '" type="checkbox" ' + cssInput + '> Branch Line (Freight) </label>';

        html += '</div>';
        html += '</div>';

        return {
            html: html,
            checkboxIds: [
                ids.chkIdOthers,
                ids.chkIdLocal,
                ids.chkIdIntercity,
                ids.chkIdFreight,
                ids.chkIdShunting,
                ids.chkIdRegional,
                ids.chkIdBranchLine,
                ids.chkIdBranchLineFreight
            ]
        };
    }

    __getLocomotiveOfRecentData(oid) {
        if (typeof this.__recentLocomotivesData === "undefined" || this.__recentLocomotivesData == null)
            return null;
        const data = this.__recentLocomotivesData[oid];
        if (typeof data === "undefined" || data == null) return null;
        return data;
    }

    __loadRecentStatesFor(recid) {
        const self = this;

        const recentLocomotiveData = this.__getLocomotiveOfRecentData(recid);
        if (recentLocomotiveData != null) {
            const ids = this.__getCtrlIdsForOptionsAndTypes(recid);

            $('#' + ids.chkIdDirection).prop('checked', recentLocomotiveData.Settings.OptionDirection);
            $('#' + ids.chkIdMainline).prop('checked', recentLocomotiveData.Settings.OptionMainline);

            $('#' + ids.chkIdOthers).prop('checked', recentLocomotiveData.Settings.TypeOthers);
            $('#' + ids.chkIdLocal).prop('checked', recentLocomotiveData.Settings.TypeLocal);
            $('#' + ids.chkIdIntercity).prop('checked', recentLocomotiveData.Settings.TypeIntercity);
            $('#' + ids.chkIdFreight).prop('checked', recentLocomotiveData.Settings.TypeFreight);
            $('#' + ids.chkIdShunting).prop('checked', recentLocomotiveData.Settings.TypeShunting);
            $('#' + ids.chkIdRegional).prop('checked', recentLocomotiveData.Settings.TypeRegional);
            $('#' + ids.chkIdBranchLine).prop('checked', recentLocomotiveData.Settings.TypeBranchLine);
            $('#' + ids.chkIdBranchLineFreight).prop('checked', recentLocomotiveData.Settings.TypeBranchLineFreight);
        }

        this.updateOccInformation(this.__recentOcc);
    }

    updateBlockInformation(blockInformation) {
        const self = this;
        const elGrid = w2ui[self.__gridName];
        const data = blockInformation.data;

        this.__recentFeedbacksData = data;

        // TBD
    }

    updateOccInformation(occInformation) {
        const self = this;
        //this.__recentOcc = occInformation;
        //const occData = occInformation.data; // array
        //for (let i = 0; i < occData.length; ++i) {
        //    const occDataItem = occData[i];
        //    const recid = occDataItem.Oid;
        //    const fromBlock = occDataItem.FromBlock;
        //    // TBD
        //}
    }
}
