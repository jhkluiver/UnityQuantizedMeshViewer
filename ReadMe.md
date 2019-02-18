

<a href="http://www.youtube.com/watch?feature=player_embedded&v=UYIF0Ae3LBA
" target="_blank"><img src="http://img.youtube.com/vi/UYIF0Ae3LBA/0.jpg" 
alt="Preview" width="240" height="180" border="10" /></a>

Dit project was een kleine test applicatie voor:
* https://github.com/TNOCS/WorldExplorer
in opdracht van https://www.driver-project.eu/

# Terrain elevation

The goal of this project is to view a 3D terrain in Unity from real world data. The Digital Elevation Data (DEM)  is provided as a raster where each pixel represents a height. To create a height mesh, the raster format is converted to a vector format. The vector data can be used to create an unity mesh with a terrain texture. 

## AHN

The elevation data for the Netherlands is provided by the AHN. The DEM files can be download on https://www.pdok.nl/nl/ahn3-downloads (GeoTiff format). In the DTM format, the raw elevation data is corrected to represent the actual terrain height and not the measured height (without buildings, threes, ...). 

To reduce the DEM file size (GeoTiff) tiles are used. The AHM DTM provides a raster density of 0.5 m and 5 meter. In Open Street Map (OSM) (on the equator) in zoom level 14 each pixel is 9.547 meter and in zoom level 15 each pixel is 4.773 meter (https://wiki.openstreetmap.org/wiki/Zoom_levels). The 5 meter raster size will be sufficient  for this zoom levels. For 0.5m resolution the tile size is ~ 500MB. For 5 meter the tile size is ~ 5 MB.

# GeoTiff

The AHN uses GeoTiff as format. The AHN NO_DATA value is set to MIN FLOAT value, some tools can't handle this value. 

The following GDAL tools can be useful:

* Use GDAL tools in docker

​	https://github.com/geo-data/gdal-docker

* Info about GeoTiff file

​	 gdalinfo <geotiff file>

* Batch convert projection to EPSG:3857 (mercator)

```BASH 
#!/bin/bash
for file in $(ls *.tif); do 
    gdalwarp -t_srs EPSG:3857 $file mercator_$file; 
done
```

* Replace NO_DATA value (everything below 100 meter)

  ​	gdal_calc.py -A r_30dn2.tif --outfile=result.tif --calc="A*(A<100)" --NoDataValue=0

* Logical merge multiple GeoTiff files:

  ​	gdalbuildvrt tiles.vrt *.tif

* Naar mercator projectie converteren

  ​	gdalwarp -t_srs EPSG:3857 -of GTiff -ot Float32 inputA.tif outputA.tif

To view the GeoTiff in 3D the 'qgis2threejs' plugin from QGIS can be used. 



# Projections

* Mercator (EPSG:3857): Used by OSM and GOOGLE in tiles
* World Geodetic System (EPSG:4326): Latitude / longitude projection
* Rijksdienst (RD, EPSG:28992): Coordinate system (based on meter) used in the Netherlands, AHN is provided in this projection.

The height tiles must be in mercator projector to match the tiles (tile identification system).



# Tiled maps

The pyramid tile structure is used to retrieve the height mesh and terrain texture.  For the terrain texture Google maps and OSM can be used as tiled maps. These tiled maps are also called ' XYZ'  or ' Slippy Map'. The tiles have a fixed size (e.g.  256x256 pixels),  the projection is web mercator (meter). The tiles are defined by zoom level and X and Y. Google Maps and OpenStreetMap have the same Y  encoding, in  the TMS (Tile map Service) the Y is flipped.  The Terrain Mesh format uses the TMS encoding.

For Open Street Map / Google Maps:

```c#
public PointF WorldToTilePos(double lon, double lat, int zoom)
{
	PointF p = new Point();
	p.X = (float)((lon + 180.0) / 360.0 * (1 << zoom));
	p.Y = (float)((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 
		1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));
		
	return p;
}

public PointF TileToWorldPos(double tile_x, double tile_y, int zoom) 
{
	PointF p = new Point();
	double n = Math.PI - ((2.0 * Math.PI * tile_y) / Math.Pow(2.0, zoom));

	p.X = (float)((tile_x / Math.Pow(2.0, zoom) * 360.0) - 180.0);
	p.Y = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

	return p;
}
```



See https://en.wikipedia.org/wiki/Tiled_web_map and https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames

To view tile numbers on GIS map: 

http://www.maptiler.org/google-maps-coordinates-tile-bounds-projection/

# Cesium

The cesium organization has defined two formats for storing terrain height (binary vector format).

*  Terrain Height 1.0

  A [terrain tileset](https://cesiumjs.org/data-and-assets/terrain/formats/index.html) in `heightmap-1.0` format is simple multi-resolution quadtree pyramid of heightmaps according to the [Tile Map Service (TMS)](http://wiki.osgeo.org/wiki/Tile_Map_Service_Specification) layout and global-geodetic profile. All tiles have the extension `.terrain`

  The tiles are 65x65 vertices and overlap their neighbors at their edges. In other words, at the root, the eastern-most column of heights in the western tile is identical to the western-most column of heights in the eastern tile.

  Terrain tiles are served gzipped. Once extracted, they are at least 8,452 bytes in size. The first and most important part of the file is a simple array of 16-bit, little-endian, integer heights arranged from north to south and from west to east - the first 2 bytes are the height in the northwest corner, and the next 2 bytes are the height at the location just to the east of there. Each height is the number of 1/5 meter units above -1000 meters. The total size of the post data is `65 * 65 * 2 = 8450` bytes.

  Following the height data is one additional byte which is a bit mask indicating which child tiles are present. The bit values are as follows:

  - Southwest - bit 0 - value 1
  - Southeast - bit 1 - value 2
  - Northwest - bit 2 - value 4
  - Northeast - bit 3 - value 8

  If a bit is set, the corresponding `.terrain` file can be expected to be found on the server as well. If it is cleared, requesting the `.terrain`file will return a 404 error.

  The child bit mask is followed by the water mask. The water mask is either 1 byte, in the case that the tile is all land or all water, or it is `256 * 256 * 1 = 65536` bytes if the tile has a mix of land and water. Each mask value is 0 for land and 255 for water. Values between 0 and 255 are allowed as well (but not currently present in the data) in order to support anti-aliasing of the coastline.

* Quantized-mesh-1.0

  A [terrain tileset](https://cesiumjs.org/data-and-assets/terrain/formats/index.html) in `quantized-mesh-1.0` format is a simple multi-resolution quadtree pyramid of heightmaps according to the [Tile Map Service (TMS)](http://wiki.osgeo.org/wiki/Tile_Map_Service_Specification) layout and global-geodetic profile. All tiles have the extension `.terrain`

# Terrain-height or Quantized-Mesh format

The 'height-map' format is deprecated, but very simple to implement. A mesh reduction algorithm could be used to reduce the number of mesh vertexs. However the only implementation that can generate the terrain-height format seems to fail on zoom level 14 (no height data generated). When the height tile is inspected with ctb-info all height are zero (value 5000). When the same tool is used to generate a (tile) GeoTiff tile the result is as expected. 

Therefor the ' Quantized-mesh-1.0' is used in this project (after spending a long time finding the problem with height-map). The Quantized-mesh is stored as optimized meshes, this reduces the load time.

## Height tile creation

There are several tools to convert a GeoTiff to a Tile Map Service structure (a pyramid of heightmaps). This structures matches the structure of web tiles (Open Street Map / Google maps), only the Y value needs to be inverted. 

The following programs can be used to create height tiles:

* Cesium Terrain Builder

  Provided as a docker instance (https://hub.docker.com/r/homme/cesium-terrain-builder/). This version can only generate 'Terrain Height 1.0' format and therefore not used.

* Cesium Terrain Builder (upgrade)

  The project 'https://github.com/tum-gis/cesium-terrain-builder-docker' is an upgrade of cesium terrain builder that support ' Quantized-mesh-1.0' format. The application fails when mercator projection must be created.

* TIN-terrain

  The project 'https://github.com/heremaps/tin-terrain' can generate 'Quantized-mesh', 'OBJ' and 'json/geojson'. Multiple mesh reduction algorithm are supported. The GeoTiff **must** be provided in Web Mercator projection (EPSG:3857). The application can fail when gdalbuildvrt are used to combine multiple GeoTiffs. The .OBJ files can easily inspected in a 3D viewer (this can be misleading because of the heightscale). 

# Cesium Terrain Builder (upgrade)

1. https://github.com/tum-gis/cesium-terrain-builder-docker#create-cesium-terrain-files



# Create TIN-TERRAIN docker container

Unfortunately there is no container in Docker Hub, so you have to create it yourself

* Download source code from "https://github.com/heremaps/tin-terrain"
* Run './build-docker.sh' (in bash emulator under windows)
* To validate: `docker images` should contain container "tin-terrain"

# TIN-Terrain

To start the docker environment:

``

* Download GeoTiff tiles at https://www.pdok.nl/nl/ahn3-downloads (DTM, 5 meter grid)

* Start TIN-Terrain in docker:

  `docker run --name tin-terrain --rm -it -v C:\development\QuantizedMesh\AHN\DTM:/data tin-terrain /bin/bash` then `cd \data`

  Where "C:\development\QuantizedMesh\AHN\DTM" is the path on local computer with GeoTiffs

* Convert to mercator projection

  `#!/bin/bash`
  `for file in $(ls *.tif); do` 
  ​    `gdalwarp -t_srs EPSG:3857 $file mercator_$file;` 
  `done`

* Create Virtual Dataset (combine multiple tif files)

  ​	`gdalbuildvrt tiles.vrt mercator_*.tif`

* Generate terrain structure

  `tin-terrain dem2tintiles --input /data/tiles.vrt --output-dir /data/tiles --min-zoom 5 --max-zoom 14 --output-format=terrain --max-error 0.5`

The ' --max-error' (distance (in meter) used for optimization): 

* Low values:  high detail; large number of triangles in mesh
* High value: lower detail; small number of triangles in mesh



# Other solution

In https://github.com/tangrams/unity-terrain-example is an example how to use heightmap image.

























# Quantized-Mesh format (from cesium website)

When requesting tiles, be sure to include the following HTTP header in the request:

```
Accept: application/vnd.quantized-mesh,application/octet-stream;q=0.9
```

Otherwise, some servers may return a different representation of the tile than the one described here.

Each tile is a specially-encoded triangle mesh where vertices overlap their neighbors at tile edges. In other words, at the root, the eastern-most vertices in the western tile have the same longitude as the western-most vertices in the eastern tile.

Terrain tiles are served gzipped. Once extracted, tiles are little-endian, binary data. The first part of the file is a header with the following format. Doubles are IEEE 754 64-bit floating-point numbers, and Floats are IEEE 754 32-bit floating-point numbers.

```
struct QuantizedMeshHeader
{
    // The center of the tile in Earth-centered Fixed coordinates.
    double CenterX;
    double CenterY;
    double CenterZ;
    
    // The minimum and maximum heights in the area covered by this tile.
    // The minimum may be lower and the maximum may be higher than
    // the height of any vertex in this tile in the case that the min/max vertex
    // was removed during mesh simplification, but these are the appropriate
    // values to use for analysis or visualization.
    float MinimumHeight;
    float MaximumHeight;

    // The tile’s bounding sphere.  The X,Y,Z coordinates are again expressed
    // in Earth-centered Fixed coordinates, and the radius is in meters.
    double BoundingSphereCenterX;
    double BoundingSphereCenterY;
    double BoundingSphereCenterZ;
    double BoundingSphereRadius;

    // The horizon occlusion point, expressed in the ellipsoid-scaled Earth-centered Fixed frame.
    // If this point is below the horizon, the entire tile is below the horizon.
    // See http://cesiumjs.org/2013/04/25/Horizon-culling/ for more information.
    double HorizonOcclusionPointX;
    double HorizonOcclusionPointY;
    double HorizonOcclusionPointZ;
};
```

Immediately following the header is the vertex data. An `unsigned int` is a 32-bit unsigned integer and an `unsigned short` is a 16-bit unsigned integer.

```
struct VertexData
{
    unsigned int vertexCount;
    unsigned short u[vertexCount];
    unsigned short v[vertexCount];
    unsigned short height[vertexCount];
};
```

The `vertexCount` field indicates the size of the three arrays that follow. The three arrays are zig-zag encoded in order to make small integers, regardless of their sign, use a small number of bits. Decoding a zig-zag encoded value is straightforward:

```
decoded = (encodedValue >> 1) ^ (-(encodedValue & 1))
```

Once decoded, the meaning of a value in each array is as follows:

| Field  | Meaning                                                      |
| ------ | ------------------------------------------------------------ |
| u      | The horizontal coordinate of the vertex in the tile. When the `u` value is 0, the vertex is on the Western edge of the tile. When the value is 32767, the vertex is on the Eastern edge of the tile. For other values, the vertex's longitude is a linear interpolation between the longitudes of the Western and Eastern edges of the tile. |
| v      | The vertical coordinate of the vertex in the tile. When the `v` value is 0, the vertex is on the Southern edge of the tile. When the value is 32767, the vertex is on the Northern edge of the tile. For other values, the vertex's latitude is a linear interpolation between the latitudes of the Southern and Nothern edges of the tile. |
| height | The height of the vertex in the tile. When the `height` value is 0, the vertex's height is equal to the minimum height within the tile, as specified in the tile's header. When the value is 32767, the vertex's height is equal to the maximum height within the tile. For other values, the vertex's height is a linear interpolation between the minimum and maximum heights. |

Immediately following the vertex data is the index data. Indices specify how the vertices are linked together into triangles. If tile has more than 65536 vertices, the tile uses the `IndexData32` structure to encode indices. Otherwise, it uses the `IndexData16` structure.

To enforce proper byte alignment, padding is added before the IndexData to ensure 2 byte alignment for `IndexData16` and 4 byte alignment for `IndexData32`.

```
struct IndexData16
{
    unsigned int triangleCount;
    unsigned short indices[triangleCount * 3];
}

struct IndexData32
{
    unsigned int triangleCount;
    unsigned int indices[triangleCount * 3];
}
```

Indices are encoded using the high water mark encoding from [webgl-loader](https://code.google.com/p/webgl-loader/). Indices are decoded as follows:

```
var highest = 0;
for (var i = 0; i < indices.length; ++i) {
    var code = indices[i];
    indices[i] = highest - code;
    if (code === 0) {
        ++highest;
    }
}
```

Each triplet of indices specifies one triangle to be rendered, in counter-clockwise winding order. Following the triangle indices is four more lists of indices:

```
struct EdgeIndices16
{
    unsigned int westVertexCount;
    unsigned short westIndices[westVertexCount];

    unsigned int southVertexCount;
    unsigned short southIndices[southVertexCount];

    unsigned int eastVertexCount;
    unsigned short eastIndices[eastVertexCount];

    unsigned int northVertexCount;
    unsigned short northIndices[northVertexCount];
}

struct EdgeIndices32
{
    unsigned int westVertexCount;
    unsigned int westIndices[westVertexCount];

    unsigned int southVertexCount;
    unsigned int southIndices[southVertexCount];

    unsigned int eastVertexCount;
    unsigned int eastIndices[eastVertexCount];

    unsigned int northVertexCount;
    unsigned int northIndices[northVertexCount];
}
```

These index lists enumerate the vertices that are on the edges of the tile. It is helpful to know which vertices are on the edges in order to add skirts to hide cracks between adjacent levels of detail.

## Extensions

Extension data may follow to supplement the quantized-mesh with additional information. Each extension begins with an `ExtensionHeader`, consisting of a unique identifier and the size of the extension data in bytes. An `unsigned char` is a 8-bit unsigned integer.

```
struct ExtensionHeader
{
    unsigned char extensionId;
    unsigned int extensionLength;
}
```

As new extensions are defined, they will be assigned a unique identifier. If no extensions are defined for the tileset, an `ExtensionHeader`will not included in the quanitzed-mesh. Multiple extensions may be appended to the quantized-mesh data, where ordering of each extension is determined by the server.

Multiple extensions may be requested by the client by delimiting extension names with a `-`. For example, a client can request vertex normals and watermask using the following Accept header:

```
Accept : 'application/vnd.quantized-mesh;extensions=octvertexnormals-watermask'
```

The following extensions may be defined for a quantized-mesh:

#### Terrain Lighting

| Name:            | Oct-Encoded Per-Vertex Normals                               |
| ---------------- | ------------------------------------------------------------ |
| Id:              | `1`                                                          |
| Description:     | Adds per vertex lighting attributes to the quantized-mesh. Each vertex normal uses oct-encoding to compress the traditional x, y, z 96-bit floating point unit vector into an x,y 16-bit representation. The 'oct' encoding is described in "A Survey of Efficient Representations of Independent Unit Vectors", Cigolle et al 2014: <http://jcgt.org/published/0003/02/01/> |
| Data Definition: | `struct OctEncodedVertexNormals {     unsigned char xy[vertexCount * 2]; }             ` |
| Requesting:      | For oct-encoded per-vertex normals to be included in the quantized-mesh, the client must request this extension by using the following HTTP Header:`Accept : 'application/vnd.quantized-mesh;extensions=octvertexnormals'             ` |
| Comments:        | The original implementation of this extension was requested using the extension name `vertexnormals`. The `vertexnormals` extension identifier is deprecated and implementations must now request vertex normals by adding `octvertexnormals` in the request header extensions parameter, as shown above. |

#### Water Mask

| Name:            | Water Mask                                                   |
| ---------------- | ------------------------------------------------------------ |
| Id:              | `2`                                                          |
| Description:     | Adds coastline data used for rendering water effects. The water mask is either 1 byte, in the case that the tile is all land or all water, or it is `256 * 256 * 1 = 65536` bytes if the tile has a mix of land and water. Each mask value is 0 for land and 255 for water. Values in the mask are defined from north-to-south, west-to-east; the first byte in the mask defines the watermask value for the northwest corner of the tile. Values between 0 and 255 are allowed as well in order to support anti-aliasing of the coastline. |
| Data Definition: | A Terrain Tile covered entirely by land or water is defined by a single byte.`struct WaterMask {     unsigned char mask; }             `A Terrain Tile containing a mix of land and water define a 256 x 256 grid of height values.`struct WaterMask {     unsigned char mask[256 * 256]; }             ` |
| Requesting:      | For the watermask to be included in the quantized-mesh, the client must request this extension by using the following HTTP Header:`Accept : 'application/vnd.quantized-mesh;extensions=watermask'` |



The AHN tile size don't match the tiled maps (OSM). When converting 
