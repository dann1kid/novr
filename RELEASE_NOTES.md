# NOVR 0.4.23

Close the game, then extract `NOVR-0.4.23.zip` into `Nuclear Option/BepInEx`. `BepInEx/plugins/NOVR/version.txt` should read `0.4.23`.

## Green plus on the splash

0.4.21's plus was visible, but it was drawn into the hangar camera *under* the curved splash, so you only saw it when looking aside at black — and it doubled in the two lenses.

0.4.22 removed that pass and tried a 3D cube in front of your face. That cube never showed.

0.4.23 draws **one** green plus in the same overlay pass as the hangar menus, so it sits **on** the splash/menu and follows the mouse. It is not drawn into the hangar/desktop camera.

Click the game window before putting the headset on. Alt-Tab releases the mouse.

Native full-canvas menu stays off. Stereoscopic Zoom View is still temporarily off.
