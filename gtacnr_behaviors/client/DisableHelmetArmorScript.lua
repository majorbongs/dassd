-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2024 Sasinosoft Games
-- All Rights Reserved
-- 

Citizen.CreateThread(function()
    while true do 
        Citizen.Wait(0)
        SetPedConfigFlag(PlayerPedId(), 438, true)
    end
end)
