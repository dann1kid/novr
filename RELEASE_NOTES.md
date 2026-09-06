# NOVR 0.4.9

Close the game, then extract `NOVR-0.4.9.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.9`.

## Menu cursor and center occlusion

0.4.8 stopped the VR UI camera from drawing in the headset to avoid a black view. That also hid the VR cursor and native menus, so the 3D globe stayed visible but you could not click Start. The green cursor ring could also sit on the near clip plane and look like a sphere covering the middle of the view.

0.4.9 restores the 0.4.3 overlay camera (stereo Both in the URP stack). The zoom hook from 0.4.8 stays off. The VR cursor stays visible while OpenXR is running even if the hardware cursor is hidden, and it is kept at least 1 m from the headset. The fade canvas only sticks to the HMD while it is actually fading.

Stereoscopic Zoom View is still temporarily off. HUD layout from 0.4.6 is unchanged.
