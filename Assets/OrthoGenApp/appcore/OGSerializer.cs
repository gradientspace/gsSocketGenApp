using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using g3;
using f3;
using gs;
using gsbody;
using System.IO;
using System.IO.Compression;

namespace orthogen
{
    public class OGSerializer
    {

        public void StoreCurrent(string path)
        {
            DebugUtil.Log("[OGSerializer] Saving scene to " + path);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            using (XmlWriter writer = XmlWriter.Create(path, settings)) {

                writer.WriteStartElement("OrthogenFile");

                XMLOutputStream stream = new XMLOutputStream(writer);
                StoreScene(stream, OG.Scene);

                StoreDataModel(writer);

                writer.WriteEndElement();
            }
        }



        public void RestoreToCurrent(string path)
        {
            DebugUtil.Log("[OGSerializer] Restoring scene from " + path);

            XmlDocument doc = new XmlDocument();
            try {
                doc.Load(path);
            }catch (Exception) {
                DebugUtil.Log("[OGSerializer] failed to read XmlDocument");
                throw;
            }

            // begin restore
            OGActions.BeginRestoreExistingScene();

            // restore scene objects
            XMLInputStream stream = new XMLInputStream() { xml = doc };
            SceneSerializer serializer = new SceneSerializer() {
                SOFactory = new SOFactory()
            };
            serializer.Restore(stream, OG.Scene);

            // restore datamodel
            RestoreDataModel(doc);
        }



        /// <summary>
        /// serialize scene, with SO filter
        /// </summary>
        protected virtual void StoreScene(IOutputStream o, FScene scene)
        {
            SceneSerializer serializer = new SceneSerializer();
            serializer.SOFilterF = SerializeSOFilter;
            serializer.Store(o, scene);
        }

        /// <summary>
        /// Serializer SO filter
        /// If you return false from this object for an SO, it is not serialized.
        /// </summary>
        protected virtual bool SerializeSOFilter(SceneObject so)
        {
            if (OG.Leg != null && (so == OG.Leg.SO || so == OG.Leg.RectifiedSO))
                return false;
            if (OG.Socket != null && (so == OG.Socket.Socket))
                return false;
            return true;
        }


        /// <summary>
        /// Serialize data model.
        /// </summary>
        protected virtual void StoreDataModel(XmlWriter writer)
        {
            writer.WriteStartElement("DataModel");

            StoreClientDataModelData_Start(writer);

            if (OG.Scan != null)
                StoreScanData(writer);

            if (OG.Leg != null)
                StoreLegData(writer);

            if (OG.Socket != null)
                StoreSocketData(writer);

            StoreClientDataModelData_End(writer);

            writer.WriteEndElement();
        }
        protected virtual void StoreClientDataModelData_Start(XmlWriter writer)
        {
        }
        protected virtual void StoreClientDataModelData_End(XmlWriter writer)
        {
        }




        protected virtual void StoreScanData(XmlWriter writer)
        {
            StoreClientScanData_Start(writer);

            // nothing yet...

            StoreClientScanData_End(writer);
        }
        protected virtual void StoreClientScanData_Start(XmlWriter writer)
        {
        }
        protected virtual void StoreClientScanData_End(XmlWriter writer)
        {
        }





        protected virtual void StoreLegData(XmlWriter writer)
        {
            StoreClientLegData_Start(writer);

            // iterate over deformation operators and store them
            // elements are [so,op]
            foreach (var pair in OG.Leg.OperatorObjectPairs()) {
                if (pair.Item2 is EnclosedRegionOffsetOp) {
                    StoreOffsetRegionOp(pair.Item2 as EnclosedRegionOffsetOp, pair.Item1, writer);

                } else if (pair.Item2 is EnclosedRegionSmoothOp) {
                    StoreSmoothRegionOp(pair.Item2 as EnclosedRegionSmoothOp, pair.Item1, writer);

                } else if (pair.Item2 is PlaneBandExpansionOp) {
                    StorePlaneBandOp(pair.Item2 as PlaneBandExpansionOp, pair.Item1, writer);

                } else if (pair.Item2 is LegSqueezeOp) {
                    StoreLegSqueezeOp(pair.Item2 as LegSqueezeOp, pair.Item1, writer);

                } else if (pair.Item2 is LengthenOp) {
                    StoreLengthenOp(pair.Item2 as LengthenOp, pair.Item1, writer);
                }
            }

            MeshScaleOp postScale = OG.Leg.GetPostScaleOp();
            if (postScale != null)
                StoreScaleOp(postScale, writer, "PostScale");

            StoreDisplacementMapLayer(writer, "GlobalMap");

            StoreClientLegData_End(writer);
        }
        protected virtual void StoreClientLegData_Start(XmlWriter writer)
        {
        }
        protected virtual void StoreClientLegData_End(XmlWriter writer)
        {
        }


