// Maps the direction indicators to corresponding integer values
var dirs = ["X", "Y", "NEG_X", "NEG_Y"];

$(function () {
    // Create buttons.
    $("#submitButton").button();
    $("#printImg").button();
    $("#delete").button().click(function () {
        // Create confirmation modal dialog
        $("#dialog-confirm").dialog({
            resizable: false,
            height: "auto",
            width: 400,
            modal: true,
            buttons: {
                "Delete board": function () {
                    $(this).dialog("close");
                    var Name = $("#name").val();
                    // Send post request to delete board
                    $.post("/Setup/deleteBoard", { name: Name })
                        .done(function (data) {
                            // Remove board from the select menu
                            $("#boardSel").val('');
                            $("#boardSel option[value='" + Name + "']").remove();
                            $("#boardSel").selectmenu("refresh");
                            $("#boardSel").change();
                            $("#name").val("");
                            $("#nameContainer").show();
                            $("#exButtons").hide();
                            alert("Board deleted");
                        });
                },
                Cancel: function () {
                    $(this).dialog("close");
                }
            }
        });
    });

    // Enable the board selection menu.
    $("#boardSel").selectmenu({
        width: null,
        change: function (event, ui) {
            var name = $(this).val();
            if (name) {
                $("#name").val(name);
                $("#printImg").attr("href", "/images/printable_boards/" + name.replace(" ", "") + ".png");
                $("#nameContainer").hide();
                $("#exButtons").show();
                createBoard(name);
            } else {
                $("#name").val("");
                $("#nameContainer").show();
                $("#exButtons").hide();
            }
        }
    });

    // Intercept submit.
    $("#boardMakerForm").submit(function (e) {
        // Add board date to form
        $("#boardData").val(buildBoard());
        // Add corner date to form
        $("#cornerData").val(getCorners());
    });

    // Redraw board on size change.
    $(".boardSize").change(function () {
        drawBoard($("#x_size").val(), $("#y_size").val());
    });
    
    // Makes board elements draggable.
    $(".element").draggable({
        revert: "invalid", // When not dropped, the element will revert back to its initial position
        containment: "document",
        helper: "clone", // Use a clone helper
        cursor: "move"
    }).attr('unselectable', 'on').on('selectstart', false);

    // Make disposal container droppable.
    $("#trashCan").droppable({
        accept: ".boardSquare div",
        hoverClass: "boardSquare-hover",
        activeClass: "ui-state-highlight",
        drop: function (event, ui) {
            $(ui.draggable).parent().droppable("option", "accept", "#flagContainer div, .boardSquare div");
            $(ui.draggable).remove();
        }
    });

    // Allow for rotateable elements to rotate.
    $("#boardMaker").on("click", ".boardSquare .rotateable", function () {
        var rotation = 0;
        var current = Number($(this).data("orientation"));
        if (current !== 3) {
            current++;
            rotation = -90 * current;
            $(this).data("orientation", current);
        } else {
            $(this).data("orientation", 0);
        }
        $(this).css({ 'transform': 'rotate(' + rotation + 'deg)' });
    });
});

// Draws the board
function drawBoard(x, y) {
    if (x > 0 && y > 0) {
        $("#boardMaker").empty();
        board = "";
        // Draw board squares
        for (var i = y - 1; i >= 0; i--) {
            board += '<div class="boardRow" id="y_' + i + '">';
            for (var j = 0; j <= x - 1; j++) {
                board += '<div class="boardSquare" id="' + j + '_' + i + '"></div>';
            }
            board += '</div>';
        }
        // Add board to page
        $("#boardMaker").html(board);
        // Make board squares droppable
        $(".boardSquare").droppable({
            accept: "#flagContainer div, .boardSquare div",
            hoverClass: "boardSquare-hover",
            drop: function (event, ui) {
                // Only accept one element per board square with the exception of stackable elements
                $(this).droppable("option", "accept", ".stackable");
                if ($(ui.draggable).parent().hasClass("boardSquare") && $(ui.draggable).parent().children().length === 1) {
                    $(ui.draggable).parent().droppable("option", "accept", "#flagContainer div, .boardSquare div");
                }
                // Check if this is a new element or one already on the board being moved
                if (!ui.draggable.hasClass("dropped")) {
                    // Clone the original element and make it draggable
                    $(ui.draggable).clone().draggable({
                        revert: "invalid", // When not dropped, the element will revert back to its initial position
                        containment: "document",
                        cursor: "move",
                        appendTo: 'body',                       
                        scroll: false,
                        helper: 'clone',
                        start: function (evet, ui) {
                            $(this).hide();
                        },
                        stop: function (evet, ui) {
                            $(this).show();
                        }
                    })
                    .addClass("dropped") // Mark this as an on the board element
                    .attr('unselectable', 'on')
                    .on('selectstart', false)
                    .appendTo(this)
                    .css({ top: "", left: "", position: "" }); // Fix the CSS
                } else {
                    // Add the element to the square and fix the CSS
                    $(ui.draggable).detach().appendTo(this).css({ top: "", left: "", position: "" });
                }
            }
        });
    }
}

