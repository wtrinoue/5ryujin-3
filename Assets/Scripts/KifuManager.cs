using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class KifuData
{
    public string name;
    public GameRecord record;
}

[System.Serializable]
public class KifuList
{
    public List<KifuData> list = new List<KifuData>();
}

public class KifuManager : MonoBehaviour
{
    public static KifuManager instance;

    private string path;
    private KifuList kifuList = new KifuList();

    private const int MAX_KIFU = 6;

    // 選択された棋譜を一時保持
    public KifuData selectedKifu;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            path = Application.persistentDataPath + "/kifu.json";
            Load();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveKifu(string name, GameRecord record)
    {
        KifuData kd = new KifuData();
        kd.name = name;
        kd.record = record;

        kifuList.list.Add(kd);

        while (kifuList.list.Count > MAX_KIFU)
        {
            kifuList.list.RemoveAt(0);
        }

        SaveToFile();

        Debug.Log("棋譜保存: " + name);
    }

    private void SaveToFile()
    {
        string json = JsonUtility.ToJson(kifuList, true);
        File.WriteAllText(path, json);
    }

    public void Load()
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            kifuList = JsonUtility.FromJson<KifuList>(json);

            if (kifuList == null)
            {
                kifuList = new KifuList();
            }
            if (kifuList.list == null)
            {
                kifuList.list = new List<KifuData>();
            }

            while (kifuList.list.Count > MAX_KIFU)
            {
                kifuList.list.RemoveAt(0);
            }
        }
        else
        {
            kifuList = new KifuList();
        }
    }

    public List<KifuData> GetKifuList()
    {
        return kifuList.list;
    }
}