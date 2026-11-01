////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved 
// Written by Philippe Leefsma 2011 - ADN/Developer Technical Services
//
// This software is provided as is, without any warranty that it will work. You choose to use this tool at your own risk.
// Neither Autodesk nor the author Philippe Leefsma can be taken as responsible for any damage this tool can cause to 
// your data. Please always make a back up of your data prior to use this tool, as it will modify the documents involved 
// in the feature transformation.
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using Inventor;
using ThreadModeler.Addin;

namespace ThreadModeler
{
    /////////////////////////////////////////////////////////////////
    // Provides Thread-specific utilities
    //
    /////////////////////////////////////////////////////////////////
    class ThreadWorker
    {
        private static TransientGeometry _Tg;

        private static Inventor.Application _Application;

        private static bool _ConstructionWorkFeature = true;

        public const double ThresholdPitchCm = 0.001778;

        /////////////////////////////////////////////////////////////
        // Reason of the last failure of ModelizeThreads (null if
        // none), and whether it was the coil that could not be
        // computed (then a bigger pitch offset may help).
        /////////////////////////////////////////////////////////////
        public static string LastError { get; private set; }

        public static bool LastErrorIsCoil { get; private set; }

        private static bool Fail(string reason, Exception ex = null, bool coil = false)
        {
            if (LastError == null)
            {
                LastError = reason;
                LastErrorIsCoil = coil;
            }

            AddinLog.Error("Modelling: " + reason, ex);

            return false;
        }

        /////////////////////////////////////////////////////////////
        //use: Initialize the Toolkit library
        //
        /////////////////////////////////////////////////////////////
        public static void Initialize(
            Inventor.Application Application)
        {
            _Application = Application;

            _Tg = _Application.TransientGeometry;
        }

        /////////////////////////////////////////////////////////////
        // Comments of the parameters a thread profile template
        // sketch must have.
        /////////////////////////////////////////////////////////////
        public static readonly string[] TemplateParameters =
            { "Pitch", "ThreadOffset", "MajorRadius", "MinorRadius" };

        /////////////////////////////////////////////////////////////
        // Use: Opens a thread profile template invisibly. Returns
        //      null if it can be used, otherwise the reason.
        //      The template is left open: closing a document
        //      empties the undo stack.
        /////////////////////////////////////////////////////////////
        public static string OpenTemplate(string path,
            out PartDocument template)
        {
            template = null;

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                return "Template file not found: " + path;

            bool silentOp = _Application.SilentOperation;

            try
            {
                _Application.SilentOperation = true;

                template = _Application.Documents.Open(path, false)
                    as PartDocument;
            }
            catch (Exception ex)
            {
                AddinLog.Warn("Cannot open template " + path, ex);
                return "Cannot open " + path + ": " + ex.Message;
            }
            finally
            {
                _Application.SilentOperation = silentOp;
            }

            if (template == null)
                return path + " is not a part document.";

            if (template.ComponentDefinition.Sketches.Count == 0)
                return path + " contains no sketch.";

            foreach (string name in TemplateParameters)
            {
                if (!Toolkit.FindParameter(template, name))
                    return path + " has no parameter with comment '" +
                        name + "'.";
            }

            return null;
        }

        /////////////////////////////////////////////////////////////
        // Use: High-level method that modelizes a collection of
        //      ThreadFeatures.
        /////////////////////////////////////////////////////////////
        public static bool ModelizeThreads(PartDocument doc,
            PlanarSketch templateSketch,
            IEnumerable<ThreadFeature> threads,
            double extraPitch)
        {
            bool ret = true;

            LastError = null;
            LastErrorIsCoil = false;

            foreach (ThreadFeature thread in threads)
            {
                AddinLog.Info("Modelling " + thread.Name + " (" +
                    thread.ThreadInfoType + ", extra pitch " + extraPitch + " %).");

                switch (thread.ThreadInfoType)
                { 
                    case ThreadTypeEnum.kStandardThread:

                        if (!ThreadWorker.ModelizeThreadStandard(doc,
                            thread as PartFeature,
                            templateSketch,
                            thread.ThreadInfo,
                            thread.ThreadedFace[1],
                            extraPitch))

                            ret = false;

                        break;

                    case ThreadTypeEnum.kTaperedThread:

                        if (!ThreadWorker.ModelizeThreadTapered(doc, 
                            thread as PartFeature,
                            templateSketch,
                            thread.ThreadInfo,
                            thread.ThreadedFace[1],
                            extraPitch))

                            ret = false;

                        break;

                    default:
                        break;
                }
            }

            return ret;
        }