// Returns a JSON string containing the board info
function buildBoard() {
    var boardString = "{\n  \"name\": \"" + $("#name").val() + "\",\n  \"size\": [ " + (Number($("#x_size").val()) - 1) + ", " + (Number($("#y_size").val()) - 1) + " ]";
    boardString += ",\n" + getWrenches();
    boardString += ",\n" + getPits();
    boardString += ",\n" + getTurntables();
    boardString += ",\n" + getLasers();
    boardString += ",\n" + getConveyors(false);
    boardString += ",\n" + getConveyors(true);
    boardString += ",\n" + getWalls();
    boardString += "\n}";
    return boardString;
}

// Returns a string cantoning the wrench data
function getWrenches() {
    var wrenches = "  \"wrenches\": [";
    var first = true;
    $("#boardMaker .wrench").each(function () {
        if (!first) {
            wrenches += ",\n      [ ";
        } else {
            wrenches += "\n      [ ";
            first = false;
        }
        var coord = $(this).parents(".boardSquare").attr("id");
        wrenches += coord.substring(0, coord.indexOf("_")) + ", " + coord.substring(coord.indexOf("_") + 1) + " ]";
    });
    wrenches += "  \n  ]";
    return wrenches;
}

// Returns a string cantoning the pit data
function getPits() {
    var pits = "  \"pits\": [";
    var first = true;
    $("#boardMaker .pit").each(function () {
        if (!first) {
            pits += ",\n      [ ";
        } else {
            pits += "\n      [ ";
            first = false;
        }
        var coord = $(this).parents(".boardSquare").attr("id");
        pits += coord.substring(0, coord.indexOf("_")) + ", " + coord.substring(coord.indexOf("_") + 1) + " ]";
    });
    pits += "\n  ]";
    return pits;
}

// Returns a string containing the turntable data
function getTurntables() {
    var turntables = "  \"turntables\": [";
    var first = true;
    // Get left turntables
    $("#boardMaker .CCWR").each(function () {
        if (!first) {
            turntables += ",\n      {";
        } else {
            turntables += "\n      {";
            first = false;
        }
        var coord = $(this).parents(".boardSquare").attr("id");
        turntables += "\n       \"location\": [ " + coord.substring(0, coord.indexOf("_")) + ", " + coord.substring(coord.indexOf("_") + 1) + " ],\n       \"dir\": \"left\"\n      }";
    });

    // Get right turntables
    $("#boardMaker .CWR").each(function () {
        if (!first) {
            turntables += ",\n      {";
        } else {
            turntables += "\n      {";
            first = false;
        }
        var coord = $(this).parents(".boardSquare").attr("id");
        turntables += "\n       \"location\": [ " + coord.substring(0, coord.indexOf("_")) + ", " + coord.substring(coord.indexOf("_") + 1) + " ],\n       \"dir\": \"right\"\n      }";
    });
    turntables += "\n  ]";
    return turntables;
}

