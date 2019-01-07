using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using f3;
using g3;
using gs;
using gsbody;

namespace orthogen
{

    class SetupOrthoGenCockpit : ICockpitInitializer
    {
        public void Initialize(Cockpit cockpit)
        {
            cockpit.Name = "modelCockpit";
            

            // Configure how the cockpit moves

            cockpit.PositionMode = Cockpit.MovementMode.TrackPosition;
            // [RMS] use orientation mode to make cockpit follow view orientation.
            //  (however default widgets below are off-screen!)
            //cockpit.PositionMode = Cockpit.MovementMode.TrackOrientation;



            FScene Scene = cockpit.Scene;
            BoxContainer screenContainer = new BoxContainer(new Cockpit2DContainerProvider(cockpit));
            PinnedBoxes2DLayoutSolver screenLayout = new PinnedBoxes2DLayoutSolver(screenContainer);
            PinnedBoxesLayout layout = new PinnedBoxesLayout(cockpit, screenLayout) {
                StandardDepth = 1.5f
            };
            cockpit.AddLayout(layout, "2D", true);


            Func<string, float, HUDLabel> MakeButtonF = (label, buttonW) => {
                HUDLabel button = new HUDLabel() {
                    Shape = OrthogenUI.MakeMenuButtonRect(buttonW, OrthogenUI.MenuButtonHeight),
                    TextHeight = OrthogenUI.MenuButtonTextHeight,
                    AlignmentHorz = HorizontalAlignment.Center,
                    BackgroundColor = OrthogenUI.ButtonBGColor, 
                    TextColor = OrthogenUI.ButtonTextColor,
                    DisabledTextColor = OrthogenUI.DisabledButtonTextColor,
                    Text = label,
                    EnableBorder = true, BorderWidth = OrthogenUI.StandardButtonBorderWidth, BorderColor = OrthogenUI.ButtonTextColor
                };
                button.Create();
                button.Name = label;
                button.Enabled = true;
                return button;
            };
            Func<string, float, float, HUDSpacer> MakeSpacerF = (label, spacerw, spacerh) => {
                HUDSpacer spacer = new HUDSpacer() {
                    Shape = new HUDShape(HUDShapeType.Rectangle, spacerw, spacerh)
                };
                spacer.Create();
                spacer.Name = label;
                return spacer;
            };


            HUDElementList button_list = new HUDElementList() {
                Width = OrthogenUI.MenuButtonWidth,
                Height = 5*OrthogenUI.MenuButtonHeight,
				Spacing = 5*OrthogenUI.PixelScale,
				Direction = HUDElementList.ListDirection.Vertical
            };



            HUDLabel trim_scan_button = MakeButtonF("Trim Scan", OrthogenUI.MenuButtonWidth);
            trim_scan_button.OnClicked += (sender, e) => {
                OG.Transition(OGWorkflow.TrimScanStartT);
            };
            button_list.AddListItem(trim_scan_button);

            HUDLabel align_scan_button = MakeButtonF("Align Scan", OrthogenUI.MenuButtonWidth);
            align_scan_button.OnClicked += (sender, e) => {
                OG.Transition(OGWorkflow.AlignScanStartT);
            };
            button_list.AddListItem(align_scan_button);

            HUDLabel bend_scan_button = MakeButtonF("Bend Scan", OrthogenUI.MenuButtonWidth);
            bend_scan_button.OnClicked += (sender, e) => {
                OG.Transition(OGWorkflow.BendScanStartT);
            };
            button_list.AddListItem(bend_scan_button);

            HUDLabel sculpt_scan_button = MakeButtonF("Smooth Scan", OrthogenUI.MenuButtonWidth);
            sculpt_scan_button.OnClicked += (sender, e) => {
                OG.Transition(OGWorkflow.ScanSmoothBrushStartT);
            };
            button_list.AddListItem(sculpt_scan_button);


            HUDLabel accept_scan_button = MakeButtonF("Done Scan", OrthogenUI.MenuButtonWidth);
            accept_scan_button.OnClicked += (sender, e) => {
                OG.TransitionToState(RectifyState.Identifier);
            };
            button_list.AddListItem(accept_scan_button);



            button_list.AddListItem(MakeSpacerF("space", OrthogenUI.MenuButtonWidth, 0.5f * OrthogenUI.MenuButtonHeight));


            HUDLabel draw_offset_area_button = MakeButtonF("Offset Area", OrthogenUI.MenuButtonWidth);
			draw_offset_area_button.OnClicked += (sender, e) => {
                OGActions.CurrentLegDeformType = LegModel.LegDeformationTypes.Offset;
                OG.Transition(OGWorkflow.DrawAreaStartT);
            };
			button_list.AddListItem(draw_offset_area_button);

            HUDLabel draw_smooth_area_button = MakeButtonF("Smooth Area", OrthogenUI.MenuButtonWidth);
            draw_smooth_area_button.OnClicked += (sender, e) => {
                OGActions.CurrentLegDeformType = LegModel.LegDeformationTypes.Smooth;
                OG.Transition(OGWorkflow.DrawAreaStartT);
            };
            button_list.AddListItem(draw_smooth_area_button);


            HUDLabel add_plane_button = MakeButtonF("Add Squeeze", OrthogenUI.MenuButtonWidth);
            add_plane_button.OnClicked += (sender, e) => {
                OG.Transition(OGWorkflow.AddLegSqueezeStartT);
            };
            button_list.AddListItem(add_plane_button);

            HUDLabel add_lengthen_button = MakeButtonF("Add Lengthen", OrthogenUI.MenuButtonWidth);
            add_lengthen_button.OnClicked += (sender, e) => {
                if (OGActions.CanAddLengthenOp())
                    OGActions.AddLengthenOp();
            };
            button_list.AddListItem(add_lengthen_button);

            HUDLabel sculpt_region_button = MakeButtonF("Sculpt Region", OrthogenUI.MenuButtonWidth);
            sculpt_region_button.OnClicked += (sender, e) => {
                OG.Transition(OGWorkflow.SculptRegionStartT);
            };
            button_list.AddListItem(sculpt_region_button);

            HUDLabel sculpt_curve_button = MakeButtonF("Edit Curve", OrthogenUI.MenuButtonWidth);
            WorkflowRouter sculpt_router = WorkflowRouter.Build(new[] {
                OGWorkflow.RectifyState, OGWorkflow.SculptAreaStartT,
                OGWorkflow.SocketState, OGWorkflow.SculptTrimlineStartT });
            sculpt_curve_button.OnClicked += (sender, e) => {
                sculpt_router.Apply(OG.Model.Workflow);
			};
			button_list.AddListItem(sculpt_curve_button);

            HUDLabel sculpt_layer_button = MakeButtonF("Sculpt Global", OrthogenUI.MenuButtonWidth);
            sculpt_layer_button.OnClicked += (sender, e) => {
                OG.Transition(OGWorkflow.SculptGlobalMapStartT);
            };
            button_list.AddListItem(sculpt_layer_button);

            HUDLabel accept_rectify_button = MakeButtonF("Begin Device", OrthogenUI.MenuButtonWidth);
            accept_rectify_button.OnClicked += (sender, e) => {
                OG.Leg.SetOpWidgetVisibility(false);
                OG.TransitionToState(SocketDesignState.Identifier);
            };
            button_list.AddListItem(accept_rectify_button);



            button_list.AddListItem(MakeSpacerF("space", OrthogenUI.MenuButtonWidth, 0.5f * OrthogenUI.MenuButtonHeight));



            HUDLabel draw_trim_line_button = MakeButtonF("Draw Trimline", OrthogenUI.MenuButtonWidth);
            draw_trim_line_button.OnClicked += (sender, e) => {
                OG.Transition(OGWorkflow.DrawTrimlineStartT);
            };
            button_list.AddListItem(draw_trim_line_button);

            HUDLabel plane_trim_line_button = MakeButtonF("Plane Trimline", OrthogenUI.MenuButtonWidth);
            plane_trim_line_button.OnClicked += (sender, e) => {
                OG.Transition(OGWorkflow.PlaneTrimlineStartT);
            };
            button_list.AddListItem(plane_trim_line_button);

            HUDLabel add_socket_button = MakeButtonF("Add Socket", OrthogenUI.MenuButtonWidth);
            add_socket_button.OnClicked += (sender, e) => {
                if ( OGActions.CanAddDevice() )
                    OGActions.AddDevice(SocketModel.ModelModes.Socket);
            };
            button_list.AddListItem(add_socket_button);

            HUDLabel add_afo_button = MakeButtonF("Add AFO", OrthogenUI.MenuButtonWidth);
            add_afo_button.OnClicked += (sender, e) => {
                if (OGActions.CanAddDevice())
                    OGActions.AddDevice(SocketModel.ModelModes.AFO);
            };
            button_list.AddListItem(add_afo_button);

            HUDLabel export_socket_button = MakeButtonF("Export Mesh", OrthogenUI.MenuButtonWidth);
            export_socket_button.OnClicked += (sender, e) => {
                if (OGActions.CanExportSocket())
                    OGActions.ExportSocketInteractive();
            };
            button_list.AddListItem(export_socket_button);

            HUDLabel export_gcode_button = MakeButtonF("Export GCode", OrthogenUI.MenuButtonWidth);
            export_gcode_button.OnClicked += (sender, e) => {
                if (OGActions.CanExportGCode())
                    OGActions.ExportGCodeInteractive();
            };
            button_list.AddListItem(export_gcode_button);




            button_list.AddListItem(MakeSpacerF("space", OrthogenUI.MenuButtonWidth, 1.0f * OrthogenUI.MenuButtonHeight));


            HUDLabel accept_button = MakeButtonF("Accept", OrthogenUI.MenuButtonWidth);
            accept_button.OnClicked += (sender, e) => {
                OGActions.AcceptCurrentTool();
            };


            HUDLabel cancel_button = MakeButtonF("Cancel", OrthogenUI.MenuButtonWidth);
            cancel_button.OnClicked += (sender, e) => {
                OGActions.CancelCurrentTool();
            };
            button_list.AddListItem(accept_button);
            button_list.AddListItem(cancel_button);



            button_list.Create();
            button_list.Name = "button_bar";

            // align button list to center of timeline
            layout.Add(button_list, new LayoutOptions() { Flags = LayoutFlags.None,
                PinSourcePoint2D = LayoutUtil.BoxPointF(button_list, BoxPosition.TopLeft),
                PinTargetPoint2D = LayoutUtil.BoxPointF(screenContainer, BoxPosition.TopLeft, 10*OrthogenUI.PixelScale*(Vector2f.AxisX-Vector2f.AxisY) )
            });

            screenLayout.RecomputeLayout();



            // Configure interaction behaviors
            //   - below we add behaviors for mouse, gamepad, and spatial devices (oculus touch, etc)
            //   - keep in mind that Tool objects will register their own behaviors when active

            // setup key handlers (need to move to behavior...)
            cockpit.AddKeyHandler(new OrthoGenKeyHandler(cockpit.Context));

            // these behaviors let us interact with UIElements (ie left-click/trigger, or either triggers for Touch)
            cockpit.InputBehaviors.Add(new Mouse2DCockpitUIBehavior(cockpit.Context) { Priority = 0 });
            cockpit.InputBehaviors.Add(new VRMouseUIBehavior(cockpit.Context) { Priority = 1 });

            // selection / multi-selection behaviors
            // Note: this custom behavior implements some selection redirects that we use in various parts of Archform
            cockpit.InputBehaviors.Add(new MouseMultiSelectBehavior(cockpit.Context) { Priority = 10 });

            // left click-drag to tumble, and left click-release to de-select
            cockpit.InputBehaviors.Add(new MouseClickDragSuperBehavior() {
                Priority = 100,
                DragBehavior = new MouseViewRotateBehavior(cockpit.Context) { Priority = 100, RotateSpeed = 3.0f },
                ClickBehavior = new MouseDeselectBehavior(cockpit.Context) { Priority = 999 }
            });

            // also right-click-drag to tumble
            cockpit.InputBehaviors.Add(new MouseViewRotateBehavior(cockpit.Context) {
                Priority = 100, RotateSpeed = 3.0f,
                ActivateF = MouseBehaviors.RightButtonPressedF, ContinueF = MouseBehaviors.RightButtonDownF
            });

            // middle-click-drag to pan
            cockpit.InputBehaviors.Add(new MouseViewPanBehavior(cockpit.Context) {
                Priority = 100, PanSpeed = 10.0f,
                ActivateF = MouseBehaviors.MiddleButtonPressedF, ContinueF = MouseBehaviors.MiddleButtonDownF
            });


            cockpit.OverrideBehaviors.Add(new MouseWheelZoomBehavior(cockpit) { Priority = 100, ZoomScale = 100.0f });

            // touch input
            cockpit.InputBehaviors.Add(new TouchUIBehavior(cockpit.Context) { Priority = 1 });
            cockpit.InputBehaviors.Add(new Touch2DCockpitUIBehavior(cockpit.Context) { Priority = 0 });
            cockpit.InputBehaviors.Add(new TouchViewManipBehavior(cockpit.Context) {
                Priority = 999, TouchZoomSpeed = 1.0f, TouchPanSpeed = 0.3f
            });


            // update buttons enable/disable on state transitions, selection changes
            Action updateStateChangeButtons = () => {
                trim_scan_button.Enabled = OG.CanTransition(OGWorkflow.TrimScanStartT);
                align_scan_button.Enabled = OG.CanTransition(OGWorkflow.AlignScanStartT);
                bend_scan_button.Enabled = OG.CanTransition(OGWorkflow.BendScanStartT);
                sculpt_scan_button.Enabled = OG.CanTransition(OGWorkflow.ScanSmoothBrushStartT);
                accept_scan_button.Enabled = 
                    OG.IsInState(ScanState.Identifier) && OG.CanTransitionToState(RectifyState.Identifier);

                draw_offset_area_button.Enabled = OG.CanTransition(OGWorkflow.DrawAreaStartT);
                draw_smooth_area_button.Enabled = OG.CanTransition(OGWorkflow.DrawAreaStartT);
                add_plane_button.Enabled = OG.CanTransition(OGWorkflow.AddDeformRingStartT);
                add_lengthen_button.Enabled = OGActions.CanAddLengthenOp();
                sculpt_layer_button.Enabled = OGActions.CanSculptGlobalMap();
                sculpt_region_button.Enabled = OGActions.CanSculptRegion();
                accept_rectify_button.Enabled = OG.IsInState(RectifyState.Identifier) &&
                        OG.CanTransitionToState(SocketDesignState.Identifier);

                draw_trim_line_button.Enabled = OG.CanTransition(OGWorkflow.DrawTrimlineStartT);
                plane_trim_line_button.Enabled = OG.CanTransition(OGWorkflow.PlaneTrimlineStartT);
                add_afo_button.Enabled = add_socket_button.Enabled = OGActions.CanAddDevice();
                export_socket_button.Enabled = OGActions.CanExportSocket();
                export_gcode_button.Enabled = OGActions.CanExportGCode();

                sculpt_curve_button.Enabled = sculpt_router.CanApply(OG.Model.Workflow);
            };
            OG.OnWorfklowInitialized += (o,e) => { updateStateChangeButtons(); };
            OG.OnStateTransition += (from, to) => { updateStateChangeButtons(); };
            OG.OnDataModelModified += (from, to) => { updateStateChangeButtons(); };
            cockpit.Scene.SelectionChangedEvent += (o,e) => { if (OG.WorkflowInitialized) updateStateChangeButtons(); };
            cockpit.Scene.ChangedEvent += (scene,so,type) => { if (OG.WorkflowInitialized) updateStateChangeButtons(); };

            // accept/cancel buttons need to be checked every frame because the CanApply state
            // could change at any time, and there is no event about it
            cockpit.Context.RegisterEveryFrameAction("update_accept_cancel_buttons", () => {
                if (cockpit.Context.ToolManager.ActiveRightTool != null) {
                    cancel_button.Enabled = true;
                    accept_button.Enabled = cockpit.Context.ToolManager.ActiveRightTool.CanApply;
                } else {
                    cancel_button.Enabled = accept_button.Enabled = false;
                }

                // [RMS] currently this state changes outside workflow state changes...
                add_afo_button.Enabled = add_socket_button.Enabled = OGActions.CanAddDevice();

            });


        }
    }












