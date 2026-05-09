using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebGLScreenFitter : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float targetAspect = 16f / 9f;
        float windowsAspect = (float)Screen.width / (float)Screen.height;
        float ScaleHight = windowsAspect / targetAspect;
        Camera cam = GetComponent<Camera>();

        if (ScaleHight < 1.0f)
        {
            //画面が縦に長い場合
            cam.rect = new Rect(0, (1.0f - ScaleHight) / 2.0f, 1.0f, ScaleHight);
        }
        else
        {
            //画面が横に長い場合
            float Scalewidth = 1.0f / ScaleHight;
            cam.rect = new Rect((1.0f - Scalewidth) / 2.0f, 0, Scalewidth, 1.0f);
        }
        
    }
}
