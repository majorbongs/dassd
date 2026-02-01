local mp_pointing = false
local keyPressed = false
local isCuffed = false

AddEventHandler('gtacnr:police:gotCuffed', function() 
    isCuffed = true
  end)
  
AddEventHandler('gtacnr:police:gotUncuffed', function() 
    isCuffed = false
end)

local function startPointing()
    local ped = GetPlayerPed(-1)
    RequestAnimDict("anim@mp_point")
    while not HasAnimDictLoaded("anim@mp_point") do
        Wait(0)
    end
    SetPedCurrentWeaponVisible(ped, 0, 1, 1, 1)
    SetPedConfigFlag(ped, 36, 1)
    Citizen.InvokeNative(0x2D537BA194896636, ped, "task_mp_pointing", 0.5, 0, "anim@mp_point", 24)
    RemoveAnimDict("anim@mp_point")
end

local function stopPointing()
    local ped = GetPlayerPed(-1)
    Citizen.InvokeNative(0xD01015C7316AE176, ped, "Stop")
    if not IsPedInjured(ped) then
        ClearPedSecondaryTask(ped)
    end
    if not IsPedInAnyVehicle(ped, 1) then
        SetPedCurrentWeaponVisible(ped, 1, 1, 1, 1)
    end
    SetPedConfigFlag(ped, 36, 0)
    ClearPedSecondaryTask(PlayerPedId())
end

exports('startPointing', function()
    startPointing()
    mp_pointing = true
end)

exports('stopPointing', function()
    stopPointing()
    mp_pointing = false
end)

local function canPedPoint()
    return 
        IsPedOnFoot(PlayerPedId()) and 
        not IsPedSwimming(PlayerPedId()) and
        not IsPedSwimmingUnderWater(PlayerPedId()) and
        not IsPedDiving(PlayerPedId()) and
        not IsPedFalling(PlayerPedId()) and
        not IsPedInParachuteFreeFall(PlayerPedId()) and
        not IsPlayerSwitchInProgress() and
        IsScreenFadedIn() and
        not isCuffed
end

local function gtacnrUpdatePointingState()
    exports.gtacnr:SetPointingState(false)
end

local oldval = false
local oldvalped = false

Citizen.CreateThread(function()
    while true do
        Wait(0)

        --if not keyPressed then
        --    if IsControlPressed(0, 29) and not mp_pointing and canPedPoint(PlayerPedId()) then
        --        Wait(200)
        --        if not IsControlPressed(0, 29) then
        --            keyPressed = true
        --            startPointing()
        --            mp_pointing = true
        --        else
        --            keyPressed = true
        --            while IsControlPressed(0, 29) do
        --                Wait(50)
        --            end
        --        end
        --    elseif (IsControlPressed(0, 29) and mp_pointing) or (not canPedPoint(PlayerPedId()) and mp_pointing) then
        --        keyPressed = true
        --        mp_pointing = false
        --        stopPointing()
        --    end
        --end
        --
        --if keyPressed then
        --    if not IsControlPressed(0, 29) then
        --        keyPressed = false
        --    end
        --end

        local unk, curWeap = GetCurrentPedWeapon(PlayerPedId(), true)
        if curWeap ~= GetHashKey("WEAPON_UNARMED") and mp_pointing then
            stopPointing()
            gtacnrUpdatePointingState()
        end

        if Citizen.InvokeNative(0x921CE12C489C4C41, PlayerPedId()) and not mp_pointing then
            stopPointing()
            gtacnrUpdatePointingState()
        end
        
        if Citizen.InvokeNative(0x921CE12C489C4C41, PlayerPedId()) then
            if not canPedPoint(PlayerPedId()) then
                stopPointing()
                gtacnrUpdatePointingState()
            else
                local ped = GetPlayerPed(-1)
                local camPitch = GetGameplayCamRelativePitch()
                if camPitch < -70.0 then
                    camPitch = -70.0
                elseif camPitch > 42.0 then
                    camPitch = 42.0
                end
                camPitch = (camPitch + 70.0) / 112.0

                local camHeading = GetGameplayCamRelativeHeading()
                local cosCamHeading = Cos(camHeading)
                local sinCamHeading = Sin(camHeading)
                if camHeading < -180.0 then
                    camHeading = -180.0
                elseif camHeading > 180.0 then
                    camHeading = 180.0
                end
                camHeading = (camHeading + 180.0) / 360.0

                local blocked = 0
                local nn = 0

                local coords = GetOffsetFromEntityInWorldCoords(ped, (cosCamHeading * -0.2) - (sinCamHeading * (0.4 * camHeading + 0.3)), (sinCamHeading * -0.2) + (cosCamHeading * (0.4 * camHeading + 0.3)), 0.6)
                local ray = Cast_3dRayPointToPoint(coords.x, coords.y, coords.z - 0.2, coords.x, coords.y, coords.z + 0.2, 0.4, 95, ped, 7);
                nn,blocked,coords,coords = GetRaycastResult(ray)

                Citizen.InvokeNative(0xD5BB4025AE449A4E, ped, "Pitch", camPitch)
                Citizen.InvokeNative(0xD5BB4025AE449A4E, ped, "Heading", camHeading * -1.0 + 1.0)
                Citizen.InvokeNative(0xB0A6CFD2C69C1088, ped, "isBlocked", blocked)
                Citizen.InvokeNative(0xB0A6CFD2C69C1088, ped, "isFirstPerson", Citizen.InvokeNative(0xEE778F8C7E1142E2, Citizen.InvokeNative(0x19CAFA3C87F7C2FF)) == 4)

            end
        end
    end
end)