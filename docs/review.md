# Phase 3 コードレビュー

**レビュー日**: 2026-01-19
**レビュアー**: Claude (Reviewer)
**対象**: Phase 3 - Pythonコアロジック - 素材管理 (Core Logic)

---

## 1. レビュー対象ファイル

### 変更ファイル
| ファイル | 変更内容 |
|----------|----------|
| `backend/main.py` | Asset API、TTS API追加、静的ファイル配信設定 |
| `backend/requirements.txt` | gTTS依存関係追加 |
| `Services/PythonService.cs` | TTS生成、キャラクター取得メソッド追加 |
| `ViewModels/ScriptEditorViewModel.cs` | バックエンドからキャラクター一覧取得 |

### 新規追加ファイル
| ファイル | 役割 |
|----------|------|
| `backend/services/__init__.py` | Servicesパッケージ |
| `backend/services/asset_manager.py` | キャラクターアセット管理 |
| `backend/services/tts_service.py` | 音声合成サービス（gTTS） |
| `backend/assets/characters/zundamon/info.json` | サンプルキャラクター設定 |

---

## 2. Phase 3 要件との整合性チェック

| 要件 | 状態 | 確認内容 |
|------|------|----------|
| 画像ディレクトリ構造の整備 | ✅ 完了 | `backend/assets/characters/` 構造 |
| キャラクター一覧API | ✅ 完了 | `GET /assets/characters` |
| キャラクター画像一覧API | ✅ 完了 | `GET /assets/characters/{id}/images` |
| TTSインターフェース定義 | ✅ 完了 | `TTSService` クラス |
| Google TTS実装 | ✅ 完了 | gTTS ライブラリ使用 |
| TTS API実装 | ✅ 完了 | `POST /tts/generate` |
| C#側からのキャラクター取得 | ✅ 完了 | `GetCharactersJsonAsync()` |
| C#側からのTTS呼び出し | ✅ 完了 | `GenerateTtsAsync()` |

**Phase 3 要件達成率: 100%**

---

## 3. コード品質レビュー

### 3.1. backend/services/asset_manager.py

**良い点**:
- ディレクトリの存在チェックと自動作成
- 複数の起動パスに対応（`backend/assets` または `assets`）
- 画像拡張子のフィルタリング（`.png`, `.jpg`, `.jpeg`, `.webp`）

**コード例**:
```python
def get_characters(self) -> List[Dict[str, str]]:
    chars = []
    for item in os.listdir(self.characters_dir):
        path = os.path.join(self.characters_dir, item)
        if os.path.isdir(path):
            chars.append({
                "id": item,
                "name": item.capitalize(),
                "path": path
            })
    return chars
```

### 3.2. backend/services/tts_service.py

**良い点**:
- UUID でユニークなファイル名生成（競合回避）
- 空テキストのバリデーション
- `cleanup_temp_files()` でリソース解放可能
- エラーハンドリング（try-catch）

**注意点**:
- gTTS は無料のGoogle TTSを使用（APIキー不要）
- 将来的なVOICEVOX対応のため、インターフェース抽象化を検討（Phase 5以降）

### 3.3. backend/main.py の追加API

**良い点**:
- Pydantic `BaseModel` でリクエストバリデーション
- 静的ファイル配信（`/static/assets`, `/static/audio`）
- エラー時に `HTTPException` で適切なレスポンス

**API一覧**:
| エンドポイント | メソッド | 説明 |
|---------------|---------|------|
| `/assets/characters` | GET | キャラクター一覧取得 |
| `/assets/characters/{id}/images` | GET | キャラクター画像一覧 |
| `/tts/generate` | POST | 音声合成 |
| `/static/assets/*` | GET | アセット静的配信 |
| `/static/audio/*` | GET | 音声ファイル静的配信 |

### 3.4. Services/PythonService.cs

**良い点**:
- `GenerateTtsAsync()` で音声合成API呼び出し
- `GetCharactersJsonAsync()` でキャラクター取得
- JSON パース に `JsonDocument` を使用

### 3.5. ViewModels/ScriptEditorViewModel.cs

**良い点**:
- バックエンドからキャラクター一覧を動的取得
- ダミーキャラクターのハードコードを削除
- エラー時のフォールバック（Fallbackキャラクター）
- `PropertyNameCaseInsensitive = true` でJSONマッピング柔軟性

**注意点**:
- `_ = InitializeAsync()` - Fire-and-forget パターン。例外は内部でキャッチされているため許容。

---

## 4. アーキテクチャ評価

### 4.1. サービス分離

| レイヤー | 責務 |
|----------|------|
| `main.py` | APIルーティング |
| `AssetManager` | ファイルシステム操作 |
| `TTSService` | 音声合成ロジック |

**評価**: 責務が適切に分離されており、テストしやすい構造

### 4.2. 将来のTTS拡張性

現在の `TTSService` はgTTSに特化していますが、将来的にVOICEVOX等に対応する場合：

```python
# 将来的な抽象化例
class ITTSService(ABC):
    @abstractmethod
    def generate_audio_file(self, text: str, lang: str) -> str:
        pass

class GoogleTTSService(ITTSService):
    ...

class VoicevoxTTSService(ITTSService):
    ...
```

**現時点の判断**: Phase 3 では具体的な実装で問題なし。Phase 5 で必要に応じて抽象化。

---

## 5. セキュリティ要件チェック

| 要件 | 状態 | 確認内容 |
|------|------|----------|
| APIキーのメモリ保持のみ | ✅ OK | 変更なし、維持 |
| TTS APIでのAPIキー使用 | ✅ OK | gTTSは無料API、APIキー不要 |
| ファイルパス漏洩 | ✅ OK | 相対URL `/static/audio/` で返却 |

**補足**: gTTSはGoogleの無料Text-to-Speech APIを使用しており、APIキーは不要です。将来的に有料APIに切り替える場合は、セキュリティ要件に従いAPIキーをインメモリで管理する必要があります。

---

## 6. 問題点と改善提案

### 6.1. 軽微な問題（今後のフェーズで対応可）

| # | 問題 | 影響度 | 提案 |
|---|------|--------|------|
| 1 | TTSServiceの抽象化未実施 | 低 | Phase 5でVOICEVOX対応時に検討 |
| 2 | info.jsonが未使用 | 低 | Phase 4で活用予定 |
| 3 | 一時音声ファイルの自動クリーンアップ未実装 | 低 | アプリ終了時に `cleanup_temp_files()` 呼び出しを検討 |

### 6.2. 対応不要（確認済み）

| 項目 | 理由 |
|------|------|
| `__pycache__` ディレクトリ | .gitignoreに追加推奨（軽微） |
| Fire-and-forget初期化 | 例外ハンドリング済み |

---

## 7. 最終判定

| 項目 | 判定 |
|------|------|
| Phase 3 要件達成 | **完了** |
| コード品質 | **良好** |
| サービス分離 | **適切** |
| セキュリティ要件 | **維持** |
| 致命的な問題 | **なし** |

## 結論

**Phase 3 の実装を承認いたします。**

キャラクターアセット管理とTTS基盤が適切に実装されており、バックエンドとフロントエンドの連携も正しく動作しています。Phase 4（動画生成）への移行に問題はありません。

---

*以上*
