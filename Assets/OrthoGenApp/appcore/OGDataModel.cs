using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;
using gs;
using f3;
using gsbody;

namespace orthogen
{

    /// <summary>
    /// Types of design supported by Orthogen
    /// </summary>
    public enum DesignTypes
    {
        StandardLegSocket = 0,
        StandardAFO = 1
    }



    /// <summary>
    /// Orthogen DataModel implementation. 
    /// Holds refs to main Model objects and critical SOs, funcs for initializing and updating them.
    /// Base AppDataModel class holds Workflow, as well as lots of event-handler stuff
    /// </summary>
    public class OGDataModel : AppDataModel
    {
        public ScanModel scan = null;
        public LegModel leg = null;
        public SocketModel device = null;
        public TrimLoopSO trimline = null;

        public ModelingOperator activeModelingOp = null;


        public OGDataModel()
        {
            
        }

        ~OGDataModel()
        {
            
        }

        /// <summary>
        /// Initialize or Re-initialize this DataModel with the given Context
        /// </summary>
        public override void Reinitialize(FContext context)
        {
            Disconnect();
            base.Reinitialize(context);
        }


        /// <summary>
        /// Disconnect this Datamodel
        /// </summary>
        public override void Disconnect()
        {
            activeModelingOp = null;

            if (base.Workflow != null)
                base.Workflow.OnStateTransition -= OG.ForwardStateTransition;

            if (device != null) {
                device.OnSocketUpdated -= OG.notifySocketUpdated;
                device.OnSocketUpdateStatus -= OG.notifySocketUpdateStatus;
                device.Disconnect();
                device = null;
            }

            if (trimline != null) {
                trimline.GetScene().RemoveSceneObject(trimline, true);
                trimline = null;
            }

            if (leg != null) {
                leg.OnDeformationAdded -= OG.notifyModelingOpAdded;
                leg.OnDeformationRemoved -= OG.notifyModelingOpRemoved;
                leg.Disconnect();
                leg = null;
            }

            if (scan != null) {
                scan.Disconnect();
                scan = null;
            }

            base.Disconnect();
        }


        /// <summary>
        /// Initialize workflow based on this graph
        /// </summary>
        public override void InitializeWorkflow(WorkflowGraph graph)
        {
            // connect event forwarder
            if (base.Workflow != null)
                base.Workflow.OnStateTransition -= OG.ForwardStateTransition;

            base.InitializeWorkflow(graph);

            base.Workflow.OnStateTransition += OG.ForwardStateTransition;

        }


        public void InitializeScan(ScanSO so)
        {
            scan = new ScanModel(so);
            OnDataModelModified?.Invoke(this, EventArgs.Empty);
        }

        public void InitializeLeg(LegSO so)
        {
            leg = new LegModel(so, OrthogenMaterials.RectifiedLegMaterial);
            OnDataModelModified?.Invoke(this, EventArgs.Empty);
            leg.OnDeformationAdded += OG.notifyModelingOpAdded;
            leg.OnDeformationRemoved += OG.notifyModelingOpRemoved;

            // add links so that if we transform LegModel.SO, scan and rectified SO's
            // are also transformed
            SOFrameLink scanLink = new SOFrameLink(scan.SO, leg.SO);
            Scene.LinkManager.AddLink(scanLink);
            SOFrameLink rectifiedLink = new SOFrameLink(leg.RectifiedSO, leg.SO);
            Scene.LinkManager.AddLink(rectifiedLink);

        }

        public void InitializeTrimline(TrimLoopSO so)
        {
            trimline = so;

            if (device != null) {
                device.SetNewTrimLine(trimline);
                device.EnableUpdate = true;
                device.ShowSocket();
            }

            // add link so that curve moves with LegModel.SO
            SOFrameLink link = new SOFrameLink(so, OG.Leg.SO);
            OG.Scene.LinkManager.AddLink(link);

            OnDataModelModified?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveTrimLine()
        {
            trimline = null;

            if (device != null) {
                device.EnableUpdate = false;
                device.RemoveTrimLine();
                device.HideSocket();
            }

            OnDataModelModified?.Invoke(this, EventArgs.Empty);
        }


        public bool HasDevice()
        {
            return device != null;
        }
        public void InitializeDevice(SocketSO so, SocketModel.ModelModes eMode)
        {
            if (leg == null || trimline == null)
                DebugUtil.Log(2, "OrthogenDM.InitializeDevice: must have created leg and trimline first!");

            // [RMS] because we are going to transform the input leg, it is 
            //  simpler to keep SocketSO at the origin. Then we don't have to worry
            //  about relative frame orientations.
            // use same position as leg, but w/o rotation
            //Frame3f legFrameS = leg.RectifiedSO.GetLocalFrame(CoordSpace.SceneCoords);
            //legFrameS.Rotation = Quaternionf.Identity;
            //so.SetLocalFrame(legFrameS, CoordSpace.SceneCoords);

            device = new SocketModel(so, leg, trimline, eMode);
            device.OnSocketUpdated += OG.notifySocketUpdated;
            device.OnSocketUpdateStatus += OG.notifySocketUpdateStatus;

            OnDataModelModified?.Invoke(this, EventArgs.Empty);
        }



        public void SetActiveModelingOperator(ModelingOperator op)
        {
            activeModelingOp = op;
            OG.notifyActiveModelingOpChanged();
        }


        public override void Update()
        {
            if (scan != null)
                scan.Update();

            if (leg != null)
                leg.Update();

            if (device != null)
                device.Update();
        }



        public EventHandler OnDataModelModified;

    }




