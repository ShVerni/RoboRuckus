$(function () {
    // Sets a timer to retrieve the game's status at a regular interval
    setInterval(function () { $.get("/Player/Status", function (data) { processData(data); }) }, 1000);
    $("#bots").selectmenu({
        width: null
    });
    // Setup buttons and variables
    $("#button").button({ disabled: true });
    var flags = $("#board").data("flag");
    var player = $("#playerNum").data("player");
    var direction = 1;
    $(".boardSquare").css({"cursor": "pointer", "user-select": "none"}).attr('unselectable', 'on').on('selectstart', false);

    // Loads the background image for the board
    $("#board").css("background-image", 'url("/images/boards/' + $("#board").data("board") + '.png")');

    // Lets a user select a starting position for their bot.
    // TODO: Restrict selection to predefined starting squares.
    $(".boardSquare").click(function () {
        if (!$(this).hasClass("occupied")) {
            if ($("#button").button("option", "disabled") == true) {
                $("#button").button("option", "disabled", false);
            }
            $("#player").val(player);
            if ($(this).hasClass("selected")) {
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
                $("#botDir").val(direction);
            }
            else {
                $("#botX").val($(this).data("x"));
                $("#botY").val($(this).data("y"));
                $("#botDir").val(direction);
            }
            $(".boardSquare:not(:has(>.flags))").not(".occupied").empty().css("background", "").removeClass("selected");
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
            $(this).html("<p>" + player + orientation + "</p>").css("background", "yellow").addClass("selected");
        }
    });

    // Receives the locations of all bots that have selected starting squares, makes those slots unselectable, and updates list of available bots
    function processData(data) {
        var i = 1;
        if (flags != null) {
            flags.forEach(function (entry) {
                $("#" + entry[0] + "_" + entry[1]).html('<div class="flags"><p>' + i + " &#x2690;</p></div>");
                i++;
            });
        }
        var chosen = $("#bots").val();
        $("#bots").html('<option value="">Select a Robot</option>');
        $.each(data.botNames, function () {
            $("#bots").append('<option value="' + this + '">' + this + "</option>");
        });
        if (chosen != "" && 0 != $('#bots option[value="' + chosen + '"]').length)
        {
            $("#bots").val(chosen);
        }
        $("#bots").selectmenu("refresh");
        $.each(data.robots, function () {
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
            $("#" + this.x.toString() + "_" + this.y.toString()).html("<p>" + (this.number + 1).toString() + orientation + "</p>").css("background", "yellow").addClass("occupied");
        });
    }

    $("#playerForm").submit(function () {
        $("#selMessage").empty();
        if($("#bots").val() == "")
        {
            $("#selMessage").html("Please select a robot");
            return false
        }
        return true;
    });

});