// Returns a string containing the laser data
function getLasers() {
    var lasers = "  \"lasers\": [";
    var first = true;
    $("#boardMaker .laser").each(function () {
        var coord = $(this).parents(".boardSquare").attr("id");
        var coordArray = [Number(coord.substring(0, coord.indexOf("_"))), Number(coord.substring(coord.indexOf("_") + 1))];
        var orientation = Number($(this).data("orientation"));
        var end = checkLOS(coordArray[0], coordArray[1], orientation);
        if (!first) {
            lasers += ",\n      {";
        } else {
            lasers += "\n      {";
            first = false;
        }
        lasers += "\n       \"start\": [ " + coordArray[0] + ", " + coordArray[1] + " ],\n       \"end\": [ " + end[0] + ", " + end[1] + " ],\n       \"strength\": " + $(this).data("strength") + ",\n       \"facing\": \"" + dirs[orientation] + "\"\n      }";
    });
    lasers += "\n  ]";
    return lasers;
}

// Returns a string containing the conveyor, or express conveyor, data
function getConveyors(express) {
    var selector = "#boardMaker .conveyor, #boardMaker .conveyorCurve, #boardMaker .conveyorCurveS";
    var conveyors = "  \"conveyors\": [";
    var first = true;

    // Check if looking for express conveyors
    if (express) {
        selector = "#boardMaker .exConveyor, #boardMaker .exConveyorCurve, #boardMaker .exConveyorCurveS";
        conveyors = "  \"expressConveyors\": [";
    }

    $(selector).each(function () {
        var coord = $(this).parents(".boardSquare").attr("id");
        var coordArray = [Number(coord.substring(0, coord.indexOf("_"))), Number(coord.substring(coord.indexOf("_") + 1))];
        var entrance = Number($(this).data("orientation"));
        var exit;
        switch ($(this).data("shape")) {
            case "l":
                exit = entrance + 2;
                if (exit >= 4) {
                    exit -= 4;
                }
                break;
            case "c":
                exit = entrance + 1;
                if (exit >= 4) {
                    exit -= 4;
                }
                break;
            case "s":
                exit = entrance - 1;
                if (exit < 0) {
                    exit += 4;
                }
                break;
        }
        if (!first) {
            conveyors += ",\n      {";
        } else {
            conveyors += "\n      {";
            first = false;
        }
        conveyors += "\n       \"location\": [ " + coordArray[0] + ", " + coordArray[1] + " ],\n       \"entrance\": \"" + dirs[entrance] + "\",\n       \"exit\": \"" + dirs[exit] + "\"\n      }";
    });

    conveyors += "\n  ]";
    return conveyors;
}

// Returns a string containing the wall data
function getWalls() {
    var walls = "  \"walls\": [";
    var first = true;
    // Add walls
    $("#boardMaker .laser, #boardMaker .wall").each(function () {
        var coord = $(this).parents(".boardSquare").attr("id");
        var coordArray = [Number(coord.substring(0, coord.indexOf("_"))), Number(coord.substring(coord.indexOf("_") + 1))];
        var orientation = Number($(this).data("orientation"));
        var faces;
        if (!first) {
            walls += ",\n      [";
        } else {
            walls += "\n      [";
            first = false;
        }
        if ($(this).hasClass("wall")) {
            switch (orientation) {
                case 0:
                    faces = [coordArray[0] + 1, coordArray[1]];
                    break;
                case 1:
                    faces = [coordArray[0], coordArray[1] + 1];
                    break;
                case 2:
                    faces = [coordArray[0] - 1, coordArray[1]];
                    break;
                case 3:
                    faces = [coordArray[0], coordArray[1] - 1];
                    break;
            }
        }
        else if ($(this).hasClass("laser")) {
            switch (orientation) {
                case 0:
                    faces = [coordArray[0] - 1, coordArray[1]];
                    break;
                case 1:
                    faces = [coordArray[0], coordArray[1] - 1];
                    break;
                case 2:
                    faces = [coordArray[0] + 1, coordArray[1]];
                    break;
                case 3:
                    faces = [coordArray[0], coordArray[1] + 1];
                    break;
            }
        }
        walls += "\n        [ " + coordArray[0] + ", " + coordArray[1] + " ],\n        [ " + faces[0] + ", " + faces[1] + " ]\n      ]";
    });
    // Add corners
    $("#boardMaker .corner").each(function () {
        var coord = $(this).parents(".boardSquare").attr("id");
        var coordArray = [Number(coord.substring(0, coord.indexOf("_"))), Number(coord.substring(coord.indexOf("_") + 1))];
        var orientation = Number($(this).data("orientation"));
        var faces;
        var alsoFaces;
        if (!first) {
            walls += ",\n      [";
        } else {
            walls += "\n      [";
            first = false;
        }
        switch (orientation) {
            case 0:
                faces = [coordArray[0] + 1, coordArray[1]];
                alsoFaces = [coordArray[0], coordArray[1] - 1];
                break;
            case 1:
                faces = [coordArray[0], coordArray[1] + 1];
                alsoFaces = [coordArray[0] + 1, coordArray[1]];
                break;
            case 2:
                faces = [coordArray[0] - 1, coordArray[1]];
                alsoFaces = [coordArray[0], coordArray[1] + 1];
                break;
            case 3:
                faces = [coordArray[0], coordArray[1] - 1];
                alsoFaces = [coordArray[0] - 1, coordArray[1]];
                break;
        }
        walls += "\n        [ " + coordArray[0] + ", " + coordArray[1] + " ],\n        [ " + faces[0] + ", " + faces[1] + " ]\n      ]";
        walls += ",\n      [\n      [ " + coordArray[0] + ", " + coordArray[1] + " ],\n        [ " + alsoFaces[0] + ", " + alsoFaces[1] + " ]\n      ]";
    });
    walls += "\n  ]";
    return walls;
}

