function getRouteByName(routeName) {
    if (typeof routeName === "undefined" || routeName == null || routeName.length === 0)
        return null;
    if (typeof window.routes === "undefined" || window.routes == null)
        return null;
    for (let i = 0; i < window.routes.length; ++i) {
        const r = window.routes[i];
        if (r.name === routeName) {
            return r;
        }
    }
    return null;
}

function getWidthOfText(txt, fontname, fontsize) {
    if (getWidthOfText.c === undefined) {
        getWidthOfText.c = document.createElement('canvas');
        getWidthOfText.ctx = getWidthOfText.c.getContext('2d');
    }
    var fontspec = fontsize + ' ' + fontname;
    if (getWidthOfText.ctx.font !== fontspec)
        getWidthOfText.ctx.font = fontspec;
    return getWidthOfText.ctx.measureText(txt).width;
}

function addJqueryExtensions() {
    $.fn.filterByData = function (prop, val) {
        return this.filter(
            function () { return $(this).data(prop) == val; }
        );
    }
}

//If you write your own code, remember hex color shortcuts (eg., #fff, #000)
/*
returned value: (String)
rgba(251,175,255,1)
hexToRgbA('#fbafff')
*/
function hexToRgbA(hex) {
    if (hex.charAt(0) !== '#')
        hex = '#' + hex;
    var c;
    if (/^#([A-Fa-f0-9]{3}){1,2}$/.test(hex)) {
        c = hex.substring(1).split('');
        if (c.length === 3) {
            c = [c[0], c[0], c[1], c[1], c[2], c[2]];
        }
        c = '0x' + c.join('');
        //return 'rgba('+[(c>>16)&255, (c>>8)&255, c&255].join(',')+',1)';
        return {
            r: (c >> 16) & 255,
            g: (c >> 8) & 255,
            b: c & 255,
            w: 1023
        };
    }
    throw new Error('Bad Hex');
}

function toggleAllLocomotiveInformation(state) {
    const infos = $('div.locomotiveInfo');
    let i;
    const iMax = infos.length;
    for (i = 0; i < iMax; ++i) {
        if (state === true) {
            $(infos[i]).show();
        } else {
            $(infos[i]).hide();
        }
    }
}

function toggleAllLabelInformation(state) {
    $.localStorage.setItem("labelShown", JSON.stringify({
        shown: state
    }));
    const lbls = $('div.elementLabel');
    let i;
    const iMax = lbls.length;
    for (i = 0; i < iMax; ++i) {
        if (state === true) {
            $(lbls[i]).show();
        } else {
            $(lbls[i]).hide();
        }
    }
}

function toggleAllWebcamsVisibility(state) {
    $.localStorage.setItem("webcamShown", JSON.stringify({
        shown: state
    }));
    const videoCtrls = $('div.videoStream');
    let i;
    const iMax = videoCtrls.length;
    for (i = 0; i < iMax; ++i) {
        if (state === true) {
            $(videoCtrls[i]).show();
        } else {
            $(videoCtrls[i]).hide();
        }
    }
}

/**
 * Returns of an object of the accessories addressed by
 * ecosAddr. The planField item of ecosAddr can have
 * assigned two DCC addresses (e.g. 4-way-switch, specific
 * signals). In this case the object field `ecosAddresses`
 * contains the two addresses.
 * @param {any} accCtrls
 * @param {any} ecosAddr
 */
