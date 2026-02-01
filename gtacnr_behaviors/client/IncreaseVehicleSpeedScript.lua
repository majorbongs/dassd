-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2023 Sasinosoft Games
-- All Rights Reserved
-- 

local function Update()
    if IsPedInAnyVehicle(PlayerPedId(), false) then 
        local veh = GetVehiclePedIsIn(PlayerPedId(), false)
        ModifyVehicleTopSpeed(veh, 15.0)
    end
end

Citizen.CreateThread(function()
    while true do 
        Citizen.Wait(500)
        Update()
    end
end)
