# Legacy 层说明



本目录为旧 `EasyManufacture.Core` 的 APS 业务迁移代码（**APSCore / MSSQLCore / EF**）。



## 核心类型



- **`ApsCoreEngine`**（partial）：旧 `APSCore` 全量逻辑（`LegacyCore.cs`）

- **`MSSQLCore`**：字典 SQL 构建（精简 `MSSQLCore.cs` / 全量 `MSSQLCore.LegacyFull.cs`）

- **`ManufactureDbContext`**：EF Core 映射旧实体



## APSAPIController 源码位置



旧 Web `APSAPIController` **不在本目录**，已独立到 Api 项目：



```

EasyManufacture.Api/Legacy/

  APSAPIController.LegacyBusiness.cs

  APSAPIController.ApsDataOverride.cs

```



`APSAPIController : ApsCoreEngine` 在 Api 项目编译；业务方法为 `partial APSAPIController`。



## 维护约定



| 源文件 | 更新方式 |

|--------|----------|

| `EasyManufacture.Core/MvcControl/APSCore.cs` | `scripts/Migrate-ApsCore.ps1` → `LegacyCore.cs` |

| `EasyManufacture.Web/Controllers/APSAPIController.cs` | `scripts/Migrate-ApsApi.ps1` → Api/Legacy/ |



## 编译开关



`EasyManufacture.Infrastructure.csproj`：



- `EnableLegacyApsCoreSource` → `LegacyCore.cs` + `MSSQLCore.LegacyFull.cs`


