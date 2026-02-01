fx_version 'cerulean'
game 'gta5'

description 'GTA CnR Audio Player'
version '0.3'
author 'Sasino, Piterson'
copyright 'Sasinosoft Games, Strazzullo Software LLC'

files {
    'data/*.*',
    'gtacnr_audio/*.awc',
    'nui/**/*.*',
    'src/**/*.lua'
}

data_file 'AUDIO_SOUNDDATA' 'data/gtacnr_diseases.dat'
data_file 'AUDIO_WAVEPACK' 'gtacnr_audio'

client_script 'src/program.lua'

ui_page 'nui/index.html'
