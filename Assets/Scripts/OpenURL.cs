using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

public class OpenURL : MonoBehaviour
{
    public string url = "https://5ryujin.com"; // 移動先URL

    public void Open()
    {
        Application.OpenURL(url);
    }
}