

if (!window.__trainEvents) {
    window.__trainEvents = new Events();
    window.__eventOn = function (eventName, callback) {
        this.__trainEvents.on(eventName, callback);
    };
    window.__eventTrigger = function (eventName, dataObject) {
        this.__trainEvents.triggerHandler(eventName, {
            sender: this,
            data: dataObject
        });
    }
}

window.occ = null;
window.blocks = null;
window.ecosData = null; // the native data of the ECoS
window.serverHandling = null;
window.editState = false;
window.routesDlg = null;
window.accessoriesDlg = null;
window.locomotivesDlg = null;
window.locomotiveCtrlDlgs = [];
window.textfieldElementInstances = []; // all text elements in the plan
window.planField = null;
window.toolbox = null;
window.errorHandler = null;
window.labelShown = false;

window.findLocomotivesDlg = (function (id) {
    if (!id) return null;
    var len = window.locomotiveCtrlDlgs.length;
    for (var i = 0; i < len; ++i) {
        var instance = window.locomotiveCtrlDlgs[i];
        if (!instance) continue;
        if (instance.__dlgId === id)
            return instance;
    }
    return null;
});

// changes the state of a locomotive function
function changeLocomotive(mode, data = {}) {
    var srv = window.serverHandling;
    if (typeof srv === "undefined" || srv == null)
        return; // TODO show error

    // mode := speedstep
    // data.oid : int
    // data.speedstep : int | ["Level0", "Level1", "Level2", "Level3", "Level4"]
    // data.timestamp : long

    // mode := function
    // data.oid : int
    // data.fncIdx : 0
    // data.timestamp : long

    // mode := direction
    // data.oid : int

    data.mode = mode;

    srv.sendCommand({
        "command": "locomotive",
        "timestamp": Date.now(),
        "cmddata": data
    });
}

// inside the accessory grid a row is selected and the button "Execute" is triggered
function accessoryExecute(data) {
    var srv = window.serverHandling;
    if (typeof srv === "undefined" || srv == null)
        return; // TODO show error

    // data.ctrlId : string
    // data.coord.x : int
    // data.coord.y : int

    srv.sendCommand({
        "command": "accessory",
        "timestamp": Date.now(),
        "cmddata": data
    });
}

function accessoryTest(data) {
    var srv = window.serverHandling;
    if (typeof srv === "undefined" || srv == null)
        return; // TODO show error

    srv.sendCommand({
        "command": "accessory",
        "timestamp": Date.now(),
        "cmddata": data
    });
}

// an item in the plan is clicked
function itemClicked(data) {
    var srv = window.serverHandling;
    if (typeof srv === "undefined" || srv == null)
        return; // TODO show error

    // data.ctrlId : string
    // data.coord.x : int
    // data.coord.y : int

    srv.sendCommand({
        "command": "accessory",
        "timestamp": Date.now(),
        "cmddata": data
    });
}

function assignLocomotiveToBlock(data) {
    var srv = window.serverHandling;
    if (typeof srv === "undefined" || srv == null)
        return; // TODO show error

    // data.mode : 'assignToBlock'
    // data.oid : int
    // data.coord : { x: int, y: int }

    srv.sendCommand({
        "command": "routing",
        "timestamp": Date.now(),
        "cmddata": data
    });
}

function resetAssignment(data) {
    var srv = window.serverHandling;
    if (typeof srv === "undefined" || srv == null)
        return; // TODO show error

    // data.mode : 'resetAssignment'
    // data.oid : int

    srv.sendCommand({
        "command": "routing",
        "timestamp": Date.now(),
        "cmddata": data
    });
}

// a locomotive is dragged&dropped from a block to a second block
function gotoBlock(data) {
    var srv = window.serverHandling;
    if (typeof srv === "undefined" || srv == null)
        return; // TODO show error

    // data.mode : 'gotoBlock'
    // data.oid : int
    // data.fromBlock : string
    // data.toBlock : string

    srv.sendCommand({
        "command": "routing",
        "timestamp": Date.now(),
        "cmddata": data
    });
}

// callback handler to check the route set-up
function checkRoute(data) {
    var srv = window.serverHandling;
    if (typeof srv === "undefined" || srv == null)
        return; // TODO show error

    // data.mode : 'checkRoute'
    // data.routeName := string
    // data.native := the original/native information of the route which are stored in a metamodel-route

    srv.sendCommand({
        "command": "routing",
        "timestamp": Date.now(),
        "cmddata": data
    });
}

// callback handler to apply any new setting to any ecositem
function changeSetting(data) {
    var srv = window.serverHandling;
    if (typeof srv === "undefined" || srv == null)
        return; // TODO show error

    // data.mode := string ['locomotive', 'ecos', 'block', 'accessory', ...]
    // data.cmd := string (This is a data.mode specific command, e.g. "rename" for a locomotive.)
    // data.value := any (This is a data.cmd specific value.)

    srv.sendCommand({
        "command": "setting",
        "timestamp": Date.now(),
        "cmddata": data
    });
}