        protected virtual void StoreOffsetRegionOp(EnclosedRegionOffsetOp op, SceneObject so, XmlWriter writer)
        {
            writer.WriteStartElement("LegDeformOp");
            writer.WriteAttributeString("OpType", op.GetType().ToString());
            writer.WriteAttributeString("SceneObjectUUID", so.UUID);
            writer.WriteAttributeString("Offset", op.PushPullDistance.ToString());
            writer.WriteAttributeString("HaveEditedMap", (!op.GenerateMap).ToString());
            if (op.GenerateMap == false) {
                byte[] buffer = BufferUtil.CompressZLib(op.GetMapCopy().GetBytes(), false);
                writer.WriteElementString("ydVectors", System.Convert.ToBase64String(buffer));
            }

            StoreClientOffsetRegionOpData(op, so, writer);

            writer.WriteEndElement();
        }
        protected virtual void StoreClientOffsetRegionOpData(EnclosedRegionOffsetOp op, SceneObject so, XmlWriter writer)
        {
        }




        protected virtual void StoreSmoothRegionOp(EnclosedRegionSmoothOp op, SceneObject so, XmlWriter writer)
        {
            writer.WriteStartElement("LegDeformOp");
            writer.WriteAttributeString("OpType", op.GetType().ToString());
            writer.WriteAttributeString("SceneObjectUUID", so.UUID);
            writer.WriteAttributeString("Offset", op.OffsetDistance.ToString());
            writer.WriteAttributeString("Smooth", op.SmoothAlpha.ToString());

            StoreClientSmoothRegionOpData(op, so, writer);

            writer.WriteEndElement();
        }
        protected virtual void StoreClientSmoothRegionOpData(EnclosedRegionSmoothOp op, SceneObject so, XmlWriter writer)
        {
        }



        protected virtual void StorePlaneBandOp(PlaneBandExpansionOp op, SceneObject so, XmlWriter writer)
        {
            writer.WriteStartElement("LegDeformOp");
            writer.WriteAttributeString("OpType", op.GetType().ToString());
            writer.WriteAttributeString("SceneObjectUUID", so.UUID);
            writer.WriteAttributeString("Extent", op.BandDistance.ToString());
            writer.WriteAttributeString("Offset", op.PushPullDistance.ToString());
            writer.WriteAttributeString("Origin", op.Origin.ToString());
            writer.WriteAttributeString("Normal", op.Normal.ToString());

            StoreClientPlaneBandOpData(op, so, writer);

            writer.WriteEndElement();
        }
        protected virtual void StoreClientPlaneBandOpData(PlaneBandExpansionOp op, SceneObject so, XmlWriter writer)
        {
        }




