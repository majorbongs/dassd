-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2025 Sasinosoft Games
-- All Rights Reserved
-- 

-- Define the custom textures here (without extension, which must always be png anyway)
local customTextures = {
    ["Bench"] = {
        "bench_cnr_ads",
        "bench_cnr_discord",
        "bench_cnr_store"
    },
    ["Bus"] = {
        "bus_cnr_ads_1",
        "bus_cnr_ads_2",
        "bus_cnr_ads_3"
    }
}

---------------------------------------------------------------------
---------------------------------------------------------------------
-- Do not edit anything below unless you know what you're doing
---------------------------------------------------------------------
---------------------------------------------------------------------

local originalTextures = {
    { "Bench", "prop_bench_add", "prop_bench_add_02" },
    --{ "Bus", "prop_busstop_02", "prop_busstop_poster_01" },
    --{ "Bus", "prop_busstop_05", "prop_busstop_poster_02" },
    --{ "Bus", "prop_busstop_04", "prop_busstop_poster_03" }
}

local changeDelay = 5000
local currentIdx = {}
local runtimeTxds = {}

Citizen.CreateThread(function()
    while true do 

        for k, v in pairs(originalTextures) do 
            local type = v[1]
            local origTxd = v[2]
            local origTxn = v[3]
            local idx = currentIdx[k] or 1
            local fileName = customTextures[type][idx]

            ReplaceTexture(origTxd, origTxn, fileName)

            if idx + 1 > #customTextures[type] then 
                currentIdx[k] = 1
            else
                currentIdx[k] = idx + 1
            end
        end
        Citizen.Wait(changeDelay)
    end
end)

function ReplaceTexture(origTxd, origTxn, fileName)
    Citizen.CreateThread(function() 
        -- print('ReplaceTexture ' .. origTxd .. ' ' .. origTxn .. ' ' .. fileName)

        local replaceTxd = origTxd .. "_rt"
    
        while not HasStreamedTextureDictLoaded(origTxd)
        do Citizen.Wait(0) end
    
        if runtimeTxds[replaceTxd] == nil then
            runtimeTxds[replaceTxd] = CreateRuntimeTxd(replaceTxd)
            while not HasStreamedTextureDictLoaded(replaceTxd)
            do Citizen.Wait(0) end
        end
    
        RemoveReplaceTexture(origTxd, origTxn)
        CreateRuntimeTextureFromImage(runtimeTxds[replaceTxd], fileName, "img/" .. fileName .. ".png")
        AddReplaceTexture(origTxd, origTxn, replaceTxd, fileName)
    end)
end
