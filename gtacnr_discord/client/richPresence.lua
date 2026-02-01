-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2021 Sasinosoft Games, Strazzullo.NET LLC
-- All Rights Reserved
-- 

local playerState = {}

RegisterNetEvent('gtacnr:updateLoginInfo')
AddEventHandler('gtacnr:updateLoginInfo', function(jsonData)
    playerState = json.decode(jsonData)
end)

Citizen.CreateThread(function() 
    Citizen.Wait(5000)
    while true do 
        SetDiscordAppId("1066406231243235458")
        SetDiscordRichPresenceAsset("cnrv")
        SetDiscordRichPresenceAssetSmall("sasinosoft")
        SetDiscordRichPresenceAssetText("Cops and Robbers V")
        SetDiscordRichPresenceAssetSmallText("Sasinosoft Games")
        SetDiscordRichPresenceAction(0, "Play Now", "https://gtacnr.net/play")
        SetDiscordRichPresenceAction(1, "Join Discord", "https://gtacnr.net/discord")
        
        local srvId = ""
        if GlobalState.ServerId then 
            srvId = GlobalState.ServerId .. " | "
        end
        
        local maxPlayers = 64
        if GlobalState.MaxPlayers then 
            maxPlayers = GlobalState.MaxPlayers
        end

        local playerId = tostring(GetPlayerServerId(PlayerId()))
        if playerState[playerId] then 
            local username = playerState[playerId].Username
            local richPresenceString = username .. " (" .. tostring(playerId) .. ")\n" .. srvId .. tostring(tablelength(playerState)) .. "/ " .. tostring(maxPlayers) .. " players"
            SetRichPresence(richPresenceString)
        else
            SetRichPresence(tostring(#playerState) .. "/" .. tostring(maxPlayers) .. " players")
        end
        
        Citizen.Wait(30000)
    end
end)

function tablelength(T)
    local count = 0
    for _ in pairs(T) do count = count + 1 end
    return count
end