        /////////////////////////////////////////////////////////////
        // Use: Modelizes a Standard ThreadFeature.
        //
        /////////////////////////////////////////////////////////////
        public static bool ModelizeThreadStandard(PartDocument doc, 
            PartFeature feature,
            PlanarSketch templateSketch, 
            ThreadInfo threadInfo,
            Face threadedFace,
            double extraPitch)
        {
            Transaction Tx = 
             _Application.TransactionManager.StartTransaction(
                doc as _Document,
                "Modelizing Thread " + feature.Name);

            string step = "read thread data";

            try
            {
                // Body the modeled thread is joined to at the end.
                SurfaceBody threadedBody = threadedFace.SurfaceBody;

                double pitch =
                    ThreadWorker.GetThreadPitch(threadInfo);

                Vector threadDirection =
                    threadInfo.ThreadDirection;

                step = "determine interior/exterior face";

                bool isInteriorFace =
                    Toolkit.IsInteriorFace(threadedFace);

                Point basePoint =
                    threadInfo.ThreadBasePoints[1] as Point;

                AddinLog.Info("  pitch " + pitch + " cm, length " +
                    threadDirection.Length + " cm, " +
                    (isInteriorFace ? "interior" : "exterior") + ".");

                if (isInteriorFace)
                {
                    Vector normal =
                        Toolkit.GetOrthoVector(
                        threadDirection.AsUnitVector()).AsVector();

                    normal.ScaleBy(
                        ThreadWorker.GetThreadMajorRadiusStandard(
                            threadedFace));

                    basePoint.TranslateBy(normal);
                }

                step = "insert profile sketch from template";

                PlanarSketch newSketch = Toolkit.InsertSketch(doc,
                    templateSketch,
                    threadDirection.AsUnitVector(),
                    Toolkit.GetOrthoVector(
                        threadDirection.AsUnitVector()),
                    basePoint);

                Point coilBase =
                    threadInfo.ThreadBasePoints[1] as Point;

                bool rightHanded = threadInfo.RightHanded;

                double taper = 0;

                step = "prepare coil body";

                if (!ThreadWorker.InitializeForCoilStandard(doc,
                    threadInfo,
                    threadedFace,
                    newSketch.Name, 
                    isInteriorFace, pitch))
                {
                    Tx.Abort();
                    return false;
                }

                step = "create profile from template sketch";

                Profile profile =
                  newSketch.Profiles.AddForSolid(true, null, null);

                step = "create coil";

                if (!ThreadWorker.CreateCoilFeature(doc,
                    threadedBody,
                    profile,
                    threadDirection,
                    coilBase,
                    rightHanded,
                    taper,
                    pitch,
                    extraPitch))
                {
                    Tx.Abort();
                    return false;
                }

                newSketch.Shared = false;

                feature.Suppressed = true;

                Tx.End();

                return true;
            }
            catch (Exception ex)
            {
                Fail(feature.Name + ": " + step + " failed: " + ex.Message, ex);

                try
                {
                    Tx.Abort();
                }
                catch (Exception)
                {
                }

                return false;
            }
        }

