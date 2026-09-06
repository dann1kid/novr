## 0.4.7

Close the game, then extract `NOVR-0.4.7.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.7`.

## VR entry fix

0.4.6 could fail to start OpenXR: one missing Harmony target aborted the whole plugin before VR toggled on, and a failed OpenXR init threw instead of retrying. 0.4.7 skips bad patches, retries OpenXR if the headset was asleep, and no longer recenters yaw when tracking is first acquired.

## HUD (from 0.4.6)

- Tighter HUD: the pitch/climb ladder is a narrow view-cone scale instead of a wide wraparound ring. Weapon and status plates sit closer to the boresight.
- HMD/radar contacts use a smaller off-boresight ring so markers stay in central vision. The HMD tape is attached to the headset, not world origin.
- Cockpit recenter uses seated yaw, runs when you spawn, and Home / VR CENTER work in the cockpit. Look forward during the countdown.
- Proximity gradient: nearer contacts and waypoints are larger, closer, and brighter; distant ones recede.

## Also in 0.4.5

- Stereoscopic VR zoom on Zoom View
- Menus and HUD plates face the headset
- VR CENTER recenters tracking and the native menu plane
