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
                    }
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

    __saveLocomotiveSettings(locomotiveData) {
        let chkStates = {};
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
                    oid: locomotiveData.recid,
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
        this.__recentEcosData = ecosDataLocomotives;
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
            }
        }

        if (listOfObjectsToAdd.length > 0)
            elGrid.add(listOfObjectsToAdd);
    }

    __getExpandedHtml(rec) {
        const self = this;
        const recid = rec.oid;
        const locomotiveDataId = "locomotivesData_" + recid;

        let html = '<div id="' + locomotiveDataId + '" style="padding: 5px; width: 100%; float: left;">';

        const dataOptions = self.__getCheckboxOptions('Options:', recid);
        const dataTypes = self.__getCheckboxTypes('Types:', recid);

        html += dataOptions.html;
        html += dataTypes.html;

        html += '</div>';

        let chkIds = [];
        chkIds = chkIds.concat(dataOptions.checkboxIds);
        chkIds = chkIds.concat(dataTypes.checkboxIds);

        return {
            renderId: locomotiveDataId,
            checkboxIds: chkIds,
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
        html += '<label ' + cssLabel + '><input name="OptionMainline" id="' + ids.chkIdMainline + '" type="checkbox" ' + cssInput + '> Mainline </label>';

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
        this.__recentOcc = occInformation;
        const occData = occInformation.data; // array
        for (let i = 0; i < occData.length; ++i) {
            const occDataItem = occData[i];

            const recid = occDataItem.Oid;
            const fromBlock = occDataItem.FromBlock;

            // TBD
        }
    }
}
