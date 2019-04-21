using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.ArcScene;
using System.Text;
using System.IO;
using System.Windows.Forms;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Editor;

        /// <summary>
        /// Occurs when this command is clicked
        /// </summary>
		
        public static BinaryReader binReader;
        public static Boolean transformatievlag = true, ZBoolean;

        public override void OnClick()
        {
            transformatievlag = TransformatieDialog();

            //Referenties
            ISxDocument pSxDoc = (ISxDocument) m_application.Document;
            IScene pScene = pSxDoc.Scene;
            IEnumLayer pEnumLayer = FeaturelayerReferentie(pScene);
            IEditor editor = EditorReferentie();

            //Bestand openen om binair te lezen
            binReader = new BinaryReader(File.Open(@"C:/3DGISBuffer.dat", FileMode.Open));

            //Zoek de juiste layer op
            ILayer pLayer = GaNaarLayer(pEnumLayer);


            //FeatureClass en Workspace
            IFeatureClass pFC = ((IFeatureLayer)pLayer).FeatureClass;
            IWorkspace pWorkspace = ((IDataset)pLayer).Workspace;

            //Bepaal of de layer z-waarden accepteert
            ZBoolean = LayerZ(pFC);

            //start edit sessie
            editor.StartEditing(pWorkspace);

            try
            {
                while (true)
                {
                    //Lees geometrietype
                    esriGeometryType geometrietype = (esriGeometryType)binReader.ReadInt32();

                    editor.StartOperation();

                    //Maak de feature aan
                    IFeatureCursor myFeatureCursor = pFC.Insert(true);
                    IFeatureBuffer pFeature = pFC.CreateFeatureBuffer();

                    //Lees attributen
                    LeesAttributen(pFeature);

                    //Koppel de geometrische vorm aan de featuredefinitie
                    switch (geometrietype)
                    {
                        case esriGeometryType.esriGeometryMultiPatch:
                            pFeature.Shape = MaakObjecten.MaakTrianglesMP();
                            break;
                        case esriGeometryType.esriGeometryPolygon:
                            pFeature.Shape = MaakObjecten.MaakRingenMP();
                            break;
                        case esriGeometryType.esriGeometryPolyline:
                            pFeature.Shape = MaakObjecten.MaakPolyline();
                            break;
                        case esriGeometryType.esriGeometryPoint:
                            pFeature.Shape = MaakObjecten.MaakPunt();
                            break;
                        default: MessageBox.Show("type:" + geometrietype.ToString() + " niet ondersteund");
                            break;
                    }

                    //Sla de feature op
                    myFeatureCursor.InsertFeature(pFeature);
                    myFeatureCursor.Flush();
                    editor.StopOperation(pLayer.Name);
                }
            }

            catch (EndOfStreamException e)
            {
                //Einde van het bestand
            }

            catch (Exception e)
            {
                MessageBox.Show("een probleem: " + e.ToString());
            }

            finally
            {
                // Sluit binReader
                binReader.Close();

                //Beeindig edit sessie en sla de wijzigingen op
                editor.StopEditing(true);

                //Refresh layers die van deze workspace gebruik maken
                IEnvelope envelope = null;
                IActiveView pActiveview = (IActiveView)pScene;
                pEnumLayer.Reset();
                pLayer = pEnumLayer.Next();
                while (pLayer != null)
                {
                    if (pWorkspace == ((IDataset)pLayer).Workspace)
                        pActiveview.PartialRefresh(esriViewDrawPhase.esriViewGeography, pLayer, envelope);
                    pLayer = pEnumLayer.Next();
                }

                MessageBox.Show("Gereed");
            }
        }

        //----------------------------------------------------------------------------------

        private IEnumLayer FeaturelayerReferentie(IScene pScene)
        {
            IUID pUID = new UIDClass();
            pUID.Value = "{40A9E885-5533-11D0-98BE-00805F7CED21}";
            return pScene.get_Layers((UID)pUID, true);

        }
        //----------------------------------------------------------------------------------

        private IEditor EditorReferentie()
        {
            UID uid = new UIDClass();
            uid.Value = "esriEditor.Editor";
            return m_application.FindExtensionByCLSID(uid) as IEditor;
        }

        //----------------------------------------------------------------------------------

        private Boolean TransformatieDialog()
        {
            var result = MessageBox.Show("Transformatie toepassen ?", "", MessageBoxButtons.YesNo);
            return (result == DialogResult.Yes);
        }

        //----------------------------------------------------------------------------------

        private ILayer GaNaarLayer(IEnumLayer pEnumLayer)
        {
            int lrindex = binReader.ReadInt32();
            pEnumLayer.Reset();
            ILayer pLayer = pEnumLayer.Next();
            for (int i = 0; i < lrindex; i++)
            {
                pLayer = pEnumLayer.Next();
            }
            return pLayer;
        }

        //----------------------------------------------------------------------------------

        private Boolean LayerZ(IFeatureClass pFC)
        {
            String shapeFieldName = pFC.ShapeFieldName;
            int shapeFieldIndex = pFC.FindField(shapeFieldName);
            IFields fields = pFC.Fields;
            IField shapeField = fields.get_Field(shapeFieldIndex);
            IGeometryDef geometryDef = shapeField.GeometryDef;
            return geometryDef.HasZ;
        }


        //----------------------------------------------------------------------------------

        private void LeesAttributen(IFeatureBuffer pFeature)
        {

            //Lees de attribuutgegevens uit de buffer
            IFields fs = pFeature.Fields;
            for (int j = 2; j < fs.FieldCount; j++)
            {
                string attrtype = Leesstring();
                switch (attrtype)
                {
                    case "Integer": pFeature.set_Value(j, binReader.ReadInt32()); break;
                    case "Float": pFeature.set_Value(j, binReader.ReadSingle()); break;
                    case "String": pFeature.set_Value(j, Leesstring()); break;
                    default: break; //gegevenstype nog niet ondersteund
                }
            }
        }

        //----------------------------------------------------------------------------------

        private string Leesstring()
        {
            StringBuilder sb = new StringBuilder();
            char ch = binReader.ReadChar();
            while (ch != '\0')
            {
                sb.Append(ch);
                ch = binReader.ReadChar();
            }
            return sb.ToString();
        }
