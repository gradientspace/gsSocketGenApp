﻿using System;
using System.Collections.Generic;
using System.Linq;
using g3;
using f3;
using gs;
using gsbody;

namespace orthogen
{

    /// <summary>
    /// Actions for Model step of Orthogen workflow
    /// </summary>
    public static partial class OGActions
    {


        public static void InitializeLegFromScan()
        {
            OG.Scan.Hide();

            LegSO legSO = new LegSO();
            legSO.Create( new DMesh3(OG.Scan.OutputMesh), OG.Scene.DefaultMeshSOMaterial);
            OG.Scene.AddSceneObject(legSO, false);
            Frame3f f = OG.Scan.SO.GetLocalFrame(CoordSpace.ObjectCoords);
            legSO.SetLocalFrame(f, CoordSpace.ObjectCoords);

            OG.Model.InitializeLeg(legSO);

            legSO.AssignSOMaterial(OrthogenMaterials.LegMaterial);
            legSO.SetLayer(FPlatform.WidgetOverlayLayer);
        }



        //public static LegModel.LegDeformationTypes CurrentLegDeformType = LegModel.LegDeformationTypes.Offset;
        public static LegModel.LegDeformationTypes CurrentLegDeformType = LegModel.LegDeformationTypes.Smooth;




        /// <summary>
        /// this is set as DrawSurfaceCurveTool.EmitNewCurveF, which is called automatically when user
        /// releases mouse, so we need to exit the tool here.
        /// </summary>
        public static void EmitRegionDeformationFromCurvePreview(CurvePreview preview)
        {
            if (preview.Closed == false) {
                preview.Closed = true;
                DrawSurfaceCurveTool tool = OG.Context.ToolManager.ActiveRightTool as DrawSurfaceCurveTool;
                if (tool != null)
                    tool.Closed = true;
            }
            EnclosedPatchSO curveSO = EnclosedPatchSO.CreateFromPreview(preview, OrthogenMaterials.OffsetRegionMaterial, OG.Scene);
            curveSO.Name = (CurrentLegDeformType == LegModel.LegDeformationTypes.Offset) ?
                "OffsetRegion" : "SmoothRegion";
            AddNewRegionDeformation(curveSO, CurrentLegDeformType);

            // [RMS] this is called at end
            OG.Context.RegisterNextFrameAction(() => {
                OG.Transition(OGWorkflow.DrawAreaExitT);
                OG.Scene.Select(curveSO, true);
            });
        }


        /// <summary>
        /// add a region-deformation to the datamodel
        /// </summary>
        public static ModelingOperator AddNewRegionDeformation(EnclosedPatchSO curveSO, LegModel.LegDeformationTypes deformType)
        {
            curveSO.AssignSOMaterial(OrthogenMaterials.OffsetRegionMaterial);
            curveSO.ConnectToTarget(OG.Leg, OG.Leg.SO, true);
            curveSO.EnableRegionOverlay = false;
            curveSO.RootGameObject.SetLayer(OrthogenMaterials.CurvesLayer);

            var deformOp = OG.Leg.AppendRegionOp(curveSO, deformType);

            // add link so that curve moves with leg
            SOFrameLink link = new SOFrameLink(curveSO, OG.Leg.SO);
            OG.Scene.LinkManager.AddLink(link);

            // create index op that listens to curveSO.on_curve_modified
            OG.Model.RegisterDeleteSOAction(curveSO, MakeDeleteLegDeformationAction(deformOp));
            OG.Model.RegisterSelectSOAction(curveSO, MakeSelectLegDeformationAction(deformOp));

            return deformOp;
        }



