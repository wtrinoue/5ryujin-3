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

        // 手番チェック（旧と同じ）
        if (Board.turn != turn)
        {
            Debug.Log("手番が違うため選択できません。");
            return;
        }

        // TD処理（旧: number == 12）
        if (pieceType == PieceType.td)
        {
            if (PieceCursor.instance != null)
            {
                PieceCursor.instance.Select(pieceType, null);
            }
            else
            {
                Debug.LogError("PieceCursor.instance が見つかりません。");
            }
            return;
        }

        // 通常駒（旧: Stock経由）
        if (stock == null)
        {
            Debug.LogError("Stock が付いていません: " + gameObject.name);
            return;
        }

        stock.Select(pieceType, turn);
    }
}