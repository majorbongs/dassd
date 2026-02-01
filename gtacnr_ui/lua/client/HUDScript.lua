-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2022 Sasinosoft Games
-- All Rights Reserved
-- 
-- Author: Sasino

local margin = 0.0365
local fadeSpeed = 40

local xBase = 0.0
local yBase = 0.5

local externalXOffset = 0.0
local externalYOffset = 0.0

local drawHud = true

AddEventHandler("gtacnr:hud:offsetX", function(value)
    externalXOffset = value
end)

AddEventHandler("gtacnr:hud:offsetY", function(value)
    externalYOffset = value
end)

AddEventHandler("gtacnr:hud:toggle", function(value)
    drawHud = value
end)

----------------------------------------------------------------------
----------------------------- Draw Loop ------------------------------
----------------------------------------------------------------------
Citizen.CreateThread(function() 
    while true do 
        Citizen.Wait(0)

        if not IsHudComponentActive(20) and drawHud then
            local xOffset = 0.0
            local yOffset = 0.0
            
            SetScriptGfxAlign(82, 84)
            SetScriptGfxAlignParams(0, 0, 0, 0)
            
            --
            if externalXOffset > 0.0 then
                xOffset = externalXOffset
            end
            
            if externalYOffset > 0.0 then
                yOffset = externalYOffset
            end

            --
            xOffset, yOffset = DrawWantedLevel(xOffset, yOffset)
            xOffset, yOffset = DrawBounty(xOffset, yOffset)
            xOffset, yOffset = DrawHitContract(xOffset, yOffset)
            xOffset, yOffset = DrawTime(xOffset, yOffset)
            xOffset, yOffset = DrawAdminDuty(xOffset, yOffset)
            xOffset, yOffset = DrawAdminGhost(xOffset, yOffset)
            xOffset, yOffset = DrawAdminUndercover(xOffset, yOffset)
            xOffset, yOffset = DrawAdminFakeName(xOffset, yOffset)
            xOffset, yOffset = DrawJob(xOffset, yOffset)
            xOffset, yOffset = DrawCash(xOffset, yOffset)
            xOffset, yOffset = DrawCashChange(xOffset, yOffset)
            xOffset, yOffset = DrawBank(xOffset, yOffset)
            xOffset, yOffset = DrawBankChange(xOffset, yOffset)
            xOffset, yOffset = DrawDebt(xOffset, yOffset)
            xOffset, yOffset = DrawDebtChange(xOffset, yOffset)
            xOffset, yOffset = DrawXPChange(xOffset, yOffset)
            xOffset, yOffset = DrawWeapon(xOffset, yOffset)
            xOffset, yOffset = DrawAmmo(xOffset, yOffset)
            
            ResetScriptGfxAlign()
        end
    end
end)

Citizen.CreateThread(function() 
    while true do 
        Citizen.Wait(1000)
        FlashWantedDisplay(true)
    end
end)

----------------------------------------------------------------------
---------------------------- Wanted Level ----------------------------
----------------------------------------------------------------------
local dictStars = "gtacnr_stars"
local dictWeaps = "gtacnr_weapons"
local starSprites =
{
    { "star_white", "star_transparent", "star_transparent", "star_transparent", "star_transparent" },
    { "star_white", "star_white", "star_transparent", "star_transparent", "star_transparent" },
    { "star_white", "star_white", "star_white", "star_transparent", "star_transparent" },
    { "star_white", "star_white", "star_white", "star_white", "star_transparent" },
    { "star_red", "star_red", "star_red", "star_red", "star_red" }
}

--
local drawWantedLevel = false
local wantedLevel = 0
local isFlashing = false
local flashState = false