// Returns an int[][] containing the corner data
function getCorners() {
    var corners = "[";
    var first = true;
    $("#boardMaker .corner").each(function () {
        if (!first) {
            corners += ",[";
        } else {
            corners += "[";
            first = false;
        }
        var orientation = $(this).data("orientation");
        var coord = $(this).parents(".boardSquare").attr("id");
        corners += coord.substring(0, coord.indexOf("_")) + "," + coord.substring(coord.indexOf("_") + 1) + "," + orientation + "]";
    });
    corners += "]";
    return corners;
}

// Lays out a board based on its JSON description.
function createBoard(boardName) {
    $.get("/Setup/getBoard", { name: boardName },
        function (board) {
            $("#x_size").val(board.size[0] + 1);
            $("#y_size").val(board.size[1] + 1).change();
            placeWrenches(board.wrenches);
            placePits(board.pits);
            placeTurntables(board.turntables);
            placeConveyors(board.conveyors, false);
            placeConveyors(board.expressConveyors, true);
            placeWalls(board.walls);
            placeLasers(board.lasers);

            $(".boardSquare .element").draggable({
                revert: "invalid", // When not dropped, the element will revert back to its initial position
                containment: "document",
                cursor: "move",
                appendTo: 'body',
                scroll: false,
                helper: 'clone',
                start: function (evet, ui) {
                    $(this).hide();
                },
                stop: function (evet, ui) {
                    $(this).show();
                }
            })
                .addClass("dropped") // Mark this as an on the board element
                .attr('unselectable', 'on')
                .on('selectstart', false)
                .css({ top: "", left: "", position: "" }); // Fix the CSS
        }, "json");
}

// Places wrenches on a board based on their data
function placeWrenches(wrenches) {
    for (var i = 0; i < wrenches.length; i++) {
        var square = "#" + wrenches[i][0] + "_" + wrenches[i][1];
        $("#flagContainer .wrench").clone().appendTo(square);
        $(square).droppable("option", "accept", ".stackable");
    }
}

// Places pits on a board based on their data
function placePits(pits) {
    for (var i = 0; i < pits.length; i++) {
        var square = "#" + pits[i][0] + "_" + pits[i][1];
        $("#flagContainer .pit").clone().appendTo(square);
        $(square).droppable("option", "accept", ".stackable");
    }
}

// Places turntables on a board based on their data
function placeTurntables(turntables) {
    for (var i = 0; i < turntables.length; i++) {
        var turntable = turntables[i];
        var clone;
        if (turntable.dir === "right") {
            clone = $("#flagContainer .CWR").clone();
        } else {
            clone = $("#flagContainer .CCWR").clone();
        }
        var square = "#" + turntable.location[0] + "_" + turntable.location[1];
        clone.appendTo(square);
        $(square).droppable("option", "accept", ".stackable");
    }
}