        /*
         * These are the actions we use for the Draw-Area tool (ie draw closed loop to select region)
         */
        public static bool CanDrawArea()
        {
            var M = OG.Model;
            return M.Workflow.IsInState(RectifyState.Identifier) && M.Context.ToolManager.ActiveRightTool == null;
        }
        public static void BeginDrawAreaTool()
        {
            var M = OG.Model;

            M.Context.ToolManager.DeactivateTool(ToolSide.Right);
            M.Scene.ClearSelection();
            M.Context.ToolManager.SetActiveToolType(BodyModelTools.DrawSurfaceCurveRegionIdentifier, ToolSide.Right);
            M.Scene.Select(OG.Leg.SO, true);
            M.Context.ToolManager.ActivateTool(ToolSide.Right);

            DrawSurfaceCurveTool tool = M.Context.ToolManager.ActiveRightTool as DrawSurfaceCurveTool;
            var target = new SOProjectionTarget(OG.Leg.SO, CoordSpace.WorldCoords) { Offset = M.Scene.ToWorldDimension(0.5f) };
            tool.CurveProcessorF = gs.CurveDrawingUtil.MakeLoopOnSurfaceProcessorF(M.Scene, target,
                () => { return tool.SamplingRateScene; }, 
                () => { return tool.Closed; },
                0.25f, 10 );
        }
        public static void EndDrawAreaTool()
        {
            var M = OG.Model;
            M.Context.ToolManager.DeactivateTools();
            M.Scene.ClearSelection();
        }







        /// <summary>
        /// adds an expand/contract-type plane/leg intersection thing at the given frame 
        /// </summary>
        public static void EmitPlaneBandExpansionFromTool(SceneObject targetLegSO, Frame3f planeFrameS)
        { 
            PlaneIntersectionCurveSO curveSO = PlaneIntersectionCurveSO.CreateFromPlane( 
                targetLegSO as DMeshSO, planeFrameS,  OrthogenMaterials.PlaneCurveMaterial, 
                targetLegSO.GetScene(), OrthogenUI.CurveOnSurfaceOffsetTol);
            curveSO.Name = "PlaneBandExpansion";

            AddNewPlaneBandExpansion(curveSO);

            // [RMS] this is called at end
            OG.Context.RegisterNextFrameAction(() => {
                OG.Scene.Select(curveSO, true);
            });
        }


        /// <summary>
        /// adds an expand/contract-type plane/leg intersection thing to the datamodel
        /// </summary>
        public static ModelingOperator AddNewPlaneBandExpansion(PlaneIntersectionCurveSO curveSO)
        {
            curveSO.AssignSOMaterial(OrthogenMaterials.PlaneCurveMaterial);
            curveSO.RootGameObject.SetLayer(OrthogenMaterials.CurvesLayer);

            // create and associate deformation op
            var deformOp = OG.Leg.AppendPlaneBandExpansion(curveSO);

            // add link so that curve moves with leg
            SOFrameLink link = new SOFrameLink(curveSO, OG.Leg.SO);
            OG.Scene.LinkManager.AddLink(link);

            OG.Model.RegisterDeleteSOAction(curveSO, MakeDeleteLegDeformationAction(deformOp));
            OG.Model.RegisterSelectSOAction(curveSO, MakeSelectLegDeformationAction(deformOp));

            return deformOp;
        }




        /*
         * These are the actions we use for the deformation-ring tool
         */
        public static bool CanAddDeformRing()
        {
            var M = OG.Model;
            return M.Workflow.IsInState(RectifyState.Identifier) && M.Context.ToolManager.ActiveRightTool == null;
        }
        public static void BeginDeformRingTool()
        {
            var M = OG.Model;

            M.Context.ToolManager.DeactivateTool(ToolSide.Right);
            M.Scene.ClearSelection();
            //M.Context.ToolManager.SetActiveToolType(BodyModelTools.CreateInOutPlaneTool, ToolSide.Right);
            M.Context.ToolManager.SetActiveToolType(BodyModelTools.CreateOffsetBandTool, ToolSide.Right);
            M.Scene.Select(OG.Leg.SO, true);
            M.Context.ToolManager.ActivateTool(ToolSide.Right);

            TwoPointBandTool tool = M.Context.ToolManager.ActiveRightTool as TwoPointBandTool;
            tool.InitializeOnTarget(OG.Leg.SO, 10.0f);
        }
        public static bool CanAcceptDeformRingTool()
        {
            var M = OG.Model;
            return (M.Context.ToolManager.ActiveRightTool != null) && M.Context.ToolManager.ActiveRightTool.CanApply;
        }
        public static void AcceptDeformRingTool()
        {
            var M = OG.Model;
            M.Context.ToolManager.ActiveRightTool.Apply();
            M.Context.ToolManager.DeactivateTools();
            M.Scene.ClearSelection();
        }
        public static void CancelDeformRingTool()
        {
            var M = OG.Model;
            M.Context.ToolManager.DeactivateTools();
            M.Scene.ClearSelection();
        }