    /// <summary>
    /// The OG class is basically just a bunch of convenient global functions, that 
    /// let us write things more concisely elsewhere, and give us a place to put
    /// breakpoints/etc to catch problems. 
    /// </summary>
    public static class OG
    {
        public static FContext Context;
        public static FScene Scene { get { return Context.Scene; } }

        public static DesignTypes DesignType;
        public static OGDataModel Model;

        public static bool WorkflowInitialized = false;


        /// <summary>
        /// "Reset" the DataModel for a new design
        /// </summary>
        public static void Reinitialize(FContext context, DesignTypes type)
        {
            DesignType = type;
            Context = context;

            if (Model != null) {
                Model.OnDataModelModified -= OnDataModelModified;
                Model.Disconnect();
                Model = null;
            }

            Model = new OGDataModel();
            Model.Reinitialize(Context);
            Model.OnDataModelModified += notifyDataModelModified;

            OGActions.InitializeSocketDataModel();

            OGActions.InitializeCancelRouter();
            OGActions.InitializeAcceptRouter();

            WorkflowInitialized = true;
            OnWorfklowInitialized?.Invoke(null, EventArgs.Empty);
        }


        /// <summary>
        /// Shutdown the datamodel (cleanly...)
        /// </summary>
        public static void Shutdown()
        {
            if (Model != null) {
                Model.OnDataModelModified -= OnDataModelModified;
                Model.Disconnect();
                Model = null;
                WorkflowInitialized = false;
            }
        }



        // accessors for DataModel objects

        public static ScanModel Scan {
            get { return Model.scan; }
        }
        public static LegModel Leg {
            get { return Model.leg; }
        }
        public static SocketModel Socket {
            get { return Model.device; }
        }
        public static TrimLoopSO TrimLine {
            get { return Model.trimline; }
        }



        public static LegModel.DeformationAddedEventHandler OnModelingOpAdded;
        internal static void notifyModelingOpAdded(SceneObject so, IVectorDisplacementSourceOp op) {
            OnModelingOpAdded?.Invoke(so, op);
        }

        public static LegModel.DeformationRemovedEventHandler OnModelingOpRemoved;
        internal static void notifyModelingOpRemoved(SceneObject so, IVectorDisplacementSourceOp op) {
            OnModelingOpRemoved?.Invoke(so, op);
        }



        public static ModelingOperator LastActiveModelingOp {
            get { return Model.activeModelingOp; }
        }
        public static T ActiveModelingOpAs<T>() where T : class {
            if (Model.activeModelingOp != null && Model.activeModelingOp is T)
                return Model.activeModelingOp as T;
            return null;
        }


        public static EventHandler OnModelingOpActivated;

        internal static void notifyActiveModelingOpChanged() {
            OnModelingOpActivated?.Invoke(null, EventArgs.Empty);
        }



        public static SocketModel.SocketUpdatedEventHandler OnSocketUpdated;
        internal static void notifySocketUpdated() { 
            OnSocketUpdated?.Invoke();
        }

        public static SocketModel.SocketUpdateStatusEventHandler OnSocketUpdateStatus;
        internal static void notifySocketUpdateStatus(SocketModel.SocketStatus status, string message)
        {
            OnSocketUpdateStatus?.Invoke(status, message);
        }


        /// <summary>
        /// forwarder for data model changes
        /// </summary>
        public static EventHandler OnDataModelModified;

        internal static void notifyDataModelModified(object sender, EventArgs e) {
            OnDataModelModified?.Invoke(sender, e);
        }



        // Shortcuts for Workflow functions

        public static bool IsInState(string state) {
            return Model.Workflow.IsInState(state);
        }
        public static bool CanTransition(string transition) {
            return Model.Workflow.CanTransition(transition);
        }
        public static bool CanTransitionToState(string state) {
            return Model.Workflow.CanTransitionToState(state);
        }
        public static void Transition(string transition) {
            Model.Workflow.Transition(transition);
        }
        public static void TransitionToState(string state) {
            Model.Workflow.TransitionToState(state);
        }


        /// <summary>
        /// Connect to this to get notified when workflow is initialized
        /// </summary>
        public static event EventHandler OnWorfklowInitialized;

        /// <summary>
        /// Connect to this to get forwarded events from current workflow
        /// </summary>
        public static event WorkflowGraphTransitionEvent OnStateTransition;

        internal static void ForwardStateTransition(WorkflowState from, WorkflowState to) {
            OnStateTransition?.Invoke(from, to);
        }



        // Tool shortcuts

        public static T ActiveToolAs<T>() where T : class {
            if (Context.ToolManager.HasActiveTool(ToolSide.Right) && Context.ToolManager.ActiveRightTool is T)
                return Context.ToolManager.ActiveRightTool as T;
            return null;
        }


    }

}
