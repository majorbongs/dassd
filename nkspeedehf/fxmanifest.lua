fx_version 'cerulean'
game 'gta5'

data_file 'VEHICLE_LAYOUTS_FILE' 'vehiclelayouts.meta'
data_file 'HANDLING_FILE' 'handling.meta'
data_file 'VEHICLE_METADATA_FILE' 'vehicles.meta'
data_file 'CARCOLS_FILE' 'carcols.meta'
data_file 'VEHICLE_VARIATION_FILE' 'carvariations.meta'

files {
  '*.meta',
}

escrow_ignore {
  "stream/*.ytd",
  "*.meta"
}

lua54 'yes'
dependency '/assetpacks'