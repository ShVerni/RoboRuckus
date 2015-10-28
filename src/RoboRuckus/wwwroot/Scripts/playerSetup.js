$(function () {
    setInterval(function () { $.get("/Setup/Status", function (data) { processData(data); }) }, 1000);

    $("#button").prop("disabled", true);
    var player = $("#playerNum").data("player");
    var direction = 0;
    $(".boardSquare").css({"cursor": "pointer", "user-select": "none"}).attr('unselectable', 'on').on('selectstart', false);
    $(".boardSquare").click(function () {
        if (!$(this).hasClass("occupied")) {
            $("#button").prop("disabled", false);
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
            $(".boardSquare").not(".occupied").empty().css("background", "").removeClass("selected");
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
            $(this).html(player + orientation).css("background", "yellow").addClass("selected");
        }
    });

    function processData(data) {
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
            $("#" + this.x.toString() + "_" + this.y.toString()).html((this.number + 1).toString() + orientation).css("background", "yellow").addClass("occupied");
        });
    }
});