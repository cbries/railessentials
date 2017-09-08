/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
var TESTDATA = false;
var isEdit = false;

const ModeAddMove = 0;
const ModeRemove = 1;
const ModeRotate = 2;
const ModeObject = 3;

var editMode = ModeAddMove;

var currentSelection = "";
var currentRow = -1;
var currentColumn = -1;
var isDrag = false;

var objDrag = null;
var objPosition = null; // top, left

// callable by RailwayEssential
//   a) changeDirectionMarker(col, row, direction)   direction := 1 or 2
//   b) changeLocnameMarker(col, row, locname)
//   c) changeItemIdMarker(col, row, idname)
//   d) changeLocnameMarkerPreview(col, row, locname)
//   e) changeLocnameMarkerLock(col, row, lockState)
//   f) activateEditMode(modeindex)

function activateEditMode(modeindex) {
    switch (modeindex) {
        case 1:
            editMode = ModeAddMove;
            resetSelection();
            break;
        case 2:
            editMode = ModeRemove;
            resetSelection();
            break;
        case 3:
            editMode = ModeRotate;
            resetSelection();
            break;
        case 4:
            editMode = ModeObject;
            break;
        default:
            editMode = ModeAddMove;
            break;
    }
}

function getIdOfItemNameLbl(col, row) { return 'lbl_' + col + '_' + row + '_itemname'; }
function getIdOfDirectionLbl(col, row) { return 'lbl_' + col + '_' + row + '_direction'; }
function getIdOfLocanameLbl(col, row) { return 'lbl_' + col + '_' + row + '_locname'; }

function changeDirectionMarker(col, row, direction) {
    // direction = 1 -> Reverse / Backward
    // direction = 2 -> Forward
    try {
        var id = getIdOfDirectionLbl(col, row);
        var el = $('#' + id)[0];
        if (direction === 1)
            el.firstChild.data = 'B';
        else if (direction === 2)
            el.firstChild.data = 'F';
        else
            el.firstChild.data = ' ';
    } catch (ex) { }
}

function changeLocnameMarker(col, row, locname) {
    try {
        var id = getIdOfLocanameLbl(col, row);
        var el = $('#' + id)[0];
        el.setAttribute('stroke', 'black');
        el.setAttribute('fill', 'black');
        el.firstChild.data = locname;
    } catch (ex) { }
}

function changeLocnameMarkerPreview(col, row, locname) {
    try {
        var id = getIdOfLocanameLbl(col, row);
        var el = $('#' + id)[0];
        el.setAttribute('stroke', 'green');
        el.setAttribute('fill', 'green');
        el.firstChild.data = locname;
    } catch (ex) { }
}

function changeLocnameMarkerLock(col, row, lock) {
    try {
        var id = getIdOfLocanameLbl(col, row);
        var el = $('#' + id)[0];
        if (lock) {
            el.setAttribute('stroke', 'red');
            el.setAttribute('fill', 'red');
            //    el.setAttribute('style', 'font-style: italic; text-decoration: line-through;');			
            var img = el.parentElement.querySelectorAll('image')[1];
            img.setAttributeNS(null, 'visibility', 'visible');
        } else {
            //    el.setAttribute('stroke', 'black');
            //    el.setAttribute('fill', 'black');
            //    el.setAttribute('style', '');
            var img = el.parentElement.querySelectorAll('image')[1];
            img.setAttributeNS(null, 'visibility', 'hidden');
        }
    } catch (ex) { }
}

function changeItemIdMarker(col, row, idname) {
    try {
        var id = getIdOfItemNameLbl(col, row);
        var el = $('#' + id)[0];
        el.firstChild.data = idname;
    } catch (ex) { }
}

function isBetween(n, a, b) {
    return (n - a) * (n - b) <= 0;
}