        protected virtual void StoreLegSqueezeOp(LegSqueezeOp op, SceneObject so, XmlWriter writer)
        {
            writer.WriteStartElement("LegDeformOp");
            writer.WriteAttributeString("OpType", op.GetType().ToString());
            writer.WriteAttributeString("SceneObjectUUID", so.UUID);
            writer.WriteAttributeString("UpperPoint", op.UpperPoint.ToString());

            // deprecated
            //writer.WriteAttributeString("ReductionPercent", op.ReductionPercent.ToString());
            writer.WriteAttributeString("ReductionPercentTop", op.ReductionPercentTop.ToString());
            writer.WriteAttributeString("ReductionPercentBottom", op.ReductionPercentBottom.ToString());

            writer.WriteAttributeString("NumMidPoints", op.GetNumMidPoints().ToString() );
            for (int k = 0; k < op.GetNumMidPoints(); ++k) {
                string name = "MidPoint" + k.ToString();
                writer.WriteAttributeString(name, op.GetMidPoint(k).ToString());
            }

            ///
            writer.WriteAttributeString("Axis", op.Axis.ToString());
            StoreClientLegSqueezeOpData(op, so, writer);

            writer.WriteEndElement();
        }
        protected virtual void StoreClientLegSqueezeOpData(LegSqueezeOp op, SceneObject so, XmlWriter writer)
        {
        }



        protected virtual void StoreLengthenOp(LengthenOp op, SceneObject so, XmlWriter writer)
        {
            writer.WriteStartElement("LegDeformOp");
            writer.WriteAttributeString("OpType", op.GetType().ToString());
            writer.WriteAttributeString("SceneObjectUUID", so.UUID);
            writer.WriteAttributeString("Distance", op.LengthenDistance.ToString());

            StoreClientLengthenOpData(op, so, writer);

            writer.WriteEndElement();
        }
        protected virtual void StoreClientLengthenOpData(LengthenOp op, SceneObject so, XmlWriter writer)
        {
        }





        protected virtual void StoreScaleOp(MeshScaleOp op, XmlWriter writer, string tag)
        {
            writer.WriteStartElement("LegDeformOp");
            writer.WriteAttributeString("OpType", op.GetType().ToString());
            writer.WriteAttributeString("Tag", tag);
            writer.WriteAttributeString("Scale", op.ScaleFactor.ToString());

            StoreClientScaleOpData(op, writer, tag);

            writer.WriteEndElement();
        }
        protected virtual void StoreClientScaleOpData(MeshScaleOp op, XmlWriter writer, string tag)
        {
        }




        protected virtual void StoreDisplacementMapLayer(XmlWriter writer, string tag)
        {
            VectorDisplacementMapOp globalDisplaceMap = OG.Leg.GetBrushLayer();
            if (globalDisplaceMap != null) {
                VectorDisplacement map = globalDisplaceMap.GetMapCopy();
                if (map.IsNonZero()) {
                    writer.WriteStartElement("LegDeformOp");
                    writer.WriteAttributeString("OpType", globalDisplaceMap.GetType().ToString());
                    writer.WriteAttributeString("Tag", tag);
                    byte[] buffer = BufferUtil.CompressZLib(map.GetBytes(), false);
                    writer.WriteElementString("ydVectors", System.Convert.ToBase64String(buffer));

                    StoreClientDisplacementMapLayerData(globalDisplaceMap, writer, tag);

                    writer.WriteEndElement();
                }
            }
        }
        protected virtual void StoreClientDisplacementMapLayerData(VectorDisplacementMapOp op, XmlWriter writer, string tag)
        {
        }









        protected virtual void StoreSocketData(XmlWriter writer)
        {
            StoreClientSocketData_Start(writer);

            // nothing yet...

            StoreClientSocketData_End(writer);
        }
        protected virtual void StoreClientSocketData_Start(XmlWriter writer)
        {
        }
        protected virtual void StoreClientSocketData_End(XmlWriter writer)
        {
        }










        /// <summary>
        /// parse the DataModel section of the save file, and restore the scene/datamodel as necessary
        /// </summary>
        protected virtual void RestoreDataModel(XmlDocument xml)
        {
            // look up root datamodel (should only be one)
            XmlNodeList datamodels = xml.SelectNodes("//DataModel");
            XmlNode rootNode = datamodels[0];

            RestoreClientDataModelData_Start(rootNode);

            RestoreScanData(rootNode);

            RestoreLegData(rootNode);

            RestoreSocketData(rootNode);

            RestoreClientDataModelData_End(rootNode);

        }
        protected virtual void RestoreClientDataModelData_Start(XmlNode rootNode)
        {
        }
        protected virtual void RestoreClientDataModelData_End(XmlNode rootNode)
        {
        }




