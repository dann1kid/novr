# NOVR 0.4.11

Close the game, then extract `NOVR-0.4.11.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.11`.

## Opaque plane in the headset

The headset starts at world origin. Several stock menus were converted to world-space canvases and left on that same origin, so the HMD sat inside an opaque UI plane. Leaning back left the plane in the world and it blocked the view.

0.4.11 parks suppressed stock menus far offscreen, keeps world-space canvases from being snapped to origin, and moves any leftover plane that still sits in the headset 3 m forward (or hides it).

Stereoscopic Zoom View is still temporarily off. HUD layout from 0.4.6 is unchanged.
