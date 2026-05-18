using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    [SerializeField] GameObject pc;

    // =========================
    // ★ Inspectorで対応付ける用
    // =========================
    [System.Serializable]
    public struct PieceImagePair
    {
        public PieceType type;
        public GameObject image;
    }

    [SerializeField]
    private List<PieceImagePair> images;

    private Dictionary<PieceType, GameObject> imageDict;

    void Start()
    {
        turn = false;
        instance = this;

        // Dictionary化
        imageDict = new Dictionary<PieceType, GameObject>();
        foreach (var pair in images)
        {
            imageDict[pair.type] = pair.image;
        }

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

        // =========================
        // PieceTypeベース生成
        // =========================
        int pieceIndex = 0;

        foreach (PieceType type in System.Enum.GetValues(typeof(PieceType)))
        {
            if (!imageDict.TryGetValue(type, out GameObject imagePrefab))
            {
                Debug.LogWarning("PieceType の画像が設定されていません: " + type);
                continue;
            }

            // 上側（2P）
            GameObject t = Instantiate(button, canvas.transform);
            var pb = t.GetComponent<PieceButton>();
            pb.pieceType = type;
            pb.turn = true;

            var st = t.GetComponent<Stock>();
            st.pieceType = type;
            st.isFirstPlayer = true;
            st.image = imagePrefab;

            t.transform.localPosition = new Vector3(pieceIndex * 150 - 825, 480, 0);

            // 下側（1P）
            t = Instantiate(button, canvas.transform);
            pb = t.GetComponent<PieceButton>();
            pb.pieceType = type;
            pb.turn = false;

            st = t.GetComponent<Stock>();
            st.pieceType = type;
            st.isFirstPlayer = false;
            st.image = imagePrefab;

            t.transform.localPosition = new Vector3(pieceIndex * 150 - 825, -480, 0);

            if (type == PieceType.F)
            {
                firstPlayerIButton = t;
            }

            pieceIndex++;
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