--
function DrawWantedLevel(xOffset, yOffset)
    --
    HideHudComponentThisFrame(1)

    if drawWantedLevel and wantedLevel > 0 and wantedLevel <= 5 then 
        if not HasStreamedTextureDictLoaded(dictStars) then
            RequestStreamedTextureDict(dictStars, true)
            while not HasStreamedTextureDictLoaded(dictStars) do
                Citizen.Wait(10)
            end
        end

        local spritesToDraw = table.clone(starSprites[wantedLevel])
        if isFlashing and not flashState then 
            spritesToDraw[wantedLevel] = "star_transparent"
        end

        local i = 0
        while i < 5 do
            local starXOffset = i * -0.0175
            DrawSprite(
                dictStars,
                spritesToDraw[i + 1],
                starXOffset + xOffset + 0.01,
                yOffset + 0.015,
                0.0175,
                0.030,
                0.0,
                255,
                255,
                255,
                255
            )
            i = i + 1
        end
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleWantedLevel")
AddEventHandler("gtacnr:hud:toggleWantedLevel", function(toggle)
    drawWantedLevel = toggle
end)

--
RegisterNetEvent("gtacnr:hud:setWantedLevel")
AddEventHandler("gtacnr:hud:setWantedLevel", function(wl)
    wantedLevel = wl
end)

--
RegisterNetEvent("gtacnr:hud:setIsFlashing")
AddEventHandler("gtacnr:hud:setIsFlashing", function(toggle)
    isFlashing = toggle
end)

--
RegisterNetEvent("gtacnr:hud:setFlashingSpeed")
AddEventHandler("gtacnr:hud:setFlashingSpeed", function(speed)
    flashingSpeed = speed
end)

--
Citizen.CreateThread(function()
    while true do
        if (not flashingSpeed) or (flashingSpeed < 0.5) then 
            flashingSpeed = 0.5
        end
        
        Citizen.Wait(500 / flashingSpeed)
        flashState = not flashState
    end
end)

----------------------------------------------------------------------
-------------------------- Weekday and Time --------------------------
----------------------------------------------------------------------
local weekdayNames = 
{
    "sun",
    "mon",
    "tue",
    "wed",
    "thu",
    "fri",
    "sat"
}

--
local drawTime = false
local hour = 0
local minute = 0
local weekday = 0
local timeAlpha = 255

--
function DrawTime(xOffset, yOffset)
    if drawTime then
        local str = (weekdayNames[weekday + 1] or "err") .. " " 
            .. string.format("%02d", hour) .. ":" 
            .. string.format("%02d", minute)

        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            228, 
            228, 
            228, 
            timeAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleTime")
AddEventHandler("gtacnr:hud:toggleTime", function(toggle)
    if toggle then 
        drawTime = true
        timeAlpha = 0
        while timeAlpha < 255 do 
            Citizen.Wait(0)
            timeAlpha = timeAlpha + fadeSpeed
            if timeAlpha > 255 then timeAlpha = 255 end
        end
    else
        timeAlpha = 255
        while timeAlpha > 0 do 
            Citizen.Wait(0)
            timeAlpha = timeAlpha - fadeSpeed
            if timeAlpha < 0 then timeAlpha = 0 end
        end
        drawTime = false
    end
end)

--
RegisterNetEvent("gtacnr:hud:setTime")
AddEventHandler("gtacnr:hud:setTime", function(h, m, w)
    hour = h or 0
    minute = m or 0
    weekday = w or 0
end)

----------------------------------------------------------------------
------------------------------- Bounty -------------------------------
----------------------------------------------------------------------
local drawBounty = false
local bounty = 0
local bountyAlpha = 255

function DrawBounty(xOffset, yOffset)
    if bounty <= 0 then 
        drawBounty = false
    end

    if drawBounty then
        local val = FormatCurrency(bounty)
        local str = "bounty $" .. val
        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            200, 
            60, 
            60, 
            bountyAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleBounty")
AddEventHandler("gtacnr:hud:toggleBounty", function(toggle)
    if toggle then 
        drawBounty = true
        bountyAlpha = 0
        while bountyAlpha < 255 do 
            Citizen.Wait(0)
            bountyAlpha = bountyAlpha + fadeSpeed
            if bountyAlpha > 255 then bountyAlpha = 255 end
        end
    else
        bountyAlpha = 255
        while bountyAlpha > 0 do 
            Citizen.Wait(0)
            bountyAlpha = bountyAlpha - fadeSpeed
            if bountyAlpha < 0 then bountyAlpha = 0 end
        end
        drawBounty = false
    end
end)

