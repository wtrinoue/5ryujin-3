using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Board : MonoBehaviour
{
    [SerializeField] GameObject tile;
    [SerializeField] GameObject grid;
    [SerializeField] GameObject button;
    [SerializeField] GameObject canvas;
    [SerializeField] GameObject turnText;
    [SerializeField] TextMeshProUGUI tmp;
    public static bool turn { get; private set; } // false = 1P
    public static Board instance;
    public List<GameObject> images;
    [SerializeField] GameObject pc;

    void Start()
    {
        turn = false;
        instance = this;

        for (int i = 0; i < 30; i++)
        {
            for (int j = 0; j < 60; j++)
            {
                GameObject t = Instantiate(tile, transform);
                t.transform.position = new Vector3(j, i, 0);
            }
        }

        for (int i = 0; i < 11; i++)
        {
            GameObject t = Instantiate(grid, transform);
            t.transform.position = new Vector3(i * 5 + 4.5f, 14.5f, 0);
            t.transform.localScale = new Vector3(0.2f, 30, 1);
        }

        for (int i = 0; i < 5; i++)
        {
            GameObject t = Instantiate(grid, transform);
            t.transform.position = new Vector3(29.5f, i * 5 + 4.5f, 0);
            t.transform.localScale = new Vector3(60, 0.2f, 1);
        }

        GameObject firstPlayerIButton = null;

        for (int i = 0; i < images.Count; i++)
        {
            // 上側（2P側）
            GameObject t = Instantiate(button, canvas.transform);
            var pb = t.GetComponent<PieceButton>();
            pb.number = i;
            pb.turn = true;

            var st = t.GetComponent<Stock>();
            st.image = images[i];

            t.transform.localPosition = new Vector3(i * 150 - 825, 480, 0);

            // 下側（1P側）
            t = Instantiate(button, canvas.transform);
            pb = t.GetComponent<PieceButton>();
            pb.number = i;
            pb.turn = false;

            st = t.GetComponent<Stock>();
            st.image = images[i];

            t.transform.localPosition = new Vector3(i * 150 - 825, -480, 0);

            if (i == 0)
            {
                firstPlayerIButton = t;
            }
        }

        if (firstPlayerIButton != null)
        {
            firstPlayerIButton.transform.SetAsLastSibling();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Change();
        }
    }

    public void Change()
    {
        turn = !turn;

        if (turn)
        {
            turnText.transform.localPosition = new Vector3(0, -18, 10);
        }
        else
        {
            turnText.transform.localPosition = new Vector3(0, 18, 10);
        }

        Timer.ResetCounter();
    }

    public void Judge()
    {
        int r1 = Map.reach[false];
        int r2 = 29 - Map.reach[true];

        if (r1 == r2)
        {
            Win(0);
        }
        else if (r1 > r2)
        {
            Win(1);
        }
        else
        {
            Win(2);
        }
    }

    public void Win(int i)
    {
        tmp.text = $"Player{i} win!!";
        tmp.transform.parent.gameObject.SetActive(true);
        pc.SetActive(false);

        if (i == 0)
        {
            tmp.text = "Draw";
        }
    }

    public void Giveup(int i)
    {
        if (i == 1)
        {
            Win(1);
        }

        if (i == 2)
        {
            Win(2);
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene("HomeScene");
    }
}