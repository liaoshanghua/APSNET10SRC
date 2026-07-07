# 源码版（EasyManufacture.Net10）

> **中文简称**：**源码版** · 对外公版见同级目录 **公版**（`../APSNET10/`）  
> 名称对照：[docs/项目名称对照.md](docs/项目名称对照.md)

基于 **.NET 10** 的易制造后台 API，对齐旧版 `EasyManufacture.Web` 的路由、JSON 入参、Token/Cookie 登录与字典配置接口。

> 目标框架：`net10.0`。默认 API 端口见 `src/EasyManufacture.Api/Properties/launchSettings.json`（通常 `9999`）。

## 项目结构

```
EasyManufacture.Net10/
├── src/EasyManufacture.Api              # ASP.NET Core 宿主（联调：直接引用类库源码）
├── src/EasyManufacture.Application      # 服务抽象（IConfigService、IApsDataService…）
├── src/EasyManufacture.Domain           # 配置项、DTO
├── src/EasyManufacture.Infrastructure   # EF、SqlHelper、ApsCoreEngine Legacy、定时任务
├── src/EasyManufacture.Licence          # AppInfo、JDRegister、SystemLog
├── packages/                            # 本地 NuGet 包（scripts/Pack-Libraries.ps1）
├── scripts/                             # 迁移、打包、路由生成脚本
└── docs/                                # 使用说明与结构文档
```

**公版（给同事）**：同级目录 [`../APSNET10/`](../APSNET10/README.md)（仅 Api 源码 + `lib/` 混淆 DLL，GitHub 仓库名仍为 `APSNET10`）。

## 旧版源码在哪里？

旧站 `APSAPIController` + `APSCore` **已迁入 Infrastructure**，不在 Api 的 Controller 文件里：

| 旧文件 | 新版位置 |
|--------|----------|
| `EasyManufacture.Web/Controllers/APSAPIController.cs` | `Infrastructure/Legacy/ApsCoreEngine.LegacyApi.cs` |
| `EasyManufacture.Core/MvcControl/APSCore.cs` | `Infrastructure/Legacy/ApsCoreEngine.LegacyCore.cs` |

Api 层 `APSAPIController` 只做 HTTP 路由；`LegacyStubs` 通过反射调用 `ApsCoreEngine` 同名方法。

完整索引见 **[docs/项目结构说明.md](docs/项目结构说明.md)**，APS 迁移细节见 **[docs/MIGRATION-APS.md](docs/MIGRATION-APS.md)**。

## 编译开关（Infrastructure.csproj）

| 属性 | 默认 | 说明 |
|------|------|------|
| `EnableLegacyApsApiSource` | `true` | 编译 `LegacyApi.cs`（旧 APSAPIController 全文） |
| `EnableLegacyApsCoreSource` | `true` | 编译 `LegacyCore.cs`（旧 APSCore 全文） |

两者均为 `true` 时与旧站行为最接近。改 `APSCore.cs` 后须重跑 `scripts/Migrate-ApsCore.ps1`。

## 发布与部署（Windows）

**推荐一条命令发布**（自动带上 bat/ps1，可选打包 .NET 安装包）：

```powershell
cd EasyManufacture.Net10

# 方式 A：统一缓存目录 deps\dotnet\（推荐）
# 1) 首次下载（只需一次，有网）
.\scripts\Download-DotNetRuntimePack.ps1 -OutputDir ".\deps\dotnet"
# 2) 之后 publish 会自动复制到 publish\api\runtime\
.\scripts\Publish-Aps.ps1 -OutputDir ".\publish\api"

# 方式 B：发布时顺带下载（等价于先填 deps\dotnet 再 publish）
.\scripts\Publish-Aps.ps1 -OutputDir ".\publish\api" -WithRuntime

# 方式 C：仅发布（服务器需已装 .NET 10）
.\scripts\Publish-Aps.ps1 -OutputDir ".\publish\api"
```

**离线 runtime 统一目录**（`*.exe` 不提交 Git）：

```
EasyManufacture.Net10/deps/dotnet/     ← 开发机放安装包，publish 自动复制
├── dotnet-runtime-10.x.x-win-x64.exe
├── aspnetcore-runtime-10.x.x-win-x64.exe
└── README.txt
```

发布目录结构（**直接整包拷到服务器**，双击 `APS-启动.bat`）：

