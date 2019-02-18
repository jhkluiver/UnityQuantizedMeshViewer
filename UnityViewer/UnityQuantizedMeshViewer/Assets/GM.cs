


using System;
using UnityEngine;

namespace Helpers
{
    //SOURCE: http://stackoverflow.com/questions/12896139/geographic-coordinates-converter
    public static class GM
    {
        private const int TileSize = 256;
        private const int EarthRadius = 6378137;
        private const double InitialResolution = 2 * Math.PI * EarthRadius / TileSize;
        private const double OriginShift = 2 * Math.PI * EarthRadius / 2;

        public static Vector2 LatLonToMeters(Vector2 v)
        {
            return LatLonToMeters(v.x, v.y);
        }

        /// <summary>
        /// Converts given lat/lon in WGS84 Datum to XY in Spherical Mercator EPSG:900913
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <returns></returns>
        public static Vector2 LatLonToMeters(double lat, double lon)
        {
            var p = new Vector2();
            p.x = (float)(lon * OriginShift / 180);
            p.y = (float)(Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) / (Math.PI / 180));
            p.y = (float)(p.y * OriginShift / 180);
            return new Vector2(p.x, p.y);
        }

        /// <summary>
        /// Converts EPSG:900913 to pyramid pixel coordinates in given zoom level
        /// </summary>
        /// <param name="m"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static Vector2 MetersToPixels(Vector2 m, int zoom)
        {
            var res = Resolution(zoom);
            var pix = new Vector2();
            pix.x = (float)((m.x + OriginShift) / res);
            pix.y = (float)((-m.y + OriginShift) / res);
            return pix;
        }

        /// <summary>
        /// Converts pixel coordinates in given zoom level of pyramid to EPSG:900913
        /// </summary>
        /// <param name="p"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static Vector2 PixelsToMeters(Vector2 p, int zoom)
        {
            var res = Resolution(zoom);
            var met = new Vector2();
            met.x = (float)(p.x * res - OriginShift);
            met.y = (float)-(p.y * res - OriginShift);
            return met;
        }

        /// <summary>
        /// Returns a TMS (NOT Google!) tile covering region in given pixel coordinates
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Vector2 PixelsToTile(Vector2 p)
        {
            var t = new Vector2();
            t.x = (int)Math.Ceiling(p.x / (double)TileSize) - 1;
            t.y = (int)Math.Ceiling(p.y / (double)TileSize) - 1;
            return t;
        }

        public static Vector2 PixelsToRaster(Vector2 p, int zoom)
        {
            var mapSize = TileSize << zoom;
            return new Vector2(p.x, mapSize - p.y);
        }

        /// <summary>
        /// Returns tile for given mercator coordinates
        /// </summary>
        /// <param name="m"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static Vector2 MetersToTile(Vector2 m, int zoom)
        {
            var p = MetersToPixels(m, zoom);
            return PixelsToTile(p);
        }

        /// <summary>
        /// Returns bounds of the given tile in EPSG:900913 coordinates
        /// </summary>
        /// <param name="t"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static Rect TileBounds(Vector2Int t, int zoom)
        {
            var min = PixelsToMeters(new Vector2(t.x * TileSize, t.y * TileSize), zoom);
            var max = PixelsToMeters(new Vector2((t.x + 1) * TileSize, (t.y + 1) * TileSize), zoom);
            return new Rect(min, max - min);
        }
        /// <summary>
        /// Converts meters to a Latitude (y) and Longitude (x).
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Vector2 MetersToLatLon(Vector2 m)
        {
            var ll = new Vector2();
            ll.x = (float)(m.x / OriginShift) * 180;
            ll.y = (float)(m.y / OriginShift) * 180;
            ll.y = (float)(180 / Math.PI * (2 * Math.Atan(Math.Exp(ll.y * Math.PI / 180)) - Math.PI / 2));
            return ll;
        }

        //Returns bounds of the given tile in latutude/longitude using WGS84 datum
        //public static RectD TileLatLonBounds(Vector2d t, int zoom)
        //{
        //    var bound = TileBounds(t, zoom);
        //    var min = MetersToLatLon(new Vector2d(bound.Min.x, bound.Min.y));
        //    var max = MetersToLatLon(new Vector2d(bound.Min.x + bound.Size.x, bound.Min.y + bound.Size.y));
        //    return new RectD(min.x, min.y, Math.Abs(max.x - min.x), Math.Abs(max.y - min.y));
        //}

        /// <summary>
        /// Resolution (meters/pixel) for given zoom level (measured at Equator) 
        /// </summary>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static double Resolution(int zoom)
        {
            return InitialResolution / (Math.Pow(2, zoom));
        }

        public static double ZoomForPixelSize(double pixelSize)
        {
            for (var i = 0; i < 30; i++)
                if (pixelSize > Resolution(i))
                    return i != 0 ? i - 1 : 0;
            throw new InvalidOperationException();
        }

        // Switch to Google Tile representation from TMS
        public static Vector2 ToGoogleTile(Vector2 t, int zoom)
        {
            return new Vector2(t.x, ((int)Math.Pow(2, zoom) - 1) - t.y);
        }

        // Switch to TMS Tile representation from Google
        public static Vector2 ToTmsTile(Vector2 t, int zoom)
        {
            return new Vector2(t.x, ((int)Math.Pow(2, zoom) - 1) - t.y);
        }
    }
}
