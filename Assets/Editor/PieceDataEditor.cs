using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PieceData))]
public class PieceDataEditor : Editor
{
    const int TileRange = 2;
    const int MagnetRange = 3;

    const float TileSize = 50f;
    const float MagnetSize = 16f;

    const float Offset = 60f;

    public override void OnInspectorGUI()
    {
        PieceData data = (PieceData)target;

        DrawDefaultInspector();
        GUILayout.Space(20);

        DrawGrid(data);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }
    }

    void DrawGrid(PieceData data)
    {
        GUILayout.Label("Shape Editor", EditorStyles.boldLabel);

        Rect start = GUILayoutUtility.GetRect(400, 400);

        Vector2 center = start.center;

        // =========================
        // タイル
        // =========================
        for (int y = -TileRange; y <= TileRange; y++)
        {
            for (int x = -TileRange; x <= TileRange; x++)
            {
                Vector2 pos = center + new Vector2(x * Offset, -y * Offset);

                DrawTile(data, new Vector2Int(x, y), pos);
            }
        }

        // =========================
        // マグネット（タイル間 + 外周含む）
        // =========================
        for (int y = -TileRange; y <= TileRange; y++)
        {
            for (int x = -TileRange; x <= TileRange; x++)
            {
                // 横（タイル間）
                {
                    Vector2 grid = new Vector2(x + 0.5f, y);
                    Vector2 pos = center + new Vector2((x + 0.5f) * Offset, -y * Offset);

                    DrawMagnet(data, grid, pos);
                }

                // 縦（タイル間）
                {
                    Vector2 grid = new Vector2(x, y + 0.5f);
                    Vector2 pos = center + new Vector2(x * Offset, -(y + 0.5f) * Offset);

                    DrawMagnet(data, grid, pos);
                }
            }
        }

        // =========================
        // 外周マグネット（上下左右1周）
        // =========================
        for (int i = -TileRange; i <= TileRange; i++)
        {
            float outer = TileRange + 0.5f;

            // 上
            DrawMagnet(
                data,
                new Vector2(i, -outer),
                center + new Vector2(i * Offset, -(-outer) * Offset)
            );

            // 下
            DrawMagnet(
                data,
                new Vector2(i, outer),
                center + new Vector2(i * Offset, -outer * Offset)
            );

            // 左
            DrawMagnet(
                data,
                new Vector2(-outer, i),
                center + new Vector2(-outer * Offset, -i * Offset)
            );

            // 右
            DrawMagnet(
                data,
                new Vector2(outer, i),
                center + new Vector2(outer * Offset, -i * Offset)
            );
        }
    }

    void DrawTile(PieceData data, Vector2Int grid, Vector2 screenPos)
    {
        bool contains = data.tiles.Contains(grid);

        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = contains ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.25f, 0.25f, 0.25f);

        Rect r = new Rect(screenPos.x - TileSize / 2, screenPos.y - TileSize / 2, TileSize, TileSize);

        if (GUI.Button(r, ""))
        {
            Undo.RecordObject(data, "Edit Tile");

            if (contains)
                data.tiles.Remove(grid);
            else
                data.tiles.Add(grid);
        }

        GUI.backgroundColor = prev;
    }

    void DrawMagnet(PieceData data, Vector2 grid, Vector2 screenPos)
    {
        bool contains = data.magnets.Contains(grid);

        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = contains ? Color.white : new Color(0.35f, 0.35f, 0.35f);

        Rect r = new Rect(screenPos.x - MagnetSize / 2, screenPos.y - MagnetSize / 2, MagnetSize, MagnetSize);

        if (GUI.Button(r, ""))
        {
            Undo.RecordObject(data, "Edit Magnet");

            if (contains)
                data.magnets.Remove(grid);
            else
                data.magnets.Add(grid);
        }

        GUI.backgroundColor = prev;
    }
}