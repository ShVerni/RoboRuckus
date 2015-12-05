$(function () {
    var faces = new Array();
    faces["right"] = "R";
    faces["left"] = "L";
    faces["uturn"] = "U";
    faces["backup"] = "B";

    var detail = new Array();
    detail["right"] = "Right";
    detail["left"] = "Left";
    detail["backup"] = "Backup";
    detail["uturn"] = "U-Turn";
    detail["forward"] = "Move";

    var flags = $("#board").data("flag");

    // Loads board background image
    $("#board").css("background-image", 'url("/images/boards/' + $("#board").data("board") + '.png")');
 
    // Start a timer to check the game status every second
    var fetcher = setInterval(function () { $.get("/Setup/Status", function (data) { processData(data); }) }, 1000);

    // Start connection to player hub
    var cardControl = $.connection.playerHub;
    $.connection.hub.start();

    // Shows the current move being executed
    cardControl.client.showMove = (function (cards, robot) {
        $("#cardsContainer").empty();
        var card = $.parseJSON(cards);
        var face;
        var details;
        if (card.direction == "forward") {
            face = card.magnitude;
            details = detail[card.direction] + " " + card.magnitude;
        }
        else {
            face = faces[card.direction];
            details = detail[card.direction];
        }

        $("#cardsContainer").append("<li class='ui-widget-content dealtCard'>\
                <div class='cardBody'>\
                    <p class='order'>" + card.priority + "</p>\
                    <p class='face'>" + face + "</p>\
                    <p class='details'>" + details + "</p>\
                    <img src='/images/cards/bg.png'alt='card'>\
                </div>\
            </li>\
            <li id='player'>Robot moving: " + robot + "<\li>"
        );
        //Set the width of the cards to fill the screen in one row
        var imageWidth = (($(window).width() - 80) / 8);
        if (imageWidth < 350) {
            var percent = (imageWidth / 350);
            var imageHeight = percent * 520;
            $(".order").css("font-size", percent * 4 + "em");
            $(".face").css("font-size", percent * 11.8 + "em");
            $(".details").css("font-size", percent * 2.5 + "em");
            $("#cardsContainer img, .slot img").width(imageWidth);
            $("#cardsContainer img,.slot img").height(imageHeight);
            $("#cardsContainer li, .slot li").css({
                "height": "",
                "width": ""
            });
            $("#player").css("font-size", percent * 5.8 + "em");
            $(".slot, #sendcards").width(imageWidth + 2);
            $(".slot, #sendcards").height(imageHeight + 28);
            $("#cardsContainer").css("min-height", imageHeight + 5);
        }
    });

    // Display a message from the server
    cardControl.client.displayMessage = (function (message, sound) {
        $("#cardsContainer").html("<h2>" + message + "</h2>");
    });

    // Processes the current game status
    function processData(data) {
        $("#botStatus").empty();
        $(".boardSquare").empty().css("background", "");
        var i = 1;
        if (flags != null) {
            flags.forEach(function (entry) {
                $("#" + entry[0] + "_" + entry[1]).html('<div class="flags"><p>' + i + " &#x2690;</p></div>").addClass("hasFlag").data("flag", i);
                i++;
            });;
        }
        // Check is robots need to re-enter game
        if (data.entering) {
            // Pause the update interval
            clearInterval(fetcher);
            // Get all the bots that need re-entering
            var content = '<div id="botContainer" class="ui-helper-reset">';
            $.each(data.players, function () {
                if (this.reenter != 0) {
                    content += '<div class="bots" data-number="' + this.number.toString() + '" data-orientation="0"><p>' + (this.number + 1).toString() + '&#x2192;</p></div>';
                }
            });
            content += '</div><button id="sendBots">Re-enter bots</button>';

            // Add content to the cardContainer
            $("#cardsContainer").html(content);

            // Make bots dragable
            $(".bots").draggable({
                revert: "invalid", // When not dropped, the item will revert back to its initial position
                containment: "document",
                cursor: "move"
            }).attr('unselectable', 'on').on('selectstart', false).click(function () {
                orient(this)
            });

            // Make the bot container dropppable
            $("#botContainer").droppable({
                accept: ".boardSquare div",
                hoverClass: "ui-state-hover",
                drop: function (event, ui) {
                    var parent = $(ui.draggable).parent();
                    parent.droppable("enable");
                    if (parent.hasClass("hasFlag"))
                    {
                        parent.append('<div class="flags"><p>' + parent.data("flag") + " &#x2690;</p></div>");
                    }
                    $(ui.draggable).detach().css({ top: "auto", left: "auto", margin: "0 0 1em 0" }).appendTo(this);
                }
            });
            // Make board squares droppable for player re-entry
            $(".boardSquare").droppable({
                accept: "#botContainer div, .boardSquare div",
                hoverClass: "ui-state-hover",
                drop: function (event, ui) {
                    $(this).droppable("disable").empty();
                    var parent = $(ui.draggable).parent();
                    if (parent.hasClass("boardSquare")) {
                        parent.droppable("enable");
                        if (parent.hasClass("hasFlag")) {
                            parent.append('<div class="flags"><p>' + parent.data("flag") + " &#x2690;</p></div>");
                        }
                    }
                    $(ui.draggable).detach().css({ top: 0, left: 0, margin: "0 auto" }).appendTo(this);
                }
            });
            // Bind function to re-enter bots 
            $("#sendBots").button().click(sendBots);
        } else {
            // Display updated bot statuses
            $.each(data.players, function () {
                $("#botStatus").append("<p>Player number " + (this.number + 1).toString() + " Damage: " + this.damage + " Flags: " + this.flags + " Lives: " + this.lives + "</p>");
                var orientation;
                switch (this.direction) {
                    case 0:
                        orientation = "&#x2192;";
                        break;
                    case 1:
                        orientation = "&#x2191;";
                        break;
                    case 2:
                        orientation = "&#x2190;";
                        break;
                    case 3:
                        orientation = "&#x2193;";
                        break;
                }
                $("#" + this.x.toString() + "_" + this.y.toString()).html("<p>" + (this.number + 1).toString() + orientation + "</p>").css("background", "yellow");
            });
        }
    }

    // Sends bot info to server for re-entry
    function sendBots() {
        if ($('#botContainer > *').length == 0) {
            var result = "[";
            var first = true;
            $(".bots").each(function (index) {
                if (!first)
                {
                    result += ",";
                }
                first = false;
                var coord = $(this).parent().attr("id");
                result += "[" + $(this).data("number") + ", " + coord.substring(0, coord.indexOf("_")) + ", " + coord.substring(coord.indexOf("_") + 1) + ", " + $(this).data("orientation") + "]";
            });
            result += "]";
            $(".ui-droppable").droppable("destroy");
            $.post("/Setup/enterPlayers", { players: result });
            $("#cardsContainer").empty();
            // Restart the update interval
            fetcher = setInterval(function () { $.get("/Setup/Status", function (data) { processData(data); }) }, 1000);
        }
    }
    
    // Re-orients a bot when clicked
    function orient(bot) {
        if ($(bot).parents('.boardSquare').length > 0) {
            var direction = $(bot).data("orientation");
            switch (direction) {
                case 0:
                    direction = 3;
                    break;
                case 1:
                    direction = 0;
                    break
                case 2:
                    direction = 1;
                    break
                case 3:
                    direction = 2;
                    break
            }
            $(bot).data("orientation", direction);
            var orientation;
            switch (direction) {
                case 0:
                    orientation = "&#x2192;";
                    break;
                case 1:
                    orientation = "&#x2191;";
                    break;
                case 2:
                    orientation = "&#x2190;";
                    break;
                case 3:
                    orientation = "&#x2193;";
                    break;
            }
            $(bot).html("<p>" + ($(bot).data("number") + 1).toString() + orientation + "</p>");
        }
    }

    // Resets the current game
    $("#reset").button().click(function (event) {
        $.get("/Setup/Reset?resetAll=0", function (data) { alert(data) });
    });

    //Resets the game the very start power on state
    $("#resetAll").button().click(function (event) {
        $.get("/Setup/Reset?resetAll=1", function (data) { alert(data); window.location = "/Setup"; });
    });
});