        /// <summary>
        /// adds an expand/contract-type plane/leg intersection thing at the given frame 
        /// </summary>
        public static void EmitOffsetBandFromTool(SceneObject targetLegSO, Frame3f startFrameS, Frame3f endFrameS)
        {
            Frame3f midFrame = new Frame3f((startFrameS.Origin + endFrameS.Origin) * 0.5f);
            PlaneIntersectionCurveSO curveSO = PlaneIntersectionCurveSO.CreateFromPlane(
                targetLegSO as DMeshSO, midFrame, OrthogenMaterials.PlaneCurveMaterial,
                targetLegSO.GetScene(), OrthogenUI.CurveOnSurfaceOffsetTol);
            curveSO.Name = "PlaneBandExpansion";
            curveSO.RootGameObject.SetLayer(OrthogenMaterials.CurvesLayer);

            // create and associate deformation op
            PlaneBandExpansionOp deformOp = OG.Leg.AppendPlaneBandExpansion(curveSO) as PlaneBandExpansionOp;
            deformOp.BandDistance = startFrameS.Origin.Distance(endFrameS.Origin) * 0.5;
            

            OG.Model.RegisterDeleteSOAction(curveSO, MakeDeleteLegDeformationAction(deformOp));
            OG.Model.RegisterSelectSOAction(curveSO, MakeSelectLegDeformationAction(deformOp));

            // [RMS] this is called at end
            OG.Context.RegisterNextFrameAction(() => {
                OG.Scene.Select(curveSO, true);
            });
        }












        /*
         * These are the actions we use for the leg-squeeze tool
         */
        public static bool CanAddLegSqueeze()
        {
            var M = OG.Model;
            return M.Workflow.IsInState(RectifyState.Identifier) && M.Context.ToolManager.ActiveRightTool == null;
        }
        public static void BeginLegSqueezeTool()
        {
            var M = OG.Model;

            M.Context.ToolManager.DeactivateTool(ToolSide.Right);
            M.Scene.ClearSelection();
            M.Context.ToolManager.SetActiveToolType(BodyModelTools.CreateLegSqueezeTool, ToolSide.Right);
            M.Scene.Select(OG.Leg.SO, true);
            M.Context.ToolManager.ActivateTool(ToolSide.Right);

            TwoPointBandTool tool = M.Context.ToolManager.ActiveRightTool as TwoPointBandTool;
            tool.InitializeOnTarget(OG.Leg.SO, 10.0f);
        }
        public static bool CanAcceptLegSqueezeTool()
        {
            var M = OG.Model;
            return (M.Context.ToolManager.ActiveRightTool != null) && M.Context.ToolManager.ActiveRightTool.CanApply;
        }
        public static void AcceptLegSqueezeTool()
        {
            var M = OG.Model;
            M.Context.ToolManager.ActiveRightTool.Apply();
            M.Context.ToolManager.DeactivateTools();
            M.Scene.ClearSelection();
        }
        public static void CancelLegSqueezeTool()
        {
            var M = OG.Model;
            M.Context.ToolManager.DeactivateTools();
            M.Scene.ClearSelection();
        }





