using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;
using gs;

namespace gsbody
{
    /// <summary>
    /// [RMS] this is the connector with the four holes in the bottom and
    /// one hole on the side, that Matt designed / gave me
    /// </summary>
    public class Type1SocketConnector : SocketConnector
    {
        public Type1SocketConnector() : base()
        {
        }


        bool geometry_valid = false;


        protected override void validate_geometry()
        {
            if (geometry_valid)
                return;

            float outer_diam = 100;
            float inner_diam = 68;
            float base_thickness = 5.0f;
            float inner_height = 29;
            float wall_thickness = (outer_diam - inner_diam) * 0.5f;
            generate_matt_v1(outer_diam, base_thickness + inner_height, wall_thickness, base_thickness);

            geometry_valid = true;
        }


        protected void generate_matt_v1(float fDiameter, float fHeight, float fWallThickness, float fBaseThickness)
        {
            base.reset_holes();

            double SidePegDiam = 14.0;
            double BasePegLargeDiam = 12.5;
            double BasePegSmallDiam = 8.0;
            double BasePegSmallDist = 15.2 + (BasePegLargeDiam / 2.0) + (BasePegSmallDiam / 2.0);

            CappedCylinderGenerator outer_cylgen = new CappedCylinderGenerator() {
                BaseRadius = fDiameter / 2, TopRadius = fDiameter / 2,
                Height = fHeight + 10,
                Slices = 60,
                Clockwise = true
            };
            DMesh3 outer_mesh = outer_cylgen.Generate().MakeDMesh();

            float fInnerDiam = fDiameter - 2 * fWallThickness;
            CappedCylinderGenerator inner_cylgen = new CappedCylinderGenerator() {
                BaseRadius = fInnerDiam / 2, TopRadius = fInnerDiam / 2,
                Height = fHeight + 10,
                Slices = 60,
                Clockwise = false
            };
            DMesh3 inner_mesh = inner_cylgen.Generate().MakeDMesh();
            MeshTransforms.Translate(inner_mesh, fBaseThickness * Vector3d.AxisY);

            DMesh3[] meshes = new DMesh3[2] { outer_mesh, inner_mesh };

            foreach (DMesh3 mesh in meshes) {
                Remesher r = new Remesher(mesh);
                r.SetTargetEdgeLength(TargetEdgeLength);
                r.SmoothSpeedT = 0.5f;
                r.SetExternalConstraints(new MeshConstraints());
                MeshConstraintUtil.FixAllGroupBoundaryEdges(r.Constraints, mesh, true);
                r.SetProjectionTarget(MeshProjectionTarget.Auto(mesh));
                for (int k = 0; k < 10; ++k)
                    r.BasicRemeshPass();
            }

            Vector3d vCutPos = new Vector3d(0, fHeight, 0);
            Vector3d vCutNormal = Vector3d.AxisY;

            foreach (DMesh3 mesh in meshes) {
                MeshPlaneCut cut = new MeshPlaneCut(mesh, new Vector3d(0, fHeight, 0), Vector3d.AxisY);
                cut.Cut();
            }

            base.set_output_meshes(inner_mesh, outer_mesh);

            HoleInfo largeSidePeg = new HoleInfo() {
                IsVertical = false,
                Radius = SidePegDiam / 2.0,
                Height = fBaseThickness + 1.5 + (SidePegDiam / 2.0),
                AroundAngle = 180.0
            };
            base.add_hole(largeSidePeg);

            HoleInfo centerBasePeg = new HoleInfo() {
                IsVertical = true,
                Radius = BasePegLargeDiam / 2.0,
                XZOffset = Vector2d.Zero
            };
            base.add_hole(centerBasePeg);

            HoleInfo offsetBasePeg = new HoleInfo() {
                IsVertical = true,
                Radius = BasePegSmallDiam / 2.0,
                XZOffset = BasePegSmallDist * Vector2d.AxisX
            };
            base.add_hole(offsetBasePeg);
            offsetBasePeg.XZOffset = BasePegSmallDist * Vector2d.AxisY;
            base.add_hole(offsetBasePeg);
            offsetBasePeg.XZOffset = -BasePegSmallDist * Vector2d.AxisY;
            base.add_hole(offsetBasePeg);
            offsetBasePeg.XZOffset = -BasePegSmallDist * Vector2d.AxisX;
            base.add_hole(offsetBasePeg);
        }
    }













    /// <summary>
    /// [RMS] Ottobock connector with large central hole and four surrounding
    /// holes that have hex sockets combined with cylindrical sockets
    /// (presumably for bolt holes?)
    /// </summary>
    public class OttobockSocketAdapter : SocketConnector
    {
        double base_diameter = 62;
        public double BaseDiameter {
            get { return base_diameter; }
            set { base_diameter = value; geometry_valid = false; post_modified_event(); }
        }

        double bolt_base_height = 15;
        public double BoltBaseHeight {
            get { return bolt_base_height; }
            set { bolt_base_height = value; geometry_valid = false; post_modified_event(); }
        }

        double bolt_head_height = 10;
        public double BoltHeadHeight {
            get { return bolt_head_height; }
            set { bolt_head_height = value; geometry_valid = false; post_modified_event(); }
        }


