class WindowGeometryStorage {
    constructor(name, defaultGeometry = {
        left: 100,
        top: 100,
        width: 450,
        height: 500
    }) {
        this.__fieldName = name;
        this.__defaultGeometry = defaultGeometry;
    }

    save(position, size) {
        const self = this;
        var geometry = {
            left: position.left,
            top: position.top,
            width: size.width,
            height: size.height
        }
        $.localStorage.setItem(this.__fieldName, JSON.stringify(geometry));
    }

    recent() {
        const self = this;
        var geometry = $.localStorage.getItem(this.__fieldName);
        if (typeof geometry === "undefined" || geometry == null)
            return this.__defaultGeometry;

        var jsonObj = JSON.parse(geometry);
        return jsonObj;
    }

    showWithGeometry(jqueryEl, useSize = true) {
        var geometry = this.recent();
        if (useSize === true) {
            jqueryEl.dialog("option", "height", geometry.height);
            jqueryEl.dialog("option", "width", geometry.width);
        }
        jqueryEl.dialog("option", "position",
            {
                my: "left top",
                at: "left+" + geometry.left + " top+" + geometry.top,
                of: window
            } );
    }
}

