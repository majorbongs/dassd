-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2023 Sasinosoft Games
-- All Rights Reserved
-- 

local servicesToDisable = {
	1, -- DT_PoliceAutomobile
	2, -- DT_PoliceHelicopter
	-- 3, -- DT_FireDepartment
	4, -- DT_SwatAutomobile
	5, -- DT_AmbulanceDepartment
	6, -- DT_PoliceRiders
	7, -- DT_PoliceVehicleRequest
	-- 8, -- DT_PoliceRoadBlock
	9, -- DT_PoliceAutomobileWaitPulledOver
	10, -- DT_PoliceAutomobileWaitCruising
	11, -- DT_Gangs
	-- 12, -- DT_SwatHelicopter
	-- 13, -- DT_PoliceBoat
    -- 14, -- DT_ArmyVehicle
	15 -- DT_BikerBackup 
}

local function IsInFortZancudo()
    local coords = GetEntityCoords(PlayerPedId())

    if coords.x > -3244 and coords.x < -1559 and
        coords.y > 2870 and coords.y < 3525 and
        coords.z > 29 and coords.z < 100 then

        return true
    end
    return false
end

local function Update()

    if IsInFortZancudo() then 
        for k, i in ipairs(servicesToDisable) do
            EnableDispatchService(i, true)
        end
        return
    end

    for k, i in ipairs(servicesToDisable) do
        EnableDispatchService(i, false)
    end
end

Citizen.CreateThread(function()
    SetPoliceRadarBlips(false)
    while true do
        Citizen.Wait(2000)
        Update()
    end
end)
