fx_version 'cerulean'
games {'gta5'}

author 'GOM'
description 'GOM | Modeling FXManifest Template'
version '3.0.0'

data_file 'VEHICLE_LAYOUTS_FILE' 'vehiclelayouts.meta'
data_file 'HANDLING_FILE' 'handling.meta'
data_file 'VEHICLE_METADATA_FILE' 'vehicles.meta'
data_file 'CARCOLS_FILE' 'carcols.meta'
data_file 'VEHICLE_VARIATION_FILE' 'carvariations.meta'
data_file "AUDIO_GAMEDATA" "audioconfig/lg211gomescaladev_game.dat"
data_file "AUDIO_SOUNDDATA" "audioconfig/lg211gomescaladev_sounds.dat"
data_file "AUDIO_WAVEPACK" "sfx/dlc_lg211gomescaladev"


files {
  'vehiclelayouts.meta',
  'handling.meta',
  'vehicles.meta',
  'carcols.meta',
  'carvariations.meta',
  "audioconfig/*.dat151.rel",
  "audioconfig/*.dat54.rel",
  "sfx/**/*.awc"
}

client_script 'vehicle_names.lua'
lua54 'yes'
dependency '/assetpacks'