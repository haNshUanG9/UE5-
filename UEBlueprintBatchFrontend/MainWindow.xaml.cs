using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace UEBlueprintBatchFrontend;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<FbxFileItem> _fbxFiles = new();
    private readonly ObservableCollection<PreviewItem> _previewItems = new();
    private readonly ObservableCollection<ComponentRuleItem> _componentRules = new();
    private readonly ObservableCollection<BlueprintTemplateItem> _templates = new();
    private readonly ObservableCollection<GenerationTaskItem> _tasks = new();
    private readonly ObservableCollection<ResultItem> _results = new();
    private readonly ObservableCollection<RuleItem> _folderRules = new();
    private readonly ObservableCollection<RuleItem> _namingRules = new();
    private readonly ObservableCollection<LogItem> _logs = new();

    private Grid _mainArea = null!;
    private TextBlock _pageTitle = null!;
    private TextBlock _pageSubTitle = null!;
    private TextBlock _statusText = null!;
    private TextBox _sourceFolderBox = null!;
    private TextBox _singleFileBox = null!;
    private TextBox _outputPathBox = null!;
    private ComboBox _templateCombo = null!;
    private ComboBox _profileCombo = null!;
    private ComboBox _missingPolicyCombo = null!;
    private ProgressBar _executionProgress = null!;
    private TextBlock _executionText = null!;
    private DataGrid _previewGrid = null!;
    private DataGrid _resultGrid = null!;
    private DataGrid _logGrid = null!;

    private readonly Dictionary<string, Func<FrameworkElement>> _pageFactories;

    public MainWindow()
    {
        InitializeComponent();
        SeedData();
        _pageFactories = new Dictionary<string, Func<FrameworkElement>>
        {
            ["首页"] = CreateDashboardPage,
            ["导入与生成"] = CreateImportGeneratePage,
            ["蓝图模板"] = CreateBlueprintTemplatePage,
            ["组件挂载"] = CreateComponentRulePage,
            ["目录与命名"] = CreateDirectoryNamingPage,
            ["任务预览"] = CreateTaskPreviewPage,
            ["执行任务"] = CreateExecutionPage,
            ["结果报告"] = CreateResultReportPage,
            ["配置管理"] = CreateProfilePage,
            ["设置"] = CreateSettingsPage,
            ["关于"] = CreateAboutPage
        };
        BuildShell();
        Navigate("首页");
        AddLog("Info", "前端原型已启动。当前版本只跑通界面流程，UE导入和蓝图生成接口尚未接入。");
    }

    private void BuildShell()
    {
        RootHost.Children.Clear();
        RootHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
        RootHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var nav = new Border
        {
            Background = Brush("#0F172A"),
            BorderBrush = Brush("#1E293B"),
            BorderThickness = new Thickness(0, 0, 1, 0)
        };
        Grid.SetColumn(nav, 0);
        RootHost.Children.Add(nav);

        var navPanel = new DockPanel { LastChildFill = true };
        nav.Child = navPanel;

        var logo = new StackPanel { Margin = new Thickness(18, 22, 18, 18) };
        DockPanel.SetDock(logo, Dock.Top);
        navPanel.Children.Add(logo);
        logo.Children.Add(new TextBlock
        {
            Text = "UE蓝图工具",
            Foreground = Brushes.White,
            FontSize = 22,
            FontWeight = FontWeights.Bold
        });
        logo.Children.Add(new TextBlock
        {
            Text = ".NET 8 / WPF 前端版",
            Foreground = Brush("#93C5FD"),
            Margin = new Thickness(0, 6, 0, 0)
        });

        var foot = new StackPanel { Margin = new Thickness(18), VerticalAlignment = VerticalAlignment.Bottom };
        DockPanel.SetDock(foot, Dock.Bottom);
        navPanel.Children.Add(foot);
        _statusText = new TextBlock { Text = "● 就绪", Foreground = Brush("#22C55E"), FontSize = 13 };
        foot.Children.Add(new TextBlock { Text = "v2.0.0 Frontend", Foreground = Brush("#CBD5E1"), Margin = new Thickness(0, 0, 0, 8) });
        foot.Children.Add(_statusText);

        var navButtons = new StackPanel { Margin = new Thickness(10, 4, 10, 0) };
        navPanel.Children.Add(navButtons);

        foreach (var item in _pageFactories.Keys)
        {
            navButtons.Children.Add(CreateNavButton(item));
        }

        var main = new Grid { Margin = new Thickness(22) };
        main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        main.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(main, 1);
        RootHost.Children.Add(main);

        var header = Card(new Thickness(18), new Thickness(0, 0, 0, 16));
        Grid.SetRow(header, 0);
        main.Children.Add(header);

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Child = headerGrid;

        var titleBox = new StackPanel();
        headerGrid.Children.Add(titleBox);
        _pageTitle = new TextBlock { Text = "首页", FontSize = 26, FontWeight = FontWeights.Bold, Foreground = Brush("#111827") };
        _pageSubTitle = new TextBlock { Text = "批量导入FBX、按模板生成蓝图、挂载逻辑组件。", Foreground = Brush("#6B7280"), Margin = new Thickness(0, 7, 0, 0) };
        titleBox.Children.Add(_pageTitle);
        titleBox.Children.Add(_pageSubTitle);

        var topActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(topActions, 1);
        headerGrid.Children.Add(topActions);
        _profileCombo = Combo(new[] { "固定翼通用方案", "地面车辆方案", "舰船方案", "通用模型方案" }, 180);
        topActions.Children.Add(new TextBlock { Text = "当前方案：", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), Foreground = Brush("#374151") });
        topActions.Children.Add(_profileCombo);
        topActions.Children.Add(Button("保存方案", "Secondary", (_, _) => AddLog("Info", "已模拟保存当前方案。")));
        topActions.Children.Add(Button("加载方案", "Secondary", (_, _) => AddLog("Info", "已模拟加载方案。")));
        topActions.Children.Add(Button("重置", "Secondary", (_, _) => ResetFrontendState()));

        _mainArea = new Grid();
        Grid.SetRow(_mainArea, 1);
        main.Children.Add(_mainArea);
    }

    private Button CreateNavButton(string text)
    {
        return new Button
        {
            Content = text,
            Height = 42,
            Margin = new Thickness(0, 0, 0, 6),
            Padding = new Thickness(12, 0, 12, 0),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Foreground = Brush("#E5E7EB"),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Cursor = System.Windows.Input.Cursors.Hand
        }.Also(button => button.Click += (_, _) => Navigate(text));
    }

    private void Navigate(string pageName)
    {
        _mainArea.Children.Clear();
        if (!_pageFactories.TryGetValue(pageName, out var factory))
        {
            pageName = "首页";
            factory = _pageFactories[pageName];
        }

        _pageTitle.Text = pageName;
        _pageSubTitle.Text = pageName switch
        {
            "首页" => "查看任务统计、最近任务和整体流程。",
            "导入与生成" => "选择FBX、设置导入选项、指定模板并创建生成任务。",
            "蓝图模板" => "维护蓝图父类、组件层级、默认组件顺序和默认参数。",
            "组件挂载" => "配置逻辑组件名称、类路径、挂载点、缺失策略和测试查找。",
            "目录与命名" => "配置资产归档目录、命名前缀、贴图识别规则和冲突策略。",
            "任务预览" => "在执行前预览生成计划、冲突、缺失组件和资产路径。",
            "执行任务" => "模拟任务执行流程，后续接入UE导入和蓝图生成接口。",
            "结果报告" => "查看生成结果、组件挂载结果、失败原因和导出报告。",
            "配置管理" => "维护导入生成方案、模板组合和规则集合。",
            "设置" => "配置UE项目路径、Content目录、编辑器路径、缓存和日志目录。",
            _ => "前端原型页面。"
        };
        _mainArea.Children.Add(factory());
        AddLog("Info", $"已切换页面：{pageName}");
    }

    private FrameworkElement CreateDashboardPage()
    {
        var scroll = Scroll();
        var root = new StackPanel();
        scroll.Content = root;

        var stats = new UniformGrid { Columns = 5, Margin = new Thickness(0, 0, 0, 12) };
        root.Children.Add(stats);
        stats.Children.Add(StatCard("FBX文件", _fbxFiles.Count.ToString(), "已扫描模型源文件", "#2563EB"));
        stats.Children.Add(StatCard("预览资产", _previewItems.Count.ToString(), "Mesh/材质/蓝图等", "#7C3AED"));
        stats.Children.Add(StatCard("蓝图模板", _templates.Count.ToString(), "固定翼/车辆/舰船", "#0891B2"));
        stats.Children.Add(StatCard("组件规则", _componentRules.Count.ToString(), "可挂载逻辑组件", "#D97706"));
        stats.Children.Add(StatCard("生成结果", _results.Count.ToString(), "成功/失败/跳过", "#16A34A"));

        var grid = TwoColumns(0.62, 0.38);
        root.Children.Add(grid);

        var recent = Card();
        Grid.SetColumn(recent, 0);
        grid.Children.Add(recent);
        recent.Child = Stack("最近任务", DataGrid(_tasks, 280));

        var quick = Card();
        Grid.SetColumn(quick, 1);
        grid.Children.Add(quick);
        var quickStack = new StackPanel();
        quick.Child = quickStack;
        quickStack.Children.Add(SectionTitle("快速操作"));
        quickStack.Children.Add(Button("进入导入与生成", "Primary", (_, _) => Navigate("导入与生成")));
        quickStack.Children.Add(Button("进入蓝图模板", "Secondary", (_, _) => Navigate("蓝图模板")));
        quickStack.Children.Add(Button("进入组件挂载", "Secondary", (_, _) => Navigate("组件挂载")));
        quickStack.Children.Add(Button("查看结果报告", "Secondary", (_, _) => Navigate("结果报告")));
        quickStack.Children.Add(new Separator { Margin = new Thickness(0, 12, 0, 12) });
        quickStack.Children.Add(SectionTitle("前端范围"));
        quickStack.Children.Add(InfoText("已完成：页面切换、输入框、表格、按钮响应、假数据流程、日志、导出。"));
        quickStack.Children.Add(InfoText("未接入：UE Editor Import API、真实蓝图创建、真实资产保存。"));

        root.Children.Add(CardWithContent("整体流程", FlowText()));
        return scroll;
    }

    private FrameworkElement CreateImportGeneratePage()
    {
        var scroll = Scroll();
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.42, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.58, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        scroll.Content = root;

        var left = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
        Grid.SetColumn(left, 0);
        root.Children.Add(left);

        var sourceCard = Card();
        left.Children.Add(sourceCard);
        var sourceStack = new StackPanel();
        sourceCard.Child = sourceStack;
        sourceStack.Children.Add(SectionTitle("A. 选择FBX来源"));
        _sourceFolderBox = TextBox("G:\\Models\\Aircraft");
        _singleFileBox = TextBox("G:\\Models\\Aircraft\\F16.fbx");
        sourceStack.Children.Add(Row("FBX文件夹：", _sourceFolderBox, Button("浏览文件夹", "Secondary", (_, _) => BrowseFolder(_sourceFolderBox))));
        sourceStack.Children.Add(Row("单个FBX：", _singleFileBox, Button("浏览文件", "Secondary", (_, _) => BrowseFile(_singleFileBox))));
        sourceStack.Children.Add(CheckRow("递归扫描子目录", true, "仅扫描FBX", false, "忽略隐藏文件", true));
        sourceStack.Children.Add(Button("扫描FBX", "Primary", (_, _) => GenerateMockFbxFiles()));

        var settingCard = Card();
        left.Children.Add(settingCard);
        var settingStack = new StackPanel();
        settingCard.Child = settingStack;
        settingStack.Children.Add(SectionTitle("B. 导入选项"));
        settingStack.Children.Add(CheckRow("导入Mesh", true, "导入贴图", true, "导入材质", true));
        settingStack.Children.Add(CheckRow("导入Skeleton", true, "生成PhysicsAsset", true, "导入动画", false));
        settingStack.Children.Add(CheckRow("自动创建蓝图", true, "自动绑定材质", true, "导入前预览", true));
        settingStack.Children.Add(Row("Mesh类型：", Combo(new[] { "自动判断", "StaticMesh", "SkeletalMesh" }, 160), Combo(new[] { "Import Normals and Tangents", "Import Normals", "Compute Normals" }, 230)));
        settingStack.Children.Add(Row("贴图处理：", Combo(new[] { "按后缀识别", "全部归入Other", "不导入贴图" }, 160), Combo(new[] { "sRGB按类型", "全部sRGB", "全部Linear" }, 230)));

        var targetCard = Card();
        left.Children.Add(targetCard);
        var targetStack = new StackPanel();
        targetCard.Child = targetStack;
        targetStack.Children.Add(SectionTitle("C. 输出与模板"));
        _outputPathBox = TextBox("/Game/ImportedModels/Aircraft");
        _templateCombo = Combo(_templates.Select(t => t.TemplateName).ToArray(), 260);
        _missingPolicyCombo = Combo(new[] { "跳过并记录", "报错停止", "使用替代组件" }, 180);
        targetStack.Children.Add(Row("输出目录：", _outputPathBox, Button("说明", "Secondary", (_, _) => Alert("UE虚拟路径示例：/Game/ImportedModels/Aircraft。前端版不连接Content Browser。"))));
        targetStack.Children.Add(Row("蓝图模板：", _templateCombo));
        targetStack.Children.Add(Row("蓝图前缀：", TextBox("BP_"), _missingPolicyCombo));
        targetStack.Children.Add(Button("生成预览任务", "Primary", (_, _) => GeneratePreviewPlan()));

        var right = new Grid();
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.72, GridUnitType.Star) });
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.28, GridUnitType.Star) });
        Grid.SetColumn(right, 1);
        root.Children.Add(right);

        var previewCard = Card(new Thickness(14), new Thickness(0, 0, 0, 12));
        Grid.SetRow(previewCard, 0);
        right.Children.Add(previewCard);
        _previewGrid = DataGrid(_previewItems, 0);
        previewCard.Child = Stack("蓝图生成预览", _previewGrid);

        var logCard = Card();
        Grid.SetRow(logCard, 1);
        right.Children.Add(logCard);
        _logGrid = DataGrid(_logs, 0);
        logCard.Child = Stack("运行日志", _logGrid);
        return scroll;
    }

    private FrameworkElement CreateBlueprintTemplatePage()
    {
        var root = TwoColumns(0.28, 0.72);
        var left = Card(new Thickness(14), new Thickness(0, 0, 12, 0));
        Grid.SetColumn(left, 0);
        root.Children.Add(left);
        left.Child = Stack("模板列表", DataGrid(_templates, 520), Button("新增模板", "Primary", (_, _) => AddTemplate()), Button("复制模板", "Secondary", (_, _) => AddTemplate("_Copy")), Button("删除选中", "Secondary", (_, _) => Alert("前端版：删除动作已预留。")));

        var right = Card();
        Grid.SetColumn(right, 1);
        root.Children.Add(right);
        var tabs = new TabControl();
        right.Child = tabs;

        tabs.Items.Add(Tab("组件层级", CreateTemplateTreePanel()));
        tabs.Items.Add(Tab("基础信息", CreateFormPanel(new[]
        {
            ("模板名称", "BP_Aircraft_Template"),
            ("蓝图父类", "/Script/Engine.Actor"),
            ("适用类型", "固定翼/无人机/导弹"),
            ("默认Mesh组件", "AircraftMesh"),
            ("默认Root组件", "SceneRoot")
        })));
        tabs.Items.Add(Tab("默认值", DataGrid(new ObservableCollection<RuleItem>
        {
            new("bAutoBindMaterial", "true", "自动绑定材质"),
            new("MeshComponentName", "AircraftMesh", "Mesh组件名称"),
            new("ComponentMissingPolicy", "SkipAndLog", "缺失组件策略")
        }, 0)));
        return root;
    }

    private FrameworkElement CreateTemplateTreePanel()
    {
        var grid = TwoColumns(0.55, 0.45);
        var tree = new TreeView { Margin = new Thickness(0, 0, 12, 0) };
        Grid.SetColumn(tree, 0);
        grid.Children.Add(tree);
        var root = new TreeViewItem { Header = "BP_Aircraft_Template" };
        root.Items.Add(new TreeViewItem { Header = "Root (SceneComponent)" });
        var mesh = new TreeViewItem { Header = "AircraftMesh (SkeletalMeshComponent)" };
        mesh.Items.Add(new TreeViewItem { Header = "LeftWingSocket (SceneComponent)" });
        mesh.Items.Add(new TreeViewItem { Header = "RightWingSocket (SceneComponent)" });
        mesh.Items.Add(new TreeViewItem { Header = "EngineSocket (SceneComponent)" });
        root.Items.Add(mesh);
        var logic = new TreeViewItem { Header = "Logic (SceneComponent)" };
        logic.Items.Add(new TreeViewItem { Header = "EntityInfoComponent" });
        logic.Items.Add(new TreeViewItem { Header = "DISRuntimeComponent" });
        logic.Items.Add(new TreeViewItem { Header = "FlightControlComponent" });
        logic.Items.Add(new TreeViewItem { Header = "TrailComponent" });
        root.Items.Add(logic);
        tree.Items.Add(root);
        root.IsExpanded = true;
        mesh.IsExpanded = true;
        logic.IsExpanded = true;

        var detail = Stack("选中节点属性", CreateFormPanel(new[]
        {
            ("显示名称", "FlightControlComponent"),
            ("组件类型", "C++组件"),
            ("组件路径", "/Script/AirSystem.FlightControlComponent"),
            ("挂载父级", "Logic"),
            ("是否必需", "否"),
            ("缺失策略", "跳过并记录")
        }), Button("保存节点属性", "Primary", (_, _) => AddLog("Success", "已模拟保存模板节点属性。")));
        Grid.SetColumn(detail, 1);
        grid.Children.Add(detail);
        return grid;
    }

    private FrameworkElement CreateComponentRulePage()
    {
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.32, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.43, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.25, GridUnitType.Star) });

        var listCard = Card(new Thickness(14), new Thickness(0, 0, 12, 0));
        Grid.SetColumn(listCard, 0);
        root.Children.Add(listCard);
        listCard.Child = Stack("组件规则列表", DataGrid(_componentRules, 560), Button("添加组件规则", "Primary", (_, _) => AddComponentRule()), Button("复制规则", "Secondary", (_, _) => AddComponentRule("Copy")));

        var detailCard = Card(new Thickness(14), new Thickness(0, 0, 12, 0));
        Grid.SetColumn(detailCard, 1);
        root.Children.Add(detailCard);
        var detailStack = new StackPanel();
        detailCard.Child = detailStack;
        detailStack.Children.Add(SectionTitle("组件挂载配置"));
        detailStack.Children.Add(Row("显示名称：", TextBox("FlightControlComponent")));
        detailStack.Children.Add(Row("组件类型：", Combo(new[] { "C++组件", "蓝图组件", "ActorComponent", "SceneComponent" }, 180)));
        detailStack.Children.Add(Row("组件类路径：", TextBox("/Script/AirSystem.FlightControlComponent"), Button("选择", "Secondary", (_, _) => Alert("后续接入UE类选择器。"))));
        detailStack.Children.Add(Row("挂载父级：", Combo(new[] { "Root", "Mesh", "Logic", "EffectSockets" }, 180)));
        detailStack.Children.Add(Row("组件名称：", TextBox("FlightControl")));
        detailStack.Children.Add(Row("顺序：", Combo(new[] { "1", "2", "3", "4", "5" }, 100), Combo(new[] { "必需", "可选" }, 120), Combo(new[] { "跳过并记录", "报错停止", "替代组件" }, 160)));
        detailStack.Children.Add(Button("保存规则", "Primary", (_, _) => AddLog("Success", "已模拟保存组件挂载规则。")));

        var testCard = Card();
        Grid.SetColumn(testCard, 2);
        root.Children.Add(testCard);
        testCard.Child = Stack("查找测试结果",
            InfoText("状态：已找到"),
            InfoText("类型：FlightControlComponent"),
            InfoText("模块：AirSystem"),
            InfoText("路径：/Script/AirSystem"),
            InfoText("缺失策略：跳过并记录"),
            Button("测试查找", "Primary", (_, _) => AddLog("Success", "模拟查找完成：FlightControlComponent 已找到。")),
            Button("测试缺失", "Secondary", (_, _) => AddLog("Warning", "模拟查找完成：TrailComponent 未找到，已按策略跳过。")));
        return root;
    }

    private FrameworkElement CreateDirectoryNamingPage()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.5, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.5, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.56, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.44, GridUnitType.Star) });

        var folderCard = Card(new Thickness(14), new Thickness(0, 0, 12, 12));
        Grid.SetRow(folderCard, 0);
        Grid.SetColumn(folderCard, 0);
        root.Children.Add(folderCard);
        folderCard.Child = Stack("目录规则", DataGrid(_folderRules, 0), Button("添加目录规则", "Primary", (_, _) => _folderRules.Add(new RuleItem("Blueprint", "/Game/Imported/Blueprints", "自动添加"))));

        var namingCard = Card(new Thickness(14), new Thickness(0, 0, 0, 12));
        Grid.SetRow(namingCard, 0);
        Grid.SetColumn(namingCard, 1);
        root.Children.Add(namingCard);
        namingCard.Child = Stack("命名规则", DataGrid(_namingRules, 0), Button("添加命名规则", "Primary", (_, _) => _namingRules.Add(new RuleItem("NewAsset", "NA_", "自动添加"))));

        var textureCard = Card(new Thickness(14), new Thickness(0, 0, 12, 0));
        Grid.SetRow(textureCard, 1);
        Grid.SetColumn(textureCard, 0);
        root.Children.Add(textureCard);
        textureCard.Child = Stack("贴图识别规则", DataGrid(new ObservableCollection<RuleItem>
        {
            new("BaseColor", "_D / _BaseColor / _Albedo", "归入Textures/BaseColor"),
            new("Normal", "_N / _Normal", "归入Textures/Normal"),
            new("ORM", "_ORM / _OcclusionRoughnessMetallic", "归入Textures/ORM"),
            new("Emissive", "_E / _Emissive", "归入Textures/Emissive")
        }, 0));

        var previewCard = Card();
        Grid.SetRow(previewCard, 1);
        Grid.SetColumn(previewCard, 1);
        root.Children.Add(previewCard);
        previewCard.Child = Stack("命名预览",
            InfoText("输入：F16_A.fbx"),
            InfoText("Mesh：SK_F16_A"),
            InfoText("Blueprint：BP_F16_A"),
            InfoText("Material：MI_F16_A"),
            InfoText("Texture：T_F16_A_D / T_F16_A_N"),
            Button("刷新预览", "Primary", (_, _) => AddLog("Info", "已根据当前规则刷新命名预览。")));
        return root;
    }

    private FrameworkElement CreateTaskPreviewPage()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var summary = new UniformGrid { Columns = 5, Margin = new Thickness(0, 0, 0, 12) };
        Grid.SetRow(summary, 0);
        root.Children.Add(summary);
        summary.Children.Add(StatCard("计划FBX", _fbxFiles.Count.ToString(), "模型文件", "#2563EB"));
        summary.Children.Add(StatCard("生成Mesh", _previewItems.Count(x => x.AssetType.Contains("Mesh")).ToString(), "Static/Skeletal", "#0891B2"));
        summary.Children.Add(StatCard("生成蓝图", _previewItems.Count(x => x.AssetType == "Blueprint").ToString(), "BP_资源", "#7C3AED"));
        summary.Children.Add(StatCard("冲突", _previewItems.Count(x => x.Conflict != "无").ToString(), "命名/路径冲突", "#DC2626"));
        summary.Children.Add(StatCard("缺失组件", "2", "模拟检测", "#D97706"));

        var tabs = new TabControl();
        Grid.SetRow(tabs, 1);
        root.Children.Add(tabs);
        tabs.Items.Add(Tab("生成计划", DataGrid(_previewItems, 0)));
        tabs.Items.Add(Tab("冲突检测", DataGrid(new ObservableCollection<ConflictItem>
        {
            new("F16_A.fbx", "材质命名冲突", "MI_F16_A已存在", "自动重命名"),
            new("Tank_01.fbx", "蓝图路径冲突", "BP_Tank_01已存在", "跳过并记录")
        }, 0)));
        tabs.Items.Add(Tab("组件缺失", DataGrid(new ObservableCollection<MissingComponentItem>
        {
            new("J20.fbx", "TrailComponent", "可选", "跳过并记录"),
            new("Ship_01.fbx", "WakeComponent", "可选", "跳过并记录")
        }, 0)));
        tabs.Items.Add(Tab("操作", Stack("执行控制", Button("确认并执行", "Primary", (_, _) => { Navigate("执行任务"); StartMockExecution(); }), Button("返回导入页", "Secondary", (_, _) => Navigate("导入与生成")), Button("导出预览CSV", "Secondary", (_, _) => ExportPreviewCsv()))));
        return root;
    }

    private FrameworkElement CreateExecutionPage()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(250) });

        var top = Card(new Thickness(16), new Thickness(0, 0, 0, 12));
        Grid.SetRow(top, 0);
        root.Children.Add(top);
        var topStack = new StackPanel();
        top.Child = topStack;
        topStack.Children.Add(SectionTitle("任务执行状态"));
        _executionProgress = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0, Height = 18, Margin = new Thickness(0, 6, 0, 10) };
        _executionText = new TextBlock { Text = "等待执行", Foreground = Brush("#374151") };
        topStack.Children.Add(_executionProgress);
        topStack.Children.Add(_executionText);
        topStack.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 12, 0, 0),
            Children =
            {
                Button("开始模拟执行", "Primary", (_, _) => StartMockExecution()),
                Button("暂停", "Secondary", (_, _) => AddLog("Info", "已模拟暂停任务。")),
                Button("停止", "Secondary", (_, _) => { _executionProgress.Value = 0; _executionText.Text = "已停止"; AddLog("Warning", "已模拟停止任务。"); }),
                Button("打开结果页", "Secondary", (_, _) => Navigate("结果报告"))
            }
        });

        var taskCard = Card(new Thickness(14), new Thickness(0, 0, 0, 12));
        Grid.SetRow(taskCard, 1);
        root.Children.Add(taskCard);
        taskCard.Child = Stack("任务项", DataGrid(_tasks, 0));

        var logCard = Card();
        Grid.SetRow(logCard, 2);
        root.Children.Add(logCard);
        logCard.Child = Stack("执行日志", DataGrid(_logs, 0));
        return root;
    }

    private FrameworkElement CreateResultReportPage()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var summary = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 12) };
        Grid.SetRow(summary, 0);
        root.Children.Add(summary);
        summary.Children.Add(StatCard("成功", _results.Count(x => x.Status == "成功").ToString(), "蓝图可用", "#16A34A"));
        summary.Children.Add(StatCard("部分成功", _results.Count(x => x.Status == "部分成功").ToString(), "组件缺失", "#D97706"));
        summary.Children.Add(StatCard("失败", _results.Count(x => x.Status == "失败").ToString(), "需处理", "#DC2626"));
        summary.Children.Add(StatCard("跳过", _results.Count(x => x.Status == "跳过").ToString(), "策略跳过", "#64748B"));

        var tabs = new TabControl();
        Grid.SetRow(tabs, 1);
        root.Children.Add(tabs);
        _resultGrid = DataGrid(_results, 0);
        tabs.Items.Add(Tab("任务汇总", _resultGrid));
        tabs.Items.Add(Tab("组件挂载结果", DataGrid(new ObservableCollection<ComponentAttachResult>
        {
            new("BP_F16_A", "EntityInfoComponent", "成功", "已添加"),
            new("BP_F16_A", "FlightControlComponent", "成功", "已添加"),
            new("BP_J20", "TrailComponent", "跳过", "组件类未找到"),
            new("BP_Tank_01", "WeaponComponent", "成功", "已添加")
        }, 0)));
        tabs.Items.Add(Tab("日志", DataGrid(_logs, 0)));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        Grid.SetRow(actions, 2);
        root.Children.Add(actions);
        actions.Children.Add(Button("导出报告CSV", "Primary", (_, _) => ExportResultsCsv()));
        actions.Children.Add(Button("导出日志TXT", "Secondary", (_, _) => ExportLogTxt()));
        actions.Children.Add(Button("打开输出目录", "Secondary", (_, _) => Alert("前端版：后续接UE Content Browser或本地输出目录。")));
        return root;
    }

    private FrameworkElement CreateProfilePage()
    {
        var root = TwoColumns(0.48, 0.52);
        var profiles = new ObservableCollection<ProfileItem>
        {
            new("FixedWing_v2", "固定翼", "BP_Aircraft_Template", "跳过并记录", "2026-06-18"),
            new("GroundVehicle_v1", "地面车辆", "BP_GroundVehicle_Template", "跳过并记录", "2026-06-18"),
            new("Ship_v1", "舰船", "BP_Ship_Template", "跳过并记录", "2026-06-18"),
            new("Generic_v1", "通用", "BP_Generic_Template", "报错停止", "2026-06-18")
        };
        var list = Card(new Thickness(14), new Thickness(0, 0, 12, 0));
        Grid.SetColumn(list, 0);
        root.Children.Add(list);
        list.Child = Stack("方案列表", DataGrid(profiles, 0), Button("新建方案", "Primary", (_, _) => profiles.Add(new ProfileItem("New_Profile", "自定义", "BP_Generic_Template", "跳过并记录", DateTime.Now.ToString("yyyy-MM-dd")))), Button("保存到JSON", "Secondary", (_, _) => SaveFrontendConfig()), Button("从JSON加载", "Secondary", (_, _) => LoadFrontendConfig()));

        var detail = Card();
        Grid.SetColumn(detail, 1);
        root.Children.Add(detail);
        detail.Child = Stack("方案详情",
            CreateFormPanel(new[]
            {
                ("方案名称", "FixedWing_v2"),
                ("模型分类", "固定翼"),
                ("蓝图模板", "BP_Aircraft_Template"),
                ("目录规则", "Aircraft_FolderRule_v2"),
                ("命名规则", "Aircraft_NamingRule_v1"),
                ("组件规则", "Aircraft_ComponentRule_v2"),
                ("冲突策略", "自动重命名"),
                ("缺失策略", "跳过并记录")
            }),
            Button("应用到当前任务", "Primary", (_, _) => AddLog("Success", "已模拟应用方案到当前任务。")));
        return root;
    }

    private FrameworkElement CreateSettingsPage()
    {
        var root = new StackPanel();
        var tabs = new TabControl();
        root.Children.Add(tabs);
        tabs.Items.Add(Tab("项目设置", CreateFormPanel(new[]
        {
            ("UE项目路径", "D:\\UE_Projects\\AircraftProject"),
            ("Content根目录", "/Game"),
            ("UE版本", "5.4 / 5.5"),
            ("UE编辑器路径", "D:\\UE_5.4\\Engine\\Binaries\\Win64\\UnrealEditor.exe"),
            ("插件目录", "D:\\UE_Projects\\AircraftProject\\Plugins")
        })));
        tabs.Items.Add(Tab("导入设置", CreateFormPanel(new[]
        {
            ("默认源目录", "G:\\Models"),
            ("默认输出目录", "/Game/ImportedModels"),
            ("最大并发任务", "1"),
            ("失败重试次数", "0"),
            ("默认缺失策略", "跳过并记录")
        })));
        tabs.Items.Add(Tab("日志与缓存", CreateFormPanel(new[]
        {
            ("缓存目录", "D:\\UE_Tool\\Cache"),
            ("日志目录", "D:\\UE_Tool\\Logs"),
            ("自动保存日志", "true"),
            ("保留最近任务", "50"),
            ("界面主题", "浅色专业版")
        })));
        root.Children.Add(Button("保存设置", "Primary", (_, _) => AddLog("Success", "已模拟保存设置。")));
        return root;
    }

    private FrameworkElement CreateAboutPage()
    {
        var root = TwoColumns(0.5, 0.5);
        var left = Card(new Thickness(24), new Thickness(0, 0, 12, 0));
        Grid.SetColumn(left, 0);
        root.Children.Add(left);
        left.Child = Stack("UE 批量模型导入与蓝图生成工具",
            InfoText("版本：v2.0.0 Frontend"),
            InfoText("运行环境：.NET 8 + WPF"),
            InfoText("定位：先跑通前端，后续接入UE真实导入与蓝图生成接口。"),
            InfoText("当前状态：界面可切换、按钮可响应、表格可显示、假数据流程可跑。"));

        var right = Card(new Thickness(24), new Thickness(0));
        Grid.SetColumn(right, 1);
        root.Children.Add(right);
        right.Child = Stack("接口预留说明",
            InfoText("1. ImportFbxAsync：导入FBX并生成Mesh/Texture/Material。"),
            InfoText("2. CreateBlueprintAsync：按模板生成BP_蓝图。"),
            InfoText("3. AttachComponentAsync：按组件规则挂载逻辑组件。"),
            InfoText("4. SaveAssetAsync：保存UE资产并记录结果。"),
            Button("检查更新", "Secondary", (_, _) => AddLog("Info", "前端版：检查更新功能已预留。")));
        return root;
    }

    private void SeedData()
    {
        _templates.Add(new BlueprintTemplateItem("BP_Aircraft_Template", "固定翼", "/Script/Engine.Actor", 8, "飞行器模板"));
        _templates.Add(new BlueprintTemplateItem("BP_GroundVehicle_Template", "地面车辆", "/Script/Engine.Actor", 6, "车辆模板"));
        _templates.Add(new BlueprintTemplateItem("BP_Ship_Template", "舰船", "/Script/Engine.Actor", 5, "舰船模板"));
        _templates.Add(new BlueprintTemplateItem("BP_Generic_Template", "通用", "/Script/Engine.Actor", 4, "通用模板"));

        _componentRules.Add(new ComponentRuleItem("EntityInfoComponent", "C++组件", "/Script/DIS.EntityInfoComponent", "Root", "EntityInfo", true, "报错停止"));
        _componentRules.Add(new ComponentRuleItem("DISRuntimeComponent", "C++组件", "/Script/DIS.DISRuntimeComponent", "Root", "DISRuntime", true, "报错停止"));
        _componentRules.Add(new ComponentRuleItem("FlightControlComponent", "C++组件", "/Script/AirSystem.FlightControlComponent", "Logic", "FlightControl", false, "跳过并记录"));
        _componentRules.Add(new ComponentRuleItem("TrailComponent", "蓝图组件", "/Game/Components/BP_TrailComponent", "Mesh", "Trail", false, "跳过并记录"));
        _componentRules.Add(new ComponentRuleItem("WeaponComponent", "C++组件", "/Script/Weapon.WeaponComponent", "Logic", "Weapon", false, "跳过并记录"));

        _folderRules.Add(new RuleItem("StaticMesh", "/Game/Imported/{Category}/{Model}/Meshes", "SM_"));
        _folderRules.Add(new RuleItem("SkeletalMesh", "/Game/Imported/{Category}/{Model}/Meshes", "SK_"));
        _folderRules.Add(new RuleItem("Texture", "/Game/Imported/{Category}/{Model}/Textures", "T_"));
        _folderRules.Add(new RuleItem("Material", "/Game/Imported/{Category}/{Model}/Materials", "MI_"));
        _folderRules.Add(new RuleItem("Blueprint", "/Game/Imported/{Category}/{Model}/Blueprints", "BP_"));
        _folderRules.Add(new RuleItem("Skeleton", "/Game/Imported/{Category}/{Model}/Skeletons", "SKEL_"));
        _folderRules.Add(new RuleItem("PhysicsAsset", "/Game/Imported/{Category}/{Model}/Physics", "PHYS_"));

        _namingRules.Add(new RuleItem("StaticMesh", "SM_{ModelName}", "静态网格"));
        _namingRules.Add(new RuleItem("SkeletalMesh", "SK_{ModelName}", "骨骼网格"));
        _namingRules.Add(new RuleItem("Blueprint", "BP_{ModelName}", "蓝图资产"));
        _namingRules.Add(new RuleItem("MaterialInstance", "MI_{ModelName}", "材质实例"));
        _namingRules.Add(new RuleItem("Texture", "T_{ModelName}_{TextureType}", "贴图资产"));

        GenerateMockFbxFiles(false);
        GeneratePreviewPlan(false);
        AddMockTasks();
    }

    private void GenerateMockFbxFiles(bool showLog = true)
    {
        _fbxFiles.Clear();
        _fbxFiles.Add(new FbxFileItem(true, "F16_A.fbx", "G:\\Models\\Aircraft\\F16_A.fbx", "12.4 MB", "固定翼", "2026-06-18 10:42"));
        _fbxFiles.Add(new FbxFileItem(true, "F16_B.fbx", "G:\\Models\\Aircraft\\F16_B.fbx", "11.8 MB", "固定翼", "2026-06-18 10:41"));
        _fbxFiles.Add(new FbxFileItem(true, "J20.fbx", "G:\\Models\\Aircraft\\J20.fbx", "16.2 MB", "固定翼", "2026-06-18 10:40"));
        _fbxFiles.Add(new FbxFileItem(true, "Tank_01.fbx", "G:\\Models\\Ground\\Tank_01.fbx", "8.6 MB", "地面车辆", "2026-06-18 10:39"));
        _fbxFiles.Add(new FbxFileItem(true, "Ship_01.fbx", "G:\\Models\\Ship\\Ship_01.fbx", "15.2 MB", "舰船", "2026-06-18 10:38"));
        _fbxFiles.Add(new FbxFileItem(true, "Helicopter_01.fbx", "G:\\Models\\Air\\Helicopter_01.fbx", "9.1 MB", "直升机", "2026-06-18 10:37"));

        if (showLog) AddLog("Success", $"已扫描到 {_fbxFiles.Count} 个模拟FBX文件。真实FBX解析后续接入。");
    }

    private void GeneratePreviewPlan(bool showLog = true)
    {
        _previewItems.Clear();
        foreach (var fbx in _fbxFiles.Where(x => x.Selected))
        {
            var model = Path.GetFileNameWithoutExtension(fbx.FileName);
            var meshType = fbx.Category == "地面车辆" || fbx.Category == "舰船" ? "StaticMesh" : "SkeletalMesh";
            var meshPrefix = meshType == "StaticMesh" ? "SM_" : "SK_";
            _previewItems.Add(new PreviewItem("待生成", fbx.FileName, meshType, meshPrefix + model, $"Meshes/{model}", "无"));
            _previewItems.Add(new PreviewItem("待生成", fbx.FileName, "MaterialInstance", "MI_" + model, $"Materials/{model}", "无"));
            _previewItems.Add(new PreviewItem("待生成", fbx.FileName, "Texture", "T_" + model + "_D", $"Textures/{model}", model.Contains("F16_A") ? "命名冲突" : "无"));
            _previewItems.Add(new PreviewItem("待生成", fbx.FileName, "Blueprint", "BP_" + model, $"Blueprints/{model}", model.Contains("Tank") ? "路径冲突" : "无"));
        }
        if (showLog) AddLog("Success", $"已生成 {_previewItems.Count} 条模拟预览资产。可进入任务预览页查看。 ");
    }

    private void AddMockTasks()
    {
        _tasks.Clear();
        _tasks.Add(new GenerationTaskItem("Task_20260618_001", "FixedWing_v2", 3, "已预览", "2026-06-18 10:30", "等待执行"));
        _tasks.Add(new GenerationTaskItem("Task_20260618_002", "GroundVehicle_v1", 1, "已预览", "2026-06-18 10:35", "等待执行"));
    }

    private void StartMockExecution()
    {
        if (_executionProgress != null)
        {
            _executionProgress.Value = 68;
            _executionText.Text = "模拟执行中：导入贴图 T_F16_A_D.png，进度 68%";
        }
        foreach (var task in _tasks)
        {
            task.Status = "模拟完成";
            task.Description = "已生成模拟结果";
        }
        _results.Clear();
        _results.Add(new ResultItem("F16_A.fbx", "成功", "BP_F16_A", 8, "6/6", "无"));
        _results.Add(new ResultItem("F16_B.fbx", "成功", "BP_F16_B", 8, "6/6", "无"));
        _results.Add(new ResultItem("J20.fbx", "部分成功", "BP_J20", 7, "5/6", "TrailComponent 未找到，已跳过"));
        _results.Add(new ResultItem("Tank_01.fbx", "部分成功", "BP_Tank_01", 7, "4/5", "路径冲突，已自动重命名"));
        _results.Add(new ResultItem("Ship_01.fbx", "失败", "-", 0, "0/5", "蓝图父类未找到，前端模拟失败"));
        AddLog("Warning", "已完成模拟执行：这不是UE真实导入，只是前端流程验证。 ");
    }

    private void AddTemplate(string suffix = "")
    {
        _templates.Add(new BlueprintTemplateItem("BP_New_Template" + suffix, "自定义", "/Script/Engine.Actor", 3, "界面新增模拟模板"));
        AddLog("Success", "已添加模拟蓝图模板。 ");
    }

    private void AddComponentRule(string suffix = "")
    {
        _componentRules.Add(new ComponentRuleItem("NewComponent" + suffix, "C++组件", "/Script/Module.NewComponent", "Root", "NewComponent", false, "跳过并记录"));
        AddLog("Success", "已添加模拟组件挂载规则。 ");
    }

    private void ResetFrontendState()
    {
        GenerateMockFbxFiles(false);
        GeneratePreviewPlan(false);
        AddMockTasks();
        _results.Clear();
        AddLog("Info", "已重置前端模拟数据。 ");
    }

    private void BrowseFolder(TextBox target)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "选择FBX来源文件夹",
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            target.Text = dialog.SelectedPath;
            AddLog("Info", $"已选择文件夹：{dialog.SelectedPath}");
        }
    }

    private void BrowseFile(TextBox target)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择FBX文件",
            Filter = "FBX文件 (*.fbx)|*.fbx|所有文件 (*.*)|*.*",
            Multiselect = false
        };
        if (dialog.ShowDialog(this) == true)
        {
            target.Text = dialog.FileName;
            AddLog("Info", $"已选择FBX文件：{dialog.FileName}");
        }
    }

    private void SaveFrontendConfig()
    {
        var dialog = new SaveFileDialog
        {
            Title = "保存前端配置",
            FileName = "UEBlueprintBatchFrontend_Profile.json",
            Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;
        var config = new FrontendConfig(_sourceFolderBox?.Text ?? "", _outputPathBox?.Text ?? "", _profileCombo?.Text ?? "", _templateCombo?.Text ?? "", _missingPolicyCombo?.Text ?? "");
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        AddLog("Success", $"已保存前端配置：{dialog.FileName}");
    }

    private void LoadFrontendConfig()
    {
        var dialog = new OpenFileDialog
        {
            Title = "加载前端配置",
            Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;
        var json = File.ReadAllText(dialog.FileName, Encoding.UTF8);
        var config = JsonSerializer.Deserialize<FrontendConfig>(json);
        if (config == null) return;
        if (_sourceFolderBox != null) _sourceFolderBox.Text = config.SourceFolder;
        if (_outputPathBox != null) _outputPathBox.Text = config.OutputPath;
        AddLog("Success", $"已加载前端配置：{dialog.FileName}");
    }

    private void ExportPreviewCsv()
    {
        var dialog = new SaveFileDialog { Title = "导出预览CSV", FileName = "PreviewPlan.csv", Filter = "CSV文件 (*.csv)|*.csv" };
        if (dialog.ShowDialog(this) != true) return;
        var lines = new List<string> { "Status,SourceFile,AssetType,AssetName,TargetPath,Conflict" };
        lines.AddRange(_previewItems.Select(x => $"{x.Status},{x.SourceFile},{x.AssetType},{x.AssetName},{x.TargetPath},{x.Conflict}"));
        File.WriteAllLines(dialog.FileName, lines, Encoding.UTF8);
        AddLog("Success", $"已导出预览CSV：{dialog.FileName}");
    }

    private void ExportResultsCsv()
    {
        var dialog = new SaveFileDialog { Title = "导出结果CSV", FileName = "GenerateResult.csv", Filter = "CSV文件 (*.csv)|*.csv" };
        if (dialog.ShowDialog(this) != true) return;
        var lines = new List<string> { "SourceFile,Status,Blueprint,AssetCount,ComponentResult,Message" };
        lines.AddRange(_results.Select(x => $"{x.SourceFile},{x.Status},{x.BlueprintName},{x.AssetCount},{x.ComponentResult},{x.Message}"));
        File.WriteAllLines(dialog.FileName, lines, Encoding.UTF8);
        AddLog("Success", $"已导出结果CSV：{dialog.FileName}");
    }

    private void ExportLogTxt()
    {
        var dialog = new SaveFileDialog { Title = "导出日志TXT", FileName = "RunLog.txt", Filter = "文本文件 (*.txt)|*.txt" };
        if (dialog.ShowDialog(this) != true) return;
        File.WriteAllLines(dialog.FileName, _logs.Select(x => $"[{x.Time}] [{x.Level}] {x.Message}"), Encoding.UTF8);
        AddLog("Success", $"已导出日志TXT：{dialog.FileName}");
    }

    private void AddLog(string level, string message)
    {
        _logs.Insert(0, new LogItem(DateTime.Now.ToString("HH:mm:ss"), level, message));
        if (_logs.Count > 200) _logs.RemoveAt(_logs.Count - 1);
        if (_statusText != null) _statusText.Text = level == "Error" ? "● 异常" : "● 就绪";
    }

    private void Alert(string message)
    {
        System.Windows.MessageBox.Show(this, message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        AddLog("Info", message);
    }

    private static Brush Brush(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;

    private static Border Card(Thickness? padding = null, Thickness? margin = null)
    {
        return new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(10),
            BorderBrush = Brush("#DDE5F0"),
            BorderThickness = new Thickness(1),
            Padding = padding ?? new Thickness(16),
            Margin = margin ?? new Thickness(0)
        };
    }

    private static Border CardWithContent(string title, UIElement content)
    {
        var card = Card(new Thickness(16), new Thickness(0, 12, 0, 0));
        card.Child = Stack(title, content);
        return card;
    }

    private static TextBlock SectionTitle(string text) => new()
    {
        Text = text,
        FontSize = 18,
        FontWeight = FontWeights.Bold,
        Foreground = Brush("#1D4ED8"),
        Margin = new Thickness(0, 0, 0, 12)
    };

    private static TextBlock InfoText(string text) => new()
    {
        Text = text,
        Foreground = Brush("#374151"),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 4, 0, 4),
        LineHeight = 22
    };

    private static TextBox TextBox(string text = "") => new()
    {
        Text = text,
        Height = 34,
        VerticalContentAlignment = VerticalAlignment.Center,
        Padding = new Thickness(9, 0, 9, 0),
        BorderBrush = Brush("#CBD5E1"),
        BorderThickness = new Thickness(1),
        Margin = new Thickness(0, 0, 8, 0)
    };

    private static ComboBox Combo(IEnumerable<string> items, double width)
    {
        var combo = new ComboBox
        {
            Width = width,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        foreach (var item in items) combo.Items.Add(item);
        combo.SelectedIndex = combo.Items.Count > 0 ? 0 : -1;
        return combo;
    }

    private static Button Button(string text, string kind, RoutedEventHandler onClick)
    {
        var b = new Button
        {
            Content = text,
            Height = 36,
            MinWidth = 96,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(0, 0, 8, 8),
            Cursor = System.Windows.Input.Cursors.Hand,
            Background = kind == "Primary" ? Brush("#2563EB") : Brushes.White,
            Foreground = kind == "Primary" ? Brushes.White : Brush("#111827"),
            BorderBrush = kind == "Primary" ? Brush("#2563EB") : Brush("#CBD5E1"),
            BorderThickness = new Thickness(1)
        };
        b.Click += onClick;
        return b;
    }

    private static StackPanel Stack(string title, params UIElement[] children)
    {
        var stack = new StackPanel();
        stack.Children.Add(SectionTitle(title));
        foreach (var child in children)
        {
            stack.Children.Add(child);
        }
        return stack;
    }

    private static Grid TwoColumns(double left, double right)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(left, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(right, GridUnitType.Star) });
        return grid;
    }

    private static ScrollViewer Scroll() => new()
    {
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
    };

    private static Border StatCard(string title, string value, string caption, string color)
    {
        var card = Card(new Thickness(16), new Thickness(0, 0, 12, 0));
        var stack = new StackPanel();
        card.Child = stack;
        stack.Children.Add(new TextBlock { Text = title, Foreground = Brush("#6B7280"), FontSize = 13 });
        stack.Children.Add(new TextBlock { Text = value, Foreground = Brush(color), FontSize = 30, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 7, 0, 2) });
        stack.Children.Add(new TextBlock { Text = caption, Foreground = Brush("#6B7280"), FontSize = 12 });
        return card;
    }

    private static StackPanel Row(string label, params UIElement[] controls)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        row.Children.Add(new TextBlock
        {
            Text = label,
            Width = 96,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brush("#374151")
        });
        foreach (var control in controls)
        {
            if (control is TextBox tb) tb.Width = 300;
            row.Children.Add(control);
        }
        return row;
    }

    private static StackPanel CheckRow(params object[] items)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        for (var i = 0; i < items.Length; i += 2)
        {
            var text = items[i]?.ToString() ?? "选项";
            var isChecked = i + 1 < items.Length && items[i + 1] is bool b && b;
            row.Children.Add(new CheckBox
            {
                Content = text,
                IsChecked = isChecked,
                Margin = new Thickness(0, 0, 22, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brush("#374151")
            });
        }
        return row;
    }

    private static DataGrid DataGrid(System.Collections.IEnumerable source, double height)
    {
        var grid = new DataGrid
        {
            ItemsSource = source,
            AutoGenerateColumns = true,
            IsReadOnly = false,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            GridLinesVisibility = DataGridGridLinesVisibility.All,
            RowHeight = 34,
            ColumnHeaderHeight = 36,
            AlternatingRowBackground = Brush("#F8FAFC"),
            BorderBrush = Brush("#E5E7EB"),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 10)
        };
        if (height > 0) grid.Height = height;
        return grid;
    }

    private static TabItem Tab(string header, UIElement content) => new()
    {
        Header = header,
        Content = content,
        Padding = new Thickness(12, 6, 12, 6)
    };

    private static FrameworkElement CreateFormPanel(IEnumerable<(string Label, string Value)> fields)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
        foreach (var field in fields)
        {
            stack.Children.Add(Row(field.Label + "：", TextBox(field.Value)));
        }
        return stack;
    }

    private static UIElement FlowText()
    {
        return InfoText("选择FBX源文件 → 配置导入选项 → 选择蓝图模板 → 配置组件挂载 → 预览任务与冲突 → 执行导入与生成 → 查看结果报告。当前版本只模拟前端流程，不执行真实UE资产创建。");
    }
}

