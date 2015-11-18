$(function () {
    Howler._enableiOSAudio();
    var curDamage = parseInt($("#damage").data("damage"));

    var damageSound = new Howl({
        src: ['/sounds/damage.ogg', '/sounds/damage.mp3']
    });

    var laserSound = new Howl({
        src: ['/sounds/laser.ogg', '/sounds/laser.mp3']
    });

    $("#shutdown").button().click(function () {
        if ($('#shutdown').is(":checked"))
        {
            $("#labelText").html("Shutdown On");
        }
        else
        {
            $("#labelText").html("Shutdown Off");
        }
    });

    $("#shutdownLabel").hover(function () {
        $(this).removeClass("ui-state-hover");
    });

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

    var boxes = 9;

    // Start connection to player hub
    var cardControl = $.connection.playerHub;

    // Processes and displays cards dealt to the player
    cardControl.client.deal = (function (cards, lockedCards) {
        $("#sendcards").unbind("click");
        $(".slot ul").empty();
        if ($("#submitted").length != 0) {
            $("#submitted").remove();
        }
        var _cards = $.parseJSON(cards);
        if (_cards.length == 0) {
            $("#shutdownLabel").css("background", "red");
            $("#cardsContainer").html("<h2>Robot shutdown</h2>");
        }
        else
        {
            $("#cardsContainer").empty();
            $("#shutdownLabel").css("background", "none");
        }
        var i = 1;
        $.each(_cards, function () {
            var face;
            var details;
            if (this.direction == "forward") {
                face = this.magnitude;
                details = detail[this.direction] + " " + this.magnitude;
            }
            else {
                face = faces[this.direction];
                details = detail[this.direction];
            }

            $("#cardsContainer").append("<li id='card" + i + "' class='ui-widget-content dealtCard'>\
                <div class='cardBody'>\
                    <p class='order'>" + this.priority + "</p>\
                    <p class='face'>" + face + "</p>\
                    <p class='details'>" + details + "</p>\
                    <img src='/images/cards/bg.png'alt='card'>\
                </div>\
            </li>");
            $("#card" + i).data("cardinfo", this);
            i++;
        });

        $(".slot").droppable("enable");

        $(".locked").remove();

        var slot = 5;
        $.each($.parseJSON(lockedCards), function () {
            var face;
            var details;
            if (this.direction == "forward") {
                face = this.magnitude;
                details = detail[this.direction] + " " + this.magnitude;
            }
            else {
                face = faces[this.direction];
                details = detail[this.direction];
            }
            $("#slot" + slot + " ul").append("<li style='cursor: default' id='card" + i + "' class='ui-widget-content'>\
                <div class='cardBody'>\
                    <p class='order'>" + this.priority + "</p>\
                    <p class='face'>" + face + "</p>\
                    <p class='details'>" + details + "</p>\
                    <img src='/images/cards/bg.png'alt='card'>\
                </div>\
            </li>");
            $("#slot" + slot).droppable("disable");
            $("#slot" + slot + " h4").append("<span class='locked'> LOCKED!</span>");
            $("#card" + i).data("cardinfo", this);
            slot--;
            i++;
        });

        boxes = ($(".dealtCard").length < 7) ? 7 : $(".dealtCard").length;

        // Set the width of the cards to fill the screen in one row
        var imageWidth = (($(window).width() - 80) / boxes);
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
            $(".slot, #sendcards").width(imageWidth + 2);
            $(".slot, #sendcards").height(imageHeight + 28);
            $("#cardsContainer").css("min-height", imageHeight + 5);
        }

        // Let the card items be draggable
        $("li", "#cardsContainer").draggable({
            revert: "invalid", // When not dropped, the item will revert back to its initial position
            containment: "document",
            cursor: "move"
        });

        $("#sendcards img").click(sendCards);
        cardControl.server.getHealth($('#playerNum').data("player")).done(function (damage) {
            updateHealth(damage);
        })
    });

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
        var imageWidth = (($(window).width() - 80) / 7);
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

    // Update damage
    cardControl.client.UpdateHealth = (function (damage) {
        var curHealth = $.parseJSON(damage);
        var player = parseInt($('#playerNum').data("player"));
        var myDamage = curHealth[player - 1];
        updateHealth(myDamage);
    });

    // Request a deal from the server
    cardControl.client.requestdeal = (function () {
        $("#shutdown").prop("checked", false).button("refresh");
        cardControl.server.dealMe($('#playerNum').data("player"));
    });

    //  Once the page loads, get the player's first hand
    $.connection.hub.start().done(function () {
        cardControl.server.dealMe($('#playerNum').data("player"));
    });

    // Game has been reset, return to setup page
    cardControl.client.Reset = (function () {
        window.location = "/Player/playerSetup/" + $('#playerNum').data("player");
    });

    cardControl.client.displayMessage = (function (message, sound) {
        $("#cardsContainer").html("<h2>" + message + "</h2>");
        switch(sound)
        {
            case "laser":
                laserSound.play();
                break;
        }
    });

    //Resize the cards as the window resizes
    $(window).resize(function () {
        var imageWidth = (($(window).width() - 80) / boxes);
        if (imageWidth < 350) {
            var percent = (imageWidth / 350);
            var imageHeight = percent * 520;
            $(".order").css("font-size", percent * 4 + "em");
            $(".face").css("font-size", percent * 11.8 + "em");
            $(".details").css("font-size", percent * 2.5 + "em");
            $("#cardsContainer img, .slot img").width(imageWidth);
            $("#cardsContainer img, .slot img").height(imageHeight);
            $("#cardsContainer li, .slot li").css({
                "height": "",
                "width": ""
            });
            $(".slot, #sendcards").width(imageWidth + 2);
            $(".slot, #sendcards").height(imageHeight + 28);
            $("#cardsContainer").css("min-height", imageHeight + 5);
        }
        else
        {
            $(".order").css("font-size", "2.15em");
            $("#cardsContainer img, .slot img").width(350);
            $("#cardsContainer img, .slot img").height(520);
            $("#cardsContainer li, .slot li").css({
                "height": "",
                "width": ""
            });
            $(".slot, #sendcards").width(352);
            $(".slot, #sendcards").height(548);
            $("#cardsContainer").css("min-height", 525);
        }
    });

    // Let the slots be droppable, accepting the card items
    $(".slot").droppable({
        accept: "#cardsContainer > li, .slot li",
        hoverClass: "ui-state-hover",
        drop: function (event, ui) {
            if ($(ui.draggable).parent().hasClass("slotList"))
            {
                $(ui.draggable).closest("div").droppable("enable");
            }
            $(ui.draggable).css({
                "top": 0,
                "right": 0,
                "left": 0,
                "bottom": 0
            });
            $(ui.draggable).appendTo($("ul", this));
            $(this).droppable("disable");
        }
    });

    // Let the card area be droppable as well, accepting items from the slots
    $("#cardsContainer").droppable({
        accept: ".slot li",
        activeClass: "custom-state-active",
        drop: function (event, ui) {
            $(ui.draggable).closest("div").droppable("enable");
            $(ui.draggable).css({
                "top": 0,
                "right": 0,
                "left": 0,
                "bottom": 0
            });
            $(ui.draggable).appendTo($(this));
        }
    });

    // Receives the robot's current health from the server
    function updateHealth(damage)
    {
        if (damage > curDamage)
        {
            damageSound.play();
        }
        $(".damageBox").css('background', 'none');
        if (damage >= 10) {
            $("#damage").html("Robot is dead! ");
            $("#cardsContainer").html("<h2>Your robot has died!</h2>");
        }
        else {
            $("#damage").html("Robot damage: ");
        }
        for (var i = 0; i <= damage; i++)
        {
            $("#damageBox_" + i).css('background', 'red');
        }
        curDamage = damage;
    }

    // Sends the selected cards to the server
    function sendCards () {
        if ($(".slot").has("li").length != $(".slot").length)
        {
            alert("Please fill all movement slots");
        }
        else
        {
            var shutdown = false;
            var move = new Array();
            $(".slot").each(function () {
                move.push($("li", this).data("cardinfo"));
            });
            $(".ui-draggable").draggable("option", "disabled", true).css("cursor", "default");
            if ($('#shutdown').is(":checked"))
            {
                shutdown = true;
            }
            cardControl.server.sendCards($('#playerNum').data("player"), move, shutdown);
            $("#slots").prepend("<h3 id='submitted' style='color: red'>Program Submitted</h3>")
            $("#sendcards").unbind("click");
        }
    }
});