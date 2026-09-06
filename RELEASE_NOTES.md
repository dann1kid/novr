# NOVR 0.4.6

Extract `NOVR.zip` into `Nuclear Option/BepInEx` (not the game root). Close the game first.

After install, `BepInEx/plugins/NOVR/version.txt` should read `0.4.6`.

## Changes

- Tighter HUD: the pitch/climb ladder is a narrow view-cone scale instead of a wide wraparound ring. Weapon and status plates sit closer to the boresight.
- HMD/radar contacts use a smaller off-boresight ring so markers stay in central vision. The HMD tape is attached to the headset, not world origin.
- Cockpit recenter uses seated yaw, runs when you spawn, and Home / VR CENTER work in the cockpit. Look forward during the countdown.
- Proximity gradient: nearer contacts and waypoints are larger, closer, and brighter; distant ones recede.

## Also in 0.4.5

- Stereoscopic VR zoom on Zoom View
- Menus and HUD plates face the headset
- VR CENTER recenters tracking and the native menu plane