    public class OrthoGenKeyHandler : IShortcutKeyHandler
    {
        FContext context;
        public OrthoGenKeyHandler(FContext c)
        {
            context = c;
        }
        public bool HandleShortcuts()
        {
            bool bShiftDown = Input.GetKey(KeyCode.LeftShift);
            bool bCtrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            // ESCAPE CLEARS ACTIVE TOOL OR SELECTION
            if (Input.GetKeyUp(KeyCode.Escape)) {
                if (context.ToolManager.HasActiveTool(0) || context.ToolManager.HasActiveTool(1)) {
                    OGActions.CancelCurrentTool();
                } else if (context.Scene.Selected.Count > 0) {
                    context.Scene.ClearSelection();
                }
                return true;


            // ENTER AND LETTER A APPLY CURRENT TOOL IF POSSIBLE
            } else if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.A)) {
                if (OGActions.CanAcceptCurrentTool())
                    OGActions.AcceptCurrentTool();
                return true;

            } else if (Input.GetKeyUp(KeyCode.T)) {
                //RMSTests.TestIsoCurve();
                return true;

            } else if (Input.GetKeyUp(KeyCode.Delete)) {
                if (context.Scene.Selected.Count == 1) {
                    DeleteSOChange change = new DeleteSOChange() { scene = context.Scene, so = context.Scene.Selected[0] };
                    context.Scene.History.PushChange(change, false);
                }
                return true;


                // CENTER TARGET (??)
            } else if (Input.GetKeyUp(KeyCode.C)) {
                Ray3f cursorRay = context.MouseController.CurrentCursorWorldRay();
                AnyRayHit hit = null;
                if (context.Scene.FindSceneRayIntersection(cursorRay, out hit)) {
                    context.ActiveCamera.Animator().AnimatePanFocus(hit.hitPos, CoordSpace.WorldCoords, 0.3f);
                }
                return true;

                // TOGGLE FRAME TYPE
            } else if (Input.GetKeyUp(KeyCode.F)) {
                FrameType eCur = context.TransformManager.ActiveFrameType;
                context.TransformManager.ActiveFrameType = (eCur == FrameType.WorldFrame)
                    ? FrameType.LocalFrame : FrameType.WorldFrame;
                return true;

            } else if (Input.GetKeyUp(KeyCode.D)) {
                return true;

                // VISIBILITY  (V HIDES, SHIFT+V SHOWS)
            } else if (Input.GetKeyUp(KeyCode.V)) {
                // show/hide (should be abstracted somehow?? instead of directly accessing GOs?)
                if (bShiftDown) {
                    foreach (SceneObject so in context.Scene.SceneObjects)
                        so.RootGameObject.Show();
                } else {
                    foreach (SceneObject so in context.Scene.Selected)
                        so.RootGameObject.Hide();
                    context.Scene.ClearSelection();
                }
                return true;

                // UNDO
            } else if (bCtrlDown && Input.GetKeyUp(KeyCode.Z)) {
                context.Scene.History.InteractiveStepBack();
                return true;

                // REDO
            } else if (bCtrlDown && Input.GetKeyUp(KeyCode.Y)) {
                context.Scene.History.InteractiveStepForward();
                return true;


            } else if (Input.GetKeyUp(KeyCode.Backspace)) {
                if (OG.IsInState(OGWorkflow.SocketState) && OG.CanTransitionToState(OGWorkflow.RectifyState)) {
                    OG.TransitionToState(OGWorkflow.RectifyState);
                    OG.Leg.SetOpWidgetVisibility(true);
                }
                return true;

            } else if (Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow)) {
                float sign = Input.GetKeyUp(KeyCode.UpArrow) ? 1 : -1;
                if (OG.Scene.Selected.Count == 0 && OG.IsInState(OGWorkflow.SocketState)) {
                    float new_height = MathUtil.Clamp(OG.Socket.ConnectorCutHeight + sign * 5.0f, 5.0f, 200.0f);
                    OG.Socket.ConnectorCutHeight = new_height;

                } else if (OG.Scene.Selected.Count == 1 && OG.LastActiveModelingOp != null) {
                    if (OG.LastActiveModelingOp is PlaneBandExpansionOp) {
                        PlaneBandExpansionOp deform = OG.LastActiveModelingOp as PlaneBandExpansionOp;
                        deform.BandDistance = MathUtil.Clamp(deform.BandDistance + sign * 2.0f, 10.0f, 1000.0f);
                    }
                    if (OG.LastActiveModelingOp is EnclosedRegionSmoothOp) {
                        EnclosedRegionSmoothOp deform = OG.LastActiveModelingOp as EnclosedRegionSmoothOp;
                        deform.OffsetDistance += sign * 0.1f;
                    }
                    if (OG.LastActiveModelingOp is EnclosedRegionOffsetOp) {
                        EnclosedRegionOffsetOp deform = OG.LastActiveModelingOp as EnclosedRegionOffsetOp;
                        WyvillFalloff falloff = deform.Falloff as WyvillFalloff;
                        falloff.ConstantRange = MathUtil.Clamp(falloff.ConstantRange + sign * 0.1, 0.0, 0.9);
                        deform.Falloff = falloff;
                    }
                }
                return true;

            } else if (Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow)) {
                float sign = Input.GetKeyUp(KeyCode.RightArrow) ? 1 : -1;
                if (bCtrlDown && bShiftDown && OG.Model.HasDevice()) {
                    int debug = OG.Socket.DeviceGenerator.DebugStep;
                    if (debug == int.MaxValue) debug = 0;
                    else debug = MathUtil.Clamp(debug + (int)sign, 0, 10);
                    OG.Socket.DeviceGenerator.DebugStep = debug;
                } else if (OG.LastActiveModelingOp != null) {
                    if (OG.LastActiveModelingOp is PlaneBandExpansionOp) {
                        PlaneBandExpansionOp deform = OG.LastActiveModelingOp as PlaneBandExpansionOp;
                        deform.PushPullDistance += sign * 0.25f;
                    }
                    if (OG.LastActiveModelingOp is EnclosedRegionOffsetOp) {
                        EnclosedRegionOffsetOp deform = OG.LastActiveModelingOp as EnclosedRegionOffsetOp;
                        deform.PushPullDistance += sign * 0.25f;
                    }
                    if (OG.LastActiveModelingOp is EnclosedRegionSmoothOp) {
                        EnclosedRegionSmoothOp deform = OG.LastActiveModelingOp as EnclosedRegionSmoothOp;
                        deform.SmoothAlpha += sign * 0.1f;
                    }
                } else if ( OG.Model.HasDevice() ) {
                    if (OG.Socket.IsSocket) {
                        VariableSizeFlatBaseConnector conn = OG.Socket.DeviceSocket.Connector as VariableSizeFlatBaseConnector;
                        if (conn != null)
                            conn.BaseDiameter += sign * 5.0;
                    }
                }

                return true;
            } else if (Input.GetKeyUp(KeyCode.LeftBracket) || Input.GetKeyUp(KeyCode.RightBracket)) {
                float fSign = Input.GetKeyUp(KeyCode.LeftBracket) ? -1 : 1;
                if (OG.ActiveToolAs<SculptCurveTool>() != null) {
                    SculptCurveTool tool = OG.ActiveToolAs<SculptCurveTool>();
                    double fRadiusS = tool.Radius.SceneValue;
                    fRadiusS = MathUtil.Clamp(fRadiusS + 2.5 * fSign, 5.0, 100.0);
                    tool.Radius = fDimension.Scene(fRadiusS);
                } else if (OG.ActiveToolAs<SurfaceBrushTool>() != null) {
                    SurfaceBrushTool tool = OG.ActiveToolAs<SurfaceBrushTool>();
                    double fRadiusS = tool.Radius.SceneValue;
                    fRadiusS = MathUtil.Clamp(fRadiusS + 2.5 * fSign, 5.0, 100.0);
                    tool.Radius = fDimension.Scene(fRadiusS);
                }
                return true;
                // REDO
            } else if (Input.GetKeyUp(KeyCode.M)) {
                if (OG.IsInState(OGWorkflow.ScanState))
                    context.Scene.Select(OG.Scan.SO, true);
                else if (OG.IsInState(OGWorkflow.RectifyState) )
                    context.Scene.Select(OG.Leg.SO, true);
                if (context.Scene.Selected.Count > 0) {
                    context.ToolManager.SetActiveToolType(TwoPointMeasureTool.Identifier, 0);
                    context.ToolManager.ActivateTool(0);
                }
                return true;


            } else if (Input.GetKeyUp(KeyCode.L)) {
                DemoActions.CustomGizmoDemo(OG.Context);
                return true;



            } else if (Input.GetKeyUp(KeyCode.Alpha1) || Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.Alpha3)) {
                if ( OG.Model.HasDevice() && OG.Socket.IsSocket ) {
                    if (Input.GetKeyUp(KeyCode.Alpha1)) {
                        //OG.Socket.DeviceSocket.Connector = new Type1SocketConnector();
                        OG.Socket.DeviceSocket.Connector = new OttobockSocketAdapter();
                    } else if (Input.GetKeyUp(KeyCode.Alpha2)) {
                        OG.Socket.DeviceSocket.Connector = new VariableSizeFlatBaseConnector() { HasInner = false };
                    } else if (Input.GetKeyUp(KeyCode.Alpha3)) {
                        OG.Socket.DeviceSocket.Connector = null;
                    }
                }
                return true;


            } else if ( Input.GetKeyUp(KeyCode.P)) {
                SOSectionPlane section = new SOSectionPlane(OG.Leg.RectifiedSO);
                Frame3f center = OG.Leg.RectifiedSO.GetLocalFrame(CoordSpace.SceneCoords);
                Frame3f cutPlane = new Frame3f(center.Origin, Vector3f.AxisY);
                section.UpdateSection(cutPlane, CoordSpace.SceneCoords);
                List<DCurve3> sectionCurves = section.GetSectionCurves();
                foreach ( var c in sectionCurves ) {
                    DebugUtil.EmitDebugCurve("curve", c.Vertices.ToArray(), c.Closed, 1.0f, Colorf.Red, Colorf.Black, OG.Scene.RootGameObject, false);
                }
                DMesh3 sectionMesh = section.GetSectionMesh();
                MeshTransforms.Translate(sectionMesh, 100 * Vector3d.AxisX);
                DebugUtil.EmitDebugMesh("section", sectionMesh, Colorf.Yellow, OG.Scene.RootGameObject, false);
                

                return true;

            } else if (Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.R)) {
                OGSerializer serializer = new OGSerializer();
                if (Input.GetKeyUp(KeyCode.R)) {
                    serializer.RestoreToCurrent("c:\\scratch\\OGSCENE.txt");
                } else {
                    serializer.StoreCurrent("c:\\scratch\\OGSCENE.txt");
                }
                return true;


            } else
                return false;
        }
    }

}