function appendBlockText(col, row, esvg, themeId) {

    var el = null;

    // BLOCK
    if (themeId === 150 || themeId === 151 || themeId === 152) {
        el = document.createElementNS('http://www.w3.org/2000/svg', 'text');
        el.setAttribute('id', getIdOfDirectionLbl(col, row));
        el.setAttribute('x', 5);
        el.setAttribute('y', 18);
        el.setAttribute('font-size', '12px');
        el.setAttribute('stroke', 'blue');
        el.setAttribute('fill', 'blue');
        el.innerHTML = '.';
        esvg.append(el);
        changeDirectionMarker(col, row, 2);

        el = document.createElementNS('http://www.w3.org/2000/svg', 'text');
        el.setAttribute('id', getIdOfLocanameLbl(col, row));
        el.setAttribute('x', 15);
        el.setAttribute('y', 21);
        el.setAttribute('font-size', '15px');
        el.setAttribute('stroke', 'black');
        el.setAttribute('fill', 'black');
        el.innerHTML = '.';
        esvg.append(el);

        if (themeId == 150 || themeId == 152) {
            el = document.createElementNS('http://www.w3.org/2000/svg', 'image');
            el.setAttributeNS(null, 'height', '22');
            el.setAttributeNS(null, 'width', '22');
            el.setAttributeNS('http://www.w3.org/1999/xlink', 'href', 'images/lock.png');
            if (themeId == 152)
                el.setAttributeNS(null, 'x', '102');
            else
                el.setAttributeNS(null, 'x', '104');
            el.setAttributeNS(null, 'y', '5');
            el.setAttributeNS(null, 'visibility', 'hidden');
            esvg.append(el);
        } else if (themeId == 151) {
            el = document.createElementNS('http://www.w3.org/2000/svg', 'image');
            el.setAttributeNS(null, 'height', '10');
            el.setAttributeNS(null, 'width', '10');
            el.setAttributeNS('http://www.w3.org/1999/xlink', 'href', 'images/lock.png');
            el.setAttributeNS(null, 'x', '53');
            el.setAttributeNS(null, 'y', '1');
            el.setAttributeNS(null, 'visibility', 'hidden');
            esvg.append(el);
        }
    }

    // Switch
    if (isBetween(themeId, 50, 53) || isBetween(themeId, 200, 202)) {
        el = document.createElementNS('http://www.w3.org/2000/svg', 'text');
        el.setAttribute('id', getIdOfItemNameLbl(col, row));
        el.setAttribute('x', 1);
        el.setAttribute('y', 9);
        el.setAttribute('font-size', '9px');
        el.setAttribute('stroke', 'none');
        el.setAttribute('fill', 'black');
        el.innerHTML = '.';
        esvg.append(el);
    }

    // SIGNAL
    if (isBetween(themeId, 100, 108)) {
        el = document.createElementNS('http://www.w3.org/2000/svg', 'text');
        el.setAttribute('id', getIdOfItemNameLbl(col, row));
        el.setAttribute('x', 1);
        el.setAttribute('y', 29);
        el.setAttribute('font-size', '9px');
        el.setAttribute('stroke', 'none');
        el.setAttribute('fill', 'black');
        el.innerHTML = '.';
        esvg.append(el);
    }

    //if (TESTDATA && el !== 'undefined' && el != null) {
    changeDirectionMarker(col, row, 2);
    changeLocnameMarker(col, row, ' ');
    changeItemIdMarker(col, row, ' ');
    //}

    if (TESTDATA) {
        changeLocnameMarker(col, row, 'BR 10');
        changeLocnameMarkerLock(col, row, true);
    }
}

function preloadSvgsLoaded() {
    // just increment the counter if there are still images pending...
    ++counter;
    if (counter >= total) {
        // this function will be called when everything is loaded
        // e.g. you can set a flag to say "I've got all the images now"
        preloadSvgsAlldone();
    }
}

