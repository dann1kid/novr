# NOVR 0.4.8

Close the game, then extract `NOVR-0.4.8.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.8`.

## Black headset fix

0.4.7 reached OpenXR but could show a black HMD: a HarmonyX postfix on URP `XRPass.GetProjMatrix` could replace the projection with a zero matrix, and the VR UI camera could submit its own empty stereo view. 0.4.8 disables that zoom hook, stops the UI camera from rendering to the headset, and restores the 0.4.3 FOV guard. Stereoscopic Zoom View is temporarily off; HUD layout from 0.4.6 is unchanged.

The patcher also continues if `Unity.XR.Management.dll` is locked by another process instead of aborting the rest of CopyToGame.

## Also in 0.4.7

A failed Harmony patch or a not-yet-ready headset no longer prevents OpenXR from starting.

## HUD (from 0.4.6)

- Tighter HUD: the pitch/climb ladder is a narrow view-cone scale instead of a wide wraparound ring. Weapon and status plates sit closer to the boresight.
- HMD/radar contacts use a smaller off-boresight ring so markers stay in central vision. The HMD tape is attached to the headset, not world origin.
- Cockpit recenter uses seated yaw via Home / VR CENTER. Look forward during the countdown.
- Proximity gradient: nearer contacts and waypoints are larger, closer, and brighter; distant ones recede.
