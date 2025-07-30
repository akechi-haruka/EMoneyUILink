APMCoreFixes / EMoneyUILink
2024-2025 Haruka
Licensed under GPLv3.

See also: https://github.com/akechi-haruka/APMv3MenuTranslation

--- APMCoreFixes ---

Adds some QoL features to the launcher.

* Disable checks for file names and .opt files
* Add a clock
* Show mouse cursor
* Send the server the list of games installed (so it can return the "allowed" status for all of them)
* Add support for an analog IO4 device
* Skip Japan warning
* Use root directory instead of App directory for launched games

--- APMHeadbananaLink ---

Allows setting of the audio channels that APMHeadbanana changes.

APMHeadbanana (Audio control replacement dll - control arbitary audio channels with the headphone slider in APM): https://github.com/akechi-haruka/apmHeadbanana

--- emoneyUIFixes ---

Adds some QoL features to eMoneyUI:

* Shrinks the hitbox for the buttons (useful in touch games)
* SegAPI integration
    - Add an "exit game" button
    
--- EXMoney ---

Integrates eMoneyUI into non-APM games via SegAPI.

This requires the game to run in windowed/borderless.

Basic Usage:

Have a game with SegAPI (minimum required features are credit inserts and card reading).
Create an app.json as it's used by APMv3.
In launch.bat, add following before the game is started:

EXMoney.exe -s <path_to_game>\App\segatools.ini <path_to_game>\app.json http://<path_to_exmoney_server>
Server URL may be omitted for no server.

Run "EXMoney.exe help" for all flags and features, or see the GMG wiki for APMv3 integration.