function preloadSvgsAlldone() {
    try {
        railwayEssentialCallback.message("SVGs have been loaded");
    } catch (e) {
        console.log(e);
    }
}

function preloadSvgs() {
    if (symbolFiles == null || symbolFiles === 'undefined')
        return;

    // This will load the images in parallel:
    // In most browsers you can have between 4 to 6 parallel requests
    // IE7/8 can only do 2 requests in parallel per time
    for (var i = 0; i < total; i++) {
        var img = new Image();
        // When done call the function "loaded"
        img.onload = preloadSvgsLoaded;
        // cache it
        svgCache[symbolFiles[i]] = img;
        img.src = symbolFiles[i];
    }
}

function showImage(url, id) {
    // get the image referenced by the given url
    var cached = svgCache[url];
    // and append it to the element with the given id
    document.getElementById(id).appendChild(cached);
}

$(document).keyup(function (e) {
    if (e.keyCode == 27) {
        resetSelection();
    }
});

function updateUi() {

    var o = $('#editMenu');

    if (isEdit) {

        o.show();

        $('td').each(function () {
            var img = $(this).find('svg');
            if (img.length == 1)
                img.parent().draggable({ disabled: false });
        });

        $('.cell').each(function () {
            $(this).css("border", "1px solid rgba(178, 179, 179, 0.2)");
        });
    }
    else {

        o.hide();

        $('td').each(function () {
            var img = $(this).find('svg');
            if (img.length == 1)
                img.parent().draggable({ disabled: true });
        });

        $('.cell').each(function () {
            $(this).css("border", "");
        });
    }
}

function rebuildCell(col, row) {
    var oel = $('#td_' + col + '_' + row);
    if (oel != null) {
        oel.html("<div class=\"overflow\"></div>");
    }
}

function rebuildTable() {
    $('td').each(function (index, el) {
        if ($(el).find('svg').length == 0) {
            var col = $(el).parent().children().index($(el)) + 1;
            var row = $(el).parent().parent().children().index($(el).parent()) + 1;
            try {
                railwayEssentialCallback.cellEdited(col, row, -1);
            } catch (ex) { /* ignore */ }
            $(el).html("<div class=\"overflow\"></div>");
        }
    });
}

function findTargetTd(evt, callback) {

    var clientX = evt.clientX;
    var clientY = evt.clientY;

    $('td').each(function (index, el) {

        if (objDrag == null)
            return;

        var position = $(el).position();

        var w = $(el).width();
        var h = $(el).height();

        var x0 = position.left;
        var x1 = x0 + w;
        var y0 = position.top;
        var y1 = y0 + h;

        if (clientX >= x0 && clientX <= x1) {
            if (clientY >= y0 && clientY <= y1) {
                var o = $(el);
                var col = o.parent().children().index(o) + 1;
                var row = o.parent().parent().children().index(o.parent()) + 1;

                callback(col, row, $(el));

                return $(el);
            }
        }
    });

    return null;
}

function resetSelection() {
    $('td').each(function () {
        $(this).css("background-color", "");
    });
    try {
        railwayEssentialCallback.cellSelected(-1, -1);
    } catch (ex) { /* ignore */ }
}

function selectElement(col, row, el) {
    resetSelection();
    el.parent().css("background-color", "red");
    try {
        railwayEssentialCallback.cellSelected(col, row);
    } catch (ex) { /* ignore */ }
}

function rotateElement2(col, row, el) {

    if (!isEdit)
        return;

    var o = el;

    function ss(col, row, orientation) {
        //console.log("vs: cellRotated(" + col + ", " + row + ", " + orientation + ")");
        try {
            railwayEssentialCallback.cellRotated(col, row, orientation);
        } catch (ex) { /* ignore */ }
    }

    if (o.hasClass('imgflip')) {
        // links oben
        o.removeClass('imgflip');
        o.addClass('rot90');
        ss(col, row, "rot90");
    } else if (o.hasClass('rot90')) {
        // rechts oben
        o.removeClass('rot90');
        o.addClass('rot180');
        ss(col, row, "rot180");
    } else if (o.hasClass('rot180')) {
        // rechts unten
        o.removeClass('rot180');
        o.addClass('rot-90');
        ss(col, row, "rot-90");
    } else if (o.hasClass('rot-90')) {
        // links unten
        o.removeClass('rot-90');
        o.addClass('rot0');
        ss(col, row, "rot0");
    } else {
        // fallback, links oben
        o.addClass('rot90');
        ss(col, row, "rot90");
    }
}

