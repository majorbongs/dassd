-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2021 Sasinosoft Games
-- All Rights Reserved
-- 

Citizen.CreateThread(function()
    while true do 
        Citizen.Wait(1)
        DisablePlayerVehicleRewards(PlayerId())
    end
end)