        protected virtual void RestoreScanData(XmlNode rootNode)
        {
            RestoreClientScanData_Start(rootNode);

            // find scan 
            ScanSO scanSO = OG.Scene.FindSceneObjectsOfType<ScanSO>().FirstOrDefault();
            if (scanSO == null)
                throw new Exception("OGSerializer.RestoreDataModel: no ScanSO?");

            // [TODO] we have scanIn and scanOut, don't we?!?
            // start in scan state, restore the scan
            OGActions.RestoreSocketDesignFromScan(OG.Context, scanSO);

            RestoreClientScanData_End(rootNode);
        }
        protected virtual void RestoreClientScanData_Start(XmlNode rootNode)
        {
        }
        protected virtual void RestoreClientScanData_End(XmlNode rootNode)
        {
            // [TODO] should only do this transition if user has accepted scan
            //    (probably should have some current-state field in datamodel)
            OG.TransitionToState(OGWorkflow.RectifyState);
        }



        protected virtual void RestoreLegData(XmlNode rootNode)
        {
            RestoreClientLegData_Start(rootNode);

            // restore LegModel deformation ops
            XmlNodeList deformationOps = rootNode.SelectNodes("LegDeformOp");
            foreach (XmlNode opNode in deformationOps) {
                string type = opNode.Attributes["OpType"].InnerText;
                string so_uuid = (opNode.Attributes["SceneObjectUUID"] != null) ?
                        opNode.Attributes["SceneObjectUUID"].InnerText : "";

                if (type == typeof(EnclosedRegionOffsetOp).ToString()) {
                    RestoreOffsetRegionOp(opNode, so_uuid);

                } else if (type == typeof(EnclosedRegionSmoothOp).ToString()) {
                    RestoreSmoothRegionOp(opNode, so_uuid);

                } else if (type == typeof(PlaneBandExpansionOp).ToString()) {
                    RestorePlaneBandExpansionOp(opNode, so_uuid);

                } else if (type == typeof(LegSqueezeOp).ToString()) {
                    RestoreLegSqueezeOp(opNode, so_uuid);

                } else if (type == typeof(LengthenOp).ToString()) {
                    RestoreLengthenOp(opNode, so_uuid);

                } else if (type == typeof(MeshScaleOp).ToString()) {
                    RestoreScaleOp(opNode, so_uuid);

                } else if (type == typeof(VectorDisplacementMapOp).ToString()) {
                    RestoreDisplacementMapOp(opNode, so_uuid);
                }
            }


            RestoreClientLegData_End(rootNode);
        }
        protected virtual void RestoreClientLegData_Start(XmlNode rootNode)
        {
        }
        protected virtual void RestoreClientLegData_End(XmlNode rootNode)
        {
        }



        protected virtual void RestoreOffsetRegionOp(XmlNode opNode, string so_uuid)
        {
            EnclosedPatchSO patchSO = OG.Scene.FindByUUID(so_uuid) as EnclosedPatchSO;
            var newOp = OGActions.AddNewRegionDeformation(patchSO, LegModel.LegDeformationTypes.Offset) as EnclosedRegionOffsetOp;
            double offset = 0.0f;
            if (double.TryParse(opNode.Attributes["Offset"].InnerText, out offset))
                newOp.PushPullDistance = offset;
            if (opNode.Attributes["HaveEditedMap"] != null && opNode.Attributes["HaveEditedMap"].InnerText == "True") {
                XmlNode bufferNode = opNode.SelectSingleNode("ydVectors");
                byte[] buffer = Convert.FromBase64String(bufferNode.InnerText);
                VectorDisplacement map = new VectorDisplacement(BufferUtil.DecompressZLib(buffer));
                newOp.GenerateMap = false;
                newOp.UpdateMap(map);
            }

            RestoreClientOffsetRegionOpData(opNode, patchSO, newOp);
        }
        protected virtual void RestoreClientOffsetRegionOpData(XmlNode opNode, EnclosedPatchSO so, EnclosedRegionOffsetOp op)
        {
        }