        /////////////////////////////////////////////////////////////
        // Use: Modelizes a Tapered ThreadFeature.
        //
        /////////////////////////////////////////////////////////////
        public static bool ModelizeThreadTapered(PartDocument doc,
            PartFeature feature,
            PlanarSketch templateSketch,
            ThreadInfo threadInfo,
            Face threadedFace,
            double extraPitch)
        {
            Transaction Tx = 
             _Application.TransactionManager.StartGlobalTransaction(
              doc as _Document,
              "Modelizing Thread ");

            string step = "read thread data";

            try
            {
                // Body the modeled thread is joined to at the end.
                SurfaceBody threadedBody = threadedFace.SurfaceBody;

                double pitch =
                    ThreadWorker.GetThreadPitch(threadInfo);

                Vector threadDirection =
                    threadInfo.ThreadDirection;

                Point coilBase =
                    threadInfo.ThreadBasePoints[1] as Point;

                step = "determine interior/exterior face";

                bool isInteriorFace =
                    Toolkit.IsInteriorFace(threadedFace);

                AddinLog.Info("  pitch " + pitch + " cm, length " +
                    threadDirection.Length + " cm, " +
                    (isInteriorFace ? "interior" : "exterior") + ", tapered.");

                step = "insert profile sketch from template";

                Line sideDir =
                    ThreadWorker.GetThreadSideDirection(
                        threadInfo,
                        threadedFace);

                Vector sketchYAxis = 
                    sideDir.RootPoint.VectorTo(coilBase);

                sketchYAxis.ScaleBy((isInteriorFace ? -1.0 : 1.0));

                PlanarSketch newSketch = Toolkit.InsertSketch(doc,
                    templateSketch,
                    sideDir.Direction,
                    sketchYAxis.AsUnitVector(),
                    sideDir.RootPoint);

                bool rightHanded = threadInfo.RightHanded;

                bool IsExpanding = ThreadWorker.IsExpanding(
                    threadInfo,
                    threadedFace);

                double taper = 
                    Math.Abs(threadDirection.AngleTo(
                        sideDir.Direction.AsVector())) 
                        * (IsExpanding ? 1.0 : -1.0);

                step = "prepare coil body";

                if (!ThreadWorker.InitializeForCoilTapered(doc,
                    threadInfo, 
                    threadedFace,
                    newSketch.Name, 
                    isInteriorFace, pitch))
                {
                    Tx.Abort();
                    return false;
                }

                step = "create profile from template sketch";

                Profile profile =
                    newSketch.Profiles.AddForSolid(true,
                        null,
                        null);

                step = "create coil";

                if (!ThreadWorker.CreateCoilFeature(doc,
                    threadedBody,
                    profile,
                    threadDirection,
                    coilBase,
                    rightHanded,
                    taper,
                    pitch,
                    extraPitch))
                {
                    Tx.Abort();
                    return false;
                }

                newSketch.Shared = false;

                feature.Suppressed = true;

                Tx.End();

                return true;
            }
            catch (Exception ex)
            {
                Fail(feature.Name + ": " + step + " failed: " + ex.Message, ex);

                try
                {
                    Tx.Abort();
                }
                catch (Exception)
                {
                }

                return false;
            }
        }

        /////////////////////////////////////////////////////////////
        // Use: Finds a parameter copied from the template sketch
        //      (see Toolkit.FindAndUpdateParameter), logs if missing.
        /////////////////////////////////////////////////////////////
        private static Parameter FindTemplateParameter(
            PartDocument doc,
            string comment,
            string sketchName)
        {
            Parameter parameter =
                Toolkit.FindAndUpdateParameter(doc, comment, sketchName);

            if (parameter == null)
                Fail("parameter with comment '" + comment +
                    "' not found after copying the template sketch " +
                    "into " + sketchName + ".");

            return parameter;
        }

        /////////////////////////////////////////////////////////////
        // Use: Initializes parameters to create new solid bodies
        //      affected by the future CoilFeature for Standard
        //      Thread.
        /////////////////////////////////////////////////////////////
        private static bool InitializeForCoilStandard(
            PartDocument doc, 
            ThreadInfo threadInfo, 
            Face threadedFace,
            string sketchName,
            bool isInteriorFace, double pitchValue)
        {
            Parameter pitch =
                ThreadWorker.FindTemplateParameter(doc,
                    "Pitch",
                    sketchName);

            if (pitch == null)
                return false;

            Parameter offset = 
                ThreadWorker.FindTemplateParameter(doc, 
                    "ThreadOffset", 
                    sketchName);

            if (offset == null)
                return false;

            Parameter major = 
                ThreadWorker.FindTemplateParameter(doc, 
                    "MajorRadius", 
                    sketchName);

            if (major == null)
                return false;

            Parameter minor = 
                ThreadWorker.FindTemplateParameter(doc, 
                    "MinorRadius", 
                    sketchName);

            if (minor == null)
                return false;

            pitch.Value = pitchValue;

            offset.Value = 0;

            double majorRad = 
                ThreadWorker.GetThreadMajorRadiusStandard(
                    threadedFace);

            //Modif
            major.Value = (isInteriorFace ? 0 : majorRad);
 
            doc.Update();

            double minorRad = 
                (isInteriorFace ? 
                    majorRad + Math.Abs((double)minor.Value) : 
                    Math.Abs((double)minor.Value));

            if (isInteriorFace)
            {
                major.Value = (isInteriorFace ? Math.Abs((double)minor.Value) : majorRad);
                doc.Update();
            }

            bool ret = ThreadWorker.CreateCoilBodyStandard(doc, 
                threadInfo, 
                minorRad, 
                majorRad,
                isInteriorFace);

            return ret;
        }

