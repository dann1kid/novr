# NOVR 0.4.15

Close the game, then extract `NOVR-0.4.15.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.15`.

## Gray pancake on a black view

The native VR menu was a 2 m × 4 m world-space canvas drawn by the headset camera, so it filled the view as a gray disc and hid the hangar.

0.4.15 turns that native canvas off and restores the 0.4.8 overlay (in the camera stack, not submitting its own stereo view). You should see the hangar and the game's own menus again, world-locked about 3 m in front of their parent — not glued to your head.

Stereoscopic Zoom View is still temporarily off. HUD layout from 0.4.6 is unchanged.
