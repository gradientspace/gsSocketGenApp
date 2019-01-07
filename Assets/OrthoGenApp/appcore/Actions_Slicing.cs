using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using g3;
using f3;
using gs;
using gs.info;   // for print settings
using gsbody;

namespace orthogen
{
    /// <summary>
    /// Actions for Socket step of Orthogen workflow
    /// </summary>
    public static partial class OGActions
    {

        public static bool CanExportGCode()
        {
            return OG.IsInState(OGWorkflow.SocketState) && OG.Model.HasDevice();
        }

        public static void ExportGCodeInteractive(Action OnCompletedF = null, Action OnErrorF = null)
        {
            if (CanExportGCode() == false)
                return;

            if (ShowExportDialogInEditor || FPlatform.InUnityEditor() == false) {
                FPlatform.GetSaveFileName_Async("Export GCode",
                    Path.Combine(ExportSocketPath, "socket.gcode"), new string[] { "*.gcode" }, "GCode Files (*.gcode)",
                    (filename) => { ExportGCode(filename, OnCompletedF, OnErrorF); });
            } else {
                string filename = Path.Combine(ExportSocketPath, "socket.gcode");
                ExportGCode(filename, OnCompletedF, OnErrorF);
            }

        }

        public static void ExportGCode(string sPath, Action OnCompletedF = null, Action OnErrorF = null)
        {
            if (OG.Model.HasDevice() == false)
                return;

            DMesh3 SocketMesh = new DMesh3(OG.Socket.Socket.Mesh);

            // run in background thread
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    GCodeFile gcode = background_slicing(SocketMesh);

                    DebugUtil.Log("[gsSlicer] writing gcode... ");

                    StandardGCodeWriter writer = new StandardGCodeWriter();
                    using (StreamWriter w = new StreamWriter(sPath)) {
                        writer.WriteFile(gcode, w);
                    }

                    DebugUtil.Log("[gsSlicer] done!");

                } catch (Exception e) {
                    DebugUtil.Log("[gsSlicer] Exception! " + e.Message);
                    if (OnErrorF != null)
                        ThreadMailbox.PostToMainThread(OnErrorF);
                    return;
                }


                if ( OnCompletedF != null )
                    ThreadMailbox.PostToMainThread(OnCompletedF);
            });

        }



        static GCodeFile background_slicing(DMesh3 mesh)
        {
            // [RMS] speeds up testing...
            //MeshTransforms.Scale(mesh, 0.5);

            MeshTransforms.FlipLeftRightCoordSystems(mesh);   // convert from unity coordinate system
            MeshTransforms.ConvertYUpToZUp(mesh);

            AxisAlignedBox3d bounds = mesh.CachedBounds;
            Vector3d baseCenterPt = bounds.Center - bounds.Depth * 0.5 * Vector3d.AxisZ;
            MeshTransforms.Translate(mesh, -baseCenterPt);

            PrintMeshAssembly meshes = new PrintMeshAssembly();
            meshes.AddMesh(mesh);

            PrintrbotSettings settings = new PrintrbotSettings(Printrbot.Models.Plus);
            settings.LayerHeightMM = 0.2;
            settings.Machine.NozzleDiamMM = 0.4;
            settings.Shells = 2;
            settings.InteriorSolidRegionShells = 0;
            settings.SparseLinearInfillStepX = 5;
            settings.ClipSelfOverlaps = true;
            settings.GenerateSupport = true;

            DebugUtil.Log("[gsSlicer] computing slice stack... ");

            // do slicing
            MeshPlanarSlicer slicer = new MeshPlanarSlicer() {
                LayerHeightMM = settings.LayerHeightMM
            };
            slicer.Add(meshes);
            PlanarSliceStack slices = slicer.Compute();

            DebugUtil.Log("[gsSlicer] generating paths for {0} slices... ", slices.Count);

            // run print generator
            SingleMaterialFFFPrintGenerator printGen =
                new SingleMaterialFFFPrintGenerator(meshes, slices, settings);
            // this helps for support but not sure it will work on printers?
            printGen.LayerPostProcessor = new SupportConnectionPostProcessor() { ZOffsetMM = 0.2f };
            printGen.AccumulatePathSet = false;    // could get gcode paths back using this...

            printGen.Generate();
            return printGen.Result;
        }


    }
}
