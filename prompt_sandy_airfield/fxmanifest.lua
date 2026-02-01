fx_version 'bodacious'
game 'gta5'
this_is_a_map 'yes'
author 'Prompt Mods'
version "1.0.0"

data_file 'AUDIO_GAMEDATA' 'audio/garage_door_sound_game.dat' -- dat151
data_file 'AUDIO_GAMEDATA' 'audio/sdair_maintenance_game.dat' -- dat151
data_file 'AUDIO_GAMEDATA' 'audio/prompt_sdair_terminal_game.dat' -- dat151

files {
  'audio/garage_door_sound_game.dat151.rel',
  'audio/sdair_maintenance_game.dat151.rel',
  'audio/prompt_sdair_terminal_game.dat151.rel',
}

escrow_ignore {
  'stream/unlocked/**'
}


-- scripts --
lua54 'yes'


server_scripts{
	'sv_Tokens.lua',
	'sv_MapChainHandler.lua',
	'sv_MapVersionCheck.lua'
}
dependency '/assetpacks'