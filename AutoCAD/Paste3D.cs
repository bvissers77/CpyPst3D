/* 
Deze Class-file bouwt het commando "ImportMesh".
Daarbij worden meshes uit 3dsMax geimporteerd in de vorm van AutoCAD-surfaces of AutoCAD-3DSolids. 
De objectnamen worden als XRecords toegevoegd. 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Reflection;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

[assembly: CommandClass(typeof(ImportUit3ds.Class1))]

namespace ImportUit3ds
{
    public class Class1
    {
		BinaryReader binReader;
	
        [CommandMethod("ImportUit3ds")]
        public void ImportUit3ds()
        {
            // Verkrijg document en database
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            acCurDb.Surfu = 0;
            acCurDb.Surfv = 0;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open de block-tabel
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                
                // Open de Model space om in te schrijven
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                //Bestand openen om binair te lezen
                binReader = new BinaryReader(File.Open(@"C:/3DGISBuffer.dat", FileMode.Open));

                //Loop door objecten
                int aantalobjecten = binReader.ReadInt32();
                for (int i = 0; i < aantalobjecten; i++)
                {
                    Point3dCollection vertarray = new Point3dCollection();
                    Int32Collection facearray = new Int32Collection();

                    // Maak een subdivision-mesh aan
                    SubDMesh sdm = new SubDMesh();
                    sdm.SetDatabaseDefaults();

                    // Voeg het object toe aan de block-tabel
                    acBlkTblRec.AppendEntity(sdm);
                    acTrans.AddNewlyCreatedDBObject(sdm, true);

                    //objectkleur lezen
                    byte kleur_r = binReader.ReadByte();
                    byte kleur_g = binReader.ReadByte();
                    byte kleur_b = binReader.ReadByte();

                    //solidvlag lezen
                    Boolean solidvlag = LeesBoolean();

                    // Vertexarray vullen met vertices
                    Point3dCollection acPts3dPFMesh = new Point3dCollection();
                    Int32 nvts = binReader.ReadInt32();

                    for (int j = 1; j <= nvts; j++)
                    {
                        Single nX = binReader.ReadSingle();
                        Single nY = binReader.ReadSingle();
                        Single nZ = binReader.ReadSingle();
                        vertarray.Add(new Point3d(nX, nY, nZ));
                    }

                    //Facearray vullen met faces
                    int nfcs = binReader.ReadInt32();
                    for (int j = 1; j <= nfcs; j++)
                    {
                        int fc1 = binReader.ReadInt32();
                        int fc2 = binReader.ReadInt32();
                        int fc3 = binReader.ReadInt32();

                        facearray.Add(3);
                        facearray.Add(fc1 - 1);
                        facearray.Add(fc2 - 1);
                        facearray.Add(fc3 - 1);
                    }

                    //Vertex- en facearray toevoegen aan mesh, smoothlevel 0
                    sdm.SetSubDMesh(vertarray, facearray, 0);

                    Entity pMijnObj = null;
                    if (solidvlag)
                    {
                        Autodesk.AutoCAD.DatabaseServices.Solid3d pSolid = sdm.ConvertToSolid(false, false);
                        pMijnObj = (Entity)pSolid;
                    }
                    else
                    {
                        Autodesk.AutoCAD.DatabaseServices.Surface pSurface = sdm.ConvertToSurface(false, false);
                        pMijnObj = (Entity)pSurface;
                    }
                    acBlkTblRec.AppendEntity(pMijnObj);
                    acTrans.AddNewlyCreatedDBObject(pMijnObj, true);

                    //Verwijder mesh
                    sdm.Erase();

                    
                    // Schrijf objectnaam naar Xrecord in de entity-dictionary van de surface
                    SaveXRecord(pMijnObj, LeesAttributen(), "GISData", acTrans);

                    //kleur van het object updaten
                    pMijnObj.Color = Color.FromRgb(kleur_r, kleur_g, kleur_b);


                }//einde for-loop

                // Schrijf het object naar de AutoCAD-database en sluit binReader
                acTrans.Commit();
                binReader.Close();
            }//einde using transaction
        }//einde method

        //----------------------------------------------------------------------------------

        private static ObjectId SaveXRecord (DBObject o, ResultBuffer buffer, string key, Transaction trans)
        {
            if (o.ExtensionDictionary == ObjectId.Null)
            {
                o.CreateExtensionDictionary();
            }

            using (DBDictionary dict = trans.GetObject(o.ExtensionDictionary, OpenMode.ForWrite, false) as DBDictionary)
            {
                if (dict.Contains(key))
                {
                    Xrecord xRecord = (Xrecord)trans.GetObject(dict.GetAt(key), OpenMode.ForWrite);
                    xRecord.Data = buffer;
                    return xRecord.ObjectId;
                }
                else
                {
                    Xrecord xRecord = new Xrecord();
                    xRecord.Data = buffer;

                    dict.SetAt(key, xRecord);
                    trans.AddNewlyCreatedDBObject(xRecord, true);
                    return xRecord.ObjectId;
                }
            }
        }


        //----------------------------------------------------------------------------------

        private ResultBuffer LeesAttributen()
        {
            ResultBuffer MijnResultBuffer = new ResultBuffer();

            //Lees attributenaantal
            int attributenaantal = binReader.ReadInt32();

            //Lees de attribuutgegevens uit de buffer
            for (int i = 0; i < attributenaantal; i++)
            {
                //Lees attrnaam
                string attrnaam = Leesstring();
                MijnResultBuffer.Add(new TypedValue((int)DxfCode.Text, attrnaam));

                //Lees attrtype
                string attrtype = Leesstring();

                //Lees attrwaarde
                switch (attrtype)
                {
                    case "Integer": MijnResultBuffer.Add(new TypedValue((int)DxfCode.Int32, binReader.ReadInt32())); break;
                    case "Float": MijnResultBuffer.Add(new TypedValue((int)DxfCode.Real, binReader.ReadSingle())); break;
                    case "String": MijnResultBuffer.Add(new TypedValue((int)DxfCode.Text, Leesstring())); break;
                    default: break; //gegevenstype nog niet ondersteund
                }
            }
            
            return MijnResultBuffer;
        }

        //----------------------------------------------------------------------------------

        //Deze method is nodig omdat C# (.NET) strings op een andere manier opbouwt/leest dan C++ of MaxScript.
        //In MaxScript (en C++) eindigt een string intern in het geheugen of op schijf met een nulkarakter: '\0'.
        //In C# (.NET) wordt een string in het geheugen daarentegen voorafgegegaan door een aantal bits waarin de lengte van de string is opgeslagen.

        public string Leesstring()
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

        //----------------------------------------------------------------------------------
        
        public Boolean LeesBoolean()
        {
            return (binReader.ReadByte() == 1);
        }


        //----------------------------------------------------------------------------------
        
        [CommandMethod("RegisterImportUit3ds")]
        public void RegisterImportUit3ds()
        {
            string sProdKey = HostApplicationServices.Current.RegistryProductRootKey;
            string sAppName = "ImportUit3ds";

            RegistryKey regAcadProdKey = Registry.CurrentUser.OpenSubKey(sProdKey);
            RegistryKey regAcadAppKey = regAcadProdKey.OpenSubKey("Applications", true);

            string[] subKeys = regAcadAppKey.GetSubKeyNames();
            foreach (string subKey in subKeys)
            {
                if (subKey.Equals(sAppName))
                {
                    regAcadAppKey.Close();
                    return;
                }
            }

            string sAssemblyPath = Assembly.GetExecutingAssembly().Location;
            RegistryKey regAppAddInKey = regAcadAppKey.CreateSubKey(sAppName);
            regAppAddInKey.SetValue("DESCRIPTION", sAppName, RegistryValueKind.String);
            regAppAddInKey.SetValue("LOADCTRLS", 14, RegistryValueKind.DWord);
            regAppAddInKey.SetValue("LOADER", sAssemblyPath, RegistryValueKind.String);
            regAppAddInKey.SetValue("MANAGED", 1, RegistryValueKind.DWord);
            regAcadAppKey.Close();
        }

        //----------------------------------------------------------------------------------------------------------------

        [CommandMethod("UnregisterImportUit3ds")]
        public void UnregisterImportUit3ds()
        {
            string sProdKey = HostApplicationServices.Current.RegistryProductRootKey;
            string sAppName = "ImportUit3ds";
            RegistryKey regAcadProdKey = Registry.CurrentUser.OpenSubKey(sProdKey);
            RegistryKey regAcadAppKey = regAcadProdKey.OpenSubKey("Applications", true);
            regAcadAppKey.DeleteSubKeyTree(sAppName);
            regAcadAppKey.Close();
        }

    
    }//class
}//namespace