public static class ControlExtensions
{
    public static T Also<T>(this T self, Action<T> action)
    {
        action(self);
        return self;
    }
}

public sealed class FbxFileItem
{
    public bool Selected { get; set; }
    public string FileName { get; set; }
    public string FullPath { get; set; }
    public string Size { get; set; }
    public string Category { get; set; }
    public string ModifiedTime { get; set; }
    public FbxFileItem(bool selected, string fileName, string fullPath, string size, string category, string modifiedTime)
    {
        Selected = selected;
        FileName = fileName;
        FullPath = fullPath;
        Size = size;
        Category = category;
        ModifiedTime = modifiedTime;
    }
}

public sealed class PreviewItem
{
    public string Status { get; set; }
    public string SourceFile { get; set; }
    public string AssetType { get; set; }
    public string AssetName { get; set; }
    public string TargetPath { get; set; }
    public string Conflict { get; set; }
    public PreviewItem(string status, string sourceFile, string assetType, string assetName, string targetPath, string conflict)
    {
        Status = status; SourceFile = sourceFile; AssetType = assetType; AssetName = assetName; TargetPath = targetPath; Conflict = conflict;
    }
}

public sealed class ComponentRuleItem
{
    public string DisplayName { get; set; }
    public string ComponentType { get; set; }
    public string ClassPath { get; set; }
    public string AttachParent { get; set; }
    public string ComponentName { get; set; }
    public bool Required { get; set; }
    public string MissingPolicy { get; set; }
    public ComponentRuleItem(string displayName, string componentType, string classPath, string attachParent, string componentName, bool required, string missingPolicy)
    {
        DisplayName = displayName; ComponentType = componentType; ClassPath = classPath; AttachParent = attachParent; ComponentName = componentName; Required = required; MissingPolicy = missingPolicy;
    }
}