function getAccByEcosAddr(accCtrls, ecosAddr) {
    if (typeof accCtrls === "undefined" || accCtrls == null) return null;
    if (typeof ecosAddr === "undefined" || ecosAddr == null) return null;

    /*
     addresses:
        Addr1: 0
        Addr2: 17
        Inverse1: true
        Inverse2: false
        Port1: 1
        Port2: 1
     */

    const max = accCtrls.length;
    for (let i = 0; i < max; ++i) {
        const acc = $(accCtrls[i]);
        const data = acc.data(constDataThemeItemObject);
        if (data == null) continue;

        const addr = data.addresses;

        if (typeof addr === "undefined") continue;
        if (addr == null) continue;

        const addrEcos1 = ((addr.Addr1 - 1) * 4) + addr.Port1;
        const addrEcos2 = ((addr.Addr2 - 1) * 4) + addr.Port2;

        if (ecosAddr === addrEcos1 || ecosAddr === addrEcos2) {
            const valid1 = parseInt(addr.Addr1) > 0 && parseInt(addr.Port1) > 0;
            const valid2 = parseInt(addr.Addr2) > 0 && parseInt(addr.Port2) > 0;

            return {
                "planItem": acc,
                "themeData": data,
                "ecosAddresses": [{
                    "ecosAddr": addrEcos1,
                    "ecosAddrValid": valid1,
                    "inverse": addr.Inverse1
                }, {
                    "ecosAddr": addrEcos2,
                    "ecosAddrValid": valid2,
                    "inverse": addr.Inverse2
                }
                ]
            };
        }
    }

    return null;
}

/**
 * Returns of an object of the feedback/sensor addressed by ecosAddr. 
 * @param {any} accCtrls
 * @param {any} ecosAddr
 */
function getFbByEcosAddr(fbCtrls, ecosAddr) {
    if (typeof fbCtrls === "undefined") return null;
    if (fbCtrls == null) return null;
    if (typeof ecosAddr === "undefined") return null;

    /*
      addresses:
        Addr: INT
     */

    const max = fbCtrls.length;
    for (let i = 0; i < max; ++i) {
        const fb = $(fbCtrls[i]);
        const data = fb.data(constDataThemeItemObject);
        if (data == null) continue;
        if (typeof data.addresses === "undefined") continue;
        if (data.addresses == null) continue;
        const addr = data.addresses.Addr;
        if (addr === ecosAddr)
            return {
                "planItem": fb,
                "themeData": data,
                "ecosAddr": {
                    "Addr": data.addresses.Addr
                }
            };
    }

    return null;
}

/**
 * Of
 *   "../../hello/world/ries.jpg"
 * it will return:
 *   "../../hello/world/"
 * @param {any} pathToFile
 */
function getDirpathOf(pathToFile) {
    const lastIdx = pathToFile.replace("\\", "/").lastIndexOf('/');
    const dirpath = pathToFile.substr(0, lastIdx + 1);
    return dirpath;
}

function getLabelOffsetBy(jsonData) {
    const themeId = jsonData.editor.themeId;
    const offset = {
        "font-size": "8px",
        "z-index": 10000,
        "position": "relative",
        "top": "-12px",
        "left": "0px"
    };

    switch (themeId) {
        case 10: return null;
        case 11: return null;
        case 13: return null;
        case 14: return null;
        case 17: return null;
        case 18: return null;
        case 19: return null;
        case 1013: return null;
    }

    return offset;
}

function getArrayOfFunctions2(oid, noOfFunctions, funcset, funcdesc) {
    return getArrayOfFunctions({
        oid: oid,
        noOfFunctions: noOfFunctions,
        funcset: funcset,
        funcdesc: funcdesc
    });
}

function getArrayOfFunctions(record) {
    const availableFuncsFromLeftToRight = [];

    const oid = record.oid;
    const maxFnc = record.noOfFunctions;
    const funcset = record.funcset;
    const funcdescArray = record.funcdesc;

    var usedCounter = 0;
    for (let i = 0; i < funcset.length; ++i) {
        const funcdesc = funcdescArray[i];

        if (typeof funcdesc === "undefined" || funcdesc == null) continue;
        if (typeof funcdesc.idx === "undefined" || funcdesc.idx == null) continue;

        const idx = funcdesc.idx;
        const type = funcdesc.type;
        if (type === 0) continue;

        const o = {
            oid: oid,
            idx: idx,
            state: parseInt(funcset[idx]) === 1,
            type: type
        };

        availableFuncsFromLeftToRight.push(o);

        usedCounter++;
        if (usedCounter >= maxFnc)
            break;
    }
    return availableFuncsFromLeftToRight;
}

