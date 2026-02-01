-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2021 Sasinosoft Games
-- All Rights Reserved
-- 

local damageType

local function Check()
    --
    local ped = PlayerPedId()
    if not ped then 
        return
    end

    --
    local retval, weapon = GetCurrentPedWeapon(ped, true)
    damageType = GetWeaponDamageType(weapon)
end

local function Update()
    SetPlayerLockon(PlayerId(), damageType == 2)
end

Citizen.CreateThread(function()
    while true do
        Citizen.Wait(250)
        Check()
    end
end)

Citizen.CreateThread(function()
    while true do
        Citizen.Wait(0)
        Update()
    end
end)
