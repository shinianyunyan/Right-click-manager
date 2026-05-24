<h1 align="center">
  <span>RightClickManager</span>
</h1>
<p align="center">
  <span align="center">RightClickManager是一个轻量级的右键管理程序，它可帮助您管理 Windows 上的右键菜单，并避免第三方程序向你的右键菜单里塞屎。</span>
</p>

>[!WARNING]
>本项目的相当一部分代码由 AI 辅助生成，并经过持续的人工作业、联调和重构，但它仍然可能存在遗漏、边界情况处理不足或行为与预期不完全一致的问题。 如果你在使用过程中发现 Bug、兼容性问题、异常行为或文档缺失，欢迎积极提交 Issue。最好附上复现步骤、日志、截图和系统版本信息，这会非常有帮助。


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
本项目整体以 GNU General Public License v3.0 (GPLv3) 发布，因为仓库包含来自 PLFJY/ContextMenuMgr 的 GPLv3 代码。cnbluefire/ModernContextMenuManager 的部分代码遵循 MIT 许可证；这些部分在 `LICENSE` 中保留 MIT 版权与许可文本。有关详细信息和如何获取对应的源代码，请参阅 `LICENSE`。

来源与致谢
本项目基于并参考了以下两个开源项目的实现：
- PLFJY/ContextMenuMgr: https://github.com/PLFJY/ContextMenuMgr (GPLv3)
- cnbluefire/ModernContextMenuManager: https://github.com/cnbluefire/ModernContextMenuManager (MIT)

来源与许可说明
- 本仓库包含 GPLv3（PLFJY）与 MIT（cnbluefire）代码；因此整体以 GPLv3 发布，并保留 MIT 文件的原始版权与许可声明。