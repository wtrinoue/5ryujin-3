using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Stock : MonoBehaviour
{
    int count = 5;
    public int pieceNumber;     // この在庫が何番の駒か
    public bool isFirstPlayer;  // 先手側なら false / 後手側なら true など、Board.turn と合わせる

    public GameObject image;
    public GameObject image2;
    public List<GameObject> images = new List<GameObject>();
    public float imageDistance;

    void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject im = Instantiate(image, transform);

            im.transform.localScale = new Vector3(15, 15, 1);
            im.transform.localPosition = new Vector3((-2 + i) * imageDistance, 0, 0);

            SortingGroup sg = im.GetComponent<SortingGroup>();
            if (sg != null)
            {
                sg.sortingOrder = 10 - i;
            }

            images.Add(im);
        }
    }

    void OnMouseDown()
    {
        Select(pieceNumber, isFirstPlayer);
    }

    public void Decrement()
    {
        count--;

        if (images.Count > 0)
        {
            Destroy(images[images.Count - 1]);
            images.RemoveAt(images.Count - 1);
        }
    }

    public void Select(int n, bool turn)
    {
        if (turn != Board.turn) return;
        if (count <= 0) return;
        if (PieceCursor.instance == null) return;

        PieceCursor.instance.Select(n, this);
    }
}