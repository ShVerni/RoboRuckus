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

    // Create the tuning sliders
    $(".tuneFactor").slider({
        slide: function (event, ui) {
            $("#" + $(this).attr("id") + "-handle").text(ui.value);
        }
    });

    // Set the ranges and step sizes on the sliders
    $(".driveFactor").slider("option", "max", 110);
    $(".driveFactor").slider("option", "min", 88);
    $(".driveFactor").slider("option", "step", 1);

    $("#Z_threshold").slider("option", "max", 0);
    $("#Z_threshold").slider("option", "min", -200);
    $("#Z_threshold").slider("option", "step", 1);

    $("#turnBoost").slider("option", "max", 10);
    $("#turnBoost").slider("option", "min", 0);
    $("#turnBoost").slider("option", "step", 1);

    $("#drift_threshold").slider("option", "max", 10);
    $("#drift_threshold").slider("option", "min", 0);
    $("#drift_threshold").slider("option", "step", 1);

    $("#turnFactor").slider("option", "max", 2);
    $("#turnFactor").slider("option", "min", 0.5);
    $("#turnFactor").slider("option", "step", 0.01);

    $("#turn_drift_threshold").slider("option", "max", 1.5);
    $("#turn_drift_threshold").slider("option", "min", 0);
    $("#turn_drift_threshold").slider("option", "step", 0.1);

    $("#finish").button().click(finish);
    $("#speedtest").button().click(speedTest);
    $("#navtest").button().click(navTest);
});

// Puts a selected robot in setup mode
function configureRobot(robotNumber) {
    $.get("/Setup/botConfig", { bot: robotNumber, choice: -1, value: "" }, function (data) { inSetupMode(data, robotNumber); }, "text");
    $("#robots").hide(500);
    $("#controls").show(500).data("robot", robotNumber);
}

// Saves all the settings to the robot and has the robot exit setup mode
function finish() {
    var data = "";
    var first = true;
    // Collect each setting and build comma-separated string
    $(".tuneFactor").each(function () {
        var param = $(this).slider("option", "value");
        var id = $(this).attr("id");
        // Correct for orientation of wheels
        if (id === "leftBackwardSpeed" || id === "rightForwardSpeed") {
            param = 180 - param;
        }
        if (first) {
            data += param.toString();
            first = false;
        }
        else {
            data += "," + param.toString();
        }
    });
    data += "," + encodeURIComponent($("#robotName").val());

    // Send settings to the robot
    $.get("/Setup/botConfig", { bot: $("#controls").data("robot"), choice: 13, value: data });

    // Update robot name in button
    $("#bot-" + $("#controls").data("robot")).text = $("#robotName").val();

    // Hide controls
    $("#robots").show(500);
    $("#controls").hide(500).data("robot", 0);

    // Reset initial slider values
    $(".tuneFactor").each(function () {
        $(this).slider("option", "value", 0);
        $("#" + $(this).attr("id") + "-handle").text(0);
    });
}

// Saves speed values to robot and runs speed test
function speedTest() {
    var data = "";
    var first = true;
    // Collect each setting and build comma-separated string
    $(".driveFactor").each(function () {
        var param = $(this).slider("option", "value");
        var id = $(this).attr("id");
        // Correct for orientation of wheels
        if (id === "leftBackwardSpeed" || id === "rightForwardSpeed") {
            param = 180 - param;
        }
        if (first) {
            data += param.toString();
            first = false;
        }
        else {
            data += "," + param.toString();
        }
    });
    // Send the settings to the robot
    $.get("/Setup/botConfig", { bot: $("#controls").data("robot"), choice: 11, value: data });
}

// Saves navigation values to robot and runs navigation test
function navTest() {
    var data = "";
    var first = true;
    // Collect each setting and build comma-separated string
    $(".navFactor").each(function () {
        if (first) {
            data += $(this).slider("option", "value").toString();
            first = false;
        }
        else {
            data += "," + $(this).slider("option", "value").toString();
        }
    });
    // Send settings to the robot
    $.get("/Setup/botConfig", { bot: $("#controls").data("robot"), choice: 12, value: data });
}

// When a robot successfully enters setup mode, get its current status
function inSetupMode(data, robotNumber) {
    // Checks if robot entered setup mode successfully
    if (data === "OK") {
        // Get robot's current settings
        $.get("/Setup/botConfig", { bot: robotNumber, choice: 10, value: "" }, function (data) { processInitialValues(data); }, "text");
    }
    else {
        // Robot didn't entered setup mode, reset sliders and return to bot selection
        $("#robots").show(500);
        $("#controls").hide(500).data("robot", 0);

        $(".tuneFactor").each(function () {
            $(this).slider("option", "value", 0);
            $("#" + $(this).attr("id") + "-handle").text(0);
        });
    }
}

// Processes a robots status and updates the tuning sliders with the current settings
function processInitialValues(data) {
    // Data is a comma-separated string
    var parameters = data.split(",");

    var value = Number(parameters[0]);
    $("#leftForwardSpeed").slider("option", "value", value);
    $("#leftForwardSpeed-handle").text(value);

    // Correct for wheel orientation
    value = 180 - Number(parameters[1]);
    $("#rightForwardSpeed").slider("option", "value", value);
    $("#rightForwardSpeed-handle").text(value);

    value = Number(parameters[2]);
    $("#rightBackwardSpeed").slider("option", "value", value);
    $("#rightBackwardSpeed-handle").text(value);

    // Correct for wheel orientation
    value = 180 - Number(parameters[3]);
    $("#leftBackwardSpeed").slider("option", "value", value);
    $("#leftBackwardSpeed-handle").text(value);

    value = Number(parameters[4]);
    $("#Z_threshold").slider("option", "value", value);
    $("#Z_threshold-handle").text(value);

    value = Number(parameters[5]);
    $("#turnBoost").slider("option", "value", value);
    $("#turnBoost-handle").text(value);

    value = Number(parameters[6]);
    $("#drift_threshold").slider("option", "value", value);
    $("#drift_threshold-handle").text(value);

    value = Number(parameters[7]);
    $("#turn_drift_threshold").slider("option", "value", value);
    $("#turn_drift_threshold-handle").text(value);

    value = Number(parameters[8]);
    $("#turnFactor").slider("option", "value", value);
    $("#turnFactor-handle").text(value);

    value = decodeURIComponent(parameters[9]);
    $("#robotName").val(value);
}