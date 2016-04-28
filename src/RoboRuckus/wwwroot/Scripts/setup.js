$(function () {
    var _sizes = $("#board").data("sizes");

    // Allow validation of hidden fields
    $.validator.setDefaults({ ignore: null });
    $("#submitButton").button();

    // Enable the board selection menu
    $("#boardSel").selectmenu({
        change: function (event, ui) {
            // Draw the selected board
            var name = $(this).val();
            if (name) {
                var boardSize = _sizes[name];
                var board = "";
                for (var i = boardSize[1]; i >= 0; i--) {
                    board += '<div class="boardRow" id="y_' + i + '">';
                    for (var j = 0; j <= boardSize[0]; j++) {
                        board += '<div class="boardSquare" id="' + j + '_' + i + '"></div>';
                    }
                    board += '<div class="clear"></div></div>';
                }
                // Load board image
                $("#board").html(board).css("background-image", 'url("/images/boards/' + name.replace(" ", "") + '.png")');;
                //Enable flag placement
                $(".boardSquare").droppable({
                    accept: "#flagContainer div, .boardSquare div",
                    hoverClass: "ui-state-hover",
                    drop: function (event, ui) {
                        $(this).droppable("disable");
                        if ($(ui.draggable).parent().hasClass("boardSquare")) {
                            $(ui.draggable).parent().droppable("enable");
                        }
                        $(ui.draggable).detach().css({ top: 0, left: 0}).appendTo(this);
                        assignFlags();
                    }
                });
            } else {
                $("#board").empty();
            }
            // Reset flags
            $("#flagContainer").html(
              '<div id="flag1" data-number="1" class="flags ">\
                    <p>1 &#x2690;</p>\
                </div>\
                <div id="flag2" data-number="2" class="flags">\
                    <p>2 &#x2690;</p>\
                </div>\
                <div id="flag3" data-number="3" class="flags">\
                    <p>3 &#x2690;</p>\
                </div>\
                <div id="flag4" data-number="4" class="flags">\
                    <p>4 &#x2690;</p>\
                </div>'
               );

            $("#flagString").val("");

            // Makes flags draggable
            $(".flags").draggable({
                revert: "invalid", // When not dropped, the element will revert back to its initial position
                containment: "document",
                cursor: "move"
            }).attr('unselectable', 'on').on('selectstart', false);

        }
    });

    // Make flag container droppable
    $("#flagContainer").droppable({
        accept: ".boardSquare div",
        hoverClass: "ui-state-hover",
        drop: function (event, ui) {
            $(ui.draggable).parent().droppable("enable");
            $(ui.draggable).detach().css({ top: "auto", left: "auto"}).appendTo(this);
            assignFlags();
        }
    });

    // Assigns the flags to the selected squares
    function assignFlags() {
        var placed = "[ ";
        var first = true;
        $("#board .flags").each(function () {
            if (!first) {
                placed += ", ";
            }
            var coord = $(this).parent().attr("id");
            first = false;
            placed += "[ " + $(this).data("number") + ", " + coord.substring(0, coord.indexOf("_")) + ", " + coord.substring(coord.indexOf("_") + 1) + " ]";
        });
        placed += " ]";
        $("#flagString").val(placed);
    }

    // Check to make sure a flag is placed
    $("#setupForm").submit(function () {
        var placed = $("#flagString").val();
        if (!placed || $("#flagString").val() == "[  ]")
        {
            $("#flagString").val("");
        }
    });
});