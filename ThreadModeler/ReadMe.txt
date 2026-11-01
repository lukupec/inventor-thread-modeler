================================================
         Plugin of the Month, May 2011
================================================
------------------------
Inventor ThreadModeler
------------------------

Description
-----------
The ThreadModeler tool has been developed to allow Inventor users
to more easily create realistic modelized threads based on an 
existing Inventor thread feature.

Through functionality provided by the tool, users are able to select
an existing thread feature in the model and the ThreadModeler will
then generate a realistic thread based on the feature properties.

The ThreadModeler can reliably generate a thread which can save a 
lot of time and effort for the user when compared with a manual 
approach, and bring to your models a higher degree of realism.

System Requirements
-------------------
Autodesk Inventor 2027, or Inventor 2026 with Update 2026.4 or later
(both run add-ins on .NET 10). Windows x64 only.

The source code is a Visual Studio 2026 / .NET 10 (net10.0-windows)
C# project (not required to run the plugin). See docs\BUILDING.md.

Installation
------------
Download ThreadModeler-<version>.msi from
https://github.com/lukupec/inventor-thread-modeler/releases/latest
and run it. It installs the add-in for all users to

    %ProgramData%\Autodesk\ApplicationPlugins\ThreadModeler.bundle

Newer versions are offered automatically inside Inventor and installed
after Inventor is closed. See docs\INSTALL.md.

Usage
-----
Once loaded, the ThreadModeler ribbon tab containing the ThreadModeler
panel is available in the Part environment.

When you run the threadModeler command a new modeless form is
displayed, allowing you to select which existing thread features you
want to modelize.

Uninstallation
--------------
You can unload the plugin without uninstalling it by unchecking the
"Loaded" checkbox associated with the plugin in the Inventor Add-In
Manager. Unchecking "Load Automatically" causes the plugin not to be
loaded in future sessions of Inventor.

To remove the plugin completely, uninstall "ThreadModeler for
Autodesk Inventor" in Settings > Apps.

Known Issues
------------

1# 

The minimal thread pitch that the plug-in allows you 
to modelize is 0.0007 in (or 0.01778 mm).
Keep in mind that around this scale the quality of the 
modelization will look very poor in Inventor.


Author 
------
This plugin was written by Philippe Leefsma.

Further Reading
---------------
See README.md and the docs folder of the repository.

Feedback
--------
Please open an issue at
https://github.com/lukupec/inventor-thread-modeler/issues

Release History
---------------

1.0 Original release

1.1 Fixed issue for handling small thread sizes.
    Fixed issue concerning inverted right/left handed thread


30.0 Ported to .NET 10 for Inventor 2026 (2026.4+) and Inventor 2027.
     Renamed to ThreadModeler (30.0.3); derived from coolOrange threadModeler,
     https://github.com/coolOrangeApps/inventor-thread-modeler (MIT License).


(C) Copyright 2011 by coolOrange s.r.l
