let inputText = null;

console.log('inputDialog.js loaded');

window.addEventListener("message", OnNuiMessage);

function OnNuiMessage(event) {
    var method = event.data.method;

    if (method == "showDialog") {
        ShowDialog(event.data.dialog);
    }
}

//
function ShowDialog(dialog) {
    $('#input-dialog-header').text(dialog.Header);
    $('#input-dialog-content').text(dialog.Content);
    $('#input-dialog-submit').text(dialog.SubmitLabel);
    $('#input-dialog-cancel').text(dialog.CancelLabel);

    $('#input-dialog-input').val("");
    $('#input-dialog-input').attr("type", dialog.InputType);
    $('#input-dialog-input').attr("placeholder", dialog.Placeholder);

    if (dialog.DefaultText) {
        $('#input-dialog-input').val(dialog.DefaultText);
    }

    if (dialog.MaxLength > 0) {
        $('#input-dialog-input').attr("maxlength", dialog.MaxLength);
    }

    inputText = null;
    $('#input-dialog').modal();
}

function SendDialogResponse() {
    TriggerNUIEvent("gtacnr:onDialogResponse", { Input: inputText });
}

//
function TriggerNUIEvent(eventName, data = { content: "none" }, callback = function(){}) {
    $.post(
        "https://gtacnr/" + eventName, 
        JSON.stringify(data), 
        callback
    );
}

$(document).ready(function() {
    $('#input-dialog').on('shown.bs.modal', function () {
        $('#input-dialog-input').trigger('focus');
    });

    $('#input-dialog').on('hidden.bs.modal', function (e) {
        SendDialogResponse();
    });

    $('#input-dialog-submit').on('click', function(e) {
        inputText = $("#input-dialog-input").val();
    });

    $('#input-dialog-modal-content').on('keypress', function(e) {
        if (e.which == 13 && !e.ctrlKey) {
            inputText = $("#input-dialog-input").val();
            $('#input-dialog').modal('hide');
        }
    });
});
