class TextfieldElement {
    constructor() {
        console.log("**** construct TextfieldElement");

        window.__currentTextEditorElement = null;

        this.__tbId = "textfieldElementToolbar";
        this.__uniqueId = null;
        this.__element = null;

        this.__initialText = "TEXT";
        this.__initialFontSize = "14px";
    }

    setEditMode(state) {
        if (state) {
            /*this.__element.attr("contenteditable", true);*/
            this.__element.resizable("enable");
        } else {
            this.__element.attr("contenteditable", false);
            this.__element.resizable("disable");
        }
    }

    /**
     * 
     * @param {any} options
     * options.fontSize
     */
    install(options = {}) {
        if (typeof options.fontSize !== "undefined")
            this.__initialFontSize = options.fontSize;

        if (typeof options.uniqueId !== "undefined")
            this.__uniqueId = options.uniqueId;
        else
            this.__uniqueId = null;

        this.__initEditorToolbar();

        var tplEl = $('.tplElEditorRoot');
        var el = tplEl.clone();
        el.removeClass("tplElEditorRoot");
        el.addClass("elEditorRoot");
        this.__element = el;
        if(this.__uniqueId == null)
            this.__uniqueId = window.planField.generateUniqueItemIdentifier();
        el.attr('id', this.__uniqueId);
        el.show();
        this.__element.resizable({
            grid: constItemWidth
        });
        this.__initEditorForTextElements();
        this.__alignToolbarToEditor(el);
        this.setEditMode(false);
    }
    
    __initEditorToolbar() {
        const self = this;

        this.__selectedSizeIdx = "idSize14";
        switch (this.__initialFontSize) {
            case "10px":
                this.__selectedSizeIdx = "idSize10";
                break;
            case "11px":
                this.__selectedSizeIdx = "idSize11";
                break;
            case "12px":
                this.__selectedSizeIdx = "idSize12";
                break;
            case "13px":
                this.__selectedSizeIdx = "idSize13";
                break;
            case "14px":
                this.__selectedSizeIdx = "idSize14";
                break;
            case "15px":
                this.__selectedSizeIdx = "idSize15";
                break;
            case "16px":
                this.__selectedSizeIdx = "idSize16";
                break;
        }

        if (typeof window.__editorToolboxInitialized === "undefined")
            window.__editorToolboxInitialized = false;
        if (window.__editorToolboxInitialized) return;
        window.__editorToolboxInitialized = true;

        $('#' + this.__tbId).w2toolbar({
            name: 'toolbarFont',
            style: "position: absolute; display: none;",
            items: [
                { id: 'cmdRemove', type: 'button', icon: 'fas fa-trash-alt' },
                { type: 'break' },
                { id: 'cmdBold', type: 'button', icon: 'fas fa-bold' },
                { id: 'cmdItalic', type: 'button', icon: 'fas fa-italic' },
                { id: 'cmdUnderline', type: 'button', icon: 'fas fa-underline' },
                {
                    type: 'menu-radio',
                    id: 'fontSize',
                    text: function (item) {
                        var el = this.get('fontSize:' + item.selected);
                        return el.text;
                    },
                    selected: self.__selectedSizeIdx,
                    items: [
                        { id: 'idSize10', text: '10px' },
                        { id: 'idSize11', text: '11px' },
                        { id: 'idSize12', text: '12px' },
                        { id: 'idSize13', text: '13px' },
                        { id: 'idSize14', text: '14px' },
                        { id: 'idSize15', text: '15px' },
                        { id: 'idSize16', text: '16px' }
                    ]
                },
                { id: 'cmdBackgroundColor', type: 'color' },
                { id: 'cmdTextColor', type: 'text-color', transparent: false }
            ],
            onClick: function (event) {
                if (event.color != null) {
                    event.done(function () {
                        if (window.__currentTextEditorElement == null)
                            return;

                        switch (event.item.type) {
                            case "color":
                                document.execCommand("hilitecolor", false, event.item.color);
                                break;
                            case "text-color":
                                document.execCommand("foreColor", false, event.item.color);
                                break;
                        }
                    });
                }
                else if (event.object !== null && typeof event.object !== "undefined") {

                    switch (event.object.id) {
                        case "cmdRemove": { 
                            var ctxRootEditorElement = window.__currentTextEditorElement;

                            ctxRootEditorElement.off('DOMSubtreeModified');
                            ctxRootEditorElement.find('.elEditor').off('DOMSubtreeModified');
                            $('#' + self.__tbId).hide();

                            var uniqueId = ctxRootEditorElement.attr("id");
                            window.serverHandling.removeControl(uniqueId);
                            ctxRootEditorElement.remove();
                            removeTextfieldFromGlobalList(uniqueId);
                        }
                        break;

                        case "cmdBold":
                            document.execCommand("bold", false, null);
                            break;

                        case "cmdItalic":
                            document.execCommand("italic", false, null);
                            break;

                        case "cmdUnderline":
                            document.execCommand("underline", false, null);
                            break;
                    }
                } else if (event.item !== null && typeof event.item !== "undefined") {
                    var ctxRootEditorElement = window.__currentTextEditorElement;
                    var fontSize = w2ui.toolbarFont.get(event.target).text;
                    ctxRootEditorElement.find('.elEditor').css({ "font-size": fontSize });
                    window.serverHandling.sendControl(ctxRootEditorElement);
                }
            }
        });
    }

