$(function () {
    $("#controls").hide();
    $("#done").button();

    // Get the list of robots to create the buttons
    bots = $("#robots").data("bots");
    for (var i = 0; i < bots.length; i++) {
        robot = bots[i];
        $("#robots").append('<a class="roboButton" id="bot-' + robot.number + '" href="#" data-number="' + robot.number + '">' + robot.name + '</a>');
    }

    // Create the robot buttons
    $(".roboButton").button();
    $(".roboButton").click(function () {
        configureRobot($(this).data("number"));
    });

    // Create the control buttons
    $("#finish").button().click(finish);
    $("#speedtest").button().click(speedTest);
    $("#navtest").button().click(navTest);
});

// Puts a selected robot in setup mode
function configureRobot(robotNumber) {
    $.get("/Setup/botConfig", { bot: robotNumber, option: 1, value: "" }, function (data) { inSetupMode(data, robotNumber); }, "text");
    $("#controls").data("robot", robotNumber);
}

// Saves the settings to the robot and has the robot exit setup mode
function finish() {
    var data = collectParameters();

    // Send settings to the robot
    $.get("/Setup/botConfig", { bot: $("#controls").data("robot"), option: 3, value: data });
    // Update robot name in button
    $("#bot-" + $("#controls").data("robot")).text($("#robotName").val());

    // Hide controls
    $("#robots").show(500);
    $("#controls").hide(500).data("robot", 0);

    // Remove sliders
    $("#sliders").empty();
}

// Saves values to robot and runs speed test
function speedTest() {
    var data = collectParameters();
    // Send the settings to the robot
    $.get("/Setup/botConfig", { bot: $("#controls").data("robot"), option: 1, value: data });
}

// Saves values to robot and runs navigation test
function navTest() {
    var data = collectParameters();
    // Send settings to the robot
    $.get("/Setup/botConfig", { bot: $("#controls").data("robot"), option: 2, value: data });
}

// When a robot successfully enters setup mode, get its current status
function inSetupMode(data, robotNumber) {
    // Checks if robot entered setup mode successfully
    if (data === "OK") {
        // Get robot's current settings
        $.get("/Setup/botConfig", { bot: robotNumber, option: 0, value: "" }, function (data) { processInitialValues(data); }, "text");
    }
    else {
        // Robot didn't enter setup mode, return to bot selection
        $("#robots").show(500);
        $("#controls").hide(500).data("robot", 0);
        // Remove sliders 
        $("#sliders").empty();
    }
}

// Processes a robots status and updates the tuning sliders with the current settings
function processInitialValues(data) {
    // Data is a JSON string
    var parameters = JSON.parse(data);

    // Display robot name
    $("#robotName").val(decodeURIComponent(parameters.name));

    // Set up tuning sliders
    parameters.controls.forEach((control, index) => {
        // Add slider to DOM
        var id = control.name;
        $("#sliders").append('\
        <p>' + control.displayname + '</p>\
        <div class="slider-wrapper">\
            <a data-slider="' + id + '" class="slide-button slide-button-decrease">&#9664;</a>\
            <div id="' + id + '" class="tuneFactor">\
                <div id = "' + id + '-handle" class= "ui-slider-handle" ></div>\
            </div>\
            <a data-slider="' + id + '" class="slide-button slide-button-increase">&#9654;</a>\
        </div>');

        // Create jQuery UI slider
        $("#" + id).slider({
            slide: function (event, ui) {
                $("#" + id + "-handle").text(ui.value);
            }
        });

        // Set the ranges and step sizes on the slider
        $("#" + id).slider("option", "max", control.max);
        $("#" + id).slider("option", "min", control.min);
        $("#" + id).slider("option", "step", control.increment);

        // Set current value of slider
        $("#" + id).slider("option", "value", control.current);
        $("#" + id + "-handle").text(control.current.toString()).addClass("tuning-handle");
    });

    // Create increment buttons
    $(".slide-button").button().click(function () {
        var id = $(this).data("slider");
        if ($(this).hasClass("slide-button-decrease")) {
            $("#" + id).slider("value", $("#" + id).slider("value") - $("#" + id).slider("option", "step"));
        } else {
            $("#" + id).slider("value", $("#" + id).slider("value") + $("#" + id).slider("option", "step"));
        }
        $("#" + id + "-handle").text($("#" + id).slider("value"));
    });

    // Show slider controls
    $("#robots").hide(500);
    $("#controls").show(500);
}

/* 
*  Collects all the tuning parameters into a comma separated list
*  starting with the name, and then the other parameters in the order
*  they were listed in the JSON object. Terminates with ":"
*  Ex: test%20bot,65,22,55:
*/  
function collectParameters() {
    var data = encodeURIComponent($("#robotName").val());
    $(".tuneFactor").each(function () {
        data += "," + $(this).slider("option", "value").toString();
    });
    data += ":";
    return data;
}