# DS4 Battery Monitor
<img width="80" height="20" alt="window" src="https://github.com/ZPII3/DS4BatteryMonitor/blob/main/window.png" />
<img width="76" height="63" alt="TrayIcon" src="https://github.com/ZPII3/DS4BatteryMonitor/blob/main/TrayIcon.png" />

A lightweight Windows taskbar application to monitor Sony DualShock 4 battery levels, built with C# WPF and .NET 8.  
.NET 8で構築された、Sony DualShock 4のバッテリー残量をタスクトレイで確認できる軽量なC# WPF Windowsアプリです。

## Features / 機能
- **Real-time Monitoring**: Polling every 10 seconds. (10秒おきのリアルタイム監視)
- **Minimalist Tray Icon**: Dynamic icon showing battery level and charging status. (残量と充電状態を示すトレイアイコン)
- **Multi-language Support**: English and Japanese. (英語・日本語対応)
- **Portable**: Settings are saved in `settings.json` next to the executable. (設定は実行ファイルと同じ場所のJSONに保存されます)

## Requirements / 動作環境
- Windows 10 / 11 (x64)
- **[.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)** or later
- Sony DualShock 4 Controller (connected via Bluetooth)

## Installation / 使い方
1. Download the latest `DS4BatteryMonitor.zip` from the [Releases](https://github.com/ZPII3/DS4BatteryMonitor/releases) page.
2. Extract the ZIP file to any folder.
3. Ensure the following files are in the same folder:
    - `DS4BatteryMonitor.exe`
4. Run `DS4BatteryMonitor.exe`.

<BR>

1. [Releases](https://github.com/ZPII3/DS4BatteryMonitor/releases) ページから最新の ZIP ファイルをダウンロードします。
2. 任意のフォルダに解凍します。
3. 以下のファイルが同じフォルダにあることを確認してください：
    - `DS4BatteryMonitor.exe`
4. `DS4BatteryMonitor.exe` を実行します。

## Disclaimer

The author assumes no responsibility for any damages, losses, or issues (including but not limited to malfunctions of DualShock4 controllers or PC system troubles) arising from the use of this software. This software is provided "as is," and you use it at your own risk.

## Trademarks

- "DualShock", "PlayStation", "PS4" and "Sony Interactive Entertainment" are registered trademarks or trademarks of Sony Interactive Entertainment Inc.
- This software is an unofficial project created by an individual developer and is not affiliated with, authorized, or endorsed by Sony Interactive Entertainment or Sony Group Corporation.

## 免責事項 (Disclaimer)

本ソフトウェアの利用に関連して生じた損害、損失、または不利益（DualShock4コントローラーの不具合、PCのシステムトラブル等を含みますが、これらに限定されません）について、作者は一切の責任を負いません。本ソフトウェアは「現状のまま」提供され、利用者自身の責任において使用するものとします。

## 権利表記 (Copyright Notice)

- 「DualShock」、「PlayStation」、「PS4」および「Sony Interactive Entertainment」は、株式会社ソニー・インタラクティブエンタテインメント（Sony Interactive Entertainment Inc.）の登録商標または商標です。
- 本ソフトウェアは、ソニー・インタラクティブエンタテインメント、またはソニーグループ株式会社とは一切関係のない個人の開発者によって制作された非公式のプロジェクトです。

## License / ライセンス
This project is licensed under the MIT License.
