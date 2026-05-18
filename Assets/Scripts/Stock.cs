using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Stock : MonoBehaviour
{
    private int count = 5;

    public PieceType pieceType;
    public bool isFirstPlayer;
    public GameObject image;
    public float imageDistance;

    private readonly List<GameObject> stockIcons = new List<GameObject>();

    void Start()
    {
        if (image == null)
        {
            Debug.LogWarning("Stock image is not set: " + pieceType);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject im = Instantiate(image, transform);

            im.transform.localScale = new Vector3(15, 15, 1);
            im.transform.localPosition = new Vector3((-2 + i) * imageDistance, 0, 0);

            SortingGroup sg = im.GetComponent<SortingGroup>();
            if (sg != null)
            {
                sg.sortingOrder = 10 - i;
            }

            stockIcons.Add(im);
        }
    }

    void OnMouseDown()
    {
        Select(pieceType, isFirstPlayer);
    }

    public void Decrement()
    {
        count--;

        if (stockIcons.Count > 0)
        {
            Destroy(stockIcons[stockIcons.Count - 1]);
            stockIcons.RemoveAt(stockIcons.Count - 1);
        }
    }

    public void Select(PieceType type, bool turn)
    {
        if (turn != Board.turn) return;
        if (count <= 0) return;
        if (PieceCursor.instance == null) return;

        PieceCursor.instance.Select(type, this);
    }
}
