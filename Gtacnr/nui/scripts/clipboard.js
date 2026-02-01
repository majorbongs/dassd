function OnNuiMessage(event) {
    if (event.data.method == "copyText") {
        copyTextToClipboard(event.data.content);
    }
}

window.addEventListener("message", OnNuiMessage);

function copyTextToClipboard(text) {
    var copyFrom = $('<textarea/>');
    copyFrom.text(text);
    $('body').append(copyFrom);
    copyFrom.select();
    document.execCommand('copy');
    copyFrom.remove();
}