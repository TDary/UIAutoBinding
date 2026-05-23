# UIAutoBind

基于命名约定的 Unity uGUI 静态 UI 绑定方案。Editor 时自动扫描 Prefab 子节点，运行时 O(1) 字典查找替代 GameObject.Find / GetComponentsInChildren 全场景遍历。

## 接入步骤

### 1. 导入文件

将 Runtime/ 和 Editor/ 两个文件夹解压到项目的任意 Assets/ 子目录下（推荐 Assets/Plugins/UIAutoBind/）。

### 2. 命名 UI 子节点

按前缀命名 Prefab 中的 UI 控件：

| 前缀 | 类型 | 示例 |
|------|------|------|
| btn_ | Button | btn_Start -> Key "Start" |
| txt_ | Text / TMP | txt_Title -> Key "Title" |
| img_ | Image | img_Icon -> Key "Icon" |
| tog_ | Toggle | tog_Music -> Key "Music" |
| sld_ | Slider | sld_Volume -> Key "Volume" |
| scr_ | ScrollRect | scr_List -> Key "List" |
| inp_ | InputField / TMP | inp_Name -> Key "Name" |
| drp_ | Dropdown / TMP | drp_Quality -> Key "Quality" |
| raw_ | RawImage | raw_Thumb -> Key "Thumb" |

### 3. 给 Prefab 根节点加组件

选中 Prefab 根节点 -> Add Component -> UIAutoBinder。

### 4. 配置搜索路径

修改 Editor/UIAutoBinderScanner.cs 中的 UIPrefabSearchPaths 数组，指向项目的 UI Prefab 文件夹。

### 5. 一键挂载 + 扫描

菜单栏 Tools -> UIAutoBind -> Setup All UI Form Prefabs。

此后每次在 Prefab Mode 中 Ctrl+S 保存，绑定列表自动刷新。

### 6. 代码中使用

```csharp
using UIAutoBind;

var binder = GetComponent<UIAutoBinder>();
Button startBtn = binder.GetButton("Start");
Text titleTxt = binder.GetText("Title");
Slider volSlider = binder.GetSlider("Volume");
Image icon = binder.GetUIComponent<Image>("Icon");
if (binder.HasKey("Start")) { ... }
List<Button> allButtons = binder.GetAllOfType<Button>();
```

### 7. 集成到 UI 基类

```csharp
private UIAutoBinder m_AutoBinder;
public UIAutoBinder AutoBinder => m_AutoBinder;
// OnInit 末尾:
m_AutoBinder = GetComponent<UIAutoBinder>();
```

## 构建保障

打包时自动触发 IPreprocessBuildWithReport，扫描空绑定并校验损毁引用。

## 依赖

- Unity 2019.4+
- com.unity.ugui

## 许可

MIT

## 自动化框架集成示例

借助 UIAutoBind，自动化/测试框架可直接按键名操控 UI，无需硬编码层级路径：

```csharp
// 获取当前顶层 Form
var topForm = FindTopUIForm(); // 项目自行实现
var binder = topForm.GetComponent<UIAutoBinder>();

// 模拟点击按钮
binder.GetButton("Start").onClick.Invoke();

// 读取文本
string hp = binder.GetText("HpValue").text;

// 设置开关
binder.GetToggle("Music").isOn = false;

// 列出当前 Form 所有可交互按钮
foreach (var btn in binder.GetAllOfType<Button>())
    Debug.Log($"Button: {btn.name}, interactable={btn.interactable}");
```

对比传统方式 `GameObject.Find("Canvas/.../btn_Start")` —— 层级变动不影查找，O(1) 字典查表零 GC 分配。

## 性能说明

- **运行时查询**: `GetUIComponent<T>()` 纯字典查找，O(1)，零 GC 分配
- **Editor 扫描**: Type 解析结果自动缓存，同类型名只反射一次，后续扫描零开销
- **构建扫描**: 与 Setup 共享同一份 `UIPrefabSearchPaths` 配置，只扫 UI 文件夹不扫全项目
