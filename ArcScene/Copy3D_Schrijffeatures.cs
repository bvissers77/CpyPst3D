using System;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

    class SchrijfFeatures
    {

        
        public static void SchrijfTrianglesMP(IFeature pFeat)
        {
            IMultiPatch pMultiPatch = (IMultiPatch)pFeat.ShapeCopy;
            IGeometryCollection GeoColl = (IGeometryCollection)pMultiPatch;

            //Schrijf TrianglesVlag = true
            Boolean TrianglesVlag = true;
            WriteBoolean(TrianglesVlag);

            //Schrijf aantal parts
            int aantalparts = GeoColl.GeometryCount;
            Naar3dsMax.binWriter.Write(aantalparts); //int

            //Loop door de onderdelen
            for (int i = 0; i < aantalparts; i++)
            {
                IPointCollection pPartPoints = (IPointCollection)GeoColl.get_Geometry(i);

                //Schrijf aantal punten
                int aantalpunten = pPartPoints.PointCount;
                Naar3dsMax.binWriter.Write(aantalpunten); //int

                //Loop door de vertices van de onderdelen
                for (int n = 0; n < aantalpunten; n++)
                {
                    SchrijfTransformatie(pPartPoints.get_Point(n));
                }
            }
        }

        //----------------------------------------------------------------------------------

        public static void SchrijfRingenMP(IFeature pFeat)
        {
            IMultiPatch pMultiPatch = (IMultiPatch)pFeat.ShapeCopy;
            IGeometryCollection GeoColl = (IGeometryCollection)pMultiPatch;
            
            //Schrijf TrianglesVlag = false
            Boolean TrianglesVlag = false;
            WriteBoolean(TrianglesVlag);

            //Schrijf aantalringen
            int aantalringen = GeoColl.GeometryCount;
            Naar3dsMax.binWriter.Write(aantalringen);

            //Loop door de ringencollectie
            for (int i = 0; i < aantalringen; i++)
            {
                IRing pRing = (IRing) GeoColl.get_Geometry(i);
                Boolean isbeginning = true;
                esriMultiPatchRingType ringtype = pMultiPatch.GetRingType(pRing, ref isbeginning);
                
                //Schrijf Buitenringvlag
                Boolean Buitenringvlag = (ringtype == esriMultiPatchRingType.esriMultiPatchOuterRing);
                WriteBoolean(Buitenringvlag);
                SchrijfRing(pRing);
            }

            
        }

        //----------------------------------------------------------------------------------


        //Deze constructie is nodig omdat MaxScript geen readboolean-functie kent
        private static void WriteBoolean(Boolean b)
        {
            if (b)
            {
                { Naar3dsMax.binWriter.Write((byte)1); }
            }
            else
            { Naar3dsMax.binWriter.Write((byte)0); }
        }


        //----------------------------------------------------------------------------------

        public static void SchrijfPoint(IFeature pFeat)
        {
            IPoint point;
            point = (IPoint)pFeat.ShapeCopy;
            SchrijfTransformatie(point);
        }

        //----------------------------------------------------------------------------------

        public static void SchrijfMultipoint(IFeature pFeat)
        {
            IMultipoint pMultipoint = (IMultipoint)pFeat.ShapeCopy;
            IGeometryCollection GeoColl = (IGeometryCollection)pMultipoint;
            IPoint point;

            //Schrijf puntenaantal
            int puntenaantal = GeoColl.GeometryCount;
            Naar3dsMax.binWriter.Write(puntenaantal); //int

            //Loop door de punten
            for (int i = 0; i < puntenaantal; i++)
            {
                point = (IPoint)GeoColl.get_Geometry(i);
                SchrijfTransformatie(point);
            }

        }

        //----------------------------------------------------------------------------------

        public static void SchrijfPolygon(IFeature pFeat)
        {
            IPolygon4 pPolygon = (IPolygon4)pFeat.ShapeCopy;

            //Schrijf aantal ringen
            IGeometryCollection GeoColl = (IGeometryCollection)pPolygon;
            int aantalringen = GeoColl.GeometryCount;
            Naar3dsMax.binWriter.Write(aantalringen); //int

            //Loop door Buitenringen
            IGeometryBag BuitenringenVerzameling = pPolygon.ExteriorRingBag;
            IGeometryCollection Buitenringen = (IGeometryCollection)BuitenringenVerzameling;
            int aantalBuitenringen = Buitenringen.GeometryCount;
            for (int i = 0; i < aantalBuitenringen; i++)
            {
                //Schrijf Buitenringvlag = true
                Boolean Buitenringvlag = true;
                WriteBoolean(Buitenringvlag);
                
                //Schrijf Buitenring
                IGeometry Buitenring = Buitenringen.get_Geometry(i);
                SchrijfRing(Buitenring);

                //Loop door Binnenringen
                IGeometryBag BinnenringenVerzameling = pPolygon.get_InteriorRingBag(Buitenring as IRing);
                IGeometryCollection Binnenringen = BinnenringenVerzameling as IGeometryCollection;
                int aantalBinnenringen = Binnenringen.GeometryCount;
                for (int k = 0; k < aantalBinnenringen; k++)
                {
                    //Schrijf Buitenringvlag = false
                    Buitenringvlag = false;
                    WriteBoolean(Buitenringvlag);

                    //Schrijf Binnenring
                    IGeometry Binnenring = Binnenringen.get_Geometry(k);
                    SchrijfRing(Binnenring);
                }
            }
        }

        //----------------------------------------------------------------------------------

        public static void SchrijfPolygonSimpel(IFeature pFeat)
        {
            IPolygon4 pPolygon = (IPolygon4)pFeat.ShapeCopy;
            IGeometryCollection GeoColl = (IGeometryCollection)pPolygon;
            
            //Schrijf aantal ringen
            int aantalringen = GeoColl.GeometryCount;
            Naar3dsMax.binWriter.Write(aantalringen); //int

            for (int i = 0; i < aantalringen; i++)
            {
                //Schrijf Buitenringvlag = true
                Boolean Buitenringvlag = true;
                WriteBoolean(Buitenringvlag);

                //Schrijf Ring
                IGeometry Ring = GeoColl.get_Geometry(i);
                SchrijfRing(Ring);

            }
        }


        //----------------------------------------------------------------------------------
        
        
        
        private static void SchrijfRing(IGeometry pRing)
        {
            IPointCollection punten = pRing as IPointCollection;

            //Schrijf puntenaantal van ring
            int puntenaantal = punten.PointCount - 1; //laatste punt = eerste punt 
            Naar3dsMax.binWriter.Write(puntenaantal); //int

            //Schrijf punten van ring
            for (int j = 0; j < puntenaantal; j++)
            {
                SchrijfTransformatie(punten.get_Point(j));
            }

        }

        //----------------------------------------------------------------------------------

        public static void SchrijfPolyline(IFeature pFeat)
        {

            IPolyline pPolyline = (IPolyline)pFeat.ShapeCopy;
            IGeometryCollection GeoColl = (IGeometryCollection)pPolyline;
            IPointCollection pPartPoints;

            //Schrijf aantal onderdelen
            int aantalonderdelen = GeoColl.GeometryCount;
            Naar3dsMax.binWriter.Write(aantalonderdelen); //int

            //Loop door de onderdelen
            for (int i = 0; i < aantalonderdelen; i++)
            {
                pPartPoints = (IPointCollection)GeoColl.get_Geometry(i);

                //Schrijf puntenaantal
                int puntenaantal = pPartPoints.PointCount;
                Naar3dsMax.binWriter.Write(puntenaantal); //int

                //Loop door de vertices van de onderdelen
                for (int n = 0; n < puntenaantal; n++)
                {
                    SchrijfTransformatie(pPartPoints.get_Point(n));
                }
            }
        }

        //----------------------------------------------------------------------------------

        public static void SchrijfTransformatie(IPoint ipt)
        {
            //Declaraties
            double tX, tY, nX, nY;

            if (Naar3dsMax.transformatievlag)
            {//De transformatieinstellingen
                const double lokaalnulpuntx = 121289.57465572;
                const double lokaalnulpunty = 486979.75115863;
                const double theta = 0.142274476794895;

                //Translatie
                tX = ipt.X - lokaalnulpuntx;
                tY = ipt.Y - lokaalnulpunty;

                //Rotatie
                nX = tX * Math.Cos(theta) - tY * Math.Sin(theta);
                nY = tX * Math.Sin(theta) + tY * Math.Cos(theta);

                Naar3dsMax.binWriter.Write((Single)nX);
                Naar3dsMax.binWriter.Write((Single)nY);
            }
            else
            {
                Naar3dsMax.binWriter.Write((Single)ipt.X);
                Naar3dsMax.binWriter.Write((Single)ipt.Y);
            }

            if (Naar3dsMax.ZBoolean) Naar3dsMax.binWriter.Write((Single)ipt.Z);
            else Naar3dsMax.binWriter.Write((Single)0);
        }

    }
