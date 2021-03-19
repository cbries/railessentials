(function ($) {

    $.fn.videoStream = function(options) {

        var __isInstalled = false;

        var settings = $.extend({
            //objectId: -1, // the ESU ECoS Locomotive ID
            //speedMode: "dcc28",
            //width: 750,
            //height: 200,
            //speedTimeMaxDefault: 5,
            //speedStepMaxDefault: null,
            //preloadData: [],
            //onChanged: null
        }, options);

        __install();

        __isInstalled = true;

        return this;

        function __install() {

        }

    } // $.fn.videoStream

}(jQuery));