var _htmlEntities = (function (str) {
    return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
});

window.__cachedLocomotiveImages = {};

function loadLocomotiveImageIntoHtml(targetImageId, locname, percentage = "100%") {
    if (typeof window.__cachedLocomotiveImages[locname] !== "undefined" &&
        window.__cachedLocomotiveImages[locname] != null) {
        const targetImg = $('img#' + targetImageId);
        targetImg.get(0).src = window.__cachedLocomotiveImages[locname];
    }

    const newImg = new Image;
    if (typeof percentage === "undefined" || percentage == null)
        percentage = "100%";

    newImg.onload = function () {
        const targetImg = $('img#' + targetImageId);
        targetImg.get(0).src = this.src;
        targetImg.css({
            width: percentage
        });
        window.__cachedLocomotiveImages[locname] = this.src;
    }
    locname = locname.replace("/", "_");
    locname = locname.replace(".", "_");
    const url = './images/locomotives/' + locname + ".png";
    newImg.src = url;
}

/**
 * Queries and returns the control under coord(x, y).
 * @param x x-coord to check for a ctrl 
 * @param y y-coord to check for a ctrl
 * @param listOfCtrls The list of controls in which the search is done.
 */
function getCtrlOfPosition(x, y, listOfCtrls) {
    if (x == null) return null;
    if (y == null) return null;
    if (listOfCtrls == null) return null;
    let i;
    const iMax = listOfCtrls.length;
    for (i = 0; i < iMax; ++i) {
        const c = listOfCtrls[i];
        if (c === "undefined" || c === null) continue;
        const yStart = c.offsetTop;
        const xStart = c.offsetLeft;
        const clientRect = c.getBoundingClientRect();
        const w = clientRect.width; // $(this).width();
        const h = clientRect.height; // $(this).height();
        const xEnd = xStart + w;
        const yEnd = yStart + h;

        if (x >= xStart && x <= xEnd
            && y >= yStart && y <= yEnd) {
            return $(c);
        }
    }
    return null;
}

function getCtrlOfCoord(x, y, listOfCtrls) {
    if (x == null) return null;
    if (y == null) return null;
    if (listOfCtrls == null) return null;
    let i;
    const iMax = listOfCtrls.length;
    for (i = 0; i < iMax; ++i) {
        const c0 = $(listOfCtrls[i]);
        if (typeof c0 === "undefined" || c0 === null) continue;
        const c0data = c0.data(constDataThemeItemObject);
        if (typeof c0data === "undefined" || c0data == null) continue;
        const c = c0data.coord;
        if (typeof c === "undefined" || c == null) return null;
        if (c.x === x && c.y === y) return c;
    }
    return null;
}

/**
 * Queries and returns the control under the ev position.
 * @param ctx jQuery element used to calculate the relative position.
 * @param ev Mostly a mouse event. Web reference: https://api.jquery.com/Types/#Event
 * @param listOfCtrls The list of controls in which the search is done.
 */
function getCtrlOfEvent(ctx, ev, listOfCtrls) {
    return getCtrlOfPosition(
        ev.pageX - ctx.get(0).offsetLeft,
        ev.pageY - ctx.get(0).offsetTop,
        listOfCtrls);
};

/**
 * Returns the JSON {"x": .., "y": ..} coord of the click.
 * Thats not the mouse coord, it is a index-based coord, e.g.
 * {"x":0,"y":0} := first field
 * {"x":5,"y":2} := field of column 6 and row 3
 * The field size is defined by the global constants:
 *   constItemWidth
 *   constItemHeight
 * @param ctx jQuery element of the click area.
 * @param e mouse event, e.g. document.onmousemove
 */