function relayWebsocketCommand(data) {
    var srv = window.serverHandling;
    if (typeof srv === "undefined" || srv == null)
        return; // TODO show error

    // data.mode := string ['websocket', ...]
    // data.target := string ['ws://host:port', ...]
    // data.contentType := string ['application/json', ...]
    // data.data := object, binary, etc.

    srv.sendCommand({
        "command": "relayCommand",
        "timestamp": Date.now(),
        "cmddata": data
    });
}

function removeTextfieldFromGlobalList(uniqueId) {
    if (uniqueId == null)
        return;
    var idxToRemove = -1;
    for (var i = 0; i < window.textfieldElementInstances.length; ++i) {
        var instance = window.textfieldElementInstances[i];
        if (instance.__uniqueId === uniqueId) {
            idxToRemove = i;
            break;
        }
    }
    if (idxToRemove !== -1) {
        window.textfieldElementInstances.splice(idxToRemove, 1);
    }
}

function removeLocomotiveDialogFromGlobalList(dlgId) {
    var idsToRemove = [];
    for (var j = 0; j < window.locomotiveCtrlDlgs.length; ++j) {
        var dlgInstance = window.locomotiveCtrlDlgs[j];
        if (dlgInstance == null) {
            idsToRemove.push(j);
            continue;
        }
        try {
            if (dlgInstance.__dlgId === dlgId)
                idsToRemove.push(j);
        }
        catch (ev) {
            // ignore
        }
    }
    const reversed = idsToRemove.reverse();
    for (var j = 0; j < reversed.length; ++j)
        window.locomotiveCtrlDlgs.splice(reversed[j], 1);
}

function openLocomotiveControlDialogByOid(oid) {
    const gridRecordRow = window.locomotivesDlg.getLocomotiveRecord(oid);
    openLocomotiveControlDialog(gridRecordRow);
}

function openLocomotiveControlDialog(gridRecordRow) {
    const locCtrl = new LocomotiveControl();
    const res = locCtrl.open(gridRecordRow);
    if (!res) {
        console.log("Already open!");
    } else {
        window.locomotiveCtrlDlgs.push(locCtrl);
        locCtrl.on('dialogClosed', function (ev) {
            removeLocomotiveDialogFromGlobalList(ev.data.instance.id);
        });
        locCtrl.on('functionChanged', function (ev) {
            changeLocomotive('function', ev.data);
        });
        locCtrl.on('speedChanged', function (ev) {
            changeLocomotive('speedstep', ev.data);
        });
        locCtrl.on('directionChanged', function (ev) {
            changeLocomotive('direction', ev.data);
        });
    }
}

function scrollDebugToEnd() {
    const debugMessages = document.getElementsByClassName("messageContainer");
    $(debugMessages).scroll();
    $(debugMessages).animate({
        scrollTop: debugMessages[0].scrollHeight
    }, "fast");
}

function initDebugConsole() {
    const ctrlLoggingBtn = $('#statusBar div.logging');
    const ctrlDebugConsole = $('.debugConsole');

    ctrlDebugConsole.find('.clearDebug').click(function () {
        $('.messageContainer').html("");
    });
    ctrlDebugConsole.find('.scrollTop').click(function () {
        // scroll to top
        const debugMessages = document.getElementsByClassName("messageContainer");
        $(debugMessages).scroll();
        $(debugMessages).animate({
            scrollTop: 0
        }, "slow");
    });
    ctrlDebugConsole.find('.scrollBottom').click(function () {
        // scroll to bottom
        scrollDebugToEnd();
    });

    ctrlLoggingBtn.click(function () {
        if (ctrlDebugConsole.is(':visible')) {
            ctrlDebugConsole.hide();
        } else {
            ctrlDebugConsole.css({
                "bottom": ($('#statusBar').height() + 1) + "px",
                "right": $('#sidebar').width() + "px"
            });

            if (typeof window.__ctrlDebugConsoleInitialized === "undefined" ||
                window.__ctrlDebugConsoleInitialized == null ||
                window.__ctrlDebugConsoleInitialized === false) {
                window.__ctrlDebugConsoleInitialized = true;
                ctrlDebugConsole.resizable({
                    handles: "n, w",
                    stop: function (event, ui) {
                        realignDebugConsole();
                    }
                });
            }

            ctrlDebugConsole.show();

            scrollDebugToEnd();
        }
    });
}

function changeWorkspace(selCtrl) {
    const ws = $("#w2ui-popup table#workspaceSelection").find("select :selected").val();
    if (typeof ws === "undefined" || ws == null || ws.length <= 0) return;
    window.open("?workspace=" + ws, "_self");
}

function createWorkspace(inCtrl) {
    for (let i = 0; i < inCtrl.length; ++i) {
        const ws = $(inCtrl[i]).val();
        if (typeof ws !== "undefined" && ws != null && ws.length > 0) {
            window.open("?workspace=" + ws, "_self");
            return;
        }
    }
}