public sealed class BlueprintTemplateItem
{
    public string TemplateName { get; set; }
    public string Category { get; set; }
    public string ParentClass { get; set; }
    public int ComponentCount { get; set; }
    public string Description { get; set; }
    public BlueprintTemplateItem(string templateName, string category, string parentClass, int componentCount, string description)
    {
        TemplateName = templateName; Category = category; ParentClass = parentClass; ComponentCount = componentCount; Description = description;
    }
}

public sealed class GenerationTaskItem
{
    public string TaskName { get; set; }
    public string Profile { get; set; }
    public int FbxCount { get; set; }
    public string Status { get; set; }
    public string CreatedTime { get; set; }
    public string Description { get; set; }
    public GenerationTaskItem(string taskName, string profile, int fbxCount, string status, string createdTime, string description)
    {
        TaskName = taskName; Profile = profile; FbxCount = fbxCount; Status = status; CreatedTime = createdTime; Description = description;
    }
}

public sealed class ResultItem
{
    public string SourceFile { get; set; }
    public string Status { get; set; }
    public string BlueprintName { get; set; }
    public int AssetCount { get; set; }
    public string ComponentResult { get; set; }
    public string Message { get; set; }
    public ResultItem(string sourceFile, string status, string blueprintName, int assetCount, string componentResult, string message)
    {
        SourceFile = sourceFile; Status = status; BlueprintName = blueprintName; AssetCount = assetCount; ComponentResult = componentResult; Message = message;
    }
}