--
RegisterNetEvent("gtacnr:hud:setBounty")
AddEventHandler("gtacnr:hud:setBounty", function(amount)
    bounty = amount
end)

----------------------------------------------------------------------
---------------------------- Hit Contract ----------------------------
----------------------------------------------------------------------
local drawHitContract = false
local hitContract = 0
local hitContractAlpha = 255

function DrawHitContract(xOffset, yOffset)
    if drawHitContract then
        local val = FormatCurrency(hitContract)
        local str = "hit $" .. val
        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            200, 
            60, 
            60, 
            hitContractAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end


---@param toggle boolean
function ToggleHitContract(toggle)
    if toggle then 
        drawHitContract = true
        hitContractAlpha = 0
        while hitContractAlpha < 255 do
            Citizen.Wait(0)
            hitContractAlpha = hitContractAlpha + fadeSpeed
            if hitContractAlpha > 255 then hitContractAlpha = 255 end
        end
    else
        hitContractAlpha = 255
        while hitContractAlpha > 0 do
            Citizen.Wait(0)
            hitContractAlpha = hitContractAlpha - fadeSpeed
            if hitContractAlpha < 0 then hitContractAlpha = 0 end
        end
        drawHitContract = false
    end
end

--
RegisterNetEvent("gtacnr:hud:setHitContract")
AddEventHandler("gtacnr:hud:setHitContract", function(amount)

    if hitContract == 0 and amount > 0 then
        ToggleHitContract(true)
    end

    if hitContract > 0 and amount == 0 then
        ToggleHitContract(false)
    end

    hitContract = amount
end)

----------------------------------------------------------------------
------------------------- Thousands Separator ------------------------
----------------------------------------------------------------------
local thousandsSeparator = false

RegisterNetEvent("gtacnr:hud:toggleThousandsSeparator")
AddEventHandler("gtacnr:hud:toggleThousandsSeparator", function(toggle)
    thousandsSeparator = toggle
end)

----------------------------------------------------------------------
-------------------------------- Cash --------------------------------
----------------------------------------------------------------------
local drawCash = false
local cash = 0
local cashAlpha = 255

function DrawCash(xOffset, yOffset)
    HideHudComponentThisFrame(3)
    HideHudComponentThisFrame(4)
    HideHudComponentThisFrame(13)

    if drawCash then
        local val = FormatCurrency(math.abs(cash))
        local str = "$" .. val
        if cash < 0 then 
            str = "-$" .. val
        end

        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            114, 
            204, 
            114, 
            cashAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleCash")
AddEventHandler("gtacnr:hud:toggleCash", function(toggle, noFadeOut)
    if toggle then 
        drawCash = true
        cashAlpha = 0
        while cashAlpha < 255 do 
            Citizen.Wait(0)
            cashAlpha = cashAlpha + fadeSpeed
            if cashAlpha > 255 then cashAlpha = 255 end
        end
    else
        if not noFadeOut then 
            cashAlpha = 255
            while cashAlpha > 0 do 
                Citizen.Wait(0)
                cashAlpha = cashAlpha - fadeSpeed
                if cashAlpha < 0 then cashAlpha = 0 end
            end
        end
        drawCash = false
    end
end)

--
RegisterNetEvent("gtacnr:hud:setCash")
AddEventHandler("gtacnr:hud:setCash", function(amount)
    cash = amount
end)

----------------------------------------------------------------------
---------------------------- Cash Change -----------------------------
----------------------------------------------------------------------
local drawCashChange = false
local cashChange = 0
local cashChangeAlpha = 255

function DrawCashChange(xOffset, yOffset)
    if drawCashChange then
        local val = FormatCurrency(math.abs(cashChange))
        local col = { r = 228, g = 228, b = 228 }
        local str = "+$" .. val
        if cashChange < 0 then 
            str = "-$" .. val
            col = { r = 194, g = 80, b = 80 }
        end

        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            col.r, 
            col.g, 
            col.b, 
            cashChangeAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleCashChange")