function handleWorkspace(eventNode) {
    const ctrl = $('#workspaceSelectionFrame');
    ctrl.w2popup({
        "title": "Workspace Selection",
        "height": "200px",
        "width": "450px",
        buttons: '<button class="w2ui-btn" onclick="w2popup.close();">Close</button>',
        onOpen: function (ev) {
            ev.onComplete = function () {
                // TBD
            }
        }
    });
}

function realignDebugConsole() {
    const ctrlDebugConsole = $('.debugConsole');
    ctrlDebugConsole.css({
        "inset": "",
        "bottom": ($('#statusBar').height() + 1) + "px",
        "right": $('#sidebar').width() + "px",
        "position": "absolute"
    });
}

function __formatDebugMessage(msgObj) {
    if (typeof msgObj === "undefined") return null;
    if (msgObj == null) return null;

    // TODO filter message level, i.e. msgObj.level [Info, Error, Warning, Debug]

    return '<span class="dt">' + msgObj.time + '</span><span class="msg">' + msgObj.message + '</span>';
}

function isBlank(str) {
    return (!str || /^\s*$/.test(str));
}

function addDebugMessages(msgsArray, targetClassName = "messageContainer") {
    const debugConsoles = document.getElementsByClassName(targetClassName);
    const targetCtrl = debugConsoles[0];
    for (let i = 0; i < msgsArray.length; ++i) {
        let innerHtml = targetCtrl.innerHTML;
        const msgObj = JSON.parse(msgsArray[i]);
        if (msgObj.message.length === 0) continue;
        const m = __formatDebugMessage(msgObj);
        if (m == null) continue;

        if (targetCtrl.innerHTML.length === 0 || isBlank(targetCtrl.innerHTML))
            targetCtrl.innerHTML = m;
        else
            targetCtrl.innerHTML = innerHtml + "<br>" + m;
    }
    targetCtrl.scrollTop = targetCtrl.scrollHeight;
}

function initWebcamsState() {
    const webcamsShown = $.localStorage.getItem("webcamShown");
    if (typeof webcamsShown === "undefined" || webcamsShown == null) return;
    const obj = JSON.parse(webcamsShown);
    toggleAllWebcamsVisibility(obj.shown);
    const sb = w2ui["sidebar"];
    const node = sb.get("cmdToggleWebcams");
    toggleLabelOnOff(node, "Webcams", null, null, obj.shown);
}

function initLabelState() {
    const labelsShown = $.localStorage.getItem("labelShown");
    if (typeof labelsShown === "undefined" || labelsShown == null) return;
    const obj = JSON.parse(labelsShown);
    window.labelShown = obj.shown;
    toggleAllLabelInformation(obj.shown);
    const sb = w2ui["sidebar"];
    const node = sb.get("cmdToggleLabels");
    toggleLabelOnOff(node, "Labels", null, null, obj.shown);
}

