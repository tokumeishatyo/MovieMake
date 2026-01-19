# Phase 2 コードレビュー

**レビュー日**: 2026-01-19
**レビュアー**: Claude (Reviewer)
**対象**: Phase 2 - データモデルと台本UI実装 (Core Data & UI)

---

## 1. レビュー対象ファイル

### 変更ファイル
| ファイル | 変更内容 |
|----------|----------|
| `App.xaml.cs` | MainWindow を static プロパティ化、コード簡略化 |
| `MainWindow.xaml` | Frame-based navigation 導入 |
| `MainWindow.xaml.cs` | SetupPage への初期ナビゲーション |
| `Services/PythonService.cs` | 動的ポート割り当て、エラーハンドリング強化 |
| `backend/main.py` | エラーハンドリング追加、Python 3.9互換対応 |

### 新規追加ファイル
| ファイル | 役割 |
|----------|------|
| `Models/Script.cs` | 台本データモデル |
| `Models/Line.cs` | 行データモデル |
| `Models/Character.cs` | キャラクターデータモデル |
| `Services/ScriptManager.cs` | 台本のJSON保存/読み込み |
| `Services/FilePickerService.cs` | ファイルダイアログ処理 |
| `ViewModels/SetupViewModel.cs` | セットアップ画面ViewModel（旧MainViewModel） |
| `ViewModels/ScriptEditorViewModel.cs` | 台本編集画面ViewModel |
| `Views/SetupPage.xaml(.cs)` | セットアップ画面 |
| `Views/ScriptEditorPage.xaml(.cs)` | 台本編集画面 |

### 削除ファイル
| ファイル | 理由 |
|----------|------|
| `ViewModels/MainViewModel.cs` | SetupViewModelに置き換え |

---

## 2. Phase 2 要件との整合性チェック

| 要件 | 状態 | 確認内容 |
|------|------|----------|
| Script データモデル定義 | ✅ 完了 | `Models/Script.cs` - Title, Characters, Lines |
| Line データモデル定義 | ✅ 完了 | `Models/Line.cs` - Id, CharacterId, Text |
| Character データモデル定義 | ✅ 完了 | `Models/Character.cs` - Id, Name, DefaultVoiceId, ImageBasePath |
| テキスト入力エリア | ✅ 完了 | `ScriptEditorPage.xaml` - TextBox |
| タイムライン(リスト)表示 | ✅ 完了 | `ScriptEditorPage.xaml` - ListView |
| 話者設定プルダウン | ✅ 完了 | `ScriptEditorPage.xaml` - ComboBox |
| ScriptManager (Load/Save) | ✅ 完了 | `Services/ScriptManager.cs` |
| JSON シリアライズ | ✅ 完了 | System.Text.Json 使用 |

**Phase 2 要件達成率: 100%**

---

## 3. コード品質レビュー

### 3.1. データモデル (Models/)

**良い点**:
- シンプルで明確な構造
- `Guid.NewGuid().ToString()` でユニークID生成
- `ObservableCollection<Line>` でUI自動更新対応
- nullable 対応 (`DefaultVoiceId?`, `ImageBasePath?`)

**コード例 (Script.cs)**:
```csharp
public class Script
{
    public string Title { get; set; } = "Untitled Script";
    public List<Character> Characters { get; set; } = new();
    public ObservableCollection<Line> Lines { get; set; } = new();
}
```

### 3.2. ScriptManager.cs

**良い点**:
- 非同期I/O (`async/await`)
- `WriteIndented = true` で可読性の高いJSON出力
- `using` で適切なリソース解放

**注意点**:
- `LoadScriptAsync` がファイル不在時に `null` を返す設計 → 呼び出し元で適切にハンドリング済み

### 3.3. FilePickerService.cs

**良い点**:
- WinUI 3 の FilePicker を正しく初期化（`InitializeWithWindow`）
- `App.MainWindow` からハンドル取得

### 3.4. ScriptEditorViewModel.cs

