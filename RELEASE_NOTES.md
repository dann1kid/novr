# NOVR 0.4.21

Close the game, then extract `NOVR-0.4.21.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.21`.

## Green cross on the hangar splash

The splash/hangar with the aircraft is drawn by the game's XR camera. Previous pointers lived on a separate UI overlay layer, so they never appeared in the headset — and if the menu canvas was not found yet, they were hidden entirely.

0.4.21 draws a large unlit green cross **in front of the hangar camera**:
- one stuck in the center of your view (look around — it should follow your head)
- one that follows the mouse

Mouse capture is unchanged. Click the game window on the monitor before putting the headset on.

Native full-canvas menu stays off. Stereoscopic Zoom View is still temporarily off. HUD layout from 0.4.6 is unchanged.