function getElementCoord(ctx, e) {
    const c = ctx.get(0);
    const ctxPosY = c.offsetTop;
    const ctxPosX = c.offsetLeft;
    const x = e.pageX - ctxPosX;
    const y = e.pageY - ctxPosY;
    var coordX = (x / constItemWidth);
    var coordY = (y / constItemHeight);
    coordX = Math.floor(coordX);
    coordY = Math.floor(coordY);
    if (coordX < 0) coordX = 0;
    if (coordY < 0) coordY = 0;
    return { x: coordX, y: coordY };
}

function getElementStartCoord(ctx, xx, yy) {
    const ctxPosX = ctx.get(0).offsetLeft;
    const ctxPosY = ctx.get(0).offsetTop;
    const x = xx - ctxPosX;
    const y = yy - ctxPosY;
    var coordX = (x / constItemWidth);
    var coordY = (y / constItemHeight);
    coordX = Math.floor(coordX);
    coordY = Math.floor(coordY);
    if (coordX < 0) coordX = 0;
    if (coordY < 0) coordY = 0;
    return { x: coordX, y: coordY };
}

/**
 * Calculates the pixel coordinates of the index-based coord
 * returned be getElementCoord()
 * @param {any} coord
 * @see getElementCoord
 */
function coord2pixel(coord) {
    if (coord == null) return;
    return {
        x: coord.x * constItemWidth,
        y: coord.y * constItemHeight
    };
}

/**
 * 
 * @param {any} jqueryElement
 */
function getThemeJsonData(jqueryElement) {
    if (jqueryElement == null) return null;
    return jqueryElement.data(constDataThemeItemObject);
}

function getThemeJsonDataById(ctrlId) {
    // ctrlItemFeedback
    const fbCtrls = $('div.ctrlItemFeedback');
    for (let i = 0; i < fbCtrls.length; ++i) {
        const c = fbCtrls[i];
        const cid = $(c).attr("id");
        if (cid === ctrlId)
            return getThemeJsonData($(c));
    }
    return null;
}

function setThemeJsonDataById(ctrlId, data) {
    // ctrlItemFeedback
    const fbCtrls = $('div.ctrlItemFeedback');
    for (let i = 0; i < fbCtrls.length; ++i) {
        const c = fbCtrls[i];
        const cid = $(c).attr("id");
        if (cid === ctrlId) {
            $(c).data(constDataThemeItemObject, data);
            return true;
        }
    }
    return false;
}


/**
 * 
 * @param {any} jqueryElement
 */
function getItemRenderedSize(jqueryElement) {
    const defaultResult = { w: constItemWidth, h: constItemHeight };
    const jsonData = getThemeJsonData(jqueryElement);
    if (jsonData == null)
        return defaultResult;
    if (typeof jsonData.dimensions === "undefined")
        return defaultResult;
    var themeDimIdx = jqueryElement.data(constDataThemeDimensionIndex);
    if (typeof themeDimIdx === "undefined")
        themeDimIdx = 0;
    const dimension = jsonData.dimensions[themeDimIdx];
    return {
        w: constItemWidth * dimension.w,
        h: constItemHeight * dimension.h
    };
}

function isSignal(themeId) {
    if (!themeId) return false;
    if (themeId >= 100 && themeId <= 125)
        return true;
    return false;
}

function isSwitchOrAccessory(themeId) {
    if (!themeId) return false;
    if (themeId >= 50 && themeId < 150
        && (themeId !== 54 && themeId !== 55))
        return true;
    return false;
}

function isAccessory(themeId) {
    if (!themeId) return false;

    // Bahnübergänge
    if (themeId >= 250 && themeId <= 260)
        return true;

    return false;
}

function isDecoupler(themeId) {
    if (!themeId) return false;
    // decoupler
    if (themeId === 70) return true;
    return false;
}

function isBlock(themeId) {
    if (themeId === 150) return true;
    if (themeId === 151) return true;
    if (themeId === 152) return true;
    return false;
}

function isFeedback(themeId) {
    if (!themeId) return false;
    if (themeId >= 200 && themeId <= 210)
        return true;
    return false;
}