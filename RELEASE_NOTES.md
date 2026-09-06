# NOVR 0.4.13

Close the game, then extract `NOVR-0.4.13.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.13`.

## Black headset plus leftover plane

The VR UI overlay camera was submitting its own stereo view, which replaced the game with black. A full-size native menu background sat in the world, so leaning the headset through it showed a wall.

0.4.13 stops that overlay from rendering to the HMD. VR UI is drawn by the headset camera. Full-screen menu backgrounds are removed so they cannot clip the view.

Stereoscopic Zoom View is still temporarily off. HUD layout from 0.4.6 is unchanged.
