1. フロントエンド (React)

React SPA（シングルページアプリケーション）で完結

Web Audio APIを使用してブラウザ上で録音し、生の音声データ（Blob）としてバックエンドAPIに送信

2. バックエンド API (Node.js / Python / Go など)

独立したAPIサーバー

フロントから送られてきた音声バイナリをストリームで受け取り、Google Drive APIを経由して指定フォルダへ直接アップロード

アップロード完了後、DBへのレコード追加と、AI処理（文字起こし・要約）のトリガーを実行

データベース (PostgreSQL, MySQL, Cloud SQL など)

PostgreSQLで

AudioRecords テーブル: メタデータ、ステータス、Google DriveのファイルID、文字起こし結果、最終要約を格納。メールアドレス一覧などは、配列型（JSON/JSONB）または中間テーブルでスマートに管理

Users テーブル: ユーザー名とメールアドレス（台帳情報）これはGoogle IAMに任せたい（FireBaseか？）

4. 認証・認可 (Google IAM & OAuth 2.0)

Google Identity Services (OAuth 2.0) を組み込み、ユーザーに「Googleでサインイン」させます。これはのちにEntra IDとかOktaに対応可能？

サービス間の認可: バックエンドAPIがGoogle Driveや後述するAIサービスにアクセスする際は、Google Cloud上のサービスアカウントとIAM（Identity and Access Management）を使用して安全に権限（roles/storage.objectCreator や roles/aiplatform.user など）を制御

5. AI処理・データ連携 (Vertex AI Gemini API)


レイヤー	採用するGoogle Cloudサービス	選定理由と役割
1. フロントエンド	
Cloud Storage + Cloud CDN
→Firebase Hosting（React SPAなので標準のDDoS防御で十分）
：SSL、ドメイン、CDNをコマンドで制御（IaC）
：GitHubのPull Requestごとに、一時的な確認用URL（プレビューチャネル）を自動生成
：ドメインを設定すればSSL証明書も自動で無料発行・更新される。

Reactのビルド成果物（静的ファイル）を配置。低コストで高速に配信。Firebase Hostingの裏側もこれらなので、どちらでも問題ありません。
2. バックエンドAPI	Cloud Run	コンテナ化したAPI（Node.js/Python等）を動かすサーバーレス環境。音声アップロードのストリーム処理（最大32GB/リクエスト）や、最長60分の処理時間をサポートしているため、今回の用途に最適です。
3. データベース	Cloud SQL for PostgreSQL	フルマネージドなPostgreSQL。JSONB型をサポートしているため、メールアドレス一覧などの配列データもスマートに扱えます。
4. 認証・認可	
Firebase Authentication

バックエンドを守る場合（SQLインジェクション、クロスサイトスクリプティング（XSS）、ブルートフォース）
user-[ 外部アプリケーションロードバランサ（HTTPS LB） ]-[Google Cloud Armor (WAF) ]
                                                    -[Cloud CDN (キャッシュ配信)]-[ Cloud Run (バックエンドAPI) ]

(Identity Platform)

Googleサインインを簡単に実装。裏側がGoogle Cloudの「Identity Platform」であるため、後からEntra IDやOktaなどのSAML/OIDCプロバイダへの拡張がスムーズに可能です。
5. AI処理	Vertex AI (Gemini API)	マルチモーダル対応のGemini（gemini-1.5-pro など）を利用。音声ファイルをそのままインプット、あるいはCloud Run経由でテキスト化して超長尺の要約を一発で実行。
🔍 オブザーバビリティ	
Google Cloud Observability


(旧 Stackdriver)

APIログ、コンテナのメトリクス、エラー検知、分散トレーシングを一元管理（詳細は後述）。
🚀 CI/CD	Cloud Build + Artifact Registry	GitHubと連携し、コード変更時に自動でコンテナイメージをビルド・格納し、Cloud Runへ自動デプロイ。

CI/CD（継続的インテグレーション／デリバリー）のパイプライン
GitHubにコードをプッシュしてから本番環境に反映されるまでのクリーンな自動化フローです。

[ GitHub Repository ]
        |
        |  1. code push / Pull Request merged
        v
[ Cloud Build ]  <--- (2. トリガー起動 / テスト・ビルド実行)
        |
        |  3. Dockerコンテナイメージをビルド
        v
[ Artifact Registry ] (コンテナ保管庫)
        |
        |  4. 最新イメージを指定してデプロイ
        v
[ Cloud Run ] (API実行環境) & [ Cloud Storage ] (フロント公開)
静的解析・テスト: GitHubへのプッシュを検知して Cloud Build が起動。Linterによるコードチェックや、バックエンドのユニットテストを実行。

コンテナビルド: テスト通過後、APIのDockerイメージを作成し、セキュアなリポジトリである Artifact Registry にプッシュ。

カナリアデプロイ（安全な反映）: Cloud Run に新しいコンテナをデプロイ。最初は新しいコードにトラフィックを「10%」だけ流し、Cloud Logging/Error Reportingでエラーが出ないかを確認しながら自動で100%に切り替える（段階的リリース）といった運用も簡単に組めます。

フロントエンド配信: Reactのビルド成果物（HTML/JS/CSS）をビルドし、Cloud Storage に上書きアップロード。Cloud CDNのキャッシュをクリア。