function rotateElement(col, row, el) {
    rotateElement2(col, row, el);
    return;
}

function test(col, row) {
    console.log(col + ", " + row);
}

function highlightRoute(jsonArray, styleName) {
    //console.log("Highlight Route");

    for (var i = 0; i < jsonArray.length; ++i) {
        var o = jsonArray[i];
        if (o == null || o === 'undefined')
            continue;

        var col = o.col;
        var row = o.row;

        var oel = $('#td_' + col + '_' + row);
        oel.addClass(styleName);
    }
}

function resetHighlightRoute() {
    //console.log("Reset Highlight Route");
    $('td').each(function () {
        $(this).removeClass("routeHighlightStart");
        $(this).removeClass("routeHighlightEnd");
        $(this).removeClass("routeHighlight");
    });
}

function changeSymbol(col, row, themeId, orientation, symbol) {
    var oel = $('#td_' + col + '_' + row);
    var cdiv = oel.find("div");
    if (cdiv.find("svg").length === 0)
        return;
    try {
        var m = "";
        m += "Change Coord(" + col + ", " + row + "): " + themeId + ", " + orientation + ", " + symbol;
        railwayEssentialCallback.message(m);
    } catch (e) {
        console.log(e);
    }
    rebuildCell(col, row);
    simulateClick(col, row, themeId, symbol, orientation, false);
}

function simulateClick2(jsonArray) {
    for (var i = 0; i < jsonArray.length; ++i) {
        var o = jsonArray[i];
        if (o == null || o === 'undefined')
            continue;

        var col = o.col;
        var row = o.row;
        var themeId = o.themeId;
        var symbol = o.symbol;
        var orientation = o.orientation;
        var response = false;

        simulateClick(col, row, themeId, symbol, orientation, response);
    }
}

function simulateClick(col, row, themeid, symbol, orientation, response) {

    if (response == null || response === 'undefined')
        response = false;

    var oel = $('#td_' + col + '_' + row);

    var cdiv = oel.find("div");
    if (cdiv.find("svg").length === 1)
        return;

    var v = themeDirectory + '/' + symbol + '.svg';

    if (response) {
        try {
            var m = "";
            m += "Coord(" + col + ", " + row + "): " + symbol + ", " + orientation + ", " + themeid + ", " + v;
            railwayEssentialCallback.message(m);
        } catch (e) {
            console.log(e);
        }
    }

    //var img = $(svgCache[v]).clone();
    var img = null;
    try {
        img = window.atob(symbolFilesBase64[symbol]);

        var newChild = cdiv.append(img);
        newChild.addClass("overflow");
        newChild.addClass(orientation);
        newChild.attr("border", 0);
        newChild.data("railway-themeid", themeid);
        newChild.data("src", v);
        var svgChild = newChild.find("svg");
        appendBlockText(col, row, svgChild, themeid);

        newChild.mousedown(function (evt) {

            if (isEdit) {
                switch (evt.which) {
                    case 2: // middle button
                        break;
                    case 3: // right button
                        rotateElement(col, row, $(this));
                        return;
                }
            }

            switch (editMode) {
                case ModeRotate:
                    if (isEdit) {
                        rotateElement(col, row, $(this));
                    }
                    break;

                case ModeRemove:
                    if (isEdit) {
                        $(this).remove();
                        resetSelection();
                        rebuildTable();
                    }
                    break;

                case ModeObject:
                    if (isEdit) {
                        selectElement(col, row, $(this));
                    }
                    break;
            }
        });

        newChild.draggable();
    } catch (e) {
        console.log(e);
    }

}

