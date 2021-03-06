class Events {
    constructor() {
        this.__triggers = {};
    }

    on(event, callback) {
        if (!this.__triggers [event])
            this.__triggers [event] = [];
        this.__triggers [event].push(callback);
    }

    triggerHandler(event, params) {
        if (this.__triggers[event]) {
            for (var i in this.__triggers[event])
                this.__triggers[event][i](params);
        }
    }
}