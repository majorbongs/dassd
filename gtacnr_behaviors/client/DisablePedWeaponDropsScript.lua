-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2021 Sasinosoft Games
-- All Rights Reserved
-- 

local function Update()
    for ped in EnumeratePeds() do
        if not IsPedAPlayer(ped) then
            SetPedDropsWeaponsWhenDead(ped, false)
        end
    end
end

Citizen.CreateThread(function()
    while true do 
        Citizen.Wait(1000)
        Update()
    end
end)
