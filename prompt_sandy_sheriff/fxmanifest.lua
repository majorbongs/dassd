fx_version 'bodacious'
game 'gta5'
this_is_a_map 'yes'
author 'Prompt Mods'
version "1.1.0"




data_file 'AUDIO_GAMEDATA' 'audio/prompt_sandy_sheriff_game.dat' -- dat151
data_file 'AUDIO_GAMEDATA' 'audio/prompt_sandy_sheriff_doors_game.dat' -- dat151
data_file 'GTXD_PARENTING_DATA' 'meta/gtxd.meta'

files {
  'audio/prompt_sandy_sheriff_game.dat151.rel',
  'audio/prompt_sandy_sheriff_doors_game.dat151.rel',
  'meta/gtxd.meta'
}

escrow_ignore {
    'stream/unlocked/**',
    'audio/prompt_sandy_sheriff_game.dat151.rel',
  	'audio/prompt_sandy_sheriff_doors_game.dat151.rel'
}

-- scripts --
lua54 'yes'


server_scripts{
	'sv_Tokens.lua',
	'sv_MapChainHandler.lua',
	'sv_MapVersionCheck.lua'
}
dependency '/assetpacks'