    __initEditorForTextElements() {
        const self = this;

        this.setEditMode(window.planField.isEditMode);

        var el = this.__element;

        var fncQueryTxt = function (ev) {
            if (typeof ev.target === "undefined") return;
            if (ev.target === null) return;

            var currentTarget = ev.currentTarget;
            if (currentTarget) {
                if ($(currentTarget).hasClass('elEditorRoot')) {
                    window.serverHandling.sendControl($(currentTarget));
                    return;
                }
            }

            var target = $(ev.target);
            var rootEl = target.parent('.elEditorRoot');
            if(rootEl)
                window.serverHandling.sendControl(rootEl);
        }

        var handleDomChanges = (function (jqueryEl, state) {
            if (state) {
                jqueryEl.on('DOMSubtreeModified', fncQueryTxt);
            }
            else {
                jqueryEl.off('DOMSubtreeModified', fncQueryTxt);
            }
        });
        
        el.dblclick(function (ev) {
            if (!window.planField.isEditMode)
                return;

            self.__element.attr("contenteditable", true);
            $(this).focus();

            handleDomChanges($(this), true);

            var tb = $('#' + self.__tbId);
            tb.show();

            self.__alignToolbarToEditor(self.__element);

            window.__currentTextEditorElement = $(this);

            w2ui['toolbarFont'].refresh();
            $('#' + self.__tbId).find('.w2ui-scroll-right').css("display", "none");
            $('#tb_toolbarFont_right').css("display", "none");
        });

        el.keydown(function (e) {
            if (!e.shiftKey) {
                if (e.keyCode === constKeyEnter || e.keyCode === constKeyEscape) {
                    e.stopPropagation();
                    e.preventDefault();

                    $('#' + self.__tbId).hide();
                    var txtItems = $('.elEditorRoot');
                    txtItems.each(function (ev) {
                        $(this).blur();
                        handleDomChanges($(this).find('.elEditor'), false);
                        $(this).attr("contenteditable", false);
                    });
                }
            }
        });

        el.resize(function (ev) {
            var w = $(this).width();
            var h = $(this).height();
            self.__element.find('.elEditor').css({
                width: w + "px",
                height: h + "px"
            });

            var ctxRootEditorElement = window.__currentTextEditorElement;
            if (!ctxRootEditorElement)
                ctxRootEditorElement = self.__element;
            window.serverHandling.sendControl(ctxRootEditorElement);
        });

        var elEditor = el.find('.elEditor');
        elEditor.css({ "font-size": this.__initialFontSize });
        elEditor.html(this.__initialText);
    }

    __alignToolbarToEditor(anchorEl) {
        if (!anchorEl) return;
        var rect = anchorEl.get(0).getBoundingClientRect();
        var x = rect.left;
        var y = rect.top;
        var tbRect = $('#'+ this.__tbId).get(0).getBoundingClientRect();
        var h = tbRect.height;

        var noOfItems = w2ui.toolbarFont.items.length;

        // was "30", but with a spacer the w is different
        var offsetW = 2;
        var newWidth = (noOfItems * constItemWidth) + offsetW + "px";

        var newTop = (y - h - 5);
        if (newTop < 0) newTop = 0;

        var tb = $('#' + this.__tbId);
        tb.css({
            "z-index": 999,
            left: (x - constItemWidth - 5) + "px",
            top: newTop + "px",
            width: newWidth,
            border: "1px solid rgba(0,0,0,0.2)"
        });
    }
}