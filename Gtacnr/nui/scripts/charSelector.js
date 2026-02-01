console.log('charSelector.js loaded');

//
hideCharacterSelection();

//
window.addEventListener("message", OnNuiMessage);

//
characters = [];
highlightedIndex = 0;

//
function OnNuiMessage(event) 
{
    var eventType = event.data.type;
    if (eventType === "showCharacterSelection")
    {
        characters = event.data.characters;
        detachListHandlers();
        clearCharacterSelection();

        for (var i = 0; i < characters.length; i++)
        {
            var character = characters[i];
            addCharacterToSelection(character);
            console.log('Added character ' + character.Slot + ' - ' + character.Id);
        }
        // addCharacterToSelection(null); // adds the "create character" button
        setHighlightedIndex(0);
        showCharacterSelection();
        attachListHandlers();
    }
    else if (eventType === "hideCharacterSelection")
    {
        hideCharacterSelection();
    }
    // controls
    else if (eventType === "control")
    {
        var control = event.data.control;
        if (control === "frontendUp")
        {
            var newIndex = highlightedIndex - 1;
            if (newIndex < 0)
                newIndex = characters.length;

            setHighlightedIndex(newIndex);
        }
        else if (control === "frontendDown")
        {
            var newIndex = highlightedIndex + 1;
            if (newIndex > characters.length)
                newIndex = 0;
            
            setHighlightedIndex(newIndex);
        }
        else if (control === "frontendAccept")
        {
            selectCharacter(highlightedIndex);
        }
        else if (control === "frontendY")
        {
            createCharacter();
        }
    }
}

//
function showCharacterSelection() 
{
    $("#character-selection").show();
}

function hideCharacterSelection() 
{
    $("#character-selection").hide();
}

function addCharacterToSelection(character)
{
    if (character != null)
    {
        $("#list-panel").append(`
            <div class="list-item list-item-character">
                Character ` + character.Slot + `
            </div>
        `);
    }
    else
    {
        $("#list-panel").append(`
            <div class="list-item list-item-create">
                Create a new character
            </div>
        `);
    }
}

function clearCharacterSelection()
{
    $("#list-panel").empty();
}

function attachListHandlers()
{
    $(".list-item").on("click", onListItemClick);
}

function detachListHandlers()
{
    $(".list-item").off("click", onListItemClick);
}

function onListItemClick()
{
    var idx = $(this).index();
    if (idx == highlightedIndex)
        selectCharacter(idx);
    else
    {
        TriggerNUIEvent("onHighlight");
        setHighlightedIndex(idx);
    }
}

function setHighlightedIndex(index)
{
    $(".list-item").removeClass("list-item-active");
    $("#list-panel").children().eq(index).addClass("list-item-active");
    highlightedIndex = index;
    updateInfoPanel(index);
}

function updateInfoPanel(index)
{
    if (index < characters.length)
    {
        var character = characters[index];
        $("#info-title").text("Character " + character.Slot);
        $("#info-subtitle").text("Last played on " + character.LastPlayDateS);
        $("#info-content").html(`
            <div>Sex: ` + character.SexS + `</div>
            <div>Job: ` + character.JobS + `</div>
            <div>Wanted Level: ` + character.WantedLevel + `</div>
            <div>Bounty: ` + character.Bounty + `</div>
            <div>Created: ` + character.CreationDateS + `</div>
        `);
    }
    else
    {
        $("#info-title").text("New character");
        $("#info-subtitle").text("Create a new character");
        $("#info-content").html(
            "Create a new character, with its own progress, money, properties and stats. " + 
            "You can switch between your characters when you connect to the server."
        );
    }
}

function selectCharacter(index)
{
    if (highlightedIndex == characters.length)
    {
        createCharacter();
        return;
    }

    TriggerNUIEvent("onSelectCharacter", { index: index });
}

function createCharacter()
{
    TriggerNUIEvent("onCreateCharacter");
}

function TriggerNUIEvent(eventName, data = { content: "none" }, callback = function(){}) {
    $.post(
        "https://gtacnr/" + eventName, 
        JSON.stringify(data), 
        callback
    );
}
