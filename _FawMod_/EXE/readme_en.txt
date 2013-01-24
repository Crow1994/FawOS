//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//ENBSeries is a set of graphical modifications for games
//Description on the web page may not be equal to this info.
//created by Boris Vorontsov http://enbdev.com
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

PUBLISHING BINARY FILES OF ENBSERIES ON NEXUS SITES (TES NEXUS, SKYRIM NEXUS, ETC)
IS STRICTLY PROHIBITED. ONLY PRESETS AND SHADERS CAN BE HOSTED THERE.



ENBSeries v0.132 for TES Skyrim (graphic mod).

Bugfixes of previous versions.



WARNING!
bFloatPointRenderTarget=1 must be set in SkyrimPrefs.ini file to make this mod work.
Start SkyrimLauncher.exe to configure your video options again.


This is not final version, performance and quality are not good yet.
Description of presets to this version will be available later on http://enbdev.com
Bright hairs of characters is result of post processing in enbeffect.fx shader.
Performance and visual tweaking is work on your side. If you don't want to learn how
edit parameters of ENBSeries, get configuration files of other users.


Version 0.131:
Fixed fog for water, added new category [PARTICLE] to control many transparent
objects (including waterfall particles).

Version 0.130:
Added Edge AA instead of hardware antialiasing (MSAA), which not supported in
all versions after 0.119. Unfortunately changes made in 0.129 beta produce
artifacts for some users, this is not fixed yet, because of no testers.

Version 0.126:
This version differ from 12.12.12 by GUI implemented. To activate it press together
SHIFT and ENTER keys. Some variables can't be changed in GUI, better not to touch
quality parameters at this moment, they are not dynamic.
In version 0.126 vs 0.125 added some interior variables for ambient occlusion
properties; different frame limiter code (it worked wrong for some users); GUI
editing now allow keyboard input for changing values.

Version 0.125:
Internal GUI implemented to simplify editing presets.

Version 12.12.12:
Optimized reflections. Fixed some minor bugs

Version 0.123:
Reflections implemented, they aren't good quality and very slow now and only for testing
purposes if they fit TES Skyrim or not. Also fixed bug when AOMixingType set 2.

Version 0.122:
Parallax fix changed to different and now don't have specular artifacts (see parallax.txt).
Ssao and ssil quality increased and changed their mixing to scene methods (added new variables).
There are many other changes, which i don't remember and never do logs, sorry. Reflections
are not yet finished. Bug with bright silhouettes around objects very annoying, but i'm still
searching better algorithm to detect edges for bilateral filter for various distances.
And finally, performance is from v0.121, except ssao.

Version 0.121:
Performance and quality optimizations, mostly.

Version 0.119:
Added parameters for tweaking interior separately. Old *Day and *Night variables are
now only for exterior scenes.
Added to shaders variable EInteriorFactor for toggling between interior and exterior
scenes.

Version 0.117:
To fix issue with bright hair added new parameter GammaCurve which is equal to parameters
ColorPowDay and ColorPowNight, but apply to entire screen and should replace those two.
But global usage of this variable may require changes in most of other parameters. Also
new global variable is Brightness.
Added fake clouds scattering on their edges, similar to Fallout New Vegas and GTA 4 versions.
Parameters for this effect are CloudsEdgeClamp and CloudsEdgeIntensity.
This version have all previous bug fixes and new feature is programmable lenz fx or any
other sprites for sun only. New external shader file named enbsunsprite.fx and it use
texture for computations named enbsunsprite.tga, enbsunsprite.png or enbsunsprite.bmp.
This effect is done for modders who know how to edit shaders, probably later i'll create
my own, but at this moment there are many other things not implemented yet (depth of
field effect is also still as example for modders and some of them did good fx).

