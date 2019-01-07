using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;
using f3;

namespace orthogen
{
    static class DemoActions
    {

        /// <summary>
        /// when the socket is updated, shift the ground plane to be directly below it
        /// </summary>
        public static void AddRepositionGroundPlaneOnSocketEdit()
        {
            OG.OnSocketUpdated += () => {
                // compute scene-space bbox of socket mesh
                Frame3f socketF = OG.Socket.Socket.GetLocalFrame(CoordSpace.ObjectCoords);
                AxisAlignedBox3d boundsS =
                    MeshMeasurements.Bounds(OG.Socket.Socket.Mesh, socketF.FromFrameP);

                // vertically translate bounds objects to be at same y 
                //  (assumes they are xz planes!!)
                Vector3d baseS = boundsS.Center - boundsS.Extents[1] * Vector3d.AxisY;
                Vector3d baseW = OG.Scene.ToWorldP(baseS);
                foreach (var go in OG.Scene.BoundsObjects) {
                    Vector3f pos = go.GetPosition();
                    pos.y = (float)baseW.y;
                    go.SetPosition(pos);
                }
            };
        }










        public static void CustomGizmoDemo(FContext context)
        {
            // call this at setup to register a transform gizmo type

            const string PlaneGizmoType = "my_plane_gizmo_type";
            context.TransformManager.RegisterGizmoType(PlaneGizmoType,
                new AxisTransformGizmoBuilder() {
                    Factory = new PlaneTranslateGizmoWidgetFactory() { OverrideRenderQueue = 100 },
                    EnableRotationSnapping = false,
                    RotationSnapStepSizeDeg = 25.0f
                }
            );


            // create a pivot. This is just so there is something to click on in the middle, 
            // and so that there is a frame. You would do this in a Tool.Setup(), and then listen
            // for transform changes
            PivotSO so = new PivotSO();
            so.Create(context.Scene.PivotSOMaterial);
            context.Scene.AddSceneObject(so);
            //so.OnTransformModified += some_event_handler

            // Give it an initial frame. You would initialize this yourself to something sane
            // The gizmo will only translate in the XY plane of this frame
            Frame3f anyFrame = new Frame3f(Vector3f.Zero, Vector3f.OneNormalized);
            so.SetLocalFrame(anyFrame, CoordSpace.SceneCoords);

            // ok set this gizmo type as active. Also something you would do in tool setup,
            // and clear in tool shutdown. (Or maybe in workflow graph transitions)
            context.TransformManager.SetActiveGizmoType(PlaneGizmoType);

            // now select the pivot. This will cause the gizmo to be created.
            context.Scene.Select(so, true);

        }


        class PlaneTranslateGizmoWidgetFactory : DefaultAxisGizmoWidgetFactory
        {
            public override bool Supports(AxisGizmoFlags widget)
            {
                return (widget & (AxisGizmoFlags.AxisTranslateX | AxisGizmoFlags.AxisTranslateY | AxisGizmoFlags.AxisRotateZ | AxisGizmoFlags.PlaneTranslateZ)) != 0;
            }
        }







    }
}