// Places lasers on the a board based on their data
function placeLasers(lasers) {
    for (var i = 0; i < lasers.length; i++) {
        var laser = lasers[i];
        var clone;
        var beamClone;
        if (laser.strength === 1) {
            clone = $("#flagContainer .laser-1").clone();
            beamClone = "#flagContainer .beam-1";
        } else if (laser.strength === 2) {
            clone = $("#flagContainer .laser-2").clone();
            beamClone = "#flagContainer .beam-2";
        } else {
            clone = $("#flagContainer .laser-3").clone();
            beamClone = "#flagContainer .beam-3";
        }        
        var square = "#" + laser.start[0] + "_" + laser.start[1];
        if ($(square + " .element").length === 0) {
            $(square).droppable("option", "accept", ".stackable");
        }

        $(square + " .wall").each(function () {
            if (Math.abs($(this).data("orientation") - laser.facing) === 2) {
                $(this).remove();
            }
        });
        clone.appendTo(square).css({ 'transform': 'rotate(' + -90 * laser.facing + 'deg)' }).data("orientation", laser.facing);

        if (laser.facing === 1 || laser.facing === 3) {
            var beamStart = laser.start[1];
            var beamEnd = laser.end[1];
            var x = laser.start[0];
            if (laser.facing === 1) {
                for (; beamStart <= beamEnd; beamStart++) {
                    var copy = $(beamClone).clone();
                    copy.prependTo("#" + x + "_" + beamStart);
                    copy.css({ 'transform': 'rotate(-90deg)' }).data("orientation", 1);
                }
            } else {
                for (; beamStart >= beamEnd; beamStart--) {
                    copy = $(beamClone).clone();
                    copy.prependTo("#" + x + "_" + beamStart);
                    copy.css({ 'transform': 'rotate(-90deg)' }).data("orientation", 1);
                }
            }
        } else {
            beamStart = laser.start[0];
            beamEnd = laser.end[0];
            var y = laser.start[1];
            if (laser.facing === 0) {
                for (; beamStart <= beamEnd; beamStart++) {
                    $(beamClone).clone().prependTo("#" + beamStart + "_" + y);
                }
            } else {
                for (; beamStart >= beamEnd; beamStart--) {
                    $(beamClone).clone().prependTo("#" + beamStart + "_" + y);
                }
            }
        }
    }
}

// Places walls on a board based on their data (cannot place corners separately)
function placeWalls(walls) {
    for (var i = 0; i < walls.length; i++) {
        var wall = walls[i];
        
        var square = "#" + wall[0][0] + "_" + wall[0][1];
        var rotation = 0;
        if (wall[0][0] === wall[1][0]) {
            if (wall[0][1] > wall[1][1]) {
                rotation = 3;
            } else {
                rotation = 1;
            }
        } else {
            if (wall[0][0] > wall[1][0]) {
                rotation = 2;
            }
        }
        if ($(square + " .element").length === 0) {
            $(square).droppable("option", "accept", ".stackable");
        }
        $("#flagContainer .wall").clone().appendTo(square).css({ 'transform': 'rotate(' + -90 * rotation + 'deg)' }).data("orientation", rotation);
    }
}

// Places conveyors or express conveyors on a board based on their data
function placeConveyors(conveyors, express) {
    for (var i = 0; i < conveyors.length; i++) {
        var conveyor = conveyors[i];
        var square = "#" + conveyor.location[0] + "_" + conveyor.location[1];
        var difference = conveyor.entrance - conveyor.exit;
        var clone;
        if (!express) {
            if (Math.abs(difference) === 2) {
                clone = $("#flagContainer .conveyor").clone();
            } else if (difference === -1 || difference === 3) {
                clone = $("#flagContainer .conveyorCurve").clone();
            } else {
                clone = $("#flagContainer .conveyorCurveS").clone();
            }
        } else {
            if (Math.abs(difference) === 2) {
                clone = $("#flagContainer .exConveyor").clone();
            } else if (difference === -1 || difference === 3) {
                clone = $("#flagContainer .exConveyorCurve").clone();
            } else {
                clone = $("#flagContainer .exConveyorCurveS").clone();
            }
        }
        clone.appendTo(square).css({ 'transform': 'rotate(' + -90 * conveyor.entrance + 'deg)' }).data("orientation", conveyor.entrance);
        $(square).droppable("option", "accept", ".stackable");
    }
}