        /////////////////////////////////////////////////////////////
        // Use: Initializes parameters to create new solid bodies
        //      affected by the future CoilFeature for Tapered
        //      Thread.
        /////////////////////////////////////////////////////////////
        private static bool InitializeForCoilTapered(
            PartDocument doc,
            ThreadInfo threadInfo,
            Face threadedFace,
            string sketchName,
            bool isInteriorFace, double pitchValue)
        {
            Parameter pitch = 
                ThreadWorker.FindTemplateParameter(doc, 
                    "Pitch", 
                    sketchName);

            if (pitch == null)
                return false;

            Parameter offset = 
                ThreadWorker.FindTemplateParameter(doc, 
                    "ThreadOffset", 
                    sketchName);

            if (offset == null)
                return false;

            Parameter major = 
                ThreadWorker.FindTemplateParameter(doc, 
                    "MajorRadius", 
                    sketchName);

            if (major == null)
                return false;

            Parameter minor = 
                ThreadWorker.FindTemplateParameter(doc, 
                    "MinorRadius", 
                    sketchName);

            if (minor == null)
                return false;

            pitch.Value = pitchValue;

            offset.Value = 0;

            double majorRad = 0;

            major.Value = (isInteriorFace ? 0 : majorRad);

            doc.Update();

            double minorRad = 
                (isInteriorFace ? 
                    majorRad + Math.Abs((double)minor.Value) : 
                    Math.Abs((double)minor.Value));

            bool ret = ThreadWorker.CreateCoilBodyTapered(doc,
                threadInfo, 
                threadedFace,
                Math.Abs(majorRad - minorRad),
                isInteriorFace);

            return ret;
        }

        /////////////////////////////////////////////////////////////
        // Use: Creates new solid bodies affected by the future
        //      CoilFeature for Standard Thread.
        /////////////////////////////////////////////////////////////
        private static bool CreateCoilBodyStandard(PartDocument doc,
            ThreadInfo threadInfo,
            double minorRad,
            double majorRad,
            bool isInteriorFace)
        {
            try
            {
                PartComponentDefinition compDef = 
                    doc.ComponentDefinition;

                Vector direction = threadInfo.ThreadDirection;

                Point basePoint =
                    threadInfo.ThreadBasePoints[1] as Point;

                Point endPoint = _Tg.CreatePoint(
                    basePoint.X + direction.X,
                    basePoint.Y + direction.Y,
                    basePoint.Z + direction.Z);

                UnitVector yAxis = direction.AsUnitVector();

                UnitVector xAxis = Toolkit.GetOrthoVector(yAxis);

                WorkPoint wpt = compDef.WorkPoints.AddFixed(basePoint, 
                    _ConstructionWorkFeature);

                WorkPlane wpl = compDef.WorkPlanes.AddFixed(basePoint, 
                    xAxis, 
                    yAxis, 
                    _ConstructionWorkFeature);

                WorkAxis xWa = compDef.WorkAxes.AddFixed(basePoint, 
                    xAxis, 
                    _ConstructionWorkFeature);

                WorkAxis yWa = compDef.WorkAxes.AddFixed(basePoint, 
                    yAxis, 
                    _ConstructionWorkFeature);

                Point sidePt = _Tg.CreatePoint(
                    basePoint.X + xAxis.X * majorRad,
                    basePoint.Y + xAxis.Y * majorRad,
                    basePoint.Z + xAxis.Z * majorRad);

                //Modif
                //double revDepth = 
                //    Math.Abs(majorRad - minorRad) * 
                //    (isInteriorFace ? -1.0 : 1.0);

                double revDepth = Math.Abs(majorRad - minorRad);

                Line l1 = _Tg.CreateLine(sidePt, yAxis.AsVector());

                Line l2 = _Tg.CreateLine(basePoint, xAxis.AsVector());

                Line l3 = _Tg.CreateLine(endPoint, xAxis.AsVector());

                Point p1 =
                    l1.IntersectWithCurve(l2, 0.0001)[1] as Point;

                Point p2 =
                    l1.IntersectWithCurve(l3, 0.0001)[1] as Point;

                Point p3 = _Tg.CreatePoint(
                    p2.X - xAxis.X * revDepth,
                    p2.Y - xAxis.Y * revDepth,
                    p2.Z - xAxis.Z * revDepth);

                Point p4 = _Tg.CreatePoint(
                    p1.X - xAxis.X * revDepth,
                    p1.Y - xAxis.Y * revDepth,
                    p1.Z - xAxis.Z * revDepth);

               
                SketchPoint skp1 = null;
                SketchPoint skp2 = null;
                SketchPoint skp3 = null;
                SketchPoint skp4 = null;

                PlanarSketch sketch = null;
                Profile profile = null;

                if (!isInteriorFace)
                {
                    sketch =
                        compDef.Sketches.AddWithOrientation(wpl,
                            xWa,
                            true,
                            true,
                            wpt,
                            false);

                    skp1 = sketch.SketchPoints.Add(
                        sketch.ModelToSketchSpace(p1), false);

                    skp2 = sketch.SketchPoints.Add(
                        sketch.ModelToSketchSpace(p2), false);

                    skp3 = sketch.SketchPoints.Add(
                        sketch.ModelToSketchSpace(p3), false);

                    skp4 = sketch.SketchPoints.Add(
                        sketch.ModelToSketchSpace(p4), false);

                    sketch.SketchLines.AddByTwoPoints(skp1, skp2);
                    sketch.SketchLines.AddByTwoPoints(skp2, skp3);
                    sketch.SketchLines.AddByTwoPoints(skp3, skp4);
                    sketch.SketchLines.AddByTwoPoints(skp4, skp1);

                    profile =
                        sketch.Profiles.AddForSolid(true, null, null);

                    RevolveFeature rev1 =
                     compDef.Features.RevolveFeatures.AddFull(
                      profile,
                      yWa,
                      PartFeatureOperationEnum.kCutOperation);
                }

              
                sketch = compDef.Sketches.AddWithOrientation(wpl, 
                    xWa, 
                    true, 
                    true, 
                    wpt, 
                    false);

                skp1 = sketch.SketchPoints.Add(
                    sketch.ModelToSketchSpace(p1), false);

                skp2 = sketch.SketchPoints.Add(
                    sketch.ModelToSketchSpace(p2), false);

                skp3 = sketch.SketchPoints.Add(
                    sketch.ModelToSketchSpace(p3), false);

                skp4 = sketch.SketchPoints.Add(
                    sketch.ModelToSketchSpace(p4), false);

                sketch.SketchLines.AddByTwoPoints(skp1, skp2);
                sketch.SketchLines.AddByTwoPoints(skp2, skp3);
                sketch.SketchLines.AddByTwoPoints(skp3, skp4);
                sketch.SketchLines.AddByTwoPoints(skp4, skp1);

                profile = sketch.Profiles.AddForSolid(true, 
                    null, null);

                RevolveFeature rev2 = 
                 compDef.Features.RevolveFeatures.AddFull(
                  profile,
                  yWa,
                  PartFeatureOperationEnum.kNewBodyOperation);

                return true;
            }
            catch (Exception ex)
            {
                return Fail("creating the coil body (revolve) failed: " +
                    ex.Message, ex);
            }
        }

