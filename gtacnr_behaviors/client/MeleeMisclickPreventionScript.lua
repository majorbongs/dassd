-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2021 Sasinosoft Games
-- All Rights Reserved
-- 

local function Update()
    local r, weapon = GetCurrentPedWeapon(PlayerPedId(), true)
    local type = GetWeaponDamageType(weapon)

    if type == 2 then 
        if not IsControlPressed(0, 25) then
            DisableControlAction(0, 24, true)
            DisableControlAction(0, 140, true)
            DisableControlAction(0, 141, true)
            DisableControlAction(0, 142, true)
        end
    end
end

Citizen.CreateThread(function()
    while true do
        Citizen.Wait(0)
        Update()
    end
end)
