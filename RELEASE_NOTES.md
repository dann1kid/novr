# NOVR 0.4.14

Close the game, then extract `NOVR-0.4.14.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.14`.

## Black headset and clipping plane

The VR UI overlay camera is now fully disabled and removed from every URP camera stack, so it cannot replace the game with black. Menus and the cursor are drawn by the headset camera.

Full-screen native menu backgrounds are no longer copied into the world. Stock world-space canvases that sit in the headset plane are parked off-origin. The fade quad only appears during a real CanvasGroup fade, not as a leftover wall you can walk through.

Stereoscopic Zoom View is still temporarily off. HUD layout from 0.4.6 is unchanged.
