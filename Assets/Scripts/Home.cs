using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Home : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    public void Play(int sec)
    {
        Timer.limit = sec;
        SceneManager.LoadScene("MainScene");
    }
    public void Leave()
    {
        Debug.Log(0);
    }
}
 