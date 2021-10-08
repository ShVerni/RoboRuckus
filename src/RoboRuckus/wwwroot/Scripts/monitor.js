$(function () {
    // Set up arrays for card faces and details
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

    // Check if game is started
    if ($("#startButton").data("started") === "True") {
        $("#startButton").hide();
        $("#controlButtons").show();
    } else {
        $("#startButton").show();
        $("#controlButtons").hide();
    }
 
    // Start a timer to check the game status every second
    var fetcher = new Interval(function () { $.get("/Setup/Status", function (data) { processData(data); }); }, 1000);
    fetcher.start();

    // Create connection to player hub
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/playerHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();    

    // Makes sure the monitor is ready for a round if multiple monitors are being used
    connection.on("requestdeal", () => {
        if (!fetcher.isRunning()) {
            $(".ui-droppable").droppable("destroy");
            $("#cardsContainer").empty();
            // Restart the update interval
            fetcher.start();
        }
    });

    // Shows a preview of the upcoming register
    connection.on("showRegister", (cards, robots) => {
        $("#cardsContainer").empty();
        var _cards = $.parseJSON(cards);
        var _robots = $.parseJSON(robots);
        var i = 1;
        $.each(_cards, function () {
            var face;
            var details;
            if (this.direction === "forward") {
                face = this.magnitude;
                details = detail[this.direction] + " " + this.magnitude;
            }
            else {
                face = faces[this.direction];
                details = detail[this.direction];
            }
            // Append card to card container
            $("#cardsContainer").append("<li id='card" + i + "' class='ui-widget-content dealtCard'>\
                    <div class='cardBody'>\
                        <p class='order'>" + this.priority + "</p>\
                        <p class='face'>" + face + "</p>\
                        <p class='details'>" + details + "</p>\
                        <img src='/images/cards/bg.png'alt='card'>\
                        <p class='robot-names'>" + _robots[i - 1] + "</p>\
                    </div>\
                </li>"
            );
            $("#card" + i).data("cardinfo", this);
            i++;
        });

        // Minimum width of window is 7 slots worth
        boxes = $(".dealtCard").length;

        // Set the width of the cards to fill the screen in one row
        resize();
    });

    // Shows the current move being executed
    connection.on("showMove", (card, robot, register) => { 
        $("#cardsContainer").empty();
        var _card = $.parseJSON(card);
        var face;
        var details;
        if (_card.direction === "forward") {
            face = _card.magnitude;
            details = detail[_card.direction] + " " + _card.magnitude;
        }
        else {
            face = faces[_card.direction];
            details = detail[_card.direction];
        }

        $("#cardsContainer").append("<li class='ui-widget-content dealtCard'>\
                <div class='cardBody'>\
                    <p class='order'>" + _card.priority + "</p>\
                    <p class='face'>" + face + "</p>\
                    <p class='details'>" + details + "</p>\
                    <img src='/images/cards/bg.png'alt='card'>\
                </div>\
            </li>\
            <li style=\"display: block\" id='player'>Robot moving: " + robot + "<\li>"
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
            $("#player").css("font-size", percent * 3 + "em");
            $(".slot, #sendcards").width(imageWidth + 2);
            $(".slot, #sendcards").height(imageHeight + 28);
            $("#cardsContainer").css("min-height", imageHeight + 5);
        }
    });

    // Processes the current game status
    function processData(data) {
        $("#botStatus").empty();
        $(".boardSquare").empty().css("background", "").removeClass("managePlayer").off("click");
        var i = 1;
        if (flags !== null) {
            flags.forEach(function (entry) {
                $("#" + entry[0] + "_" + entry[1]).html('<div class="flags"><p>' + i + " &#x2690;</p></div>").addClass("hasFlag").data("flag", i);
                i++;
            });
        }
        // Check if robots need to re-enter game
        if (data.entering) {
            // Pause the update interval
            fetcher.stop();
            // Get all the bots that need re-entering
            var content = '<div id="botContainer" class="ui-helper-reset">';
            $.each(data.players, function () {
                if (this.reenter !== 0) {
                    content += '<div class="bots" data-number="' + this.number.toString() + '" data-orientation="1"><p>' + (this.number + 1).toString() + '&#x2191;</p></div>';
                    content += '<p>Checkpoint: [' + this.last_x + ', ' + this.last_y + ']</p>';
                }
            });
            content += '</div><button id="sendBots">Re-enter Bots</button>';

            // Add content to the cardContainer
            $("#cardsContainer").html(content);

            // Make bots dragable
            $(".bots").draggable({
                revert: "invalid", // When not dropped, the item will revert back to its initial position
                containment: "document",
                cursor: "move"
            }).attr('unselectable', 'on').on('selectstart', false).click(function () {
                orient(this);
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
                    $(ui.draggable).detach().css({ top: "", left: "", margin: "0 auto", position: "" }).appendTo(this);
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
                $("#" + this.x.toString() + "_" + this.y.toString()).html('<p data-player="' + (this.number + 1).toString() + '">' + (this.number + 1).toString() + orientation + "</p>").css("background", "yellow").addClass("managePlayer");
            });
            $(".managePlayer").click(function (event) {
                event.stopImmediatePropagation();
                $('<div id="dialog"></div>').html('<iframe style="border: 0px; " src="/Setup/Manage/' + $(this).find("p").data("player") + '" width="100%" height="100%"></iframe>').dialog({
                    autoOpen: true,
                    height: 650,
                    width: 450,
                    title: "Manage Player" + $(this).find("p").data("player")
               });
            });
        }
    }

    // Display a message from the server
    connection.on("displayMessage", (message, sound) => { 
        $("#cardsContainer").html("<h2>" + message + "</h2>");
    });

    // Start the connection to the player hub
    connection.start().catch(err => console.error(err.toString()));

    // Sends bot info to server for re-entry
    function sendBots() {
        if ($('#botContainer > .bots').length === 0) {
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
            fetcher.start();
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
                    break;
                case 2:
                    direction = 1;
                    break;
                case 3:
                    direction = 2;
                    break;
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

    // Starts the game
    $("#startGame").button().click(function (event) {
        $.get("/Setup/startGame?status=0", function (data) {
            // alert(data);
            $("#startButton").hide();
            $("#controlButtons").show();
        });
    });

    // Resets the current game
    $("#reset").button().click(function (event) {
        $.get("/Setup/Reset?resetAll=0", function (data) { alert(data); });
        $("#startButton").show();
        $("#controlButtons").hide();
    });

    // Resets the game the very start power on state
    $("#resetAll").button().click(function (event) {
        $.get("/Setup/Reset?resetAll=1", function (data) {
            alert(data);
            setTimeout(function () {
                window.location.assign("/Setup");
            }, 1);
        });
    });

    // Timer button toggle effects
    $("#timer").button().click(function () {
        if ($('#timer').prop("checked")) {
            $.get("/Setup/Timer?timerEnable=true", function (data) {
                if (data === "OK") {
                    $("#timerText").html("Timer Enabled");
                } else {
                    $('#timer').prop("checked", false);
                    $('#timer').button("refresh");
                }
            });
        } else {
            $.get("/Setup/Timer?timerEnable=false", function (data) {
                if (data === "OK") {
                    $("#timerText").html("Timer Disabled");
                } else {
                    $('#timer').prop("checked", true);
                    $('#timer').button("refresh");
                }
            });
        }
    });
    $("#timerLabel").hover(function () {
        $(this).removeClass("ui-state-hover");
    });
    if ($('#timer').prop("checked")) {
        $("#timerText").html("Timer Enabled");
    }
    else {
        $("#timerText").html("Timer Disabled");
    }

    function resize() {
        var imageWidth = (($(window).width() - 80) / boxes) / 3;
        if (imageWidth < 350) {
            var percent = imageWidth / 350;
            var imageHeight = percent * 520;
            $(".order").css("font-size", percent * 4 + "em");
            $(".face").css("font-size", percent * 11.8 + "em");
            $(".details").css("font-size", percent * 2.5 + "em");
            $(".robot-names").css("font-size", percent * 2.5 + "em");
            $("#cardsContainer img, .slot img").width(imageWidth);
            $("#cardsContainer img, .slot img").height(imageHeight);
            $("#cardsContainer li, .slot li").css({
                "height": "",
                "width": ""
            });
            $("#cardsContainer").css("min-height", imageHeight + 5);
        }
        else {
            $(".order").css("font-size", "2.15em");
            $("#cardsContainer img, .slot img").width(350);
            $("#cardsContainer img, .slot img").height(520);
            $("#cardsContainer li, .slot li").css({
                "height": "",
                "width": ""
            });
            $("#cardsContainer").css("min-height", 525);
        }
        $("#labelText").css("font-size", 1.35 + (0.3 * (9 - boxes)) + "vw");
    }
});

// Creates an interval with an associated running status
function Interval(fn, time) {
    var timer = false;
    this.start = function () {
        if (!this.isRunning())
            timer = setInterval(fn, time);
    };
    this.stop = function () {
        clearInterval(timer);
        timer = false;
    };
    this.isRunning = function () {
        return timer !== false;
    };
}

