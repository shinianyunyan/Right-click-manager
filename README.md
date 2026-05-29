<h1 align="center">RightClickManager（右键菜单管理器）</h1>

<p align="center">
  轻量、高效的 Windows 右键菜单管理工具，夺回系统右键菜单的控制权。
</p>
<p align="center">
  <a href="./README_EN.md">English</a>
</p>

## 功能

- **全面管理右键菜单** — 支持传统注册表菜单、Packaged App（UWP/WinUI）新版右键菜单、系统级 Shell 扩展及动词项
- **实时监控与拦截审批** — 第三方软件悄悄添加右键菜单时自动拦截并进入待审核状态，用户决定放行或禁用
- **现代化界面** — 基于 Avalonia UI 11 + FluentAvalonia，Fluent Design 风格，亚克力模糊，自适应明暗主题
- **后台托盘运行** — 支持开机自启，以托盘图标静默运行，随用随唤
- **分类管理** — 应用项和系统项分开展示，已允许/已禁用/待审核三 Tab，支持批量启用/禁用

## 编译与运行

1. 安装 [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
2. 克隆仓库
3. 运行：`dotnet run --project src -c Release -p:Platform=x64`
4. 或打包发布：
   ```
   dotnet publish src -c Release -r win-x64 -p:Platform=x64 --self-contained
   dotnet publish src -c Release -r win-x86 -p:Platform=x86 --self-contained
   ```

## 反馈

本项目代码经过 AI 辅助与人工作业联调。遇到 Bug 或兼容问题，欢迎提交 Issue，请附带系统版本和复现步骤。

## 许可证

[GPLv3](./LICENSE)

*部分源码借鉴自 [PLFJY/ContextMenuMgr](https://github.com/PLFJY/ContextMenuMgr) 和 [cnbluefire/ModernContextMenuManager](https://github.com/cnbluefire/ModernContextMenuManager)，感谢原作者。*
