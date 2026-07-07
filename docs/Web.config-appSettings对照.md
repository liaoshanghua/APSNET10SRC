# Web.config → appsettings.json 对照

来源：`EasyManufacture.Web/Web.config` 中当前启用的 `<appSettings>`（盈瑞丰环境）。

| Web.config `key` | appsettings.json 路径 | 说明 |
|------------------|----------------------|------|
| `AppCode` | `App:AppCode` | Cookie/Token 名称，旧值为 `ISGO` |
| `MSSQLConnectionString` | `ConnectionStrings:MSSQLConnectionString` | 与 Web.config 活动连接串一致 |
| `mes:WebService` | `App:mes:WebService` | MES 亮灯接口 |
| `X-KDApi-*` | `App:X-KDApi-*` | 金蝶云星空 OpenAPI |
| `PushType` | `App:PushType` | 客户类型（如 `YS`） |
| `IsSafe` | `App:IsSafe` | 防刷（旧站未配置时默认 `0`） |
| `SAPConn` / `WebJsonInterface` / `UI` | `App:*` | AppInfo 使用，Web.config 无则留空 |

## 未迁入 App 节的键（仅 MVC 使用）

以下键保留在旧站 Web.config，.NET 9 API 不需要：

- `webpages:Version`、`webpages:Enabled`
- `ClientValidationEnabled`、`UnobtrusiveJavaScriptEnabled`

## 本地覆盖

开发环境可在 `appsettings.Development.json` 中覆盖任意 `App:*` 项（示例已开启 `IsSaveLog=1`）。

## 敏感信息

`Password`、`X-KDApi-AppSec`、`ConnectionStrings` 含密钥，生产环境建议使用用户机密或环境变量：

```bash
dotnet user-secrets set "App:Password" "your-password" --project src/EasyManufacture.Api
```