        /// <summary>
        /// adds an expand/contract-type plane/leg intersection thing at the given frame 
        /// </summary>
        public static void EmitLegSqueezeFromTool(SceneObject targetLegSO, Frame3f startFrameS, Frame3f endFrameS)
        {
            if ( startFrameS.Origin.y > endFrameS.Origin.y ) {
                Frame3f tmp = startFrameS; startFrameS = endFrameS; endFrameS = tmp;
            }
            Frame3f planeCutFrameS = endFrameS;
            planeCutFrameS.Rotation = Quaternionf.Identity;
            PlaneIntersectionCurveSO curveSO = PlaneIntersectionCurveSO.CreateFromPlane(
                targetLegSO as DMeshSO, planeCutFrameS, OrthogenMaterials.PlaneCurveMaterial,
                targetLegSO.GetScene(), OrthogenUI.CurveOnSurfaceOffsetTol);
            curveSO.Name = "LegSqueezeTop";

            Vector3f bottomPointL = SceneTransforms.SceneToObject(targetLegSO, startFrameS).Origin;
            Vector3f topPointL = SceneTransforms.SceneToObject(targetLegSO, endFrameS).Origin;

            LegSqueezeOp op = AddNewLegSqueeze(topPointL, curveSO) as LegSqueezeOp;

            //List<Vector2d> midpoints = new List<Vector2d>() {
            //    new Vector2d(0.25f, 5.0f),
            //    new Vector2d(0.5f, 0.0f),
            //    new Vector2d(0.75f, 30.0f)
            //};
            //op.UpdateMidpoints(midpoints);
            //op.SetMidPoint(2, midpoints[2].x, midpoints[2].y);
            //op.SetMidPoint(1, midpoints[1].x, midpoints[1].y);
            //op.SetMidPoint(0, midpoints[0].x, midpoints[0].y);

            // [RMS] this is called at end
            OG.Context.RegisterNextFrameAction(() => {
                OG.Scene.Select(curveSO, true);
            });
        }


        /// <summary>
        /// adds leg squeeze thing
        /// </summary>
        public static ModelingOperator AddNewLegSqueeze(Vector3f topPointL, PlaneIntersectionCurveSO curveSO)
        {
            curveSO.AssignSOMaterial(OrthogenMaterials.PlaneCurveMaterial);
            curveSO.RootGameObject.SetLayer(OrthogenMaterials.CurvesLayer);

            curveSO.Name = "LegSqueezeSO";

            // create and associate deformation op
            LegSqueezeOp deformOp = OG.Leg.AppendLegSqueezeOp(topPointL, curveSO) as LegSqueezeOp;

            // add link so that curve moves with leg
            SOFrameLink link = new SOFrameLink(curveSO, OG.Leg.SO);
            OG.Scene.LinkManager.AddLink(link);

            OG.Model.RegisterDeleteSOAction(curveSO, MakeDeleteLegDeformationAction(deformOp));
            OG.Model.RegisterSelectSOAction(curveSO, MakeSelectLegDeformationAction(deformOp));

            return deformOp;
        }










        public static bool CanAddLengthenOp()
        {
            return OG.IsInState(OGWorkflow.RectifyState)
                && OG.Leg.HasLengthenOp() == false;
        }


        /// <summary>
        /// adds an expand/contract-type plane/leg intersection thing at the given frame 
        /// </summary>
        public static void AddLengthenOp()
        {
            if (OG.IsInState(OGWorkflow.RectifyState) == false)
                return;

            LengthenPivotSO pivotSO = new LengthenPivotSO();
            pivotSO.Create(OG.Scene.PivotSOMaterial, null);
            OG.Scene.AddSceneObject(pivotSO, false);
            pivotSO.Name = "LengthenPivot";

            // we put the lengthen pivot at min vertex of leg
            DMesh3 mesh = OG.Leg.SO.Mesh;
            Vector3d minYPt = Vector3d.Zero;
            foreach (Vector3d v in mesh.Vertices()) {
                if (v.y < minYPt.y)
                    minYPt = v;
            }
            Vector3d basePtS = SceneTransforms.ObjectToSceneP(OG.Leg.SO, minYPt);
            Frame3f pivotF = new Frame3f(basePtS);
            pivotSO.SetLocalFrame(pivotF, CoordSpace.SceneCoords);
            pivotSO.InitialLegPtL = minYPt;

            AddNewLengthenOp(pivotSO);

            // select pivot next frame
            OG.Context.RegisterNextFrameAction(() => {
                OG.Scene.Select(pivotSO, true);
            });
        }


