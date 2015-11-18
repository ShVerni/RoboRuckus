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

    $("#board").css("background-image", 'url("/images/boards/' + $("#board").data("board") + '.png")');
 
    // Start a timer to check the game status every second
    setInterval(function () { $.get("/Setup/Status", function (data) { processData(data); }) }, 1000);

    // Start connection to player hub
    var cardControl = $.connection.playerHub;
    $.connection.hub.start();

    // Shows the current move being executed
    cardControl.client.showMove = (function (cards, player) {
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
            <li id='player'>Player moving: " + player + "<\li>"
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
        $.each($.parseJSON(data), function () {
            $("#botStatus").append("<p>Player number " + (this.number + 1).toString() + " damage: " + this.damage + "</p>");
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

    // Resets the game to the initial state
    $("#reset").button().click(function (event) {
        $.get("/Setup/Reset", function (data) { alert(data) })
    });
});