using Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TileGeoJSON 
{

    GameObject mParent;
    private Rect mTileBoundry;
    private Vector2 mTileCenter;
    public TileGeoJSON(GameObject pParent, int pZoom, int pX, int pY)
    {
        mParent = pParent;
        mTileBoundry = GM.TileBounds(new Vector2Int(pX, pY), pZoom);
        mTileCenter = mTileBoundry.center;
       
        

        var fi = new System.IO.FileInfo(String.Format(@"C:\development\GIT_WorldExplorer2018\WorldExplorer\world-explorer-server\cache\{0}\{1}\{2}\buildings.json", pZoom, pX, pY));
        if (fi.Exists)
        {
            JSONObject jsonObject = new JSONObject(System.IO.File.ReadAllText(fi.FullName));
            CreateBuilding(jsonObject);
        }
    }

    private void CreateBuilding(JSONObject pJson)
    {

        foreach (var geo in pJson.list[1].list.Where(x => x["geometry"]["type"].str == "Polygon"))
        {
            var bb = geo["geometry"]["coordinates"].list[0]; //this is wrong but cant fix it now
            for (int i = 0; i < bb.list.Count - 1; i++)
            {
                var c = bb.list[i];
                var dotMerc = GM.LatLonToMeters(c[1].f, c[0].f);

                var localMercPos = dotMerc - mTileCenter;

              
       
                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.transform.position = new Vector3((float)localMercPos.x/1000f, 0.5f, (float)localMercPos.y / 1000f);
                capsule.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                capsule.transform.SetParent(mParent.transform, false);
            }
        }
        
    }
}
