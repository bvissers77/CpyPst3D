-- *** VAN ARCSCENE NAAR 3DSMAX ***
-- Deze macro leest attribuutgegevens en vertices van features en hun onderdelen uit 3DGISBuffer.dat.
-- Op basis van de vertices bouwt hij 3dsMax-objecten.
-- De attribuutgegevens (inclusief layerindex en geometrietype) worden aan de objecten gekoppeld als 'custom attributes'. 
-- Deze 'custom attributes' worden ook in een aparte 'rollout' bij het object geplaatst.
-- De macro gaat uit van de volgende algemene bestandsopbouw:
---------------------------------------------------------------------------
-- 	feature_aantal 								(int32)
-- 		layerindex								(int32)
--		geometrietype							(int32)
-- 		attributenaantal						(int32)
--			attrnaam							(string met '\0')
--			typecode							(int32)
--			attrwaarde							(type afhankelijk van typecode) 						
--		kleur, rood								(int32)	
--		kleur, groen							(int32)	
--		kleur, blauw							(int32)	
--		(onderdelenaantal)						(int32)
--			vertex,x							(single float)
--			vertex,y							(single float)
--			vertex,z							(single float)
---------------------------------------------------------------------------
-- Opmerkingen:
-- 1: Ondersteunde types voor attribuutgegevens zijn: Integer (int32), Float (single float) en String.
-- 2: Double floats in de attribuutdata worden opgeslagen als single float !!!!
-- 3: Door via de maxscript-listener de variabele meshvlag=1 te definieren worden polygonen ingelezen als meshes
-- anders worden zij ingelezen als polygonen (=splineshapes)
----------------------------------------------------------------------------

		--declaratie van globals
		global kaartlaag, Geometrytype, mijnkleur, f
		global attrarray = #()

		--definieer ringendataCA om daarin aantal buiten en binnenringen op te slaan van poygonen en multipatches
		RingenDataCA = attributes RingenData
		(
			parameters main 
			(
				buitenringvlaggen type:#boolTab tabSizeVariable:true
			)
		)
	
		--esriGeometryTypes
		esriGeometryNull = 0
		esriGeometryPoint  = 1
		esriGeometryMultipoint = 2
		esriGeometryPolyline = 3
		esriGeometryPolygon = 4
		esriGeometryEnvelope  = 5
		esriGeometryPath = 6
		esriGeometryAny = 7
		esriGeometryMultiPatch = 9
		esriGeometryRing = 11
		esriGeometryLine = 13
		esriGeometryCircularArc = 14
		esriGeometryBezier3Curve  = 15
		esriGeometryEllipticArc = 16
		esriGeometryBag = 17
		esriGeometryTriangleStrip = 18
		esriGeometryTriangleFan = 19
		esriGeometryRay = 20
		esriGeometrySphere = 21
		
		--TypeCodes in .NET
		typeCodeEmpty = 1
		typeCodeObject = 2
		typeCodeBoolean = 3
		typeCodeChar = 4 	
		typeCodeSByte = 5 	
		typeCodeByte = 6 	
		typeCodeInt16 = 7 	
		typeCodeUInt16 = 8 	
		typeCodeInt32 = 9 	
		typeCodeUInt32 = 10 	
		typeCodeInt64 = 11 	
		typeCodeUInt64 = 12 	
		typeCodeSingle = 13	
		typeCodeDouble = 14	
		typeCodeDecimal = 15
		typeCodeDateTime = 16 
		typeCodeString = 18

------------------------------------------------------------------------------------------------------
		
		function koppelgisdata ob =
		(
			paramstr = ""
			rolloutstr = ""
	
			for regel in attrarray do
			(
				naam = regel[1]
				type = regel[2]
				waarde = regel[3]
	
				paramstr = paramstr +  naam + " type:#" + type + "\n" 
				rolloutstr = rolloutstr + "edittext " + naam + " \"" + naam + "\" text:\""+ (waarde as string) +"\" enabled:false\n"
			)	
	
			GISDataCA = execute("attributes GISData( parameters main (\n" +
					paramstr +
				") rollout params \"GISData\" (\n" +
					rolloutstr +
				"))")
		
			CustAttributes.add ob GISDataCA 	

			for regel in attrarray do
			(
				global attrnaam = regel[1]
				global attrtype = regel[2]
				global attrwaarde = regel[3]

				setProperty ob attrnaam attrwaarde
			)
		)


-------------------------------------------------------------------------------
		function readboolean =
		(
			return ((readbyte f) == 1)
		)
		