AddEventHandler("gtacnr:hud:toggleCashChange", function(toggle, noFadeOut)
    if toggle then 
        drawCashChange = true
        cashChangeAlpha = 0
        while cashChangeAlpha < 255 do 
            Citizen.Wait(0)
            cashChangeAlpha = cashChangeAlpha + fadeSpeed
            if cashChangeAlpha > 255 then cashChangeAlpha = 255 end
        end
    else
        if not noFadeOut then 
            cashChangeAlpha = 255
            while cashChangeAlpha > 0 do 
                Citizen.Wait(0)
                cashChangeAlpha = cashChangeAlpha - fadeSpeed
                if cashChangeAlpha < 0 then cashChangeAlpha = 0 end
            end
        end
        drawCashChange = false
    end
end)

--
RegisterNetEvent("gtacnr:hud:setCashChange")
AddEventHandler("gtacnr:hud:setCashChange", function(amount)
    cashChange = amount
end)

----------------------------------------------------------------------
------------------------------- Bank ---------------------------------
----------------------------------------------------------------------
local drawBank = false
local bank = 0
local bankAlpha = 255

function DrawBank(xOffset, yOffset)
    if drawBank then
        local val = FormatCurrency(math.abs(bank))
        local str = "$" .. val
        if bank < 0 then 
            str = "-$" .. val
        end

        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            185, 
            230, 
            185, 
            bankAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleBank")
AddEventHandler("gtacnr:hud:toggleBank", function(toggle, noFadeOut)
    if toggle then 
        drawBank = true
        bankAlpha = 0
        while bankAlpha < 255 do 
            Citizen.Wait(0)
            bankAlpha = bankAlpha + fadeSpeed
            if bankAlpha > 255 then bankAlpha = 255 end
        end
    else
        if not noFadeOut then 
            bankAlpha = 255
            while bankAlpha > 0 do 
                Citizen.Wait(0)
                bankAlpha = bankAlpha - fadeSpeed
                if bankAlpha < 0 then bankAlpha = 0 end
            end
        end
        drawBank = false
    end
end)

--
RegisterNetEvent("gtacnr:hud:setBank")
AddEventHandler("gtacnr:hud:setBank", function(amount)
    bank = amount
end)

----------------------------------------------------------------------
---------------------------- Bank Change -----------------------------
----------------------------------------------------------------------
local drawBankChange = false
local bankChange = 0
local bankChangeAlpha = 255

function DrawBankChange(xOffset, yOffset)
    if drawBankChange then
        local val = FormatCurrency(math.abs(bankChange))
        local str = "+$" .. val
        local col = { r = 228, g = 228, b = 228 }
        if bankChange < 0 then 
            str = "-$" .. val
            col = { r = 194, g = 80, b = 80 }
        end

        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            col.r, 
            col.g, 
            col.b, 
            bankChangeAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleBankChange")
AddEventHandler("gtacnr:hud:toggleBankChange", function(toggle, noFadeOut)
    if toggle then 
        drawBankChange = true
        bankChangeAlpha = 0
        while bankChangeAlpha < 255 do 
            Citizen.Wait(0)
            bankChangeAlpha = bankChangeAlpha + fadeSpeed
            if bankChangeAlpha > 255 then bankChangeAlpha = 255 end
        end
    else
        if not noFadeOut then 
            bankChangeAlpha = 255
            while bankChangeAlpha > 0 do 
                Citizen.Wait(0)
                bankChangeAlpha = bankChangeAlpha - fadeSpeed
                if bankChangeAlpha < 0 then bankChangeAlpha = 0 end
            end
        end
        drawBankChange = false
    end
end)

--
RegisterNetEvent("gtacnr:hud:setBankChange")
AddEventHandler("gtacnr:hud:setBankChange", function(amount)
    bankChange = amount
end)

----------------------------------------------------------------------
------------------------------- Debt ---------------------------------
----------------------------------------------------------------------
local drawDebt = false
local debt = 0
local debtAlpha = 255
local debtPrefix = ""

function DrawDebt(xOffset, yOffset)
    if drawDebt then
        local val = FormatCurrency(math.abs(debt))
        local str = debtPrefix .. "debt $" .. val
        if debt < 0 then
            str = "debt -$" .. val
        end
        
        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            200, 
            50, 
            50, 
            debtAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleDebt")
