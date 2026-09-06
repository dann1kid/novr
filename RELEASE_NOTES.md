# NOVR 0.4.10

Close the game, then extract `NOVR-0.4.10.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.10`.

## Black square in the headset

0.4.9 put the VR menus back, but the game's `BlackoutCanvas` was still a full-screen black UI panel converted to world space. That panel sat in the camera frustum and looked like a black square cutting across the view.

0.4.10 keeps that canvas disabled in VR. Scene fades use a separate quad that only appears while a fade is actually running. The VR UI overlay camera also stops clearing to a black depth buffer.

Stereoscopic Zoom View is still temporarily off. HUD layout from 0.4.6 is unchanged.
