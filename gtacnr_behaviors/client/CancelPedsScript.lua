-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2025 Sasinosoft Games
-- All Rights Reserved
-- 

AddEventHandler('populationPedCreating', function(x, y, z, model, setters)
    -- MRPD
    if (403.468 < x and x < 494.4575) and (-1036.4436 < y and y < -961.3433) then
        CancelEvent()
    end
end)
