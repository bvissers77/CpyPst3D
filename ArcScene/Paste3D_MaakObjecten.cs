using System;
using ESRI.ArcGIS.Geometry;
using System.Windows.Forms;

    public class MaakObjecten
    {
        private static object _missing = Type.Missing;


        //----------------------------------------------------------------------------------

        public static IGeometry MaakRingenMP()
        {
            IGeometryCollection multiPatchGeometryCollection = new MultiPatchClass();
            IMultiPatch multiPatch = multiPatchGeometryCollection as IMultiPatch;
            IPointCollection RingPointCollection;

            //Lees aantal ringen
            int aantalringen = ImportInArcscene.binReader.ReadInt32();

            for (int i = 0; i < aantalringen; i++)
            {
                //Lees buitenringvlag
                Boolean buitenringvlag = leesboolean();
                
                //Ringtype definieren
                esriMultiPatchRingType ringtype; 
                if (buitenringvlag)
                { ringtype = esriMultiPatchRingType.esriMultiPatchOuterRing; }
                else
                { ringtype = esriMultiPatchRingType.esriMultiPatchInnerRing; }

                //Ring
                RingPointCollection = MaakRingPointCollection();
                multiPatchGeometryCollection.AddGeometry(RingPointCollection as IGeometry, ref _missing, ref _missing);
                multiPatch.PutRingType(RingPointCollection as IRing, ringtype);

            }
            return multiPatchGeometryCollection as IGeometry;
        }
        //----------------------------------------------------------------------------------

        
        //Deze method is nodig omdat MaxScript geen read/write-boolean functies kent.
        //Vanuit MaxScript schrijf ik booleans als bytes (1= true of 0=false).
        private static Boolean leesboolean()
        {
            return (ImportInArcscene.binReader.ReadByte() == 1);
        }

        //----------------------------------------------------------------------------------



        private static IPointCollection MaakRingPointCollection()
        {
            IPointCollection RingPointCollection = new RingClass();
            int vertexaantal = ImportInArcscene.binReader.ReadInt32();
            for (int j = 0; j < vertexaantal; j++)
            {
                double ptx = ImportInArcscene.binReader.ReadSingle();
                double pty = ImportInArcscene.binReader.ReadSingle();
                double ptz = ImportInArcscene.binReader.ReadSingle();
                RingPointCollection.AddPoint(PuntTransformatie(ptx, pty, ptz), ref _missing, ref _missing);
            }
            RingPointCollection.AddPoint(RingPointCollection.get_Point(0), ref _missing, ref _missing);

            return RingPointCollection;
        }

        //----------------------------------------------------------------------------------



        public static MultiPatch MaakTrianglesMP()
        {
            MultiPatch pMultiPatch = new MultiPatch();
            IGeometryCollection pGCol = (IGeometryCollection)pMultiPatch;
            IPointCollection pTriangles = new Triangles();
            IGeometry2 pGeom = (IGeometry2)pTriangles;
            int nfcs = ImportInArcscene.binReader.ReadInt32();

            for (int i = 1; i <= nfcs * 3; i++)
            {
                double ptx = ImportInArcscene.binReader.ReadSingle();
                double pty = ImportInArcscene.binReader.ReadSingle();
                double ptz = ImportInArcscene.binReader.ReadSingle();
                pTriangles.AddPoint(PuntTransformatie(ptx, pty, ptz), ref _missing, ref _missing);
            }

            pGCol.AddGeometry(pGeom, ref _missing, ref _missing);
            return pMultiPatch;
        }

        //----------------------------------------------------------------------------------

        public static IPolyline MaakPolyline()
        {
            //Declaraties en initialisaties
            IPolyline pPolyline = new PolylineClass();
            IGeometryCollection pGCol = (IGeometryCollection)pPolyline;
            IZAware pZAware = (IZAware)pPolyline;
            pZAware.ZAware = ImportInArcscene.ZBoolean;

            int aantalpaths = ImportInArcscene.binReader.ReadInt32();

            for (int i = 1; i <= aantalpaths; i++)
            {
                IPath pPath = new PathClass();
                IPointCollection pPointCol = (IPointCollection)pPath;

                int aantalvertices = ImportInArcscene.binReader.ReadInt32();

                for (int k = 1; k <= aantalvertices; k++)
                {
                    double ptx = ImportInArcscene.binReader.ReadSingle();
                    double pty = ImportInArcscene.binReader.ReadSingle();
                    double ptz = ImportInArcscene.binReader.ReadSingle();
                    pPointCol.AddPoint(PuntTransformatie(ptx, pty, ptz), ref _missing, ref _missing);
                }
                pGCol.AddGeometry(pPath, ref _missing, ref _missing);
            }
            return pPolyline;
        }

        //----------------------------------------------------------------------------------

        public static IPoint MaakPunt()
        {
            IPoint point = new PointClass();

            double ptx = ImportInArcscene.binReader.ReadSingle();
            double pty = ImportInArcscene.binReader.ReadSingle();
            double ptz = ImportInArcscene.binReader.ReadSingle();

            point = PuntTransformatie(ptx, pty, ptz);
            IZAware pZAware = (IZAware)point;
            pZAware.ZAware = ImportInArcscene.ZBoolean;

            return point;
        }


        //----------------------------------------------------------------------------------

        public static IPoint PuntTransformatie(double x, double y, double z)
        {
            IPoint point = new PointClass();

            if (ImportInArcscene.transformatievlag)
            {
                //De transformatieinstellingen
                const double lokaalnulpuntx = 121289.57465572;
                const double lokaalnulpunty = 486979.75115863;
                const double theta = 0.142274476794895;

                //Rotatie
                double tX = x * Math.Cos(-theta) - y * Math.Sin(-theta);
                double tY = x * Math.Sin(-theta) + y * Math.Cos(-theta);

                //Translatie
                double nX = tX + lokaalnulpuntx;
                double nY = tY + lokaalnulpunty;

                point.PutCoords(nX, nY);
            }
            else
            {
                point.PutCoords(x, y);

            }

            if (ImportInArcscene.ZBoolean) point.Z = z;
            return point;
        }

    
    }
