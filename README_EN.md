<h1 align="center">RightClickManager</h1>

<p align="center">
  A lightweight, modern Windows context menu manager. Take back control of your right-click menu.
</p>
<p align="center">
  <a href="./README.md">中文</a>
</p>

## Features

- **Full context menu management** — Traditional registry menus, Packaged App (UWP/WinUI) menus, system-level Shell extensions and verbs
- **Real-time monitoring & interception** — Automatically intercepts newly added context menu items into a pending review queue; user decides to allow or block
- **Modern UI** — Built with Avalonia UI 11 + FluentAvalonia, Fluent Design, acrylic blur, light/dark theme support
- **System tray** — Minimizes to tray on close; optional auto-start with Windows
- **Organized views** — App items and system items displayed separately across Allowed / Disabled / Pending tabs, with batch enable/disable

## Build & Run

1. Install [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
2. Clone the repository
3. Run: `dotnet run --project src -c Release -p:Platform=x64`
4. Or publish:
   ```
   dotnet publish src -c Release -r win-x64 -p:Platform=x64 --self-contained
   dotnet publish src -c Release -r win-x86 -p:Platform=x86 --self-contained
   ```

## Feedback

This project was developed with AI-assisted coding and manual review. If you encounter bugs or compatibility issues, please submit an Issue with your system version and reproduction steps.

## License

[GPLv3](./LICENSE)

*Portions inspired by [PLFJY/ContextMenuMgr](https://github.com/PLFJY/ContextMenuMgr) and [cnbluefire/ModernContextMenuManager](https://github.com/cnbluefire/ModernContextMenuManager). Thanks to the original authors.*
