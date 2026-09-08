# Dialogue Command Guide

この資料は、Google Sheets の Command 列に書く Yarn Spinner 用コマンドの使い方です。

基本フォーマット:

```text
[command:arg1:arg2:arg3]
```

同じ行で複数のコマンドを使う場合は、ブラケットごとに分けます。

```text
[background:BG_City_Day:0.5:black] [bgm:play:CityTheme:1.0] [char:show:momoka:Ch_Momoka_TF_default:0.25:0.3:false]
```

コマンド名やアクション名は小文字推奨です。画像名・音声名は Unity 上のアセット名に合わせてください。引数の中にスペースやコロンは入れない運用が安全です。

## Background

背景画像を変更します。画像は `Assets/Graphic/Backgrounds` の Sprite を参照します。

```text
[background:BG_City_Day:instant]
[background:BG_City_Day:0.6:black]
[bg:BG_City_Day:0.6:white]
```

引数:

```text
[background:画像名:切り替え時間:フェード色]
```

`background` と `bg` は同じ意味です。

`画像名` は背景 Sprite の名前です。例: `BG_City_Day`

`切り替え時間` は秒数、または `instant` です。`instant` は即時変更です。秒数を入れると、フェード色へ暗転してから画像を差し替え、再度表示します。

`フェード色` は省略可能です。省略時は `black` です。

使える色:

```text
black, white, red, green, blue, yellow, cyan, magenta, gray, grey, clear, #RRGGBB, #RRGGBBAA
```

## Character

キャラクターの表示、表情差分、位置、反転、色、サイズ、非表示を操作します。画像は `Assets/Graphic/Characters` の Sprite を参照します。

キャラクターは `character id` で管理されます。同じキャラクターを後から表情変更・移動・非表示にするため、最初に決めた ID を使い続けます。

`character id` は `Master` シートの話者名と同じ文字列にしてください。名前付きの台詞が表示されると、その ID のキャラクターだけが通常の色になり、それ以外の表示中キャラクターは少しグレーになります。話者名のないナレーションでは、全員が通常の色に戻ります。

桃香は特別扱いです。ID が `toka`、`momoka`、`白崎桃香` のいずれかで、`Ch_Toka_Face_*` の画像を指定すると、ゲーム側は `PlayerProfile.TokaCurrentBody` の衣装を下に置き、その上へ顔画像を元画像と同じ座標で重ねます。シートと Yarn には顔画像のコマンドだけを書きます。

```text
[char:show:toka:Ch_Toka_Face_default:0.5:0.5:false]
[char:face:toka:Ch_Toka_Face_angry:0.2]
```

`Ch_Toka_Face_*` 以外の桃香画像を指定した場合は、その画像を単体で表示します。衣装プリセットを使わない特別な場面では、完成済みの立ち絵 Sprite を指定してください。

例:

```text
[char:show:momoka:Ch_Momoka_TF_default:0.25:0.3:false]
[char:face:momoka:Ch_Momoka_TF_angry:0.2]
[char:move:momoka:0.75:0.4]
[char:flip:momoka:left]
[char:tint:momoka:#888888:0.2]
[char:scale:momoka:1.1:0.2]
[char:hide:momoka:0.3]
[char:clear:0.5]
```

### Show / Add

キャラクターを表示します。すでに同じ ID のキャラクターがいる場合は、その表示を更新します。

```text
[char:show:ID:画像名:X位置:フェード時間:反転]
[char:add:ID:画像名:X位置:フェード時間:反転]
```

`ID` は管理用の名前です。例: `momoka`

`画像名` は Sprite 名です。例: `Ch_Momoka_TF_default`

`X位置` は 0 から 1 の数値です。`0` では立ち絵の左端が画面左端より 250px 外、`0.5` では中央、`1` では立ち絵の右端が画面右端より 250px 外になります。

`フェード時間` は秒数、または `instant` です。

`反転` は省略可能です。通常は `false`、左右反転したい場合は `true` または `left` を使います。

### Face / Sprite / Variation

表示中キャラクターの表情差分を変更します。

```text
[char:face:ID:画像名:フェード時間]
[char:sprite:ID:画像名:フェード時間]
[char:variation:ID:画像名:フェード時間]
```

例:

```text
[char:face:momoka:Ch_Momoka_TF_angry:0.2]
```

`フェード時間` を入れると、軽くフェードアウトして画像を差し替えてから戻します。

### Move / Position

表示中キャラクターを左右に移動します。

```text
[char:move:ID:X位置:移動時間]
[char:position:ID:X位置:移動時間]
```

例:

```text
[char:move:momoka:0.75:0.4]
```

`移動時間` が `instant` または `0` の場合は即時移動です。

### Flip

表示中キャラクターを左右反転します。

```text
[char:flip:ID:反転]
```

反転する値:

```text
true, flip, flipped, left
```

通常向きに戻す値:

```text
false, normal, none, right
```

例:

```text
[char:flip:momoka:left]
[char:flip:momoka:right]
```

### Tint / Color

表示中キャラクターの色を変更します。暗くしたり、白に戻したりする用途です。

```text
[char:tint:ID:色:フェード時間]
[char:color:ID:色:フェード時間]
```

例:

