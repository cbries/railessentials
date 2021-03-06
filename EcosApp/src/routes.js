class Routes {
    constructor() {
        console.log("**** construct Routes");

        this.__installed = false;

        this.__gridName = "gridRoutes";
        this.__dialogName = "dialogRoutes";
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
        const planField = window.planField;
        if (planField) {
            planField.clearRouteUserHighlight();
        }
    }

    install(options = {}) {
        const self = this;
        const state = this.isShown();
        if (state) return;

        if (typeof options.autoOpen === "undefined")
            options.autoOpen = false;

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
            },
            close: function (event, ui) {
                self.__removeAllHighlights();

                const elGrid = w2ui[self.__gridName];
                elGrid.selectNone();
            }
        });

        if (!w2ui[this.__gridName]) {
            const targetGridEl = $('#' + this.__gridName);
            targetGridEl.w2grid({
                name: this.__gridName,
                header: "Routes",
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
                recid: 'routeId', // rename "recid" to "routeId"
                searches: [
                    { field: 'name', caption: 'Name', type: 'text' }
                ],
                sortData: [{ field: 'routeId', direction: 'asc' }],
                columns: [
                    { field: 'routeId', caption: 'Route ID', sortable: false, hidden: true },
                    { field: 'native', caption: 'Native', sortable: true, hidden: true },
                    { field: 'name', caption: 'Name', size: '20%', sortable: true },
                    {
                        field: 'isDisabled',
                        caption: 'Disabled',
                        size: '10%',
                        style: 'text-align: center',
                        editable: {
                            type: 'checkbox',
                            style: 'text-align: center'
                        }
                    },
                    { field: 'switches', caption: 'Switches', size: '10%', sortable: false },
                    { field: 'sensors', caption: 'Sensors', size: '10%', sortable: false },
                    { field: 'signals', caption: 'Signals', size: '10%', sortable: false },
                    { field: 'tracks', caption: 'Tracks', size: '10%', sortable: false },
                ],
                records: [],
                onSelect: function (ev) {
                    const elGrid = w2ui[self.__gridName];
                    const rec = elGrid.get(ev.recid);
                    const planField = window.planField;
                    if (planField) {
                        planField.clearRouteUserHighlight();
                        planField.activateRouteUserHighlight(rec.native);
                    }
                },
                onUnselect: function (ev) {
                    self.__removeAllHighlights();
                }
            });

            const gridTb = w2ui[self.__gridName].toolbar;
            if (gridTb) {

                //
                // button to test route configuration
                //
                gridTb.insert('search', {
                    type: 'button',
                    id: 'itemCheckRoute',
                    text: 'Check',
                    img: 'fas fa-check',
                    checked: false,
                    tooltip: function (item) {
                        return 'Simulates the route by changing any relevant accessory to let the trains reach its destination.';
                    },
                    onClick: function (ev) {
                        const elGrid = w2ui[self.__gridName];
                        const sel = elGrid.getSelection();
                        for (let i = 0; i < sel.length; ++i) {
                            const recid = sel[i];
                            const rec = elGrid.get(recid);
                            self.__trigger("checkRoute",
                                {
                                    mode: 'checkRoute',
                                    routeName: rec.name,
                                    native: rec.native
                                });
                        }
                    }
                });

                //
                // button to save current changes
                //
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

                            const isDisabled = change.isDisabled;
                            if (typeof isDisabled !== "undefined" && isDisabled != null) {
                                row.isDisabled = isDisabled;
                                elGrid.refreshCell(row, 'isDisabled');
                                self.__trigger("setting",
                                    {
                                        "mode": "route",
                                        "cmd": "disable",
                                        "value": {
                                            "routeName": row.name,
                                            "disableState": isDisabled
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
    }

    __cleanupChangedState() {
        const self = this;
        $('#' + this.__dialogName + ' td.w2ui-grid-data').each(function () {
            $(this).removeClass('w2ui-changed');
        });
    }

    updateRoutes(routes) {
        const self = this;

        if (typeof routes === "undefined" || routes == null)
            return;

        const elGrid = w2ui[self.__gridName];

        let listOfObjectsToAdd = [];

        let routeId;
        const iMax = routes.length;
        for (routeId = 0; routeId < iMax; ++routeId) {
            const recs = elGrid.find({ routeId: routeId });
            const route = routes[routeId];
            const name = route.name;
            let isDisabled = route.isDisabled;

            if (typeof isDisabled === "undefined" || isDisabled == null)
                isDisabled = false;

            if (recs.length <= 0) {
                listOfObjectsToAdd.push({
                    routeId: routeId,
                    native: route,
                    name: name,
                    isDisabled: isDisabled,
                    switches: route.switches.length,
                    sensors: route.sensors.length,
                    signals: route.signals.length,
                    tracks: route.tracks.length
                });
            }
            else {
                //
                // update available entry
                //
                const recid = recs[0];
                const row = elGrid.get(recid);

                if (row.isDisabled !== isDisabled) {
                    row.isDisabled = isDisabled;
                    elGrid.refreshCell(routeId, 'isDisabled');
                }
            }
        }

        if (listOfObjectsToAdd.length > 0)
            elGrid.add(listOfObjectsToAdd);
    }
}