        protected virtual void RestoreSmoothRegionOp(XmlNode opNode, string so_uuid)
        {
            EnclosedPatchSO patchSO = OG.Scene.FindByUUID(so_uuid) as EnclosedPatchSO;
            var newOp = OGActions.AddNewRegionDeformation(patchSO, LegModel.LegDeformationTypes.Smooth) as EnclosedRegionSmoothOp;
            double smooth = 0.0f;
            if (double.TryParse(opNode.Attributes["Smooth"].InnerText, out smooth))
                newOp.SmoothAlpha = smooth;
            double offset = 0.0f;
            if (double.TryParse(opNode.Attributes["Offset"].InnerText, out offset))
                newOp.OffsetDistance = offset;

            RestoreClientSmoothRegionOpData(opNode, patchSO, newOp);
        }
        protected virtual void RestoreClientSmoothRegionOpData(XmlNode opNode, EnclosedPatchSO so, EnclosedRegionSmoothOp op)
        {
        }



        protected virtual void RestorePlaneBandExpansionOp(XmlNode opNode, string so_uuid)
        {
            PlaneIntersectionCurveSO curveSO = OG.Scene.FindByUUID(so_uuid) as PlaneIntersectionCurveSO;
            var newOp = OGActions.AddNewPlaneBandExpansion(curveSO) as PlaneBandExpansionOp;
            double extent = 0.0f;
            if (double.TryParse(opNode.Attributes["Extent"].InnerText, out extent))
                newOp.BandDistance = extent;
            double offset = 0.0f;
            if (double.TryParse(opNode.Attributes["Offset"].InnerText, out offset))
                newOp.PushPullDistance = offset;
            Vector3d origin = TryParseVector3(opNode.Attributes["Origin"].InnerText);
            newOp.Origin = origin;
            Vector3d normal = TryParseVector3(opNode.Attributes["Normal"].InnerText);
            newOp.Normal = normal;

            RestoreClientPlaneBandExpansionOpData(opNode, curveSO, newOp);
        }
        protected virtual void RestoreClientPlaneBandExpansionOpData(XmlNode opNode, PlaneIntersectionCurveSO so, PlaneBandExpansionOp op)
        {
        }



        protected virtual void RestoreLegSqueezeOp(XmlNode opNode, string so_uuid)
        {
            PlaneIntersectionCurveSO curveSO = OG.Scene.FindByUUID(so_uuid) as PlaneIntersectionCurveSO;

            Vector3d upperPoint = TryParseVector3(opNode.Attributes["UpperPoint"].InnerText);
            var newOp = OGActions.AddNewLegSqueeze((Vector3f)upperPoint, curveSO) as LegSqueezeOp;
            double percent = 0.0f;
            //if (double.TryParse(opNode.Attributes["ReductionPercent"].InnerText, out percent))
            //    newOp.ReductionPercent = percent;
            if (double.TryParse(opNode.Attributes["ReductionPercentTop"].InnerText, out percent))
                newOp.ReductionPercentTop = percent;
            if (double.TryParse(opNode.Attributes["ReductionPercentBottom"].InnerText, out percent))
                newOp.ReductionPercentBottom = percent;

            int num_mid_points = 0;
            int.TryParse(opNode.Attributes["NumMidPoints"].InnerText, out num_mid_points);
            List<Vector2d> midpoints = new List<Vector2d>();
            for (int k = 0; k < num_mid_points; ++k) {
                Vector2d v = TryParseVector2(opNode.Attributes["MidPoint" + k.ToString()].InnerText);
                midpoints.Add(v);
            }
            newOp.UpdateMidpoints(midpoints);

            newOp.Axis = TryParseVector3(opNode.Attributes["Axis"].InnerText);

            RestoreClientLegSqueezeOpData(opNode, curveSO, newOp);
        }
        protected virtual void RestoreClientLegSqueezeOpData(XmlNode opNode, PlaneIntersectionCurveSO so, LegSqueezeOp op)
        {
        }



