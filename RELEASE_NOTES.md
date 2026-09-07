# NOVR 0.4.22

Close the game, then extract `NOVR-0.4.22.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.22`.

## One green cross, on the splash

0.4.21 put a huge cross in each eye twice (a 3D pair plus a GL overlay), so it looked like two crosses stuck to the lenses. The splash/menu curve also sat in front of it.

0.4.22:
- one cross only (follows the mouse)
- real 3D depth, so both eyes should fuse it
- about 1 m in front of your face, so it should sit *in front of* the curved splash instead of behind it
- no GL overlay (the desktop window should no longer be a lone cross on black)

Look at the splash: you should see one green plus. Look aside: it should stay in front of you with the mouse.

Native full-canvas menu stays off. Stereoscopic Zoom View is still temporarily off.