AddEventHandler("gtacnr:hud:toggleDebt", function(toggle)
    if toggle then 
        drawDebt = true
        debtAlpha = 0
        while debtAlpha < 255 do 
            Citizen.Wait(0)
            debtAlpha = debtAlpha + fadeSpeed
            if debtAlpha > 255 then debtAlpha = 255 end
        end
    else
        debtAlpha = 255
        while debtAlpha > 0 do 
            Citizen.Wait(0)
            debtAlpha = debtAlpha - fadeSpeed
            if debtAlpha < 0 then debtAlpha = 0 end
        end
        drawDebt = false
    end
end)

--
RegisterNetEvent("gtacnr:hud:setDebt")
AddEventHandler("gtacnr:hud:setDebt", function(amount)
    debt = amount
end)

--
RegisterNetEvent("gtacnr:hud:setDebtPrefix")
AddEventHandler("gtacnr:hud:setDebtPrefix", function(prefix)
    debtPrefix = prefix .. " "
end)

----------------------------------------------------------------------
---------------------------- Debt Change -----------------------------
----------------------------------------------------------------------
local drawDebtChange = false
local debtChange = 0
local debtChangeAlpha = 255

function DrawDebtChange(xOffset, yOffset)
    if drawDebtChange then
        local val = FormatCurrency(math.abs(debtChange))
        local str = "+$" .. val
        local col = { r = 194, g = 80, b = 80 }
        if debtChange < 0 then 
            str = "-$" .. val
            col = { r = 228, g = 228, b = 228 }
        end
        
        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            col.r, 
            col.g, 
            col.b, 
            debtChangeAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleDebtChange")
AddEventHandler("gtacnr:hud:toggleDebtChange", function(toggle)
    if toggle then 
        drawDebtChange = true
        debtChangeAlpha = 0
        while debtChangeAlpha < 255 do 
            Citizen.Wait(0)
            debtChangeAlpha = debtChangeAlpha + fadeSpeed
            if debtChangeAlpha > 255 then debtChangeAlpha = 255 end
        end
    else
        debtChangeAlpha = 255
        while debtChangeAlpha > 0 do 
            Citizen.Wait(0)
            debtChangeAlpha = debtChangeAlpha - fadeSpeed
            if debtChangeAlpha < 0 then debtChangeAlpha = 0 end
        end
        drawDebtChange = false
    end
end)

--
RegisterNetEvent("gtacnr:hud:setDebtChange")
AddEventHandler("gtacnr:hud:setDebtChange", function(amount)
    debtChange = amount
end)

----------------------------------------------------------------------
---------------------------- XP Change -----------------------------
----------------------------------------------------------------------
local drawXPChange = false
local xpChange = 0
local xpChangeBonus = 0
local xpChangeAlpha = 255

function DrawXPChange(xOffset, yOffset)
    if drawXPChange then
        local str = "+" .. xpChange .. "XP"
        local col = { r = 60, g = 150, b = 255 }

        if xpChange < 0 then 
            str = "-" .. math.abs(xpChange) .. "XP"
            col = { r = 228, g = 228, b = 228 }
        end
        
        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            col.r, 
            col.g, 
            col.b, 
            xpChangeAlpha, 
            true
        )
        yOffset = yOffset + margin

        if xpChangeBonus > 0 then
            str = "+" .. xpChangeBonus .. "xp bonus"
            col = { r = 6, g = 181, b = 255 }
            
            DrawHudText(
                xBase + xOffset, 
                yBase + yOffset, 
                1.0, 
                1.0, 
                0.65, 
                str, 
                col.r, 
                col.g, 
                col.b, 
                xpChangeAlpha, 
                true
            )
            yOffset = yOffset + margin
        end
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleXPChange")
AddEventHandler("gtacnr:hud:toggleXPChange", function(toggle)
    if toggle then 
        drawXPChange = true
        xpChangeAlpha = 0
        while xpChangeAlpha < 255 do 
            Citizen.Wait(0)
            xpChangeAlpha = xpChangeAlpha + fadeSpeed
            if xpChangeAlpha > 255 then xpChangeAlpha = 255 end
        end
    else
        xpChangeAlpha = 255
        while xpChangeAlpha > 0 do 
            Citizen.Wait(0)
            xpChangeAlpha = xpChangeAlpha - fadeSpeed
            if xpChangeAlpha < 0 then xpChangeAlpha = 0 end
        end
        drawXPChange = false
    end
end)

