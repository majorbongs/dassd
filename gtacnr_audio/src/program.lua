AddEventHandler("gtacnr:audio:play", function(fileName, volume, loop)
    SendNUIMessage({
            method = "play",
            volume = volume,
            fileName = fileName,
            loop = loop
        }
    )
end)

AddEventHandler("gtacnr:audio:stop", function()
    SendNUIMessage({
            method = "stop"
        }
    )
end)