public sealed class RuleItem
{
    public string AssetType { get; set; }
    public string RuleValue { get; set; }
    public string Description { get; set; }
    public RuleItem(string assetType, string ruleValue, string description)
    {
        AssetType = assetType; RuleValue = ruleValue; Description = description;
    }
}

public sealed class LogItem
{
    public string Time { get; set; }
    public string Level { get; set; }
    public string Message { get; set; }
    public LogItem(string time, string level, string message)
    {
        Time = time; Level = level; Message = message;
    }
}

public sealed class ConflictItem
{
    public string SourceFile { get; set; }
    public string ConflictType { get; set; }
    public string Detail { get; set; }
    public string Policy { get; set; }
    public ConflictItem(string sourceFile, string conflictType, string detail, string policy)
    {
        SourceFile = sourceFile; ConflictType = conflictType; Detail = detail; Policy = policy;
    }
}

public sealed class MissingComponentItem
{
    public string SourceFile { get; set; }
    public string ComponentName { get; set; }
    public string RequiredLevel { get; set; }
    public string Policy { get; set; }
    public MissingComponentItem(string sourceFile, string componentName, string requiredLevel, string policy)
    {
        SourceFile = sourceFile; ComponentName = componentName; RequiredLevel = requiredLevel; Policy = policy;
    }
}

public sealed class ComponentAttachResult
{
    public string BlueprintName { get; set; }
    public string ComponentName { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public ComponentAttachResult(string blueprintName, string componentName, string status, string message)
    {
        BlueprintName = blueprintName; ComponentName = componentName; Status = status; Message = message;
    }
}

public sealed class ProfileItem
{
    public string ProfileName { get; set; }
    public string Category { get; set; }
    public string TemplateName { get; set; }
    public string MissingPolicy { get; set; }
    public string UpdatedTime { get; set; }
    public ProfileItem(string profileName, string category, string templateName, string missingPolicy, string updatedTime)
    {
        ProfileName = profileName; Category = category; TemplateName = templateName; MissingPolicy = missingPolicy; UpdatedTime = updatedTime;
    }
}

public sealed class FrontendConfig
{
    public string SourceFolder { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string MissingPolicy { get; set; } = string.Empty;
    public FrontendConfig() { }
    public FrontendConfig(string sourceFolder, string outputPath, string profileName, string templateName, string missingPolicy)
    {
        SourceFolder = sourceFolder; OutputPath = outputPath; ProfileName = profileName; TemplateName = templateName; MissingPolicy = missingPolicy;
    }
}