```text
[char:tint:momoka:#888888:0.2]
[char:tint:momoka:white:0.2]
```

使える色:

```text
black, white, gray, grey, clear, transparent, red, green, blue, yellow, cyan, magenta, #RRGGBB, #RRGGBBAA
```

### Scale / Size

表示中キャラクターの大きさを変更します。`1` が通常サイズです。

```text
[char:scale:ID:倍率:変更時間]
[char:size:ID:倍率:変更時間]
```

例:

```text
[char:scale:momoka:1.1:0.2]
[char:scale:momoka:1:0.2]
```

### Hide / Remove

表示中キャラクターを消します。

```text
[char:hide:ID:フェード時間]
[char:remove:ID:フェード時間]
```

例:

```text
[char:hide:momoka:0.3]
```

### Clear / Hide All / Remove All

表示中の全キャラクターを消します。

```text
[char:clear:フェード時間]
[char:hide_all:フェード時間]
[char:remove_all:フェード時間]
```

例:

```text
[char:clear:0.5]
```

## BGM

BGM を再生・停止・一時停止します。音声は `Assets/Resources/Audio/BGM` の AudioClip を参照します。

```text
[bgm:play:CityTheme]
[bgm:play:CityTheme:1.0]
[bgm:crossfade:BattleTheme:1.0]
[bgm:stop:0.5]
[bgm:pause]
[bgm:resume]
[bgm:volume:0.7]
```

### Play

BGM を再生します。

```text
[bgm:play:音声名:フェード時間]
```

`フェード時間` は省略可能です。省略時、または `instant` の場合は即時再生です。

### Crossfade

現在の BGM から次の BGM へクロスフェードします。

```text
[bgm:crossfade:音声名:フェード時間]
[bgm:cross:音声名:フェード時間]
```

### Stop

BGM を停止します。

```text
[bgm:stop:フェード時間]
```

`instant` または `0` の場合は即時停止です。

### Pause / Resume

BGM を一時停止・再開します。

```text
[bgm:pause]
[bgm:resume]
[bgm:unpause]
```

### Volume

BGM 音量を変更します。範囲は `0` から `1` です。

```text
[bgm:volume:0.7]
```

## SE

効果音を再生します。音声は `Assets/Resources/Audio/SE` と `Assets/Resources/Audio/BATTLE_SE` の AudioClip を参照します。

```text
[se:play:DoorOpen]
[se:play:Click:0.8]
[se:volume:0.6]
```

### Play

効果音を再生します。

```text
[se:play:音声名:音量]
```

`音量` は省略可能です。指定する場合は `0` から `1` です。

### Volume

SE 全体の音量を変更します。範囲は `0` から `1` です。

```text
[se:volume:0.6]
```

## Fade

画面全体をフェードします。背景切り替えとは別の、画面全体の演出用フェードです。

```text
[fade:out:black:0.5]
[fade:in:black:0.5]
[fade:to:black:0.5:0.3]
[fade:clear]
```

### Out

画面を指定色で覆います。

```text
[fade:out:色:時間]
```

### In

指定色で覆われた状態から透明に戻します。

```text
[fade:in:色:時間]
```

### To

指定色・指定透明度までフェードします。

```text
[fade:to:色:透明度:時間]
```

`透明度` は `0` から `1` です。`0` が透明、`1` が完全に不透明です。

### Clear

フェードを透明に戻します。

```text
[fade:clear]
[fade:clear:0.3]
```

使える色:

```text
black, white, gray, grey, clear, transparent, red, green, blue, yellow, cyan, magenta, #RRGGBB, #RRGGBBAA
```

## Shake

会話画面を揺らします。

```text
[shake]
[shake:0.3:8]
```

引数:

```text
[shake:時間:強さ]
```

`時間` は秒数です。省略時は `0.25` です。

`強さ` は揺れ幅です。省略時は `8` です。まず会話用 Canvas を揺らし、見つからない場合は Camera を揺らします。

## Dialogue UI

Yarn の会話 UI を表示・非表示にします。背景やキャラクターは消えません。

```text
[dialogue:hide]
[dialogue:show]
[dialogue:toggle]
[dialogue:hide:0.2]
[dialogue:show:0.2]
```

引数:

```text
[dialogue:アクション:フェード時間]
```

`アクション` は `show`, `hide`, `toggle` です。

`フェード時間` は省略可能です。省略時は即時変更です。

## よく使う組み合わせ

背景と BGM を同時に変える:

```text
[background:BG_City_Day:0.6:black] [bgm:crossfade:CityTheme:1.0]
```

キャラクターを左に出す:

```text
[char:show:momoka:Ch_Momoka_TF_default:0.25:0.3:false]
```

表情を変えて効果音を鳴らす:

```text
[char:face:momoka:Ch_Momoka_TF_angry:0.2] [se:play:Surprise:0.8]
```

一枚絵や演出を見せるために会話 UI を隠す:

```text
[dialogue:hide:0.2] [fade:out:black:0.5]
```

演出後に戻す:

```text
[fade:in:black:0.5] [dialogue:show:0.2]
```

全キャラクターを消して背景変更:

```text
[char:clear:0.3] [background:BG_Hideout:0.6:black]
```