-------------------------------------------------------------------------------
				
	   	function TekenTrianglesMP =
		(
			vertexarray = #()
			polygonarray = #()
				
			--Lees aantal onderdelen
			aantalparts = (readlong f)
			
			--Loop door de onderdelen  
			i=1
			for n = 1 to aantalparts do
			(	
				vertnumarray = #()
		
				--Lees aantal vertices
				nvts = (readlong f)
				
				--Loop door de vertices
				for k = 1 to nvts do
			    (	
					append vertexarray [(readfloat f), (readfloat f), (readfloat f)]
					append vertnumarray i
					i=i+1
				)
				append polygonarray vertnumarray
			)
			
			ob = mesh vertices:vertexarray faces:#()
			ob.wirecolor = mijnkleur
			koppelgisdata ob
			
			for pgn in polygonarray do
			(	
				meshop.createPolygon ob pgn
			)
			
			update ob
			meshop.weldVertsByThreshold ob #{1..ob.numverts} 0.0001
			update ob
			meshop.flipnormals ob #{1..ob.numfaces}
			update ob
			ob.pivot = ob.center	
		)
		
		--------------------------------------------------------
		function TekenPolygon =
		(
				
				--maak de feature aan
				ob = SplineShape pos:[0,0,0]
				ob.wirecolor = mijnkleur
				koppelgisdata ob
				CustAttributes.add ob RingenDataCA 	
			
				--Lees aantalringen
				aantalringen = (readlong f)
			
				--Loop door de ringen
				for n = 1 to aantalringen do
				(	
					--teken ring
					nieuwering = addNewSpline ob
					
					--lees buitenringvlag
					ob.RingenData.buitenringvlaggen[n] = readboolean()
					
					--Lees aantal vertices
					nvts = (readlong f)
				
					--Loop door de vertices
					for k = 1 to nvts do
					(	addKnot ob nieuwering #corner #line [(readfloat f), (readfloat f), (readfloat f)]
					)
					close ob nieuwering

				)
				updateShape ob
				ob.pivot = ob.center
		)

		
		----------------------------------------------------------
		
		function TekenPolyline =
		(
			--maak de feature aan
			ob = SplineShape pos:[0,0,0]
			ob.wirecolor = mijnkleur		
			koppelgisdata ob
			
			--Lees aantal onderdelen
			aantalparts = (readlong f)

			--Loop door de onderdelen  
			for n = 1 to aantalparts do
			(	
				nieuwering = addNewSpline ob
							
				--Lees aantal vertices
				nvts = (readlong f)
				
				--Loop door de vertices
				for k = 1 to nvts do
				(	addKnot ob nieuwering #corner #line [(readfloat f), (readfloat f), (readfloat f)]
				)
			)
			updateShape ob
			ob.pivot = ob.center
		)
		
		
		----------------------------------------------------------

	    function TekenMultipoint =
		(
			--Lees aantal onderdelen
			aantalparts = (readlong f)

			--Loop door de onderdelen  
			for n = 1 to aantalparts do
			(	
				--maak een punt aan
				ob = Point pos:[(readfloat f), (readfloat f), (readfloat f)]

				--attribuutgegevens en kleur
				ob.wirecolor = mijnkleur		
				koppelgisdata ob
				ob.size = 0.25
			)
		)
		

		----------------------------------------------------------
	   
		function TekenPoint =
		(   
			--maak een punt aan
			ob = Point pos:[(readfloat f), (readfloat f), (readfloat f)]
					
			--kleur
			ob.wirecolor = mijnkleur
			koppelgisdata ob
			ob.size = 0.25
		)

		----------------------------------------------------------

		
		function leesattrarray =
		(
			--lees attributen-aantal
			nattr = (readlong f)
			
			attrarray[1]=#("kaartlaag", "integer", kaartlaag) 
			attrarray[2]=#("Geometrytype", "integer", Geometrytype)
			
			for i = 3 to nattr+2 do
			(   attrnaam = (readstring f)
				typecode = (readlong f)
				case typecode of
				(
					typeCodeInt16: ( waarde = ((readshort f) as integer)
											typestr = "integer")
					typeCodeInt32: (	waarde = (readlong f)
											typestr = "integer")	
					typeCodeSingle: (waarde = (readfloat f)
											typestr = "float")
					typeCodeDouble: ( waarde = ((readdouble f) as float)
											  typestr = "float")
					typeCodeString: (waarde = (readstring f)
											typestr = "string")
					default: print  "gegevenstype nog niet ondersteund" 
				)	
				attrarray[i]=#(attrnaam, typestr, waarde)
			)
		)
-----------------------------------------------------------------------------------

			function importdatafile =
		(
			undo off
			(
				f= fopen "C:/3DGISBuffer.dat" "rb"
				
				--Lees het feature-aantal
				feature_aantal = (readlong f)
				
				--Loop door de features
				for n = 1 to feature_aantal do 
				(   
					--lees de kaartlaag
					global kaartlaag = (readlong f)
					
					--lees het geometrietype
					global Geometrytype = (readlong f)
					
					--lees de attribuutgegevens
					leesattrarray()

					--lees de kleur
					mijnkleur = (color (readlong f) (readlong f) (readlong f))
		
					case Geometrytype of
					(
						esriGeometryMultiPatch: 
						(
							TrianglesVlag = readboolean()
							if (TrianglesVlag) then
							(
								TekenTrianglesMP() 
							)
							else
							(
								TekenPolygon()
							)
						)
						esriGeometryPolygon: TekenPolygon()
						esriGeometryPolyline: TekenPolyline()
						esriGeometryMultipoint: TekenMultipoint() 
						esriGeometryPoint: TekenPoint() 
						default: print "geometrietype nog niet ondersteund"
					)
				)	
				fclose f
			)
		)
			importdatafile()