        /////////////////////////////////////////////////////////////
        // Use: Creates new solid bodies affected by the future
        //      CoilFeature for Tapered Thread.
        /////////////////////////////////////////////////////////////
        private static bool CreateCoilBodyTapered(PartDocument doc,
            ThreadInfo threadInfo,
            Face threadedFace,
            double depth,
            bool isInteriorFace)
        {
            try
            {
                PartComponentDefinition compDef = 
                    doc.ComponentDefinition;

                Vector direction = threadInfo.ThreadDirection;

                Point basePoint =
                    threadInfo.ThreadBasePoints[1] as Point;

                Point endPoint = _Tg.CreatePoint(
                    basePoint.X + direction.X,
                    basePoint.Y + direction.Y,
                    basePoint.Z + direction.Z);

                UnitVector yAxis = direction.AsUnitVector();

                UnitVector xAxis = Toolkit.GetOrthoVector(yAxis);

                WorkPoint wpt = compDef.WorkPoints.AddFixed(basePoint, 
                    _ConstructionWorkFeature);

                WorkPlane wpl = compDef.WorkPlanes.AddFixed(basePoint, 
                    xAxis, yAxis, _ConstructionWorkFeature);

                WorkAxis xWa = compDef.WorkAxes.AddFixed(basePoint, 
                    xAxis, _ConstructionWorkFeature);

                WorkAxis yWa = compDef.WorkAxes.AddFixed(basePoint, 
                    yAxis, _ConstructionWorkFeature);

                PlanarSketch sketch = 
                    compDef.Sketches.AddWithOrientation(wpl, 
                        xWa, true, true, wpt, false);

                Cone cone = threadedFace.Geometry as Cone;

                double revDepth = 
                    depth / Math.Cos(cone.HalfAngle) * 
                        (isInteriorFace ? -1.0 : 1.0);

                Line l1 = Toolkit.GetFaceSideDirection(threadedFace, xAxis);

                Line l2 = _Tg.CreateLine(basePoint, xAxis.AsVector());

                Line l3 = _Tg.CreateLine(endPoint, xAxis.AsVector());

                Point p1 = l1.IntersectWithCurve(l2, 0.0001)[1] as Point;
                Point p2 = l1.IntersectWithCurve(l3, 0.0001)[1] as Point;

                Point p3 = _Tg.CreatePoint(
                    p2.X - xAxis.X * revDepth,
                    p2.Y - xAxis.Y * revDepth,
                    p2.Z - xAxis.Z * revDepth);

                Point p4 = _Tg.CreatePoint(
                    p1.X - xAxis.X * revDepth,
                    p1.Y - xAxis.Y * revDepth,
                    p1.Z - xAxis.Z * revDepth);

                SketchPoint skp1 = sketch.SketchPoints.Add(
                    sketch.ModelToSketchSpace(p1), false);

                SketchPoint skp2 = sketch.SketchPoints.Add(
                    sketch.ModelToSketchSpace(p2), false);

                SketchPoint skp3 = sketch.SketchPoints.Add(
                    sketch.ModelToSketchSpace(p3), false);

                SketchPoint skp4 = sketch.SketchPoints.Add(
                    sketch.ModelToSketchSpace(p4), false);

                sketch.SketchLines.AddByTwoPoints(skp1, skp2);
                sketch.SketchLines.AddByTwoPoints(skp2, skp3);
                sketch.SketchLines.AddByTwoPoints(skp3, skp4);
                sketch.SketchLines.AddByTwoPoints(skp4, skp1);

                Profile profile = sketch.Profiles.AddForSolid(true, 
                    null, null);

                RevolveFeature rev1 = 
                 compDef.Features.RevolveFeatures.AddFull(
                  profile,
                  yWa,
                  PartFeatureOperationEnum.kCutOperation);

                sketch = compDef.Sketches.AddWithOrientation(wpl, 
                    xWa, true, true, wpt, false);

                skp1 = sketch.SketchPoints.Add(
                    sketch.ModelToSketchSpace(p1), false);

                skp2 = sketch.SketchPoints.Add(
                    sketch.ModelToSketchSpace(p2), false);

                skp3 = sketch.SketchPoints.Add(
                    sketch.ModelToSketchSpace(p3), false);

                skp4 = sketch.SketchPoints.Add(
                    sketch.ModelToSketchSpace(p4), false);

                sketch.SketchLines.AddByTwoPoints(skp1, skp2);
                sketch.SketchLines.AddByTwoPoints(skp2, skp3);
                sketch.SketchLines.AddByTwoPoints(skp3, skp4);
                sketch.SketchLines.AddByTwoPoints(skp4, skp1);

                profile = sketch.Profiles.AddForSolid(true, null, null);

                RevolveFeature rev2 = 
                 compDef.Features.RevolveFeatures.AddFull(
                  profile,
                  yWa,
                  PartFeatureOperationEnum.kNewBodyOperation);

                return true;
            }
            catch (Exception ex)
            {
                return Fail("creating the coil body (revolve) failed: " +
                    ex.Message, ex);
            }
        }

