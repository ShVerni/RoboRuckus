$(function () {
    $("#bots").selectmenu();
    $("#botDir").selectmenu();
    $("#button").button();
    var nameString = $('<textarea/>').html($("#bots").data("names")).text();
    if (nameString !== "") {
        var botNames = $.parseJSON(nameString);
        $("#bots").html('<option value="">Select a Robot</option>');
        $.each(botNames, function () {
            $("#bots").append('<option value="' + this + '">' + this + "</option>");
        });
    }
    $("#dealMe").button().click(function () {
        $.post("/Setup/redealPlayer", { player: $("#player").val() })
            .done(function (data) {
                $(window.top.document).find('.ui-dialog').remove();
            });
    });
});