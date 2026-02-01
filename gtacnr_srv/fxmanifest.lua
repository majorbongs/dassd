fx_version 'cerulean'
game 'gta5'

description 'GTA CnR Game Mode'
version '0.3'
author 'Sasino, Piterson'
copyright 'Sasinosoft Games, Strazzullo Software LLC'

server_script 'bin/server/*.net.dll'

server_scripts {
    'lua/server/utils.lua',
    'lua/server/weapon_hash.lua',
    'lua/server/*.lua'
}

lua54 'yes'
use_experimental_fxv2_oal 'yes'
mono_rt2 'Prerelease expiring 2025-12-31. See https://aka.cfx.re/mono-rt2-preview for info.'
