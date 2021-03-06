class Toolbox {

    constructor(selector) {toolbox
        console.log("**** construct Toolbox");
        this.__toolbox = $('#toolbox');
        this.recentItem = null;
    }

    rotate(data) {
        const ctrl = $("div.toolboxItem").filterByData("theme-id", data.themeId);
        if (typeof ctrl === "undefined") return;
        if (ctrl == null) return;

        const ctrlImg = ctrl.find("img");
        if (typeof ctrlImg === "undefined") return;
        if (ctrlImg == null) return;

        ctrl.data(constDataThemeDimensionIndex, data.themeDimIdx);

        ctrlImg.css({
            "transform-origin": data.transformOrigin,
            "transform": data.transform
        });

        const jsonObj = this.getJsonDataOfItem(ctrl);
        ctrl.data(constDataThemeItemObject, jsonObj);

        this.recentItem = jsonObj;
    }

    getJsonDataOfItem(toolboxItem) {
        const jsonObj = toolboxItem.data(constDataThemeItemObject);
        if (typeof jsonObj.editor === "undefined")
            jsonObj.editor = {};
        jsonObj.editor.themeId = toolboxItem.data("theme-id");
        jsonObj.editor.themeDimIdx = toolboxItem.data(constDataThemeDimensionIndex);
        return jsonObj;
    }

    showToolbox() {
        this.__toolbox.show();
    }

    hideToolbox() {
        this.__toolbox.hide();
    }

    __collapseCategory(headTitle) {
        headTitle.data("railway-collapsed", "");
        headTitle.parent().find("div.toolboxItem").each(function () {
            $(this).hide();
        });
        const img = headTitle.find('img');
        img.attr("src", "images/expand.png");
    }

    __expandCategory(headTitle) {
        headTitle.removeData("railway-collapsed");
        headTitle.parent().find("div.toolboxItem").each(function () {
            $(this).show();
        });
        const img = headTitle.find('img');
        img.attr("src", "images/collapse.png");
    }

    install() {
        const self = this;

        themeObject.forEach(item => {
            var headItem = $('<div>', {
                //text: item.category
            }).css({ })
                .addClass("toolboxHead")
                .appendTo(this.__toolbox);
            
            var headTitle = $('<div>', { text: item.category }).addClass("toolboxTitle");
            
            headTitle.click(function () {
                var collapsed = headTitle.data("railway-collapsed");
                if (typeof collapsed === "undefined" && collapsed == null)
                    collapsed = false;
                else
                    collapsed = true;

                if (collapsed) {
                    self.__expandCategory(headTitle);
                } else {
                    self.__collapseCategory(headTitle);
                }
            });

            var imgExpandCollapse = $('<img>', {
                width: "16px",
                height: "16px",
                float: "right"
            });
            //imgExpandCollapse.attr("src", "images/collapse.png");
            imgExpandCollapse.appendTo(headTitle);

            headTitle.appendTo(headItem);

            var itemContainer = $('<div>').appendTo(headItem);
            itemContainer.addClass("toolboxContainer");

            item.objects.forEach(it => {
                if (it.id != -1) {

                    if (typeof it.visible !== "undefined" && it.visible === false)
                        return;

                    const item = $('<div>', { html: "" })
                        .addClass("toolboxItem")
                        .attr({
                            draggable: "true"
                        });
                    
                    item.data(constDataThemeId, it.id);
                    item.data(constDataThemeItemObject, it);
                    
                    item.appendTo(itemContainer);

                    let icon = "";
                    if (it.active && it.active.default)
                        icon = it.active.default + ".png";
                    else
                        icon = it.basename + ".png";

                    const itemIcon = $('<img>', {})
                        .addClass("imgWithTooltip")
                        .attr({
                            "data-tipso-title": it.name,
                            //"data-tipso": it.name,
                            draggable: "false",
                            src: "theme/" + window.settings.themeName + "/" + icon
                        });

                    itemIcon.appendTo(item);
                }
            });

            // expand/collapse categories
            if (item.category === "Signal"
                || item.category === "Block") {
                this.__collapseCategory(headTitle);
            } else {
                this.__expandCategory(headTitle);
            }
        });

        function dragstart(ev) {
            const toolboxItem = $(ev.target);
            const jsonObj = self.getJsonDataOfItem(toolboxItem);
            jsonObj.editor.offsetX = ev.offsetX;
            jsonObj.editor.offsetY = ev.offsetY;

            toolboxItem.data(constDataThemeItemObject, jsonObj);

            const data = JSON.stringify(jsonObj);
            ev.dataTransfer.setData("text/plain", data);

            self.recentItem = jsonObj;
        }
        
        const toolboxItems = $('div.toolboxItem');
        for (let idx = 0; idx < toolboxItems.length; ++idx) {
            const item = toolboxItems[idx];
            item.ondragstart = dragstart;
        }

        $('.imgWithTooltip').tipso({
            size: 'tiny',
            speed: 100,
            delay: 500,
            useTitle: true,
            width: "auto",
            background: '#333333',
            titleBackground: '#333333'
        });

        this.__toolbox.resizable({
            minWidth: 125,
            autoHide: true
            //ghost: true
        });
    }
}