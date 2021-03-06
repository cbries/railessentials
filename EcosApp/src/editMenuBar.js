class EditMenuBar {

    /**
     * 
     * @param {any} options
     */
    constructor(options = {}) {
        console.log("**** construct EditMenuBar");
        this.editMenu = $('#planField > div#planFieldItemEdit');
        this.cmdRotLeft = this.editMenu.find('div.rot-left');
        this.cmdRotRight = this.editMenu.find('div.rot-right');
        this.cmdRemove = this.editMenu.find('div.delete');
        this.cmdUngroup = this.editMenu.find('div.ungroup');
        this.options = options;
    }

    /**
     *  
     * @param {any} mode 0 := rotate right, 1 := rotate left
     * @param {any} jqueryElement
     * @return true := rotation applied, false := nothing has changed
     */
    rot(mode, jqueryElement, isInitialization = false) {
        var themeJsonData = getThemeJsonData(jqueryElement);

        var themeTransformOrigin = null;
        var currentDimensionIndex = jqueryElement.data(constDataThemeDimensionIndex);
        if (typeof currentDimensionIndex === "undefined")
            currentDimensionIndex = 0;

        var dimensions = { w: 1, h: 1 };
        var noOfDimensions = 1;
        if (typeof themeJsonData.dimensions !== "undefined" && themeJsonData.dimensions.length > 0) {
            dimensions = themeJsonData.dimensions[0];
            noOfDimensions = themeJsonData.dimensions.length;
        }

        if (dimensions.w === 1 && dimensions.h === 1) {
            themeTransformOrigin = "center center";
            if (mode === 0) {
                ++currentDimensionIndex;
                if (currentDimensionIndex > 3)
                    currentDimensionIndex = 0;
            }
            else if (mode === 1) {
                --currentDimensionIndex;
                if (currentDimensionIndex < 0)
                    currentDimensionIndex = 3;
            }
        }
        else {
            themeTransformOrigin = "top left";
            if (mode === 0) {
                if (currentDimensionIndex === 1)
                    currentDimensionIndex = 0;
                else
                    currentDimensionIndex = 1;
            }
            else if (mode === 1) {
                if (currentDimensionIndex === 0)
                    currentDimensionIndex = 1;
                else
                    currentDimensionIndex = 0;
            }
        }

        jqueryElement.data(constDataThemeDimensionIndex, currentDimensionIndex);

        if (noOfDimensions > 1) {
            var currentLeft = null;
            var newLeft = null;

            var isZeroDegree = (currentDimensionIndex % 4) === 0;

            if (!isInitialization) {
                if (mode > -1) { // only move ctrl when rotation is applied
                    if (isZeroDegree) {
                        currentLeft = this.currentSelection.css("left");
                        newLeft = "calc( " + currentLeft + " - " + constItemWidth + "px)";
                        this.currentSelection.css("left", newLeft);
                    } else {
                        currentLeft = this.currentSelection.css("left");
                        newLeft = "calc( " + currentLeft + " + " + constItemWidth + "px)";
                        this.currentSelection.css("left", newLeft);
                    }
                }
            } else {
                if (isZeroDegree) {
                    // do not move the ctrl during initialization when the rotation is 0deg
                } else {
                    currentLeft = this.currentSelection.css("left");
                    newLeft = "calc( " + currentLeft + " + " + constItemWidth + "px)";
                    this.currentSelection.css("left", newLeft);
                }
            }
        }

        var themeTransform = "rotate(" + (currentDimensionIndex * 90) + "deg)";
        this.currentSelection.css("transform-origin", themeTransformOrigin);
        this.currentSelection.css("transform", themeTransform);

        if (!isInitialization) {
            this.updateEditMenuPositionAndEvents(jqueryElement);
            if (noOfDimensions === 1) {
                window.toolbox.rotate({
                    themeId: themeJsonData.id,
                    themeDimIdx: currentDimensionIndex,
                    transformOrigin: themeTransformOrigin,
                    transform: themeTransform
                });
            }
            window.serverHandling.sendControl(this.currentSelection);
        }

        return true;
    }

    setCurrentSelection(jqueryElement) {
        this.currentSelection = jqueryElement;
        this.unbindMenuBar();
        const self = this;

        this.cmdRotLeft.click(function (e) {
            e = e || window.event;
            e.preventDefault();
            e.stopPropagation();

            var res = self.rot(1, self.currentSelection);
            if (!res) return;
        });

        this.cmdRotRight.click(function (e) {
            e = e || window.event;
            e.preventDefault();
            e.stopPropagation();

            var res = self.rot(0, self.currentSelection);
            if (!res) return;
        });

        this.cmdRemove.click(function (e) {
            e = e || window.event;
            e.preventDefault();
            e.stopPropagation();
            self.hideEditMenu();

            var itemId = self.currentSelection.attr("id");
            self.currentSelection.remove();
            window.serverHandling.removeControl(itemId);
        });

        this.cmdUngroup.click(function (e) {
            e = e || window.event;
            e.preventDefault();
            e.stopPropagation();
            if (typeof window.planField === "undefined")
                return;
            window.planField.clearCtrlSelection();
        });
    }

    unbindMenuBar() {
        this.cmdRemove.unbind("click");
        this.cmdRotLeft.unbind("click");
        this.cmdRotRight.unbind("click");
    }

    updateEditMenuPositionAndEvents(jqueryElementCtrl) {
        if (typeof jqueryElementCtrl === "undefined") return;
        if (jqueryElementCtrl == null) return;

        const nativeCtrl = jqueryElementCtrl.get(0);
        const currentCtrlWidth = nativeCtrl.getBoundingClientRect().width;
        const currentCtrlX = nativeCtrl.offsetLeft;
        const currentCtrlY = nativeCtrl.offsetTop;
        const xLeftCtrl = currentCtrlX - (constItemWidth / 2);
        const xRightCtrl = currentCtrlX + currentCtrlWidth + (constItemWidth / 2);

        var listOfCtrlsInPlan = $('div.ctrlItem[id]');
        const leftCtrl = getCtrlOfPosition(xLeftCtrl, currentCtrlY + 10, listOfCtrlsInPlan);
        const rightCtrl = getCtrlOfPosition(xRightCtrl, currentCtrlY + 10, listOfCtrlsInPlan);

        var menuWidth = parseInt(this.editMenu.width());
        var menuHeight = parseInt(this.editMenu.height());

        var showLeft = true;
        if (xLeftCtrl < 0) {
            showLeft = false;
        } else {
            if (leftCtrl != null && rightCtrl == null) { showLeft = true; }
            else if (leftCtrl == null && rightCtrl == null) { showLeft = true; }
            else if (leftCtrl == null) { showLeft = false; }
        }

        if (showLeft) { // show on the left side
            const x = currentCtrlX - menuWidth - 10;
            const y = currentCtrlY - menuHeight / 3;
            this.editMenu.css({ left: x + "px", top: y + "px" });
        } else { // show on the right side
            const x = currentCtrlX + currentCtrlWidth + 5;
            const y = currentCtrlY - menuHeight / 3;
            this.editMenu.css({ left: x + "px", top: y + "px" });
        }
    }

    hideEditMenu() {
        this.unbindMenuBar();
        this.editMenu.hide();
    }

    showEditMenu() {
        this.editMenu.show();
    }
}