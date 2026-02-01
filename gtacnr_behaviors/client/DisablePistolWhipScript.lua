-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2021 Sasinosoft Games
-- All Rights Reserved
-- 

local damageType

local function Update()
    if damageType ~= 2 or IsPedSwappingWeapon(PlayerPedId()) then
        DisableControlAction(2, 140, true)
        DisableControlAction(2, 141, true)
        DisableControlAction(2, 142, true)
        DisableControlAction(2, 263, true)
        DisableControlAction(2, 264, true)
    end
end

local function Update2()
    local ped = PlayerPedId()
    if not ped then 
        return
    end
    
    --
    local retval, weapon = GetCurrentPedWeapon(ped, true)
    damageType = GetWeaponDamageType(weapon)
end

Citizen.CreateThread(function()
    while true do 
        Citizen.Wait(0)
        Update()
    end
end)

Citizen.CreateThread(function()
    while true do 
        Citizen.Wait(100)
        Update2()
    end
end)