        protected virtual void RestoreLengthenOp(XmlNode opNode, string so_uuid)
        {
            LengthenPivotSO pivotSO = OG.Scene.FindByUUID(so_uuid) as LengthenPivotSO;
            LengthenOp newOp = OGActions.AddNewLengthenOp(pivotSO);
            double offset = 0.0f;
            if (double.TryParse(opNode.Attributes["Distance"].InnerText, out offset))
                newOp.LengthenDistance = offset;

            RestoreClientLengthenOpData(opNode, pivotSO, newOp);
        }
        protected virtual void RestoreClientLengthenOpData(XmlNode opNode, LengthenPivotSO so, LengthenOp op)
        {
        }




        protected virtual void RestoreScaleOp(XmlNode opNode, string so_uuid)
        {
            double scale = 1.0f;
            double.TryParse(opNode.Attributes["Scale"].InnerText, out scale);
            var tag = opNode.Attributes["Tag"];
            MeshScaleOp scaleOp = null;
            if (tag != null && tag.InnerText == "PostScale") {
                scaleOp = OG.Leg.GetPostScaleOp();
                scaleOp.ScaleFactor = scale;
            }
            if (scaleOp != null)
                RestoreClientScaleOpData(opNode, scaleOp);
        }
        protected virtual void RestoreClientScaleOpData(XmlNode opNode, MeshScaleOp op)
        {
        }



        protected virtual void RestoreDisplacementMapOp(XmlNode opNode, string so_uuid)
        {
            XmlNode bufferNode = opNode.SelectSingleNode("ydVectors");
            byte[] buffer = Convert.FromBase64String(bufferNode.InnerText);
            VectorDisplacement map = new VectorDisplacement(BufferUtil.DecompressZLib(buffer));
            var tag = opNode.Attributes["Tag"];
            VectorDisplacementMapOp mapOp = null;
            if (tag != null && tag.InnerText == "GlobalMap") {
                mapOp = OG.Leg.GetBrushLayer();
                mapOp.UpdateMap(map);
            }
            if (mapOp != null)
                RestoreClientDisplacementMapOpData(opNode, mapOp);
        }
        protected virtual void RestoreClientDisplacementMapOpData(XmlNode opNode, VectorDisplacementMapOp op)
        {
        }



        protected virtual void RestoreSocketData(XmlNode rootNode)
        {
            RestoreClientSocketData_Start(rootNode);

            // if we have a trimloop, restore it
            TrimLoopSO trimSO = OG.Scene.FindSceneObjectsOfType<TrimLoopSO>().FirstOrDefault();
            if (trimSO != null) {
                OG.TransitionToState(OGWorkflow.SocketState);
                OGActions.AddNewTrimCurve(trimSO);
            }


            RestoreClientSocketData_End(rootNode);
        }
        protected virtual void RestoreClientSocketData_Start(XmlNode rootNode)
        {
        }
        protected virtual void RestoreClientSocketData_End(XmlNode rootNode)
        {
        }







        protected Vector2d TryParseVector2(string text)
        {
            if (text == null || text.Length == 0)
                throw new Exception("OGSerializer.ParseVector2: invalid input");
            Vector2d v = Vector2d.Zero;
            string[] tokens = text.Split(' ');
            if (tokens.Length != 2)
                throw new Exception("OGSerializer.ParseVector2: string [" + text + "] is not a 2-element vector");
            if (double.TryParse(tokens[0], out v.x) &&
                 double.TryParse(tokens[1], out v.y))
                return v;
            return Vector2d.Zero;
        }

        protected Vector3d TryParseVector3(string text)
        {
            if ( text == null || text.Length == 0 )
                throw new Exception("OGSerializer.ParseVector3: invalid input");
            Vector3d v = Vector3d.Zero;
            string[] tokens = text.Split(' ');
            if (tokens.Length != 3)
                throw new Exception("OGSerializer.ParseVector3: string [" + text + "] is not a 3-element vector");
            if (double.TryParse(tokens[0], out v.x) &&
                 double.TryParse(tokens[1], out v.y) &&
                 double.TryParse(tokens[2], out v.z))
                return v;
            return Vector3d.Zero;
        }


    }









}
