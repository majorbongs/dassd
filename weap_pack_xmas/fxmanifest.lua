fx_version 'cerulean' 
game 'gta5'

files {
  'data/**/*.meta'
}

data_file 'WEAPONINFO_FILE'             'data/**/weapons.meta'
data_file 'WEAPONCOMPONENTSINFO_FILE'   'data/**/weaponcomponents.meta'
data_file 'WEAPON_METADATA_FILE'        'data/**/weaponarchetypes.meta'
data_file 'WEAPON_SHOP_INFO'            'data/**/shop_weapon.meta'
data_file 'WEAPON_ANIMATIONS_FILE'      'data/**/weaponanimations.meta'
data_file 'CONTENT_UNLOCKING_META_FILE' 'data/**/contentunlocks.meta'
data_file 'LOADOUTS_FILE'               'data/**/loadouts.meta'
data_file 'PED_PERSONALITY_FILE'        'data/**/pedpersonality.meta'

client_script 'weapon_names.lua'
