```mermaid
graph TD;
    %% 登場人物とブラウザ
    subgraph Client [クライアントサイド (React)]
        A[ユーザー / 面接官 / 面接対象者] -->|ログイン / Google Auth| B(React App / SPA)
        B -->|① 音声データのストリーム送信<br>5秒ごと| C[MediaRecorder API]
    end

    %% ホスティング & 認証
    subgraph Frontend_Auth [Web・認証インフラ]
        D[Cloud Run<br>Reactホスティング] --- B
        E[Google Identity Platform<br>旧 Firebase Auth] --- B
    end

    %% バックエンド API と データベース
    subgraph Backend [バックエンド & データ管理]
        F[Cloud Run<br>APIサービス]
        G[(Cloud Firestore<br>メタデータ & ACL管理)]
    end

    %% ストレージとAI処理
    subgraph Storage_AI [ストレージ & AI処理パイプライン]
        H[(Cloud Storage - GCS<br>音声・JSON保管)]
        I[Cloud Functions / Run<br>イベント駆動型ワーカー]
        J[Vertex AI<br>Gemini 1.5/2.0 / Audio API]
    end

    %% フローの接続
    B -->|② APIリクエスト| F
    F -->|③ アクセス権の検証 / 保存| G
    C -->|④ 署名付きURL経由で<br>チャンク毎に直接アップロード| H
    H -->|⑤ ファイル完了を検知<br>Object Created| I
    I -->|⑥ 音声ファイルを読み込み| J
    J -->|⑦ 音声認識 & 要約 & JSON構造化| J
    I -->|⑧ 構造化データと要約を保存| G
    I -->|⑨ 構造化JSONファイルを保存| H

    %% アクセス制御の表現
    F -.->|⑩ 一時的な閲覧用Signed URL発行| H
```