-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2021 Sasinosoft Games
-- All Rights Reserved
-- 

local function Update()
    SetVehicleDensityMultiplierThisFrame(0.85)
    SetPedDensityMultiplierThisFrame(0.85)
    SetRandomVehicleDensityMultiplierThisFrame(0.70)
    SetParkedVehicleDensityMultiplierThisFrame(1.0)
    SetScenarioPedDensityMultiplierThisFrame(0.50, 0.50)
end

Citizen.CreateThread(function()
    while true do 
        Citizen.Wait(0)
        Update()
    end
end)
