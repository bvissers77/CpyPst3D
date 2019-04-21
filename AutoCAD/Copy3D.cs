/* 
Deze Class-file bouwt het commando "Naar3dsMax".
Daarbij worden AutoCAD-surfaces of AutoCAD-3DSolids weggeschreven als 3dsMax-meshes. 
Attribuutgegevens worden uit de XRecords gelezen.
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
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

[assembly: CommandClass(typeof(Naar3dsMax.Class1))]

namespace Naar3dsMax
{
    public class Class1
    {
        static BinaryWriter binWriter;

        [CommandMethod("Naar3dsMax")]
        public static void Naar3dsMax()
        {
            // Document en database
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            try
            {

                //Bestand openen om binair te schrijven
                binWriter = new BinaryWriter(File.Open(@"C:/3DGISBuffer.dat", FileMode.Create));

                // Begin transactie
                using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                {
                    // Vraag gebruiker om objecten te selecteren
                    PromptSelectionResult acSSPrompt = acDoc.Editor.GetSelection();

                    // Prompt status geeft aan of objecten geselecteerd zijn
                    if (acSSPrompt.Status == PromptStatus.OK)
                    {
                        //De 'selection set'
                        SelectionSet acSSet = acSSPrompt.Value;

                        //Schrijf objectenaantal
                        int objectenaantal = acSSet.Count;
                        binWriter.Write(objectenaantal);

                        // Loop door de objecten in de 'selection set'
                        foreach (SelectedObject acSSObj in acSSet)
                        {
                            if (acSSObj != null)
                            {
                                // Open de geselecteerde 'entity' om te lezen
                                Entity acEnt = acTrans.GetObject(acSSObj.ObjectId, OpenMode.ForRead) as Entity;
                                if (acEnt != null)
                                {
                                    //Open de XRecord "GISData"
                                    Xrecord mgrXRec = LeesXRecord(acEnt, "GISData", acTrans);

                                    //Lees de attributen
                                    SchrijfAttributen(mgrXRec);

                                    //Schrijf object weg
                                    SchrijfObject(acEnt);

                                    //Schrijf kleur weg
                                    binWriter.Write(acEnt.Color.Red);
                                    binWriter.Write(acEnt.Color.Green);
                                    binWriter.Write(acEnt.Color.Blue);
                                }
                            }
                        }//foreach
                    }//if
                }//using
                binWriter.Close();
            }//try

            catch (IOException e)
            {
                MessageBox.Show("Export niet mogelijk door I/O probleem");
            }

            catch (System.Exception e)
            {
                MessageBox.Show(e.ToString());
            }




        }//method

        //----------------------------------------------------------------------------------------------------------------
        private static void SchrijfAttributen(Xrecord xr)
        {
            ResultBuffer buffer = xr.Data;
            TypedValue[] tvs = buffer.AsArray();

            //Schrijf attributenaantal
            int attributenaantal = tvs.Count() / 2;
            binWriter.Write(attributenaantal);

            for (int i = 0; i < attributenaantal; i++)
            {
                //Schrijf attrnaam
                string attrnaam = tvs[2 * i].Value.ToString();
                Schrijfstring(attrnaam);

                //Schrijf attrtype en attrwaarde
                TypedValue typedRecord = tvs[2*i+1];
                short typecode = typedRecord.TypeCode;
                switch (typecode)
                {
                    case (int)DxfCode.Int32:
                        Schrijfstring("Integer");
                        binWriter.Write((Int32) typedRecord.Value);
                        break;
                    case (int)DxfCode.Real: 
                        Schrijfstring("Float"); 
                        binWriter.Write(Convert.ToSingle(typedRecord.Value));
                        break;
                    case (int)DxfCode.Text:
                        Schrijfstring("String");
                        Schrijfstring((string) typedRecord.Value);
                        break;
                    default: break; //gegevenstype nog niet ondersteund
                }

                //Schrijf attrwaarde
                var attrwaarde = typedRecord.Value;
            }
        }

        //----------------------------------------------------------------------------------------------------------------

        private static void SchrijfObject(Entity acEnt)
        {
            MeshDataCollection meshData = SubDMesh.GetObjectMesh(acEnt, new MeshFaceterData());
            Point3dCollection vertexarray = meshData.VertexArray;
            Int32Collection facearray = meshData.FaceArray;
            
            //Schrijf aantalvertices
            int aantalvertices = vertexarray.Count;
            binWriter.Write(aantalvertices);

            //Schrijf vertices weg
            for (int i = 0; i < aantalvertices; i++)
            {
                Point3d punt = vertexarray[i];
                binWriter.Write(Convert.ToSingle(punt.X));
                binWriter.Write(Convert.ToSingle(punt.Y));
                binWriter.Write(Convert.ToSingle(punt.Z));
            }

            //Schrijf facearraycount
            int facearraycount = facearray.Count;
            binWriter.Write(facearraycount);

            //Schrijf facearray weg
            for (int i = 0; i < facearraycount; i++)
            {
                binWriter.Write(facearray[i]);
            }
        }

        //----------------------------------------------------------------------------------------------------------------

        private static void Schrijfstring(string str)
        {
            char[] b = new char[str.Length + 1];
            StringReader sr = new StringReader(str);
            sr.Read(b, 0, str.Length);
            b[str.Length] = '\0';
            binWriter.Write(b);
        }



        //----------------------------------------------------------------------------------------------------------------

        
        private static Xrecord LeesXRecord(DBObject o, string key, Transaction trans)
        {

            using (DBDictionary dict = trans.GetObject(o.ExtensionDictionary, OpenMode.ForRead, false) as DBDictionary)
            {
                Xrecord xRecord = (Xrecord)trans.GetObject(dict.GetAt(key), OpenMode.ForRead);
                return xRecord;
            }
        }

        //----------------------------------------------------------------------------------------------------------------

        [CommandMethod("RegisterNaar3dsMax")]
        public void RegisterNaar3dsMax()
        {
            string sProdKey = HostApplicationServices.Current.RegistryProductRootKey;
            string sAppName = "Naar3dsMax";
  
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

        [CommandMethod("UnregisterNaar3dsMax")]
        public void UnregisterNaar3dsMax()
        {
            string sProdKey = HostApplicationServices.Current.RegistryProductRootKey;
            string sAppName = "Naar3dsMax";
            RegistryKey regAcadProdKey = Registry.CurrentUser.OpenSubKey(sProdKey);
            RegistryKey regAcadAppKey = regAcadProdKey.OpenSubKey("Applications", true);
            regAcadAppKey.DeleteSubKeyTree(sAppName);
            regAcadAppKey.Close();
        }

    
    }//class
}//namespace
