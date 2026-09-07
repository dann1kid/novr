# NOVR 0.4.16

Close the game, then extract `NOVR-0.4.16.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.16`.

## Mouse cursor in the headset

Menus from 0.4.15 are unchanged: hangar visible, no clipping planes, native full-canvas menu still off.

The VR pointer was being projected from the overlay camera at tracking origin, and it hid itself when the game window was unfocused. It now sits in front of the headset camera as a larger green ring and stays visible while the cursor is not locked.

Stereoscopic Zoom View is still temporarily off. HUD layout from 0.4.6 is unchanged.