        /////////////////////////////////////////////////////////////
        // Use: Creates CoilFeature that represents the modelized
        //      Thread, and cuts it out of the coil body (the last
        //      body, created by CreateCoilBody*).
        //      The coil is created as a new body and subtracted with
        //      a CombineFeature: a coil created as a cut is computed
        //      against every body of the part first, and Inventor
        //      fails on some parts (E_FAIL) when the thread profile
        //      touches the threaded face of the part's own body.
        //      The threaded coil body is then joined to
        //      threadedBody, the body that owns the threaded face.
        /////////////////////////////////////////////////////////////
        private static bool CreateCoilFeature(PartDocument doc,
            SurfaceBody threadedBody,
            Profile profile,
            Vector threadDirection,
            Point basePoint,
            bool rightHanded,
            double taper,
            double pitch,
            double extraPitch)
        {
            string step = "creating the coil";

            try
            {
                PartComponentDefinition compDef =
                    doc.ComponentDefinition;

                SurfaceBody coilBody =
                    compDef.SurfaceBodies[compDef.SurfaceBodies.Count];

                WorkAxis wa = compDef.WorkAxes.AddFixed(
                    basePoint, 
                    threadDirection.AsUnitVector(), 
                    _ConstructionWorkFeature);
            
                double height = threadDirection.Length + 2 * pitch;
            
                double coilPitch = pitch * (100 + extraPitch) * 0.01;
            
                CoilFeature coil =
                  compDef.Features.CoilFeatures.AddByPitchAndHeight(
                        profile,
                        wa,
                        coilPitch,
                        height,
                        PartFeatureOperationEnum.kNewBodyOperation,
                        false,
                        !rightHanded,
                        taper,
                        false,
                        0,
                        0,
                        false,
                        0,
                        0);

                HealthStatusEnum health = coil.HealthStatus;

                if (health != HealthStatusEnum.kUpToDateHealth)
                    return Fail("coil " + coil.Name + " is " + health +
                        " (pitch " + coilPitch + " cm, height " +
                        height + " cm).", null, true);

                step = "cutting the coil out of the thread body";

                ObjectCollection tools =
                 _Application.TransientObjects.
                    CreateObjectCollection(null);

                tools.Add(coil.SurfaceBodies[1]);

                CombineFeature cut =
                    compDef.Features.CombineFeatures.Add(
                        coilBody,
                        tools,
                        PartFeatureOperationEnum.kCutOperation,
                        false);

                health = cut.HealthStatus;

                if (health != HealthStatusEnum.kUpToDateHealth)
                    return Fail(cut.Name + " (coil cut) is " + health +
                        " (pitch " + coilPitch + " cm, height " +
                        height + " cm).", null, true);

                step = "joining the thread to " + threadedBody.Name;

                tools = _Application.TransientObjects.
                    CreateObjectCollection(null);

                tools.Add(cut.SurfaceBodies[1]);

                CombineFeature join =
                    compDef.Features.CombineFeatures.Add(
                        threadedBody,
                        tools,
                        PartFeatureOperationEnum.kJoinOperation,
                        false);

                health = join.HealthStatus;

                if (health != HealthStatusEnum.kUpToDateHealth)
                    return Fail(join.Name + " (join thread to " +
                        threadedBody.Name + ") is " + health + ".");

                return true;
            }
            catch (Exception ex)
            {
                return Fail(step + " failed: " + ex.Message,
                    ex, step == "creating the coil" ||
                        step.StartsWith("cutting"));
            }
        }

