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

    // Theme toggle — light / dark
    var STORAGE_KEY = 'ww-theme';
    var btn = document.getElementById('theme-toggle');
    var icon = document.getElementById('theme-icon');

    if (btn && icon) {
        function getTheme() {
            return localStorage.getItem(STORAGE_KEY) || 'dark';
        }

        function applyTheme(theme) {
            if (theme === 'dark') {
                document.documentElement.setAttribute('data-theme', 'dark');
                icon.className = 'fas fa-sun';
                btn.title = 'Switch to light mode';
            } else {
                document.documentElement.removeAttribute('data-theme');
                icon.className = 'fas fa-moon';
                btn.title = 'Switch to dark mode';
            }
        }

        // Sync icon with whatever the anti-FOUT script already set
        applyTheme(getTheme());

        btn.addEventListener('click', function () {
            var next = getTheme() === 'dark' ? 'light' : 'dark';
            localStorage.setItem(STORAGE_KEY, next);
            applyTheme(next);
        });
    }
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

function handleCommentClicks() {
    // auth required for these actions so we can use GET and not worry about CSRF tokens
    $(document).on('click', '.approve-comment', function (e) {
        e.preventDefault();

        var id = $(this).data("id");
        ajaxJson('/comments/' + id + '/approve', null,
            (res) => {
                $("#" + id).removeClass("inactive");
                $(this).remove();
            },
            (err) => {
                alert("couldn't approve comment" + err.message);
            },
            { method: "GET", accepts: "application/json" });
    });

    $(document).on('click', '.remove-comment', function (e) {
        e.preventDefault();
        var id = $(this).data("id");                
        ajaxJson('/comments/' + id + '/remove', null,
            (res) => {                           
                $("#" + id).remove();			                                    
            },
            (err) => {
                alert("couldn't remove comment" + err.message);
            },
            { method: "GET", accepts: "application/json" });
    });
}