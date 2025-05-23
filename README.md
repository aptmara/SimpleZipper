# SimpleZipper

SimpleZipperは、ファイルやフォルダを簡単にZIP形式に圧縮したり、既存のZIPファイルにファイルを追加したりするためのWindows向けデスクトップアプリケーションです。

## 概要

日々のファイル管理や、メールでのファイル共有、データのバックアップなど、様々な場面でご活用いただけます。直感的な操作性と、豊富なオプション機能で、効率的なファイル圧縮作業をサポートします。

## 主な機能

* **ファイル・フォルダの柔軟な追加**:
    * 複数ファイル・フォルダの選択（ダイアログ経由）
    * ドラッグ＆ドロップによる簡単追加（ファイルリストおよびフォーム全体）
    * フォルダ内のファイルを再帰的に追加するオプション
* **多彩なZIP操作モード**:
    * 新しいZIPファイルを一から作成
    * 既存のZIPファイルを選び、そこに新しいファイルやフォルダを追加
* **豊富な圧縮オプション**:
    * **圧縮レベル**: 「標準」「速度優先」「高圧縮」から選択可能
    * **パスワード保護**: AES-256暗号化によるパスワード付きZIPファイルの作成
    * **コメント追加**: ZIPファイル自体へのコメント埋め込み
    * **ファイル分割**: (新規ZIP作成時のみ) 指定サイズ（MB/KB単位）でのZIPファイル分割
* **ユーザーフレンドリーなインターフェース**:
    * 処理の進捗を表示するプログレスバー
    * 圧縮対象ファイルリストの編集（個別削除、全クリア）
    * ライトテーマ・ダークテーマの切り替え
    * 操作結果やエラーを通知するメッセージ表示（通常通知、アニメーション警告）
    * 処理内容の詳細を確認できるログ表示エリア
    * 圧縮完了後に元ファイルサイズ、圧縮後ファイルサイズ、圧縮率を表示
* **便利なユーティリティ機能**:
    * 圧縮完了後に、ZIPファイルが保存されたフォルダを自動的に開くオプション
    * コマンドライン引数によるファイル指定に対応（「送る」メニュー等からの起動）
    * 前回使用した設定（保存先、オプション、テーマなど）の記憶と自動復元

## 動作環境

* Windows OS
* （自己完結型としてビルド・配布されている場合は、特定の.NETランタイムの事前インストールは不要です。インストーラーが提供されている場合は、インストーラーが必要なコンポーネントを処理します。）

## インストール方法

1.  **インストーラーを使用する場合**:
    * GitHubリリースページから最新のインストーラー（例: `SimpleZipperSetup.msi` または `setup.exe`）をダウンロードします。
    * ダウンロードしたインストーラーを実行し、画面の指示に従ってインストールを完了してください。
2.  **実行ファイル一式 (ZIP等) を使用する場合**:
    * GitHubリリースページから最新のアプリケーションファイル（ZIPアーカイブなど）をダウンロードします。
    * ダウンロードしたファイルを任意のフォルダに展開（解凍）します。
    * フォルダ内の `SimpleZipper.exe` を実行します。

## 基本的な使い方

1.  **操作モードの選択**: アプリケーション上部で「新規ZIP作成」または「既存のZIPに追加」を選びます。
2.  **ファイル/フォルダの追加**:
    * 「ファイルを選択」ボタンで対象を指定します。
    * または、ファイルやフォルダをアプリケーションのリストエリアやウィンドウの空きスペースにドラッグ＆ドロップします。
3.  **出力先の設定**:
    * (新規作成時) 保存先のフォルダとZIPファイル名を指定します。
    * (既存に追加時) 対象の既存ZIPファイルを選択します。
4.  **オプションの設定**: 必要に応じて、圧縮レベル、パスワード、コメント、分割設定などを行います。
5.  **実行**: 「圧縮実行」または「ファイルを追加」ボタンで処理を開始します。

## 「送る」メニューへの登録 (手動)

より便利に使うために、Windowsの「送る」メニューにSimpleZipperを登録できます。

1.  `SimpleZipper.exe` (アプリケーションの実行ファイル) のショートカットを作成します。
2.  エクスプローラーのアドレスバーに `shell:sendto` と入力してEnterキーを押し、「送る」フォルダを開きます。
3.  作成したショートカットを、この「送る」フォルダに移動またはコピーします。
4.  ショートカットの名前を「SimpleZipperで圧縮」のように変更すると分かりやすくなります。

## ライセンス

このプロジェクトは [MIT License](#license) の下で公開されています。詳細は `LICENSE` ファイルをご覧ください。

## 謝辞

* このアプリケーションは [DotNetZip library](http://dotnetzip.codeplex.com/) を利用しています。

---
