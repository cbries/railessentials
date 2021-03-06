class ServerHandling {
    constructor(options = {}) {
        console.log("**** construct ServerHandling");
        this.__reconnectIntervalMsecs = 5000;
        this.__updateServerInformation(options);
        this.__planField = window.planField;
        this.__errorHandler = window.errorHandler;
        this.__checkConnectIntervalId = null;
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

    __getReconnectCountdownMessage(strSeconds) {
        return "Connection not established to "
            + this.wsUrl
            + ". Try to reconnect in "
            + strSeconds + " seconds.";
    }

    __updateServerInformation(options) {
        if (options) {
            if (options.wsAddr)
                this.wsAddr = options.wsAddr;
            if (options.wsPort)
                this.wsPort = options.wsPort;
        }
    }

    __cleanupWebSocket() {
        if (typeof self.__ws !== "undefined" || self.__ws != null) {
            this.__ws.onopen = function () { };
            this.__ws.onmessage = function (e) { };
            this.__ws.onerror = function (e) { }
            this.__ws.onclose = function () { }
            this.__ws.close();
        }
    }

    __connect() {
        const self = this;

        this.__cleanupWebSocket();

        this.wsUrl = 'ws://' + this.wsAddr + ':' + this.wsPort + '/';
        this.__ws = new WebSocket(this.wsUrl);

        this.__ws.onopen = function () { self.__handleOnOpen(); };
        this.__ws.onmessage = function (e) { self.__handleOnMessage(e); };
        this.__ws.onerror = function (e) { self.__handleOnError(e); }
        this.__ws.onclose = function () { self.__handleOnClose(); }
    }

    __startConnectHandlerInterval() {
        if (this.__checkConnectIntervalId != null) return;
        const self = this;
        var secondsToWait = parseInt(this.__reconnectIntervalMsecs / 1000);
        this.__checkConnectIntervalId = setInterval(function () {
            if (secondsToWait <= 0) {
                console.log("Try to connect...");
                self.__errorHandler.setLevel(ErrorHandlerLevel.Info);
                self.__errorHandler.setText("Try to connect...", true);
                self.__stopConnectHandlerInterval();
                self.__connect();
            } else {
                self.__errorHandler.setLevel(ErrorHandlerLevel.Error);
                self.__errorHandler.setText(self.__getReconnectCountdownMessage(secondsToWait), true);
            }
            --secondsToWait;
        }, 1000);
    }

    __stopConnectHandlerInterval() {
        if (this.__checkConnectIntervalId == null)
            return;
        const self = this;
        clearInterval(this.__checkConnectIntervalId);
        this.__checkConnectIntervalId = null;
    }

    __handleOnOpen() {
        console.log('ServerHandling Connected to ' + this.wsUrl);
        this.__stopConnectHandlerInterval();
        this.__errorHandler.hide();
    }

    __handleOnMessage(e) {
        //console.log("__handleOnMessage()");
        const jsonData = JSON.parse(e.data);
        switch (jsonData.command) {
            case "debugMessages": {
                this.__trigger('debugMessages', jsonData.messages);
            } break;
            case "initialization":
            {
                this.__trigger('settingsDataReceived', jsonData.settings);
                this.__trigger('themeDataReceived', jsonData.themeData);
                this.__planField.initFromServer(jsonData.metamodel);
                this.__trigger('dataReceived', jsonData);
            } break;
            case "update": {
                this.__trigger('dataReceived', jsonData);
            } break;
            case "occ": {
                this.__trigger('occReceived', jsonData);
            } break;
            case "locomotivesData": {
                this.__trigger('locomotivesDataReceived', jsonData);
            } break;
            case "feedbacksData": {
                this.__trigger('feedbacksDataReceived', jsonData);
            } break;
            case "autoMode": {
                this.__trigger('autoModeReceived', jsonData);
            } break;
            case "result": {
                this.__trigger('commandResultReceived', jsonData);
            }
                break;
            default: {
                //console.log("Received: '" + e.data + "'");
            } break;
        }
    }

    __handleOnError(e) {
        const strSeconds = this.__reconnectIntervalMsecs / 1000;
        this.__errorHandler.setLevel(ErrorHandlerLevel.Error);
        this.__errorHandler.setText(this.__getReconnectCountdownMessage(strSeconds));
        this.__cleanupWebSocket();
        this.__startConnectHandlerInterval();
    }

    __handleOnClose() {
        this.__errorHandler.setLevel(ErrorHandlerLevel.Error);
        this.__errorHandler.setText("Connection closed.");
        this.__cleanupWebSocket();
        this.__startConnectHandlerInterval();
    }

    establishConnection(options = {}) {
        const self = this;
        this.__errorHandler.setLevel(ErrorHandlerLevel.Info);
        this.__errorHandler.setText("Try to establish connection...");
        this.__updateServerInformation(options);
        this.__connect();
    }

    isConnected() {
        if (typeof this.__ws === "undefined") return false;
        if (this.__ws == null) return false;
        var state = this.__ws.readyState;
        const State_Connecting = 0;
        const State_Open = 1;
        const State_Closing = 2;
        const State_Closed = 3;
        return state == State_Open;
    }

    __getCoordOf(jqueryElement) {
        const coordX = jqueryElement.get(0).offsetLeft / constItemWidth;
        const coordY = jqueryElement.get(0).offsetTop / constItemHeight;
        return { x: coordX, y: coordY };
    }

    __getJsonData(jqueryElement) {
        return jqueryElement.data(constDataThemeItemObject);
    }

    sendControl(jqueryElement) {
        if (!this.isConnected())
            return false;

        var dataToSend = {
            command: "update",
            itemData: this.__getJsonData(jqueryElement)
        }
        dataToSend.itemData.identifier = jqueryElement.attr("id");
        dataToSend.itemData.coord = this.__getCoordOf(jqueryElement);
        dataToSend.itemData.editor.themeDimIdx = jqueryElement.data(constDataThemeDimensionIndex);
        var isTextFieldCtrl = jqueryElement.hasClass("elEditorRoot");
        if (isTextFieldCtrl) {
            var elEditor = jqueryElement.find('div.elEditor');
            dataToSend.itemData.editor.innerHtml = elEditor.html();
            dataToSend.itemData.editor.outerHtml = jqueryElement.html();
            dataToSend.itemData.editor.size = {
                width: elEditor.css("width"),
                height: elEditor.css("height")
            };
        }

        var jsonData = JSON.stringify(dataToSend);
        this.__ws.send(jsonData);
    }

    removeControl(itemId) {
        if (!this.isConnected())
            return false;

        var dataToSend = {
            command: "remove",
            itemId: itemId
        };

        this.__ws.send(JSON.stringify(dataToSend));
    }

    sendCommand(cmdData) {
        if (typeof cmdData === "undefined") return;
        if (cmdData === null) return;

        this.__ws.send(JSON.stringify(cmdData));
    }
}