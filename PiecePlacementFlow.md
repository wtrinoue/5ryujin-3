# 駒を選択してから置くまでの処理の流れ

このドキュメントでは、`Assets/Scripts` 内のスクリプトが連携して、プレイヤーが駒を選択してから盤面に配置するまでの流れをまとめます。

## 関係する主なクラス

- `PieceButton`: UI の駒選択ボタンを管理する
- `Stock`: 駒の残り枚数と在庫表示を管理する
- `PieceCursor`: 選択中の駒をマウス・タッチに追従させ、配置処理を行う
- `Map`: 盤面上に駒を置けるか判定し、配置情報を記録する
- `RecordManager`: 配置した手を棋譜として記録する
- `Board`: 現在の手番を管理し、配置後に手番を切り替える
- `Timer`: 手番交代時に制限時間をリセットする

## 全体の流れ

```text
駒ボタンを押す
↓
PieceButton が現在の手番か確認する
↓
通常駒なら Stock に選択処理を渡す
↓
Stock が残数を確認する
↓
PieceCursor が駒 prefab を生成する
↓
PieceCursor がマウス・タッチに追従する
↓
クリックまたは配置ボタンで Put() が呼ばれる
↓
Map が配置可能か判定する
↓
配置成功なら RecordManager が棋譜を記録する
↓
Stock の残数を減らす
↓
駒を PieceCursor から切り離して盤面に固定する
↓
Board が手番を切り替える
```

## 1. 駒ボタンを押す

UI 上の駒ボタンには `PieceButton` が付いています。

ボタンが押されると、`PieceButton.ClickSelect()` が呼ばれます。

```csharp
public void ClickSelect()
```

ここではまず、押されたボタンが現在の手番のプレイヤー用かどうかを確認します。

```csharp
if (Board.turn != turn)
{
    return;
}
```

`Board.turn` は現在の手番を表す static 変数です。`false` が 1P、`true` が 2P として扱われています。

現在の手番と違うプレイヤー側のボタンを押した場合は、そこで処理を止めます。

## 2. 通常駒は Stock に処理を渡す

通常の駒の場合、`PieceButton` は自分についている `Stock` に選択処理を渡します。

```csharp
stock.Select(number, turn);
```

`number` は駒の種類を表す番号です。`turn` はそのボタンがどちらのプレイヤー用かを表します。

一方、`number == 12` のタッチダウン駒は、在庫を使わずに直接 `PieceCursor` へ渡します。

```csharp
PieceCursor.instance.Select(number, null);
```

## 3. Stock が残数を確認する

`Stock.Select()` では、駒を選択できる状態か確認します。

```csharp
public void Select(int n, bool turn)
```

主に次の条件を見ています。

```csharp
if (turn != Board.turn) return;
if (count <= 0) return;
if (PieceCursor.instance == null) return;
```

つまり、以下のすべてを満たす必要があります。

- 現在の手番と一致している
- 駒の残数がある
- `PieceCursor` が存在している

条件を満たすと、`PieceCursor` に駒選択を依頼します。

```csharp
PieceCursor.instance.Select(n, this);
```

ここで `this` を渡しているため、配置成功後に `PieceCursor` から `Stock.Decrement()` を呼んで残数を減らせます。

## 4. PieceCursor が選択中の駒を生成する

`PieceCursor.Select()` は、選択された駒 prefab を生成する処理です。

```csharp
public void Select(int n, Stock s)
```

まず、現在選択中の駒があれば削除します。

```csharp
Trash();
```

次に、選択された駒番号を保持します。

```csharp
number = n;
```

そして `pieces` リストから対応する prefab を生成します。

```csharp
piece = Instantiate(pieces[number], transform);
```

生成された駒は `PieceCursor` の子オブジェクトになります。そのため、`PieceCursor` が移動すると、選択中の駒も一緒に移動します。

## 5. Magnet と Tile を集める

駒を生成したあと、`PieceCursor` は駒の子オブジェクトを調べます。

```csharp
if (child.CompareTag("Magnet"))
{
    childMagnets.Add(child);
}
else if (child.CompareTag("Tile"))
{
    childTiles.Add(child);
}
```

ここで集めた情報は、後で `Map.Add(this)` に渡されます。

- `childMagnets`: 磁石判定に使う座標
- `childTiles`: 実際に盤面を占有するマス

また、手番に応じて駒の色も変更します。

```csharp
sr.color = Board.turn ? color2p : color1p;
```

## 6. PieceCursor がマウス・タッチに追従する

`PieceCursor.Update()` では、毎フレーム入力を確認しています。

```csharp
void Update()
{
    if (Input.touchCount > 0)
    {
        HandleTouch();
    }
    else
    {
        HandleMouse();
    }
}
```

PC 操作では `HandleMouse()` が呼ばれます。マウス座標をワールド座標に変換し、整数座標に丸めてカーソル位置を更新します。

```csharp
transform.position = new Vector3(x, y, 0);
```

スマートフォン操作では `HandleTouch()` が呼ばれ、タッチ位置に合わせて同じようにカーソルを動かします。

## 7. 回転・反転・配置操作

PC 操作では、`HandleMouse()` 内で次の入力を処理しています。

```csharp
float scroll = Input.GetAxis("Mouse ScrollWheel");
if (piece != null && scroll != 0)
{
    Rotate(scroll);
}

if (Input.GetMouseButtonDown(0))
{
    Put();
}

if (Input.GetMouseButtonDown(1))
{
    FlipButton();
}
```

