# NOVR 0.4.19

Close the game, then extract `NOVR-0.4.19.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.19`.

## Green pointer on the menus

Mouse capture was already working. The ring still did not show because it was a separate 3D mesh with its own shader, and the VR overlay only draws the game's UI canvases.

The pointer is now a child of those menus: a large green cross, a ring, and a + sign, built from the same Unity `Image` / `Text` components as the buttons. It should sit on the panel and follow the mouse.

Click the game window on the monitor before putting the headset on. Alt-tab still releases the mouse.

Native full-canvas menu stays off. Stereoscopic Zoom View is still temporarily off. HUD layout from 0.4.6 is unchanged.