Version 0.114:
Test of sky lighting effect (fake ambient occlusion), everything else is almost 0.113.
To work correctly, sky lighting require rendering objects to shadow, so edit manually
following lines in file SkyrimPrefs.ini:
bTreesReceiveShadows=1
bDrawLandShadows=1
bShadowsOnGrass=1
And make sure that bFloatPointRenderTarget=1 is set in same file.
New parameters:
UseOriginalObjectsProcessing turn off all per object changes of the mod for those, who
wish to use only parallax fix or some other components without radical changes to graphic.
FixGameBugs is bugfix of underwater and grass bugs of game patch 1.5.26.0 for ATI users.
FixParallaxBugs fix game bugs when parallax mod installed, temporary for NVidia only.
AntiBSOD and SpeedHack increasing performance in some locations.
UseComplexIndirectLighting affect performance of ssao if UseIndirectLighting=true, but
quality of indirect lighting is lower.
EnableDetailedShadows temporary for NVidia only, new effect which increase shadow details
(video as example http://youtu.be/9uG-s9cuPPM)
ShadowCastersFix some huge objects or mountains cast shadows more correctly.
ShadowQualityFix temporary for NVidia only, decreasing noise of game shadows by little
cost of performance.
DetailedShadowQuality temporary for NVidia only, greatly affect performance of shadows.
UseBilateralShadowFilter temporary for NVidia only, reduce blurred edges of shadows
artifact which exist in original game.

Version 0.113:
Difference between new and previous version is sun rays effect.
This version is similar to 0.103, but with new shadow effect, bugfix for parallax,
optimization. Performance in this version optimized for ATI videocards also, but
i don't have hardware for testing, so not sure how it work on practice. Do not set
ForceFakeVideocard=true in this version, it's now for bugfixing mode and will be removed
later. Parallax bugfix also should work for ATI users (mod is here http://skyrim.nexusmods.com/downloads/file.php?id=16919).
Partially changed code of SSAO and Indirect Lighting mix for better quality, modified
method of computing distance fade factors of these effects (FadeFogRangeDay, FadeFogRangeNight).
Changed detailed shadow quality presets (-1 is extreme, 0 is high, 1 is middle, 2 is low)
for better details.

Version 0.110:
Similar to 0.103, but with new shadow effect, bugfix for parallax, optimization.
Best performance with NVidia cards temporary, but ATI users also may use this version,
it have fixes for bugs of 1.5.26.0 patch (set ForceFakeVideocard=true).
For NVidia users this version do not create fake videocard "ENB", so you must redetect
graphic options by game launcher if previously used modification 0.103 or earlier version.

Version 0.109:
Test beta version for NVidia cards with huge optimization.

Version 0.108:
Simplified version with many effects removed, but performance is very high.

Version 0.105, 0.106:
Optimized code to make it less cpu dependent.

Version 0.103:
Implemented experimental code of injector instead of standart d3d9 wrapper. This
may be useful for users with Optimus laptops or for those, who using overlay tools
like EVGA, Afterburn, D3D Overrider, XFire and others. Graphic changes are only get
back to SSAO code from version 0.099 with minor update of indirect lighting intensity.

Version 0.102 Tatsudoshi:
Fixed few bugs, most work is done for increasing performance (not all yet). Many
bugs of previous version is still here, i'll fix them later.

Version 0.101:
Fixed bugs of previous version (at least what is see). Added code of programmable
external depth of field effect (only added, but "todo"). With ne enbeffectprepass.fx
shader file you can make more than just depth of field, it's executed before enbeffect.fx
and working with hdr values in multipass mode (up to 8 passes). Changed standart of
external shaders, removed ScreenScaleY and ScreenSize replaced by vector of 4 values.
Most of old effects will not work, replace in them ScreenScaleY with ScreenSize.z
and float ScreenSize; with float4 ScreenSize;. Increased quality of bloom and removed
parameters of radius 1 and 2 for it.

Version 0.100:
Removed parameter CyclicConfigReading (it read configuration file every 5 seconds),
from now this will be handled by pressing a button BACK (can be changed KeyReadConfig).
Added almost all code from my patch AntiFREEZE TES Skyrim 0.096, including most
of it parameters. FPS limiter implemented, fps counter. Screenshot capturing is back,
but different key assigned.
SSAO effect now have additional "lite" version. To switch it setup parameter
UseIndirectLighting=false in enbseries.ini file and restart the game. Added values
to control SSAO distance relative to fog distance. Night and day time are separated.
Properties for adaptation in enbseries.ini are finished, but they are partially
clamped by limits in enbeffect.fx, so if you wish to control by enbeffect.ini only,
remove limit code in shader (or wait when i'll post new shader). Added parameters
for SubSurfaceScattering to reduce lighting in shadows for characters and ugly thin
line on them (game bug). Added parameter for control of lights from windows, but
it affect some fx, for example freeze spell. Added ShadowObjectsFix to apply
shadows from mountains properly. Various bug fixes. SSAO work with antialiasing.

Version 0.099:
Fixed crash in the evening. Added palette texture support (enbpalette.bmp, tga,
png files). Removed code for screenshot capturing. SSAO disabled by default, activate
it in enbseries.ini, parameter UseAmbientOcclusion=true and make sure antialiasing
is not enabled by game or drivers. Not tested with other d3d9.dll files and they are
not supported now.

Version 0.098:
Implemented code from AntiFREEZE patch to fix some game bugs and increase
stability with this mod. Added parameters to control environmental fog.

Version 0.097:
I'm going to be crazy with fixing game bugs for make it work with ENBSeries,
official patches destroy my progress every time, so i decided to release "light"
version. Crashes of the game 1.1 happening very frequently (at least on my PC),
they are not internal modification errors. Hardware antialiasing (multisampling)
unsupported at this moment, so to make SSAO work properly, disable antialiasing (msaa).
Optimization not applied, same as in any other first versions of ENBSeries, if
you wish to get higher framerate, turn off SSAO or decrease quality of it. Game
have some strange mistakes which aren't fixed yet, for example in interior locations
direct light enabled and applied from bottom or from side (sun, uh?), so increaing
intensity of it is not good idea, better to decrease all other values together and
increase overall brightness in post processing shader enbeffect.fx. Users, who already
tweaked parameters for GTA 4 version will not have much problems with this one.
Do not change SubSurfaceScattering parameters, game have bug for characters and i'll
fix it when latest official patch will be released (bright thin line on skin).




//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
INSTALLING:
Extract files from archive in to the game directory or where game execution file exist (.exe).
Run game launcher to reconfigure it again.


PROBLEMS:
If game crashing on startup with this patch, make sure you are not running XFire,
Afterburn, EVGA and other kind of that tools.


//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
DONATE:
If on Your opinion ENBSeries project must continue to live or simply it was useful
for yourself, i'll be grateful for sponsoring project (or donation).



Using AntTweakBar middleware
Copyright (C) 2005-2011 Philippe Decaudin
AntTweakBar web site: http://www.antisphere.com



http://enbdev.com
Copyright (c) 2007-2012 Vorontsov Boris (ENB developer)
