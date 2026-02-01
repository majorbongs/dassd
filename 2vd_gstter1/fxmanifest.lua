fx_version 'cerulean'
games {'gta5'}

author 'GOM x VD Customs'
version '1.0.0'

data_file 'HANDLING_FILE' 'handling.meta'
data_file 'VEHICLE_METADATA_FILE' 'vehicles.meta'
data_file 'CARCOLS_FILE' 'carcols.meta'
data_file 'VEHICLE_VARIATION_FILE' 'carvariations.meta'
data_file 'AUDIO_GAMEDATA' 'audioconfig/subaruej20_game.dat'
data_file 'AUDIO_SOUNDDATA' 'audioconfig/subaruej20_sounds.dat'
data_file 'AUDIO_WAVEPACK' 'sfx/dlc_subaruej20'

files {
  'handling.meta',
  'vehicles.meta',
  'carcols.meta',
  'carvariations.meta',
  'audioconfig/subaruej20_game.dat151.rel',
  'audioconfig/subaruej20_sounds.dat54.rel',
  'sfx/dlc_subaruej20/subaruej20.awc',
  'sfx/dlc_subaruej20/subaruej20_npc.awc'
}

client_script 'tun_names.lua'
lua54 'yes'
dependency '/assetpacks'