$(document).ready(function () {
    window.__autoModeState = false;

    addJqueryExtensions();
    initDebugConsole();

    $('#statusBar div.autoMode').tipso({
        size: 'tiny',
        speed: 100,
        delay: 250,
        useTitle: true,
        background: '#333333',
        titleBackground: '#333333'
    });

    $('#statusBar div.logging').tipso({
        size: 'tiny',
        speed: 100,
        delay: 250,
        useTitle: true,
        background: '#333333',
        titleBackground: '#333333'
    });

    window.__ecosbaseChanged = false;
    window.__locomotivesChanged = false;
    window.__accessoriesChanged = false;
    window.__feedbacksChanged = false;

    window.errorHandler = new ErrorHandler();
    window.toolbox = new Toolbox();

    window.dialogLightAndPower = new LightAndPower();
    window.dialogLightAndPower.install();
    window.dialogLightAndPower.on('relayCommand', function (ev) {
        const data = ev.data;
        if (data.mode === "websocket")
            relayWebsocketCommand(data);
        else
            console.log("TODO unknown relay mode: " + data.mode);
    });

    window.dialogS88 = new FeedbackVisualization();
    window.dialogS88.install();

    window.blocksDlg = new Blocks();
    window.blocksDlg.install();
    window.blocksDlg.on('setting', function (ev) {
        changeSetting(ev.data);
    });

    window.occ = new Occ();
    window.occ.on('gotoBlock', function (ev) {
        gotoBlock(ev.data);
    });
    window.occ.on('resetAssignment', function (ev) {
        resetAssignment(ev.data);
    });
    window.occ.on('speedChanged', function (ev) {
        changeLocomotive('speedstep', ev.data);
    });
    window.occ.on('directionChanged', function (ev) {
        changeLocomotive('direction', ev.data);
    });
    window.occ.on('openLocomotiveControl', function (ev) {
        const oid = ev.data.oid;
        openLocomotiveControlDialogByOid(oid);
    });
    window.occ.on('setting', function (ev) {
        changeSetting(ev.data);
    });

    window.accessoriesDlg = new Accessories();
    window.accessoriesDlg.install();
    window.accessoriesDlg.on("accessoryExecute", function (ev) {
        accessoryExecute(ev.data);
    });
    window.accessoriesDlg.on("setting", function (ev) {
        changeSetting(ev.data);
    });
    window.__eventOn('startAccessoryTest', function (ev) {
        accessoryTest(ev.data);
    });

    window.routesDlg = new Routes();
    window.routesDlg.install();
    window.routesDlg.on("checkRoute", function (ev) {
        checkRoute(ev.data);
    });
    window.routesDlg.on("setting", function (ev) {
        changeSetting(ev.data);
    });

    window.locomotivesDlg = new Locomotives();
    window.locomotivesDlg.install();
    window.locomotivesDlg.on('locomotiveDoubleClick', function (ev) {
        const gridRecordRow = ev.data;
        openLocomotiveControlDialog(gridRecordRow);
    });
    window.locomotivesDlg.on('setting', function (ev) {
        changeSetting(ev.data);
    });
    window.__eventOn('functionChanged', function (ev) {
        var jsonData = JSON.parse(ev.data);
        changeLocomotive('function', jsonData);
    });

    window.planField = new Planfield({
        isEditMode: false
    });
    window.planField.install();
    window.planField.on('controlCreated', function (ev) {
        const ctrlInstance = ev.data.instance;

        // inform the Route/S88/Signals dialog for updating its internal lists
        if (typeof window.blocksDlg !== "undefined" && window.blocksDlg != null)
            window.blocksDlg.controlCreated(ctrlInstance);
    });
    window.planField.on('clicked', function (ev) { itemClicked(ev.data); });
    window.planField.on('assignToBlock', function (ev) { assignLocomotiveToBlock(ev.data); });
    window.planField.on("setting", function (ev) {
        changeSetting(ev.data);
    });

    window.serverHandling = new ServerHandling({
        wsAddr: "localhost",
        wsPort: 45099
    });
    window.serverHandling.establishConnection();
    window.serverHandling.on("occReceived", function (ev) {
        window.occ.handleData(ev.data);
        window.locomotivesDlg.updateOccInformation(ev.data);
    });
    window.serverHandling.on("locomotivesDataReceived", function (ev) {
        window.locomotivesDlg.updateLocomotives2(ev.data);
        window.occ.updateOccWithLocomotiveData(ev.data);
    });
    window.serverHandling.on("feedbacksDataReceived", function (ev) {
        window.blocksDlg.updateFeedbacks(ev.data);
        window.planField.updateFeedbacks(ev.data);
        window.locomotivesDlg.updateBlockInformation(ev.data);
    });
    window.serverHandling.on('settingsDataReceived', function (ev) {
        window.settings = ev.data;
    });
    window.serverHandling.on('themeDataReceived', function (ev) {
        window.themeObject = ev.data;
        window.toolbox.install();
    });
    window.serverHandling.on('debugMessages', function (ev) {
        window.addDebugMessages(ev.data);
        //const debugMessages = ev.data;
        //let i;
        //const iMax = debugMessages.length;
        //for (i = 0; i < iMax; ++i)
        //    console.log(debugMessages[i]);
    });

    function handleResult_planitemRemove(cmdResult) {
        if (typeof cmdResult === "undefined" || cmdResult == null) return false;
        window.planField.updateEcosAccessories(window.ecosData.accessories);
        if (typeof window.accessoriesDlg === "undefined" || window.accessoriesDlg == null) return false;
        if (cmdResult.result === true) {
            window.accessoriesDlg.removeAccessory(cmdResult.identifier);
            window.blocksDlg.controlRemoved(cmdResult.identifier);
        }
        return true;
    }

    window.serverHandling.on("commandResultReceived", function (ev) {
        const jsonCommand = ev.data;
        if (typeof jsonCommand === "undefined" || jsonCommand == null) return false;
        const cmd = jsonCommand.command;
        const cmdResult = jsonCommand.result;
        if (typeof cmd === "undefined" || cmd == null) return false;
        if (typeof cmdResult === "undefined" || cmdResult == null) return false;

        switch (cmd) {
            case "result": {
                return handleResult_planitemRemove(cmdResult);
            }
                break;
        }

        return false;
    });

    window.serverHandling.on("dataReceived", function (ev) {
        window.ecosData = ev.data.ecosData;
        if (typeof window.ecosData === "undefined" || window.ecosData == null) {
            // TODO set default values to all components
        } else {
            window.__ecosbaseChanged = window.ecosData.ecosbaseChanged;
            window.__locomotivesChanged = window.ecosData.locomotivesChanged;
            window.__accessoriesChanged = window.ecosData.accessoriesChanged;
            window.__feedbacksChanged = window.ecosData.feedbacksChanged;

            //console.log("CHANGES: " +
            //    window.__ecosbaseChanged +
            //    "  |  " +
            //    window.__locomotivesChanged +
            //    "  |  " +
            //    window.__accessoriesChanged +
            //    "  |  " +
            //    window.__feedbacksChanged
            //);

            // update statusbar
            if (window.__ecosbaseChanged === true) {
                var ecosbase = window.ecosData.ecosbase[0];
                if (typeof ecosbase !== "undefined" && ecosbase != null) {
                    var divPowerStatus = $('#statusBar div.ecosPowerStatus');
                    if (ecosbase.status === "GO") {
                        divPowerStatus.removeClass("offline");
                        divPowerStatus.addClass("online");
                    } else {
                        divPowerStatus.removeClass("online");
                        divPowerStatus.addClass("offline");
                    }

                    var divInformation = $('#statusBar div.ecosInformation');
                    var name = ecosbase.name;
                    var protocolVersion = ecosbase.protocolVersion;
                    var applicationVersion = ecosbase.applicationVersion;
                    var hardwareVersion = ecosbase.hardwareVersion;
                    var strInfo = "<b>" +
                        name +
                        "</b>" +
                        " (SW: " +
                        applicationVersion +
                        ", HW: " +
                        hardwareVersion +
                        ", Protocol: " +
                        protocolVersion +
                        ")";
                    divInformation.html(strInfo);
                }
            }

            // update locomotives
            if (window.__locomotivesChanged === true) {
                var locomotives = window.ecosData.locomotives;
                if (typeof locomotives !== "undefined") {
                    window.locomotivesDlg.updateLocomotives(locomotives);
                    window.locomotivesDlg.updateLocomotives2(locomotives);
                    window.occ.updateEcosDirection(locomotives);
                    window.occ.updateEcosSpeedInfo(locomotives);
                }
            }
        }

        if (typeof ev.data.routes !== "undefined" && ev.data.routes != null) {
            window.routes = ev.data.routes;
            if (typeof window.routes === "undefined" || window.routes == null) {
                // TODO 
            } else {
                window.routesDlg.updateRoutes(window.routes);
            }
        }

        if (window.accessoriesDlg) {
            window.accessoriesDlg.updateAccessories(ev.data);
        }

        if (typeof window.ecosData !== "undefined" && window.ecosData !== null) {
            if (window.__accessoriesChanged === true) {
                window.planField.updateEcosAccessories(window.ecosData.accessories);
            }

            if (window.__feedbacksChanged === true) {
                window.planField.updateEcosFeedbacks(window.ecosData.feedbacks);
                window.dialogS88.updateFeedbackSensors(window.ecosData.feedbacks);
            }

            if (window.accessoriesDlg) {
                window.accessoriesDlg.updateStates(ev.data);
            }

            if (typeof window.ecosData.locomotives !== "undefined" && window.ecosData.locomotives != null) {
                if (window.__locomotivesChanged === true) {
                    window.occ.loadLocomotives(window.ecosData.locomotives);
                }
            }
        }
    });

    window.serverHandling.on("autoModeReceived",
        function (ev) {
            const jsonCommand = ev.data;
            if (typeof jsonCommand === "undefined" || jsonCommand == null) return false;
            const cmd = jsonCommand.command;
            if (typeof cmd === "undefined" || cmd == null) return false;

            // planField.activateRouteUserHighlight
            // planField.clearRouteUserHighlight

            const subCmd = jsonCommand.data.command;

            switch (subCmd) {

                case "ghost":
                    {
                        const state = jsonCommand.data.state;
                        if (typeof state === "undefined" || state == null) break;

                        const fncClearGhost = function() {
                            const allFbItems = $('div.ctrlItemFeedback[id]');
                            const iMax = allFbItems.length;
                            for (let i = 0; i < iMax; ++i) {
                                $(allFbItems[i]).removeClass("ghost");
                            }
                        }

                        if (typeof state.found === "undefined"
                            || state.found == null
                            || state.found === false)
                        {
                            fncClearGhost();
                        } else {
                            fncClearGhost();

                            const allFbItems = $('div.ctrlItemFeedback[id]');
                            const fbs = state.fbs;
                            for (let i = 0; i < fbs.length; ++i) {
                                const fbItem = fbs[i];
                                if (typeof fbItem === "undefined") continue;
                                if (fbItem == null) continue;

                                const fbItemCoord = fbItem.coord;
                                const item = getCtrlOfCoord(fbItemCoord.x, fbItemCoord.y, allFbItems);
                                item.addClass("ghost");
                            }
                        }

                    } break;

                case "state": {

                    let state = jsonCommand.data.state;
                    if (typeof state === "undefined" || state == null) break;

                    let isStopping = jsonCommand.data.state.stopping;
                    if (typeof isStopping === "undefined" || isStopping == null)
                        isStopping = false;
                    if (isStopping === true) {

                        // TODO restore state of AutoMode stopping
                        // is relevant when AutoMode has been recently stopped
                        // and user have reloaded the web ui

                        if (state.message.length === 0)
                            state.message = "AutoMode is stopping...";

                        $('.overlayAutoMode').show();
                        $('.overlayAutoModeText').html(state.message);
                        $('.overlayAutoModeText').show();

                        break;
                    }

                    $('.overlayAutoMode').hide();
                    $('.overlayAutoModeText').hide();

                    let isStarted = jsonCommand.data.state.started;
                    if (typeof isStarted === "undefined" || isStarted == null)
                        isStarted = false;

                    reinitAutoMode(isStarted);
                }
                    break;

                case "routeShow":
                    {
                        const routes = jsonCommand.data.routeNames;
                        if (typeof routes === "undefined" || routes == null) return false;

                        let i;
                        const iMax = routes.length;
                        for (i = 0; i < iMax; ++i) {
                            if (routes[i] == null) continue;
                            const route = routes[i];
                            const routeInstance = getRouteByName(route);
                            if (routeInstance == null) {
                                console.log("Route does not exist: " + route);
                                continue;
                            }
                            window.planField.activateRouteVisualization(routeInstance);
                        }
                    }
                    break;

                case "routeReset":
                    {
                        const routes = jsonCommand.data.routeNames;
                        if (typeof routes === "undefined" || routes == null) return false;

                        let i;
                        const iMax = routes.length;
                        for (i = 0; i < iMax; ++i) {
                            if (routes[i] == null) continue;
                            const route = routes[i];
                            const routeInstance = getRouteByName(route);
                            if (routeInstance == null) {
                                console.log("Route does not exist: " + route);
                                continue;
                            }
                            window.planField.deativateRouteVisualization(routeInstance);
                        }
                    }
                    break;
            }
        });

    loadSideBar();

    loadWebcams();

    initWebcamsState();
    initLabelState();
});

