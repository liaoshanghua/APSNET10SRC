# EasyManufacture.Net10 使用说明（目标框架 .NET 10）

> 完整目录与 ApsCoreEngine 文件索引见 **[项目结构说明.md](项目结构说明.md)**。

## 1. APSData（预排产/报表查询）

### 已实现

- **原生引擎**：`MSSQLCore`（精简版 SQL 构建）+ `APSDataCore` + 盈瑞丰 dic 钩子（28574/28580/28581/28589/28634/28636）
- **登录**：`AccountAuthenticationMiddleware` 写入 `V_Dev_Account`（含 `RoleMap`），与旧站 Session 等价
- **授权**：`JDRegister` 校验，未注册返回与旧站相同提示

### 接口

```http
POST /APSAPI/APSData
Content-Type: application/json
token: {DES 加密的账号}

{"dicID":28581,"monthRange":["2025-01-01","2025-12-31"],...}
```

### 与旧站差异

| 场景 | 说明 |
|------|------|
| 通用字典查询 | Net10 精简版 `MSSQLCore` 覆盖主路径（条件、分页、组织过滤） |
| 极复杂 dic 分支 | 旧 `APSCore.APSData` 约 3700 行特殊逻辑；未覆盖时请用 **旧站转发** |
| 盈瑞丰报表 dic | `SetDt` 已同步，需登录后调用 |

### 旧站转发（可选，100% 行为一致）

`appsettings.json`：

```json
"LegacyWeb": {
  "BaseUrl": "http://旧站地址:端口",
  "ForwardApsData": true
}
```

启用后 Net10 将请求体原样 `POST` 到旧站 `/APSAPI/APSData`，并透传 `token`/`Cookie`。

---

## 2. 项目结构与 NuGet 封装

| 目录 | 用途 |
|------|------|
| `EasyManufacture.Net10/` | 类库源码开发；`src/` 含 Domain / Application / Infrastructure / Licence + 联调 Api |
| `EasyManufacture.Net10/packages/` | 本地 NuGet 包输出（`scripts/Pack-Libraries.ps1`） |
| `EasyManufacture.Api/` | **独立交付宿主**，仅含 Api 源码，通过 NuGet 引用上述包 |

### 类库打包

```powershell
cd EasyManufacture.Net10
.\scripts\Pack-Libraries.ps1
```

### 独立 Api 运行

```powershell
cd EasyManufacture.Api
dotnet restore
dotnet run --project src\EasyManufacture.Api
```

版本号：`Directory.Build.props` 的 `VersionPrefix` 与 `EasyManufacture.Api` 中 `EasyManufacturePackageVersion` 保持一致。

---

## 3. SSO 单点登录

### 配置格式（`App:SSOUrl`）

分隔符为 Unicode 字符 **‖**（非竖线 `|`）：

```
项目名‖回调根地址‖密钥
```

示例：

```json
"App": {
  "SSOUrl": "XingHe‖http://192.168.1.231‖tJ7qE9sA2fDgHkLzP6bN4cV1mR8uY0w"
}
```

| 项目名 | 算法 |
|--------|------|
| `EK` | AES-ECB，`EncryptToken("ek_{loginid}|{timestamp}_sso", 密钥)` |
| `XingHe` | MD5，`MD5("{loginid}|{timestamp}|{盐}")` |

### 接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET/POST | `/APSAPI/RequestSSOUrl?loginid=&timestamp=&token=&EFTime=&gopage=` | 生成免登录跳转 URL |
| POST | `/APSAPI/APSRequestSSOUrl` | Body JSON 校验 token 并返回账号密码 |

启动时若 `SSOUrl` 为空，日志会提示配置方法；格式错误会警告段数不足。

---

## 4. 定时任务（Global.asax）

见 `ScheduledTasks` 与 `App:PushType`：

| PushType | 行为 |
|----------|------|
| `YS`（当前） | 仅 SAP 接口表检查 + 可选 `WX` 企业微信 |
| `YRF` | 每 30 分钟扫描 `ScheduledTasks:YrfExcelDirectory` 导入产能 Excel |
| `ISGO` | 图纸/PDF 定时任务（目录见 `IsgoDrawingDirectory`） |
| `EK` | 每天 18:10–18:16 MO 开工调度 |

关闭全部后台任务：`ScheduledTasks:Enabled: false`

---

## 5. 联调清单

1. 配置 `ConnectionStrings:MSSQLConnectionString`
2. 配置 `App` 节点（与旧 `Web.config` appSettings 同名）
3. `POST /APSAPI/Ping` 健康检查
4. `POST /Login/CheckAccount` 登录拿 token
5. `POST /APSAPI/GetConfig` / `SaveData` / `APSData`
6. SSO：配置 `SSOUrl` 后测 `RequestSSOUrl`
7. 复杂报表：必要时 `LegacyWeb:ForwardApsData=true`

---

## 6. 盈瑞丰 PushType 说明

当前 `PushType=YS` 与旧站一致：**不启动** YRF/ISGO 专用 Timer。  
若需产能 Excel 自动导入，将 `App:PushType` 改为 **`YRF`**。
