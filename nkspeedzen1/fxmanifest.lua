fx_version 'cerulean'
game 'gta5'

data_file 'VEHICLE_LAYOUTS_FILE' 'vehiclelayouts.meta'
data_file 'HANDLING_FILE' 'handling.meta'
data_file 'VEHICLE_METADATA_FILE' 'vehicles.meta'
data_file 'CARCOLS_FILE' 'carcols.meta'
data_file 'VEHICLE_VARIATION_FILE' 'carvariations.meta'
data_file "AUDIO_SYNTHDATA" "audioconfig/lg152gomcgt_amp.dat"
data_file "AUDIO_GAMEDATA" "audioconfig/lg152gomcgt_game.dat"
data_file "AUDIO_SOUNDDATA" "audioconfig/lg152gomcgt_sounds.dat"
data_file "AUDIO_WAVEPACK" "sfx/dlc_lg152gomcgt"

files {
    "audioconfig/*.dat151.rel",
    "audioconfig/*.dat54.rel",
    "audioconfig/*.dat10.rel",
    "sfx/**/*.awc",
    '*.meta'
}

client_script 'vehicle_names.lua'
lua54 'yes'

escrow_ignore {
    "stream/*.ytd",
    "*.meta"
}
dependency '/assetpacks'