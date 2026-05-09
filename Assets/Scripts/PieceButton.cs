using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PieceButton : MonoBehaviour
{
    public int number; // 0～12
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
        Debug.Log("PieceButtonが押されました: " + gameObject.name + " number=" + number + " turn=" + turn);

        // 自分の手番でないボタンは押せない
        if (Board.turn != turn)
        {
            Debug.Log("手番が違うため選択できません。");
            return;
        }

        // TDボタンは Stock を使わず、直接 PieceCursor に渡す
        if (number == 12)
        {
            if (PieceCursor.instance != null)
            {
                PieceCursor.instance.Select(number, null);
            }
            else
            {
                Debug.LogError("PieceCursor.instance が見つかりません。");
            }

            return;
        }

        // 通常駒は Stock 経由
        if (stock == null)
        {
            Debug.LogError("Stock が付いていません: " + gameObject.name);
            return;
        }

        stock.Select(number, turn);
    }
}