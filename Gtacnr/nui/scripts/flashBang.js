function OnNuiMessage(event) {
    if (event.data.method == "setFlashBang") {
        $('#flashBang').css('opacity', event.data.intensity);
        $('#flashBang').css('background-color', event.data.blackoutMode ? "black" : "white");
    }
}

window.addEventListener("message", OnNuiMessage);

function TriggerNUIEvent(eventName, data = { content: "none" }, callback = function(){}) {
    $.post(
        "https://gtacnr/" + eventName, 
        JSON.stringify(data), 
        callback
    );
}