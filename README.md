# UE 批量模型导入与蓝图生成工具 - 前端原型 v2.0

## 定位

这是一个 **.NET 8 + WPF 前端原型**，目标是先跑通界面与交互流程。

当前版本只做：

- 多页面导航
- FBX来源选择界面
- 导入选项配置界面
- 蓝图模板管理界面
- 组件挂载配置界面
- 目录与命名规则界面
- 任务预览与冲突检测界面
- 执行任务界面
- 结果报告与日志界面
- 配置管理、设置、关于界面
- 按钮点击响应
- 表格假数据
- 日志追加
- CSV/TXT导出
- JSON配置保存/加载

当前版本不做：

- 真实解析 FBX
- 真实导入 UE 资产
- 真实创建 Blueprint
- 真实挂载 UE 组件
- 真实调用 Unreal Editor API
- 真实保存 .uasset

## 运行环境

- Windows 10/11
- Visual Studio 2022
- .NET 8 SDK
- WPF Desktop workload

## 打开方式

用 Visual Studio 2022 打开：

```text
UEBlueprintBatchFrontend.sln
```

然后直接运行 `UEBlueprintBatchFrontend` 项目。

## 后续接口预留方向

后续真正接 UE 时，可以把以下动作从前端模拟逻辑替换为真实服务：

- ImportFbxAsync：导入 FBX，生成 Mesh / Texture / Material / Skeleton / PhysicsAsset
- CreateBlueprintAsync：按照蓝图模板生成 BP_XXX
- AttachComponentAsync：按照组件挂载规则添加逻辑组件
- SaveAssetAsync：保存 UE 资产并记录结果
- ValidateComponentAsync：检测 C++/蓝图组件类是否存在
- ExportReportAsync：导出真实任务结果报告

## 项目结构

```text
UEBlueprintBatchFrontend/
├── UEBlueprintBatchFrontend.sln
├── README.md
└── UEBlueprintBatchFrontend/
    ├── UEBlueprintBatchFrontend.csproj
    ├── App.xaml
    ├── App.xaml.cs
    ├── MainWindow.xaml
    └── MainWindow.xaml.cs
```

## 说明

为了避免 `System.Windows.Application` 与 `System.Windows.Forms.Application` 冲突，`App.xaml.cs` 显式继承：

```csharp
public partial class App : System.Windows.Application
```

项目启用了：

```xml
<UseWPF>true</UseWPF>
<UseWindowsForms>true</UseWindowsForms>
<ImplicitUsings>disable</ImplicitUsings>
```

`UseWindowsForms` 只用于前端选择文件夹对话框。
