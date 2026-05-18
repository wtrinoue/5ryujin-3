using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PieceButton : MonoBehaviour
{
    public PieceType pieceType; // ★ int → enum に変更
    public bool turn;

    [SerializeField] TextMeshProUGUI tmp;

    private Stock stock;

    void Start()
    {
        stock = GetComponent<Stock>();

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(ClickSelect);
        }
        else
        {
            Debug.LogError("Button が付いていません: " + gameObject.name);
        }
    }

    public void ClickSelect()
    {
        Debug.Log("PieceButtonが押されました: " + gameObject.name + " pieceType=" + pieceType + " turn=" + turn);

        if (Board.turn != turn)
        {
            Debug.Log("手番が違うため選択できません。");
            return;
        }

        // TD処理
        if (pieceType == PieceType.td)
        {
            if (PieceCursor.instance != null)
            {
                PieceCursor.instance.Select(pieceType, null);
            }
            return;
        }

        if (stock == null)
        {
            Debug.LogError("Stock が付いていません: " + gameObject.name);
            return;
        }

        stock.Select(pieceType, turn);
    }
}