/**
 * parts of the source from here:
 * https://github.com/artberri/jquery-html5storage/blob/master/jquery.html5storage.js
 */

;(function(window, $ ) {
    "use strict";

    var types = ['localStorage','sessionStorage'],
        support = [];

    $.each(types, function( i, type ) {
        try {
            support[type] = type in window && window[type] !== null;
        } catch (e) {
            support[type] = false;
        }

        $[type] = {
            settings : {
                cookiePrefix : 'ecosApp:' + type + ':',
                cookieOptions : {
                    path : '/ecosApp',
                    domain : document.domain,
                    expires: ('localStorage' === type)
                        ? { expires: 365 }
                        : undefined
                }
            },
            
            getItem : function( key ) {
                var response;
                if(support[type]) {
                    response = window[type].getItem(key);
                }
                else {
                    response = $.cookie(this.settings.cookiePrefix + key);
                }
                
                return response;
            },
            
            setItem : function( key, value ) {
                if(support[type]) {
                    return window[type].setItem(key, value);
                }
                else {
                    return $.cookie(this.settings.cookiePrefix + key, value, this.settings.cookieOptions);
                }
            },

            removeItem : function( key ) {
                if(support[type]) {
                    return window[type].removeItem(key);
                }
                else {
                    var options = $.extend(this.settings.cookieOptions, {
                        expires: -1
                    });
                    return $.cookie(this.settings.cookiePrefix + key, null, options);
                }
            },

            clear : function() {
                if(support[type]) {
                    return window[type].clear();
                }
                else {
                    var reg = new RegExp('^' + this.settings.cookiePrefix, ''),
                        options = $.extend(this.settings.cookieOptions, {
                            expires: -1
                        });

                    if(document.cookie && document.cookie !== ''){
                        $.each(document.cookie.split(';'), function( i, cookie ){
                            if(reg.test(cookie = $.trim(cookie))) {
                                 $.cookie( cookie.substr(0,cookie.indexOf('=')), null, options);
                            }
                        });
                    }
                }
            }
        };
    });
})(window, jQuery);