function handleUserClick(col, row) {
    //console.log("vs: cellClicked(" + col + ", " + row + ")");
    try {
        railwayEssentialCallback.cellClicked(col, row);
    } catch (ex) { /* ignore */ }
}

function changeEditMode(state) {
    if (state == null || state === 'undefined')
        isEdit = !isEdit;
    else
        isEdit = state;

    if (!isEdit) {
        resetSelection();
        rebuildTable();
    }

    try {
        railwayEssentialCallback.editModeChanged(isEdit);
    } catch (ex) { /* ignore */ }

    updateUi();
}

$(document).ready(function (e) {

    var isMouseDown = false;
    var isDragging = false;
    var startingPos = [];

    var currentCategory = "Track";

    //$('#webmenuDivTrack').hide();
    $('#webmenuDivSwitch').hide();
    $('#webmenuDivSignal').hide();
    $('#webmenuDivBlock').hide();
    $('#webmenuDivSensor').hide();
    $('#webmenuDivAccessory').hide();

    $('#webmenuCategories').change(function () {
        $('#webmenuDivTrack').hide();
        $('#webmenuDivSwitch').hide();
        $('#webmenuDivSignal').hide();
        $('#webmenuDivBlock').hide();
        $('#webmenuDivSensor').hide();
        $('#webmenuDivAccessory').hide();

        var cname = $(this).val();

        var sel = "#webmenuDiv" + cname;
        var oo = $(sel);
        currentCategory = oo.val();
        oo.show();
    });

    $("td")
        .mousedown(function (evt) {
            isDragging = false;
            isMouseDown = true;
            startingPos = [evt.pageX, evt.pageY];
            objDrag = $(this).find("svg");
        })
        .mousemove(function (evt) {
            if (!(evt.pageX === startingPos[0] && evt.pageY === startingPos[1])) {
                isDragging = true;

                if (isMouseDown) {
                    // ignore
                }
            }
        })
        .mouseup(function (evt) {
            isMouseDown = false;

            var col = $(this).parent().children().index($(this)) + 1;
            var row = $(this).parent().parent().children().index($(this).parent()) + 1;

            if (isDragging && objDrag !== null) {

                // ###################
                //        DROP
                // ###################

                if (!isEdit)
                    return;

                var targetObject = findTargetTd(evt, function (col, row, target) {
                    var src = objDrag.data("src");
                    if (src === 'undefined' || src == null)
                        src = objDrag.parent().data("src");
                    if (src === 'undefined' || src == null)
                        return;

                    var themeId = objDrag.data("railway-themeid");
                    if (themeId === null || typeof themeId == 'undefined')
                        themeId = objDrag.parent().data("railway-themeid");

                    var rotClass = "";
                    if (objDrag.parent().hasClass("rot0"))
                        rotClass = "rot0";
                    if (objDrag.parent().hasClass("rot90"))
                        rotClass = "rot90";
                    if (objDrag.parent().hasClass("rot180"))
                        rotClass = "rot180";
                    if (objDrag.parent().hasClass("rot-90"))
                        rotClass = "rot-90";

                    objDrag.remove();
                    objDrag = null;

                    resetSelection();
                    rebuildTable();

                    var c = target.find("div");
                    if (c.find("svg").length == 1)
                        return;

                    //var img = $(svgCache[src]).clone();
                    var symbol = src.substring(src.lastIndexOf('/') + 1);
                    symbol = symbol.substring(0, symbol.lastIndexOf('.'));
                    var img = window.atob(symbolFilesBase64[symbol]);
                    var newChild = c.append(img);
                    newChild.addClass("overflow");
                    newChild.addClass(rotClass);
                    newChild.attr("border", 0);
                    newChild.data("railway-themeid", themeId);
                    newChild.data("src", src);
                    var svgChild = newChild.find("svg");
                    appendBlockText(col, row, svgChild, themeId);

                    newChild.mousedown(function (evt) {

                        if (isEdit) {
                            switch (evt.which) {
                                case 2: // middle button
                                    break;
                                case 3: // right button
                                    rotateElement(col, row, $(this));
                                    return;
                            }
                        }

                        switch (editMode) {
                            case ModeRotate:
                                if (isEdit) {
                                    rotateElement(col, row, $(this));
                                }
                                break;

                            case ModeRemove:
                                if (isEdit) {
                                    $(this).remove();
                                    resetSelection();
                                    rebuildTable();
                                }
                                break;

                            case ModeObject:
                                if (isEdit) {
                                    selectElement(col, row, $(this));
                                }
                                break;
                        }

                    });
                    newChild.draggable();

                    //console.log("vs: #1 cellEdited(" + col + ", " + row + ", " + themeId + ")");
                    try {
                        railwayEssentialCallback.cellEdited(col, row, themeId);
                    } catch (ex) { /* ignore */ }
                });

            } else {

                // ###################
                //        CLICK
                // ###################

                objDrag = null;

                var c = $(this).find("div");
                if (c.find("svg").length == 1) {
                    if (isEdit)
                        return;

                    handleUserClick(col, row);
                    return;
                }

                if (!isEdit) {
                    handleUserClick(col, row);
                    return;
                }

                if (editMode != ModeAddMove)
                    return;

                var cname = $('#webmenuCategories').val();
                var sel = $('#webmenu' + cname);
                var o = sel.val();
                var o2 = sel.find(':selected').data("railway-themeid");
                var v = themeDirectory + '/' + o + '.svg';

                //var img = $(svgCache[v]).clone();
                var img = window.atob(symbolFilesBase64[o]);
                var newChild = c.append(img);
                newChild.addClass("overflow");
                newChild.attr("border", 0);
                newChild.data("railway-themeid", o2);
                newChild.data("src", v);
                var svgChild = newChild.find("svg");
                appendBlockText(col, row, svgChild, o2);

                newChild.mousedown(function (evt) {

                    if (isEdit) {
                        switch (evt.which) {
                            case 2: // middle button
                                break;
                            case 3: // right button
                                rotateElement(col, row, $(this));
                                return;
                        }
                    }

                    switch (editMode) {
                        case ModeRotate:
                            if (isEdit) {
                                rotateElement(col, row, $(this));
                            }
                            break;

                        case ModeRemove:
                            if (isEdit) {
                                $(this).remove();
                                resetSelection();
                                rebuildTable();
                            }
                            break;

                        case ModeObject:
                            if (isEdit) {
                                selectElement(col, row, $(this));
                            }
                            break;
                    }
                });

                newChild.draggable();

                //console.log("vs: #2 cellEdited(" + col + ", " + row + ", " + o2 + ")");
                try {
                    railwayEssentialCallback.cellEdited(col, row, o2);
                } catch (ex) { /* ignore */ }
            }
            isDragging = false;
            startingPos = [];
        });

    try {
        var options = {
            visibleRows: 7,
            rowHeight: 15,
            roundedCorner: false,
            animStyle: 'none',
            enableAutoFilter: true
        };

        $('#webmenuCategories').msDropDown(options);
        $('#webmenuTrack').msDropDown(options);
        $('#webmenuSwitch').msDropDown(options);
        $('#webmenuSignal').msDropDown(options);
        $('#webmenuBlock').msDropDown(options);
        $('#webmenuSensor').msDropDown(options);
        $('#webmenuAccessory').msDropDown(options);
    } catch (e) {
        railwayEssentialCallback.message(e.message);
    }

    updateUi();
});
