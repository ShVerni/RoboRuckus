$(function () {
    $("#bots").selectmenu();
    $("#botDir").selectmenu();
    $("#button").button();
    var botNames = $.parseJSON(($('<textarea/>').html($("#bots").data("names"))).text());
    $("#bots").html('<option value="">Select a Robot</option>');
    $.each(botNames, function () {
        $("#bots").append('<option value="' + this + '">' + this + "</option>");
    });
});