// Helper function that checks if there is line of
// sight from starting x, y coordinates in a direction.
// Returns the furthest coordinate to which LOS extends.
// This is a very ugly function.
function checkLOS(x, y, dir) {
    var max_x = $("#x_size").val();
    var max_y = $("#y_size").val();
    var first = true;
    var done = false;
    do {
        // Get blocking elements in square
        var walls = $("#" + x + "_" + y).find(".wall");
        var lasers = $("#" + x + "_" + y).find(".laser-1", ".laser-2", ".laser-3");
        var corners = $("#" + x + "_" + y).find(".corner");
        var blockers;
        // Check direction of search
        switch (dir) {
            case 0:
                // Check for corners in the way
                corners.each(function () {
                    var orientation = Number($(this).data("orientation"));
                    // Check which side of square corner is on
                    if (orientation === 0 || orientation === 1) {
                        done = true;
                    } else {
                        x--;
                        done = true;
                    }
                });
                // Check for walls in the way
                walls.each(function () {
                    var orientation = Number($(this).data("orientation"));
                     // Check which side of square wall is on
                    if (orientation === 0) {
                        done = true;
                    } else if (orientation === 2) {
                        x--;
                        done = true;
                    }
                });
                // Check for lasers in the way
                lasers.each(function () {
                    var orientation = Number($(this).data("orientation"));
                    // Check which side of square laser is on
                    if (orientation === 2) {
                        done = true;
                    } else if (orientation === 0) {
                        if (!first) {
                            x--;
                            done = true;
                        }
                    }
                });
                // Check if blocking object was found
                if (!done) {
                    x++;
                }
                break;
            // Repeat the above process but for each direction of movement
            case 2:
                corners.each(function () {
                    var orientation = Number($(this).data("orientation"));
                    if (orientation === 2 || orientation === 3) {
                        done = true;
                    } else {
                        x++;
                        done = true;
                    }
                });
                walls.each(function () {
                    var orientation = Number($(this).data("orientation"));
                    if (orientation === 2) {
                        done = true;
                    } else if (orientation === 0) {
                        x++;
                        done = true;
                    }
                });
                lasers.each(function () {
                    var orientation = Number($(this).data("orientation"));
                    if (orientation === 0) {
                        done = true;
                    } else if (orientation === 2) {
                        if (!first) {
                            x++;
                            done = true;
                        }
                    }
                });
                if (!done) {
                    x--;
                }
                break;
            case 1:
                corners.each(function () {
                    var orientation = Number($(this).data("orientation"));
                    if (orientation === 1 || orientation === 2) {
                        done = true;
                    } else {
                        y--;
                        done = true;
                    }
                });
                walls.each(function () {
                    var orientation = Number($(this).data("orientation"));
                    if (orientation === 1) {
                        done = true;
                    } else if (orientation === 3) {
                        y--;
                        done = true;
                    }
                });
                lasers.each(function () {
                    var orientation = Number($(this).data("orientation"));
                    if (orientation === 3) {
                        done = true;
                    } else if (orientation === 1) {
                        if (!first) {
                            y--;
                            done = true;
                        }
                    }
                });
                if (!done) {
                    y++;
                }
                break;
            case 3:
                corners.each(function () {
                    var orientation = Number($(this).data("orientation"));
                    if (orientation === 0 || orientation === 3) {
                        done = true;
                    } else {
                        y++;
                        done = true;
                    }
                });
                walls.each(function () {
                    var orientation = Number($(this).data("orientation"));
                    if (orientation === 3) {
                        done = true;
                    } else if (orientation === 1) {
                        y++;
                        done = true;
                    }
                });
                lasers.each(function () {
                    var orientation = Number($(this).data("orientation"));
                    if (orientation === 1) {
                        done = true;
                    } else if (orientation === 3) {
                        if (!first) {
                            y++;
                            done = true;
                        }
                    }
                });
                if (!done) {
                    y--;
                }
                break;
            default:
                break;
        }
        first = false;
    } while (x >= 0 && y >= 0 && x < max_x && y < max_y && !done);
    // Check if blocking object was found
    if (done) {
        return [x, y];
    }
    // Adjust since LOS goes off the board.
    switch (dir) {
        case 0:
            return [x - 1, y];
        case 2:
            return [x + 1, y];
        case 1:
            return [x, y - 1];
        case 3:
            return [x, y + 1];
        default:
            // Something went wrong
            return [-1, -1];
    }
}