        public static LengthenOp AddNewLengthenOp(LengthenPivotSO pivotSO)
        {
            //pivotSO.AssignSOMaterial(OrthogenMaterials.OffsetRegionMaterial);
            pivotSO.RootGameObject.SetLayer(OrthogenMaterials.CurvesLayer);

            // add link so that pivot moves with leg
            SOFrameLink link = new SOFrameLink(pivotSO, OG.Leg.SO);
            OG.Scene.LinkManager.AddLink(link);

            var deformOp = OG.Leg.AppendLengthenOp(pivotSO) as LengthenOp;
            deformOp.LengthenDistance = 50.0f;
            deformOp.LengthenDistance = 0;

            pivotSO.OnTransformModified += (so) => {
                Vector3f curPosS = so.GetLocalFrame(CoordSpace.SceneCoords).Origin;
                Vector3f inLegPosL = SceneTransforms.SceneToObjectP(OG.Leg.SO, curPosS);
                double dt = (so as LengthenPivotSO).InitialLegPtL.y - inLegPosL.y;

                //Frame3f newF = so.GetLocalFrame(CoordSpace.SceneCoords);
                //Vector3f dv = newF.Origin - pivotSO.OriginalFrameS.Origin;
                //double dt = -dv.y;
                deformOp.LengthenDistance = dt;
            };

            OG.Model.RegisterDeleteSOAction(pivotSO, MakeDeleteLegDeformationAction(deformOp));
            OG.Model.RegisterSelectSOAction(pivotSO, () => {
                OG.Model.SetActiveModelingOperator(deformOp);
                OG.Context.TransformManager.SetActiveGizmoType(OGActions.LenthenMovePivotGizmoType);
            });
            OG.Model.RegisterDeselectSOAction(pivotSO, () => {
                OG.Model.SetActiveModelingOperator(null);
                OG.Context.TransformManager.SetActiveGizmoType(TransformManager.NoGizmoType);
            });

            return deformOp;
        }





        public static bool CanSculptRegion()
        {
            if (OG.IsInState(OGWorkflow.RectifyState) == false || OG.Model.Scene.Selected.Count != 1)
                return false;
            ModelingOperator op = OG.Leg.FindOpForSO( OG.Model.Scene.Selected[0] );
            return (op is ParametricVectorDisplacementMapOp);
        }
        public static void BeginSculptRegion()
        {
            var M = OG.Model;

            var vecOp = (OG.Model.Scene.Selected.Count != 1) ? null :
                OG.Leg.FindOpForSO(OG.Model.Scene.Selected[0]) as ParametricVectorDisplacementMapOp;
            if (vecOp == null)
                throw new Exception("OGActions.BeginSculptRegion: no sculptable map for selected region!");

            // have to select leg to sculpt on
            // [TODO] restore selection afterwards?
            OG.Scene.Select(OG.Leg.SO, true);

            M.Context.ToolManager.DeactivateTool(ToolSide.Right);
            M.Context.ToolManager.SetActiveToolType(BodyModelTools.BrushDisplacementTool, ToolSide.Right);
            M.Context.ToolManager.ActivateTool(ToolSide.Right);
            BrushDisplacementTool tool = M.Context.ToolManager.ActiveRightTool as BrushDisplacementTool;
            if (tool != null) {
                vecOp.GenerateMap = false;
                tool.DisplacementOp = vecOp;
            }
        }
        public static void EndSculptRegion()
        {
            OG.Model.Context.ToolManager.DeactivateTools();
        }





        public static bool CanSculptGlobalMap()
        {
            return OG.IsInState(OGWorkflow.RectifyState);
        }
        public static void BeginSculptGlobalMap()
        {
            var M = OG.Model;

            M.Context.ToolManager.DeactivateTool(ToolSide.Right);

            OG.Scene.Select(OG.Leg.SO, true);

            M.Context.ToolManager.SetActiveToolType(BodyModelTools.BrushDisplacementTool, ToolSide.Right);
            M.Context.ToolManager.ActivateTool(ToolSide.Right);

            BrushDisplacementTool tool = M.Context.ToolManager.ActiveRightTool as BrushDisplacementTool;
            if (tool != null)
                tool.DisplacementOp = OG.Leg.GetBrushLayer();
        }
        public static void EndSculptGlobalMap()
        {
            OG.Model.Context.ToolManager.DeactivateTools();
        }





        public static Action MakeDeleteLegDeformationAction(IVectorDisplacementSourceOp op) {
            return () => {
                OG.Leg.RemoveDeformationOp(op);
            };
        }

        public static Action MakeSelectLegDeformationAction(IVectorDisplacementSourceOp op) {
            return () => {
                OG.Model.SetActiveModelingOperator(op);
            };
        }






    }
}
