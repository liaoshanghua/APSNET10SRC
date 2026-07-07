# APSCore / APSAPI 全量迁移说明

## 目标

1. **APSCore 全量**：`EasyManufacture.Core/MvcControl/APSCore.cs` → `ApsCoreEngine.LegacyCore.cs`
2. **APSAPI 全量**：`EasyManufacture.Web/Controllers/APSAPIController.cs` → `ApsCoreEngine.LegacyApi.cs`
3. **HTTP 路由**：Api 项目 `APSAPIController` + `LegacyStubs` + `LegacyCoreStubs` 暴露 `/APSAPI/{action}`

## 架构（迁移后）

```
旧:  APSAPIController : APSCore          (Web 一个 Controller 继承基类)
新:  APSAPIController (Api 路由)  →  ApsCoreEngine (Infrastructure partial)
         ├─ 显式方法 (GetConfig/APSData…)     → Service / ApsCoreEngine
         └─ LegacyStubs (InvokeLegacyAsync)   → ApsApiLegacyDispatcher 反射
```

## 已完成的工程化工作

| 项 | 说明 |
|----|------|
| `ApsCoreEngine.LegacyApi.cs` | 旧 Web APSAPIController 业务方法已迁入（约 2.75 万行） |
| `ApsCoreEngine.LegacyCore.cs` | 旧 APSCore 已迁入（约 2.8 万行，脚本生成） |
| `scripts/Migrate-ApsCore.ps1` | 从 `APSCore.cs` 一键生成 `LegacyCore.cs` |
| `EnableLegacyApsApiSource=true` | 编译 LegacyApi；排除 Menus/Organize/YrfExtensions 精简重复 |
| `EnableLegacyApsCoreSource=true` | 编译 LegacyCore；排除 Body/ApsData 精简版 |
| `ApsApiLegacyDispatcher` | 反射 `ApsCoreEngine` 同名方法，失败时可转发旧站 |
| `APSAPIController.LegacyStubs.cs` | Web Controller Action 路由壳 |
| `Generate-ApsApiStubs.ps1` | 自动生成 LegacyCoreStubs（APSCore 公共方法） |
| MVC 兼容层 | `ApsLegacyJsonResult`、`LegacyHttpShim`、`AddWatermarkToPdf` 等 |
| Release 编译 | Infrastructure 0 错误（2025 迁移批次） |

## 尚未完整迁移

| 项 | 旧位置 | 现状 |
|----|--------|------|
| `override APSData()` 的 dicID switch | Web APSAPIController 约 3090–3795 行 | **未迁入**；Net10 仅 `YrfDicHooks` 6 个 case |
| SetDt/setDetail 实现 | LegacyApi.cs 中已有 | 需 dic switch 挂载后才生效 |
| 临时方案 | — | `LegacyWeb:ForwardApsData=true` 转发旧 Web |

## 启用 / 更新步骤

### 更新 APSCore（改旧 Core 后）

```powershell
cd EasyManufacture.Net10
.\scripts\Migrate-ApsCore.ps1
dotnet build EasyManufacture.Net10.sln -c Release
```

### 更新 APSAPI 路由（改 ApsCoreEngine 公共方法后）

```powershell
.\scripts\Generate-ApsApiStubs.ps1
```

### 关闭旧站转发（本地全量运行时）

```json
"LegacyWeb": {
  "ForwardAllApsApi": false,
  "ForwardApsData": false
}
```

## Global.asax 定时任务

| 项 | 说明 |
|----|------|
| `DatabaseSchemaUpgradeHostedService` | Application_Start 库表补丁 |
| `GlobalLegacyPushTypeJob` | 各 PushType 逻辑（SAP/邮件/EAST/JG 等） |
| `GlobalScheduledTasksHostedService` | 按 `AppInfo.PushType` 注册周期任务 |

PushType 与旧站一致：`YS`、`YRF`、`ISGO`、`EK`、`12`、`OUSAI` 等。

## 当前默认（csproj）

```xml
<EnableLegacyApsApiSource>true</EnableLegacyApsApiSource>
<EnableLegacyApsCoreSource>true</EnableLegacyApsCoreSource>
```

类库打包：`.\scripts\Pack-Libraries.ps1` → `packages/`，供 `EasyManufacture.Api` 独立宿主引用。
