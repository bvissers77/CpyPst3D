using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.ArcScene;
using System.IO;
using System.Windows.Forms;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;

/// <summary>
/// Occurs when this command is clicked
/// </summary>


        public static BinaryWriter binWriter;
        public static Boolean transformatievlag = true, simpelpolygonvlag = false, ZBoolean;

        
        public override void OnClick()
        {
            //Initialisatie ArcScene-document en Scene
            ISxDocument pSxDoc = (ISxDocument) m_application.Document;
            IScene pScene = pSxDoc.Scene;

            //Deze vlag geeft aan of transformatie tussen Rijksdriehoekstelsel
            //en lokaal meetsysteem moet plaatsvinden
            transformatievlag = TransformatieDialog();


            //Enumeratie FeatureLayers 
            IUID pUID = new UIDClass();
            pUID.Value = "{40A9E885-5533-11D0-98BE-00805F7CED21}";
            IEnumLayer pEnumLayer = pScene.get_Layers((UID)pUID, true);
            pEnumLayer.Reset();
            ILayer layer = pEnumLayer.Next();

            try
            {
                //Bestand openen om binair te schrijven
                binWriter = new BinaryWriter(File.Open(@"C:/3DGISBuffer.dat", FileMode.Open));

                //Geef aantal features door
                IFeatureSelection pFeatSelection;
                Int32 cnt = 0;
                while (!(layer == null))
                {
                    pFeatSelection = (IFeatureSelection)layer;
                    cnt += pFeatSelection.SelectionSet.Count;
                    layer = pEnumLayer.Next();
                }
                pEnumLayer.Reset();
                layer = pEnumLayer.Next();
                binWriter.Write(cnt); //Int32


                //Initialisatie van de Statusbar
                IStatusBar pStatusBar = m_application.StatusBar;
                IStepProgressor pProgbar = pStatusBar.ProgressBar;
                pProgbar.Position = 0;
                pStatusBar.ShowProgressBar("Bezig...", 0, cnt, 1, true);

                //Loop door alle featurelayers
                int lrindex = 0;
                while (!(layer == null))
                {
                    //Loop door de geselecteerde features
                    pFeatSelection = (IFeatureSelection)layer;
                    ISelectionSet pSelectionSet = pFeatSelection.SelectionSet;
                    ICursor mijncursor;
                    pSelectionSet.Search(null, false, out mijncursor);
                    IFeatureCursor pFeatCursor = (IFeatureCursor)mijncursor;
                    IFeature pFeat = pFeatCursor.NextFeature();

                    //Vraag als nodig of polygonen gaten kunnen bevatten
                    if (!(pFeat == null) && pFeat.Shape.GeometryType == esriGeometryType.esriGeometryPolygon)
                    { simpelpolygonvlag = SimpelPolygonDialog(); }

                    while (!(pFeat == null))
                    {
                        //Bepaal of feature-definitie z-waarden accepteert
                        IZAware pZAware = (IZAware)pFeat.Shape;
                        ZBoolean = pZAware.ZAware;

                        //Geef de layer-index door
                        binWriter.Write(lrindex);

                        //Geef het geometrytype door
                        SchrijfGeometryType(pFeat.Shape.GeometryType);

                        //Geef attribuutgegevens door
                        SchrijfAttributen(pFeat);

                        //Geef de kleur door
                        SchrijfKleur(layer, pFeat);

                        //Schrijf de feature naar het bestand
                        switch (pFeat.Shape.GeometryType)
                        {
                            case esriGeometryType.esriGeometryMultiPatch:
                                IGeometryCollection GeoColl = (IGeometryCollection)pFeat.ShapeCopy;
                                if (GeoColl.get_Geometry(0).GeometryType == esriGeometryType.esriGeometryRing)
                                    SchrijfFeatures.SchrijfRingenMP(pFeat);
                                else
                                    SchrijfFeatures.SchrijfTrianglesMP(pFeat);
                                break;
                            case esriGeometryType.esriGeometryPoint: SchrijfFeatures.SchrijfPoint(pFeat); break;
                            case esriGeometryType.esriGeometryMultipoint: SchrijfFeatures.SchrijfMultipoint(pFeat); break;
                            case esriGeometryType.esriGeometryPolygon:
                                if (simpelpolygonvlag)
                                { SchrijfFeatures.SchrijfPolygonSimpel(pFeat); }
                                else
                                { SchrijfFeatures.SchrijfPolygon(pFeat); }
                                break;
                            case esriGeometryType.esriGeometryPolyline: SchrijfFeatures.SchrijfPolyline(pFeat); break;
                            default: /*Geometrie niet ondersteund*/ break;
                        }

                        //progressbar updaten en nieuwe feature uit selectie kiezen
                        pStatusBar.StepProgressBar();
                        pFeat = pFeatCursor.NextFeature();

                    }//while 
                    layer = pEnumLayer.Next();
                    lrindex++;
                }//while

                pStatusBar.HideProgressBar();
                binWriter.Close();
            }

            catch (IOException e)
            {
                MessageBox.Show("Export niet mogelijk door I/O probleem");
            }

            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }

                //----------------------------------------------------------------------------------

        private Boolean TransformatieDialog()
        {
            var result = MessageBox.Show("Transformatie toepassen ?", "", MessageBoxButtons.YesNo);
            return (result == DialogResult.Yes);
        }

        //----------------------------------------------------------------------------------

        private Boolean SimpelPolygonDialog()
        {
            var result = MessageBox.Show("Polygonen zonder gaten ?", "", MessageBoxButtons.YesNo);
            return (result == DialogResult.Yes);
        }


        //----------------------------------------------------------------------------------

        private void Schrijfstring(string str)
        {
            char[] b = new char[str.Length+1];
            StringReader sr = new StringReader(str);
            sr.Read(b, 0, str.Length);
            b[str.Length] = '\0';
            binWriter.Write(b);
        }

        //----------------------------------------------------------------------------------

        private void SchrijfKleur(ILayer layer, IFeature pFeat)
        {
            IGeoFeatureLayer pGeoLayer = (IGeoFeatureLayer)layer;
            IFeatureRenderer pFeatRend = pGeoLayer.Renderer;
            IFeatureClass pFeatClass = (IFeatureClass)pGeoLayer.FeatureClass;
            IQueryFilter pQF = new QueryFilter();
            IRgbColor pRgbColor = new RgbColorClass();
            pFeatRend.PrepareFilter(pFeatClass, pQF);

            switch(pFeat.Shape.GeometryType)
            {
                case esriGeometryType.esriGeometryMultipoint:
                    IMarkerSymbol pMarkerSymbol = (IMarkerSymbol)pFeatRend.get_SymbolByFeature(pFeat);
                    pRgbColor.RGB = pMarkerSymbol.Color.RGB;
                    break;

                case esriGeometryType.esriGeometryPoint:
                    IMarkerSymbol pMarkerSymbol2 = (IMarkerSymbol)pFeatRend.get_SymbolByFeature(pFeat);
                    pRgbColor.RGB = pMarkerSymbol2.Color.RGB;
                    break;

                case esriGeometryType.esriGeometryPolyline:
                    ILineSymbol pLineSym = (ILineSymbol)pFeatRend.get_SymbolByFeature(pFeat);
                    pRgbColor.RGB = pLineSym.Color.RGB;
                    break;

                default: 
                    IFillSymbol pFillSym = (IFillSymbol)pFeatRend.get_SymbolByFeature(pFeat);
                    pRgbColor.RGB = pFillSym.Color.RGB;
                    break;
        }
           
            binWriter.Write(pRgbColor.Red); //int
            binWriter.Write(pRgbColor.Green); //int 
            binWriter.Write(pRgbColor.Blue); //int
        }

        //----------------------------------------------------------------------------------

        private void SchrijfGeometryType(esriGeometryType geometrytype)
        {
            int code=0;
            switch (geometrytype)
            {
                case esriGeometryType.esriGeometryNull: code = 0; break;
                case esriGeometryType.esriGeometryPoint: code = 1; break;
                case esriGeometryType.esriGeometryMultipoint: code = 2; break;
                case esriGeometryType.esriGeometryPolyline: code = 3; break;
                case esriGeometryType.esriGeometryPolygon: code = 4; break;
                case esriGeometryType.esriGeometryEnvelope: code = 5; break;
                case esriGeometryType.esriGeometryPath: code = 6; break;
                case esriGeometryType.esriGeometryAny: code = 7; break;
                case esriGeometryType.esriGeometryMultiPatch: code = 9; break;
                case esriGeometryType.esriGeometryRing: code = 11; break;
                case esriGeometryType.esriGeometryLine: code = 13; break;
                case esriGeometryType.esriGeometryCircularArc: code = 14; break;
                case esriGeometryType.esriGeometryBezier3Curve: code = 15; break;
                case esriGeometryType.esriGeometryEllipticArc: code = 16; break;
                case esriGeometryType.esriGeometryBag: code = 17; break;
                case esriGeometryType.esriGeometryTriangleStrip: code = 18; break;
                case esriGeometryType.esriGeometryTriangleFan: code = 19; break;
                case esriGeometryType.esriGeometryRay: code = 20; break;
                case esriGeometryType.esriGeometrySphere: code = 21; break;
            }
            binWriter.Write(code);
        }

        //----------------------------------------------------------------------------------

        private void SchrijfAttributen(IFeature pFeat)
        {
            Type type;
            TypeCode typeCode; 
            IFields fs = pFeat.Fields;
            string veldnaam;

            //Schrijf attributen-aantal weg
            binWriter.Write(fs.FieldCount-2); //int

            for (int j = 2; j < fs.FieldCount; j++)
            {
                veldnaam = fs.get_Field(j).Name;
                var waarde = pFeat.get_Value(j);
                type = waarde.GetType();
                typeCode = Type.GetTypeCode(type);
                
                //Schrijf veldnaam weg
                Schrijfstring(veldnaam); 

                //Schrijf typecode weg
                binWriter.Write((int)typeCode);              
                
                //Schrijf waarde weg
                switch (typeCode)
                {   case TypeCode.Single: binWriter.Write((Single) waarde); break;
                    case TypeCode.Double: binWriter.Write((Double)waarde); break;
                    case TypeCode.Int16: binWriter.Write((Int16)waarde); break;
                    case TypeCode.Int32: binWriter.Write((Int32) waarde); break;
                    case TypeCode.String: Schrijfstring((String)waarde); break;
                    default: /* gegevenstype nog niet ondersteund */ break;
                }
            }
        }