        /////////////////////////////////////////////////////////////
        // Use: Returns True if path argument contains same sketch
        //      entity than mainPath.
        /////////////////////////////////////////////////////////////
        private static bool HasMatchingEntity(ProfilePath path, 
            ProfilePath mainPath)
        {
            foreach (ProfileEntity profileEnt1 in path)
            {
                foreach (ProfileEntity profileEnt2 in mainPath)
                {
                    if (profileEnt1.SketchEntity == 
                        profileEnt2.SketchEntity)
                    {
                        if (profileEnt1.SketchEntity is SketchLine)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /////////////////////////////////////////////////////////////
        // Use: Returns Thread Pitch value as double (in cm).
        //
        /////////////////////////////////////////////////////////////
        public static double GetThreadPitch(ThreadInfo threadInfo)
        {
            bool metric = (bool)Toolkit.GetProperty(
                threadInfo,
                "Metric");

            double pitch = (double)Toolkit.GetProperty(
                threadInfo,
                "Pitch");

            return (metric ? pitch * 0.1 : pitch * 2.54);
        }

        /////////////////////////////////////////////////////////////
        // Use: Returns Thread Pitch as string using default units.
        //
        /////////////////////////////////////////////////////////////
        public static string GetThreadPitchStr(ThreadInfo threadInfo, 
            Document doc)
        {
            double pitch = ThreadWorker.GetThreadPitch(threadInfo);

            UnitsOfMeasure uom = doc.UnitsOfMeasure;

            return uom.GetStringFromValue(pitch, 
                UnitsTypeEnum.kDefaultDisplayLengthUnits);
        }

        /////////////////////////////////////////////////////////////
        // Use: Returns thread major radius for standard thread.
        //
        /////////////////////////////////////////////////////////////
        private static double GetThreadMajorRadiusStandard(
            Face threadedFace)
        {
            System.Object radius = Toolkit.GetProperty(
               threadedFace.Geometry, 
                "Radius");

            return (double)radius;
        }

        /////////////////////////////////////////////////////////////
        // Use: Returns thread major radius for tapered thread.
        //
        /////////////////////////////////////////////////////////////
        private static double GetThreadMajorRadiusTapered(
            ThreadInfo threadInfo,
            Face threadedFace)
        {
            UnitVector yAxis =
                threadInfo.ThreadDirection.AsUnitVector();

            UnitVector xAxis = Toolkit.GetOrthoVector(yAxis);

            Point basePoint =
                threadInfo.ThreadBasePoints[1] as Point;

            Line l1 = Toolkit.GetFaceSideDirection(
                threadedFace, xAxis);

            Line l2 = _Tg.CreateLine(basePoint, xAxis.AsVector());

            Point p1 = l1.IntersectWithCurve(l2, 0.0001)[1] as Point;

            return p1.DistanceTo(basePoint);
        }

        /////////////////////////////////////////////////////////////
        // Use: Returns thread type as string.
        //
        /////////////////////////////////////////////////////////////
        public static string GetThreadTypeStr(PartFeature feature)
        {
            if (feature.Type == ObjectTypeEnum.kHoleFeatureObject)
                return "Standard";

            if (feature.Type == ObjectTypeEnum.kThreadFeatureObject || feature.Type == ObjectTypeEnum.kThreadFeatureProxyObject)
            {
                ThreadFeature thread = feature as ThreadFeature;

                return (thread.ThreadInfoType ==
                    ThreadTypeEnum.kStandardThread ?
                        "Standard" : "Tapered");
            }

            return "Invalid Feature";
        }

        /////////////////////////////////////////////////////////////
        // Use: Return threaded face type as string.
        //
        /////////////////////////////////////////////////////////////
        public static string GetThreadedFaceTypeStr(
            Face threadedFace)
        {
            try
            {
                return (Toolkit.IsInteriorFace(threadedFace) ? 
                    "Interior" : "Exterior");
            }
            catch
            {
                return "Unknown";
            }
        }

        /////////////////////////////////////////////////////////////
        // Use: Returns direction of the thread side as Line object.
        //
        /////////////////////////////////////////////////////////////
        public static Line GetThreadSideDirection(ThreadInfo threadInfo, Face threadedFace)
        {
            Point RootPoint1 = threadInfo.ThreadBasePoints[1] as Point;
            Vector threadDirection = threadInfo.ThreadDirection;
            Point point = ThreadWorker._Tg.CreatePoint(RootPoint1.X + threadDirection.X, RootPoint1.Y + threadDirection.Y, RootPoint1.Z + threadDirection.Z);
            UnitVector orthoVector = Toolkit.GetOrthoVector(threadDirection.AsUnitVector());
            Line faceSideDirection = Toolkit.GetFaceSideDirection(threadedFace, orthoVector);
            Line line1 = ThreadWorker._Tg.CreateLine(RootPoint1, orthoVector.AsVector());
            Line line2 = ThreadWorker._Tg.CreateLine(point, orthoVector.AsVector());
            Point RootPoint2 = faceSideDirection.IntersectWithCurve((object)line1, 0.0001)[1] as Point;
            Point Point = faceSideDirection.IntersectWithCurve((object)line2, 0.0001)[1] as Point;
            return ThreadWorker._Tg.CreateLine(RootPoint2, RootPoint2.VectorTo(Point));
        }

        /////////////////////////////////////////////////////////////
        // Use: Returns True if conical thread is expanding.
        //      Works only for tapered threads.
        /////////////////////////////////////////////////////////////
        public static bool IsExpanding(
            ThreadInfo threadInfo,
            Face threadedFace)
        {
            Point basePoint = 
                threadInfo.ThreadBasePoints[1] as Point;

            Vector direction = threadInfo.ThreadDirection;

            Point endPoint = _Tg.CreatePoint(
                   basePoint.X + direction.X,
                   basePoint.Y + direction.Y,
                   basePoint.Z + direction.Z);

            UnitVector yAxis = direction.AsUnitVector();

            UnitVector xAxis = Toolkit.GetOrthoVector(yAxis);
            
            Line l1 = Toolkit.GetFaceSideDirection(threadedFace, xAxis);

            Line l2 = _Tg.CreateLine(basePoint, xAxis.AsVector());

            Line l3 = _Tg.CreateLine(endPoint, xAxis.AsVector());

            Point p1 = l1.IntersectWithCurve(l2, 0.0001)[1] as Point;
            Point p2 = l1.IntersectWithCurve(l3, 0.0001)[1] as Point;

            double dBase = p1.DistanceTo(basePoint);

            double dEnd = p2.DistanceTo(endPoint);

            return (dBase < dEnd);
        }
    }
}