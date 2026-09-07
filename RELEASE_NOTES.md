# NOVR 0.4.18

Close the game, then extract `NOVR-0.4.18.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.18`.

## Green ring on the menus

0.4.17 captured the mouse but the green ring was projected from the hangar camera, so the VR overlay never drew it. The pointer is now attached to `MainCanvas` / `MenuCanvas` — the same world-space menus you already see — and sits a few centimeters in front of them as a large green ring.

Mouse capture is unchanged: while the game window is focused, the OS cursor stays in that window. Alt-tab releases it. Click the game window on the monitor before putting the headset on.

Native full-canvas menu stays off. Stereoscopic Zoom View is still temporarily off. HUD layout from 0.4.6 is unchanged.
