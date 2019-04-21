-- *** VAN 3DSMAX NAAR ARCSCENE ***
-- Deze macro schrijft attribuutgegevens en vertices van geselecteerde objecten naar 3DGISBuffer.dat.
-- De macro maakt de volgende algemene bestandsopbouw:
---------------------------------------------------------------------------
--per onclick()
--				kaartlaag							(int32)
--per object
--				geometrietype					(int32)
--per attribuut:
--				attrnaam							(string met '\0')
--				attrtype							(string met '\0')
--				attrwaarde						(type afhankelijk van attrtype) 						
--				(onderdelenaantal) 				(int32)
--per object
--				(ringenvlag)						(boolean = byte)
--per onderdeel:
--			 	(vertexaantal)					(int32)
--per vertex:
--				vertex.x							(single float)
--				vertex.y							(single float)
--				vertex.z							(single float)

---------------------------------------------------------------------------
-- Opmerkingen: 
-- 1: Ondersteunde types voor attribuutgegevens zijn: Integer (int32), Float (single float) en String.
-- 2: Niet bij alle objecten worden zowel onderdelenaantal als vertexaantal opgeslagen.
----------------------------------------------------------------------------

	--declaratie van globals	
	global f

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
	
----------------------------------------------------------	
--deze functie is nodig omdat MaxScript geen writeboolean-functie kent

	function schrijfboolean b =
	(
		if (b) then 
		(writebyte f 1 )
		else
		(writebyte f 0 )
	)
	
----------------------------------------------------------	

	function schrijfattributen ob =
	(
		--maak array met de attribuutnamen
		attrnaamarray= getPropNames ob.GISData
		
		for i = 3 to attrnaamarray.count do
		(
			p = attrnaamarray[i]
			attrwaarde = (getProperty ob p)
			attrtype = ((classof  attrwaarde) as string)
		
			--schrijf attrtype
			writestring f attrtype
			
			--schrijf attrwaarde
			case attrtype of
			(
				"Integer": (writelong f attrwaarde)
				"Float": (writefloat f attrwaarde)
				"String":(writestring f attrwaarde)
				default: print "gegevenstype nog niet ondersteund"
			)
		)
	)
	
	
	
--------------------------------------------------------------------------------------------
	
	function SchrijfSpline ob i =
	(		
		--schrijf vertexaantal weg
		aantalvertices = (numknots ob i)
		writelong f aantalvertices 

		--loop door de vertices
		for l = 1 to aantalvertices do
		(
			vertex = (getknotpoint ob i l)
			writefloat f vertex.x
			writefloat f vertex.y
			writefloat f vertex.z
		)
	)


--------------------------------------------------------------------------------------------

	function SchrijfClosedSplineShape ob =
	(
		--schrijf aantalringen weg
		aantalringen = ob.numsplines
		writelong f aantalringen 
		
		--loop door de ringen		
		for i = 1 to aantalringen do
		(	
			--schrijf Buitenringvlag
			schrijfboolean ob.RingenData.Buitenringvlaggen[i]
			
			--schrijf de ring weg
			SchrijfSpline ob i
		
		)--for
	)

--------------------------------------------------------------------------------------------

	function SchrijfOpenSplineShape ob =
	(
		--schrijf aantalringen weg
		aantalpaths = ob.numsplines
		writelong f aantalpaths 
		
		--loop door de ringen		
		for i = 1 to aantalpaths do
		(	
			--schrijf de ring weg
			SchrijfSpline ob i
		)
	)


--------------------------------------------------------------------------------------------
	
	function SchrijfMesh ob =
	(
		
		--schrijf onderdelenaantal weg (= aantal faces)
		nfcs = ob.numfaces
		writelong f nfcs 
		
		--schrijf faces weg als triangles		
		for i = 1 to nfcs do
		(	
			fc = (getface ob i)
			writefloat f (getvert ob (fc.z as integer)).x
			writefloat f (getvert ob (fc.z as integer)).y
			writefloat f (getvert ob (fc.z as integer)).z
				
			writefloat f (getvert ob (fc.y as integer)).x
			writefloat f (getvert ob (fc.y as integer)).y
			writefloat f (getvert ob (fc.y as integer)).z
				
			writefloat f (getvert ob (fc.x as integer)).x
			writefloat f (getvert ob (fc.x as integer)).y
			writefloat f (getvert ob (fc.x as integer)).z	
		)--for
	)

	
	
--------------------------------------------------------------------------------------------
	
	function SchrijfPoint ob =
	(
			writefloat f ob.pos.x
			writefloat f ob.pos.y
			writefloat f ob.pos.z		
	)

	
--------------------------------------------------------------------------
	
	function ExportNaarArcscene =
	(
				
		undo off
		(   
			f= fopen "C:/3DGISBuffer.dat" "wb"
			
			--vraag eerste object in selectie op
			ob = selection[1]
			
			--schrijf layerindex van eerste object in selectie weg
			writelong f ob.kaartlaag
			
			for ob in selection do 
			(	
				--schrijf geometrietype weg
				writelong f ob.Geometrytype

				--schrijfattributen
				schrijfattributen ob
				
				--schrijf object weg
				case ob.Geometrytype of 
				(
					esriGeometryPolygon: (SchrijfClosedSplineShape ob)
					esriGeometryPolyline: (SchrijfOpenSplineShape ob)
					esriGeometryMultiPatch: (SchrijfMesh ob)
					esriGeometryPoint: (SchrijfPoint ob)
					default: (print "Gegevenstype nog niet ondersteund")
				)		
				
			)--for

			fclose f
			print "gereed"
		)--undo off
	)--function
----------------------------------------------------------------------------------

	ExportNaarArcscene()