        double center_hole_diam = 32.5;
        public double CenterHoleDiam {
            get { return center_hole_diam; }
            set { center_hole_diam = value; geometry_valid = false; post_modified_event(); }
        }

        double center_hole_height = 10;
        public double CenterHoleHeight {
            get { return center_hole_height; }
            set { center_hole_height = value; geometry_valid = false; post_modified_event(); }
        }


        double hex_socket_diam = 8.75;
        public double HexSocketDiam {
            get { return hex_socket_diam; }
            set { hex_socket_diam = value; geometry_valid = false; post_modified_event(); }
        }

        double bolt_hole_diam = 5.0;
        public double BoltHoleDiam {
            get { return bolt_hole_diam; }
            set { bolt_hole_diam = value; geometry_valid = false; post_modified_event(); }
        }

        double hex_head_space = 0.875;
        public double HexHeadSpace {
            get { return hex_head_space; }
            set { hex_head_space = value; geometry_valid = false; post_modified_event(); }
        }

        public OttobockSocketAdapter() : base()
        {
            HasInner = false;
        }


        bool geometry_valid = false;


        protected override void validate_geometry()
        {
            if (geometry_valid)
                return;

            generate_ottobock_v1((float)base_diameter,
                (float)(bolt_base_height + bolt_head_height),
                (float)bolt_base_height);

            geometry_valid = true;
        }


        protected void generate_ottobock_v1(float fDiameter, float fHeight, float fBoltBaseHeight)
        {
            base.reset_holes();

            double largeHoleDiam = center_hole_diam;
            double largeHoleInset = center_hole_height;
            double hexSocketDiam = HexSocketDiam;
            double hexSocketDist = (center_hole_diam / 2) + (hex_socket_diam / 2) + hex_head_space;
            double boltHoleDiam = bolt_hole_diam;
            double boltHoleHeadBaseHeight = fBoltBaseHeight;

            CappedCylinderGenerator outer_cylgen = new CappedCylinderGenerator() {
                BaseRadius = fDiameter / 2, TopRadius = fDiameter / 2,
                Height = fHeight + 5,
                Slices = 60,
                Clockwise = true
            };
            DMesh3 outer_mesh = outer_cylgen.Generate().MakeDMesh();

            Remesher r = new Remesher(outer_mesh);
            r.SetTargetEdgeLength(TargetEdgeLength);
            r.SmoothSpeedT = 0.5f;
            r.SetExternalConstraints(new MeshConstraints());
            MeshConstraintUtil.FixAllGroupBoundaryEdges(r.Constraints, outer_mesh, true);
            r.SetProjectionTarget(MeshProjectionTarget.Auto(outer_mesh));
            for (int k = 0; k < 10; ++k)
                r.BasicRemeshPass();

            Vector3d vCutPos = new Vector3d(0, fHeight, 0);
            Vector3d vCutNormal = Vector3d.AxisY;

            MeshPlaneCut cut = new MeshPlaneCut(outer_mesh, new Vector3d(0, fHeight, 0), Vector3d.AxisY);
            cut.Cut();

            base.set_output_meshes(null, outer_mesh);

            // partial holes (hex sockets)

            HoleInfo hexSocket = new HoleInfo() {
                IsVertical = true,
                CutMode = HoleModes.PartialDown,
                Radius = Circle2d.BoundingPolygonRadius(hexSocketDiam / 2.0, 6),
                XZOffset = hexSocketDist * Vector2d.AxisX,
                Vertices = 6,
                AxisAngleD = 30,
                PartialHoleBaseHeight = boltHoleHeadBaseHeight,
                PartialHoleGroupID = 3737
            };
            base.add_hole(hexSocket);
            hexSocket.XZOffset = -hexSocketDist * Vector2d.AxisX;
            base.add_hole(hexSocket);
            hexSocket.AxisAngleD = 0;
            hexSocket.XZOffset = hexSocketDist * Vector2d.AxisY;
            base.add_hole(hexSocket);
            hexSocket.XZOffset = -hexSocketDist * Vector2d.AxisY;
            base.add_hole(hexSocket);


            // cylindrical holes that pass through hex insets

            HoleInfo throughHole = new HoleInfo() {
                IsVertical = true,
                Radius = boltHoleDiam / 2,
                XZOffset = hexSocketDist * Vector2d.AxisX,
                GroupIDFilters = new Index2i(0, 3737)
            };
            base.add_hole(throughHole);
            throughHole.XZOffset = -hexSocketDist * Vector2d.AxisX;
            base.add_hole(throughHole);
            throughHole.XZOffset = hexSocketDist * Vector2d.AxisY;
            base.add_hole(throughHole);
            throughHole.XZOffset = -hexSocketDist * Vector2d.AxisY;
            base.add_hole(throughHole);

            // large center hole

            HoleInfo centerHole = new HoleInfo() {
                IsVertical = true,
                Radius = largeHoleDiam / 2.0,
                XZOffset = Vector2d.Zero,
                Vertices = 64,
                CutMode = HoleModes.PartialUp,
                PartialHoleBaseHeight = largeHoleInset,
                PartialHoleGroupID = 3738
            };
            base.add_hole(centerHole);

        }
    }
}