function reinitAutoMode(state) {
    if (typeof window.__autoModeState === "undefined" || window.__autoModeState == null)
        window.__autoModeState = state;
    if (state === true)
        $('#statusBar div.autoMode').addClass("fa-spin");
}

var loadSideBar = (function () {
    $('#sidebar').w2sidebar({
        name: 'sidebar',
        flatButton: true,
        nodes: [
            {
                id: 'level-1',
                text: 'Control',
                img: 'icon-folder',
                expanded: true,
                group: true,
                groupShowHide: false,
                nodes: [
                    { id: 'cmdOpenLocomotives', text: 'Locomotives', icon: 'fa fa-train' },
                    { id: 'cmdAccessories', text: 'Accessories', icon: 'fas fa-magic' },
                    { id: 'cmdLight', text: 'Light', icon: 'fas fa-lightbulb' },
                    { id: 'cmdInitialize', text: 'Initialize', icon: 'fas fa-tasks' },
                    { id: 'cmdAutomatic', text: 'Automatic (off)', icon: 'fas fa-robot fa-red' }
                ]
            },
            {
                id: 'level-2',
                text: 'Administration',
                img: 'icon-folder',
                expanded: true,
                group: true,
                groupShowHide: true,
                nodes: [
                    { id: 'cmdToggleLabels', text: 'Labels (off)', icon: 'fas fa-tags' },
                    { id: 'cmdEditPlan', text: 'Layout', icon: 'fa fa-edit' },
                    { id: 'cmdOpenBlocksAndFeedbacks', text: 'Blocks/S88/Signals', icon: 'fas fa-traffic-light' },
                    { id: 'cmdOpenS88', text: 'S88 Viewer', icon: 'fas fa-radiation-alt' },
                    { id: 'cmdOpenRoutes', text: 'Routes', icon: 'fa fa-route' },
                    { id: 'cmdAnalyzeRoutes', text: 'Analyze Routes', icon: 'fas fa-diagnoses' },
                    // TODO add back when we implement worldclock support
                    { id: 'cmdToggleWebcams', text: 'Webcams (on)', icon: 'fas fa-video' },
                    { id: 'cmdWorldClock', text: 'Worldclock', icon: 'fas fa-history', hidden: true }
                ]
            },
            {
                id: 'levelEcos',
                text: 'ECoS',
                img: 'icon-folder',
                expanded: true,
                group: true,
                groupShowHide: false,
                nodes: [
                    { id: 'cmdPower', text: 'Power', icon: 'fas fa-power-off' },
                    { id: 'cmdStop', text: 'Stop Trains', icon: 'fas fa-stop-circle' },
                    { id: 'cmdShutdown', text: 'Shutdown', icon: 'fas fa-times-circle' },
                    { id: 'cmdChangeWorkspace', text: 'Workspace', icon: 'fas fa-code-branch' }
                ]
            },
            {
                id: 'level-3',
                text: 'Help',
                img: 'icon-folder',
                group: true,
                groupShowHide: true,
                nodes: [
                    { id: 'cmdHelp', text: 'Help', icon: 'fas fa-question-circle' },
                    { id: 'cmdAbout', text: 'About', icon: 'fas fa-address-card' },
                    { id: 'cmdReport', text: 'Report', icon: 'far fa-newspaper' }
                ]
            }
        ],
        onFlat: function (event) {
            $('#sidebar').css('width', (event.goFlat ? '35px' : '200px'));
            realignDebugConsole();
        },
        onClick: function (event) {
            var target = event.target;
            if (target === "cmdOpenLocomotives") {
                locomotivesDlg.show();
                w2ui['sidebar'].unselect('cmdOpenLocomotives');
            } else if (target === 'cmdOpenBlocksAndFeedbacks') {
                window.blocksDlg.show();
                w2ui['sidebar'].unselect('cmdOpenBlocksAndFeedbacks');
            } else if (target === "cmdOpenRoutes") {
                routesDlg.show();
                w2ui['sidebar'].unselect('cmdOpenRoutes');
            } else if (target === "cmdAccessories") {
                accessoriesDlg.show();
                w2ui['sidebar'].unselect('cmdAccessories');
            } else if (target === "cmdOpenS88") {
                window.dialogS88.show();
                w2ui['sidebar'].unselect('cmdOpenS88');
            } else if (target === "cmdEditPlan") {
                var fncToggleLocInfos = (function (state) {
                    toggleAllLocomotiveInformation(state);
                });
                if (window.editState) {
                    window.editState = false;
                    window.planField.setEditMode(false);
                    fncToggleLocInfos(true);
                    window.toolbox.hideToolbox();
                } else {
                    window.editState = true;
                    window.planField.setEditMode(true);
                    fncToggleLocInfos(false);
                    window.toolbox.showToolbox();
                }
                w2ui['sidebar'].unselect('cmdEditPlan');
            } else if (target === "cmdToggleLabels") {
                toggleLabelOnOff(event.node, 'Labels', 'cmdToggleLabels', function (state) {
                    toggleAllLabelInformation(state);
                    window.labelShown = state;
                });
                w2ui['sidebar'].unselect('cmdToggleLabels');
            } else if (target === "cmdToggleWebcams") {
                toggleLabelOnOff(event.node, 'Webcams', 'cmdToggleWebcams', function (state) {
                    toggleAllWebcamsVisibility(state);
                });
                w2ui['sidebar'].unselect('cmdToggleWebcams');
            } else if (target === "cmdLight") {
                window.dialogLightAndPower.show();
                w2ui['sidebar'].unselect('cmdLight');
            } else if (target === "cmdWorldClock") {
                showTodoDialog();
                w2ui['sidebar'].unselect('cmdWorldClock');
            } else if (target === "cmdPower") {
                changeSetting({ 'mode': 'ecos', 'cmd': 'power' });
                w2ui['sidebar'].unselect('cmdPower');
            } else if (target === "cmdStop") {
                changeSetting({ 'mode': 'ecos', 'cmd': 'stop' });
                w2ui['sidebar'].unselect('cmdStop');
            } else if (target === "cmdAutomatic") {
                const lbl = event.node.text;
                const isOff = lbl.includes("off");
                handleAutoMode(isOff, true, function (newState) {
                    toggleLabelOnOff(event.node, 'Automatic', 'cmdAutomatic', null, newState);
                });
                w2ui['sidebar'].unselect('cmdAutomatic');
            } else if (target === "cmdInitialize") {
                handleInitialize(event.node);
                w2ui['sidebar'].unselect('cmdInitialize');
            } else if (target === "cmdAnalyzeRoutes") {
                handleAnalyzeRoutes(event.node);
                w2ui['sidebar'].unselect('cmdAnalyzeRoutes');
            } else if (target === "cmdShutdown") {
                handleShutdown(event.node);
                w2ui['sidebar'].unselect('cmdShutdown');
            } else if (target === "cmdChangeWorkspace") {
                handleWorkspace(event.node);
                w2ui['sidebar'].unselect('cmdChangeWorkspace');
            } else if (target === "cmdHelp") {
                window.open(constGitWikiWebsite, "_blank");
                w2ui['sidebar'].unselect('cmdHelp');
            } else if (target === "cmdAbout") {
                w2popup.open({
                    title: 'About',
                    body: '<div class="w2ui-centered"><div style="font-weight: bold;">&copy; Dr. Christian Benjamin Ries</div><br>' +
                        '<table style="text-align: center; margin: auto;">' +
                        '<tr>' +
                        '<td>Contact:</td>' +
                        '<td style="text-align: left;">rail@christianbenjaminries.de</td>' +
                        '</tr>' +
                        '<tr>' +
                        '<td>License:</td>' +
                        '<td style="text-align: left;">MIT</td>' +
                        '</tr>' +
                        '<tr>' +
                        '<td>Sourcecode:</td>' +
                        '<td style="text-align: left;"><a href="' + constGitWebsite + '" target="_blank">' + constGitWebsite + '</a></td>' +
                        '</tr>' +
                        '<tr>' +
                        '<td>Website:</td>' +
                        '<td style="text-align: left;"><a href="http://www.railessentials.net" target="_blank">www.railessentials.net</a></td>' +
                        '</tr>' +
                        '<tr>' +
                        '<td>Used libraries:</td>' +
                        '<td style="text-align: left;">' +
                        '<a href="' + constGitUsedSwWebsite + '" target="_blank">Link to \'Third-Party Components at Their Best\'</a>' +
                        '</td>' +
                        '</tr>' +
                        '</table></div>'
                });
                w2ui['sidebar'].unselect('cmdAbout');
            } else if (target === "cmdReport") {
                window.open("/report.html", '_blank').focus();
            }
        }
    });

    function handleAutoMode(state, sendAutoModeCommandToServer = true, callback = null) {
        if (typeof window.__autoModeState === "undefined" || window.__autoModeState == null)
            window.__autoModeState = false;

        let msgToShow;
        if (state === true && window.__autoModeState === false)
            msgToShow = "Do you like to start AutoMode?";
        else
            msgToShow = "Stop AutoMode?";

        w2confirm(msgToShow, 'AutoMode')
            .yes(function () {
                const srv = window.serverHandling;
                if (typeof srv === "undefined" || srv == null) return;

                if (window.__autoModeState === true) { // switch off AutoMode

                    window.planField.clearRouteUserHighlight();

                    // trigger switch-off
                    if (sendAutoModeCommandToServer === true) {
                        srv.sendCommand({
                            "command": "autoMode",
                            "timestamp": Date.now(),
                            "cmddata": {
                                "state": false
                            }
                        });
                    }

                    $('#statusBar div.autoMode').removeClass("fa-spin");

                    window.__autoModeState = false;

                } else if (window.__autoModeState === false) { // switch on AutoMode

                    // trigger switch-on
                    if (sendAutoModeCommandToServer === true) {
                        srv.sendCommand({
                            "command": "autoMode",
                            "timestamp": Date.now(),
                            "cmddata": {
                                "state": true
                            }
                        });
                    }

                    $('#statusBar div.autoMode').addClass("fa-spin");

                    window.__autoModeState = true;
                }

                if (callback != null)
                    callback(window.__autoModeState);
            })
            .no(function () {
                // ignore
            });
    }

    function handleInitialize(eventNode) {
        w2confirm('Do you like to initialize all Switches, Accessories, Signals, and more to a well known state?', 'Initialize')
            .yes(function () {
                const srv = window.serverHandling;
                if (typeof srv === "undefined" || srv == null) return;
                srv.sendCommand({
                    "command": "initializeSystem",
                    "timestamp": Date.now(),
                    "cmddata": {
                    }
                });
            })
            .no(function () {
                // do nothing
            });
    }

    function handleAnalyzeRoutes(eventNode) {
        w2confirm('Do you like to analyze all routes?<br><br>Current disabled states will be kept, new routes will be added, vanished routes will be removed.', 'Analyze Routes')
            .yes(function () {
                const srv = window.serverHandling;
                if (typeof srv === "undefined" || srv == null) return;

                // reset current list of routes
                window.routesDlg.clearGrid();
                window.blocksDlg.clearGrid();

                // send command to server
                srv.sendCommand({
                    "command": "analyzeRoutes",
                    "timestamp": Date.now(),
                    "cmddata": {
                    }
                });
            })
            .no(function () {
                // do nothing
            });
    }

    function handleShutdown(eventNode) {
        w2confirm('Do you like to shutdown your model railway?', 'Shutdown')
            .yes(function () {
                const srv = window.serverHandling;
                if (typeof srv === "undefined" || srv == null) return;
                srv.sendCommand({
                    "command": "shutdownSystem",
                    "timestamp": Date.now(),
                    "cmddata": {
                    }
                });
            })
            .no(function () {
                // do nothing
            });
    }

    //w2ui.sidebar.goFlat();

    function showTodoDialog() {
        w2popup.open({
            title: 'TODO / TBD',
            body: '<div class="w2ui-centered">Must be implemented.</div>'
        });
    }
});

function toggleLabelOnOff(eventNode, caption, cmdName, callback, newState) {

    if (typeof newState !== "undefined" && newState != null) {
        if (newState === true) {
            eventNode.text = caption + " (on)";
        } else {
            eventNode.text = caption + " (off)";
        }
    } else {
        const lbl = eventNode.text;
        const isOff = lbl.includes("off");
        if (isOff) {
            eventNode.text = caption + " (on)";
            if (typeof callback !== "undefined" && callback != null)
                callback(true);
        } else {
            eventNode.text = caption + " (off)";
            if (typeof callback !== "undefined" && callback != null)
                callback(false);
        }
    }
    w2ui['sidebar'].refresh(cmdName);
}
