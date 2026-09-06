# NOVR 0.4.12

Close the game, then extract `NOVR-0.4.12.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.12`.

## Stock menus glued to the headset

0.4.11 pushed leftover stock canvases (including the server browser) in front of the headset every frame, so they rotated with your head and stole the cursor.

0.4.12 hides those stock menus while native VR UI is on, and parks remaining game menus in the world like 0.4.3 instead of following yaw. The VR cursor stays available in OpenXR even if the hardware cursor is hidden.

Stereoscopic Zoom View is still temporarily off. HUD layout from 0.4.6 is unchanged.
