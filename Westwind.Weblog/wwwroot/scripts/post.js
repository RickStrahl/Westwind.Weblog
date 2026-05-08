/// <reference path="jquery.js" />
/// <reference path="ww.jquery.js" />
(function ($) {
    
    $(function () {
        createH3Links();
        createDocumentOutline();
        createImageLinks();

        // force hash to reload        
        if (window.location.hash)
            window.location = window.location.hash;
        const $txtBody = $$("txtBody");
        if ($txtBody) {
            $txtBody.on("keydown", debounce(OnTextTyped, 1200));
            $txtBody
                .focus(function () {
                    OnTextTyped();
                });
        }

        $(".commentedit").click(commentEdit);
        $(".remove-comment").click(DeleteComment);

        documentOutlineScrolling();
    });

    function documentOutlineScrolling() {           
        $(window).scroll(debounce(scrollFunc, 10, true));
    }



    function scrollFunc() {
        var $docOutline = $(".document-outline");

        var scrollTop = window.scrollY;
        var offset = $(".hero-image").height() + 200;
        var offset = $(".hero-image").height() + 200;
        if (scrollTop > offset)

            $docOutline[0].style.top = scrollTop - offset + "px";
        else
            $docOutline[0].style.top = 0;

        scrollSpy();

        var maxHeight = window.innerHeight - $docOutline.css("top") - 10;
        
        if ($docOutline.innerHeight() > maxHeight)
            $docOutline.height(maxHeight);
        else
            $docOutline.height("");


        var docOutlineHeight = $docOutline.height();
        var contentHeight = $("#ArticleBody").height();

        if (scrollTop > contentHeight - docOutlineHeight + offset)
            $docOutline[0].style.top = contentHeight - docOutlineHeight + "px";
    }

    function scrollSpy() {
        var headers$ = $(".document-outline-content>a");
        if (headers$.length < 1)
            return;
        
        for (var index = 0; index < headers$.length; index++) {
          
            var hd$ = $(headers$[index]);
            var id = hd$.attr('href');

            var id$;
            try {
                id$ = $(id);
            } catch (ex) {
                continue;
            }
            if (id$.length < 1)
                continue;

            if (id$.isInViewport()) {                
                $(".document-outline-content *").removeClass("active");

                hd$.addClass("active");
                break;
            }
        }
    }
    $.fn.isInViewport = function () {
        var elementTop = $(this).offset().top;
        var elementBottom = elementTop + $(this).outerHeight();
        var viewportTop = $(window).scrollTop();
        var viewportBottom = viewportTop + $(window).height();
        return elementBottom > viewportTop && elementTop < viewportBottom;
    }; 

    function createH3Links() {
        var $h3 = $(".postcontent>h3, .postcontent>h4, .postcontent>h2");
        
        $h3.each(function() {
            var $h3item = $(this);

            var tag = this.id;
            if (!tag) {
                tag = safeId($h3item.text());                
            }

            var $a = $("<a />")
                .attr({
                    name: tag,
                    href: "#" + tag
                })
                .addClass('linkicon')
                .addClass('link-hidden');
            $h3item.prepend($a);

            $h3item
                .hover(
                    function() {
                        $a.removeClass("link-hidden");
                    },
                    function() {
                        $a.addClass("link-hidden");
                    })
                .click(function() {
                    var link = $a.prop("href");                    
                    window.location.href = link;
                });
        });
    }


    function createDocumentOutline() {
        var navbar$ = $(".document-outline-content");
        navbar$.html("");

        var headers$ = $("#ArticleBody").find("h1,h2,h3,h4");        
        var title$ = $("#PostTitle");
        headers$= headers$.add(title$);

        if (headers$.length < 2) 
            return;

        
        
        for (var index = 0; index < headers$.length; index++) {
            var el = headers$[index];
            var id = el.id;

            if (!id) {
                el.id = safeId(el.innerText);
                id = el.id;
            }

            var space = "";
            if (el.nodeName == "H1")
                space = "outline-level1";
            else if (el.nodeName == "H2")
                space = "outline-level2";
            else if (el.nodeName == "H3")
                space = "outline-level3";
            else if (el.nodeName == "H4")
                space = "outline-level4";
            var a$ = $("<a></a>")
                .prop("href", "#" + id)
                .text(el.innerText);
            if (space)
                a$.addClass(space);

            navbar$.append(a$);
        }
    }
    function safeId(inputString) {
        if (!inputString) return inputString;
        var id = $.trim(inputString)
            .replace(/-/g, "--")
            .replace(/[\s,-,']/g, "-");
        return id;
    }

    function createImageLinks() {
        var $imgs = $(".postcontent img");
        
        $imgs.each(function () {
            var $el = $(this);
            var $parent = $el.parent();
            if ($parent[0].nodeName === "A") 
                return;

            var $wrap = $("<a href='" + this.src + "'>");
            $wrap.insertBefore($el).append($el);            
        });

    }

    function OnTextTyped(event) {
        var Ctl = $("#" + serverVars.txtBodyId);
        var Ctl2 = $("#lblCommentCharCount");
        if (Ctl.length > 0 && Ctl2.length > 0) {
            var Size = Ctl.val().length;
            Ctl2.text(Size.toString() + " of " + serverVars.commentMaxLength + " characters");
        }

        Proxy.FormatComment(Ctl.val(), function(result) {
            if (result)
                $("#divCommentPreview")
                    .html(result).show();

            setTimeout(function() {
                $('pre code').each(function(i, block) {
                    hljs.highlightBlock(block);
                });
            });
        });
    }

    function DeleteComment() {
        var $el = $(this);
        var id = $el.data("id") * 1;        
        Proxy.DeleteComment(id,
            function(result) {
                if (!result) {
                    showStatus("Comment not deleted. Id is invalid or you're not logged in.");
                    return;
                }                
                $el.parents(".comment").fadeOut("slow", function() { $(this).remove(); });
            },
            function(error) {
                showStatus("Comment not deleted: " + error.message);
            }
        );
    }

    function commentEdit(evt) {
        var jComment = $(this).parents(".comment-panel-right").find(".commentbody");
        if (jComment.length < 1)
            return;

        jComment.contentEditable(
        {
            editClass: "contenteditable",
            saveHandler: function(e) {
                // grab id from parent .comment element and strip cmt_ prefix
                var id = jComment.parents(".comment").get(0).id.replace("cmt_", "");

                // call service to update comment with numeric id and updated html
                Proxy.UpdateCommentText(+id, jComment.text());

                // return true to close editor  (false leaves open)
                return true;
            }
        });
    }

    
    var shortcut = {
        'all_shortcuts': {},//All the shortcuts are stored in this array
        'add': function (shortcut_combination, callback, opt) {
            //Provide a set of default options
            var default_options = {
                'type': 'keydown',
                'propagate': false,
                'disable_in_input': false,
                'target': document,
                'keycode': false
            }
            if (!opt) opt = default_options;
            else {
                for (var dfo in default_options) {
                    if (typeof opt[dfo] == 'undefined') opt[dfo] = default_options[dfo];
                }
            }

            var ele = opt.target;
            if (typeof opt.target == 'string') ele = document.getElementById(opt.target);
            var ths = this;
            shortcut_combination = shortcut_combination.toLowerCase();

            //The function to be called at keypress
            var func = function (e) {
                e = e || window.event;

                if (opt['disable_in_input']) { //Don't enable shortcut keys in Input, Textarea fields
                    var element;
                    if (e.target) element = e.target;
                    else if (e.srcElement) element = e.srcElement;
                    if (element.nodeType == 3) element = element.parentNode;

                    if (element.tagName == 'INPUT' || element.tagName == 'TEXTAREA') return;
                }

                //Find Which key is pressed
                if (e.keyCode) code = e.keyCode;
                else if (e.which) code = e.which;
                var character = String.fromCharCode(code).toLowerCase();

                if (code == 188) character = ","; //If the user presses , when the type is onkeydown
                if (code == 190) character = "."; //If the user presses , when the type is onkeydown

                var keys = shortcut_combination.split("+");
                //Key Pressed - counts the number of valid keypresses - if it is same as the number of keys, the shortcut function is invoked
                var kp = 0;

                //Work around for stupid Shift key bug created by using lowercase - as a result the shift+num combination was broken
                var shift_nums = {
                    "`": "~",
                    "1": "!",
                    "2": "@",
                    "3": "#",
                    "4": "$",
                    "5": "%",
                    "6": "^",
                    "7": "&",
                    "8": "*",
                    "9": "(",
                    "0": ")",
                    "-": "_",
                    "=": "+",
                    ";": ":",
                    "'": "\"",
                    ",": "<",
                    ".": ">",
                    "/": "?",
                    "\\": "|"
                }
                //Special Keys - and their codes
                var special_keys = {
                    'esc': 27,
                    'escape': 27,
                    'tab': 9,
                    'space': 32,
                    'return': 13,
                    'enter': 13,
                    'backspace': 8,

                    'scrolllock': 145,
                    'scroll_lock': 145,
                    'scroll': 145,
                    'capslock': 20,
                    'caps_lock': 20,
                    'caps': 20,
                    'numlock': 144,
                    'num_lock': 144,
                    'num': 144,

                    'pause': 19,
                    'break': 19,

                    'insert': 45,
                    'home': 36,
                    'delete': 46,
                    'end': 35,

                    'pageup': 33,
                    'page_up': 33,
                    'pu': 33,

                    'pagedown': 34,
                    'page_down': 34,
                    'pd': 34,

                    'left': 37,
                    'up': 38,
                    'right': 39,
                    'down': 40,

                    'f1': 112,
                    'f2': 113,
                    'f3': 114,
                    'f4': 115,
                    'f5': 116,
                    'f6': 117,
                    'f7': 118,
                    'f8': 119,
                    'f9': 120,
                    'f10': 121,
                    'f11': 122,
                    'f12': 123
                }

                var modifiers = {
                    shift: { wanted: false, pressed: false },
                    ctrl: { wanted: false, pressed: false },
                    alt: { wanted: false, pressed: false },
                    meta: { wanted: false, pressed: false }	//Meta is Mac specific
                };

                if (e.ctrlKey) modifiers.ctrl.pressed = true;
                if (e.shiftKey) modifiers.shift.pressed = true;
                if (e.altKey) modifiers.alt.pressed = true;
                if (e.metaKey) modifiers.meta.pressed = true;

                for (var i = 0; k = keys[i], i < keys.length; i++) {
                    //Modifiers
                    if (k == 'ctrl' || k == 'control') {
                        kp++;
                        modifiers.ctrl.wanted = true;

                    } else if (k == 'shift') {
                        kp++;
                        modifiers.shift.wanted = true;

                    } else if (k == 'alt') {
                        kp++;
                        modifiers.alt.wanted = true;
                    } else if (k == 'meta') {
                        kp++;
                        modifiers.meta.wanted = true;
                    } else if (k.length > 1) { //If it is a special key
                        if (special_keys[k] == code) kp++;

                    } else if (opt['keycode']) {
                        if (opt['keycode'] == code) kp++;

                    } else { //The special keys did not match
                        if (character == k) kp++;
                        else {
                            if (shift_nums[character] && e.shiftKey) { //Stupid Shift key bug created by using lowercase
                                character = shift_nums[character];
                                if (character == k) kp++;
                            }
                        }
                    }
                }

                if (kp == keys.length &&
                            modifiers.ctrl.pressed == modifiers.ctrl.wanted &&
                            modifiers.shift.pressed == modifiers.shift.wanted &&
                            modifiers.alt.pressed == modifiers.alt.wanted &&
                            modifiers.meta.pressed == modifiers.meta.wanted) {
                    callback(e);

                    if (!opt['propagate']) { //Stop the event
                        //e.cancelBubble is supported by IE - this will kill the bubbling process.
                        e.cancelBubble = true;
                        e.returnValue = false;

                        //e.stopPropagation works in Firefox.
                        if (e.stopPropagation) {
                            e.stopPropagation();
                            e.preventDefault();
                        }
                        return false;
                    }
                }
            }
            this.all_shortcuts[shortcut_combination] = {
                'callback': func,
                'target': ele,
                'event': opt['type']
            };
            //Attach the function with the event
            if (ele.addEventListener) ele.addEventListener(opt['type'], func, false);
            else if (ele.attachEvent) ele.attachEvent('on' + opt['type'], func);
            else ele['on' + opt['type']] = func;
        },

        //Remove the shortcut - just specify the shortcut and I will remove the binding
        'remove': function (shortcut_combination) {
            shortcut_combination = shortcut_combination.toLowerCase();
            var binding = this.all_shortcuts[shortcut_combination];
            delete (this.all_shortcuts[shortcut_combination])
            if (!binding) return;
            var type = binding['event'];
            var ele = binding['target'];
            var callback = binding['callback'];

            if (ele.detachEvent) ele.detachEvent('on' + type, callback);
            else if (ele.removeEventListener) ele.removeEventListener(type, callback, false);
            else ele['on' + type] = false;
        }
    }

    // handle editor buttons
    $(document)
        .on("click",
            ".edit-toolbar>a",toolbarHandler);

    function toolbarHandler(id) {

        var $txt = $$("txtBody");
        var el = $txt[0];
        var text = el.value;

        var nSelStart = el.selectionStart;
        var nSelEnd = el.selectionEnd;

        var selectionPoint = 0;
        var sel = text.substring(nSelStart, nSelEnd);
        
        if(typeof id !== "string")
            id = this.id;

        if (id === "btnBold")
            sel = "**" + sel + "**";
        else if (id === "btnItalic")
            sel = "*" + sel + "*";
        else if (id === "btnCode")
            sel = "```\r\n" + sel + "\r\n```";
        else if (id == "btnHref") {
            sel = "[" + sel + "]()";
            selectionPoint = el.selectionEnd + 3;
        }        

        sel = el.setRangeText(sel);

        setTimeout(function() {
            el.focus();

            if (selectionPoint > 0) {
                el.selectionStart = selectionPoint;
                el.selectionEnd = selectionPoint;
            } else
                el.selectionStart = el.selectionEnd;            
        },
            10);


        //oMsgInput.setSelectionRange(bDouble || nSelStart === nSelEnd ? nSelStart + sStartTag.length : nSelStart, (bDouble ? nSelEnd : nSelStart) + sStartTag.length);
        //oMsgInput.focus();
    }
    shortcut.add("ctrl+b",
        function() {
            toolbarHandler("btnBold");
        });
    shortcut.add("ctrl+i",
        function () {
            toolbarHandler("btnItalic");
        });
    shortcut.add("alt+c",
        function () {
            toolbarHandler("btnCode");
        });
    shortcut.add("ctrl+k",
        function () {
            toolbarHandler("btnHref");
        });
    shortcut.add("ctrl+q",
            function () {
                toolbarHandler("btnQuote");
            });

    

})(jQuery);