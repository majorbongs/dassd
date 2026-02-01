fx_version 'cerulean'
game 'gta5'
author 'Gabz'
description 'GABZ Coil LOD4'
version '1.0'
lua54 'yes'

data_file 'HANDLING_FILE' 'handling.meta'
data_file 'VEHICLE_METADATA_FILE' 'vehicles.meta'
data_file 'CARCOLS_FILE' 'carcols.meta'
data_file 'VEHICLE_VARIATION_FILE' 'carvariations.meta'

files {
  'handling.meta',
  'vehicles.meta',
  'carcols.meta',
  'carvariations.meta',
}

client_script 'vehicle_names.lua'

escrow_ignore {
  'stream/*.ytd',
  '*.meta',
  'vehicle_names.lua',
  'stream/liveries/*.yft'
}
dependency '/assetpacks'