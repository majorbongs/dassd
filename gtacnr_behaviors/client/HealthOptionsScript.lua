-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2021 Sasinosoft Games
-- All Rights Reserved
-- 

local function Update()
    -- 
    if not PlayerPedId() then 
        return
    end

    SetPlayerHealthRechargeLimit(PlayerId(), 0)
    SetPlayerHealthRechargeMultiplier(PlayerId(), 0)
    SetPedSuffersCriticalHits(PlayerPedId(), true)
end

Citizen.CreateThread(function()
    while true do 
        Citizen.Wait(500)
        Update()
    end
end)
