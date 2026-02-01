/*
 * Grand Theft Auto: Cops n' Robbers
 * Copyright (c) 2020-2025 Sasinosoft Games, Strazzullo.NET LLC
 * All Rights Reserved
 */

console.log('audio.js loaded');

var audio;

function OnNuiMessage(event) {
    if (event.data.method == "play") {
        var path = "sfx/" + event.data.fileName;
        
        audio = new Audio(path);
        audio.volume = 1.0;
        audio.loop = false;

        if (event.data.volume != undefined)
            audio.volume = event.data.volume;
            
        if (event.data.loop != undefined)
            audio.loop = event.data.loop;

        audio.play();
    }
    else if (event.data.method == "stop") {
        audio.pause();
        audio.currentTime = 0;
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
