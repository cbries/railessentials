class Accessories {
    constructor() {
        console.log("**** construct Accessories");

        this.__installed = false;

        this.__gridName = "gridAccessories";
        this.__dialogName = "dialogAccessories";
        this.__storageNameGeometry = this.__dialogName + "_geometry";

        this.__windowGeometry = new WindowGeometryStorage(this.__dialogName);

        this.__initEventHandling();

        this.__recentlyHighlighted = {};

        if (typeof window.__toggleAccessoryTest === "undefined" || window.__toggleAccessoryTest == null) {
            window.__toggleAccessoryTest = function(accessoryAddress) {
                window.__eventTrigger('startAccessoryTest',
                    {
                        cmd: "accessoryTest",
                        addresses: JSON.parse(atob(accessoryAddress)),
                        periods: 10, // period of test cycles, TODO, make it editable
                        pause: 1000 // milliseoncds, TODO, make it editable
                    });
            }
        }
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

    __removeAllHighlights() {
        const self = this;
        const keys = Object.keys(self.__recentlyHighlighted);
        $(keys).each(function () {
            window.planField.highlightAccessory(this, false);
        });
        self.__recentlyHighlighted = {};
    }
    
    __renderTestFunctionality(record) {
        const oid = record.recid;

        const addrInfo = {
            addr1: record.addr1,
            addr2: record.addr2,
            port1: record.port1,
            port2: record.port2,
            inverse1: record.inverse1,
            inverse2: record.inverse2
        };

        let json = JSON.stringify(addrInfo);
        json = btoa(json);

        const innerHtml = '<div style="height: 100%; width: 100%; white-space: nowrap; text-align: center; padding-top: 3px;">' +
            '<span style="display: inline-block; height: 100%; vertical-align: middle;">'
            + '<input type="button" value="Test" id="cmdAccessoryTest_' + oid + '" onclick="window.__toggleAccessoryTest(\'' + json + '\')">'
            + '</span>'
            + '</div>';

        return innerHtml;
    }

    install(options = {}) {
        const self = this;
        const state = this.isShown();
        if (state) return;

        if (typeof options === "undefined" || typeof options.autoOpen === "undefined") {
            if (options == null) options = {};
            options.autoOpen = false;
        }

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
            },
            close: function () {
                self.__removeAllHighlights();
                const elGrid = w2ui[self.__gridName];
                elGrid.selectNone();
            }
        });

        if (!w2ui[this.__gridName]) {
            const targetGridEl = $('#' + this.__gridName);
            targetGridEl.w2grid({
                name: this.__gridName,
                header: "Accessories",
                show: {
                    //lineNumbers: true,
                    toolbar: true,
                    //header: true,
                    footer: true,
                    toolbarAdd: false,
                    toolbarDelete: false,
                    toolbarEdit: false,
                    toolbarSave: false
                },
                textSearch: 'contains',
                recid: 'accessoryId', // rename "recid" to "accessoryId"
                searches: [
                    { field: 'identifier', caption: 'Identifier', type: 'text' }
                ],
                sortData: [{ field: 'identifier', direction: 'asc' }],
                columns: [
                    { field: 'accessoryId', caption: 'Accessory ID', size: '2%', sortable: false, hidden: true },
                    { field: 'identifier', caption: 'Identifier', size: '7%', sortable: true },
                    { field: 'type', caption: 'Type', size: '10%', sortable: true },
                    { field: 'state', caption: 'State', size: '5%', sortable: true },

                    { field: 'addr1', caption: 'Address1', tooltip: 'Address1', size: '5%', sortable: true, hidden: true, render: 'int', editable: { type: 'int', min: 0, max: 32756 } },
                    { field: 'port1', caption: 'Port1', tooltip: 'Port1', size: '5%', sortable: true, hidden: true, render: 'int', editable: { type: 'int', min: 0, max: 32756 } },
                    {
                        field: 'inverse1',
                        caption: 'Inverse1',
                        tooltip: 'Inverse1',
                        size: '5%',
                        sortable: false,
                        hidden: true,
                        editable: {
                            type: 'checkbox',
                            style: 'text-align: center'
                        }
                    },

                    { field: 'addr2', caption: 'Address2', tooltip: 'Address2', size: '5%', sortable: true, hidden: true, render: 'int', editable: { type: 'int', min: 0, max: 32756 } },
                    { field: 'port2', caption: 'Port2', tooltip: 'Port2', size: '5%', sortable: true, hidden: true, render: 'int', editable: { type: 'int', min: 0, max: 32756 } },
                    {
                        field: 'inverse2',
                        caption: 'Inverse2',
                        tooltip: 'Inverse2',
                        size: '5%',
                        sortable: false,
                        hidden: true,
                        editable: {
                            type: 'checkbox',
                            style: 'text-align: center'
                        }
                    },
                    {
                        field: 'groupName',
                        caption: "Group",
                        tooltip: 'In a group with the same name, only one accessory can be enabled (e.g. "green", "straight", "on").',
                        size: '5%',
                        sortable: false,
                        hidden: false,
                        editable: { type: 'string' }
                    },
                    {
                        field: 'longTermSwitching',
                        caption: 'Test',
                        tooltip: 'Toggles long-term switching test, i.e. the accessory will be switched continously.',
                        size: '5%',
                        sortable: false,
                        hidden: true,
                        editable: false,
                        style: 'text-align: center',
                        render: self.__renderTestFunctionality
                    }
                ],
                records: [],
                onSelect: function (ev) {
                    const newRecid = ev.recid;
                    const elGrid = w2ui[self.__gridName];
                    const newRec = elGrid.get(newRecid);

                    self.__removeAllHighlights();

                    window.planField.highlightAccessory(newRec.identifier, true);
                    self.__recentlyHighlighted[newRec.identifier] = true;

                    const sel = elGrid.getSelection();
                    let i;
                    const iMax = sel.length;
                    for (i = 0; i < iMax; ++i) {
                        const recid = sel[i];
                        const rec = elGrid.get(recid);
                        window.planField.highlightAccessory(rec.identifier, true);
                        self.__recentlyHighlighted[rec.identifier] = true;
                    }

                    bringToFront(self.__dialogName);
                },
                onUnselect: function (ev) {
                    const recid = ev.recid;
                    const elGrid = w2ui[self.__gridName];
                    const rec = elGrid.get(recid);
                    if (typeof rec === "undefined" || rec == null)
                        return;
                    window.planField.highlightAccessory(rec.identifier, false);
                    self.__recentlyHighlighted[rec.identifier] = false;
                }
            });

            const gridTb = w2ui[this.__gridName].toolbar;
            if (gridTb) {

                // toggle button to show/hide columns
                gridTb.insert('search', {
                    type: 'check',
                    id: 'itemAddressing',
                    text: 'Address/Port',
                    img: 'fas fa-sitemap',
                    checked: false,
                    tooltip: function () {
                        return 'Toogles the columns to set-up the accessory addressing.';
                    },
                    onClick: function () {
                        const elGrid = w2ui[self.__gridName];

                        elGrid.toggleColumn('addr1');
                        elGrid.toggleColumn('port1');
                        elGrid.toggleColumn('inverse1');

                        elGrid.toggleColumn('addr2');
                        elGrid.toggleColumn('port2');
                        elGrid.toggleColumn('inverse2');

                        elGrid.toggleColumn('longTermSwitching');
                    }
                });

                // button to switching the accessory state
                gridTb.insert('search',
                    {
                        type: 'button',
                        id: 'itemExecute',
                        text: 'Execute',
                        img: 'fas fa-exchange-alt',
                        tooltip: function () {
                            return 'Switches the state of the current selected accessories.';
                        },
                        onClick: function () {
                            const elGrid = w2ui[self.__gridName];
                            const sel = elGrid.getSelection();
                            let j;
                            const jMax = sel.length;
                            for (j = 0; j < jMax; ++j) {
                                const recid = sel[j];
                                const rec = elGrid.get(recid);

                                // get accessory coord
                                const ctrls = $('div.ctrlItemAccessory[id]');
                                let jj;
                                const jjMax = ctrls.length;
                                for (jj = 0; jj < jjMax; ++jj) {
                                    const ctrl = ctrls[jj];
                                    if (!ctrl) continue;
                                    const jsonData = $(ctrl).data(constDataThemeItemObject);
                                    if (jsonData == null) continue;
                                    if (jsonData.identifier === rec.identifier) {

                                        const coord = jsonData.coord;

                                        self.__trigger("accessoryExecute",
                                            {
                                                "ctrlId": rec.identifier,
                                                "coord": coord
                                            });

                                        break;
                                    }
                                }
                            }
                        }
                    });

                // button to save any change
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
                        const iMax = changes.length;
                        for (let i = 0; i < iMax; ++i) {
                            const change = changes[i];

                            const recid = change.recid;
                            const row = elGrid.get(recid);

                            let inverse1Changed = false;
                            let inverse2Changed = false;

                            if (typeof change.inverse1 !== "undefined" && change.inverse1 != null)
                                inverse1Changed = change.inverse1 != row.inverse1;
                            if (typeof change.inverse2 !== "undefined" && change.inverse2 != null)
                                inverse2Changed = change.inverse2 !== row.inverse2;

                            let newData = {
                                "Addr1": change.addr1,
                                "Port1": change.port1,
                                "Inverse1": change.inverse1,

                                "Addr2": change.addr2,
                                "Port2": change.port2,
                                "Inverse2": change.inverse2
                            };

                            if (typeof newData.Addr1 === "undefined" || newData.Addr1 == null)
                                newData.Addr1 = row.addr1;
                            if (typeof newData.Port1 === "undefined" || newData.Port1 == null)
                                newData.Port1 = row.port1;
                            if (typeof newData.Inverse1 === "undefined" || newData.Inverse1 == null)
                                newData.Inverse1 = row.inverse1;

                            if (typeof newData.Addr2 === "undefined" || newData.Addr2 == null)
                                newData.Addr2 = row.addr2;
                            if (typeof newData.Port2 === "undefined" || newData.Port2 == null)
                                newData.Port2 = row.port2;
                            if (typeof newData.Inverse2 === "undefined" || newData.Inverse2 == null)
                                newData.Inverse2 = row.inverse2;

                            if (inverse1Changed === true) {
                                row.inverse1 = change.inverse1;
                                elGrid.refreshCell(row, 'inverse1');
                            }
                            if (inverse2Changed === true) {
                                row.inverse2 = change.inverse2;
                                elGrid.refreshCell(row, 'inverse2');
                            }

                            // apply the new address values to the planfield item
                            // REMARK it will not be applied when the server responses
                            // because of the fact that we just believe everything works
                            // smoothly without any interuption (and by the way, we do not
                            // have enough time to make it more robust -- right now).
                            window.planField.applyAddressToAccessory(row.identifier, newData);

                            // see server code, must be set to query accessory
                            newData.identifier = row.identifier;

                            // update groupName if changed
                            if(row.groupName !== change.groupName) {
                                row.groupName = change.groupName;
                                elGrid.refreshCell(row, 'groupName');
                                self.__trigger("setting",
                                    {
                                        "mode": "accessory",
                                        "cmd": "groupName",
                                        "value": {
                                            "identifier": row.identifier,
                                            "name": row.groupName
                                        }
                                    });
                            }

                            self.__trigger("setting",
                                {
                                    "mode": "accessory",
                                    "cmd": "address",
                                    "value": newData
                                });
                        }

                        elGrid.save();

                        //self.__cleanupChangedState();
                    }
                });
            }
        }

        this.__installed = true;
    }
    
    removeAccessory(identifier) {
        if (typeof identifier === "undefined") return;
        if (identifier == null) return;

        const self = this;
        const elGrid = w2ui[self.__gridName];
        const recs = elGrid.find({ identifier: identifier });
        if (recs.length > 0) {
            let j;
            const jMax = recs.length;
            for (j = 0; j < jMax; ++j) {
                const recid = recs[j];
                //const row = elGrid.get(recid);
                elGrid.remove(recid);
            }
        }
    }

    updateAccessories(model) {
        const self = this;

        const metamodel = model.metamodel;
        if (typeof metamodel === "undefined" || metamodel == null) return;
        const planField = metamodel.planField;
        const elGrid = w2ui[self.__gridName];

        let listOfObjectsToAdd = [];

        let i = 0;
        const keys = Object.keys(planField);
        const noOfKeys = keys.length;
        for (let planIdx = 0; planIdx < noOfKeys; ++planIdx) {
            const key = keys[planIdx];
            const accessory = planField[key];

            const themeId = accessory.editor.themeId;
            if (!isSwitchOrAccessory(themeId))
                continue;

            const accessoryId = i;
            ++i;

            const identifier = accessory.identifier;
            const type = accessory.name;
            const groupName = accessory.groupName;

            let addr1 = -1;
            let port1 = -1;
            let inverse1 = false;
            let addr2 = -1;
            let port2 = -1;
            let inverse2 = false;

            const addresses = accessory.addresses;
            if (typeof addresses === "undefined" || addresses == null) {
                // ignore
            } else {
                addr1 = addresses.Addr1;
                port1 = addresses.Port1;
                inverse1 = addresses.Inverse1;
                addr2 = addresses.Addr2;
                port2 = addresses.Port2;
                inverse2 = addresses.Inverse2;
            }

            const rec = elGrid.find({ accessoryId: accessoryId });
            if (rec.length <= 0) {
                listOfObjectsToAdd.push(
                    {
                        accessoryId: accessoryId,
                        identifier: identifier,
                        type: type,
                        groupName: groupName,
                        state: "unknown",

                        addr1: addr1,
                        port1: port1,
                        inverse1: inverse1,

                        addr2: addr2,
                        port2: port2,
                        inverse2: inverse2
                    });
            }
        }

        if (listOfObjectsToAdd.length > 0)
            elGrid.add(listOfObjectsToAdd);
    }

    updateStates(model) {
        const self = this;

        const ecosAccessories = model.ecosData.accessories;
        if (typeof ecosAccessories === "undefined" || ecosAccessories == null) return;

        const accCtrls = $('div.ctrlItemAccessory');
        const elGrid = w2ui[self.__gridName];

        const getStateOf = function (ecosAddr) {
            const noOfAccs = ecosAccessories.length;
            for (let i = 0; i < noOfAccs; ++i) {
                const ecosAcc = ecosAccessories[i];
                if (typeof ecosAcc === "undefined") continue;
                if (ecosAcc == null) continue;
                if (ecosAcc.addr === ecosAddr)
                    return ecosAcc.state;
            }
            return "unknown";
        }

        const noOfAccs = ecosAccessories.length;
        for (let i = 0; i < noOfAccs; ++i) {
            const ecosAcc = ecosAccessories[i];
            if (typeof ecosAcc === "undefined") continue;
            if (ecosAcc == null) continue;

            const accItem = getAccByEcosAddr(accCtrls, ecosAcc.addr);
            if (accItem == null) continue;

            const recs = elGrid.find({ identifier: accItem.themeData.identifier });
            if (recs.length > 0) {
                let j;
                const jMax = recs.length;
                for (j = 0; j < jMax; ++j) {
                    const rec = recs[j];
                    const row = elGrid.get(rec);
                    
                    let state0 = null; let newState0 = null;
                    let state1 = null; let newState1 = null;
                    if (accItem.ecosAddresses.length === 2) {
                        let onName = "straight";
                        let offName = "turn";
                        const themeId = accItem.themeData.editor.themeId;

                        if (isDecoupler(themeId) || isButton(themeId)) {
                            onName = "on";
                            offName = "off";
                        } else if (isSignal(themeId)) {
                            onName = "red";
                            offName = "green";
                        } else if (isAccessory(themeId)) {
                            onName = "on";
                            offName = "off";
                        }

                        if (accItem.ecosAddresses[0].ecosAddrValid === true) {
                            state0 = getStateOf(accItem.ecosAddresses[0].ecosAddr);
                            if (state0 === 0) newState0 = onName;
                            else if (state0 === 1) newState0 = offName;
                        }
                        if (accItem.ecosAddresses[1].ecosAddrValid === true) {
                            state1 = getStateOf(accItem.ecosAddresses[1].ecosAddr);
                            if (state1 === 0) newState1 = onName;
                            else if (state1 === 1) newState1 = offName;
                        }

                        let newState = "unknown";
                        if (newState0 == null && newState1 == null) {
                            // ignore
                        } else if (newState0 != null && newState1 != null) {
                            newState = newState0 + " / " + newState1;
                        } else if (newState0 == null) {
                            newState = newState1;
                        } else if (newState1 == null) {
                            newState = newState0;
                        }

                        const oldState = row.state;

                        if (oldState !== newState) {
                            row.state = newState;
                            elGrid.refreshCell(rec, 'state');
                        }
                    }
                }
            }
        }
    }
}