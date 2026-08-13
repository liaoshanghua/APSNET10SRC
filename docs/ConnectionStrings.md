# 数据库连接串（从旧 Web.config 迁移）

> 来源：`EasyManufacture.Web/Web.config` → `connectionStrings`  
> 新项目配置：`src/EasyManufacture.Api/appsettings.json`

## 当前启用

| 项 | 值 |
|----|-----|
| 客户 | 盈瑞丰 |
| 键 | `ConnectionStrings:MSSQLConnectionString` |
| 服务器 | `192.168.1.88` / `APS` |

## 切换客户

1. 打开 `docs/ConnectionStrings-Profiles.json`，按 `name` 找到客户
2. 复制该条的 `mssql` 到 `appsettings.json` → `ConnectionStrings.MSSQLConnectionString`
3. 若无 `TrustServerCertificate=True`，请补上（.NET 10 / 新版驱动常用）
4. 若有 SCM 库，同步填 `MSSQLConnectionStringSCM`
5. 重启 API

或在 `appsettings.json` 底部已注释的常用客户里直接取消注释、复制到 `MSSQLConnectionString`。

## 文件说明

| 文件 | 说明 |
|------|------|
| `docs/ConnectionStrings-Profiles.json` | **81 套**客户连接（`enabled: false`，仅备查）；含 **大叶**、**富来** 等 |
| `appsettings.json` | 运行时实际使用的连接串 |
| `scripts/Extract-ConnectionProfiles.js` | 从旧 Web.config 重新导出 profiles |

## 与旧版差异

| 旧 Web.config | 新 appsettings.json |
|---------------|---------------------|
| `EasyManufactureEntities` + `MSSQLConnectionString` 成对 | 仅 `MSSQLConnectionString`（EF 已不用） |
| XML 注释切换 | JSON `//` 注释 + Profiles 文件 |
| 无 TrustServerCertificate | 建议加上 |

## 重新导出

旧 Web.config 有更新时：

```bash
node EasyManufacture.Net10/scripts/Extract-ConnectionProfiles.js
```
