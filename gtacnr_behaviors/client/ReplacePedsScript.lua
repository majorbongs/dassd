-- 
-- Grand Theft Auto: Cops n' Robbers
-- Copyright (c) 2020-2021 Sasinosoft Games
-- All Rights Reserved
-- 

local models = {
    GetHashKey('g_m_m_mexboss_01'),
    GetHashKey('g_m_m_mexboss_02'),
    GetHashKey('a_m_m_ktown_01')
}

CreateThread(function()
    RequestModel('a_m_m_soucent_03')
end)

AddEventHandler('populationPedCreating', function(x, y, z, model, setters)
    if table.contains(models, model) then 
        setters.setModel('a_m_m_soucent_03')
    end
end)
