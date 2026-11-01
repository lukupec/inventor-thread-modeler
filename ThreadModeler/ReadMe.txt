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
Copy the cODTthreadModeler.bundle folder (from the release zip or
from dist\ after running build.ps1) to

    %ProgramData%\Autodesk\ApplicationPlugins\

and restart Inventor. No registration (RegAsm) is required.
See docs\INSTALL.md.

Usage
-----
Once loaded, the coolOrange ribbon tab containing the threadModeler
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

To remove the plugin completely, close Inventor and delete
%ProgramData%\Autodesk\ApplicationPlugins\cODTthreadModeler.bundle.

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
Email us at support@coolorange.com with feedback or requests for
enhancements.

Release History
---------------

1.0 Original release

1.1 Fixed issue for handling small thread sizes.
    Fixed issue concerning inverted right/left handed thread


30.0 Ported to .NET 10 for Inventor 2026 (2026.4+) and Inventor 2027.


(C) Copyright 2011 by coolOrange s.r.l
