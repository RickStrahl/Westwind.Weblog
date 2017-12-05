/// <reference path="jquery.js" />
$(document).ready(function () {
    $(window).on("scroll",function (e) {        
        if (window.innerWidth > 767 && window.innerWidth < 1300) {
            if (window.scrollY > 1500) {
                $(".post-sidebar").addClass("hide");
                $(".post-content").addClass("expand");
            } 
            //else {
                //$(".post-sidebar").removeClass("hide");
                //$(".post-content").removeClass("expand");
            //}
        }
        
    });
});
function relative_time(time_value) {
    var values = time_value.split(" ");
    time_value = values[1] + " " + values[2] + ", " + values[5] + " " + values[3];
    var parsed_date = Date.parse(time_value);
    var relative_to = (arguments.length > 1) ? arguments[1] : new Date();
    var delta = parseInt((relative_to.getTime() - parsed_date) / 1000);
    delta = delta + (relative_to.getTimezoneOffset() * 60);
    if (delta > 3600 * 24)
        return Math.round(delta / (3600 * 24)).toString() + "d";
    return (delta < 3600) ? Math.round(delta / 60).toString() + "m" :
	                            Math.round(delta / 3600).toString() + "h";
}