```
publish/api/
├── APS.exe
├── APS-启动.bat              ← 手动启动入口
├── APS-安装开机自启.bat      ← 系统开机自启（无需登录，需管理员）
├── start-api.bat             ← 计划任务实际执行的脚本
├── Install-ApsDependencies.ps1
├── Install-ApsAutoStart.ps1
├── appsettings.json          ← 改连接串
├── runtime/
│   ├── dotnet-runtime-10.x.x-win-x64.exe
│   ├── aspnetcore-runtime-10.x.x-win-x64.exe
│   └── README.txt
└── register.ini              ← 授权（可首次启动后生成）
```

部署脚本源码唯一位置：`src/EasyManufacture.Api/Deployment/`（**不要改 publish 输出里的副本**）。

```powershell
# 手动 publish（等效于 Publish-Aps.ps1 不带 -WithRuntime）
dotnet publish src/EasyManufacture.Api/EasyManufacture.Api.csproj -c Release -o publish/api

# 开机自启（系统启动，无需用户登录）

默认 `appsettings.json` 已配置 `AutoStart.AtStartup: true`（系统启动时 / SYSTEM 账户）。

**服务器上推荐做法**（发布目录内，右键「以管理员身份运行」）：

```text
APS-安装开机自启.bat
```

或 PowerShell（管理员）：

```powershell
cd D:\publish\api
powershell -ExecutionPolicy Bypass -File .\Install-ApsAutoStart.ps1
```

注册后可在「任务计划程序」看到任务 **APS**，触发器为「启动时」，运行账户 **SYSTEM**。重启 Windows 后即使无人登录，APS 也会自动启动。

- 日志：`logs\startup.log`、`logs\aps-console.log`
- 验证：`GET http://服务器:9999/APSAPI/Ping`
- **注意**：SYSTEM 账户访问网络共享、SQL 集成认证时权限可能与登录用户不同，共享路径请用 UNC（`\\server\share`）

若只需「某用户登录后启动」，改 `appsettings.json`：`AtLogOn: true`，`AtStartup: false`（无需管理员）。

```powershell
# 开发机从仓库脚本安装（转发到 Deployment）
.\scripts\Install-ApiAutoStart.ps1 -PublishPath ".\publish\api" -Port 9999
```

# 自包含发布（无需安装 .NET，体积大）
dotnet publish src/EasyManufacture.Api/EasyManufacture.Api.csproj -c Release -o publish/api-sc -p:PublishProfile=SelfContained-win-x64
```

- **缺 .NET 10**：`runtime/` 放两个 exe（见上）；或用 `-WithRuntime` 发布
- **不要**在 bat 里传 `-PublishPath`（已修复；脚本自动用自身目录）
- 健康检查：`GET http://服务器:9999/APSAPI/Ping`

### 热部署（短暂停服换包）

```powershell
# 开发机：发布并同步到服务器 update 文件夹
.\scripts\Publish-Aps-HotDeploy.ps1 -TargetPath "\\服务器\APSNEW"

# 服务器：双击 APS-热更新.bat（或见 docs/热部署说明.md）
```

详见 **[docs/热部署说明.md](docs/热部署说明.md)**。

## 运行（联调）

```powershell
cd EasyManufacture.Net10
dotnet run --project src/EasyManufacture.Api
```

1. 配置 `src/EasyManufacture.Api/appsettings.json`（连接串、`App` 节点与旧 `Web.config` 同名）
2. 生产环境将 `register.ini` 放在 Api 内容根目录
3. 健康检查：`GET /APSAPI/Ping`

## 打包类库（供 EasyManufacture.Api 引用）

```powershell
.\scripts\Pack-Libraries.ps1
```

## 文档

| 文档 | 内容 |
|------|------|
| [docs/Net10使用说明.md](docs/Net10使用说明.md) | APSData、SSO、定时任务、联调清单 |
| [docs/项目结构说明.md](docs/项目结构说明.md) | 目录、请求链路、ApsCoreEngine 文件索引 |
| [docs/热部署说明.md](docs/热部署说明.md) | 发布同步 + APS-热更新.bat 准零停机换包 |
| [docs/MIGRATION-APS.md](docs/MIGRATION-APS.md) | APSCore/APSAPI 迁移步骤与待办 |

## 与旧版对应（摘要）

| 旧版 | 新版 |
|------|------|
| `BaseController.BodyJson` | `RequestBodyMiddleware` + `IRequestBodyAccessor` |
| `OnAuthorization` / token | `AccountAuthenticationMiddleware` |
| `Global.asax` 定时任务 | `GlobalScheduledTasksHostedService` 等 |
| `SqlHelper` | `Infrastructure.Data.SqlHelper` |
