fx_version 'bodacious'
game 'gta5'

description 'GTA CnR UI'
version '0.3'
author 'Sasino, Piterson'
copyright 'Sasinosoft Games, Strazzullo Software LLC'

-- C#
client_script 'bin/client/*.dll'

-- Lua
client_script 'lua/client/*.lua'

-- HTML5
ui_page 'nui/index.html'

files {
    'bin/client/*.dll',
    'nui/css/**/*.*',
    'nui/data/**/*.*',
    'nui/fonts/**/*.*',
    'nui/img/**/*.*',
    'nui/js/dist/**/*.js',
    'nui/sounds/**/*.*',
    'nui/index.html'
}
