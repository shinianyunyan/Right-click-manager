<h1 align="center">
  <span>RightClickManager</span>
</h1>
<p align="center">
  <span align="center">RightClickManager是一个轻量级的右键管理程序，它可帮助您管理 Windows 上的右键菜单，并避免第三方程序向你的右键菜单里塞屎。</span>
</p>

>[!WARNING]
>本项目的相当一部分代码由 AI 辅助生成，并经过持续的人工作业、联调和重构，但它仍然可能存在遗漏、边界情况处理不足或行为与预期不完全一致的问题。 如果你在使用过程中发现 Bug、兼容性问题、异常行为或文档缺失，欢迎积极提交 Issue。最好附上复现步骤、日志、截图和系统版本信息，这会非常有帮助。

项目简介
- 名称：右键菜单管理器（Right-click manager）
- 仓库：https://github.com/shinianyunyan/Right-click-manager.git

来源与致谢
本项目基于并参考了以下两个开源项目的实现：
- PLFJY/ContextMenuMgr: https://github.com/PLFJY/ContextMenuMgr
- cnbluefire/ModernContextMenuManager: https://github.com/cnbluefire/ModernContextMenuManager

本仓库在第二个项目的基础上进行了二次开发，并借鉴了第一个项目关于实时监控右键新增并进行拦截审批的实现思路。

主要功能
- 浏览并管理系统右键菜单项（注册表/包裹应用）
- 实时监控右键新增项并支持拦截与审批工作流（参考并扩展自 PLFJY/ContextMenuMgr）
- 打包生成可执行程序（通过 `Publish.bat`）

贡献与报告 Bug
欢迎提交 Issue 或 Pull Request。提交 Issue 时请尽量提供：
- 操作系统与版本
- 可复现的步骤
- 日志、截图或异常信息

许可证
本项目采用 MIT 许可证（详见 LICENSE 文件）。

