# NOVR 0.4.20

Close the game, then extract `NOVR-0.4.20.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.20`.

## Green cross on the menu

The hangar locks the hardware mouse (`Cursor.lockState = Locked`). Older builds treated that as "cockpit, hide the VR pointer", so the ring was never drawn even though the window still captured the mouse.

0.4.20 keeps the pointer while the hangar/pause menus are up. You should see a large green cross in the **center of the menu panel**, plus a second cross that follows the mouse.

Click the game window on the monitor before putting the headset on. Alt-tab still releases the mouse.

Native full-canvas menu stays off. Stereoscopic Zoom View is still temporarily off. HUD layout from 0.4.6 is unchanged.