操作の対応は次の通りです。

- マウスホイール: 駒を 90 度単位で回転
- 左クリック: 駒を配置
- 右クリック: 駒を反転

UI ボタンから操作する場合は、次のメソッドが呼ばれます。

- `RotateButton()`
- `FlipButton()`
- `PutButton()`

最終的に、配置するときはどちらの操作でも `Put()` に入ります。

## 8. PieceCursor.Put() で配置処理を開始する

配置時には `PieceCursor.Put()` が呼ばれます。

```csharp
public void Put()
```

まず、現在の駒状態を取得します。

```csharp
int rotation = GetCurrentRotation();
bool flipped = IsCurrentlyFlipped();
float x = transform.position.x;
float y = transform.position.y;
bool player = Board.turn;
bool touchdown = (number == 12);
```

ここで取得した情報は、棋譜保存に使われます。

その後、実際に置けるかどうかを `Map` に判定させます。

```csharp
if (mm.Add(this))
```

`mm` は `PieceCursor` 内で作られている `Map` インスタンスです。

```csharp
private Map mm = new Map();
```

## 9. Map が配置できるか判定する

`Map.Add(PieceCursor piece)` は、盤面に駒を置けるかどうかを判定する中心処理です。

```csharp
public bool Add(PieceCursor piece)
```

この中では主に次の判定を行います。

- 駒のタイルがすでに置かれたタイルと重なっていないか
- 駒が盤面外にはみ出していないか
- 自分の駒と正しい数だけ隣接しているか
- 磁石位置が既存の磁石と正しく接続しているか
- 通常 P 駒やタッチダウン駒の特殊条件を満たしているか

例えば、配置済みのマスと重なっている場合は失敗します。

```csharp
if (placedTiles.ContainsKey(tp))
{
    return false;
}
```

盤面外にはみ出している場合も失敗します。

```csharp
if (tp.x < 0 || tp.x >= 60 || tp.y < 0 || tp.y >= 30)
{
    return false;
}
```

判定に成功した場合は、`placedTiles` や `magnetMap` に新しい配置情報を登録し、`true` を返します。

## 10. 配置成功後に棋譜を記録する

`Map.Add(this)` が `true` を返すと、`PieceCursor.Put()` 側で配置成功として扱われます。

棋譜管理用の `RecordManager` が設定されている場合、手を記録します。

```csharp
recordManager.AddMove(
    pieceType: number,
    rotation: rotation,
    flipped: flipped,
    x: x,
    y: y,
    player: player,
    touchdown: touchdown
);

recordManager.SaveRecord();
```

`RecordManager.AddMove()` では、`MoveData` を作成して `GameRecord.moves` に追加します。

記録される主な情報は次の通りです。

- 何手目か
- どちらのプレイヤーか
- 駒の種類
- 回転角
- 反転状態
- 配置座標
- タッチダウン駒かどうか

## 11. Stock の残数を減らす

配置成功後、通常駒では `Stock.Decrement()` が呼ばれます。

```csharp
stock.Decrement();
```

`Stock.Decrement()` は内部の `count` を 1 減らし、在庫表示の画像も 1 つ削除します。

```csharp
count--;

if (images.Count > 0)
{
    Destroy(images[images.Count - 1]);
    images.RemoveAt(images.Count - 1);
}
```

これにより、UI 上でも残り枚数が減ったように見えます。

## 12. 駒を盤面に固定する

配置に成功した駒は、`PieceCursor` の子から外されます。

```csharp
piece.transform.SetParent(transform.parent);
piece = null;
```

これにより、その駒はカーソルに追従しなくなり、盤面上に固定された状態になります。

`piece = null` にすることで、`PieceCursor` は現在何も選択していない状態になります。

## 13. Board が手番を切り替える

最後に、`Board.instance.Change()` が呼ばれます。

```csharp
Board.instance.Change();
```

`Board.Change()` では、現在の手番を反転します。

```csharp
turn = !turn;
```

さらに、ターン表示の位置を変え、タイマーもリセットします。

```csharp
Timer.ResetCounter();
```

これで 1 手分の配置処理が完了し、次のプレイヤーの手番になります。

## 処理の呼び出し関係

```text
PieceButton.ClickSelect()
  ↓
Stock.Select()
  ↓
PieceCursor.Select()
  ↓
PieceCursor.Update()
  ↓
PieceCursor.HandleMouse() または PieceCursor.HandleTouch()
  ↓
PieceCursor.Put()
  ↓
Map.Add()
  ↓
RecordManager.AddMove()
  ↓
RecordManager.SaveRecord()
  ↓
Stock.Decrement()
  ↓
Board.Change()
  ↓
Timer.ResetCounter()
```

タッチダウン駒の場合は、`Stock.Select()` を通らずに次の流れになります。

```text
PieceButton.ClickSelect()
  ↓
PieceCursor.Select(number, null)
  ↓
PieceCursor.Put()
  ↓
Map.Add()
  ↓
RecordManager.AddMove()
  ↓
RecordManager.SaveRecord()
  ↓
Board.Change()
```

## 重要なポイント

- 駒選択の入口は `PieceButton` または `Stock` です。
- 実際に選択中の駒を持っているのは `PieceCursor` です。
- 配置できるかどうかのルール判定は `Map` が担当しています。
- 配置が成功したときだけ `RecordManager` が棋譜を保存します。
- 配置成功後、駒は `PieceCursor` から切り離されて盤面に固定されます。
- 最後に `Board.Change()` によって手番が切り替わります。
