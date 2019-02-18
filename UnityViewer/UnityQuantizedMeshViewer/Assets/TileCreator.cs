using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TileCreator : MonoBehaviour
{
    public struct PointF
    {
        public float X;
        public float Y;
    }

    [SerializeField]
    private float Latitude = 52.100876f;


    [SerializeField]
    private float Longitude = 4.257421f;

    [SerializeField]
    private int ZoomLevel = 14;

    [SerializeField]
    private int NumberOfTiles = 3;

    public PointF WorldToTilePos(double lon, double lat, int zoom)
    {
        PointF p = new PointF();
        p.X = (float)((lon + 180.0) / 360.0 * (1 << zoom));
        p.Y = (float)((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) +
            1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));

        return p;
    }

    public PointF TileToWorldPos(double tile_x, double tile_y, int zoom)
    {
        PointF p = new PointF();
        double n = Math.PI - ((2.0 * Math.PI * tile_y) / Math.Pow(2.0, zoom));

        p.X = (float)((tile_x / Math.Pow(2.0, zoom) * 360.0) - 180.0);
        p.Y = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

        return p;
    }

    // Start is called before the first frame update
    void Start()
    {
        var tile = WorldToTilePos(Longitude, Latitude, ZoomLevel);
        int x = (int)tile.X;
        int y = (int)tile.Y;
        int y1 = toTms(ZoomLevel, y);
        for (int yTile = y - NumberOfTiles; yTile <= y + NumberOfTiles; yTile++)
        {
            for (int xTile = x - NumberOfTiles; xTile <= x + NumberOfTiles; xTile++)
            {
                StartCoroutine(generateTiles(ZoomLevel, xTile, yTile, xTile - x, yTile - y));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public static int toTms(int pZoom, int pY)
    {
        return (int)(System.Math.Pow(2, pZoom) - pY - 1);
    }

    private IEnumerator generateTiles(int zoom, int x, int y, int offsetX, int offsetY)
    {


        float tileSize = 1;
        var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);

        tile.name = string.Format("tile-{0}-{1}-{2}", zoom, x, y);
        tile.transform.SetParent(this.transform, false);
        tile.transform.localRotation = Quaternion.Euler(0, 0, 0);

        tile.transform.localPosition += new Vector3(offsetX * tileSize, 0, (-offsetY * tileSize) );
        tile.transform.localScale = new Vector3(1, 1 /*2.5f*/, 1);

        // OSM uses tiles: https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames
        // TMS uses tiles: https://en.wikipedia.org/wiki/Tile_Map_Service

        // Convert XYZ OSM to XYZ TMS
        int tms_zoom = zoom; // Stays the same
        int tms_x = x; // Stays the same
        int tms_y = (int)(System.Math.Pow(2, zoom) - y - 1);

        name = $"Height-mesh-tile-{tms_zoom}-{ tms_x}-{tms_y}";
        var fi = new System.IO.FileInfo(String.Format(@"C:\development\QuantizedMesh\AHN\DTM\MOORDRECHT\tiles\{0}\{1}\{2}.terrain", tms_zoom, tms_x, tms_y));
        Mesh terrainMesh = (fi.Exists) ? QuantizedMeshCreator.CreateMesh(fi.OpenRead(), name) : QuantizedMeshCreator.CreateEmptyQuad(name + "_noheightmap");
        tile.gameObject.GetComponent<MeshFilter>().mesh = terrainMesh;
        tile.gameObject.GetComponent<MeshCollider>().sharedMesh = terrainMesh;
        tile.gameObject.layer = 10;



        //new TileGeoJSON(tile, tms_zoom, tms_x, y);
        // 
        //string url = string.Format("https://tile.openstreetmap.org/{0}/{1}/{2}.png", zoom, x, y);
        string url = string.Format("http://mt0.google.com/vt/lyrs=y&hl=en&x={1}&y={2}&z={0}&s=Ga", zoom, x, y);
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                // Get downloaded asset bundle
                var texture = DownloadHandlerTexture.GetContent(uwr);
                var rend = tile.GetComponent<MeshRenderer>();
                if (rend)
                {
                    Material material = new Material(Shader.Find("Standard"));
                    //Texture2D texture = new Texture2D(512, 512, TextureFormat.DXT5, false);
                    //texture.LoadImage(bytes);
                    material.mainTexture = texture;
                    rend.material = material;
                }
            }
        }
    }

 

}
