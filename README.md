<h1 align="center">RightClickManager (右键菜单管理器)</h1>

<p align="center">
  一个轻量、现代且高效的 Windows 右键菜单管理工具，帮助你夺回系统右键菜单的控制权，拒绝第三方软件尝试带来的混乱。
</p>

## ✨ 特点与功能

* **全面管理右键菜单**
  支持浏览并管理系统各类右键菜单项。不仅支持传统基于注册表的程序菜单，还支持现代打包应用 (Packaged App / UWP / WinUI) 写入的新版右键菜单。
* **实时监控与拦截审批**
  提供右键新增项的实时监控功能。当第三方软件在后台试图悄悄添加右键菜单时，程序会自动拦截，并通过审批工作流让用户决定是否放行，从而保持右键菜单干净清爽。
* **现代化界面**
  基于 Avalonia UI 与 FluentAvalonia 构建，提供具有 Fluent Design 设计风格的现代化界面，支持亚克力模糊和自适应系统主题。
* **后台托盘与开机自启**
  支持开启“开机自启”。启动后程序默认以静默模式（隐藏主界面）在系统托盘 (System Tray) 后台运行，随用随唤，不再忍受每次开机弹窗的困扰。
* **一键编译与打包**
  内置脚本可一键发布并生成对应 \x64\、\x86\ 和 \ARM64\ 架构的独立免安装程序。

## 🚀 编译与使用

1. 确保系统已安装 [.NET 8.0 SDK](https://dotnet.microsoft.com/download) 或更高版本。
2. 克隆本仓库代码到本地。
3. 运行项目目录下的 \src/Publish.bat\ 脚本。
4. 到生成的 \src/bin/output/\ 路径下双击执行 \右键菜单管理器.exe\。

## 🤝 贡献与反馈
[!WARNING]
>本项目的极大部分代码及重构过程经过 AI 辅助与人工作业联调。如果你在使用过程中遇到 Bug、界面兼容问题、异常行为，欢迎提交 Issue 或 Pull Request。反馈时最好附带系统版本、复现步骤及对应截图。

## 📄 许可证

本项目整体以 [GPLv3 许可证](./LICENSE) 发布。
*本项目部分源码借鉴、参考自开源项目 [PLFJY/ContextMenuMgr](https://github.com/PLFJY/ContextMenuMgr) 和 [cnbluefire/ModernContextMenuManager](https://github.com/cnbluefire/ModernContextMenuManager)，感谢原作者带来的启发。*
