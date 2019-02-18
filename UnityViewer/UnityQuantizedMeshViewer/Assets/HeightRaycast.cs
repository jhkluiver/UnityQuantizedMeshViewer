using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightRaycast : MonoBehaviour
{
    GameObject mMarker;
    public float x;
    public float mHeight;

    public float z;
    // Update is called once per frame
    void Update()
    {
        int layerMask = (1 << 10);
        Vector3 pos = new Vector3(x, 10, z);
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(pos, -Vector3.up, out hit, Mathf.Infinity, layerMask))
        {
            
            Debug.DrawRay(hit.point, Vector3.up * 2, Color.yellow);
            if (mMarker != null) mMarker.transform.position = pos + (-Vector3.up * hit.distance);
            mHeight = hit.point.y;
            //Debug.Log("Height =" + m.transform.position.y);
        }
        else
        {
            Debug.DrawRay(pos, Vector3.up * 1000, Color.white);
            
        }
    }

   

    void Start()
    {
     
        
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 20), (mHeight * 100).ToString());
    }
}