--
RegisterNetEvent("gtacnr:hud:setXPChange")
AddEventHandler("gtacnr:hud:setXPChange", function(amount, bonus)
    xpChange = amount
    xpChangeBonus = bonus or 0
end)

----------------------------------------------------------------------
-------------------------------- Job ---------------------------------
----------------------------------------------------------------------
local drawJob = false
local job = ""
local jobColor = { r = 255, g = 255, b = 255 }
local jobAlpha = 255

function DrawJob(xOffset, yOffset)
    if drawJob then
        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            job, 
            jobColor.r,
            jobColor.g,
            jobColor.b,
            jobAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleJob")
AddEventHandler("gtacnr:hud:toggleJob", function(toggle)
    if toggle then 
        drawJob = true
        jobAlpha = 0
        while jobAlpha < 255 do 
            Citizen.Wait(0)
            jobAlpha = jobAlpha + fadeSpeed
            if jobAlpha > 255 then jobAlpha = 255 end
        end
    else
        jobAlpha = 255
        while jobAlpha > 0 do 
            Citizen.Wait(0)
            jobAlpha = jobAlpha - fadeSpeed
            if jobAlpha < 0 then jobAlpha = 0 end
        end
        drawJob = false
    end
end)

--
RegisterNetEvent("gtacnr:hud:setJob")
AddEventHandler("gtacnr:hud:setJob", function(j)
    job = j or ""
end)

--
RegisterNetEvent("gtacnr:hud:setJobColor")
AddEventHandler("gtacnr:hud:setJobColor", function(cr, cg, cb)
    jobColor = { r = cr, g = cg, b = cb }
end)

----------------------------------------------------------------------
------------------------------ Weapon --------------------------------
----------------------------------------------------------------------
local drawWeapon = false
local weapon = ""
local weaponAlpha = 255
local userWeaponScale = 1.0

function DrawWeapon(xOffset, yOffset)
    if drawWeapon then

        if not HasStreamedTextureDictLoaded(dictWeaps) then
            RequestStreamedTextureDict(dictWeaps, true)
            while not HasStreamedTextureDictLoaded(dictWeaps) do
                Citizen.Wait(1)
            end
        end

        local xRes, yRes = GetActiveScreenResolution()
        local ratio = GetAspectRatio(false)
        local imgRes = GetTextureResolution(dictWeaps, weapon)
        local scale = 0.0005 * userWeaponScale
        local scaleX = scale * 1920 / xRes
        local scaleY = scale * 1080 / yRes

        DrawSprite(
            dictWeaps,
            weapon,
            ((scaleX * imgRes.x) / 2), -- 0.98335 - ((scaleX * imgRes.x) / 2)
            0.035 + yOffset, --  + yOffset
            imgRes.x * scaleX,
            imgRes.y * scaleY * ratio,
            0.0, 
            255, 
            255, 
            255, 
            weaponAlpha
        )

        yOffset = yOffset + 0.04
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleWeapon")
AddEventHandler("gtacnr:hud:toggleWeapon", function(toggle)
    -- if toggle then 
    --     drawWeapon = true
    --     weaponAlpha = 0
    --     while weaponAlpha < 255 do 
    --         Citizen.Wait(0)
    --         weaponAlpha = weaponAlpha + fadeSpeed
    --         if weaponAlpha > 255 then weaponAlpha = 255 end
    --     end
    -- else
    --     weaponAlpha = 255
    --     while weaponAlpha > 0 do 
    --         Citizen.Wait(0)
    --         weaponAlpha = weaponAlpha - fadeSpeed
    --         if weaponAlpha < 0 then weaponAlpha = 0 end
    --     end
    --     drawWeapon = false
    -- end
end)

