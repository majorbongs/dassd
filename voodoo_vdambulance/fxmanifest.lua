fx_version 'adamant'

games {'gta5'}

lua54 'yes'
 
files {
	'data/**/*.meta',
}

escrow_ignore {
  'data/**/*.meta',
  'stream/*.ytd',    
  'stream/**/*.ytd'  

}

data_file 'CARCOLS_FILE' 'data/**/carcols.meta'
data_file 'HANDLING_FILE' 'data/**/handling.meta'
data_file 'VEHICLE_METADATA_FILE' 'data/**/vehicles.meta'
data_file 'VEHICLE_VARIATION_FILE' 'data/**/carvariations.meta'



 

dependency '/assetpacks'