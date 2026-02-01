fx_version 'cerulean'
game 'gta5'

files {
  'data/**/*.meta',
}

data_file 'WEAPONCOMPONENTSINFO_FILE' 'data/components/*.meta'
data_file 'WEAPONINFO_FILE_PATCH' 'data/info/*.meta'
data_file 'WEAPON_ANIMATIONS_FILE' 'data/animations/*.meta'
data_file 'WEAPON_METADATA_FILE' 'data/archetypes/*.meta'

client_script 'weapon_names.lua'
