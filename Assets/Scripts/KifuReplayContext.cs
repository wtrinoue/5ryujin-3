using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class KifuReplayContext
{
    public static GameRecord selectedKifu = null;

    public static void Set(GameRecord data)
    {
        selectedKifu = data;
    }

    public static void Clear()
    {
        selectedKifu = null;
    }

    public static bool HasKifu()
    {
        return selectedKifu != null;
    }
}