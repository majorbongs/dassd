-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2021 Sasinosoft Games
-- All Rights Reserved
-- 

local isFlyingPlane
local vehicle

local function Check()
    --
    isFlyingPlane = false

    -- 
    local ped = PlayerPedId()
    if not ped then 
        return
    end

    --
    vehicle = GetVehiclePedIsIn(ped, false)
    if not vehicle then
        return
    end

    --
    local model = GetEntityModel(vehicle)
    if not IsThisModelAPlane(model) or not IsEntityInAir(vehicle) then
        return
    end

    isFlyingPlane = true
end

local function Update()
    if isFlyingPlane then 
        SetVehicleEngineOn(vehicle, true, true, false)
        SetVehicleJetEngineOn(vehicle, true)
        SetVehicleCeilingHeight(vehicle, 30000.0)
    end
    ExtendWorldBoundaryForPlayer(-50000.0, -50000.0, 0.0)
    ExtendWorldBoundaryForPlayer(50000.0, 50000.0, 0.0)
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
