class Blocks {
    constructor() {
        console.log("**** construct Blocks");

        this.__installed = false;

        this.winLocomotives = null;
        this.gridLocomotives = null;

        this.__gridName = "gridBlocks";
        this.__dialogName = "dialogBlocks";
        this.__storageNameGeometry = this.__dialogName + "_geometry";

        this.__windowGeometry = new WindowGeometryStorage(this.__dialogName);

        this.__initEventHandling();

        // testdata
        this.__fbsList = [];

        /*
         * { id: 1, text: 'John Cook' },
            { id: 2, text: 'Steve Jobs' },
            { id: 3, text: 'Peter Sanders' },
            { id: 4, text: 'Mark Newman' },
            { id: 5, text: 'Addy Osmani' },
            { id: 6, text: 'Paul Irish' },
            { id: 7, text: 'Doug Crocford' },
            { id: 8, text: 'Nicolas Cage' }
         */
    }

    __getFbsList() {
        return this.__fbsList;
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

    install(options = {}) {
        const self = this;
        const state = this.isShown();
        if (state) return;

        if (this.__installed) return;

        if (typeof options === "undefined" || typeof options.autoOpen === "undefined") {
            options.autoOpen = false;
        }

        this.__expandedPreferences = {};
        const geometry = this.__windowGeometry.recent();

        $("#" + this.__dialogName).dialog({
            height: geometry.height,
            width: geometry.width,
            left: geometry.left,
            top: geometry.top,
            autoOpen: options.autoOpen,
            resize: function (event, ui) {
                self.__resizePreferences();
            },
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
                const elGrid = w2ui[self.__gridName];
                elGrid.selectNone();
            }
        });

        if (!w2ui[this.__gridName]) {
            const targetGridEl = $('#' + this.__gridName);
            targetGridEl.w2grid({
                name: this.__gridName,
                header: "Blocks and Feedbacks",
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
                    multiSelect: false
                },
                textSearch: 'contains',
                //recid: '', // rename "recid" to "routeId"
                searches: [
                    { field: 'blockId', caption: 'Block', type: 'text' }
                ],
                sortData: [{ field: 'blockId', direction: 'asc' }],
                columns: [
                    { field: 'recid', caption: 'ID', sortable: false, hidden: true },
                    {
                        field: 'blockId', caption: 'Block', size: '25%', sortable: true
                    },
                    {
                        field: 'fbEnter', caption: 'Feedback Enter', size: '20%', sortable: true,
                        editable: {
                            type: 'list',
                            items: self.__getFbsList(),
                            //items: self.__fbsList,
                            filter: false
                        }
                    },
                    {
                        field: 'fbIn', caption: 'Feedback In', size: '20%', sortable: false,
                        editable: {
                            type: 'list',
                            items: self.__getFbsList(),
                            //items: self.__fbsList,
                            filter: false
                        }
                    }
                ],
                records: [],
                onExpand: function (event) {

                    // box_id := "grid_gridBlocks_rec_17_expanded"
                    // fbox_id := "grid_gridBlocks_frec_17_expanded"

                    const elGrid = w2ui[self.__gridName];

                    // collapse all other 
                    for (let i = 0; i < elGrid.records.length; ++i) {
                        const r = elGrid.records[i];
                        if (typeof r === "undefined" || r == null) continue;
                        if (r.recid === parseInt(event.recid)) continue;

                        elGrid.collapse(r.recid);
                    }

                    const rec = elGrid.get(event.recid);
                    const data = self.__getExpandedHtml(rec);
                    data.recid = event.recid;
                    data.blockId = rec.blockId;
                    data.fbEnterItemId = rec.fbEnter;
                    data.fbInItemId = rec.fbIn;
                    $('#' + event.box_id).html(data.renderHtml);

                    self.__expandedPreferences[event.recid] = true;

                    event.onComplete = function () {
                        const uiCtrl = $('#' + data.selectId);
                        if (typeof window.ecosData !== "undefined" && window.ecosData != null) {
                            const locomotives = window.ecosData.locomotives;
                            for (let i = 0; i < locomotives.length; ++i) {
                                const loc = locomotives[i];
                                const isDenied = self.__isLocDenied(data.blockId, loc.objectId);
                                const o = new Option(loc.name, loc.objectId, isDenied, isDenied);
                                $(o).css({ "padding": "1px" });
                                $(o).html(loc.name);
                                uiCtrl.append(o);
                            }
                        }

                        uiCtrl.select2();
                        uiCtrl.on('select2:unselect', function (e) {
                            self.__resizePreferences();
                            self.__saveBlockSettings(data);
                        });
                        uiCtrl.on('select2:select', function (e) {
                            self.__resizePreferences();
                            self.__saveBlockSettings(data);
                        });

                        const enterCtrl = $('#' + data.fbEnterId);
                        const inCtrl = $('#' + data.fbInId);

                        enterCtrl.w2field('int');
                        inCtrl.w2field('int');

                        const chkIds = data.checkboxIds;
                        let activateChkEvents = [];
                        for (let j = 0; j < chkIds.length; ++j) {
                            const chkCtrl = $('#' + chkIds[j]);
                            if (typeof chkCtrl === "undefined" || chkCtrl == null) continue;
                            chkCtrl.w2field('checkbox');
                            activateChkEvents.push(chkCtrl);
                        }

                        // apply css changes to render the content correct
                        $('div#blockData_' + event.recid + ' #' + data.selectId + ' + span').css({
                            "margin-left": "10px",
                        });
                        $('div#blockData_' + event.recid + ' #' + data.selectId + ' + span span.selection span.select2-selection').css({
                            "border-radius": "3px",
                            "border": "1px solid #cacaca"
                        });

                        self.__loadRecentStatesFor(data.blockId, event.recid);

                        enterCtrl.change(function () {
                            const fbEnterData = getThemeJsonDataById(data.fbEnterItemId);
                            fbEnterData.addresses.Addr = parseInt($(this).val());
                            setThemeJsonDataById(data.fbEnterItemId, fbEnterData);

                            self.__saveBlockSettings(data);
                        });
                        inCtrl.change(function () {
                            const fbInData = getThemeJsonDataById(data.fbInItemId);
                            fbInData.addresses.Addr = parseInt($(this).val());
                            setThemeJsonDataById(data.fbInItemId, fbInData);

                            self.__saveBlockSettings(data);
                        });
                        for (let i = 0; i < activateChkEvents.length; ++i) {
                            activateChkEvents[i].change(function () {
                                self.__saveBlockSettings(data);
                            });
                        }

                        self.__resizePreferences();
                    }
                },
                onCollapse: function (event) {
                    self.__expandedPreferences[event.recid] = false;
                },
                onSelect: function (ev) {
                    const elGrid = w2ui[self.__gridName];
                    const rec = elGrid.get(ev.recid);
                    self.__addBlockFeedbackHiglight(rec);
                    const planField = window.planField;
                    if (planField) {
                        // TBD
                    }
                },
                onUnselect: function (ev) {
                    self.__removeBlockFeedbackHiglight();
                },
                onChange: function(ev) {
                    const elGrid = w2ui[self.__gridName];
                    const rec = elGrid.get(ev.recid);
                    const column = ev.column;

                    if (column === 2 || column === 3) {

                        let fbEnterId = "null";
                        let fbInId = "null";

                        if (column === 2) {
                            rec.fbEnter = ev.value_new.text;
                            fbEnterId = rec.fbEnter;
                        }
                        else if (column === 3) {
                            rec.fbIn = ev.value_new.text;
                            fbInId = rec.fbIn;
                        }

                        // send the fb selection to the server
                        self.__saveBlockFbs(
                            rec.blockId,
                            fbEnterId,
                            fbInId
                        );

                        elGrid.refreshCell(rec.recid, 'fbEnter');
                        elGrid.refreshCell(rec.recid, 'fbIn');

                        elGrid.save();

                        setTimeout(function () {
                            self.__addBlockFeedbackHiglight(rec);
                        }, 250);
                    }
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
                    const h = $('div#blockData_' + recid).get(0).getBoundingClientRect().height;
                    $('div#grid_gridBlocks_frec_' + recid + '_expanded').css({
                        height: h + "px"
                    });
                }
                catch (err) {

                }
            }
        });
    }

    __saveBlockFbs(blockId, fbEnterId, fbInId) {
        this.__trigger('setting',
            {
                'mode': 'block',
                'cmd': 'blockDataFbs',
                'value': {
                    blockIdentifier: blockId,
                    fbEnterId: fbEnterId,
                    fbInId: fbInId
                }
            });
    }

    __saveBlockSettings(blockData) {
        const deniedLocs = $('#' + blockData.selectId).select2('data');

        let chkStates = {};
        if (blockData.checkboxIds == null) {
            chkStates = null;
        } else {
            const chkIds = blockData.checkboxIds;
            for (let j = 0; j < chkIds.length; ++j) {
                const chkCtrl = $('#' + chkIds[j]);
                if (typeof chkCtrl === "undefined" || chkCtrl == null)
                    continue;
                const chkId = $(chkCtrl).attr('name');
                const chkState = $(chkCtrl).is(":checked");
                chkStates[chkId] = chkState;
            }
        }

        this.__trigger('setting',
            {
                'mode': 'block',
                'cmd': 'blockData',
                'value': {
                    blockIdentifier: blockData.blockId,
                    fbEnter: {
                        id: blockData.fbEnterItemId,
                        ecosAddr: parseInt($('#' + blockData.fbEnterId).val())
                    },
                    fbIn: {
                        id: blockData.fbInItemId,
                        ecosAddr: parseInt($('#' + blockData.fbInId).val())
                    },
                    deniedLocomotives: deniedLocs,
                    checkboxSettings: chkStates
                }
            });
    }

    __getBlockOfRecentData(blockId) {
        for (let i = 0; i < this.__recentFeedbacksData.length; ++i) {
            var itData = this.__recentFeedbacksData[i];
            if (typeof itData === "undefined") continue;
            if (itData == null) continue;

            if (itData.BlockId === blockId)
                return itData;
        }
        return null;
    }

    __isLocDenied(blockId, oid) {
        const recentBlockData = this.__getBlockOfRecentData(blockId);

        if (recentBlockData != null) {
            var deniedLocomotives = recentBlockData.DeniedLocomotives;
            for (let i = 0; i < deniedLocomotives.length; ++i) {
                const itLoc = deniedLocomotives[i];
                if (typeof itLoc === "undefined") continue;
                if (itLoc == null) continue;
                if (parseInt(itLoc.Id) === oid)
                    return true;
            }
        }

        return false;
    }

    __loadRecentStatesFor(blockId, recid) {
        const recentBlockData = this.__getBlockOfRecentData(blockId);
        if (recentBlockData != null) {
            const ids = this.__getCtrlIdsForOptionsAndTypes(recid);
            $('#blockEnabled_' + recid).prop('checked', recentBlockData.Settings.BlockEnabled);

            $('#' + ids.chkIdWait).prop('checked', recentBlockData.Settings.OptionWait);
            $('#' + ids.chkIdDirection).prop('checked', recentBlockData.Settings.OptionDirection);
            $('#' + ids.chkIdMainline).prop('checked', recentBlockData.Settings.OptionMainline);
            $('#' + ids.chkIdBbt).prop('checked', recentBlockData.Settings.OptionBbt);

            $('#' + ids.chkIdOthers).prop('checked', recentBlockData.Settings.TypeOthers);
            $('#' + ids.chkIdLocal).prop('checked', recentBlockData.Settings.TypeLocal);
            $('#' + ids.chkIdIntercity).prop('checked', recentBlockData.Settings.TypeIntercity);
            $('#' + ids.chkIdFreight).prop('checked', recentBlockData.Settings.TypeFreight);
            $('#' + ids.chkIdShunting).prop('checked', recentBlockData.Settings.TypeShunting);
            $('#' + ids.chkIdRegional).prop('checked', recentBlockData.Settings.TypeRegional);
            $('#' + ids.chkIdBranchLine).prop('checked', recentBlockData.Settings.TypeBranchLine);
            $('#' + ids.chkIdBranchLineFreight).prop('checked', recentBlockData.Settings.TypeBranchLineFreight);
        }
    }

    __getExpandedHtml(rec) {
        const self = this;
        const recid = rec.recid;
        const blockDataId = "blockData_" + recid;
        const blockFbEnter = "blockFbEnter_" + recid;
        const blockFbIn = "blockFbAddr_" + recid;
        const selectId = "selectDeniedLocomotives_" + recid;

        const fbEnterData = getThemeJsonDataById(rec.fbEnter);
        const fbInData = getThemeJsonDataById(rec.fbIn);

        let html = '<div id="' + blockDataId + '" style="padding: 5px; width: 100%; float: left;">';

        let fbEnterAddr = -1;
        try {
            fbEnterAddr = fbEnterData.addresses.Addr;
        } catch (err) {
            // ignore
        }

        let fbInAddr = -1;
        try {
            fbInAddr = fbInData.addresses.Addr;
        } catch (err) {
            // ignore
        }

        // Feedback Address Information
        html += '<div class="w2ui-field">';
        html += '<label>Feedback:</label>';
        html += '<div>Enter <input id="' + blockFbEnter + '" value="' + fbEnterAddr + '" style="width: 75px;">' +
            ' In <input id="' + blockFbIn + '" value="' + fbInAddr + '" style="width: 75px;"></div>';
        html += '</div>';

        html += '<div class="w2ui-field">';
        html += '<label>Denied Locs.:</label>';
        html += '<select name="selectDeniedLocomotives[]" id="' + selectId + '" multiple="multiple" style="width: 250px;">';
        html += '</select>';
        html += '</div>';

        const chkBlockEnabledId = 'blockEnabled_' + recid;
        html += self.__getCheckboxHtml('Block Enabled:', chkBlockEnabledId, 'BlockEnabled', true);

        const dataOptions = self.__getCheckboxOptions('Options:', recid);
        const dataTypes = self.__getCheckboxTypes('Types:', recid);

        html += dataOptions.html;
        html += dataTypes.html;

        html += '</div>';

        let chkIds = [];
        chkIds.push(chkBlockEnabledId);
        chkIds = chkIds.concat(dataOptions.checkboxIds);
        chkIds = chkIds.concat(dataTypes.checkboxIds);

        return {
            renderId: blockDataId,
            fbEnterId: blockFbEnter,
            fbInId: blockFbIn,
            selectId: selectId,
            checkboxIds: chkIds,
            renderHtml: html
        };
    }

    __getCtrlIdsForOptionsAndTypes(recid) {
        return {
            chkIdWait: 'blockChkWait_' + recid,
            chkIdDirection: 'blockChkDirection_' + recid,
            chkIdMainline: 'blockChkMainline_' + recid,
            chkIdBbt: 'blockChkBbt_' + recid,

            chkIdOthers: 'blockChkTypeOthers_' + recid,
            chkIdLocal: 'blockChkTypeLocal_' + recid,
            chkIdIntercity: 'blockChkTypeIntercity_' + recid,
            chkIdFreight: 'blockChkTypeFreight_' + recid,
            chkIdShunting: 'blockChkTypeShunting_' + recid,
            chkIdRegional: 'blockChkTypeRegional_' + recid,
            chkIdBranchLine: 'blockChkTypeBranchLine_' + recid,
            chkIdBranchLineFreight: 'blockChkTypeBranchLineFreight_' + recid
        };
    }

    __getCheckboxOptions(lblTxt, recid) {
        let html = '';
        html += '<div class="w2ui-field">';
        html += '<label>' + lblTxt + '</label>';
        html += '<div style="padding-top: 7px;">';

        const cssLabel = 'style="word-wrap:break-word; font-weight: normal; margin-right: 10px;"';
        const cssInput = 'style="vertical-align: middle;"';

        var ids = this.__getCtrlIdsForOptionsAndTypes(recid);

        /**
         * currently disabled "Wait" and "Mainline" -- not really supported in the moment 
         */
        //html += '<label ' + cssLabel + '><input name="OptionWait" id="' + ids.chkIdWait + '" type="checkbox" ' + cssInput + '> Wait </label>';
        //html += '<label ' + cssLabel + '><input name="OptionMainline" id="' + ids.chkIdMainline + '" type="checkbox" ' + cssInput + '> Mainline </label>';

        html += '<label ' + cssLabel + '><input name="OptionDirection" id="' + ids.chkIdDirection + '" type="checkbox" ' + cssInput + '> Allow change direction </label>';
        html += '<label ' + cssLabel + '><input name="OptionBbt" id="' + ids.chkIdBbt + '" type="checkbox" ' + cssInput + '> Speed Curve </label>';

        html += '</div>';
        html += '</div>';
        return {
            html: html,
            checkboxIds: [
                ids.chkIdWait,
                ids.chkIdDirection,
                ids.chkIdMainline,
                ids.chkIdBbt
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

        var ids = this.__getCtrlIdsForOptionsAndTypes(recid);

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

    __getCheckboxHtml(lblTxt, chkId, name, checked = false) {
        let htmlChecked = '';
        if (checked === true) htmlChecked = 'checked';
        let html = '';
        html += '<div class="w2ui-field">';
        html += '<label>' + lblTxt + '</label>';
        html += '<div style="padding-top: 7px;"><input name="' + name + '" id="' + chkId + '" type="checkbox" ' + htmlChecked + '></div>';
        html += '</div>';
        return html;
    }

    __addBlockFeedbackHiglight(rec) {
        const planField = window.planField;
        if (!planField) {
            return;
        }

        toggleAllLocomotiveInformation(false);

        var blockId = rec.blockId;
        var fbEnter = rec.fbEnter;
        var fbIn = rec.fbIn;

        const ctrlsFeedback = $('div.ctrlItemFeedback');
        ctrlsFeedback.each(function () {
            const id = $(this).attr('id');
            if (id === fbEnter || id === fbIn) {
                $(this).addClass('highlightFeedback');
            }
        });

        const ctrlsBlocks = $('div.ctrlItemBlock');
        ctrlsBlocks.each(function () {
            const id = $(this).attr('id');
            if (id === blockId) {
                $(this).addClass('highlightFeedbackBlock');
                $(this).find('img').css({
                    opacity: 0.0
                });
            }
        });
    }

    __removeBlockFeedbackHiglight() {
        const planField = window.planField;
        if (!planField) {
            return;
        }

        toggleAllLocomotiveInformation(true);

        const ctrlsFeedback = $('div.ctrlItemFeedback');
        ctrlsFeedback.each(function () {
            $(this).removeClass('highlightFeedback');
        });

        const ctrlsBlocks = $('div.ctrlItemBlock');
        ctrlsBlocks.each(function () {
            $(this).removeClass('highlightFeedbackBlock');
            $(this).find('img').css({
                opacity: 1.0
            });
        });
    }

    updateBlocks(blocks) {
        const self = this;
        const elGrid = w2ui[self.__gridName];

        console.log(blocks);
    }

    /**
     * 
     * @param {any} feedbacks -- data of fbevents.json
     */
    updateFeedbacks(feedbacks) {
        const self = this;
        const elGrid = w2ui[self.__gridName];
        const data = feedbacks.data;

        self.__recentFeedbacksData = data;

        const listOfObjectsToAdd = [];
        let noOfRecords = 0;

        //
        // update grid
        //
        let i;
        const iMax = data.length;
        for (i = 0; i < iMax; ++i) {
            const fb = data[i];
            if (typeof fb === "undefined" || fb == null) continue;
            const recs = elGrid.find({ blockId: fb.BlockId });
            if (recs.length === 0) {
                // add new block row
                //const noOfRecords = elGrid.records.length;
                listOfObjectsToAdd.push({
                    recid: noOfRecords + 1,
                    blockId: fb.BlockId,
                    fbEnter: fb.FbEnter,
                    fbIn: fb.FbIn
                });
                ++noOfRecords;
            }
        }

        if (listOfObjectsToAdd.length > 0)
            elGrid.add(listOfObjectsToAdd);

        // TODO add logic to remove vanished blocks/feedbacks
    }

    controlCreated(ctrlInstance) {
        const self = this;
        try {
            const themeItemData = ctrlInstance.data(constDataThemeItemObject);
            const isFb = isFeedback(themeItemData.editor.themeId);
            if (isFb === false) return;
            const fbId = ctrlInstance.attr("id");
            if (self.__fbsList.includes(fbId) === false)
                self.__fbsList.push(fbId);

            // human readable sort
            const collator = new Intl.Collator(undefined, {numeric: true, sensitivity: 'base'});
            self.__fbsList = self.__fbsList.sort(collator.compare);

            self.__refreshFbCells();
        } catch (err) {
            console.log(err);
        }
    }

    controlRemoved(ctrlIdentifier) {
        const self = this;
        const iMax = self.__fbsList.length;
        for( let i = 0; i < iMax; i++){
            if ( self.__fbsList[i] === ctrlIdentifier) {
                self.__fbsList.splice(i, 1); 
                break;
            }
        }
        self.__refreshFbCells();
    }

    __refreshFbCells() {
        const self = this;
        const elGrid = w2ui[self.__gridName];
        elGrid.columns[2].editable.items = self.__fbsList;
        elGrid.columns[3].editable.items = self.__fbsList;
    }

    clearGrid() {
        const self = this;
        const elGrid = w2ui[self.__gridName];
        self.__recentFeedbacksData = [];
        elGrid.clear(true);
        elGrid.reset();
    }
}