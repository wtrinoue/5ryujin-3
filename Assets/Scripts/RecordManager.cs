using System;
using System.IO;
using UnityEngine;

public class RecordManager : MonoBehaviour
{
    public GameRecord record = new GameRecord();

    string FilePath()
    {
        return Path.Combine(Application.persistentDataPath, "record.json");
    }

    // =========================
    // ★追加：次の棋譜番号を取得
    // KIF.001 ～ KIF.999
    // 999 の次は 001
    // =========================
    int GetNextKifNumber()
    {
        int num = PlayerPrefs.GetInt("KIF_NO", 0);

        num++;

        if (num > 999)
        {
            num = 1;
        }

        PlayerPrefs.SetInt("KIF_NO", num);
        PlayerPrefs.Save();

        return num;
    }

    // =========================
    // ★追加：KIF.001 形式の名前を作る
    // =========================
    string GetKifName(int num)
    {
        return "KIF." + num.ToString("D3");
    }

    // =========================
    // 手の記録
    // =========================
    public void AddMove(
        PieceType pieceType,
        int rotation,
        bool flipped,
        int x,
        int y,
        bool player,
        bool touchdown
    )
    {
        MoveData move = new MoveData();

        move.turn = record.moves.Count + 1;
        move.player = player;
        move.pieceType = pieceType;
        move.rotation = rotation;
        move.flipped = flipped;
        move.x = x;
        move.y = y;
        move.touchdown = touchdown;

        record.moves.Add(move);
    }

    // =========================
    // record.json 保存（従来）
    // =========================
    public void SaveRecord()
    {
        string json = JsonUtility.ToJson(record, true);
        File.WriteAllText(FilePath(), json);
        Debug.Log("棋譜保存(record.json): " + FilePath());
    }

    // =========================
    // record.json 読込（従来）
    // =========================
    public void LoadRecord()
    {
        string path = FilePath();

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            record = JsonUtility.FromJson<GameRecord>(json);
            Debug.Log("棋譜読込: " + path);
        }
        else
        {
            Debug.LogWarning("棋譜ファイルがありません: " + path);
        }
    }

    // =========================
    // 記録クリア
    // =========================
    public void ClearRecord()
    {
        record = new GameRecord();
    }

    // =========================
    // 棋譜として保存（KifuManager連携）
    // Recクリック時に KIF.001 ～ KIF.999 で保存
    // =========================
    public void SaveAsKifu()
    {
        // 安全チェック
        if (record == null || record.moves == null || record.moves.Count == 0)
        {
            Debug.LogWarning("保存する棋譜がありません");
            return;
        }

        if (KifuManager.instance == null)
        {
            Debug.LogError("KifuManager が存在しません");
            return;
        }

        // 名前（KIF.001 ～ KIF.999 + 日時）
        int kifNo = GetNextKifNumber();

        string dateText = DateTime.Now.ToString("yyyy/MM/dd");
        string name = GetKifName(kifNo) + "  " + dateText;

        // 参照コピーを防ぐために JSON 経由でコピー
        string json = JsonUtility.ToJson(record);
        GameRecord copy = JsonUtility.FromJson<GameRecord>(json);

        // 保存
        KifuManager.instance.SaveKifu(name, copy);

        Debug.Log("棋譜保存(Kifu): " + name + " 手数=" + copy.moves.Count);
    }
}