**良い点**:
- `AddLine` / `RemoveLine` コマンドの実装
- `SaveScriptAsync` / `LoadScriptAsync` でファイルI/O
- エラーハンドリング（try-catch）
- ステータスメッセージでユーザーフィードバック

**注意点**:
- `Line 27`: ダミーキャラクター追加（TODOコメントあり）→ Phase 3で対応予定、現時点では問題なし

### 3.5. SetupViewModel.cs

**良い点**:
- `ConnectionSuccessful` イベントでナビゲーション通知
- Phase 1のMainViewModelから適切にリファクタリング

### 3.6. ScriptEditorPage.xaml

**良い点**:
- `ElementName=RootPage` でDataTemplate内からViewModelにバインド
- `x:DataType="models:Line"` で型指定
- ComboBoxの `SelectedValuePath` / `DisplayMemberPath` 設定

**技術的メモ**:
- WinUI 3 の DataTemplate 内から親の ViewModel にバインドする `ElementName` パターンは適切

### 3.7. PythonService.cs の改善

**良い点**:
- **動的ポート割り当て**: `GetFreeTcpPort()` でポート競合回避
- **ファイル存在チェック**: `File.Exists(pythonScript)` でエラー早期検出
- **プロセス異常終了検知**: `_pythonProcess.HasExited` チェック追加
- **環境変数でポート渡し**: `psi.EnvironmentVariables["PORT"]` で安全にポート伝達

### 3.8. backend/main.py の改善

**良い点**:
- **Python 3.9 互換**: `str | None` → `Optional[str]` に変更
- **エラーハンドリング強化**: try-catch とスタックトレース出力
- **デバッグログ**: `log_level="debug"` で詳細ログ

---

## 4. アーキテクチャ改善の評価

### 4.1. Frame-based Navigation 導入

| 変更前 | 変更後 |
|--------|--------|
| MainWindow に直接UI配置 | MainWindow → Frame → Pages |
| 単一画面 | SetupPage → ScriptEditorPage |

**評価**: 画面遷移が明確になり、今後のPhaseで画面追加が容易になる良いリファクタリング

### 4.2. ViewModel分離

| 変更前 | 変更後 |
|--------|--------|
| MainViewModel (複合) | SetupViewModel + ScriptEditorViewModel |

**評価**: 責務分離が明確になり、テストしやすい構造

---

## 5. セキュリティ要件チェック

| 要件 | 状態 | 確認内容 |
|------|------|----------|
| APIキーのメモリ保持のみ | ✅ OK | SetupViewModelで引き続き維持 |
| ファイル保存禁止 | ✅ OK | ScriptManagerはAPIキーを保存しない |
| 新規コードにセキュリティ問題 | ✅ OK | 問題なし |

---

## 6. 問題点と改善提案

### 6.1. 軽微な問題（今後のフェーズで対応可）

| # | 問題 | 影響度 | 提案 |
|---|------|--------|------|
| 1 | ダミーキャラクターのハードコード | 低 | Phase 3でバックエンド連携時に解決予定 |
| 2 | FilePickerServiceが毎回newされる | 低 | DIまたはシングルトン化を検討 |

### 6.2. 対応不要（確認済み）

| 項目 | 理由 |
|------|------|
| MainViewModel.cs 削除 | SetupViewModelに適切に置き換え |
| Converters/ 未変更 | Phase 1で作成済み、継続使用 |

---

## 7. 最終判定

| 項目 | 判定 |
|------|------|
| Phase 2 要件達成 | **完了** |
| コード品質 | **良好** |
| アーキテクチャ改善 | **適切** |
| セキュリティ要件 | **維持** |
| 致命的な問題 | **なし** |

## 結論

**Phase 2 の実装を承認いたします。**

全ての要件が適切に実装されており、Frame-based navigation や動的ポート割り当てなどの追加改善も良好です。データモデルとUI、ファイルI/Oが正しく連携しており、Phase 3への移行に問題はありません。

---

*以上*
