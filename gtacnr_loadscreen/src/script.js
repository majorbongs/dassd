/*
 * Grand Theft Auto Cops And Robbers
 * Copyright (c) 2020 - Sasinosoft Games, Strazzullo.NET LLC
 * All Rights Reserved
 *
 * This source code is protected by U.S. and international copyright laws.
 * Usage and distribution without written permission of the author is strictly prohibited.
 */

var muted = false;

$(document).ready(function() {
    $('#videoObject').prop("volume", 0.2);
    $(document).keydown(OnKeyDown);
});

function OnKeyDown(e) {
    if (e.key == "Enter" || e.key == "Return" || e.key == " ") {
        var vol = 0.2;
        muted = !muted;
        if (muted) {
            vol = 0.0;
        }
        $('#videoObject').prop("volume", vol);
    }
}