--
RegisterNetEvent("gtacnr:hud:setWeapon")
AddEventHandler("gtacnr:hud:setWeapon", function(hash)
    weapon = hash or ""
end)

----------------------------------------------------------------------
-------------------------------- Ammo --------------------------------
----------------------------------------------------------------------
local drawAmmo = false
local ammo = 0
local magazine = 0
local ammoAlpha = 255

function DrawAmmo(xOffset, yOffset)
    HideHudComponentThisFrame(2)
    if drawAmmo then
        local str = ammo .. " ~c~" .. magazine
        local yWeapOffset = 0.025
        if not drawWeapon then 
            yWeapOffset = 0.0
        end

        DrawHudText(
            xBase + xOffset, 
            yBase + yWeapOffset + yOffset, 
            1.0, 
            1.0, 
            0.50, 
            str, 
            228, 
            228, 
            228,
            ammoAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleAmmo")
AddEventHandler("gtacnr:hud:toggleAmmo", function(toggle, noFadeOut)
    if toggle then 
        drawAmmo = true
        ammoAlpha = 0
        while ammoAlpha < 255 do 
            Citizen.Wait(0)
            ammoAlpha = ammoAlpha + fadeSpeed
            if ammoAlpha > 255 then ammoAlpha = 255 end
        end
    else
        if not noFadeOut then 
            ammoAlpha = 255
            while ammoAlpha > 0 do 
                Citizen.Wait(0)
                ammoAlpha = ammoAlpha - fadeSpeed
                if ammoAlpha < 0 then ammoAlpha = 0 end
            end
        end
        drawAmmo = false
    end
end)

--
RegisterNetEvent("gtacnr:hud:setAmmo")
AddEventHandler("gtacnr:hud:setAmmo", function(amount)
    ammo = amount
end)

--
RegisterNetEvent("gtacnr:hud:setMagazine")
AddEventHandler("gtacnr:hud:setMagazine", function(amount)
    magazine = amount
end)

----------------------------------------------------------------------
----------------------------- Admin Duty -----------------------------
----------------------------------------------------------------------
local drawAdminDuty = false
local adminDutyAlpha = 255

function DrawAdminDuty(xOffset, yOffset)
    if drawAdminDuty then
        local str = "on duty"
        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            50, 
            188, 
            92, 
            adminDutyAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleAdminDuty")
AddEventHandler("gtacnr:hud:toggleAdminDuty", function(toggle)
    if toggle then 
        drawAdminDuty = true
        adminDutyAlpha = 0
        while adminDutyAlpha < 255 do 
            Citizen.Wait(0)
            adminDutyAlpha = adminDutyAlpha + fadeSpeed
            if adminDutyAlpha > 255 then adminDutyAlpha = 255 end
        end
    else
        adminDutyAlpha = 255
        while adminDutyAlpha > 0 do 
            Citizen.Wait(0)
            adminDutyAlpha = adminDutyAlpha - fadeSpeed
            if adminDutyAlpha < 0 then adminDutyAlpha = 0 end
        end
        drawAdminDuty = false
    end
end)

----------------------------------------------------------------------
----------------------------- Admin Ghost -----------------------------
----------------------------------------------------------------------
local drawAdminGhost = false
local adminGhostAlpha = 255

function DrawAdminGhost(xOffset, yOffset)
    if drawAdminGhost then
        local str = "ghost mode"
        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            50, 
            188, 
            92, 
            adminGhostAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleAdminGhost")
AddEventHandler("gtacnr:hud:toggleAdminGhost", function(toggle)
    if toggle then 
        drawAdminGhost = true
        adminGhostAlpha = 0
        while adminGhostAlpha < 255 do 
            Citizen.Wait(0)
            adminGhostAlpha = adminGhostAlpha + fadeSpeed
            if adminGhostAlpha > 255 then adminGhostAlpha = 255 end
        end
    else
        adminGhostAlpha = 255
        while adminGhostAlpha > 0 do 
            Citizen.Wait(0)
            adminGhostAlpha = adminGhostAlpha - fadeSpeed
            if adminGhostAlpha < 0 then adminGhostAlpha = 0 end
        end
        drawAdminGhost = false
    end
end)

----------------------------------------------------------------------
----------------------------- Admin Undercover -----------------------
----------------------------------------------------------------------
local drawAdminUndercover = false
local adminUndercoverAlpha = 255

function DrawAdminUndercover(xOffset, yOffset)
    if drawAdminUndercover then
        local str = "undercover mode"
        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            50, 
            188, 
            92, 
            adminUndercoverAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleAdminUndercover")
AddEventHandler("gtacnr:hud:toggleAdminUndercover", function(toggle)
    if toggle then 
        drawAdminUndercover = true
        adminUndercoverAlpha = 0
        while adminUndercoverAlpha < 255 do 
            Citizen.Wait(0)
            adminUndercoverAlpha = adminUndercoverAlpha + fadeSpeed
            if adminUndercoverAlpha > 255 then adminUndercoverAlpha = 255 end
        end
    else
        adminUndercoverAlpha = 255
        while adminUndercoverAlpha > 0 do 
            Citizen.Wait(0)
            adminUndercoverAlpha = adminUndercoverAlpha - fadeSpeed
            if adminUndercoverAlpha < 0 then adminUndercoverAlpha = 0 end
        end
        drawAdminUndercover = false
    end
end)

----------------------------------------------------------------------
--------------------------- Admin Fake Name --------------------------
----------------------------------------------------------------------
local drawAdminFakeName = false
local adminFakeName = "-"
local adminFakeNameAlpha = 255

function DrawAdminFakeName(xOffset, yOffset)
    if drawAdminFakeName then
        local str = adminFakeName
        DrawHudText(
            xBase + xOffset, 
            yBase + yOffset, 
            1.0, 
            1.0, 
            0.65, 
            str, 
            50, 
            188, 
            92, 
            adminFakeNameAlpha, 
            true
        )
        yOffset = yOffset + margin
    end
    return xOffset, yOffset
end

--
RegisterNetEvent("gtacnr:hud:toggleAdminFakeName")
AddEventHandler("gtacnr:hud:toggleAdminFakeName", function(toggle)
    if toggle then 
        drawAdminFakeName = true
        adminFakeNameAlpha = 0
        while adminFakeNameAlpha < 255 do 
            Citizen.Wait(0)
            adminFakeNameAlpha = adminFakeNameAlpha + fadeSpeed
            if adminFakeNameAlpha > 255 then adminFakeNameAlpha = 255 end
        end
    else
        adminFakeNameAlpha = 255
        while adminFakeNameAlpha > 0 do 
            Citizen.Wait(0)
            adminFakeNameAlpha = adminFakeNameAlpha - fadeSpeed
            if adminFakeNameAlpha < 0 then adminFakeNameAlpha = 0 end
        end
        drawAdminFakeName = false
    end
end)

--
RegisterNetEvent("gtacnr:hud:setAdminFakeName")
AddEventHandler("gtacnr:hud:setAdminFakeName", function(fakeName)
    adminFakeName = fakeName
end)

----------------------------------------------------------------------
-------------------------------- Utils -------------------------------
----------------------------------------------------------------------
function table.clone(tbl)
    return {table.unpack(tbl)}
end

--
function DrawHudText(x, y, width, height, scale, text, r, g, b, a, outline)
    SetTextFont(7)
    SetTextProportional(0)
    SetTextScale(scale, scale)
    SetTextColour(r, g, b, a)
    SetTextDropshadow(0.1, 0, 0, 0, 255)
    SetTextDropShadow()
    SetTextJustification(2)
    SetTextWrap(
        xBase + externalXOffset, 
        0.98585
    )
    
    if outline then
	    SetTextOutline()
    end
    
    SetTextEntry("STRING")
    AddTextComponentString(text)
    DrawText(x - width/2, y - height/2)
end

function FormatCurrency(value)
    if thousandsSeparator then
        local result = string.gsub(value, "(%d)(%d%d%d)$", "%1,%2", 1)
        while true do
            result, found = string.gsub(result, "(%d)(%d%d%d),", "%1,%2,", 1)
            if found == 0 then break end
        end
        return result
    else
        return string.format("%i", value)
    end
end
