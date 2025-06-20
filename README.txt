APMCoreFixes / EMoneyUILink
2024-2025 Haruka
Licensed under GPLv3.

See also: https://github.com/akechi-haruka/APMv3MenuTranslation

--- APMCoreFixes ---

Adds some QoL features to the launcher.

* Disable ABaasGS encryption / Fake ABaasLink online status
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
* Delay startup
* SegAPI integration
    - Add an "exit game" button
    
--- eMoneyUILink ---

Library for connecting the E-Money UI memory to OpenMoney.

--- EMUISharedBackend ---

Integrates eMoneyUI into non-APM games via SegAPI.

This requires the game to run in windowed/borderless.

Usage: EMUISharedBackend <app.json> <vfd_port/0> <emoneyui_exe> <openmoney_addr> <keychip> <segatools_group> <segatools_device> <segatools_broadcast> <segatools_port> <item_name>
All arguments are required.
 - app.json: Path to the game's app.json file (from the APM option directory)
 - vfd_port: COM port to the VFD, or 0 to disable.
 - emoneyui_exe: Path to the eMoneyUI.exe from the APM launcher directory
 - openmoney_addr: Address to the OpenMoney server
 - keychip: Keychip ID
 - segatools_group: SegAPI Group ID
 - segatools_device: SegAPI Device ID
 - segatools_broadcast: SegAPI Broadcast address
 - segatools_port: SegAPI Port (use 5364)
 - item_name: The name of the purchased item (use Credit)
 