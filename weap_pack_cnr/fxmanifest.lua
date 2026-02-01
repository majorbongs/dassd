fx_version 'cerulean'
game 'gta5'

files {
  'data/**/*.meta'
}

data_file 'WEAPONCOMPONENTSINFO_FILE' 'data/**/weaponcomponents.meta'
data_file 'WEAPON_ANIMATIONS_FILE' 'data/**/weaponanimations.meta'
data_file 'WEAPON_METADATA_FILE' 'data/**/weaponarchetypes.meta'
data_file 'PED_PERSONALITY_FILE' 'data/**/pedpersonality.meta'
data_file 'WEAPONINFO_FILE' 'data/**/weapons.meta'
data_file 'LOADOUTS_FILE' 'data/**/loadouts.meta'
data_file 'EXPLOSION_INFO_FILE' 'data/**/explosion.meta'

client_script 'weapon_names.lua'
