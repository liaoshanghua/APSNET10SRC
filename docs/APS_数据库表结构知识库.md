# APS 系统数据表结构知识库

> 本文档持续记录 APS 系统的数据表结构，便于查询和维护。

**修订提示**：增量表 **§44～§58**（税率、合同域、**库存调整单头/明细**、销售订单退货、支付方式、**销售退货单头/明细**、**销售订单行完成**、**SAP 接口输出明细**、**工作中心清单**等）均含 **同一段** 可执行 SQL（表+索引+扩展属性+`V_` 视图）。各节见 **§44.5～§58.5** 标题。

**共享版索引**：团队协作速查见 **`APS_数据库表结构知识库_共享版.md`**（表清单、§44～§58 摘要）；**库存调整单**见 **§52、§53**（**§52.5、§53.5**）；**销售退货单（按出库单）**见 **§54、§55**（**§54.5、§55.5**）；**销售订单行完成**见 **§56**（**§56.5**）；**SAP 接口输出明细**见 **§57**（**§57.5**）；**工作中心清单**见 **§58**（**§58.5**）；**销售合同** §49～§50；**支付方式** §51。

**业务说明**：大部分情况下 `OrganizeID` 字段不使用，可忽略其关联。

**建表约定**：
- 新表须在数据库中维护 SQL Server 扩展属性（`MS_Description`）：表级一条业务说明，**每列一条**说明；重要索引可补充说明。脚本见各表「扩展属性（SQL）」小节。
- **备注字段**：自本知识库约定起，**新建业务表须包含** `Remark1`、`Remark2`，类型均为 `nvarchar(500) NULL`，与其它 APS 表一致；须为两列分别添加扩展属性。
- **同步视图**：新建业务表须同步创建视图 **`V_` + 表名**（示例：`APS_TaxRate` → `V_APS_TaxRate`）。视图体默认 **`SELECT * FROM 基表`**（不写死列名；只读透出）。**例外**：**§49** `V_APS_SalesContract`、**§50** `V_APS_SalesContractDetail` 为 **多表 JOIN + 显式关键列**（见各节脚本）。**视图脚本头部**（紧接在 `CREATE VIEW` 之前）须用注释块写清三项：**创建人**、**创建日期**、**作用**；并同时在视图上维护扩展属性 **`MS_Description`**，内容与头部注释保持一致（便于 SSMS 对象属性展示）。
- **脚本完整性**：**§44～§58** 已按此格式维护；**此后每张新建表**仍须在**同一 SQL 代码块**内一次性给出：**表 DDL**、**索引**、**表/列/索引扩展属性**、**同步视图**（`DROP VIEW` + `CREATE VIEW` + 视图 `MS_Description`），便于直接复制执行。
- **对话交付**：在 Cursor/对话中交付新建或变更表时，除写入本 Markdown 外，**须在当次回复中全文贴出**上述同一段可复制 SQL（不得仅给章节号）；与文档 §x.x 保持一致。
- **Status 全库约定**：凡 **`Status`（int）**，**一律 0=草稿，1=确认**（确认态含：已生效、已启用、已落库等可供业务使用的最终态）。历史表述「1启用0禁用」与本约定**同序**：**0=非确认（草稿/停用）**，**1=确认（启用）**。需 **≥2** 的扩展枚举（作废、终止等）须在**该表 §备注**中单独写明；无说明则仅用 0/1。**数据库默认值**：`Status` **`DEFAULT (1)`**（新建行默认即**已确认**；若业务要先落草稿，插入时显式写 `0`）。

---

## 1. APS_Material（料品表）

物料/料品主数据表，存储料品的基础信息、库存策略、包装规格及扩展属性。

### 1.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_Material` |
| 主键 | `MaterialID` (bigint, 自增) |
| 默认值 | `CreatedOn` = getdate()，`IsScheduling` = 1 |

### 1.2 字段清单

#### 主键与标识
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MaterialID | bigint | NOT NULL | 料品ID（主键，自增） |

#### 基础信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MaterialName | nvarchar(100) | Y | 物料名称 |
| Code | varchar(30) | Y | 料号 |
| Spec | varchar(800) | Y | 规格 |
| Model | varchar(300) | Y | 型号 |
| Color | varchar(100) | Y | 颜色 |
| Materials | varchar(100) | Y | 材料 |
| Trademark | varchar(100) | Y | 商标 |
| Unit | varchar(20) | Y | 单位 |
| MaterialType | nvarchar(10) | Y | 物料类型 |

#### 数量与价格
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Price | decimal(18,4) | Y | 单价 |
| Weight | decimal(18,9) | Y | 重量 |
| Volume | decimal(18,4) | Y | 材积 |

#### 组织与客户
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeID | int | Y | 组织ID |
| CustomerCode | varchar(60) | Y | 客户代码 |
| SystemID | int | Y | 系统ID（已不用） |
| ParentMaterialID | bigint | Y | 上级ID（已不用） |

#### 库存与排产
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| IsScheduling | bit | Y | 是否需要排产（默认 1） |
| FixedDay | int | Y | 提前期（需提前/延后天数，负=提前，正=延后） |
| SafetyStockQty | decimal(18,4) | Y | 安全库存 |
| SafetyStockPeriod | decimal(18,1) | Y | 安全库存周期 |
| InventoryMaxLimitQty | decimal(18,4) | Y | 安全库存上限 |
| InventoryMinLimitQty | decimal(18,4) | Y | 安全库存下限 |
| UsingMonth | decimal(18,1) | Y | 可用月 |

#### 包装与批量
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| TrayOfQty | int | Y | 每托盘数 |
| BoxOfQty | int | Y | 每箱数 |
| BoxOfTray | int | Y | 箱/拖 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | ERP 主键 ID，用于对接 |
| SyncDatetime | datetime | Y | 同步日期 |
| PCB | varchar(50) | Y | PCB 板料号 |

#### 扩展字段（Extend1 ~ Extend21）
| 字段名 | 类型 | 说明 |
|--------|------|------|
| Extend1 ~ Extend4 | varchar(50/500) | 扩展 1~4 |
| Extend5 ~ Extend21 | nvarchar(200) | 扩展 5~21 |

#### 审计与流程
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| ModifyedOn | datetime | Y | 修改日期 |
| WorkFlowInstanceID | varchar(20) | Y | 流程 ID |
| Status | int | Y | 状态：**0=草稿，1=确认**（全库约定；本表亦俗称启用/禁用，含义同上） |

#### 备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Remark1 | nvarchar(500) | Y | 备注 1 |
| Remark2 | nvarchar(500) | Y | 备注 2 |

### 1.3 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`
- `ParentMaterialID` → 本表物料层级（已不用）
- `ERPID` → 与 ERP 物料主数据对接
- `MaterialID` ← 被 `APS_Order.MaterialID`、`APS_PO.MaterialID`、`APS_MachineMouldMaterial.MaterialID`、`APS_MaterialBOM.MaterialID`、`APS_PR.MaterialID` 等引用

### 1.4 备注

- **已废弃字段**：`SystemID`、`ParentMaterialID`
- **默认值**：`IsScheduling = 1` 表示默认参与排产
- **提前期**：`FixedDay` 为负数表示提前加工天数

---

## 2. APS_Order（订单表）

销售/生产订单主表，存储订单基础信息、数量、交期、排产状态、包装出货及 ERP 对接字段。

### 2.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_Order` |
| 主键 | `OrderID` (bigint, 自增) |
| 默认值 | ProductionStatus=26, StockOutQty=0, CompletionStatus=0；（EK0721 另有 NoWorkHours 等） |

### 2.2 字段清单

#### 主键与标识
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrderID | bigint | NOT NULL | 订单ID（主键，自增） |

#### 订单基础信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrderNo | varchar(50) | Y | 订单号 |
| SourceOrderNo | varchar(300) | Y | 来源单号 |
| SourceOrderLineNo | varchar(10) | Y | 来源单行号 |
| OrderDate | datetime | Y | 下单日期 |
| OrderType | varchar(30) | Y | 单据类型 |
| WorkOrderTypeID | varchar(40) | Y | 订单类型 |
| Describe1 | nvarchar(100) | Y | 描述1 |
| LineNum | varchar(10) | Y | 行号 |

#### 组织与客户
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MFGOrganizeID | int | Y | 制造组织 |
| OwnOrganizeID | int | Y | 所属组织 |
| OrganizeID | bigint | Y | 组织ID（APS库） |
| CustomerID | bigint | Y | 客户ID |
| SystemID | int | Y | 系统ID（已经不用） |
| ControlID | varchar(20) | Y | 控制者代码 |
| SalesMan | nvarchar(30) | Y | 业务员 |
| SaleTo | nvarchar(100) | Y | 销售地 |

#### 物料与数量
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MaterialID | bigint | Y | 料品ID |
| Qty | decimal(18,4) | Y | 订单数量 |
| ProducedQty | decimal(18,4) | Y | 已报工数量/已生产 |
| CompletionQty | decimal(18,4) | Y | 已入库数量/已交货 |
| QualifiedQty | decimal(18,4) | Y | 合格数量 |
| StockOutQty | decimal(18,4) | Y | 出货数量（默认0，已经不用） |
| FeedingQty | decimal(18,4) | Y | 投料数 |
| OweProducedQty | decimal(18,4) | Y | 欠数 |
| NoInStockQty | int | Y | 未入库数 |
| UnPackagedQty | int | Y | 剩余包装数 |
| PackagedQty | int | Y | 已包装数 |

#### 排产相关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| TotalScheduingQty | decimal(18,4) | Y | 已排数量 |
| NoSehcduingQty | decimal(18,4) | Y | 未排数量 |
| PlanDate | datetime | Y | 排产日期（EK0721等有；APS库无） |
| PlanDateYMD | date | Y | 排产日期-年月日（部分环境） |
| APSDeliveryDate | datetime | Y | APS交货期（APS库有） |
| SalesPlanStatus | int | Y | 排产状态（0主计划 1月计划 2周计划 3日计划，部分环境） |
| SalesPlanID | bigint | Y | 主计划ID |
| ProcessGroupID | varchar(30) | Y | 工艺ID |
| DefaultLineID | int | Y | 默认产线ID（APS库） |
| Accounts | varchar(50) | Y | 计划员账号，多个用逗号隔开（APS库） |
| DataSource | nvarchar(10) | Y | 数据来源，默认ERP工单（APS库） |
| SplitQty | decimal(18,4) | Y | ERP订单数（APS库） |
| SourceOrderID | bigint | Y | 关联的生产订单ID（APS库） |
| ProcessGroupNames | nvarchar(50) | Y | 工艺组名称 |
| NoWorkHours | bit | NOT NULL | 不计算工时（默认0） |
| NoSchedulingWeek | bit | NOT NULL | 不参与周排产（默认0） |
| IsIndepenProcess | bit | NOT NULL | 独立工序（默认0） |
| ViewSort | int | Y | 显示排序 |
| FormRate | varchar(10) | Y | 齐套率（部分环境） |
| FormRate1 ~ FormRate6 | varchar(10) | Y | T1~T6齐套率（APS库） |
| FormRate7 | varchar(10) | Y | 计划齐套率 |
| FormRate8 | varchar(10) | Y | 库存齐套率 |
| FormRate9 | varchar(10) | Y | 库存+质检齐套率 |

#### 备料相关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| IsPrepare | int | Y | 是否备料 |
| PrepareDate | datetime | Y | 要求备料完成时间 |
| FactPrepareDate | datetime | Y | 实际备料时间 |
| ModifyPrepareDate | datetime | Y | 最后更新时间 |
| PrepareCreatedOn | datetime | Y | 备料计划创建时间 |
| PrepareCreatedBy | varchar(30) | Y | 备料创建账号 |
| PrepareCreatedByName | nvarchar(30) | Y | 备料创建姓名 |

#### 交期与出货
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| DeliveryDate | datetime | Y | 计划交期 |
| ShipmentDay | datetime | Y | 出货日期 |
| ActualDay | datetime | Y | 实际开拉期 |
| CompletionDate | datetime | Y | 完工日期 |
| RequestOutDatebyOrder | datetime | Y | 要货日期（针对返工/返修） |
| ContainerQty | int | Y | 货柜数 |

#### 价格与币种
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Price | decimal(18,4) | Y | 单价 |
| Currency | varchar(10) | Y | 币种 |
| ExchangeRate | decimal(18,4) | Y | 汇率 |
| WGT | decimal(18,4) | Y | 重量 |
| Volume | decimal(18,4) | Y | 材积 |

#### ERP 对接
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | ERP 主键ID |
| ERPStartDate | datetime | Y | ERP 开拉日期 |
| ERPEndDate | datetime | Y | ERP 结束日期 |
| SalesOrderDetailID | varchar(20) | Y | 销售订单明细ID |

#### 状态字段
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProductionStatus | int | Y | 生产状态（默认26） |
| CompletionStatus | bit | NOT NULL | 是否完工（默认0） |
| PickingStatus | nvarchar(10) | Y | 领料状态 |
| Status | int | Y | 状态：**0=草稿，1=确认**（全库约定；亦作启用/禁用表述） |
| EmergencyName | nvarchar(300) | Y | 紧急名称 |

#### 层级与批次
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ParentOrderID | bigint | Y | 上级订单ID |
| GroupCabinet | varchar(50) | Y | 柜组名称（已废弃） |

#### 分期数量（Q1~Q9）
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Q1 ~ Q9 | decimal(18,4) | Y | 分期数量 1~9（APS库；部分环境有Q10~Q12） |

#### 扩展字段（Extend1 ~ Extend21）
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Extend1 ~ Extend6 | nvarchar(200) | Y | 扩展 1~6 |
| Extend7 | nvarchar(200) | Y | 是否点检完成 |
| Extend8 | nvarchar(200) | Y | 是否释放备料计划 |
| Extend9 | nvarchar(200) | Y | ERP组织代码 |
| Extend10 ~ Extend11 | nvarchar(200/4000) | Y | 扩展10~11（Extend11大文本） |
| Extend12 | nvarchar(200) | Y | 一般存储工厂代码 |
| Extend13 ~ Extend21 | nvarchar(200) | Y | 扩展13~21 |

#### 审计与流程
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(30) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| WorkFlowInstanceID | varchar(20) | Y | 流程ID |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 2.3 关联关系

- `MaterialID` → `APS_Material.MaterialID`
- `CustomerID` → 客户表（表名待确认）
- `MFGOrganizeID`、`OwnOrganizeID` → `Dev_Organize.OrganizeID`
- `SalesOrderDetailID` → `APS_SalesOrderDetail.SalesOrderDetailID`（生产订单来源于销售订单行）
- `OrderID` ← 被 `APS_OrderBOM.OrderID`、`APS_OrderProcess.OrderID`、`APS_OrderPlan.OrderID`、`APS_ProcessPartPlan.OrderID`、`APS_ProcessPlan.OrderID`、`APS_DayPlan.OrderID`、`APS_PO.OrderID` 引用
- `ParentOrderID` → 本表（上级订单）
- `SalesPlanID` → 主计划表（表名待确认）
- `ProcessGroupID` → `APS_ProcessGroup.ProcessGroupID`（工艺表）
- `CreatedBy`、`ModifiedBy` → `Dev_Account.Account`（创建/修改账号）

### 2.4 备注

- **版本差异**：APS 库无 PlanDate/PlanDateYMD，有 APSDeliveryDate、FormRate1~9、Accounts、DataSource、OrganizeID、DefaultLineID、SplitQty、SourceOrderID 等；EK0721 有 PlanDate、备料相关字段
- **ProductionStatus**：26=待排，25=完成，21=已排未完成
- **Extend4**：ERP 订单状态（存中文）；**Extend9**：ERP 组织代码
- **已废弃字段**：`GroupCabinet`（柜组名称）
- **SalesPlanStatus**：0=主计划，1=月计划，2=周计划，3=日计划
- **ProductionStatus**：默认 26，表示初始生产状态
- **字段拼写**：`NoSehcduingQty` 为未排数量（注意拼写）
- **与销售订单**：`APS_Order.SalesOrderDetailID` → `APS_SalesOrderDetail.SalesOrderDetailID`

---

## 3. Dev_Organize（组织表）

组织/制造单元主数据表，存储生产线、车间等组织的层级结构、人力、效率、班制及排产参数。

### 3.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `Dev_Organize` |
| 主键 | `OrganizeID` (int, 自增) |
| 默认值 | LineCount=1，ReportDays=1 |

### 3.2 字段清单

#### 主键与标识
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeID | int | NOT NULL | 组织ID（主键，自增） |

#### 基础信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeName | nvarchar(200) | Y | 组织名称 |
| OrganizeTypeID | int | Y | 组织类型ID |
| ParentID | int | Y | 上级ID |
| OrganizeIDs | varchar(50) | Y | 本组织所包含的上级组织ID |
| OrganizeSort | varchar(20) | Y | 组织排序 |
| ViewSort | int | Y | 排序 |
| Area | nvarchar(10) | Y | 区域 |
| GroupName | int | Y | 基地 |

#### 人力与负责人
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| TotalPeoples | int | Y | 总人数 |
| ManagerCode | varchar(20) | Y | 负责人 |
| HRCode | varchar(50) | Y | 对接系统编码 |
| HRCore | varchar(50) | Y | 人力核心编码 |
| LeavePeoples | int | Y | 请假人数 |
| AbsentPeoples | int | Y | 旷工人数 |
| RestPeoples | int | Y | 休息人数 |
| LeaveTimes | decimal(10,2) | Y | 请假时长 |

#### 排产相关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| LineCount | int | NOT NULL | 拉线数量（默认1） |
| SchedulingDays | int | Y | 排产天数 |
| SchedulingType | nvarchar(10) | Y | 排产类型 |
| EffectiveDate | datetime | Y | 生效日期 |
| LeadDay | int | Y | 提前期天数 |
| WorkingTimesID | varchar(20) | Y | 班别ID |
| Sailings | int | Y | 班次 |
| WorkShift | nvarchar(10) | Y | 班制 |
| ReportDays | int | NOT NULL | 报工天数（默认1） |

#### 效率与产能
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrgEfficiency | decimal(18,2) | Y | 效率 |
| AttendanceDate | datetime | Y | 出勤日期 |

#### ERP 对接
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPOrderCode | varchar(10) | Y | ERP 制造组织单别 |
| ERPOrganizeName | nvarchar(50) | Y | ERP 组织名称 |
| ERPID | varchar(50) | Y | ERP 主键ID |
| OrderSource | varchar(50) | Y | 订单来源 |

#### 审计与流程
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| WorkFlowInstanceID | varchar(20) | Y | 流程ID |
| Status | int | Y | 状态 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |
| Remark3 | nvarchar(50) | Y | 备注3 |

### 3.3 关联关系

- `OrganizeID` ← 被 `APS_Material.OrganizeID`、`Dev_Account.OrganizeID` 引用
- `OrganizeID` ← 被 `APS_Order.MFGOrganizeID`、`APS_Order.OwnOrganizeID` 引用
- `ParentID` → 本表（上级组织）
- `OrganizeTypeID` → 组织类型表（表名待确认）
- `WorkingTimesID` → `APS_WorkingTimes.WorkingTimesID`（班别）

### 3.4 备注

- **层级结构**：`ParentID` 支持组织树形结构，`OrganizeIDs` 存储上级组织ID路径
- **LineCount**：拉线数量，表示该组织下的产线数量
- **GroupName**：类型为 int，用于基地/分组标识

---

## 4. APS_SalesOrder（销售订单主表）

销售订单主表，存储销售订单头信息，与 APS_Order 明细对应（一单多行）。

### 4.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_SalesOrder` |
| 主键 | `SalesOrderID` (varchar(20), 非自增) |
| 默认值 | ProductionStatus = 26 |

### 4.2 字段清单

#### 主键与标识
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SalesOrderID | varchar(20) | NOT NULL | 销售订单ID（主键） |
| SalesOrderNo | varchar(30) | Y | 销售订单号 |
| SrcOrderNo | varchar(50) | Y | 来源单号 |

#### 订单信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrderDate | datetime | Y | 下单日期 |
| DemandType | bigint | Y | 需求分类号 |
| WorkOrderTypeID | varchar(40) | Y | 订单类型 |

#### 组织与客户
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeID | int | Y | 组织ID |
| CustomerID | bigint | Y | 客户ID |

#### 销售信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SalesDept | nvarchar(50) | Y | 销售部门 |
| SalesDeptGroup | nvarchar(50) | Y | 销售组 |
| SalesMan | nvarchar(10) | Y | 销售员/业务员 |

#### 状态
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| OrderStatus | nvarchar(10) | Y | 单据状态 |
| CloseStatus | nvarchar(10) | Y | 关闭状态 |
| ProductionStatus | int | Y | 生产状态（默认26） |
| CompletionDate | datetime | Y | 完工日期 |
| SyncDatetime | datetime | Y | 同步时间 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 4.3 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`
- `CustomerID` → 客户表（表名待确认）
- `SalesOrderID` ← 被 `APS_SalesOrderDetail.SalesOrderID` 引用

### 4.4 备注

- **主键类型**：`SalesOrderID` 为 varchar，通常来自 ERP 或外部系统

---

## 5. APS_SalesOrderDetail（销售订单行表）

销售订单明细表，存储销售订单行信息（料品、数量、交期、备料等），与 APS_SalesOrder 头表对应，是 APS_Order 的来源。

### 5.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_SalesOrderDetail` |
| 主键 | `SalesOrderDetailID` (varchar(20), 非自增) |
| 默认值 | ProductionStatus=26, StockOutQty=0, StockInQty=0, IsSetReply=0, SalesPrepareStatus=0 |

### 5.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SalesOrderDetailID | varchar(20) | NOT NULL | 销售订单行ID（主键） |
| SalesOrderID | varchar(20) | NOT NULL | 销售订单ID（→ APS_SalesOrder） |
| LineNum | varchar(10) | Y | 行号 |

#### 物料与数量
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MaterialID | bigint | Y | 料品ID |
| Qty | decimal(18,4) | Y | 数量 |
| DemandQty | decimal(18,6) | Y | 需求数 |
| ProducedQty | decimal(18,4) | Y | 已生产 |
| QualifiedQty | decimal(18,4) | Y | 合格数 |
| CanStockIn | decimal(18,4) | Y | 在制数 |
| StockOutQty | decimal(18,4) | Y | 出货数量（默认0） |
| StockInQty | decimal(18,4) | Y | 入库数量（默认0） |
| StockQtyAllocation | decimal(18,4) | Y | 库存分配数 |
| SalesReturnQty | decimal(18,4) | Y | 累计退货 |
| TransQty | decimal(18,4) | Y | 累计调拨数 |
| MONo | varchar(4000) | Y | MO（制令单号） |

#### 价格
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Price | decimal(18,4) | Y | 单价 |
| FARAMOUNT | decimal(18,4) | Y | 应收金额 |
| TaxPrice | decimal(18,4) | Y | 含税单价（部分环境） |
| TaxRate | decimal(18,4) | Y | 税率（部分环境） |

#### 交期与日期
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrderDate | datetime | Y | 下单日期 |
| DeliveryDate | datetime | Y | 计划交期 |
| PCDeliveryDate | datetime | Y | 成品交期 |
| ActualDeliveryDate | date | Y | 实际交货日期 |
| ActualKittingDate | date | Y | 实际齐料日期 |
| CompletionDate | datetime | Y | 完工日期 |
| ProductionStatusDate | datetime | Y | 生产状态日期 |
| SetReplyDate | datetime | Y | 下发日期 |
| DemandReplyDate | datetime | Y | 要求回复日期 |
| PrepareDate | datetime | Y | 备料日期 |
| PrepareDatePCB | datetime | Y | PCB 备料日期 |
| APSDeliveryReplyStartDate | datetime | Y | 评估开始日期 |
| APSDeliveryReplyEndDate | datetime | Y | 评估结束日期 |

#### 组织与销售
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MFGOrganizeID | int | Y | 制造组织 |
| OrganizeID | int | Y | 组织ID |
| OrganizeID1 | int | Y | 组织ID1（备用） |
| SaleTo | nvarchar(50) | Y | 销往 |
| CustomerOrder | nvarchar(50) | Y | 客户订单号 |
| CustomerMaterialNo | varchar(200) | Y | 客户物料编码（部分环境） |
| CustomerMaterialName | nvarchar(200) | Y | 客户物料名称（部分环境） |

#### 状态
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| ProductionStatus | int | Y | 生产状态（默认26） |
| IsSetReply | int | Y | 是否下发（默认0） |
| SalesPrepareStatus | int | Y | 是否下发/备料状态-组装（默认0） |
| SalesPrepareStatusPCB | int | Y | 是否下发备料状态-PCB |
| StockStatus | nvarchar(10) | Y | 库存状态 |
| DocumentStatus | nvarchar(10) | Y | 单据状态 |
| BusinessClose | nvarchar(10) | Y | 业务关闭 |

#### 分期/包装数量（Q1~Q10，部分环境）
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Q1 | decimal(18,4) | Y | 个箱 |
| Q2 | decimal(18,4) | Y | 总毛重 |
| Q3 | decimal(18,4) | Y | 总净重 |
| Q4 | decimal(18,4) | Y | 总体积 |
| Q5 ~ Q10 | decimal(18,4) | Y | 分期数量 5~10 |

#### BOM 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| BOMID | varchar(20) | Y | BOMID |
| BOMVersion | varchar(100) | Y | BOM 版本 |
| SyncDatetime | datetime | Y | 同步时间 |
| SyncDatetime1 | datetime | Y | 同步日期1（仅部分环境） |
| FBILLTYPEID | varchar(40) | Y | 单别ID（部分环境） |
| PMCRemark | nvarchar(500) | Y | PMC 备注（部分环境） |

#### 扩展字段
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Extend3 ~ Extend7 | nvarchar(200) | Y | 扩展 3~7（Extend7=是否点检完成；部分环境有 Extend1/2/8~21） |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(30) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 5.3 关联关系

- `SalesOrderID` → `APS_SalesOrder.SalesOrderID`
- `MaterialID` → `APS_Material.MaterialID`
- `MFGOrganizeID`、`OrganizeID`、`OrganizeID1` → `Dev_Organize.OrganizeID`
- `SalesOrderDetailID` ← 被 `APS_Order.SalesOrderDetailID`、`APS_SalesOrderReturn.SalesOrderDetailID`（退货行）引用

### 5.4 备注

- **数据流**：APS_SalesOrder（头）→ APS_SalesOrderDetail（行）→ APS_Order（生产订单）
- **Q1~Q4**：部分环境中用于包装/物流（个箱、总毛重、总净重、总体积）
- **CanStockIn**：在制数（WIP quantity）
- **MONo**：制令单号，varchar(4000) 可存多个 MO
- **版本差异**：不同数据库（APS、EK0721、APS20260323）字段有差异：
  - **APS**：较简版，无备料/下发相关（IsSetReply、SetReplyDate、PrepareDate、SalesPrepareStatus 等）、无 Extend，部分 APS 环境有 SyncDatetime1
  - **EK0721**：有 Q1~Q10、CustomerMaterialNo/Name、TaxPrice/TaxRate、Extend1~21
  - **APS20260323**：有 FARAMOUNT、MONo、QualifiedQty、CanStockIn、OrganizeID、OrganizeID1 等

---

## 6. APS_OrderBOM（生产订单用料清单）

生产订单 BOM 明细表，存储生产订单的用料清单（子件料品、用量、领料状态等）。

### 6.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_OrderBOM` |
| 主键 | `OrderBOMID` (bigint, 自增) |

### 6.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrderBOMID | bigint | NOT NULL | 主键，自增 |
| OrderID | bigint | NOT NULL | 订单ID |
| MaterialID | bigint | Y | 料品ID（子件） |
| LineNum | varchar(10) | Y | 行号 |
| ItemNo | varchar(10) | Y | 料号 |

#### 数量相关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| DemandQty | decimal(24,6) | Y | 需求数 |
| QPA | decimal(24,6) | Y | 用量（单耗） |
| Molecule | decimal(24,6) | Y | 分子 |
| Denominator | decimal(24,6) | Y | 分母 |
| IssuedQty | decimal(24,6) | Y | 已领用量 |
| UnIssuedQty | decimal(24,6) | Y | 未领用量 |
| RcvQty | decimal(24,6) | Y | 库存数量 |
| OnloadQty | decimal(24,6) | Y | 在途数量 |
| OncheckQty | decimal(24,6) | Y | 质检数量 |
| OweQty | decimal(24,6) | Y | 欠数 |
| NoSehcduingQty | decimal(24,6) | Y | 未排数量 |
| Progress | decimal(24,6) | Y | 进度 |

#### 发料相关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| IssueStatus | nvarchar(50) | Y | 发料状态 |
| IssueType | nchar(10) | Y | 发料方式 |
| IssueDate | datetime | Y | 发料日期 |
| HairFeed | bit | Y | 是否发料 |
| SupplyWh | varchar(50) | Y | 库位ID |

#### 物料属性与 ERP
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Extend1 | varchar(50) | Y | 物料属性（自制、采购、虚拟） |
| Unit | varchar(20) | Y | 单位 |
| RGEKZ | varchar(10) | Y | 反冲标识 |
| RSPOS | varchar(30) | Y | 预留项目号 |
| XLOEK | varchar(10) | Y | 删除标识 |
| ERPID | varchar(30) | Y | ERP 主键ID |
| SyncDatetime | datetime | Y | 同步日期 |

#### 其他
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| POTracker | nvarchar(20) | Y | 物控 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Status | int | Y | 状态 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 6.3 关联关系

- `OrderID` → `APS_Order.OrderID`
- `MaterialID` → `APS_Material.MaterialID`

### 6.4 备注

- **Molecule/Denominator**：用量比例，QPA = 分子/分母 × 订单数量
- **Extend1**：物料属性，取值为自制、采购、虚拟
- **NoSehcduingQty**：字段拼写与 APS_Order 一致（未排数量）

---

## 7. APS_OrderProcess（生产订单工序表）

生产订单工序明细表，存储每个生产订单的工序信息（报工、排产、上下工序、产能等）。

### 7.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_OrderProcess` |
| 主键 | `ID` (bigint, 自增) |

### 7.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ID | bigint | NOT NULL | 主键，自增 |
| ProcessID | varchar(20) | NOT NULL | 工序ID |
| OrderID | bigint | Y | 订单ID |
| MaterialID | bigint | Y | 产品ID |
| ProcessGroupID | varchar(20) | Y | 工艺ID |
| ProcessGroupInfoID | varchar(20) | Y | 工艺工序关联ID → APS_ProcessGroupInfo |

#### 工序信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProcessName | nvarchar(100) | NOT NULL | 工序名称 |
| ProcessPriority | int | Y | 工序顺序 |
| Unit | varchar(20) | Y | 单位 |
| STEUS | nvarchar(30) | Y | 控制码（SAP） |
| WERKS | varchar(10) | Y | 工厂代码 |

#### 上下工序
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PreProcessID | varchar(20) | Y | 上工序ID |
| PreProcessName | nvarchar(30) | Y | 上工序名称 |
| PreStartDate | datetime | Y | 上工序开拉期 |
| PreEndDate | datetime | Y | 上工序结束日期 |
| PreProducedQty | decimal(18,4) | Y | 上工序完成数 |
| NextProcessID | varchar(20) | Y | 下工序ID |
| NextProcessName | nvarchar(30) | Y | 下工序名称 |
| NextStartDate | datetime | Y | 下工序开拉期 |
| NextEndDate | datetime | Y | 下工序结束日期 |
| NextProducedQty | decimal(18,4) | Y | 下工序报工数 |

#### 数量相关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| DemandQty | decimal(18,4) | Y | 需求数量 |
| ProducedQty | decimal(18,4) | Y | 报工数 |
| ConfirmQty | decimal(18,4) | Y | 确认数量 |
| BadQty | decimal(18,4) | Y | 报废数 |
| OweQty | decimal(18,4) | Y | 欠数 |
| SchedulingQty | decimal(18,4) | Y | 已排数量 |
| WIP | decimal(18,4) | Y | 在制 |

#### 排产与日期
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| StartDate | datetime | Y | 开始日期 |
| EndDate | datetime | Y | 结束日期 |
| IsScheduling | bit | Y | 是否排产 |
| IsProduction | bit | Y | 是否报产 |

#### 产能与组织
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeID | int | Y | 组织ID |
| Manager | nvarchar(20) | Y | 负责人 |
| WorkCenter | varchar(50) | Y | 工作中心 |
| Capacity | decimal(18,2) | Y | 每小时产能 |
| StandardPeoples | int | Y | 标准人数 |
| Seconds | int | Y | 单台用时（秒） |
| MachineSeconds | int | Y | 机器工时 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SyncDatetime | datetime | Y | 同步日期 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Status | int | Y | 状态 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 7.3 关联关系

- `OrderID` → `APS_Order.OrderID`
- `MaterialID` → `APS_Material.MaterialID`
- `OrganizeID` → `Dev_Organize.OrganizeID`
- `ProcessGroupID` → `APS_ProcessGroup.ProcessGroupID`（工艺表）
- `ProcessID` → `APS_Process.ProcessID`（工序主数据）

### 7.4 备注

- **ProcessGroupInfoID**：→ APS_ProcessGroupInfo（工艺工序关联），部分场景用 ProcessID 直接关联
- **工序链**：PreProcessID/NextProcessID 表示上/下工序关系

---

## 8. APS_ProcessGroup（工艺表）

工艺/工艺路线组主数据表，存储工艺ID、名称及组织关联。

### 8.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_ProcessGroup` |
| 主键 | `ProcessGroupID` (varchar(20), 非自增) |
| 默认值 | SchedulingSeq = 0 |

### 8.2 字段清单

#### 主键与基础信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProcessGroupID | varchar(20) | NOT NULL | 工艺ID（主键） |
| ProcessGroupName | nvarchar(200) | Y | 工艺名称 |
| OrganizeID | int | Y | 组织ID |

#### 排产相关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ByRegion | bit | Y | 按区域排产（已不用） |
| SchedulingSeq | varchar(1) | Y | 排程顺序，0正排1倒排（已不用，默认0） |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SyncDatetime | datetime | Y | 同步日期 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Status | int | Y | 状态 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 8.3 关联关系

- `ProcessGroupID` ← 被 `APS_Order.ProcessGroupID`、`APS_OrderProcess.ProcessGroupID`、`APS_ProcessGroupInfo.ProcessGroupID`、`APS_ProcessGroupMaterial.ProcessGroupID` 引用
- `OrganizeID` → `Dev_Organize.OrganizeID`

### 8.4 备注

- **已废弃字段**：`ByRegion`、`SchedulingSeq`

---

## 9. APS_Process（工序表）

工序主数据表，存储工序ID、名称、排产类型、负责人等基础信息。

### 9.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_Process` |
| 主键 | `ProcessID` (varchar(20), 非自增) |

### 9.2 字段清单

#### 主键与基础信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProcessID | varchar(20) | NOT NULL | 工序ID（主键） |
| ProcessName | nvarchar(30) | NOT NULL | 工序名称 |
| Unit | nvarchar(10) | Y | 单位 |
| ProcessPartName | nvarchar(50) | Y | 工段 → APS_ProcessPartName |
| Requirement | nvarchar(500) | Y | 工序要求 |

#### 组织与排产
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeID | int | Y | 组织ID |
| Manager | nvarchar(20) | Y | 负责人 |
| SchedulingType | nvarchar(20) | Y | 排产类型 |
| Priority | int | Y | 优先级 |
| TransferTime | int | Y | 转线时间 |

#### 其他
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| WorkOrderTypeID | varchar(20) | Y | 订单单别 |
| PWSProcessID | varchar(20) | Y | 计件工序 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Status | int | Y | 状态 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 9.3 关联关系

- `ProcessID` ← 被 `APS_OrderProcess.ProcessID`、`APS_ProcessPlan.ProcessID`、`APS_ProcessPosition.ProcessID`、`Dev_PositionLevel.PorcessID` 引用
- `OrganizeID` → `Dev_Organize.OrganizeID`
- `ProcessPartName` → `APS_ProcessPartName.ProcessPartName`（工段主数据）

### 9.4 备注

- **APS_Process**：工序主数据；**APS_OrderProcess**：订单工序实例（含报工、排产数据）

---

## 10. APS_ProcessGroupInfo（工艺工序关联表）

工艺与工序的关联表，定义工艺路线中的工序组成、顺序、是否排产/报产等。

### 10.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_ProcessGroupInfo` |
| 主键 | `ProcessGroupInfoID` (varchar(20), 非自增) |

### 10.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProcessGroupInfoID | varchar(20) | NOT NULL | 主键 |
| ProcessGroupID | varchar(20) | Y | 工艺ID |
| ProcessID | varchar(20) | Y | 工序ID |
| PostProcessID | varchar(20) | Y | 自动后工序关联 |
| ProductionProcessID | varchar(20) | Y | 报产来源 |
| OrganizeID | int | Y | 组织ID |

#### 排产与报产
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| IsScheduling | bit | Y | 是否排产 |
| IsProduction | bit | Y | 是否报产 |
| IsAcquisition | bit | Y | 是否采集 |
| AcquisitionTimes | int | Y | 采集次数 |
| IsPerson | bit | Y | 记录报产人 |
| OutputType | nvarchar(10) | Y | 报产方式 |
| ProcessingTimes | int | Y | 加工次数 |

#### 顺序与约束
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProcessPriority | int | Y | 顺序 |
| FixDay | int | Y | 提前期 |
| PostConstraint | varchar(30) | Y | 后段约束 |
| IsEnd | bit | Y | 工段结束 |
| ProcessEnd | bit | Y | 工艺结束 |

#### 审计与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Status | int | Y | 状态 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |
| SyncDatetime | datetime | Y | 同步日期 |

### 10.3 关联关系

- `ProcessGroupInfoID` ← 被 `APS_OrderProcess.ProcessGroupInfoID`、`APS_MaterialBOM.ProcessGroupInfoID` 引用
- `ProcessGroupID` → `APS_ProcessGroup.ProcessGroupID`
- `ProcessID` → `APS_Process.ProcessID`
- `OrganizeID` → `Dev_Organize.OrganizeID`
- `PostProcessID` → `APS_Process.ProcessID`（后工序的工序ID）

### 10.4 备注

- **工艺路线**：APS_ProcessGroup（工艺）→ APS_ProcessGroupInfo（工艺工序关联）→ APS_Process（工序）
- **ProcessPriority**：同一工艺下各工序的先后顺序

---

## 11. APS_ProcessGroupMaterial（产品工艺表）

产品与工艺的关联表，定义料品（产品）使用哪条工艺路线。

### 11.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_ProcessGroupMaterial` |
| 主键 | `ProcessGroupMaterialID` (varchar(20), 非自增) |

### 11.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProcessGroupMaterialID | varchar(20) | NOT NULL | 主键 |
| ProcessGroupID | varchar(20) | Y | 工艺ID |
| MaterialID | bigint | Y | 物料ID |
| OrganizeID | int | Y | 组织ID |

#### 审计与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Status | int | Y | 状态 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |
| SyncDatetime | datetime | Y | 同步日期 |

### 11.3 关联关系

- `ProcessGroupID` → `APS_ProcessGroup.ProcessGroupID`
- `MaterialID` → `APS_Material.MaterialID`
- `OrganizeID` → `Dev_Organize.OrganizeID`

### 11.4 备注

- **产品工艺**：料品（产品）→ APS_ProcessGroupMaterial → 工艺路线（APS_ProcessGroup）

---

## 12. Dev_Account（账号表）

用户/账号主数据表，存储账号、姓名、组织、职位等信息。

### 12.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `Dev_Account` |
| 主键 | `Account` (varchar(20), 非自增) |

### 12.2 字段清单

#### 主键与基础信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Account | varchar(20) | NOT NULL | 账号（主键） |
| Pwd | varchar(50) | NOT NULL | 密码 |
| Name | nvarchar(30) | Y | 姓名 |
| NickName | nvarchar(30) | Y | 昵称 |
| CardNo | varchar(20) | Y | 卡号 |
| Sex | nvarchar(30) | Y | 性别 |
| Tel | varchar(30) | Y | 电话 |
| Email | varchar(50) | Y | 邮箱 |

#### 组织与职位
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeID | int | NOT NULL | 组织ID |
| OrganizeName | varchar(50) | Y | 组织名称 |
| PositionID | int | Y | 职位ID |
| LeadUserCode | varchar(20) | Y | 上级账号 |

#### 负责人相关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| DepartmentMan | varchar(30) | Y | 部门负责人 |
| DepartmentMan2 | varchar(30) | Y | 部门负责人2 |
| CMOMan | varchar(30) | Y | 分管总经理 |
| CenterMan | varchar(30) | Y | 中心负责人 |

#### 在职与类型
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| EntryDate | datetime | Y | 入职日期 |
| LeaveDate | datetime | Y | 离职日期 |
| UserType | int | Y | 人员类型 |
| UserAttr | int | Y | 人员属性 |

#### 扩展与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Extend1 ~ Extend9 | nvarchar(200) | Y | 扩展 1~9 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| WorkFlowInstanceID | varchar(20) | Y | 流程ID |
| Status | int | Y | 状态 |

### 12.3 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`
- `LeadUserCode` → `Dev_Account.Account`（上级账号，自关联）
- `Account` ← 被 `APS_Order.Accounts`（计划员账号，逗号分隔多账号）、`APS_ProcessPosition.Account`、`Dev_PositionAccountMap.Account`、`Dev_PositionExamRecord.Account` 引用

### 12.4 备注

- **OrganizeID**：本表为 NOT NULL，账号需归属组织
- **Accounts**：APS_Order.Accounts 存计划员账号，多个用逗号隔开

---

## 13. APS_OrderPlan（排产主表）

排产主表，与 APS_Order 一对一。订单一旦被排产，就会在此表生成一条记录，用于表示「已排产」。

### 13.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_OrderPlan` |
| 主键 | `FirstPlanID` (bigint, 自增) |
| 默认值 | SchedulingQty = 0 |

### 13.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| FirstPlanID | bigint | NOT NULL | 主键，自增 |
| OrderID | bigint | Y | 订单ID |
| MaterialID | bigint | Y | 料品ID |

#### 数量相关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SchedulingQty | decimal(18,4) | Y | 计划数量（默认0） |
| FirstCompleteQty | int | Y | 首次完成数 |
| HasQty | decimal(18,4) | Y | 已报工（已不用） |
| AwaitQty | decimal(18,4) | Y | 待分配数（已不用） |
| PlanQty | decimal(18,4) | Y | 已分配数（已不用） |
| FormQty | int | Y | 齐套数（已不用） |
| ExpectTime | decimal(18,2) | Y | 预期时间 |

#### 日期与批次
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| StartDate | datetime | Y | 开始日期（已不用） |
| EndDate | datetime | Y | 结束日期（已不用） |
| BatchNo | varchar(10) | Y | 批次号（已不用） |
| ComputingTime | datetime | Y | 齐套计算日期 |

#### ERP 与发料
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| IssueType | nvarchar(50) | Y | 发料方式（整单发料、部分发料） |
| ERPID | varchar(30) | Y | ERPID |
| ERPDocNo | varchar(50) | Y | ERP单号（已不用） |
| SyncDatetime | datetime | Y | 同步日期 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 13.3 关联关系

- `OrderID` → `APS_Order.OrderID`
- `MaterialID` → `APS_Material.MaterialID`
- `FirstPlanID` ← 被 `APS_ProcessPartPlan.FirstPlanID`、`APS_ProcessPlan.FirstPlanID` 引用（工段/工序计划）

### 13.4 备注

- **与 APS_Order**：一对一，表示该订单已排产
- **已废弃字段**：HasQty、AwaitQty、PlanQty、FormQty、StartDate、EndDate、BatchNo、ERPDocNo

---

## 14. APS_ProcessPartName（工段表）

工段主数据表，存储工段名称、负责人、排产类型等。

### 14.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_ProcessPartName` |
| 主键 | `ProcessPartName` (nvarchar(30), 非自增) |

### 14.2 字段清单

#### 主键与基础信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProcessPartName | nvarchar(30) | NOT NULL | 工段名称（主键） |
| OrganizeID | int | Y | 组织ID |
| Manager | nvarchar(20) | Y | 负责人 |
| SchedulingType | nvarchar(20) | Y | 排产类型 |
| WorkOrderTypeID | varchar(20) | Y | 订单单别 |

#### 审计与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Status | int | Y | 状态 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |
| SyncDatetime | datetime | Y | 同步日期 |

### 14.3 关联关系

- `ProcessPartName` ← 被 `APS_Process.ProcessPartName`、`APS_ProcessPartPlan.ProcessPartName` 引用
- `OrganizeID` → `Dev_Organize.OrganizeID`

### 14.4 备注

- **APS_Process.ProcessPartName** 为 nvarchar(50)，本表主键为 nvarchar(30)，存工段名称时可关联

---

## 15. APS_ProcessPartPlan（工段计划表）

工段计划明细表，按工段存储排产计划（计划数、开始/完成日期等）。

### 15.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_ProcessPartPlan` |
| 主键 | `ProcessPartID` (bigint, 自增) |

### 15.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProcessPartID | bigint | NOT NULL | 工段计划ID（主键） |
| OrderID | bigint | Y | 订单ID |
| FirstPlanID | bigint | Y | 预排ID |
| MaterialID | bigint | Y | 料品ID |
| WorkShopID | bigint | Y | 车间ID/组织ID（已不用） |
| ProcessPartName | nvarchar(30) | Y | 工段名称 |
| DocNo | varchar(30) | Y | 单号（一般不使用） |

#### 数量与日期
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PlanQty | decimal(18,4) | Y | 计划数 |
| AwaitQty | decimal(18,4) | Y | 待分配数 |
| HasQty | decimal(18,4) | Y | 已完成数（已不用） |
| StartDate | datetime | Y | 开始日期 |
| EndDate | datetime | Y | 完成日期 |
| ExpectTime | decimal(18,2) | Y | 预计用时 |
| PrepareDate | datetime | Y | 发料日期 |

#### ERP 与扩展
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(400) | Y | ERPID |
| ERPDocNo | varchar(400) | Y | ERP单号 |
| WMSDocNo | varchar(400) | Y | WMS单号 |
| SyncDatetime | datetime | Y | 同步日期 |
| Extend1 ~ Extend9 | varchar/nvarchar | Y | 扩展 1~9 |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProductionStatus | int | Y | 生产状态 |
| Status | int | Y | 状态 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 15.3 关联关系

- `OrderID` → `APS_Order.OrderID`
- `FirstPlanID` → `APS_OrderPlan.FirstPlanID`（预排主表）
- `MaterialID` → `APS_Material.MaterialID`
- `ProcessPartName` → `APS_ProcessPartName.ProcessPartName`（工段主数据）

### 15.4 备注

- **排产层级**：APS_OrderPlan（排产主表）→ APS_ProcessPartPlan（工段计划明细）→ APS_ProcessPlan（工序计划明细）
- **已废弃**：WorkShopID、HasQty、DocNo（一般不使用）
- `ProcessPartID` ← 被 `APS_ProcessPlan.ProcessPartID` 引用

---

## 16. APS_ProcessPlan（工序计划表）

工序计划明细表，按工序存储排产计划（计划数、产能、开始/结束日期、机台等）。层级为：工段计划 → 工序计划。

### 16.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_ProcessPlan` |
| 主键 | `ProcessPlanID` (bigint, 自增) |
| 默认值 | ERPUpdateStatus = 0, HasQty1 = 0 |

### 16.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProcessPlanID | bigint | NOT NULL | 工序计划ID（主键） |
| ProcessPartID | bigint | NOT NULL | 工段计划ID |
| FirstPlanID | bigint | Y | 订单计划ID |
| OrderID | bigint | Y | 订单ID |
| MaterialID | bigint | Y | 料品ID |
| ProcessID | varchar(20) | Y | 工序ID |
| ProcessGroupInfoID | varchar(20) | Y | 工艺关联ID（一般不使用） |
| WorkShopID | bigint | Y | 排产车间ID，关联 Dev_Organize.OrganizeID |
| LineID | bigint | Y | 排产线别ID，关联 Dev_Organize.OrganizeID |
| MachineID | varchar(20) | Y | 机台ID → APS_Machine.MachineID |
| MachineMouldID | varchar(20) | Y | 机台模具ID → APS_MachineMould.MachineMouldID |

#### 数量与产能
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PlanQty | decimal(18,4) | Y | 计划数 |
| HasQty | decimal(18,4) | Y | 报工数 |
| HasQty1 | decimal(18,4) | Y | 昨日累计完成数（默认0） |
| Capacity | decimal(18,4) | Y | 每小时产能 |
| Coefficient | decimal(18,1) | Y | 效率系数 |
| TotalPeoples | int | Y | 总人数 |

#### 日期与用时
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| StartDate | datetime | Y | 开始日期 |
| EndDate | datetime | Y | 结束日期 |
| ExpectTime | decimal(18,2) | Y | 用时 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 状态与排产控制
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProductionStatus | int | Y | 生产状态（26待下达，21已排产，25已完成） |
| Status | int | Y | 状态 |
| NoWorkHours | bit | Y | 忽略工时 |
| NoScheduling | bit | Y | 不排 |
| ERPUpdateStatus | bit | Y | ERP更新状态（默认0） |
| ViewSort | int | Y | 顺序 |

#### 扩展字段
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Extend1 | nvarchar(50) | Y | 计划来源（自动计划、手工计划、后工序计划） |
| Extend2 | nvarchar(50) | Y | 待重排，空 |
| Extend3 | nvarchar(50) | Y | 扩展3 |
| Extend4 | nvarchar(50) | Y | 扩展4 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 16.3 关联关系

- `ProcessPartID` → `APS_ProcessPartPlan.ProcessPartID`（工段计划）
- `FirstPlanID` → `APS_OrderPlan.FirstPlanID`
- `OrderID` → `APS_Order.OrderID`
- `MaterialID` → `APS_Material.MaterialID`
- `ProcessID` → `APS_Process.ProcessID`
- `ProcessGroupInfoID` → `APS_ProcessGroupInfo.ProcessGroupInfoID`（一般不使用）
- `WorkShopID`、`LineID` → `Dev_Organize.OrganizeID`（排产车间/线别）
- `MachineID` → `APS_Machine.MachineID`（机台）
- `MachineMouldID` → `APS_MachineMould.MachineMouldID`（模具）

### 16.4 备注

- **排产层级**：APS_ProcessPartPlan（工段计划）→ APS_ProcessPlan（工序计划明细）→ APS_DayPlan（日计划）
- **ProductionStatus**：26 待下达，21 已排产，25 已完成
- `ProcessPlanID` ← 被 `APS_DayPlan.ProcessPlanID` 引用（工序计划每天数量）

---

## 17. APS_DayPlan（日计划表）

工序计划按天拆分表，存储每天的计划数、报工数、计划日期、开始/结束时间及达成情况等。表描述：工序计划每天数量。

### 17.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_DayPlan` |
| 主键 | `DayPlanID` (bigint, 自增) |

### 17.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| DayPlanID | bigint | NOT NULL | 日计划ID（主键） |
| ProcessPlanID | bigint | Y | 工序计划ID |
| OrderID | bigint | Y | 订单ID |
| MaterialID | bigint | Y | 料品ID |
| WorkShopID | bigint | Y | 车间ID |
| LineID | bigint | Y | 线别ID |

#### 数量与产能
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PlanQty | decimal(18,4) | Y | 计划数 |
| HasQty | decimal(18,4) | Y | 报工数 |
| Capacity | decimal(18,4) | Y | 产能 |
| ExpectTime | decimal(18,2) | Y | 预计用时 |

#### 日期与时间
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PlanDay | datetime | Y | 计划日期 |
| StartTime | datetime | Y | 开始日期时间 |
| EndTime | datetime | Y | 结束日期时间 |
| FeedingDate | datetime | Y | 备送完成时间 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 达成分析
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Reason | nvarchar(4000) | Y | 不达成原因 |
| ResponsibleDept | nvarchar(50) | Y | 责任部门 |
| IsReach | nvarchar(50) | Y | 是否达成 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 17.3 关联关系

- `ProcessPlanID` → `APS_ProcessPlan.ProcessPlanID`（工序计划）
- `OrderID` → `APS_Order.OrderID`
- `MaterialID` → `APS_Material.MaterialID`

### 17.4 备注

- **排产层级**：APS_ProcessPlan（工序计划）→ APS_DayPlan（日计划，按天拆分）
- 用于工序计划每天的数量、达成分析、责任部门等

---

## 18. APS_PO（采购单表）

采购单表，存储采购订单信息，用于齐套计算、备料、SRM 送货对接等。与生产订单（APS_Order）通过 OrderID 关联。

### 18.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_PO` |
| 主键 | `ID` (bigint, 自增) |
| 默认值 | SCMStatus = N'未生成' |

### 18.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ID | bigint | NOT NULL | 主键，自增 |
| OrderID | bigint | Y | 生产订单ID（基本上不用） |
| MaterialID | bigint | NOT NULL | 料品ID |
| Code | varchar(30) | Y | 料号 |
| PODocNo | varchar(30) | Y | 采购单号 |
| POLineNo | varchar(30) | Y | 行号 |

#### 供应商
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SupplierName | nvarchar(50) | Y | 供应商名称 |
| SupplierCode | varchar(20) | Y | 供应商代码 |

#### 数量相关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| POQty | decimal(24,4) | Y | 采购数量 |
| ReceivedQty | decimal(24,4) | Y | 已收数量 |
| ReturnedQty | decimal(24,4) | Y | 已退货 |
| ActuaArrivalQty | decimal(24,4) | Y | 实际送货数量（拼写 Actua） |
| OnloadQty | decimal(18,4) | Y | 在途数量（订单数-已收） |
| ReplyQty | decimal(24,4) | Y | 回复数量 |
| UnitName | nvarchar(20) | Y | 单位 |

#### 价格与金额
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Price | decimal(18,6) | Y | 单价 |
| Amount | decimal(24,6) | Y | 金额 |
| MonetaryUnit | varchar(10) | Y | 货币单位 |

#### 日期相关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| DeliveryDate | datetime | Y | 交期 |
| DemandDate | datetime | Y | 需求日期 |
| ActualDeliveryDate | datetime | Y | 实际送货日期 |
| ReplyDate | datetime | Y | 回复日期 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 状态与 ERP
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| SCMStatus | nvarchar(50) | NOT NULL | SRM 送货状态（默认「未生成」） |
| ERPID | varchar(30) | Y | ERP 主键 |
| Extend12 | varchar(10) | Y | 厂区 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 18.3 关联关系

- `OrderID` → `APS_Order.OrderID`（生产订单，基本上不用）
- `MaterialID` → `APS_Material.MaterialID`（料品）
- `Code` → `APS_Material.Code`（料号）
- `SupplierCode` → `Dev_Supplier.Code`（供应商代码）

### 18.4 备注

- **业务用途**：采购单，用于齐套、备料、SRM 送货对接
- **OrderID**：生产订单 ID，基本上不用；采购单主要通过 MaterialID、PODocNo 等关联
- **字段拼写**：`ActuaArrivalQty` 为实际送货数量（拼写 Actua）

---

## 19. APS_ProcessPosition（工序职位关联表）

工序与职位/岗位关联表，定义组织下工序对应的岗位、账号、工序等级等信息。表描述：工序职位关联表。

### 19.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_ProcessPosition` |
| 主键 | `ProcessPositionID` (int, 自增) |
| 默认值 | Status = 1, CreatedOn = getdate(), ModifyedOn = getdate(), SyncDatetime = getdate() |

### 19.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProcessPositionID | int | NOT NULL | 主键，自增 |
| ProcessID | varchar(20) | NOT NULL | 工序ID |
| PositionID | int | NOT NULL | 岗位ID |
| OrganizeID | int | Y | 组织ID |
| GroupID | int | Y | 群组ID |
| Account | varchar(20) | Y | 账号 |
| LevelID | int | Y | 工序等级 |
| LevelName | nvarchar(50) | Y | 工序等级名 |

#### 组织与群组
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeName | nvarchar(50) | Y | 组织名称 |
| GroupName | nvarchar(50) | Y | 群组名称 |

#### 状态与 ERP
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态（默认1） |
| ERPID | varchar(30) | Y | ERP主键 |
| DataSource | nvarchar(20) | Y | 数据来源 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 19.3 关联关系

- `ProcessID` → `APS_Process.ProcessID`（工序）
- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `PositionID` → 职位/岗位表（与 Dev_PositionAccountMap.PositionID 同源，表名待确认）
- `Account` → `Dev_Account.Account`（账号）

### 19.4 备注

- **业务用途**：工序与职位/岗位关联，用于人员排班、技能匹配等

---

## 20. Dev_PositionAccountMap（人员岗位关联表）

人员与岗位关联表，定义账号对应的职位及岗位等级。表描述：人员岗位关联表。与 APS_ProcessPosition 配合使用：APS_ProcessPosition 为工序-职位关联，本表为人员-岗位关联。

### 20.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `Dev_PositionAccountMap` |
| 主键 | `PositionAccountID` (int, 自增) |
| 默认值 | Status = 1, CreatedOn = getdate(), ModifyedOn = getdate(), SyncDatetime = getdate() |

### 20.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PositionAccountID | int | NOT NULL | 主键，自增 |
| Account | varchar(20) | NOT NULL | 账号 |
| PositionID | int | NOT NULL | 职位ID |
| PositionLevelID | int | NOT NULL | 岗位等级ID |
| OrganizeID | int | Y | 组织ID |
| GroupID | int | Y | 群组ID |

#### 组织与群组
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeName | nvarchar(50) | Y | 组织名称 |
| GroupName | nvarchar(50) | Y | 群组名称 |

#### 状态与 ERP
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态（默认1） |
| ERPID | varchar(30) | Y | ERP主键 |
| DataSource | nvarchar(20) | Y | 数据来源 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 20.3 关联关系

- `Account` → `Dev_Account.Account`（账号）
- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `PositionID` → 职位/岗位表（与 APS_ProcessPosition.PositionID 同源，表名待确认）
- `PositionLevelID` → `Dev_PositionLevel.PositionLevelID`（岗位等级）

### 20.4 备注

- **业务用途**：人员与岗位关联，定义账号具备的职位及岗位等级
- **与 APS_ProcessPosition**：APS_ProcessPosition 为工序-职位关联，本表为人员-岗位关联，两者通过 PositionID 关联
- `PositionLevelID` → `Dev_PositionLevel.PositionLevelID`（岗位等级主数据）

---

## 21. Dev_PositionLevel（岗位等级表）

岗位等级主数据表，定义工序与职位对应的等级（工序等级对照表）。Dev_PositionAccountMap.PositionLevelID 引用本表。

### 21.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `Dev_PositionLevel` |
| 主键 | `PositionLevelID` (int, 自增) |
| 默认值 | Status = 1, CreatedOn = getdate(), ModifyedOn = getdate(), SyncDatetime = getdate() |

### 21.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PositionLevelID | int | NOT NULL | 主键，自增 |
| PorcessID | varchar(20) | Y | 工序ID（拼写 Porcess，应为 Process） |
| PositionID | int | Y | 职位ID |
| PositionLevelName | nvarchar(20) | Y | 岗位等级名称 |
| PositionLevel | int | Y | 岗位等级（数值） |
| OrganizeID | int | Y | 组织ID |
| GroupID | int | Y | 群组ID |

#### 组织与群组
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeName | nvarchar(50) | Y | 组织名称 |
| GroupName | nvarchar(50) | Y | 群组名称 |

#### 状态与 ERP
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态（默认1） |
| ERPID | varchar(30) | Y | ERP主键 |
| DataSource | nvarchar(20) | Y | 数据来源 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 21.3 关联关系

- `PositionLevelID` ← 被 `Dev_PositionAccountMap.PositionLevelID` 引用
- `PorcessID` → `APS_Process.ProcessID`（工序，字段拼写为 Porcess）
- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `PositionID` → 职位/岗位表（与 APS_ProcessPosition 等同源）

### 21.4 备注

- **业务用途**：工序等级对照表，定义工序+职位对应的岗位等级
- **字段拼写**：`PorcessID` 应为 ProcessID

---

## 22. Dev_PositionLevelMap（岗位技能等级配置表）

岗位/技能等级配置表，定义职位对应的等级名称、分数区间（MinScore~MaxScore）及复审间隔等。表描述：岗位/技能等级配置表。与 Dev_PositionLevel 区别：本表按职位配置等级与分数区间，Dev_PositionLevel 按工序+职位配置等级。

### 22.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `Dev_PositionLevelMap` |
| 主键 | `PositionLevelID` (int, 自增) |
| 默认值 | Status = 1, CreatedOn = getdate(), ModifyedOn = getdate(), SyncDatetime = getdate() |

### 22.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PositionLevelID | int | NOT NULL | 主键，自增 |
| PositionID | int | Y | 职位ID |
| LevelName | nvarchar(50) | Y | 等级名称 |
| MinScore | decimal(5,2) | Y | 最低分数 |
| MaxScore | decimal(5,2) | Y | 最高分数 |
| IntervalTime | decimal(5,2) | Y | 间隔时间（如复审周期） |
| IntervalUnit | nvarchar(10) | Y | 间隔单位 |
| OrganizeID | int | Y | 组织ID |
| GroupID | int | Y | 群组ID |

#### 组织与群组
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeName | nvarchar(50) | Y | 组织名称 |
| GroupName | nvarchar(50) | Y | 群组名称 |

#### 状态与 ERP
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态（默认1） |
| ERPID | varchar(30) | Y | ERP主键 |
| DataSource | nvarchar(20) | Y | 数据来源 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 22.3 关联关系

- `PositionLevelID` ← 可能被 `Dev_PositionAccountMap.PositionLevelID` 引用（与 Dev_PositionLevel 二选一，需按实际业务确认）
- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `PositionID` → 职位/岗位表（与 APS_ProcessPosition 等同源）

### 22.4 备注

- **业务用途**：岗位/技能等级配置，定义分数区间与复审间隔
- **与 Dev_PositionLevel**：两表均有 PositionLevelID 主键，Dev_PositionLevel 为工序+职位等级对照，本表为职位等级配置（含分数区间）
- **LevelName** 可与 Dev_PositionExamRecord.LevelName 关联

---

## 23. APS_Machine（机台表）

机台/设备主数据表，存储机台ID、名称、型号、类型、产能、吨位、基准分钟等。APS_ProcessPlan.MachineID 引用本表。

### 23.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_Machine` |
| 主键 | `MachineID` (varchar(20), 非自增) |

### 23.2 字段清单

#### 主键与基础信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MachineID | varchar(20) | NOT NULL | 机台ID（主键） |
| MachineCode | varchar(20) | Y | 机台编号 |
| MachineName | nvarchar(40) | Y | 机台名称 |
| MachineModel | varchar(20) | Y | 机台型号 |
| MachineTypeID | varchar(20) | Y | 机台类型 |
| MachineCapacity | int | Y | 机台产能 |
| Tonnage | varchar(30) | Y | 机台吨位 |

#### 组织与状态
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeID | int | Y | 组织ID |
| Status | int | Y | 状态 |
| WorkingStatus | int | Y | 工作状态 |

#### 基准与产能
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| BasePoints | int | Y | 基准点 |
| BaseMinute | int | Y | 基准分钟 |
| BaseMinuteNew | int | Y | 新产品基准点 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |
| SyncDatetime | datetime | Y | 同步日期 |

### 23.3 关联关系

- `MachineID` ← 被 `APS_ProcessPlan.MachineID`、`APS_MachineMouldRelative.MachineID` 引用
- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `MachineTypeID` → 机台类型表（表名待确认）

### 23.4 备注

- **业务用途**：机台主数据，用于工序计划排产时的机台分配

---

## 24. APS_MachineMould（模具表）

模具主数据表，存储模具ID、名称、吨位、穴数、保养周期等。APS_ProcessPlan.MachineMouldID 引用本表。

### 24.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_MachineMould` |
| 主键 | `MachineMouldID` (varchar(20), 非自增) |

### 24.2 字段清单

#### 主键与基础信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MachineMouldID | varchar(20) | NOT NULL | 机台模具ID（主键） |
| MoldNO | varchar(30) | Y | 模具编号 |
| MoldName | nvarchar(50) | Y | 模具名称 |
| MoldSpec | nvarchar(500) | Y | 模具规格 |
| MouldTonnage | decimal(8,2) | Y | 模具吨位 |
| MoldHole | int | Y | 穴数 |
| MaintenanceDay | int | Y | 保养周期 |
| PROPeriod | decimal(18,0) | Y | 周期 |
| OrganizeID | int | Y | 组织ID |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |
| SyncDatetime | datetime | Y | 同步日期 |

### 24.3 关联关系

- `MachineMouldID` ← 被 `APS_ProcessPlan.MachineMouldID`、`APS_MachineMouldRelative.MachineMouldID`、`APS_MachineMouldMaterial.MachineMouldID` 引用
- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）

### 24.4 备注

- **业务用途**：模具主数据，用于工序计划排产时的模具分配
- **命名**：表中 Mold/Mould 拼写混用（MoldName、MoldNO、MouldTonnage）

---

## 25. APS_MachineMouldRelative（模具机台关系表）

模具与机台关联表，定义哪些模具可安装在哪些机台上。表描述：模具机台关系表。

### 25.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_MachineMouldRelative` |
| 主键 | `MachineMouldRelativeID` (varchar(20), 非自增) |

### 25.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MachineMouldRelativeID | varchar(20) | NOT NULL | 关系表ID（主键） |
| MachineMouldID | varchar(20) | Y | 模具ID |
| MachineID | varchar(20) | Y | 机台ID |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |
| SyncDatetime | datetime | Y | 同步日期 |

### 25.3 关联关系

- `MachineMouldID` → `APS_MachineMould.MachineMouldID`（模具）
- `MachineID` → `APS_Machine.MachineID`（机台）

### 25.4 备注

- **业务用途**：模具与机台多对多关联，用于排产时校验模具与机台的匹配关系

---

## 26. APS_MachineMouldMaterial（模具产品关系表）

模具与料品/产品关联表，定义哪些模具可生产哪些料品。表描述：模具产品关系表。

### 26.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_MachineMouldMaterial` |
| 主键 | `MachineMouldMaterial` (varchar(20), 非自增，字段名为关系表ID) |

### 26.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MachineMouldMaterial | varchar(20) | NOT NULL | 关系表ID（主键） |
| MachineMouldID | varchar(20) | NOT NULL | 模具ID |
| MaterialID | bigint | NOT NULL | 料品ID |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |
| SyncDatetime | datetime | Y | 同步日期 |

### 26.3 关联关系

- `MachineMouldID` → `APS_MachineMould.MachineMouldID`（模具）
- `MaterialID` → `APS_Material.MaterialID`（料品）

### 26.4 备注

- **业务用途**：模具与料品多对多关联，用于排产时校验模具与产品的匹配关系

---

## 27. APS_MaterialBOM（产品BOM表）

产品BOM主数据表，存储料品的物料清单结构（母件、子件、用量、层级等）。与 APS_OrderBOM 区别：本表为料品级标准BOM，APS_OrderBOM 为订单级用料清单。

### 27.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_MaterialBOM` |
| 主键 | `MaterialBomID` (bigint, 自增) |
| 默认值 | ConsumptionRatio = 1 |

### 27.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MaterialBomID | bigint | NOT NULL | 料品BOMID（主键） |
| MaterialID | bigint | Y | 子件料品ID |
| BOMMasterID | bigint | Y | 母件ID |
| ParentID | bigint | Y | 上级BOM记录ID（自关联） |
| ParentMaterialID | bigint | Y | 上级物料ID |
| ProcessGroupInfoID | varchar(20) | Y | 工序（工艺工序关联） |
| BOMID | varchar(50) | Y | BOMID |
| BomVersion | varchar(100) | Y | BOM版本 |
| OrganizeID | int | Y | 组织ID |
| LineNum | varchar(10) | Y | 行号 |
| LevelPath | varchar(50) | Y | 层级标识 |

#### 用量相关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| QPA | decimal(18,6) | Y | 用量（单耗） |
| Molecule | decimal(24,6) | Y | 分子 |
| Denominator | decimal(24,6) | Y | 分母 |
| ConsumptionRatio | decimal(18,2) | Y | 领料比例（默认1） |
| BadRate | decimal(24,6) | Y | 不良率 |
| LossRate | decimal(18,4) | Y | 损耗率 |
| TotalQPA | decimal(18,6) | Y | 需求总数 |
| ParentTotalQPA | decimal(18,6) | Y | 父项需求总数 |

#### 发料与排产
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| HairFeed | bit | Y | 是否发料 |
| IssueType | nvarchar(100) | Y | 发料方式 |
| IsCreateOrder | bit | Y | 自动生成订单 |
| IsScheduling | bit | Y | 是否排产 |
| TotalFixedDay | int | Y | 总提前期 |

#### 属性与扩展
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| BOMType | nvarchar(100) | Y | BOM类型 |
| MaterialProp | nvarchar(100) | Y | 物料属性 |
| CurrentIndex | int | Y | 当前索引 |
| Extend1 ~ Extend6 | varchar | Y | 扩展1~6 |
| ERPID | varchar(100) | Y | ERP主键 |
| SyncDatetime | datetime | Y | 同步日期 |
| SyncDatetime1 | datetime | Y | 同步日期1 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| CreatedBy | varchar(60) | Y | 创建账号 |
| CreatedByName | nvarchar(60) | Y | 创建姓名 |
| ModifiedBy | varchar(60) | Y | 修改账号 |
| ModifiedByName | nvarchar(60) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 27.3 关联关系

- `MaterialID` → `APS_Material.MaterialID`（子件料品）
- `BOMMasterID`、`ParentMaterialID` → `APS_Material.MaterialID`（母件/父级料品）
- `ParentID` → 本表 MaterialBomID（BOM层级自关联）
- `ProcessGroupInfoID` → `APS_ProcessGroupInfo.ProcessGroupInfoID`（工序）
- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）

### 27.4 备注

- **业务用途**：产品BOM主数据，用于标准用料结构、齐套计算、订单BOM展开
- **与 APS_OrderBOM**：APS_MaterialBOM 为料品级标准BOM，APS_OrderBOM 为订单级用料清单（来源于本表或ERP）
- **Molecule/Denominator**：用量比例，QPA = 分子/分母

---

## 28. APS_PR（采购申请表）

采购申请表，存储采购申请信息（申请数量、已转采购数、剩余数量、到货数量等）。表描述：采购申请。与 APS_PO 配合：PR 为申请，PO 为采购单。

### 28.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_PR` |
| 主键 | `ID` (varchar(30), 非自增) |

### 28.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ID | varchar(30) | NOT NULL | 主键 |
| DocNo | varchar(30) | Y | 单号 |
| MaterialID | bigint | Y | 料品ID |
| MaterialName | nvarchar(100) | Y | 料品名称 |
| OrganizeID | int | Y | 组织ID |
| GroupID | int | Y | 群组ID |

#### 数量相关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ReqQty | decimal(18,4) | Y | 申请数量 |
| OrderQty | decimal(18,4) | Y | 已转采购数 |
| SurplusQty | decimal(18,4) | Y | 剩余数量 |
| DeliveredQty | decimal(18,4) | Y | 到货数量 |
| UnaccountedQty | decimal(18,4) | Y | 未清数量 |

#### 采购与状态
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| POTracker | nvarchar(20) | Y | 采购员 |
| Status | int | Y | 状态 |
| ERPID | varchar(30) | Y | ERP主键 |
| DataSource | nvarchar(20) | Y | 数据来源 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 28.3 关联关系

- `MaterialID` → `APS_Material.MaterialID`（料品）
- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）

### 28.4 备注

- **业务用途**：采购申请，用于齐套缺口转采购、采购跟进
- **与 APS_PO**：PR 为采购申请，PO 为采购单，PR 转 PO 后 OrderQty 累计已转采购数

---

## 29. Dev_PositionExamRecord（员工技能考核记录表）

员工技能考核记录表，存储人员针对岗位/职位的考核记录（考试时间、产能目标、实际完成、不良统计、分数、是否通过等）。表描述：员工技能考核记录表。

### 29.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `Dev_PositionExamRecord` |
| 主键 | `PositionAccountID` (int, 自增) |
| 默认值 | Status = 1, CreatedOn = getdate(), ModifyedOn = getdate(), SyncDatetime = getdate() |

### 29.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PositionAccountID | int | NOT NULL | 主键，自增 |
| PositionID | int | Y | 职位ID |
| Account | nvarchar(20) | Y | 账号 |
| Name | nvarchar(30) | Y | 姓名 |
| LevelName | nvarchar(50) | Y | 等级名称（可与 Dev_PositionLevel/Dev_PositionLevelMap 关联）
| Code | nvarchar(50) | Y | 编码 |
| OrganizeID | int | Y | 组织ID |
| GroupID | int | Y | 群组ID |

#### 考核数据
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| TestTime | decimal(4,1) | Y | 考试时间 |
| TargetNumber | decimal(18,2) | Y | 产能目标 |
| ActualNumber | decimal(18,2) | Y | 实际完成 |
| DefectiveNumber | decimal(18,2) | Y | 不良统计 |
| Score | decimal(6,2) | Y | 分数 |
| IsPass | nvarchar(10) | Y | 是否通过 |
| Assessor | nvarchar(20) | Y | 考核人 |

#### 组织与群组
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeName | nvarchar(50) | Y | 组织名称 |
| GroupName | nvarchar(50) | Y | 群组名称 |

#### 状态与 ERP
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态（默认1） |
| ERPID | varchar(30) | Y | ERP主键 |
| DataSource | nvarchar(20) | Y | 数据来源 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 审计与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 29.3 关联关系

- `Account` → `Dev_Account.Account`（账号）
- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `PositionID` → 职位/岗位表（与 APS_ProcessPosition、Dev_PositionAccountMap 同源）

### 29.4 备注

- **业务用途**：员工技能考核记录，用于岗位资格认证、技能评估
- **主键命名**：PositionAccountID 与 Dev_PositionAccountMap 主键名相同，但为不同表；原约束名 PK_APS_SKILLACCOUNT 表明曾为技能账号相关

---

## 30. WMS_Stock（库存表）

WMS 库存表，存储仓库料品库存数量、入库数、待检数及条码等信息，与齐套计算、物料需求等业务相关。

### 30.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `WMS_Stock` |
| 主键 | `StockID` (varchar(30)) |

### 30.2 字段清单

#### 主键与标识
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| StockID | varchar(30) | NOT NULL | 库存ID（主键） |

#### 仓库与料品
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| WarehouseID | varchar(20) | Y | 仓库ID |
| MaterialID | bigint | Y | 料品ID |
| MaterialName | nvarchar(100) | Y | 料品名称 |
| MFGOrganizeID | bigint | Y | 制造组织 |

#### 数量
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| StockQty | decimal(18,4) | Y | 库存数量 |
| InQty | decimal(18,4) | Y | 入库数 |
| OncheckQty | decimal(18,4) | Y | 待检数 |

#### 业务关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CustomerID | bigint | Y | 客户ID |
| SSN | varchar(30) | Y | 条码 |
| ERPID | varchar(40) | Y | ERP 对接 ID |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |

### 30.3 关联关系

- `MaterialID` → `APS_Material.MaterialID`（料品）
- `WarehouseID` → `WMS_Warehouse.WarehouseID`（仓库）
- `CustomerID` → 客户主数据
- `MFGOrganizeID` → `Dev_Organize.OrganizeID`（制造组织，类型 bigint 与部分环境兼容）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`（账号）

### 30.4 备注

- **业务用途**：仓库库存管理、齐套计算（V_WMS_StockForActive 等视图）、物料需求分析
- **视图依赖**：齐套、采购等存储过程常引用 `V_WMS_StockForActive`、`V_WMS_StockBad` 等视图

---

## 31. WMS_Warehouse（仓库表）

WMS 仓库主数据表，存储仓库基础信息、类型、地址、联系人及齐套/预警等业务开关。

### 31.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `WMS_Warehouse` |
| 主键 | `WarehouseID` (varchar(20)) |

### 31.2 字段清单

#### 主键与层级
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| WarehouseID | varchar(20) | NOT NULL | 仓库ID（主键） |
| ParentWarehouseID | varchar(20) | Y | 上级仓库ID |

#### 基础信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| WarehouseName | nvarchar(300) | Y | 仓库名称 |
| Code | varchar(20) | Y | 仓库编码 |
| OrganizeID | int | Y | 组织ID |
| Address | nvarchar(100) | Y | 地址 |
| Contacts | varchar(30) | Y | 联系人 |
| Tel | varchar(30) | Y | 电话 |
| Area | nvarchar(50) | Y | 功能区 |
| Manager | nvarchar(50) | Y | 负责人 |

#### 类型与属性
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| DataType | nvarchar(50) | Y | 数据类型 |
| WarehouseTypeID | varchar(20) | Y | 仓库类型 |
| Property | nvarchar(50) | Y | 仓库属性 |

#### 业务开关
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| IsUsable | bit | Y | 计算供需平衡表 |
| IsWarning | bit | Y | 库存预警 |
| IsMating | bit | Y | 齐套运算 |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |

#### 备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 31.3 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `ParentWarehouseID` → 本表 `WMS_Warehouse.WarehouseID`（上级仓库）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`（账号）
- `WarehouseID` ← 被 `WMS_Stock.WarehouseID` 引用

### 31.4 备注

- **业务用途**：仓库主数据、齐套计算筛选（IsMating）、供需平衡（IsUsable）、库存预警（IsWarning）

---

## 32. APS_WorkingTimes（班别表）

上班时间配置表，存储班次/班别的基础信息、上下班时间、工时、打卡规则及加班配置，供排产、考勤等使用。

### 32.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_WorkingTimes` |
| 主键 | `WorkingTimesID` (varchar(20)) |
| 默认值 | Extend1~3=0，WorkHour/RestHour/TotalHour=0 |

### 32.2 字段清单

#### 主键与层级
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| WorkingTimesID | varchar(20) | NOT NULL | 班别ID（主键） |
| ParentWorkingTimesID | varchar(20) | Y | 上级班别ID |

#### 基础信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| WorkingTimesName | nvarchar(30) | Y | 班次名称 |
| StartTime | varchar(10) | Y | 开始时间 |
| EndTime | varchar(10) | Y | 结束时间 |
| WorkingType | nvarchar(10) | Y | 班次类型（上班、加班） |
| OrganizeID | int | Y | 组织ID |

#### 工时
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| WorkHour | decimal(10,2) | Y | 上班时长 |
| RestHour | decimal(10,2) | Y | 休息时长 |
| TotalHour | decimal(10,2) | Y | 总时长 |
| OverTimeHour | decimal(10,1) | Y | 加班时长 |
| KnotRestHour | decimal(10,2) | Y | 扣休息时长 |

#### 打卡与考勤
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| TimeScope | varchar(50) | Y | 打卡区间 |
| EndTimeScope | varchar(50) | Y | 结束打卡区间 |
| CardNum | int | Y | 打卡次数 |
| IsPunch | int | Y | 是否免打卡 |
| IsPunch2 | int | Y | 免打卡（第二档） |
| LateOverTime | decimal(10,2) | Y | 推时打卡(迟到) |
| LeaveOverTime | decimal(10,2) | Y | 提前打卡(早退) |
| LateOverTime2 | decimal(10,2) | Y | 推时打卡(旷工) |
| LeaveOverTime2 | decimal(10,2) | Y | 提前打卡(旷工) |

#### 日期范围与扩展
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| StartDate | datetime | Y | 开始日期 |
| EndDate | datetime | Y | 截止日期 |
| Extend1~16 | varchar(100) | Y | 扩展 1~16（Extend5=使用周，Extend6=顺序） |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| SyncDatetime | datetime | Y | 同步日期 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 32.3 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `ParentWorkingTimesID` → 本表 `APS_WorkingTimes.WorkingTimesID`（上级班别）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`（账号）
- `WorkingTimesID` ← 被 `APS_Order.WorkingTimesID` 等引用

### 32.4 备注

- **业务用途**：班别主数据、排产可用工时计算、考勤规则（打卡、迟到早退、旷工、加班）

---

## 33. APS_Holiday（放假表）

放假/节假日配置表，按组织存储放假日期区间，供排产排除非工作日、产能计算等使用。

### 33.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_Holiday` |
| 主键 | `HolidayID` (varchar(20)) |

### 33.2 字段清单

#### 主键与日期
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| HolidayID | varchar(20) | NOT NULL | 放假ID（主键） |
| OrganizeID | int | Y | 组织ID |
| StartDate | datetime | Y | 开始日期 |
| EndDate | datetime | Y | 结束日期 |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| SyncDatetime | datetime | Y | 同步日期 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 33.3 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`（账号）

### 33.4 备注

- **业务用途**：排产日历、产能计算时排除节假日、休息日

---

## 34. APS_OrganizeWorkingTimesDetail（排班明细表）

组织班别排班明细表，按日期存储组织的工作日排班（工作日期、班别、人数、总时长），供排产、产能计算使用。

### 34.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_OrganizeWorkingTimesDetail` |
| 主键 | `WorkingTimesDetailID` (varchar(20)) |

### 34.2 字段清单

#### 主键与关联
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| WorkingTimesDetailID | varchar(20) | NOT NULL | 明细ID（主键） |
| OrganizeID | int | Y | 组织ID |
| WorkingTimesID | varchar(20) | Y | 班别ID |

#### 日期与工时
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| WorkingDate | datetime | Y | 工作日期 |
| Peoples | int | Y | 人数 |
| TotalHours | decimal(18,10) | Y | 总时长 |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| SyncDatetime | datetime | Y | 同步日期 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 34.3 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `WorkingTimesID` → `APS_WorkingTimes.WorkingTimesID`（班别）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`（账号）

### 34.4 备注

- **业务用途**：排产、产能计算时按组织+日期获取可用工作日、班别、人数、总工时（齐套/组装计划等存储过程会引用）

---

## 35. APS_OrderPlanMaterialForm（物料齐套明细表）

物料齐套明细表，存储预排/订单的料品配套信息（需求、库存、分配、欠数、在途、采购回复、替代料、点检与复期等），为齐套计算的核心业务表。下列字段与类型以 **APS 库 2026-04-14** 导出脚本为准（主键名 **`PK_APS_ORDERPLANMATERIALFORM`**）。

### 35.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_OrderPlanMaterialForm` |
| 主键 | `MaterialFormID` (bigint, `IDENTITY(1,1)`)，约束名 `PK_APS_ORDERPLANMATERIALFORM` |
| 列级默认值 | `InspectStatus` **`DEFAULT (0)`**；`IsReplyStatus` **`DEFAULT (0)`**（库内 `DF_APS_OrderPlanMaterialForm_*`） |

### 35.2 字段清单

#### 主键与料品
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MaterialFormID | bigint | NOT NULL | 配套ID（主键，自增） |
| MaterialID | bigint | Y | 料品ID |
| Code | varchar(30) | Y | 料号 |
| Spec | nvarchar(1000) | Y | 规格 |
| MaterialName | nvarchar(100) | Y | 料品名称 |
| MaterialProp | nvarchar(10) | Y | 物料属性 |
| IssueType | nchar(10) | Y | 发料类型（与业务字典对齐） |

#### 成品信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProductCode | varchar(30) | Y | 成品代码 |
| ProductName | nvarchar(100) | Y | 成品名称 |
| ProductSpec | nvarchar(300) | Y | 成品规格 |

#### 预排、订单与来源
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| FirstPlanID | bigint | Y | 预排ID |
| OrderID | bigint | Y | 订单ID |
| OrderNo | varchar(30) | Y | 工单号 |
| OrderBOMID | bigint | Y | 订单用料（BOM）ID |
| SourceOrderNo | varchar(30) | Y | 来源单号 |
| SourceOrderLineNo | varchar(5) | Y | 销售订单行（行号类快照） |
| SalesOrderDetailID | varchar(30) | Y | 销售订单行ID（与 `APS_SalesOrderDetail` 键宽可能不一致，以实际库为准） |
| WorkOrderTypeName | nvarchar(40) | Y | 工单类型名称 |
| WorkShopName | nvarchar(30) | Y | 车间 |
| CompanyName | nvarchar(50) | Y | 公司名称快照 |
| DataSource | nvarchar(20) | Y | 数据来源 |

#### 需求与计划数量
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| DemandQty | decimal(18,4) | Y | 需求数量 |
| FormQty | decimal(18,4) | Y | 配套数 |
| OweQty | decimal(18,4) | Y | 欠数 |
| OweQty1 | decimal(18,4) | Y | 欠数（口径1，与库注释「欠数」一致） |
| OweQty2 | decimal(18,4) | Y | 欠数（口径2） |
| OweQty3 | decimal(18,4) | Y | 欠数（口径3） |
| QPA | decimal(18,6) | Y | 用量 |
| PlanQty | decimal(18,4) | Y | 计划数 |
| PlanQtyQPA | decimal(18,4) | Y | 计划数×用量类字段 |
| Denominator | int | Y | 分母（分数用量） |
| Molecule | int | Y | 分子（分数用量） |
| ShortQty | decimal(18,4) | Y | 短缺数量 |
| AllUnIssuedQty | decimal(24,6) | Y | 汇总未领用量类 |
| UnIssuedQty | decimal(24,6) | Y | 未领用量 |

#### 库存与分配
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| StockQty | decimal(18,4) | Y | 库存数 |
| StockQty1 | decimal(18,4) | Y | 库存数1 |
| StockQty2 | decimal(18,4) | Y | 库存数2 |
| StockQtyAllocation | decimal(18,4) | Y | 库存分配数 |
| StockQtyAllocationPrepare | decimal(18,4) | Y | 库存分配数（按备料计算） |
| StockQtyAllocationPrepareLess | decimal(18,4) | Y | 库存分配数（备料）余数 |
| StockQtyAllocationPrepare1 | decimal(18,4) | Y | 库存分配数（复期） |
| StockQtyAllocationPrepareLess1 | decimal(18,4) | Y | 库存分配数（复期）余数 |
| StockQtyAllocationResult | nvarchar(10) | Y | 库存分配结论 |
| IssuedQty | decimal(18,4) | Y | 在制数/已发数（与库注释「在制数」一致） |
| OncheckQty | decimal(18,4) | Y | 待检数 |
| OnCheckQtyAllocation | decimal(18,4) | Y | 分配质检 |
| OnloadQty | decimal(18,4) | Y | 在途 |
| LatestStockDate | datetime | Y | 最新入库日期 |
| LatestStockQty | decimal(18,4) | Y | 最新入库数 |
| CalculationDate | datetime | Y | 计算日期 |
| CalculationDate1 | datetime | Y | 计算日期2 |

#### 替代料
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SubstitutesStockQty | decimal(18,4) | Y | 替代料库存 |
| SubstitutesStockQtyAllocation | decimal(18,4) | Y | 替代料用量 |
| SubstitutesStockQtyLess | decimal(18,4) | Y | 替代料剩余库存 |
| SubstitutesIssuedQty | decimal(18,4) | Y | 替代料已发/在制类数量 |

#### 采购、回复与送货
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| POTracker | nvarchar(20) | Y | 物料跟踪员 |
| PODocs | varchar(max) | Y | 关联的采购单号（可多单拼接） |
| PODeliveryDate | varchar(1000) | Y | 采购交期类文本/拼接 |
| PODeliveryQty | decimal(18,4) | Y | 分配送货数 |
| POSuplierName | nvarchar(max) | Y | 供应商 |
| ReplyDate | datetime | Y | 回复日期 |
| ReplyQty | decimal(18,4) | Y | 回复数量 |
| OldReplyDate | datetime | Y | 原回复日期 |
| FirstReplyDate | datetime | Y | 首次复期 |
| LastReplyDate | datetime | Y | 最后复期 |
| SecondReplyDate | datetime | Y | 二次复期 |
| SuplierReplyDate | datetime | Y | 供应商复期 |
| SuplierOperationDate | datetime | Y | 供应商复期操作时间 |
| DemandReplyDate | datetime | Y | 要求回货日期 |
| SetReplyDate | datetime | Y | 下发日期 |
| ChangeMark | nvarchar(5) | Y | 供方复期变更标记 |
| DemandChangeMark | nvarchar(500) | Y | 需求数量变动备注 |

#### 点检、来料与复期状态
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| InspectStatus | int | Y | 点检状态：**0** 未点检，**1** 已点检，**2** 有欠料（库扩展属性；列默认 **0**） |
| InspectDate | datetime | Y | 点检日期 |
| InspectUser | varchar(30) | Y | 点检人 |
| IssueDate | datetime | Y | 发料日期 |
| InDate | datetime | Y | 来料日期 |
| InQty | decimal(18,4) | Y | 来料数量 |
| IsReplyStatus | int | Y | 复期状态（列默认 **0**） |
| IsAbnormal | bit | Y | 是否异常 |
| Notice | nvarchar(10) | Y | 通知 |

#### 备料与产线
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PrepareType | varchar(30) | Y | 备料状态 |
| PrepareDate | datetime | Y | 备料日期 |
| PrepareDate1 | datetime | Y | 备料日期1 |
| LineID | bigint | Y | 产线ID |
| LineName | nvarchar(50) | Y | 产线名称 |
| SN | varchar(5) | Y | 序号类标记 |

#### 关闭与杂项日期
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| StartDate | datetime | Y | 开拉期 |
| SaveDate | date | Y | 封存日期 |
| DemandDate | datetime | Y | 需求日期 |
| OweDate | datetime | Y | 欠料日期 |
| LastDate | datetime | Y | 最近日期类 |
| LastDate1 | datetime | Y | 最近日期类1 |
| CloseDate1 | datetime | Y | 关闭日期1 |
| Close1 | nvarchar(10) | Y | 关闭标记1 |
| CloseDate2 | datetime | Y | 关闭日期2 |
| Close2 | nvarchar(10) | Y | 关闭标记2 |
| PlanCreatedOn | datetime | Y | 计划创建时间 |
| StockState | int | Y | 生成送货计划状态 |

#### 扩展、备注与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ParentID | bigint | Y | 上级ID（树形/历史兼容；业务可不用） |
| SIndex | int | Y | 计算顺序 |
| Q1 | decimal(18,4) | Y | 扩展数量1 |
| Q2 | decimal(18,4) | Y | 扩展数量2 |
| Q3 | decimal(18,4) | Y | 扩展数量3 |
| Extend1 | nvarchar(500) | Y | 扩展1 |
| Extend2 | nvarchar(500) | Y | 扩展2 |
| Extend11 | varchar(50) | Y | 扩展11 |
| Extend111 | nvarchar(5) | Y | 供应类型（库扩展属性） |
| Extend12 | varchar(50) | Y | 扩展12 |
| Extend13 | varchar(1000) | Y | 扩展13 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |
| Remark3 | nvarchar(500) | Y | 备注3 |
| Remark4 | nvarchar(500) | Y | 备注4 |
| Remark5 | nvarchar(500) | Y | 备注5 |
| Remark6 | nvarchar(500) | Y | 备注6 |
| PMCRemark | nvarchar(50) | Y | PMC备注 |
| ERPID | varchar(30) | Y | 外部键 |
| SyncDatetime | datetime | Y | 同步时间 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |

### 35.3 关联关系

- `MaterialID` → `APS_Material.MaterialID`（料品）
- `FirstPlanID` → `APS_OrderPlan.FirstPlanID`（预排）
- `OrderID` → `APS_Order.OrderID`（生产订单）
- `OrderBOMID` → `APS_OrderBOM.OrderBOMID`（订单用料清单）
- `SalesOrderDetailID` → `APS_SalesOrderDetail.SalesOrderDetailID`（逻辑关联；类型宽度以环境为准）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`（账号）

### 35.4 备注

- **业务用途**：齐套计算核心表，存储预排/订单料品的需求、库存、分配、欠数、在途、采购与复期、点检等，供 `P_APS_OrderPlanMaterialFormNew` 等存储过程及齐套/送货计划使用。
- **与历史文档差异**：当前 APS 库脚本中**无** `OnPutQty`（待上架）；若其它环境仍存在该列，属环境差异。
- **扩展属性**：表级 MS_Description 为「配套表」；列级说明以库内 `sp_addextendedproperty` 为准（如 `InspectStatus`、`PrepareType`、库存分配系列字段等）。
- **拼写**：库中列名 **`POSuplierName`**、**`SuplierReplyDate`** 等为历史拼写，勿与业务「Supplier」混为两列。

---

## 36. APS_OrderPlanMaterialFormByItem（物料齐套按料号汇总表）

物料齐套按料号汇总表，按料号维度汇总需求、欠数、库存、在途、采购等信息及分时段欠数/需求（Q1~Q9），供报表、物料齐套查询使用。无主键，通常由存储过程或视图写入/更新。

### 36.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_OrderPlanMaterialFormByItem` |
| 主键 | 无（汇总表） |

### 36.2 字段清单

#### 料品与数量
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MaterialID | bigint | Y | 料品ID |
| Code | varchar(30) | Y | 料号 |
| MaterialName | nvarchar(100) | Y | 物料名称 |
| DemandQty | decimal(38,4) | Y | 总需求 |
| OweQty | decimal(38,4) | Y | 总欠数 |
| OweQty2 | decimal(38,4) | Y | 欠数2 |
| StockQty | decimal(18,4) | Y | 库存数 |
| OncheckQty | decimal(18,4) | Y | 待检数 |
| OnloadQty | decimal(18,4) | Y | 在途数 |
| MPQ | decimal(18,4) | Y | 最小包装数 |

#### 日期与状态
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SaveDate | date | Y | 计算日期 |
| StartDate | datetime | Y | 最早开拉日期 |
| OweDay | datetime | Y | 最早欠料日期 |
| ConflictDate | datetime | Y | 冲突日期 |
| OweQtyStatus | varchar(2) | NOT NULL | 欠数状态 |

#### 分时段欠数/需求（Q1~Q9）
| 字段名 | 类型 | 说明 |
|--------|------|------|
| Q1 | decimal(18,4) | 3日欠数 |
| Q2 | decimal(18,4) | 4日欠数 |
| Q3 | decimal(18,4) | 15日欠数 |
| Q4 | decimal(18,4) | 历史欠数 |
| Q5 | decimal(18,4) | 三日需求 |
| Q6 | decimal(18,4) | 七日需求 |
| Q7 | decimal(18,4) | 十五日需求 |
| Q8 | decimal(18,4) | 三十日需求 |
| Q9 | decimal(18,4) | 扩展 |

#### 采购与替代
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| POTracker | nvarchar(20) | Y | 物控 |
| POSuplierName | nvarchar(4000) | Y | 供应商 |
| POSuplierCodes | varchar(4000) | Y | 供应商代码 |
| PODocs | varchar(max) | Y | 采购单号 |
| PurchaseDes | nvarchar(50) | Y | 采购描述 |
| SubstituteCode | varchar(2000) | Y | 替代料 |

#### 其他
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MaterialCount | int | Y | 物料数（不使用） |
| LT | int | Y | 提前期（不使用） |
| Extend12 | varchar(10) | Y | 工厂代码 |
| SyncDatetime | datetime | Y | 同步日期 |

### 36.3 关联关系

- `MaterialID` → `APS_Material.MaterialID`（料品）
- 数据来源于 `APS_OrderPlanMaterialForm` 等齐套明细的按料号汇总

### 36.4 备注

- **业务用途**：物料齐套按料号汇总、报表展示、分时段欠数/需求分析
- **已废弃字段**：`MaterialCount`、`LT` 不使用
- **数据来源**：通常由存储过程根据齐套明细表汇总写入

---

## 37. APS_DeliveryRule（送货计划规则表）

送货计划规则表，按供应商/组织/基地配置送货规则类型（按周、按月、按需求、按PO数量）、规则值、送货天数及前置期，供送货计划生成使用。

### 37.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_DeliveryRule` |
| 主键 | `ID` (bigint, 自增) |

### 37.2 字段清单

#### 主键与规则
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ID | bigint | NOT NULL | 主键（自增） |
| SuplierCode | varchar(30) | Y | 供应商代码 |
| RuleType | nvarchar(10) | Y | 规则类型（按周、按月、按需求、按PO数量） |
| RuleValue | varchar(100) | Y | 规则值 |

#### 组织与基地
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeID | int | Y | 组织ID |
| OrganizeName | nvarchar(50) | Y | 组织名称 |
| GroupID | int | Y | 群组ID |
| GroupName | nvarchar(50) | Y | 基地名称 |

#### 送货与前置期
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| TransitTime | int | Y | 送货天数 |
| LT | decimal(18,1) | Y | 前置期 |
| DeliveryRate | int | Y | 送货频率 |

#### 物控与工厂
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Account | varchar(30) | Y | 账号（物控） |
| ControlID | varchar(30) | Y | 控制者 ID |
| WERKS | varchar(30) | Y | 工厂代码（SAP） |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | ERP 主键 ID |
| DataSource | nvarchar(20) | Y | 数据来源 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 37.3 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `Account` → `Dev_Account.Account`（物控账号）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`（账号）
- `SuplierCode` → `Dev_Supplier.Code`（供应商代码）

### 37.4 备注

- **业务用途**：送货计划规则配置，控制送货计划生成逻辑（按周、按月、按需求、按 PO 数量）；可按工厂（WERKS）、送货频率（DeliveryRate）、物控（Account）等维度细化

---

## 38. ERP_ZPPT036Temp（送货计划运算中间表）

送货计划运算中间表，从 SAP/ERP 下载并暂存 MRP 缺料、采购、库存等信息，供送货计划计算使用。表名 ZPPT036 为 SAP 报表/接口标识。

### 38.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `ERP_ZPPT036Temp` |
| 主键 | `ID` (bigint, 自增) |

### 38.2 字段清单

#### 组织与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeID | int | Y | 组织ID |
| OrganizeName | nvarchar(50) | Y | 组织名称 |
| GroupID | int | Y | 群组ID |
| GroupName | nvarchar(50) | Y | 群组名称 |
| Status | int | Y | 状态 |
| ERPID | varchar(30) | Y | ERP 主键 ID |
| DataSource | nvarchar(20) | Y | 数据来源 |
| SyncDatetime | datetime | Y | 同步日期 |
| CreatedBy / ModifiedBy / CreatedOn / ModifyedOn | ... | Y | 审计字段 |
| Remark1 / Remark2 | nvarchar(500) | Y | 备注 |

#### 料品与数量（SAP 字段）
| 字段名 | 类型 | 说明 |
|--------|------|------|
| MATNR | varchar(30) | 物料料号 |
| BAUGR | varchar(40) | 母件 |
| MAKTX / MAKTX2 | nvarchar(100/200) | 物料名称/描述 |
| MEINS / BMEIN | varchar(10/20) | 基本计量单位 |
| MATKL / MTART | varchar(10/20) | 物料组/物料类型 |
| BDMNG | decimal(18,6) | 需求量 |
| ERFMG | decimal(18,6) | 需发数量 |
| ENMNG | decimal(18,6) | 提货数 |
| STOCK | decimal(18,6) | 即时库存 |
| ZCHECK | decimal(18,6) | 待检数量 |
| TRNRES | decimal(18,6) | 采购订单未交量 |
| PLDORD | decimal(18,6) | 计划订单 |
| PRDORD | decimal(18,6) | 生产待入库 |
| ZQLS / ZWFP | decimal(18,6) | 欠数/未分配数量 |
| OweAllQty | decimal(18,6) | 总欠料数 |
| ZZYKYL / ZZYKSL | decimal(18,6) | 采购单未交量/资源开始量 |
| ZKCZYL / ZKCDQL / ZZYDQL | decimal(18,6) | 库存占用量/短缺量/资源短缺量 |

#### 采购与供应商
| 字段名 | 类型 | 说明 |
|--------|------|------|
| EBELN | varchar(30) | 采购凭证编号 |
| EBELP | varchar(20) | 采购凭证行号 |
| LIFNR | varchar(20) | 供应商代码 |
| NAME_ORG1 | nvarchar(40) | 供应商名称 |
| BANFN / BNFPO | varchar(20) | 采购申请编号/行号 |
| DEL12 / DELPS | varchar(20) | 采购单/采购单行 |
| PURSUR1 / PURSUR2 | decimal(18,6) | 未确认/已确认请购数量 |

#### 需求与订单（SAP）
| 字段名 | 类型 | 说明 |
|--------|------|------|
| RSNUM / RSPOS | varchar(10/20) | 预留需求编号/行号 |
| AUFNR | varchar(20) | 订单号 |
| KDAUF / KDPOS | decimal/varchar | 销售订单数/条款数 |
| VBELN / POSNR | varchar(10) | 销售凭证号/项目号 |
| BDTER / BDTER2 | datetime | 需求日期 |
| EINDT | datetime | 项目交货日期 |
| LFDAT | datetime | 交货日期 |

#### 其他 SAP 字段（部分）
| 字段名 | 说明 |
|--------|------|
| MANDT | 集团 |
| WERKS | 工厂代码 |
| DISPO | MRP 控制者 |
| PLIFZ | 计划交货时间（天） |
| BESKZ | 采购类型 |
| FEVOR | 生产管理员 |
| ZQLZT | 欠料状态 |
| INPUT_WEEK | 年度星期 |
| SupplierMatch | 配比 |
| POID | bigint，采购订单 ID |
| OrderID | bigint，生产订单 ID |
| ID | 主键（自增） |

### 38.3 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `MATNR` → `APS_Material.Code` 或 `APS_Material.ERPID`（料品，SAP 物料号）
- `LIFNR` → `Dev_Supplier.Code`（供应商代码，SAP 格式）
- `POID` → `APS_PO.ID`（采购订单主键）
- `OrderID` → `APS_Order.OrderID`（生产订单）
- `EBELN` → `APS_PO.PODocNo`（采购凭证编号，与 PO 单号对应）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`（账号）

### 38.4 备注

- **业务用途**：送货计划运算中间表，记录缺料信息，数据来自 SAP/ERP MRP 接口
- **Temp 表**：通常由下载/同步作业写入，供后续送货计划计算或报表使用

---

## 39. APS_DeliveryDataDetail（送货分配过程明细表）

送货分配过程明细表，存储送货计划分配结果：采购单、料品、未交量、欠料数、需求日期、回复交期、供应商等，供送货计划展示、SRM 对接使用。

### 39.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_DeliveryDataDetail` |
| 主键 | `ID` (bigint, 自增) |

### 39.2 字段清单

#### 主键与采购单
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ID | bigint | NOT NULL | 主键（自增） |
| POCreateDate | datetime | Y | PO 创建日期 |
| ResourceNO | varchar(30) | Y | 采购单号 |
| LineNum | varchar(20) | Y | 行号 |

#### 料品与数量
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MaterialID | bigint | Y | 料品ID |
| ItemCode | varchar(30) | Y | 料号 |
| ItemName | nvarchar(100) | Y | 物料名称 |
| Spec | nvarchar(300) | Y | 规格 |
| UnitName | nvarchar(10) | Y | 单位 |
| AvailableQty | decimal(18,6) | Y | 采购单未交数量 |
| OweQty | decimal(18,6) | Y | 欠料数 |

#### 日期
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| DemandDay | datetime | Y | 需求日期 |
| DemandToDay | datetime | Y | 需求到料日期 |
| ReplyDay | datetime | Y | 回复交期 |
| APSDemandDay | datetime | Y | APS 需求日期 |
| DemandDay1 | varchar(1500) | Y | 需求日期（多值拼接） |
| DemandToDay1 | varchar(1500) | Y | 需求到料日期（多值拼接） |
| ReplyDay1 | varchar(1500) | Y | 回复交期（多值拼接） |

#### 采购与供应商
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProcurementSection | nvarchar(50) | Y | 采购组 |
| Account | varchar(30) | Y | 账号（物控） |
| ControlID | varchar(5) | Y | 控制者 |
| SuplierCode | varchar(20) | Y | 供应商代码 |
| SuplierName | nvarchar(50) | Y | 供应商名称 |

#### 组织与 ERP
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeID | int | Y | 组织ID |
| OrganizeName | nvarchar(50) | Y | 组织名称 |
| GroupID | int | Y | 群组ID |
| GroupName | nvarchar(50) | Y | 群组名称 |
| ERPID | varchar(30) | Y | ERP 主键 ID |
| DataSource | nvarchar(20) | Y | 数据来源 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| MergeCount | int | Y | 合并数 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 39.3 关联关系

- `MaterialID` → `APS_Material.MaterialID`（料品）
- `ItemCode` → `APS_Material.Code`（料号）
- `SuplierCode` → `Dev_Supplier.Code`（供应商代码）
- `ResourceNO` → `APS_PO.PODocNo`（采购单号，若对接）
- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `Account` → `Dev_Account.Account`（物控账号）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`（账号）

### 39.4 备注

- **业务用途**：送货分配过程明细，送货计划分配结果的存储与展示、SRM 送货对接

---

## 40. APS_DeliveryData（供应商送货计划表）

供应商送货计划表（表描述：供应商送货计划），存储采购单行维度的送货计划数据：采购单、料号、采购未交量、欠数、需求/到料/回复日期、需求到料合并串、物控、PMC 备注、供方复期、配比等。与 `APS_DeliveryDataDetail` 配合：本表为计划主数据，Detail 为分配过程明细。

### 40.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_DeliveryData` |
| 主键 | `ID` (bigint, 自增) |

### 40.2 字段清单

#### 主键与采购单
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ID | bigint | NOT NULL | 主键（自增） |
| POCreateDate | datetime | Y | PO 创建日期 |
| ResourceNO | varchar(30) | Y | 采购单 |
| LineNum | varchar(20) | Y | 行号 |

#### 料品与数量
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ItemCode | varchar(30) | Y | 料号 |
| ItemName | nvarchar(100) | Y | 物料名称 |
| Spec | nvarchar(300) | Y | 规格 |
| UnitName | nvarchar(10) | Y | 单位 |
| AvailableQty | decimal(18,6) | Y | 采购未交数量 |
| OweQty | decimal(18,6) | Y | 欠数 |
| SupplierMatch | decimal(18,4) | Y | 配比 |

#### 日期与合并
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| DemandDay | datetime | Y | 需求日期 |
| DemandToDay | datetime | Y | 需求到料日期 |
| ReplyDay | datetime | Y | 回复交期 |
| ReplyDate | datetime | Y | 回复日期 |
| SuplierReplyDate | datetime | Y | 供方复期 |
| APSDemandDay | datetime | Y | APS 需求日期 |
| DemandToDay1 | varchar(4000) | Y | 需求到料日期合并 |
| MergeCount | int | Y | 合并数 |

#### 采购与物控
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProcurementSection | nvarchar(50) | Y | 采购组 |
| Account | varchar(30) | Y | 账号（物控） |
| ControlID | varchar(5) | Y | 控制者 |
| POTracker | nvarchar(10) | Y | 物控 |
| PMCRemark | nvarchar(500) | Y | PMC 备注 |

#### 组织与 ERP
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeID | int | Y | 组织ID |
| OrganizeName | nvarchar(50) | Y | 组织名称 |
| GroupID | int | Y | 群组ID |
| GroupName | nvarchar(50) | Y | 群组名称 |
| Extend12 | varchar(30) | Y | 工厂 |
| ERPID | varchar(30) | Y | ERP 主键 ID |
| DataSource | nvarchar(20) | Y | 数据来源 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1（库中扩展属性误标为「规则」） |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 40.3 关联关系

- `ItemCode` → `APS_Material.Code`（料号）
- `ResourceNO` → `APS_PO.PODocNo`（采购单号，若对接）
- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `Account` → `Dev_Account.Account`（物控账号）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`（账号）

### 40.4 备注

- **业务用途**：供应商送货计划主数据；与 `APS_DeliveryDataTmp` 字段结构相近，本表含 `DemandToDay1`、PMC/供方复期、配比等扩展
- **与旧版差异**：当前 DDL 无 `MaterialID`、`SuplierCode`/`SuplierName`、`StatusName`、`TransitTime`/`DeliveryRate`/`TestDay`/`TransitDay`、`DemandDay1`/`ReplyDay1` 等列

---

## 41. APS_OrderPlanMaterialFormExclude（齐套物料排除规则表）

齐套物料排除规则表，按组织/群组配置规则字段（RuleType）与规则值（RuleValue），供齐套计算时排除 `APS_OrderPlanMaterialForm` 中符合条件的行。

### 41.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_OrderPlanMaterialFormExclude` |
| 主键 | `ID` (bigint, 自增) |
| 默认值 | Status = 1；CreatedOn、ModifyedOn、SyncDatetime = getdate() |

### 41.2 字段清单

#### 规则与组织
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ID | bigint | NOT NULL | 主键（自增） |
| RuleType | varchar(300) | Y | 规则字段（对应齐套明细上的字段名或业务约定） |
| RuleValue | nvarchar(400) | Y | 规则值 |
| OrganizeID | int | Y | 组织ID |
| OrganizeName | nvarchar(50) | Y | 组织名称 |
| GroupID | int | Y | 群组ID |
| GroupName | nvarchar(50) | Y | 群组名称 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | ERP 主键 ID |
| DataSource | nvarchar(20) | Y | 数据来源 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态（默认 1） |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 41.3 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`（账号）
- 业务上与 `APS_OrderPlanMaterialForm` 的字段按 `RuleType`/`RuleValue` 匹配后排除

### 41.4 备注

- **业务用途**：齐套物料排除规则，控制哪些明细不参与或不出现在齐套结果中
- **扩展属性**：库中 `ID` 列的 MS_Description 为「规则」，实际含义为主键 ID

---

## 42. APS_POScheduling（运算送货分配采购订单临时表）

运算送货分配采购订单临时表，存储送货分配运算过程中的采购单行快照：在途数、供应商、料号、送货日期及分配用在途数等。无聚集主键约束，通常由存储过程写入/清空。

### 42.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_POScheduling` |
| 主键 | 无（DDL 未声明 PK）；`ID` bigint NOT NULL，常对应采购订单行 |

### 42.2 字段清单

| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ID | bigint | NOT NULL | 常与 `APS_PO.ID` 一致 |
| PODocNo | varchar(30) | Y | 采购单号 |
| POLineNo | varchar(30) | Y | 采购单行号 |
| OnloadQty | decimal(18,4) | Y | 在途数 |
| OnloadQty1 | decimal(18,4) | Y | 在途数（用于分配） |
| SupplierCode | varchar(20) | Y | 供应商代码 |
| SupplierName | nvarchar(100) | Y | 供应商名称 |
| Code | varchar(20) | Y | 物料编码 |
| UnitName | nvarchar(20) | Y | 单位 |
| DELIVERYDATE | datetime | Y | 送货日期 |
| Extend1 | varchar(5) | Y | 生产组织代码 |
| CreatedOn | datetime | Y | 创建日期 |
| SyncDatetime | datetime | Y | 同步日期 |

### 42.3 关联关系

- `ID` → `APS_PO.ID`（采购订单行，若运算时写入）
- `PODocNo` / `POLineNo` → `APS_PO.PODocNo` / `APS_PO.POLineNo`（采购单行）
- `SupplierCode` → `Dev_Supplier.Code`（供应商代码）
- `Code` → `APS_Material.Code`（料号）

### 42.4 备注

- **业务用途**：送货分配运算中间数据，非长期主数据表
- **命名**：`DELIVERYDATE` 为数据库列名大写写法

---

## 43. APS_DeliveryDataTmp（供应商送货计划临时表）

供应商送货计划临时表，结构与送货计划主数据相近但字段较少：采购单、料号、采购未交量、欠数、需求/到料/回复日期、采购组与物控等，用于送货计划运算过程中的暂存。表描述：供应商送货计划。

### 43.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_DeliveryDataTmp` |
| 主键 | `ID` (bigint, 自增) |

### 43.2 字段清单

#### 主键与采购单
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ID | bigint | NOT NULL | 主键（自增） |
| POCreateDate | datetime | Y | PO 创建日期 |
| ResourceNO | varchar(30) | Y | 采购单号 |
| LineNum | varchar(20) | Y | 行号 |

#### 料品与数量
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ItemCode | varchar(30) | Y | 料号 |
| ItemName | nvarchar(100) | Y | 物料名称 |
| Spec | nvarchar(300) | Y | 规格 |
| UnitName | nvarchar(10) | Y | 单位 |
| AvailableQty | decimal(18,6) | Y | 采购未交数量 |
| OweQty | decimal(18,6) | Y | 欠数 |

#### 日期与合并
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| DemandDay | datetime | Y | 需求日期 |
| DemandToDay | datetime | Y | 需求到料日期 |
| ReplyDay | datetime | Y | 回复交期 |
| APSDemandDay | datetime | Y | APS 需求日期 |
| DemandToDay1 | varchar(4000) | Y | 需求到料日期合并（多值） |
| MergeCount | int | Y | 合并数 |

#### 采购与组织
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ProcurementSection | nvarchar(50) | Y | 采购组 |
| Account | varchar(30) | Y | 账号（物控） |
| ControlID | varchar(5) | Y | 控制者 |
| OrganizeID | int | Y | 组织ID |
| OrganizeName | nvarchar(50) | Y | 组织名称 |
| GroupID | int | Y | 群组ID |
| GroupName | nvarchar(50) | Y | 群组名称 |

#### ERP 与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态 |
| ERPID | varchar(30) | Y | ERP 主键 ID |
| DataSource | nvarchar(20) | Y | 数据来源 |
| SyncDatetime | datetime | Y | 同步日期 |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1（库中扩展属性误标为「规则」） |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 43.3 关联关系

- `ItemCode` → `APS_Material.Code`（料号）
- `ResourceNO` → `APS_PO.PODocNo`（采购单号，若对接）
- `OrganizeID` → `Dev_Organize.OrganizeID`（组织）
- `Account` → `Dev_Account.Account`（物控账号）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`（账号）

### 43.4 备注

- **业务用途**：供应商送货计划运算临时表，结果可汇总写入 `APS_DeliveryData` / `APS_DeliveryDataDetail` 等
- **与正式表差异**：无 `MaterialID`、`SuplierCode` 等列，以 `ItemCode` 标识料品

---

## 44. APS_TaxRate（税率主数据表）

按税码、组织及生效区间维护税率比例，支持历史版本与按组织/地区限定；**不使用**独立税种字典表，税码由业务约定。

### 44.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_TaxRate` |
| 主键 | `TaxRateID` (bigint, 自增) |
| 默认值 | `IsPercent` = 1，`Status` = 1，`CreatedOn` = getdate() |
| 同步视图 | `V_APS_TaxRate`（`V_`+表名；`SELECT *` 只读） |

### 44.2 字段清单

#### 主键与税率
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| TaxRateID | bigint | NOT NULL | 主键，自增 |
| TaxCode | varchar(20) | NOT NULL | 税码：业务约定的税类编码；单据行存此码，按生效区间解析对应税率 |
| TaxName | nvarchar(100) | Y | 该行税率说明，如：增值税 13% |
| TaxRate | decimal(9,6) | NOT NULL | 税率数值；与 IsPercent 配合：百分数如 13 或小数如 0.13 |
| IsPercent | bit | NOT NULL | TaxRate 含义：1=百分数；0=小数比例 |

#### 组织与地区
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OrganizeID | int | Y | 组织/账套；NULL 表示全集团或全局适用（由业务约定） |
| CountryCode | char(2) | Y | 国家二字码 ISO 3166-1 alpha-2 |
| RegionCode | varchar(20) | Y | 省/州等区域编码 |

#### 生效与状态
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| EffectiveFrom | date | NOT NULL | 生效起始日期（含当日） |
| EffectiveTo | date | Y | 生效结束日期（含当日）；NULL 表示至今有效 |
| Status | int | NOT NULL | **0=草稿，1=确认**（全库约定；主数据「启用」即确认态） |

#### 备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Remark1 | nvarchar(500) | Y | 备注 1 |
| Remark2 | nvarchar(500) | Y | 备注 2 |

#### 审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建人账号 |
| CreatedByName | nvarchar(30) | Y | 创建人姓名 |
| ModifiedBy | varchar(30) | Y | 修改人账号 |
| ModifiedByName | nvarchar(30) | Y | 修改人姓名 |
| CreatedOn | datetime | Y | 创建时间 |
| ModifyedOn | datetime | Y | 最后修改时间 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | 外部 ERP 主键或唯一标识 |
| SyncDatetime | datetime | Y | 从 ERP 同步的时间 |

### 44.3 索引与同步视图

| 对象 | 名称 | 说明 |
|------|------|------|
| 聚集索引 | PK_APS_TaxRate | 主键 |
| 非聚集索引 | IX_APS_TaxRate_Code_Org_From | 按税码、组织、生效起始查询当前或历史税率 |
| 视图 | V_APS_TaxRate | `V_`+表名；`SELECT *` 只读透出基表 |

### 44.4 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`（若使用组织限定）
- 业务单据明细可通过 `TaxCode` + 业务日期 + `OrganizeID` 匹配本表生效区间解析税率（无独立税种表）

### 44.5 扩展属性（SQL）

以下为建表、索引、**同步视图 `V_APS_TaxRate`** 及 **`MS_Description`**（表、全部列、索引、视图）脚本；视图在 `CREATE VIEW` 之前有 **创建人 / 创建日期 / 作用** 注释块。执行前请按环境确认对象是否已存在。

```sql
CREATE TABLE [dbo].[APS_TaxRate](
    [TaxRateID]      BIGINT        IDENTITY(1,1) NOT NULL,
    [TaxCode]        VARCHAR(20)   NOT NULL,
    [TaxName]        NVARCHAR(100) NULL,
    [TaxRate]        DECIMAL(9,6)  NOT NULL,
    [IsPercent]      BIT           NOT NULL CONSTRAINT [DF_APS_TaxRate_IsPercent] DEFAULT (1),
    [OrganizeID]     INT           NULL,
    [CountryCode]    CHAR(2)       NULL,
    [RegionCode]     VARCHAR(20)   NULL,
    [EffectiveFrom]  DATE          NOT NULL,
    [EffectiveTo]    DATE          NULL,
    [Status]         INT           NOT NULL CONSTRAINT [DF_APS_TaxRate_Status] DEFAULT (1),
    [Remark1]        NVARCHAR(500) NULL,
    [Remark2]        NVARCHAR(500) NULL,
    [CreatedBy]      VARCHAR(20)   NULL,
    [CreatedByName]  NVARCHAR(30)  NULL,
    [ModifiedBy]     VARCHAR(30)   NULL,
    [ModifiedByName] NVARCHAR(30)  NULL,
    [CreatedOn]      DATETIME      NULL CONSTRAINT [DF_APS_TaxRate_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]     DATETIME      NULL,
    [ERPID]          VARCHAR(30)   NULL,
    [SyncDatetime]   DATETIME      NULL,
    CONSTRAINT [PK_APS_TaxRate] PRIMARY KEY CLUSTERED ([TaxRateID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_APS_TaxRate_Code_Org_From]
ON [dbo].[APS_TaxRate]([TaxCode], [OrganizeID], [EffectiveFrom]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'税率主数据：按税码、组织及生效区间维护税率比例，支持历史版本与按组织/地区限定。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_TaxRate';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'TaxRateID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'税码：业务约定的税类编码；单据行存此码，按生效区间解析对应税率。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'TaxCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'该行税率说明，如：增值税 13%。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'TaxName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'税率数值；与 IsPercent 配合：IsPercent=1 时表示百分数如 13；IsPercent=0 时表示小数如 0.13。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'TaxRate';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'TaxRate 含义：1=百分数；0=小数比例。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'IsPercent';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织/账套标识；NULL 表示全集团或全局适用，由业务约定。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'国家二字码 ISO 3166-1 alpha-2，可选，用于跨境或多国税制。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'CountryCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'省/州等区域编码，可选。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'RegionCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'生效起始日期（含当日）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'EffectiveFrom';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'生效结束日期（含当日）；NULL 表示至今有效。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'EffectiveTo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建人账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建人姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改人账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改人姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'最后修改时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'外部 ERP 主键或唯一标识，用于对账与同步。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'从 ERP 同步的时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_TaxRate',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按税码、组织、生效起始查询当前或历史税率。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_TaxRate',
    @level2type = N'INDEX',  @level2name = N'IX_APS_TaxRate_Code_Org_From';
GO

/* ---------- 同步视图：V_ + 表名 ---------- */
IF OBJECT_ID(N'dbo.V_APS_TaxRate', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_TaxRate;
GO

/*
  创建人：廖尚华
  创建日期：2025-03-24
  作用：与基表 APS_TaxRate 列一致，供报表、接口等只读查询税率主数据。
*/
CREATE VIEW [dbo].[V_APS_TaxRate]
AS
SELECT *
FROM [dbo].[APS_TaxRate];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：与基表 APS_TaxRate 列一致，供报表、接口等只读查询税率主数据。创建日期：2025-03-24。创建人：廖尚华。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_APS_TaxRate';
GO
```

### 44.6 备注

- **TaxRate + IsPercent**：应用层统一先换算为小数再参与价税计算，避免混用。
- **EffectiveTo = NULL**：表示当前仍有效；维护新税率时可为旧行填写结束日或停用 Status。
- **同步视图**：`V_APS_TaxRate` 仅用于只读；视图为 `SELECT *`，基表 **增删列** 后须执行 `EXEC sp_refreshview N'dbo.V_APS_TaxRate';` 或重建视图，否则元数据可能不一致。
- **变更说明**：已包含 `sp_addextendedproperty`，若仅改注释请用 `sp_updateextendedproperty`；删对象前 `sp_dropextendedproperty`。

---

## 45. APS_ContractPayment（合同付款信息表）

合同的分期/分笔付款计划与执行明细：款项名称、计划金额与日期、实际支付、发票及相对方快照；**一行通常对应合同的一笔付款阶段**。合同主数据表若后续落地，可通过 `ContractID` 关联。

### 45.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_ContractPayment` |
| 主键 | `ContractPaymentID` (bigint, 自增) |
| 默认值 | `PaidAmount` = 0，`PayStatus` = 0，`Status` = 1，`PhaseNo` = 1，`Currency` = `'CNY'`，`CreatedOn` = getdate() |
| 同步视图 | `V_APS_ContractPayment`（`V_`+表名；`SELECT *` 只读） |

### 45.2 字段清单

#### 主键与合同定位
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ContractPaymentID | bigint | NOT NULL | 主键，自增 |
| OrganizeID | int | Y | 组织/账套 |
| ContractID | bigint | Y | 合同主表主键（预留，无表时可空） |
| ContractNo | varchar(50) | NOT NULL | 合同编号（业务唯一标识之一） |
| ContractType | int | Y | 合同性质：1 采购合同，2 销售合同，其他由业务字典约定 |

#### 付款阶段与金额
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PhaseNo | int | NOT NULL | 付款期序（第几笔/第几期，从 1 递增） |
| PaymentItemName | nvarchar(100) | Y | 款项名称，如：预付款、到货款、验收款、质保金 |
| PayPercent | decimal(9,4) | Y | 占合同总金额比例（百分比数值，如 30 表示 30%）；可与 PlanAmount 择一或并用 |
| PlanAmount | decimal(18,2) | Y | 本笔计划应付金额 |
| PaidAmount | decimal(18,2) | NOT NULL | 本笔累计已付金额 |
| Currency | varchar(10) | Y | 币别，默认 CNY |

#### 日期与付款状态
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PlanPayDate | date | Y | 计划付款日期 |
| ActualPayDate | date | Y | 实际付款日期（末次或汇总，由业务约定） |
| PayStatus | int | NOT NULL | 付款执行状态：0 未付，1 部分付款，2 已付，3 逾期等（可扩展） |
| InvoiceNo | nvarchar(100) | Y | 发票号（或主要票据号） |

#### 相对方（快照）
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PartnerCode | varchar(60) | Y | 相对方编码（供应商/客户等，与合同一致时点快照） |
| PartnerName | nvarchar(200) | Y | 相对方名称快照 |

#### 备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Remark1 | nvarchar(500) | Y | 备注 1 |
| Remark2 | nvarchar(500) | Y | 备注 2 |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | NOT NULL | **0=草稿，1=确认**（全库约定；付款行确认后生效） |
| CreatedBy | varchar(20) | Y | 创建人账号 |
| CreatedByName | nvarchar(30) | Y | 创建人姓名 |
| ModifiedBy | varchar(30) | Y | 修改人账号 |
| ModifiedByName | nvarchar(30) | Y | 修改人姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | 外部 ERP 主键或唯一标识 |
| SyncDatetime | datetime | Y | 从 ERP 同步的时间 |

### 45.3 索引与同步视图

| 对象 | 名称 | 说明 |
|------|------|------|
| 聚集索引 | PK_APS_ContractPayment | 主键 |
| 非聚集索引 | IX_APS_ContractPayment_Contract | 按组织、合同、期次查询付款计划与执行 |
| 视图 | V_APS_ContractPayment | `V_`+表名；`SELECT *` 只读透出基表 |

### 45.4 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`
- `ContractID` → 销售场景下 **`APS_SalesContract.SalesContractID`**（§49）；采购场景合同主表由业务另定
- `PartnerCode` 可与 `Dev_Supplier.Code` 或客户主数据对照（按合同类型）

### 45.5 扩展属性（SQL）

以下为建表、索引、**同步视图 `V_APS_ContractPayment`** 及 **`MS_Description`**（表、全部列、索引、视图）脚本；视图在 `CREATE VIEW` 之前有 **创建人 / 创建日期 / 作用** 注释块。执行前请按环境确认对象是否已存在。

```sql
CREATE TABLE [dbo].[APS_ContractPayment](
    [ContractPaymentID] BIGINT         IDENTITY(1,1) NOT NULL,
    [OrganizeID]        INT            NULL,
    [ContractID]        BIGINT         NULL,
    [ContractNo]        VARCHAR(50)    NOT NULL,
    [ContractType]      INT            NULL,
    [PhaseNo]           INT            NOT NULL CONSTRAINT [DF_APS_ContractPayment_PhaseNo] DEFAULT (1),
    [PaymentItemName]   NVARCHAR(100)  NULL,
    [PayPercent]        DECIMAL(9,4)   NULL,
    [PlanAmount]        DECIMAL(18,2)  NULL,
    [PaidAmount]        DECIMAL(18,2)  NOT NULL CONSTRAINT [DF_APS_ContractPayment_PaidAmount] DEFAULT (0),
    [Currency]          VARCHAR(10)    NULL CONSTRAINT [DF_APS_ContractPayment_Currency] DEFAULT ('CNY'),
    [PlanPayDate]       DATE           NULL,
    [ActualPayDate]     DATE           NULL,
    [PayStatus]         INT            NOT NULL CONSTRAINT [DF_APS_ContractPayment_PayStatus] DEFAULT (0),
    [InvoiceNo]         NVARCHAR(100)  NULL,
    [PartnerCode]       VARCHAR(60)    NULL,
    [PartnerName]       NVARCHAR(200)  NULL,
    [Remark1]           NVARCHAR(500)  NULL,
    [Remark2]           NVARCHAR(500)  NULL,
    [Status]            INT            NOT NULL CONSTRAINT [DF_APS_ContractPayment_Status] DEFAULT (1),
    [CreatedBy]         VARCHAR(20)    NULL,
    [CreatedByName]     NVARCHAR(30)   NULL,
    [ModifiedBy]        VARCHAR(30)    NULL,
    [ModifiedByName]    NVARCHAR(30)   NULL,
    [CreatedOn]         DATETIME       NULL CONSTRAINT [DF_APS_ContractPayment_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]        DATETIME       NULL,
    [ERPID]             VARCHAR(30)    NULL,
    [SyncDatetime]      DATETIME       NULL,
    CONSTRAINT [PK_APS_ContractPayment] PRIMARY KEY CLUSTERED ([ContractPaymentID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_APS_ContractPayment_Contract]
ON [dbo].[APS_ContractPayment]([OrganizeID], [ContractNo], [PhaseNo]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'合同付款信息：记录合同分期/分笔付款计划、计划与实际金额日期、发票及相对方快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_ContractPayment';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'ContractPaymentID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织/账套。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'合同主表主键，预留；无合同主表时可空。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'ContractID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'合同编号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'ContractNo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'合同性质：1采购 2销售，其他由业务约定。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'ContractType';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'付款期序（第几笔），从 1 递增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'PhaseNo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'款项名称，如预付款、验收款。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'PaymentItemName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'占合同总金额比例（百分比数值，如 30 表示 30%）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'PayPercent';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'本笔计划应付金额。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'PlanAmount';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'本笔累计已付金额。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'PaidAmount';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'币别，默认 CNY。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'Currency';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'计划付款日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'PlanPayDate';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'实际付款日期（末次或汇总，由业务约定）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'ActualPayDate';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'付款执行状态：0未付 1部分 2已付 3逾期等。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'PayStatus';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'发票号或主要票据号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'InvoiceNo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'相对方编码（与合同一致时的快照）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'PartnerCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'相对方名称快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'PartnerName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建人账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建人姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改人账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改人姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'外部 ERP 主键或唯一标识，用于对账与同步。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'从 ERP 同步的时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractPayment',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按组织、合同编号、付款期序查询付款计划。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_ContractPayment',
    @level2type = N'INDEX',  @level2name = N'IX_APS_ContractPayment_Contract';
GO

/* ---------- 同步视图：V_ + 表名 ---------- */
IF OBJECT_ID(N'dbo.V_APS_ContractPayment', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_ContractPayment;
GO

/*
  创建人：廖尚华
  创建日期：2025-03-24
  作用：与基表 APS_ContractPayment 列一致，供报表、接口等只读查询合同付款计划与执行情况。
*/
CREATE VIEW [dbo].[V_APS_ContractPayment]
AS
SELECT *
FROM [dbo].[APS_ContractPayment];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：与基表 APS_ContractPayment 列一致，供报表、接口等只读查询合同付款计划与执行情况。创建日期：2025-03-24。创建人：廖尚华。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_APS_ContractPayment';
GO
```

### 45.6 备注

- **唯一性**：建议业务层保证同一 `OrganizeID` + `ContractNo` + `PhaseNo` 不重复；需要时可在库上加唯一索引。
- **PayPercent 与 PlanAmount**： `PayPercent` 为占合同总额比例时请与合同总金额口径一致；也可仅用 `PlanAmount` 固定本笔金额。
- **PayStatus**：与 `PaidAmount`、`PlanAmount` 的更新宜在同一事务或同一应用规则内维护，避免状态与金额不一致。
- **同步视图**：`V_APS_ContractPayment` 仅用于只读；视图为 `SELECT *`，基表 **增删列** 后须执行 `EXEC sp_refreshview N'dbo.V_APS_ContractPayment';` 或重建视图。

---

## 46. APS_CompanyPaymentAccount（本公司付款账号信息表）

维护本公司用于对外**付款/收款**的银行账号主数据：开户行、账号、币别、联行号及默认账户标记等；可与组织、合同付款方向、ERP 银行主数据对照。

### 46.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_CompanyPaymentAccount` |
| 主键 | `CompanyPaymentAccountID` (bigint, 自增) |
| 默认值 | `Status` = 1，`Currency` = `'CNY'`，`IsDefaultPay` / `IsDefaultReceive` = 0，`CreatedOn` = getdate() |
| 同步视图 | `V_APS_CompanyPaymentAccount`（`V_`+表名；`SELECT *` 只读） |

### 46.2 字段清单

#### 主键与归属
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CompanyPaymentAccountID | bigint | NOT NULL | 主键，自增 |
| OrganizeID | int | Y | 所属组织/账套；NULL 表示全集团共用（由业务约定） |
| AccountCode | varchar(30) | Y | 内部账户编码，便于单据、接口引用 |

#### 账户与银行信息
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| AccountName | nvarchar(200) | NOT NULL | 户名（账户名称） |
| BankName | nvarchar(200) | NOT NULL | 开户银行 |
| BankBranch | nvarchar(200) | Y | 开户支行/网点 |
| BankAccountNo | varchar(50) | NOT NULL | 银行账号 |
| Currency | varchar(10) | Y | 币别，默认 CNY |
| CNAPSCode | varchar(20) | Y | 联行号（人行） |
| SwiftCode | varchar(20) | Y | SWIFT/BIC（跨境时使用） |

#### 用途与标志
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| AccountPurpose | int | Y | 用途：1 主要付款，2 主要收款，3 收付共用（由业务字典可扩展） |
| AccountType | int | Y | 账户类型：1 对公，2 对私（由业务约定） |
| IsDefaultPay | bit | NOT NULL | 是否默认付款户（同组织下业务约定唯一） |
| IsDefaultReceive | bit | NOT NULL | 是否默认收款户（同组织下业务约定唯一） |

#### 状态与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | NOT NULL | **0=草稿，1=确认**（全库约定） |
| Remark1 | nvarchar(500) | Y | 备注 1 |
| Remark2 | nvarchar(500) | Y | 备注 2 |

#### 审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建人账号 |
| CreatedByName | nvarchar(30) | Y | 创建人姓名 |
| ModifiedBy | varchar(30) | Y | 修改人账号 |
| ModifiedByName | nvarchar(30) | Y | 修改人姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | 外部 ERP 主键或唯一标识 |
| SyncDatetime | datetime | Y | 从 ERP 同步的时间 |

### 46.3 索引与同步视图

| 对象 | 名称 | 说明 |
|------|------|------|
| 聚集索引 | PK_APS_CompanyPaymentAccount | 主键 |
| 非聚集索引 | IX_APS_CompanyPaymentAccount_Org | 按组织、内部编码查询 |
| 非聚集索引 | IX_APS_CompanyPaymentAccount_Org_AccountNo | 按组织、银行账号查询（防重复可在此加唯一约束） |
| 视图 | V_APS_CompanyPaymentAccount | `SELECT *` 只读透出基表 |

### 46.4 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`
- 合同/付款单可选存 `AccountCode` 或 `BankAccountNo` 与本表对照

### 46.5 扩展属性与对象脚本（表 + 索引 + 视图）

以下为 **建表、索引、同步视图 `V_APS_CompanyPaymentAccount`** 及 **`MS_Description`**；视图在 `CREATE VIEW` 前有 **创建人 / 创建日期 / 作用** 注释块；视图体为 **`SELECT *`**。

```sql
CREATE TABLE [dbo].[APS_CompanyPaymentAccount](
    [CompanyPaymentAccountID] BIGINT        IDENTITY(1,1) NOT NULL,
    [OrganizeID]              INT           NULL,
    [AccountCode]             VARCHAR(30)   NULL,
    [AccountName]             NVARCHAR(200) NOT NULL,
    [BankName]                NVARCHAR(200) NOT NULL,
    [BankBranch]              NVARCHAR(200) NULL,
    [BankAccountNo]           VARCHAR(50)   NOT NULL,
    [Currency]                VARCHAR(10)   NULL CONSTRAINT [DF_APS_CompanyPaymentAccount_Currency] DEFAULT ('CNY'),
    [CNAPSCode]               VARCHAR(20)   NULL,
    [SwiftCode]               VARCHAR(20)   NULL,
    [AccountPurpose]          INT           NULL,
    [AccountType]             INT           NULL,
    [IsDefaultPay]            BIT           NOT NULL CONSTRAINT [DF_APS_CompanyPaymentAccount_IsDefaultPay] DEFAULT (0),
    [IsDefaultReceive]        BIT           NOT NULL CONSTRAINT [DF_APS_CompanyPaymentAccount_IsDefaultReceive] DEFAULT (0),
    [Status]                  INT           NOT NULL CONSTRAINT [DF_APS_CompanyPaymentAccount_Status] DEFAULT (1),
    [Remark1]                 NVARCHAR(500) NULL,
    [Remark2]                 NVARCHAR(500) NULL,
    [CreatedBy]               VARCHAR(20)   NULL,
    [CreatedByName]           NVARCHAR(30)  NULL,
    [ModifiedBy]              VARCHAR(30)   NULL,
    [ModifiedByName]          NVARCHAR(30)  NULL,
    [CreatedOn]               DATETIME      NULL CONSTRAINT [DF_APS_CompanyPaymentAccount_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]              DATETIME      NULL,
    [ERPID]                   VARCHAR(30)   NULL,
    [SyncDatetime]            DATETIME      NULL,
    CONSTRAINT [PK_APS_CompanyPaymentAccount] PRIMARY KEY CLUSTERED ([CompanyPaymentAccountID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_APS_CompanyPaymentAccount_Org]
ON [dbo].[APS_CompanyPaymentAccount]([OrganizeID], [AccountCode]);
GO

CREATE NONCLUSTERED INDEX [IX_APS_CompanyPaymentAccount_Org_AccountNo]
ON [dbo].[APS_CompanyPaymentAccount]([OrganizeID], [BankAccountNo]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'本公司付款/收款银行账号主数据：开户行、账号、币别、联行号及默认收付户标记。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_CompanyPaymentAccount';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'CompanyPaymentAccountID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'所属组织/账套；NULL 表示全集团共用，由业务约定。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'内部账户编码，便于单据与接口引用。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'AccountCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'户名（账户名称）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'AccountName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'开户银行。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'BankName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'开户支行或网点。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'BankBranch';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'银行账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'BankAccountNo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'币别，默认 CNY。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'Currency';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'联行号（人行）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'CNAPSCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'SWIFT/BIC，跨境时使用。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'SwiftCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'用途：1付款 2收款 3收付共用等，由业务约定。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'AccountPurpose';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'账户类型：1对公 2对私。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'AccountType';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'是否默认付款户（同组织宜唯一，由应用或约束保证）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'IsDefaultPay';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'是否默认收款户（同组织宜唯一，由应用或约束保证）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'IsDefaultReceive';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建人账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建人姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改人账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改人姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'外部 ERP 主键或唯一标识，用于对账与同步。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'从 ERP 同步的时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按组织、内部账户编码查询本公司付款账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'INDEX',  @level2name = N'IX_APS_CompanyPaymentAccount_Org';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按组织、银行账号查询本公司付款账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_CompanyPaymentAccount',
    @level2type = N'INDEX',  @level2name = N'IX_APS_CompanyPaymentAccount_Org_AccountNo';
GO

/* ---------- 同步视图：V_ + 表名 ---------- */
IF OBJECT_ID(N'dbo.V_APS_CompanyPaymentAccount', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_CompanyPaymentAccount;
GO

/*
  创建人：廖尚华
  创建日期：2025-03-24
  作用：与基表 APS_CompanyPaymentAccount 列一致，供报表、接口等只读查询本公司银行付款/收款账号。
*/
CREATE VIEW [dbo].[V_APS_CompanyPaymentAccount]
AS
SELECT *
FROM [dbo].[APS_CompanyPaymentAccount];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：与基表 APS_CompanyPaymentAccount 列一致，供报表、接口等只读查询本公司银行付款/收款账号。创建日期：2025-03-24。创建人：廖尚华。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_APS_CompanyPaymentAccount';
GO
```

### 46.6 备注

- **唯一性**：同一 `OrganizeID`（含 NULL 约定）下 `BankAccountNo` 建议业务或唯一索引不重复。
- **默认户**：`IsDefaultPay` / `IsDefaultReceive` 在同一组织内建议由应用保证各至多一行，或采用过滤唯一索引。
- **同步视图**：`V_APS_CompanyPaymentAccount` 为 `SELECT *`，基表 **增删列** 后须 `EXEC sp_refreshview N'dbo.V_APS_CompanyPaymentAccount';` 或重建视图。

---

## 47. APS_ContractTerms（合同条款信息表）

按合同维护**条款主数据**：顺序、标题、正文、条款分类、必备标记及生效区间；与 `APS_ContractPayment` 等同属合同域，通过 `ContractNo` / 预留 `ContractID` 与后续合同主表衔接。

### 47.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_ContractTerms` |
| 主键 | `ContractTermsID` (bigint, 自增) |
| 默认值 | `ClauseSeq` = 1，`IsMandatory` = 1，`Status` = 1，`CreatedOn` = getdate() |
| 同步视图 | `V_APS_ContractTerms`（`V_`+表名；`SELECT *` 只读） |

### 47.2 字段清单

#### 主键与合同定位
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ContractTermsID | bigint | NOT NULL | 主键，自增 |
| OrganizeID | int | Y | 组织/账套 |
| ContractID | bigint | Y | 合同主表主键（预留） |
| ContractNo | varchar(50) | NOT NULL | 合同编号 |

#### 条款内容
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ClauseSeq | int | NOT NULL | 条款顺序（第几条），用于展示排序 |
| ClauseCode | varchar(30) | Y | 条款编码（内部或模板编码） |
| ClauseTitle | nvarchar(200) | NOT NULL | 条款标题 |
| ClauseContent | nvarchar(MAX) | Y | 条款正文（长文本） |
| TermsCategory | int | Y | 条款大类：1 付款，2 交货，3 质量，4 违约/责任，5 保密，6 其它（可扩展） |
| IsMandatory | bit | NOT NULL | 是否必备条款 |

#### 生效与状态
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| EffectiveFrom | date | Y | 条款生效起始日（可选，与合同生效对齐由业务约定） |
| EffectiveTo | date | Y | 条款生效结束日；NULL 表示至今 |
| Status | int | NOT NULL | **0=草稿，1=确认**（全库约定） |

#### 备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Remark1 | nvarchar(500) | Y | 备注 1 |
| Remark2 | nvarchar(500) | Y | 备注 2 |

#### 审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(20) | Y | 创建人账号 |
| CreatedByName | nvarchar(30) | Y | 创建人姓名 |
| ModifiedBy | varchar(30) | Y | 修改人账号 |
| ModifiedByName | nvarchar(30) | Y | 修改人姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | 外部 ERP 主键或唯一标识 |
| SyncDatetime | datetime | Y | 从 ERP 同步的时间 |

### 47.3 索引与同步视图

| 对象 | 名称 | 说明 |
|------|------|------|
| 聚集索引 | PK_APS_ContractTerms | 主键 |
| 非聚集索引 | IX_APS_ContractTerms_Contract | 按组织、合同编号、条款顺序查询 |
| 视图 | V_APS_ContractTerms | `SELECT *` 只读透出基表 |

### 47.4 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`
- `ContractNo` 与 `APS_ContractPayment.ContractNo` 等业务表同号关联（无强制外键）
- `ContractID` → **`APS_SalesContract.SalesContractID`**（销售合同，§49）
- `ContractTermsID` ← **`APS_SalesContractDetail.ContractTermsID`**（可选，销售合同头与条款行的关联，§50）

### 47.5 表、索引、视图及扩展属性（完整脚本）

以下为 **建表、索引、同步视图 `V_APS_ContractTerms`** 及表/列/索引/视图的 **`MS_Description`**。视图在 `CREATE VIEW` 前有 **创建人 / 创建日期 / 作用** 注释；视图体 **`SELECT *`**。

```sql
CREATE TABLE [dbo].[APS_ContractTerms](
    [ContractTermsID] BIGINT        IDENTITY(1,1) NOT NULL,
    [OrganizeID]      INT           NULL,
    [ContractID]      BIGINT        NULL,
    [ContractNo]      VARCHAR(50)   NOT NULL,
    [ClauseSeq]       INT           NOT NULL CONSTRAINT [DF_APS_ContractTerms_ClauseSeq] DEFAULT (1),
    [ClauseCode]      VARCHAR(30)   NULL,
    [ClauseTitle]     NVARCHAR(200) NOT NULL,
    [ClauseContent]   NVARCHAR(MAX) NULL,
    [TermsCategory]   INT           NULL,
    [IsMandatory]     BIT           NOT NULL CONSTRAINT [DF_APS_ContractTerms_IsMandatory] DEFAULT (1),
    [EffectiveFrom]   DATE          NULL,
    [EffectiveTo]     DATE          NULL,
    [Status]          INT           NOT NULL CONSTRAINT [DF_APS_ContractTerms_Status] DEFAULT (1),
    [Remark1]         NVARCHAR(500) NULL,
    [Remark2]         NVARCHAR(500) NULL,
    [CreatedBy]       VARCHAR(20)   NULL,
    [CreatedByName]   NVARCHAR(30)  NULL,
    [ModifiedBy]      VARCHAR(30)   NULL,
    [ModifiedByName]  NVARCHAR(30)  NULL,
    [CreatedOn]       DATETIME      NULL CONSTRAINT [DF_APS_ContractTerms_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]      DATETIME      NULL,
    [ERPID]           VARCHAR(30)   NULL,
    [SyncDatetime]    DATETIME      NULL,
    CONSTRAINT [PK_APS_ContractTerms] PRIMARY KEY CLUSTERED ([ContractTermsID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_APS_ContractTerms_Contract]
ON [dbo].[APS_ContractTerms]([OrganizeID], [ContractNo], [ClauseSeq]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'合同条款信息：顺序、标题、正文、分类、必备标记及生效区间，按合同编号维护。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_ContractTerms';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'ContractTermsID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织/账套。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'合同主表主键，预留。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'ContractID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'合同编号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'ContractNo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'条款顺序（第几条），用于列表与打印排序。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'ClauseSeq';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'条款编码（模板或内部编码）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'ClauseCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'条款标题。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'ClauseTitle';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'条款正文（长文本）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'ClauseContent';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'条款大类：1付款 2交货 3质量 4违约 5保密 6其它等。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'TermsCategory';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'是否必备条款。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'IsMandatory';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'条款生效起始日。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'EffectiveFrom';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'条款生效结束日；NULL 表示至今。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'EffectiveTo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建人账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建人姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改人账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改人姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'外部 ERP 主键或唯一标识，用于对账与同步。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'从 ERP 同步的时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_ContractTerms',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按组织、合同编号、条款顺序查询合同条款。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_ContractTerms',
    @level2type = N'INDEX',  @level2name = N'IX_APS_ContractTerms_Contract';
GO

/* ---------- 同步视图：V_ + 表名 ---------- */
IF OBJECT_ID(N'dbo.V_APS_ContractTerms', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_ContractTerms;
GO

/*
  创建人：廖尚华
  创建日期：2025-03-24
  作用：与基表 APS_ContractTerms 列一致，供报表、接口等只读查询合同条款信息。
*/
CREATE VIEW [dbo].[V_APS_ContractTerms]
AS
SELECT *
FROM [dbo].[APS_ContractTerms];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：与基表 APS_ContractTerms 列一致，供报表、接口等只读查询合同条款信息。创建日期：2025-03-24。创建人：廖尚华。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_APS_ContractTerms';
GO
```

### 47.6 备注

- **唯一性**：建议业务保证同一 `OrganizeID` + `ContractNo` + `ClauseSeq`（或 + `ClauseCode`）不重复，必要时加唯一索引。
- **长文本**：`ClauseContent` 为 `nvarchar(max)`，注意报表/列表查询避免默认 `SELECT *` 拖慢。
- **同步视图**：`V_APS_ContractTerms` 为 `SELECT *`，基表 **增删列** 后须 `EXEC sp_refreshview N'dbo.V_APS_ContractTerms';` 或重建视图。

---

## 48. APS_SalesOrderReturn（销售订单退货表）

记录相对 **`APS_SalesOrderDetail`** 的退货明细：**一行退货业务**对应一行（可多次退同一销售行，多行记录）；键类型与明细一致（`SalesOrderID`、`SalesOrderDetailID` 为 `varchar(20)`）。建议在应用或任务中同步回写 **`APS_SalesOrderDetail.SalesReturnQty`** 累计。

### 48.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_SalesOrderReturn` |
| 主键 | `SalesOrderReturnID` (bigint, 自增) |
| 默认值 | `Status` = 1，`CreatedOn` = getdate() |
| 同步视图 | `V_APS_SalesOrderReturn` |

### 48.2 字段清单

#### 主键与来源订单
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SalesOrderReturnID | bigint | NOT NULL | 主键，自增 |
| OrganizeID | int | Y | 组织 ID |
| SalesOrderID | varchar(20) | NOT NULL | 销售订单头 ID → APS_SalesOrder |
| SalesOrderDetailID | varchar(20) | NOT NULL | 销售订单行 ID → APS_SalesOrderDetail |
| SourceLineNum | varchar(10) | Y | 来源订单行号快照（对应明细 LineNum） |

#### 退货单据与数量金额
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ReturnDocNo | varchar(30) | Y | 退货单/退货通知单号（多行可共用一单号） |
| ReturnLineNum | varchar(10) | Y | 退货明细行号 |
| MaterialID | bigint | Y | 料品 ID（与明细一致时点快照） |
| ReturnQty | decimal(18,4) | NOT NULL | 本次退货数量（与销售行 Qty 单位一致） |
| Price | decimal(18,4) | Y | 单价快照 |
| TaxPrice | decimal(18,4) | Y | 含税单价快照 |
| TaxRate | decimal(18,4) | Y | 税率快照 |
| ReturnAmount | decimal(18,4) | Y | 退货金额（含税或不含税由业务约定填入） |

#### 业务属性
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ReturnDate | date | Y | 退货业务日期 |
| ReturnType | int | Y | 退货类型：1 仅退款，2 退货退款，3 换货等 |
| ReturnReasonCode | varchar(20) | Y | 退货原因编码 |
| ReturnReason | nvarchar(200) | Y | 退货原因说明 |
| WarehouseCode | varchar(50) | Y | 退货入仓编码（可选） |
| Status | int | NOT NULL | **0=草稿，1=确认**（全库约定；原 2/3 等多节点须迁移或改用其它字段） |

#### 备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Remark1 | nvarchar(500) | Y | 备注 1 |
| Remark2 | nvarchar(500) | Y | 备注 2 |

#### 审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(30) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | 外部 ERP 行主键或唯一标识 |
| SyncDatetime | datetime | Y | 从 ERP 同步的时间 |

### 48.3 索引与同步视图

| 对象 | 名称 | 说明 |
|------|------|------|
| 聚集索引 | PK_APS_SalesOrderReturn | 主键 |
| 非聚集索引 | IX_APS_SalesOrderReturn_Detail | 按销售订单行查退货 |
| 非聚集索引 | IX_APS_SalesOrderReturn_Doc | 按订单头 + 退货单号分组 |
| 视图 | V_APS_SalesOrderReturn | `SELECT *` 只读 |

### 48.4 关联关系

- `SalesOrderID` → `APS_SalesOrder.SalesOrderID`
- `SalesOrderDetailID` → `APS_SalesOrderDetail.SalesOrderDetailID`
- `MaterialID` → `APS_Material.MaterialID`
- `OrganizeID` → `Dev_Organize.OrganizeID`

### 48.5 表、索引、视图及扩展属性（完整脚本）

```sql
CREATE TABLE [dbo].[APS_SalesOrderReturn](
    [SalesOrderReturnID] BIGINT        IDENTITY(1,1) NOT NULL,
    [OrganizeID]         INT           NULL,
    [SalesOrderID]       VARCHAR(20)   NOT NULL,
    [SalesOrderDetailID] VARCHAR(20)   NOT NULL,
    [SourceLineNum]      VARCHAR(10)   NULL,
    [ReturnDocNo]        VARCHAR(30)   NULL,
    [ReturnLineNum]      VARCHAR(10)   NULL,
    [MaterialID]         BIGINT        NULL,
    [ReturnQty]          DECIMAL(18,4) NOT NULL,
    [Price]              DECIMAL(18,4) NULL,
    [TaxPrice]           DECIMAL(18,4) NULL,
    [TaxRate]            DECIMAL(18,4) NULL,
    [ReturnAmount]       DECIMAL(18,4) NULL,
    [ReturnDate]         DATE          NULL,
    [ReturnType]         INT           NULL,
    [ReturnReasonCode]   VARCHAR(20)   NULL,
    [ReturnReason]       NVARCHAR(200) NULL,
    [WarehouseCode]      VARCHAR(50)   NULL,
    [Status]             INT           NOT NULL CONSTRAINT [DF_APS_SalesOrderReturn_Status] DEFAULT (1),
    [Remark1]            NVARCHAR(500) NULL,
    [Remark2]            NVARCHAR(500) NULL,
    [CreatedBy]          VARCHAR(30)   NULL,
    [CreatedByName]      NVARCHAR(30)  NULL,
    [ModifiedBy]         VARCHAR(30)   NULL,
    [ModifiedByName]     NVARCHAR(30)  NULL,
    [CreatedOn]          DATETIME      NULL CONSTRAINT [DF_APS_SalesOrderReturn_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]         DATETIME      NULL,
    [ERPID]              VARCHAR(30)   NULL,
    [SyncDatetime]       DATETIME      NULL,
    CONSTRAINT [PK_APS_SalesOrderReturn] PRIMARY KEY CLUSTERED ([SalesOrderReturnID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesOrderReturn_Detail]
ON [dbo].[APS_SalesOrderReturn]([SalesOrderDetailID]);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesOrderReturn_Doc]
ON [dbo].[APS_SalesOrderReturn]([SalesOrderID], [ReturnDocNo]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'销售订单退货：按销售订单明细记录退货数量、价税快照及退货单分组。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesOrderReturn';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'SalesOrderReturnID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织 ID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'销售订单头 ID，对应 APS_SalesOrder.SalesOrderID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'SalesOrderID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'销售订单行 ID，对应 APS_SalesOrderDetail.SalesOrderDetailID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'SalesOrderDetailID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'来源销售订单行号快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'SourceLineNum';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'退货单/退货通知单号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'ReturnDocNo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'退货明细行号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'ReturnLineNum';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'料品 ID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'MaterialID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'本次退货数量（与销售行数量单位一致）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'ReturnQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'单价快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'Price';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'含税单价快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'TaxPrice';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'税率快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'TaxRate';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'退货金额（含税或不含税由业务约定）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'ReturnAmount';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'退货业务日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'ReturnDate';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'退货类型：1仅退款 2退货退款 3换货等。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'ReturnType';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'退货原因编码。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'ReturnReasonCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'退货原因说明。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'ReturnReason';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'退货入仓编码（可选）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'WarehouseCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'外部 ERP 行主键或唯一标识。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'从 ERP 同步的时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderReturn',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按销售订单行查询退货明细。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesOrderReturn',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesOrderReturn_Detail';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按销售订单与退货单号分组查询。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesOrderReturn',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesOrderReturn_Doc';
GO

IF OBJECT_ID(N'dbo.V_APS_SalesOrderReturn', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_SalesOrderReturn;
GO

/*
  创建人：廖尚华
  创建日期：2026-03-25
  作用：与基表 APS_SalesOrderReturn 列一致，供报表、接口等只读查询销售订单退货明细。
*/
CREATE VIEW [dbo].[V_APS_SalesOrderReturn]
AS
SELECT *
FROM [dbo].[APS_SalesOrderReturn];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：与基表 APS_SalesOrderReturn 列一致，供报表、接口等只读查询销售订单退货明细。创建日期：2026-03-25。创建人：廖尚华。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_APS_SalesOrderReturn';
GO
```

### 48.6 备注

- **与 §54 / §55**：**`APS_SalesReturnDetail.SalesOrderReturnID`**（可选）可指向本表行，用于将**按出库单组织的销售退货明细**与**订单行退货流水**对齐；无此字段时不影响本表独立使用。详见 **§55.6**。
- **与明细关系**：同一 `SalesOrderDetailID` 可对应多条退货行；累计退货不宜超过可退数量（由应用校验，`APS_SalesOrderDetail.SalesReturnQty` 作汇总参考）。
- **价税**：`ReturnAmount` 口径（含税/不含税）须在业务与 ERP 对接规则中统一。
- **Status**：全库 **0草稿 1确认**，**列默认 `DEFAULT (1)`**（新插入即确认；要先草稿须显式 `0`）。历史库若存 2/3 等值须迁移或映射后再与文档一致。
- **同步视图**：`SELECT *`，基表增删列后执行 `EXEC sp_refreshview N'dbo.V_APS_SalesOrderReturn';`。

---

## 49. APS_SalesContract（销售合同表）

**销售合同主数据**（头表）：合同编号、客户、金额与生效区间、签约信息及与销售订单的可选关联；**与 `APS_ContractTerms`、`APS_ContractPayment` 通过 `ContractNo` 及 `ContractID`（= `SalesContractID`）衔接**（采购合同不在本表）。

### 49.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_SalesContract` |
| 主键 | `SalesContractID` (bigint, 自增) |
| 业务键 | `ContractNo` + `OrganizeID`（建议唯一，见备注） |
| 默认值 | `Status` = 1，`Currency` = `'CNY'`，`CreatedOn` = getdate() |
| 同步视图 | `V_APS_SalesContract`（JOIN 组织、销售订单、父合同；显式关键列，§49.5） |

### 49.2 字段清单

#### 主键与编号
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SalesContractID | bigint | NOT NULL | 主键，自增；子表 `ContractID` 对应本列 |
| OrganizeID | int | Y | 组织/账套 |
| ContractNo | varchar(50) | NOT NULL | 合同编号（业务键，与条款/付款表同号） |
| ContractName | nvarchar(200) | Y | 合同名称 |
| ContractType | int | Y | 合同性质：1 框架合同，2 单笔销售合同等（可扩展） |
| VersionNo | varchar(20) | Y | 合同文本版本号 |
| ParentContractID | bigint | Y | 主合同 ID（补充协议/变更指向，自关联） |

#### 客户与关联订单
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CustomerID | bigint | Y | 客户 ID（与 `APS_SalesOrder.CustomerID` 口径一致，可空） |
| CustomerCode | varchar(60) | Y | 客户编码快照 |
| CustomerName | nvarchar(200) | Y | 客户名称快照 |
| SalesOrderID | varchar(20) | Y | 关联销售订单（可选，`APS_SalesOrder.SalesOrderID`） |

#### 金额与币别
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Currency | varchar(10) | Y | 币别，默认 CNY |
| TotalAmount | decimal(18,2) | Y | 合同总额（口径由业务约定含税/不含税） |
| TaxAmount | decimal(18,2) | Y | 税额 |
| AmountExclTax | decimal(18,2) | Y | 不含税金额（可选，与总额互算） |

#### 日期与签约
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| EffectiveDate | date | Y | 生效日期 |
| ExpiryDate | date | Y | 到期日期 |
| SignDate | date | Y | 签署日期 |
| SignPlace | nvarchar(200) | Y | 签署地点 |
| OurLegalEntity | nvarchar(200) | Y | 我方签约主体 |

#### 商务摘要
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SalesPersonAccount | varchar(30) | Y | 业务员账号 |
| DepartmentName | nvarchar(100) | Y | 部门 |
| PaymentTermSummary | nvarchar(500) | Y | 付款条件摘要 |
| DeliveryTermSummary | nvarchar(500) | Y | 交货条件摘要 |

#### 状态与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | NOT NULL | **0=草稿，1=确认**（全库约定；合同终止建议用到期/补充字段表达，勿另占枚举除非 §备注扩展） |
| Remark1 | nvarchar(500) | Y | 备注 1 |
| Remark2 | nvarchar(500) | Y | 备注 2 |

#### 审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(30) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | 外部 ERP 合同主键 |
| SyncDatetime | datetime | Y | 同步时间 |

### 49.3 索引与同步视图

| 对象 | 名称 | 说明 |
|------|------|------|
| 聚集索引 | PK_APS_SalesContract | 主键 |
| 非聚集索引 | IX_APS_SalesContract_Org_No | 组织 + 合同号 |
| 非聚集索引 | IX_APS_SalesContract_Customer | 组织 + 客户编码 |
| 非聚集索引 | IX_APS_SalesContract_SalesOrder | 销售订单（可空列，筛选用） |
| 视图 | V_APS_SalesContract | **JOIN** `Dev_Organize`、`APS_SalesOrder`、父合同；**显式关键列** |

`V_APS_SalesContract` **输出列**（与脚本一致）：`SalesContractID`，`OrganizeID`，`OrganizeName`，`ContractNo`，`ContractName`，`ContractType`，`VersionNo`，`ParentContractID`，`ParentContractNo`，`ParentContractName`，`CustomerID`，`CustomerCode`，`CustomerName`，`SalesOrderID`，`SalesOrderNo`，`SalesOrderDate`，`SalesOrderStatus`，`SalesOrderDocStatus`，`SalesOrderSalesMan`，`Currency`，`TotalAmount`，`TaxAmount`，`AmountExclTax`，`EffectiveDate`，`ExpiryDate`，`SignDate`，`SignPlace`，`OurLegalEntity`，`SalesPersonAccount`，`DepartmentName`，`PaymentTermSummary`，`DeliveryTermSummary`，`ContractStatus`，`Remark1`，`Remark2`，`CreatedOn`，`CreatedByName`，`ModifiedBy`，`ModifiedByName`，`ModifyedOn`，`ERPID`，`SyncDatetime`。

### 49.4 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`
- `SalesOrderID` → `APS_SalesOrder.SalesOrderID`（可选）
- `CustomerID` → 客户主数据（表名依环境）
- `ParentContractID` → `APS_SalesContract.SalesContractID`（自关联）
- `ContractNo` / `SalesContractID` ← `APS_ContractPayment`、`APS_ContractTerms`（销售场景，`ContractType`=2）
- `SalesContractID` ← `APS_SalesContractDetail`（合同与条款关联明细，不含物料/量价，§50）

### 49.5 表、索引、视图及扩展属性（完整脚本）

```sql
CREATE TABLE [dbo].[APS_SalesContract](
    [SalesContractID]     BIGINT         IDENTITY(1,1) NOT NULL,
    [OrganizeID]          INT            NULL,
    [ContractNo]          VARCHAR(50)    NOT NULL,
    [ContractName]        NVARCHAR(200)  NULL,
    [ContractType]        INT            NULL,
    [VersionNo]           VARCHAR(20)    NULL,
    [ParentContractID]    BIGINT         NULL,
    [CustomerID]          BIGINT         NULL,
    [CustomerCode]        VARCHAR(60)    NULL,
    [CustomerName]        NVARCHAR(200)  NULL,
    [SalesOrderID]        VARCHAR(20)    NULL,
    [Currency]            VARCHAR(10)    NULL CONSTRAINT [DF_APS_SalesContract_Currency] DEFAULT ('CNY'),
    [TotalAmount]         DECIMAL(18,2)  NULL,
    [TaxAmount]           DECIMAL(18,2)  NULL,
    [AmountExclTax]       DECIMAL(18,2)  NULL,
    [EffectiveDate]       DATE           NULL,
    [ExpiryDate]          DATE           NULL,
    [SignDate]            DATE           NULL,
    [SignPlace]           NVARCHAR(200)  NULL,
    [OurLegalEntity]      NVARCHAR(200)  NULL,
    [SalesPersonAccount]  VARCHAR(30)    NULL,
    [DepartmentName]      NVARCHAR(100)  NULL,
    [PaymentTermSummary]  NVARCHAR(500)  NULL,
    [DeliveryTermSummary] NVARCHAR(500)  NULL,
    [Status]              INT            NOT NULL CONSTRAINT [DF_APS_SalesContract_Status] DEFAULT (1),
    [Remark1]             NVARCHAR(500)  NULL,
    [Remark2]             NVARCHAR(500)  NULL,
    [CreatedBy]           VARCHAR(30)    NULL,
    [CreatedByName]       NVARCHAR(30)   NULL,
    [ModifiedBy]          VARCHAR(30)    NULL,
    [ModifiedByName]      NVARCHAR(30)   NULL,
    [CreatedOn]           DATETIME       NULL CONSTRAINT [DF_APS_SalesContract_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]          DATETIME       NULL,
    [ERPID]               VARCHAR(30)    NULL,
    [SyncDatetime]        DATETIME       NULL,
    CONSTRAINT [PK_APS_SalesContract] PRIMARY KEY CLUSTERED ([SalesContractID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesContract_Org_No]
ON [dbo].[APS_SalesContract]([OrganizeID], [ContractNo]);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesContract_Customer]
ON [dbo].[APS_SalesContract]([OrganizeID], [CustomerCode]);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesContract_SalesOrder]
ON [dbo].[APS_SalesContract]([SalesOrderID])
WHERE [SalesOrderID] IS NOT NULL;
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'销售合同主表：编号、客户、金额与生效区间、签约信息及可选销售订单关联。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesContract';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增；子表 ContractID 对应本列。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'SalesContractID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织/账套。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'合同编号（与条款、付款子表 ContractNo 一致）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'ContractNo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'合同名称。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'ContractName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'合同性质：1框架 2单笔等。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'ContractType';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'合同文本版本号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'VersionNo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主合同 ID（补充协议自关联）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'ParentContractID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'客户 ID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'CustomerID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'客户编码快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'CustomerCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'客户名称快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'CustomerName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'关联销售订单 ID（可选）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'SalesOrderID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'币别，默认 CNY。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'Currency';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'合同总额（口径由业务约定）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'TotalAmount';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'税额。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'TaxAmount';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'不含税金额。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'AmountExclTax';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'生效日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'EffectiveDate';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'到期日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'ExpiryDate';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'签署日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'SignDate';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'签署地点。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'SignPlace';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'我方签约主体。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'OurLegalEntity';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'业务员账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'SalesPersonAccount';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'部门。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'DepartmentName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'付款条件摘要。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'PaymentTermSummary';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'交货条件摘要。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'DeliveryTermSummary';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'外部 ERP 合同主键。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'同步时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContract',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按组织、合同编号查询。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesContract',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesContract_Org_No';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按组织、客户编码查询。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesContract',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesContract_Customer';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按销售订单查合同（过滤索引，非空 SalesOrderID）。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesContract',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesContract_SalesOrder';
GO

IF OBJECT_ID(N'dbo.V_APS_SalesContract', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_SalesContract;
GO

/*
  创建人：廖尚华
  创建日期：2026-03-25
  作用：关联组织、销售订单、父合同，输出销售合同关键字段（只读）。
*/
CREATE VIEW [dbo].[V_APS_SalesContract]
AS
SELECT
    c.[SalesContractID],
    c.[OrganizeID],
    org.[OrganizeName],
    c.[ContractNo],
    c.[ContractName],
    c.[ContractType],
    c.[VersionNo],
    c.[ParentContractID],
    par.[ContractNo]   AS [ParentContractNo],
    par.[ContractName] AS [ParentContractName],
    c.[CustomerID],
    c.[CustomerCode],
    c.[CustomerName],
    c.[SalesOrderID],
    so.[SalesOrderNo],
    so.[OrderDate]     AS [SalesOrderDate],
    so.[Status]        AS [SalesOrderStatus],
    so.[OrderStatus]   AS [SalesOrderDocStatus],
    so.[SalesMan]      AS [SalesOrderSalesMan],
    c.[Currency],
    c.[TotalAmount],
    c.[TaxAmount],
    c.[AmountExclTax],
    c.[EffectiveDate],
    c.[ExpiryDate],
    c.[SignDate],
    c.[SignPlace],
    c.[OurLegalEntity],
    c.[SalesPersonAccount],
    c.[DepartmentName],
    c.[PaymentTermSummary],
    c.[DeliveryTermSummary],
    c.[Status]         AS [ContractStatus],
    c.[Remark1],
    c.[Remark2],
    c.[CreatedOn],
    c.[CreatedByName],
    c.[ModifiedBy],
    c.[ModifiedByName],
    c.[ModifyedOn],
    c.[ERPID],
    c.[SyncDatetime]
FROM [dbo].[APS_SalesContract] AS c
LEFT JOIN [dbo].[Dev_Organize] AS org
    ON org.[OrganizeID] = c.[OrganizeID]
LEFT JOIN [dbo].[APS_SalesOrder] AS so
    ON so.[SalesOrderID] = c.[SalesOrderID]
LEFT JOIN [dbo].[APS_SalesContract] AS par
    ON par.[SalesContractID] = c.[ParentContractID];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：关联 Dev_Organize、APS_SalesOrder、主合同（补充协议父合同），输出合同编号/客户/金额/状态、组织名称、订单号与订单状态、父合同编号等关键字段（只读）。创建日期：2026-03-25。创建人：廖尚华。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_APS_SalesContract';
GO
```

### 49.6 备注

- **唯一性**：建议 `OrganizeID` + `ContractNo` 业务唯一；需要时可改为唯一索引（注意 `OrganizeID` NULL 语义）。
- **Status**：全库 **0草稿 1确认**；旧版若用 1/2/3 表示草稿/生效/终止，须数据迁移后方可与本文一致。
- **与条款关联明细**：见 **§50 `APS_SalesContractDetail`**（仅 **`SalesContractID` + `ContractTermsID`** 等关联信息，**不存料品/量价**；产品明细另表或走销售订单）。
- **与 ERP**：过滤索引依赖 SQL Server 版本能力；老版本可改为普通索引。
- **同步视图**：`V_APS_SalesContract` 为 **JOIN + 显式列**；`APS_SalesContract` / `Dev_Organize` / `APS_SalesOrder` 增删列或改名时须 **改视图脚本**（勿将本视图当作 `SELECT *` 基表维护）。

---

## 50. APS_SalesContractDetail（销售合同明细表）

**销售合同与条款的关联表**：仅存 **`SalesContractID`** 与 **`ContractTermsID`**（及行序、备注、审计等），用于把 **`APS_SalesContract`** 与 **`APS_ContractTerms`** 中单条条款行显式挂接；**不包含料品、规格、数量、单价、金额等物料或商务字段**（若有界面产品表另建业务表或复用销售订单行）。

### 50.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_SalesContractDetail` |
| 主键 | `SalesContractDetailID` (bigint, 自增) |
| 默认值 | `LineSeq` = 1，`Status` = 1，`CreatedOn` = getdate() |
| 同步视图 | `V_APS_SalesContractDetail` |

### 50.2 字段清单

#### 主键与归属
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SalesContractDetailID | bigint | NOT NULL | 主键，自增 |
| OrganizeID | int | Y | 组织/账套 |
| SalesContractID | bigint | NOT NULL | → `APS_SalesContract.SalesContractID` |
| LineSeq | int | NOT NULL | 行序号（同一合同下多条条款关联时的展示顺序） |

#### 关联条款
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ContractTermsID | bigint | Y | → `APS_ContractTerms.ContractTermsID`（须与同头 `ContractNo` / `ContractID` 语义一致，由应用校验） |

#### 状态与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | NOT NULL | **0=草稿，1=确认**（全库约定） |
| Remark1 | nvarchar(500) | Y | 备注 1 |
| Remark2 | nvarchar(500) | Y | 备注 2 |

#### 审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(30) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | 外部 ERP 行键 |
| SyncDatetime | datetime | Y | 同步时间 |

### 50.3 索引与同步视图

| 对象 | 名称 | 说明 |
|------|------|------|
| 聚集索引 | PK_APS_SalesContractDetail | 主键 |
| 非聚集索引 | IX_APS_SalesContractDetail_Contract | `SalesContractID`、`LineSeq` |
| 非聚集索引 | IX_APS_SalesContractDetail_Terms | `ContractTermsID`（过滤 `IS NOT NULL`） |
| 视图 | V_APS_SalesContractDetail | **JOIN** 合同头、条款，**显式关键列**（非 `SELECT *`） |

`V_APS_SalesContractDetail` **输出列**（便于与脚本对照）：`SalesContractDetailID`，`OrganizeID`，`SalesContractID`，`LineSeq`，`ContractTermsID`，`DetailLinkStatus`，`DetailRemark1`，`DetailRemark2`，`DetailCreatedOn`，`DetailCreatedByName`，`ContractNo`，`ContractName`，`CustomerCode`，`CustomerName`，`SalesOrderID`，`TotalAmount`，`Currency`，`ContractStatus`，`ContractEffectiveDate`，`ClauseSeq`，`ClauseCode`，`ClauseTitle`，`TermsCategory`，`TermsContractNo`。

### 50.4 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`
- `SalesContractID` → `APS_SalesContract.SalesContractID`
- `ContractTermsID` → `APS_ContractTerms.ContractTermsID`（建议与头表同 `ContractNo`，且 `APS_ContractTerms.ContractID` = 本合同 `SalesContractID` 时语义最完整）

### 50.5 表、索引、视图及扩展属性（完整脚本）

```sql
CREATE TABLE [dbo].[APS_SalesContractDetail](
    [SalesContractDetailID] BIGINT         IDENTITY(1,1) NOT NULL,
    [OrganizeID]            INT            NULL,
    [SalesContractID]       BIGINT         NOT NULL,
    [LineSeq]               INT            NOT NULL CONSTRAINT [DF_APS_SalesContractDetail_LineSeq] DEFAULT (1),
    [ContractTermsID]       BIGINT         NULL,
    [Status]                INT            NOT NULL CONSTRAINT [DF_APS_SalesContractDetail_Status] DEFAULT (1),
    [Remark1]               NVARCHAR(500)  NULL,
    [Remark2]               NVARCHAR(500)  NULL,
    [CreatedBy]             VARCHAR(30)    NULL,
    [CreatedByName]         NVARCHAR(30)   NULL,
    [ModifiedBy]            VARCHAR(30)    NULL,
    [ModifiedByName]        NVARCHAR(30)   NULL,
    [CreatedOn]             DATETIME       NULL CONSTRAINT [DF_APS_SalesContractDetail_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]            DATETIME       NULL,
    [ERPID]                 VARCHAR(30)    NULL,
    [SyncDatetime]          DATETIME       NULL,
    CONSTRAINT [PK_APS_SalesContractDetail] PRIMARY KEY CLUSTERED ([SalesContractDetailID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesContractDetail_Contract]
ON [dbo].[APS_SalesContractDetail]([SalesContractID], [LineSeq]);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesContractDetail_Terms]
ON [dbo].[APS_SalesContractDetail]([ContractTermsID])
WHERE [ContractTermsID] IS NOT NULL;
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'销售合同与合同条款行的关联表，不含料品与量价字段。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesContractDetail';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'SalesContractDetailID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织/账套。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'销售合同头主键，对应 APS_SalesContract.SalesContractID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'SalesContractID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'行序号（同一合同下多条款关联时的展示顺序）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'LineSeq';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'关联 APS_ContractTerms.ContractTermsID，与合同头同号/同 ContractID 由应用校验。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'ContractTermsID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'外部 ERP 行键。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'同步时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesContractDetail',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按销售合同、行序号查询明细。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesContractDetail',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesContractDetail_Contract';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按合同条款主键反查明细（过滤非空 ContractTermsID）。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesContractDetail',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesContractDetail_Terms';
GO

IF OBJECT_ID(N'dbo.V_APS_SalesContractDetail', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_SalesContractDetail;
GO

/*
  创建人：廖尚华
  创建日期：2026-03-25
  作用：关联 APS_SalesContract、APS_ContractTerms，返回合同—条款关联关键字段（只读）。
*/
CREATE VIEW [dbo].[V_APS_SalesContractDetail]
AS
SELECT
    d.[SalesContractDetailID],
    d.[OrganizeID],
    d.[SalesContractID],
    d.[LineSeq],
    d.[ContractTermsID],
    d.[Status]        AS [DetailLinkStatus],
    d.[Remark1]       AS [DetailRemark1],
    d.[Remark2]       AS [DetailRemark2],
    d.[CreatedOn]     AS [DetailCreatedOn],
    d.[CreatedByName] AS [DetailCreatedByName],
    c.[ContractNo],
    c.[ContractName],
    c.[CustomerCode],
    c.[CustomerName],
    c.[SalesOrderID],
    c.[TotalAmount],
    c.[Currency],
    c.[Status]        AS [ContractStatus],
    c.[EffectiveDate] AS [ContractEffectiveDate],
    t.[ClauseSeq],
    t.[ClauseCode],
    t.[ClauseTitle],
    t.[TermsCategory],
    t.[ContractNo]    AS [TermsContractNo]
FROM [dbo].[APS_SalesContractDetail] AS d
INNER JOIN [dbo].[APS_SalesContract] AS c
    ON c.[SalesContractID] = d.[SalesContractID]
LEFT JOIN [dbo].[APS_ContractTerms] AS t
    ON t.[ContractTermsID] = d.[ContractTermsID];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：关联销售合同头与合同条款，输出关联主键、合同编号/客户/金额/状态、条款序号/标题/分类及条款侧合同号等关键字段（只读）。创建日期：2026-03-25。创建人：廖尚华。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_APS_SalesContractDetail';
GO
```

### 50.6 备注

- **条款关联**：`ContractTermsID` 指向的 `APS_ContractTerms` 行应与 **`APS_SalesContract.ContractNo`**（及建议 **`ContractID` = `SalesContractID`**）一致；若只做编号关联，须在应用中校验 `ContractTerms.ContractNo` = 头表 `ContractNo`。
- **一条款一行或多行**：同一 `ContractTermsID` 是否允许在多行 `APS_SalesContractDetail` 重复，由业务决定；若要求唯一，可对 `(SalesContractID, ContractTermsID)` 或单独 `(ContractTermsID)` 建唯一过滤索引（`WHERE ContractTermsID IS NOT NULL`）。
- **与旧版表并存**：若库中已按旧设计建有含料品/量价列的同名表，须 **迁移** 或 **改名备份** 后再执行本脚本，避免与现结构混淆。
- **过滤索引**：老版本 SQL Server 若不兼容，将 `IX_APS_SalesContractDetail_Terms` 改为普通非聚集索引。
- **同步视图**：`V_APS_SalesContractDetail` 为 **JOIN + 显式列**，**非** `SELECT *`；`APS_SalesContractDetail` / `APS_SalesContract` / `APS_ContractTerms` 增删列或改名时须 **改视图脚本**（仅 `sp_refreshview` 无法补齐新列）。

---

## 51. APS_PaymentMethod（支付方式表）

**支付方式主数据**：编码、名称、支付渠道分类、排序与 **`Status`（0草稿/1确认）**；供销售合同、订单、收款单等业务 **`MethodCode` / `PaymentMethodID`** 引用（具体外键由业务表按需扩展）。

### 51.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_PaymentMethod` |
| 主键 | `PaymentMethodID` (bigint, 自增) |
| 业务键 | `MethodCode` + `OrganizeID`（建议唯一，见备注） |
| 默认值 | `Status` = 1，`IsDefault` = 0，`SortOrder` = 0，`CreatedOn` = getdate() |
| 同步视图 | `V_APS_PaymentMethod`（`SELECT *` 只读） |

### 51.2 字段清单

#### 主键与编码
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PaymentMethodID | bigint | NOT NULL | 主键，自增 |
| OrganizeID | int | Y | 组织/账套；全局码可空 |
| MethodCode | varchar(30) | NOT NULL | 支付方式编码（对内或对接 ERP） |
| MethodName | nvarchar(100) | NOT NULL | 支付方式名称（界面展示） |

#### 分类与展示
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| PayChannelType | int | Y | 渠道大类：1 银行转账，2 现金，3 票据，4 第三方/在线，5 其它（可扩展） |
| SortOrder | int | NOT NULL | 列表排序，默认 0 |
| IsDefault | bit | NOT NULL | 是否默认方式（同组织宜至多一行，由应用约束） |

#### 状态与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | NOT NULL | **0=草稿，1=确认**（全库约定） |
| Remark1 | nvarchar(500) | Y | 备注 1 |
| Remark2 | nvarchar(500) | Y | 备注 2 |

#### 审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(30) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | 外部系统支付方式主键或编码 |
| SyncDatetime | datetime | Y | 同步时间 |

### 51.3 索引与同步视图

| 对象 | 名称 | 说明 |
|------|------|------|
| 聚集索引 | PK_APS_PaymentMethod | 主键 |
| 非聚集索引 | IX_APS_PaymentMethod_Org_Code | `OrganizeID`、`MethodCode` |
| 视图 | V_APS_PaymentMethod | `SELECT *` 只读 |

### 51.4 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`
- **`MethodCode` / `PaymentMethodID`** ← 销售合同、销售订单、合同付款等（由业务侧增加外键或冗余编码字段时与本表对齐）

### 51.5 表、索引、视图及扩展属性（完整脚本）

```sql
CREATE TABLE [dbo].[APS_PaymentMethod](
    [PaymentMethodID]   BIGINT         IDENTITY(1,1) NOT NULL,
    [OrganizeID]        INT            NULL,
    [MethodCode]        VARCHAR(30)    NOT NULL,
    [MethodName]        NVARCHAR(100)  NOT NULL,
    [PayChannelType]    INT            NULL,
    [SortOrder]         INT            NOT NULL CONSTRAINT [DF_APS_PaymentMethod_SortOrder] DEFAULT (0),
    [IsDefault]         BIT            NOT NULL CONSTRAINT [DF_APS_PaymentMethod_IsDefault] DEFAULT (0),
    [Status]            INT            NOT NULL CONSTRAINT [DF_APS_PaymentMethod_Status] DEFAULT (1),
    [Remark1]           NVARCHAR(500)  NULL,
    [Remark2]           NVARCHAR(500)  NULL,
    [CreatedBy]         VARCHAR(30)    NULL,
    [CreatedByName]     NVARCHAR(30)   NULL,
    [ModifiedBy]        VARCHAR(30)    NULL,
    [ModifiedByName]    NVARCHAR(30)   NULL,
    [CreatedOn]         DATETIME       NULL CONSTRAINT [DF_APS_PaymentMethod_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]        DATETIME       NULL,
    [ERPID]             VARCHAR(30)    NULL,
    [SyncDatetime]      DATETIME       NULL,
    CONSTRAINT [PK_APS_PaymentMethod] PRIMARY KEY CLUSTERED ([PaymentMethodID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_APS_PaymentMethod_Org_Code]
ON [dbo].[APS_PaymentMethod]([OrganizeID], [MethodCode]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'支付方式主数据：编码、名称、渠道分类、排序与状态（0草稿1确认）；供合同/订单等引用。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_PaymentMethod';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'PaymentMethodID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织/账套；全局码可空。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'支付方式编码。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'MethodCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'支付方式名称。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'MethodName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'渠道大类：1转账 2现金 3票据 4第三方 5其它。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'PayChannelType';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'列表排序。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'SortOrder';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'是否默认支付方式。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'IsDefault';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'外部系统主键或编码。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'同步时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_PaymentMethod',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按组织、支付方式编码查询。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_PaymentMethod',
    @level2type = N'INDEX',  @level2name = N'IX_APS_PaymentMethod_Org_Code';
GO

IF OBJECT_ID(N'dbo.V_APS_PaymentMethod', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_PaymentMethod;
GO

/*
  创建人：廖尚华
  创建日期：2026-03-26
  作用：与基表 APS_PaymentMethod 列一致，供接口与报表只读查询支付方式。
*/
CREATE VIEW [dbo].[V_APS_PaymentMethod]
AS
SELECT *
FROM [dbo].[APS_PaymentMethod];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：与基表 APS_PaymentMethod 列一致，供接口与报表只读查询支付方式。创建日期：2026-03-26。创建人：廖尚华。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_APS_PaymentMethod';
GO
```

### 51.6 备注

- **唯一性**：建议 `OrganizeID`（含 NULL 约定）+ `MethodCode` 业务唯一；需要时可改为唯一索引。
- **与业务表**：若 `APS_SalesContract` 等需选支付方式，可增加 `PaymentMethodID`（bigint NULL）或 `PayMethodCode`（varchar 冗余）并通过应用或外键与本表对齐。
- **同步视图**：`SELECT *`，基表增删列后 `EXEC sp_refreshview N'dbo.V_APS_PaymentMethod';`。

---

## 52. WMS_StockAdjust（库存调整单头表）

库存调整单**头表**：业务类型（入/出库）、调整原因、头备注；与 **`WMS_StockAdjustDetail`** 一对多。界面字段与「新增库存调整单」对齐（单据号、状态、仓库等可按环境扩展）。

### 52.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `WMS_StockAdjust` |
| 主键 | `StockAdjustID` (bigint, 自增) |
| 默认值 | `Status` = 1，`CreatedOn` = getdate() |
| 同步视图 | `V_WMS_StockAdjust` |

### 52.2 字段清单

#### 主键与单号
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| StockAdjustID | bigint | NOT NULL | 主键，自增 |
| OrganizeID | int | Y | 组织/账套 |
| AdjustDocNo | varchar(50) | Y | 调整单号（业务单号，建议唯一） |

#### 业务
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| BizType | int | NOT NULL | 业务类型：**1 入库**，**2 出库**（与界面一致，可扩展） |
| ReasonCode | varchar(30) | Y | 调整原因编码 |
| ReasonName | nvarchar(200) | Y | 调整原因名称（快照/展示） |
| WarehouseCode | varchar(30) | Y | 仓库编码（可选，对接 `WMS_Warehouse` 等） |

#### 状态与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | NOT NULL | **0=草稿，1=确认**（全库约定；≥2 须本节备注定义） |
| Remark1 | nvarchar(500) | Y | 头表备注 1 |
| Remark2 | nvarchar(500) | Y | 头表备注 2 |

#### 审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(30) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | 外部单号或主键 |
| SyncDatetime | datetime | Y | 同步时间 |

### 52.3 索引与同步视图

| 对象 | 名称 | 说明 |
|------|------|------|
| 聚集索引 | PK_WMS_StockAdjust | 主键 |
| 非聚集索引 | IX_WMS_StockAdjust_Org_DocNo | `OrganizeID`、`AdjustDocNo` |
| 视图 | V_WMS_StockAdjust | `SELECT *` 只读 |

### 52.4 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`
- `WarehouseCode` → `WMS_Warehouse` 等（视环境）
- `StockAdjustID` ← `WMS_StockAdjustDetail`

### 52.5 表、索引、视图及扩展属性（完整脚本）

```sql
CREATE TABLE [dbo].[WMS_StockAdjust](
    [StockAdjustID] BIGINT         IDENTITY(1,1) NOT NULL,
    [OrganizeID]        INT            NULL,
    [AdjustDocNo]       VARCHAR(50)    NULL,
    [BizType]           INT            NOT NULL CONSTRAINT [DF_WMS_StockAdjust_BizType] DEFAULT (1),
    [ReasonCode]        VARCHAR(30)    NULL,
    [ReasonName]        NVARCHAR(200)  NULL,
    [WarehouseCode]     VARCHAR(30)    NULL,
    [Status]            INT            NOT NULL CONSTRAINT [DF_WMS_StockAdjust_Status] DEFAULT (1),
    [Remark1]           NVARCHAR(500)  NULL,
    [Remark2]           NVARCHAR(500)  NULL,
    [CreatedBy]         VARCHAR(30)    NULL,
    [CreatedByName]     NVARCHAR(30)   NULL,
    [ModifiedBy]        VARCHAR(30)    NULL,
    [ModifiedByName]    NVARCHAR(30)   NULL,
    [CreatedOn]         DATETIME       NULL CONSTRAINT [DF_WMS_StockAdjust_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]        DATETIME       NULL,
    [ERPID]             VARCHAR(30)    NULL,
    [SyncDatetime]      DATETIME       NULL,
    CONSTRAINT [PK_WMS_StockAdjust] PRIMARY KEY CLUSTERED ([StockAdjustID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_WMS_StockAdjust_Org_DocNo]
ON [dbo].[WMS_StockAdjust]([OrganizeID], [AdjustDocNo]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'库存调整单头表：业务类型、调整原因、仓库与状态。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'WMS_StockAdjust';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'StockAdjustID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织/账套。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'调整单号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'AdjustDocNo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'业务类型：1入库 2出库。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'BizType';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'调整原因编码。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'ReasonCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'调整原因名称快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'ReasonName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'仓库编码。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'WarehouseCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）；列默认 DEFAULT(1)。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注 2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'外部单号或主键。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'同步时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjust',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按组织、调整单号查询。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'WMS_StockAdjust',
    @level2type = N'INDEX',  @level2name = N'IX_WMS_StockAdjust_Org_DocNo';
GO

IF OBJECT_ID(N'dbo.V_WMS_StockAdjust', N'V') IS NOT NULL
    DROP VIEW dbo.V_WMS_StockAdjust;
GO

/*
  创建人：廖尚华
  创建日期：2026-03-26
  作用：与基表 WMS_StockAdjust 列一致，供只读查询库存调整单头。
*/
CREATE VIEW [dbo].[V_WMS_StockAdjust]
AS
SELECT *
FROM [dbo].[WMS_StockAdjust];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：与基表 WMS_StockAdjust 列一致，供只读查询库存调整单头。创建日期：2026-03-26。创建人：廖尚华。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_WMS_StockAdjust';
GO
```

### 52.6 备注

- **BizType**：与界面「业务类型」一致；枚举可扩展（如拆 3 盘盈 4 盘亏）。
- **Status**：见文首**Status 全库约定**（0草稿/1确认，**`DEFAULT (1)`**）。
- **同步视图**：`SELECT *`，基表增删列后 `EXEC sp_refreshview N'dbo.V_WMS_StockAdjust';`。

---

## 53. WMS_StockAdjustDetail（库存调整单明细表）

库存调整**明细行**：料号/名称/规格/单位、行上展示的**当前库存快照**、**调整后数量**、行备注；挂 **`StockAdjustID`**。

### 53.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `WMS_StockAdjustDetail` |
| 主键 | `StockAdjustDetailID` (bigint, 自增) |
| 默认值 | `LineSeq` = 1，`Status` = 1，`CreatedOn` = getdate() |
| 同步视图 | `V_WMS_StockAdjustDetail` |

### 53.2 字段清单

#### 主键与归属
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| StockAdjustDetailID | bigint | NOT NULL | 主键，自增 |
| OrganizeID | int | Y | 组织/账套 |
| StockAdjustID | bigint | NOT NULL | → `WMS_StockAdjust.StockAdjustID` |
| LineSeq | int | NOT NULL | 行序号 |

#### 料品快照
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MaterialID | bigint | Y | 料品 ID → `APS_Material` |
| MaterialCode | varchar(60) | Y | 产品编号 |
| MaterialName | nvarchar(200) | Y | 产品名称 |
| Spec | nvarchar(800) | Y | 规格/型号等 |
| Unit | nvarchar(20) | Y | 单位 |

#### 数量（与界面列对应）
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| OnHandQty | decimal(18,4) | Y | **库存数量**（保存时点的现存量快照） |
| AdjustQty | decimal(18,4) | Y | **调整库存数量**（界面录入值；含义见 §53.6） |

#### 状态与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | NOT NULL | **0=草稿，1=确认**（全库约定） |
| Remark1 | nvarchar(500) | Y | 行备注 1 |
| Remark2 | nvarchar(500) | Y | 行备注 2 |

#### 审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(30) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | 外部行键 |
| SyncDatetime | datetime | Y | 同步时间 |

### 53.3 索引与同步视图

| 对象 | 名称 | 说明 |
|------|------|------|
| 聚集索引 | PK_WMS_StockAdjustDetail | 主键 |
| 非聚集索引 | IX_WMS_StockAdjustDetail_Header | `StockAdjustID`、`LineSeq` |
| 视图 | V_WMS_StockAdjustDetail | `SELECT *` 只读 |

### 53.4 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`
- `StockAdjustID` → `WMS_StockAdjust.StockAdjustID`
- `MaterialID` → `APS_Material.MaterialID`

### 53.5 表、索引、视图及扩展属性（完整脚本）

```sql
CREATE TABLE [dbo].[WMS_StockAdjustDetail](
    [StockAdjustDetailID] BIGINT         IDENTITY(1,1) NOT NULL,
    [OrganizeID]              INT            NULL,
    [StockAdjustID]       BIGINT         NOT NULL,
    [LineSeq]                 INT            NOT NULL CONSTRAINT [DF_WMS_StockAdjustDetail_LineSeq] DEFAULT (1),
    [MaterialID]              BIGINT         NULL,
    [MaterialCode]            VARCHAR(60)    NULL,
    [MaterialName]            NVARCHAR(200)  NULL,
    [Spec]                    NVARCHAR(800)  NULL,
    [Unit]                    NVARCHAR(20)   NULL,
    [OnHandQty]               DECIMAL(18,4)  NULL,
    [AdjustQty]               DECIMAL(18,4)  NULL,
    [Status]                  INT            NOT NULL CONSTRAINT [DF_WMS_StockAdjustDetail_Status] DEFAULT (1),
    [Remark1]                 NVARCHAR(500)  NULL,
    [Remark2]                 NVARCHAR(500)  NULL,
    [CreatedBy]               VARCHAR(30)    NULL,
    [CreatedByName]           NVARCHAR(30)   NULL,
    [ModifiedBy]              VARCHAR(30)    NULL,
    [ModifiedByName]          NVARCHAR(30)   NULL,
    [CreatedOn]               DATETIME       NULL CONSTRAINT [DF_WMS_StockAdjustDetail_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]              DATETIME       NULL,
    [ERPID]                   VARCHAR(30)    NULL,
    [SyncDatetime]            DATETIME       NULL,
    CONSTRAINT [PK_WMS_StockAdjustDetail] PRIMARY KEY CLUSTERED ([StockAdjustDetailID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_WMS_StockAdjustDetail_Header]
ON [dbo].[WMS_StockAdjustDetail]([StockAdjustID], [LineSeq]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'库存调整单明细：料品快照、现存量快照、调整数量与行备注。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'WMS_StockAdjustDetail';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'StockAdjustDetailID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织/账套。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'库存调整单头主键。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'StockAdjustID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'行序号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'LineSeq';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'料品 ID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'MaterialID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'产品编号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'MaterialCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'产品名称。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'MaterialName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'规格/型号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'Spec';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'单位。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'Unit';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'保存时点的现存量快照（界面库存数量）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'OnHandQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'调整库存数量（目标存量或增减量，由业务与 BizType 约定）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'AdjustQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）；列默认 DEFAULT(1)。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'行备注 1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'行备注 2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'外部行键。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'同步时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按库存调整单头、行序号查询明细。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'WMS_StockAdjustDetail',
    @level2type = N'INDEX',  @level2name = N'IX_WMS_StockAdjustDetail_Header';
GO

IF OBJECT_ID(N'dbo.V_WMS_StockAdjustDetail', N'V') IS NOT NULL
    DROP VIEW dbo.V_WMS_StockAdjustDetail;
GO

/*
  创建人：廖尚华
  创建日期：2026-03-26
  作用：与基表 WMS_StockAdjustDetail 列一致，供只读查询库存调整单明细。
*/
CREATE VIEW [dbo].[V_WMS_StockAdjustDetail]
AS
SELECT *
FROM [dbo].[WMS_StockAdjustDetail];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：与基表 WMS_StockAdjustDetail 列一致，供只读查询库存调整单明细。创建日期：2026-03-26。创建人：廖尚华。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_WMS_StockAdjustDetail';
GO
```

### 53.6 备注

- **Status**：见文首**Status 全库约定**（0草稿/1确认，**`DEFAULT (1)`**）。
- **AdjustQty 两种常用口径**（二选一并在接口/单据规则写死）：**A.** 录入为**调整后的目标库存**，入账时 `新库存 = AdjustQty`；**B.** 录入为**增减量**，入账时 `新库存 = OnHandQty + (入库为正/出库为负的 AdjustQty)`。若当前界面混合，可增加 `AdjustQtyIsDelta`（bit）区分。
- **同步视图**：`SELECT *`，基表增删列后 `EXEC sp_refreshview N'dbo.V_WMS_StockAdjustDetail';`。
</think>


<｜tool▁calls▁begin｜><｜tool▁call▁begin｜>
Grep</think>


<｜tool▁calls▁begin｜><｜tool▁call▁begin｜>
Read## 54. APS_SalesReturn（销售退货单头表）

销售退货**头表**：与「新增销售退货单」类界面对齐——客户、**销售出库单**、合计（退货数量/赠品数量/总金额）、头备注；通过 **`ParentSalesReturnID`**（及可选快照 **`PriorReturnDocNo`**）关联**上一张销售退货单头**（拆单、补退、关联历史单据等）。与 **`APS_SalesReturnDetail`** 一对多。

**与 §48 的关系**：**`APS_SalesOrderReturn`** 记录**相对销售订单行**的退货流水（可多次退同一行）；**`APS_SalesReturn` / `APS_SalesReturnDetail`** 为**按单据组织的销售退货**（出库单口径、界面网格与头合计）。二者可**并行并存**；若业务要求与订单行退货表严格串联，可在明细 **`SalesOrderReturnID`** 指向 **`APS_SalesOrderReturn.SalesOrderReturnID`**（是否建库级外键由环境决定）。**禁止**在未做数据迁移前假设 §48 一行唯一对应本明细一行。

### 54.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_SalesReturn` |
| 主键 | `SalesReturnID` (bigint, 自增) |
| 默认值 | `Status` = 1，`CreatedOn` = getdate() |
| 同步视图 | `V_APS_SalesReturn` |

### 54.2 字段清单

#### 主键与单号
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SalesReturnID | bigint | NOT NULL | 主键，自增 |
| OrganizeID | int | Y | 组织/账套 |
| ReturnDocNo | varchar(50) | Y | 销售退货单号 |

#### 关联上一张退货单
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ParentSalesReturnID | bigint | Y | **上一张销售退货单头** → 本表 `SalesReturnID`（自引用逻辑关联） |
| PriorReturnDocNo | varchar(50) | Y | 上一单号快照（展示冗余，可与 `ParentSalesReturnID` 并存） |

#### 客户与出库/订单溯源
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CustomerID | bigint | Y | 客户 ID |
| CustomerCode | varchar(60) | Y | 客户编码快照 |
| CustomerName | nvarchar(200) | Y | 客户名称快照 |
| SalesOutboundDocNo | varchar(50) | Y | **销售出库单号**（界面选单） |
| SalesOutboundID | varchar(30) | Y | 出库单系统主键（类型随环境） |
| SalesOrderID | varchar(20) | Y | 溯源销售订单头（可选） |

#### 合计（通常由明细汇总回写）
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| TotalReturnQty | decimal(18,4) | Y | 合计退货数量 |
| TotalGiftQty | decimal(18,4) | Y | 合计赠品数量 |
| TotalAmount | decimal(18,2) | Y | 合计总金额 |

#### 状态与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | NOT NULL | **0=草稿，1=确认**（全库约定） |
| Remark1 | nvarchar(500) | Y | 头备注 1 |
| Remark2 | nvarchar(500) | Y | 头备注 2 |

#### 审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy | varchar(30) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |

#### ERP 与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ERPID | varchar(30) | Y | 外部单号或主键 |
| SyncDatetime | datetime | Y | 同步时间 |

### 54.3 索引与同步视图

| 对象 | 名称 | 说明 |
|------|------|------|
| 聚集索引 | PK_APS_SalesReturn | 主键 |
| 非聚集索引 | IX_APS_SalesReturn_Org_DocNo | `OrganizeID`、`ReturnDocNo` |
| 非聚集索引 | IX_APS_SalesReturn_Outbound | `OrganizeID`、`SalesOutboundDocNo` |
| 非聚集索引 | IX_APS_SalesReturn_Parent | `ParentSalesReturnID` |
| 视图 | V_APS_SalesReturn | `SELECT *` 只读 |

### 54.4 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`
- `ParentSalesReturnID` → `APS_SalesReturn.SalesReturnID`（逻辑父单；脚本未强制 FK，可按环境追加）
- `SalesReturnID` ← `APS_SalesReturnDetail.SalesReturnID`
- `SalesOrderID` → `APS_SalesOrder.SalesOrderID`（可选）
- 客户主数据视环境指向客户表（若有 `CustomerID`）

### 54.5 表、索引、视图及扩展属性（完整脚本）

```sql
-- ---------- §54 头表 APS_SalesReturn + V_APS_SalesReturn ----------

CREATE TABLE [dbo].[APS_SalesReturn](
    [SalesReturnID]       BIGINT        IDENTITY(1,1) NOT NULL,
    [OrganizeID]          INT           NULL,
    [ReturnDocNo]         VARCHAR(50)   NULL,
    [ParentSalesReturnID] BIGINT        NULL,
    [PriorReturnDocNo]    VARCHAR(50)   NULL,
    [CustomerID]          BIGINT        NULL,
    [CustomerCode]        VARCHAR(60)   NULL,
    [CustomerName]        NVARCHAR(200) NULL,
    [SalesOutboundDocNo]  VARCHAR(50)   NULL,
    [SalesOutboundID]     VARCHAR(30)   NULL,
    [SalesOrderID]        VARCHAR(20)   NULL,
    [TotalReturnQty]      DECIMAL(18,4) NULL,
    [TotalGiftQty]        DECIMAL(18,4) NULL,
    [TotalAmount]         DECIMAL(18,2) NULL,
    [Status]              INT           NOT NULL CONSTRAINT [DF_APS_SalesReturn_Status] DEFAULT (1),
    [Remark1]             NVARCHAR(500) NULL,
    [Remark2]             NVARCHAR(500) NULL,
    [CreatedBy]           VARCHAR(30)   NULL,
    [CreatedByName]       NVARCHAR(30)  NULL,
    [ModifiedBy]          VARCHAR(30)   NULL,
    [ModifiedByName]      NVARCHAR(30)  NULL,
    [CreatedOn]           DATETIME      NULL CONSTRAINT [DF_APS_SalesReturn_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]          DATETIME      NULL,
    [ERPID]               VARCHAR(30)   NULL,
    [SyncDatetime]        DATETIME      NULL,
    CONSTRAINT [PK_APS_SalesReturn] PRIMARY KEY CLUSTERED ([SalesReturnID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesReturn_Org_DocNo]
ON [dbo].[APS_SalesReturn]([OrganizeID], [ReturnDocNo]);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesReturn_Outbound]
ON [dbo].[APS_SalesReturn]([OrganizeID], [SalesOutboundDocNo]);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesReturn_Parent]
ON [dbo].[APS_SalesReturn]([ParentSalesReturnID]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'销售退货单头：客户与销售出库单、合计数量金额、关联上一退货单；与 APS_SalesReturnDetail 一对多。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesReturn';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'SalesReturnID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织/账套。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'销售退货单号（业务单号）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'ReturnDocNo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'上一张销售退货单头主键（自引用，拆单/补充退货等场景）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'ParentSalesReturnID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'上一张退货单号快照（与 ParentSalesReturnID 二选一冗余展示用）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'PriorReturnDocNo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'客户 ID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'CustomerID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'客户编码快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'CustomerCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'客户名称快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'CustomerName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'关联销售出库单号（界面选单，如 PB 前缀单号）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'SalesOutboundDocNo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'销售出库单系统主键（若环境与 varchar 主键一致可填）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'SalesOutboundID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'溯源销售订单头 ID（可选）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'SalesOrderID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'合计退货数量（一般由明细汇总回写）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'TotalReturnQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'合计赠品数量（一般由明细汇总回写）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'TotalGiftQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'合计总金额（一般由明细汇总回写）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'TotalAmount';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）；列默认 DEFAULT(1)。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'头表备注 1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'头表备注 2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'外部单号或主键。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'同步时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturn',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按组织、退货单号查询。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesReturn',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesReturn_Org_DocNo';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按组织、销售出库单号查询。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesReturn',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesReturn_Outbound';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按上一张退货单头关联查询。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesReturn',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesReturn_Parent';
GO

IF OBJECT_ID(N'dbo.V_APS_SalesReturn', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_SalesReturn;
GO

/*
  创建人：廖尚华
  创建日期：2026-03-30
  作用：与基表 APS_SalesReturn 列一致，供只读查询销售退货单头。
*/
CREATE VIEW [dbo].[V_APS_SalesReturn]
AS
SELECT *
FROM [dbo].[APS_SalesReturn];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：与基表 APS_SalesReturn 列一致，供只读查询销售退货单头。创建日期：2026-03-30。创建人：廖尚华。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_APS_SalesReturn';
GO
```

### 54.6 备注

- **与 §48**：见本节文首「与 §48 的关系」。
- **Status**：见文首 **Status 全库约定**（**`DEFAULT (1)`**）。
- **出库主数据**：若库中已有销售出库头表，可将 `SalesOutboundID` 类型与主表对齐或增加 FK。
- **同步视图**：`SELECT *`，基表增删列后 `EXEC sp_refreshview N'dbo.V_APS_SalesReturn';`。

---

## 55. APS_SalesReturnDetail（销售退货单明细表）

销售退货**明细行**：规格、单位、参考销售价、是否赠品、折扣%、单价、出库数量、剩余可退数量、**退货数量**、行金额、行备注；挂 **`SalesReturnID`**。

### 55.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_SalesReturnDetail` |
| 主键 | `SalesReturnDetailID` (bigint, 自增) |
| 默认值 | `LineSeq`=1，`IsGift`=0，`ReturnQty`=0，`Status`=1，`CreatedOn`=getdate() |
| 同步视图 | `V_APS_SalesReturnDetail` |

### 55.2 字段清单

#### 主键与归属
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SalesReturnDetailID | bigint | NOT NULL | 主键，自增 |
| OrganizeID | int | Y | 组织/账套 |
| SalesReturnID | bigint | NOT NULL | → `APS_SalesReturn` |
| LineSeq | int | NOT NULL | 行序号 |

#### 与销售订单/§48 衔接（可选）
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| SalesOrderDetailID | varchar(20) | Y | 销售订单行 |
| SalesOrderReturnID | bigint | Y | → `APS_SalesOrderReturn`（§48） |

#### 料品与界面列
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| MaterialID | bigint | Y | 料品 ID |
| MaterialCode | varchar(60) | Y | 产品编号 |
| MaterialName | nvarchar(200) | Y | 产品名称 |
| Spec | nvarchar(800) | Y | 规格 |
| Unit | nvarchar(20) | Y | 单位 |
| RefSellingPrice | decimal(18,4) | Y | 参考销售价 |
| IsGift | bit | NOT NULL | 是否赠品 |
| DiscountPct | decimal(18,4) | Y | 折扣% |
| UnitPrice | decimal(18,4) | Y | 单价（价格） |
| OutboundQty | decimal(18,4) | Y | 出库数量 |
| RemainReturnableQty | decimal(18,4) | Y | 剩余可退货数量 |
| ReturnQty | decimal(18,4) | NOT NULL | **本次退货数量** |
| LineAmount | decimal(18,4) | Y | 行金额 |

#### 状态与备注
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | NOT NULL | **0=草稿，1=确认** |
| Remark1 | nvarchar(500) | Y | 行备注 1 |
| Remark2 | nvarchar(500) | Y | 行备注 2 |

#### 审计与同步
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CreatedBy / CreatedByName / ModifiedBy / ModifiedByName | 同头表 | Y | 审计 |
| CreatedOn / ModifyedOn | datetime | Y | 时间 |
| ERPID / SyncDatetime | varchar(30) / datetime | Y | 外部键与同步 |

### 55.3 索引与同步视图

| 对象 | 名称 | 说明 |
|------|------|------|
| 聚集索引 | PK_APS_SalesReturnDetail | 主键 |
| 非聚集索引 | IX_APS_SalesReturnDetail_Header | `SalesReturnID`、`LineSeq` |
| 非聚集索引 | IX_APS_SalesReturnDetail_OrderLine | `SalesOrderDetailID` |
| 视图 | V_APS_SalesReturnDetail | `SELECT *` 只读 |

### 55.4 关联关系

- `SalesReturnID` → `APS_SalesReturn.SalesReturnID`
- `MaterialID` → `APS_Material.MaterialID`
- `SalesOrderDetailID` → `APS_SalesOrderDetail.SalesOrderDetailID`
- `SalesOrderReturnID` → `APS_SalesOrderReturn.SalesOrderReturnID`（可选）

### 55.5 表、索引、视图及扩展属性（完整脚本）

```sql
-- ---------- §55 明细 APS_SalesReturnDetail + V_APS_SalesReturnDetail ----------

CREATE TABLE [dbo].[APS_SalesReturnDetail](
    [SalesReturnDetailID]  BIGINT         IDENTITY(1,1) NOT NULL,
    [OrganizeID]           INT            NULL,
    [SalesReturnID]        BIGINT         NOT NULL,
    [LineSeq]              INT            NOT NULL CONSTRAINT [DF_APS_SalesReturnDetail_LineSeq] DEFAULT (1),
    [SalesOrderDetailID]   VARCHAR(20)    NULL,
    [SalesOrderReturnID]   BIGINT         NULL,
    [MaterialID]           BIGINT         NULL,
    [MaterialCode]          VARCHAR(60)    NULL,
    [MaterialName]          NVARCHAR(200)  NULL,
    [Spec]                  NVARCHAR(800)  NULL,
    [Unit]                  NVARCHAR(20)   NULL,
    [RefSellingPrice]       DECIMAL(18,4)  NULL,
    [IsGift]                BIT            NOT NULL CONSTRAINT [DF_APS_SalesReturnDetail_IsGift] DEFAULT (0),
    [DiscountPct]           DECIMAL(18,4)  NULL,
    [UnitPrice]             DECIMAL(18,4)  NULL,
    [OutboundQty]           DECIMAL(18,4)  NULL,
    [RemainReturnableQty]   DECIMAL(18,4)  NULL,
    [ReturnQty]             DECIMAL(18,4)  NOT NULL CONSTRAINT [DF_APS_SalesReturnDetail_ReturnQty] DEFAULT (0),
    [LineAmount]            DECIMAL(18,4)  NULL,
    [Status]                INT            NOT NULL CONSTRAINT [DF_APS_SalesReturnDetail_Status] DEFAULT (1),
    [Remark1]               NVARCHAR(500)  NULL,
    [Remark2]               NVARCHAR(500)  NULL,
    [CreatedBy]             VARCHAR(30)    NULL,
    [CreatedByName]         NVARCHAR(30)   NULL,
    [ModifiedBy]            VARCHAR(30)    NULL,
    [ModifiedByName]        NVARCHAR(30)   NULL,
    [CreatedOn]             DATETIME       NULL CONSTRAINT [DF_APS_SalesReturnDetail_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]            DATETIME       NULL,
    [ERPID]                 VARCHAR(30)    NULL,
    [SyncDatetime]          DATETIME       NULL,
    CONSTRAINT [PK_APS_SalesReturnDetail] PRIMARY KEY CLUSTERED ([SalesReturnDetailID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesReturnDetail_Header]
ON [dbo].[APS_SalesReturnDetail]([SalesReturnID], [LineSeq]);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesReturnDetail_OrderLine]
ON [dbo].[APS_SalesReturnDetail]([SalesOrderDetailID]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'销售退货单明细：规格/单位/参考价/是否赠品/折扣/单价/出库与可退数量/本次退货数量/行金额；挂 SalesReturnID。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesReturnDetail';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'SalesReturnDetailID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织/账套。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'销售退货单头主键。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'SalesReturnID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'行序号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'LineSeq';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'销售订单行 ID（可选，与 APS_SalesOrderDetail 对齐）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'SalesOrderDetailID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'销售订单退货行主键（可选，衔接 §48 APS_SalesOrderReturn）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'SalesOrderReturnID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'料品 ID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'MaterialID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'产品编号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'MaterialCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'产品名称。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'MaterialName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'规格（界面规格列）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'Spec';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'单位。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'Unit';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'参考销售价。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'RefSellingPrice';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'是否赠品。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'IsGift';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'折扣百分比。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'DiscountPct';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'单价（实际价格）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'UnitPrice';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'出库数量（来源出库单行快照）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'OutboundQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'剩余可退货数量。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'RemainReturnableQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'本次退货数量（界面主录入列）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'ReturnQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'行金额（一般由单价×退货数量计算回写）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'LineAmount';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）；列默认 DEFAULT(1)。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'行备注 1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'行备注 2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'外部行键。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'同步时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesReturnDetail',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按退货单头、行序号查询明细。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesReturnDetail',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesReturnDetail_Header';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按销售订单行过滤退货明细。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesReturnDetail',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesReturnDetail_OrderLine';
GO

IF OBJECT_ID(N'dbo.V_APS_SalesReturnDetail', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_SalesReturnDetail;
GO

/*
  创建人：廖尚华
  创建日期：2026-03-30
  作用：与基表 APS_SalesReturnDetail 列一致，供只读查询销售退货单明细。
*/
CREATE VIEW [dbo].[V_APS_SalesReturnDetail]
AS
SELECT *
FROM [dbo].[APS_SalesReturnDetail];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：与基表 APS_SalesReturnDetail 列一致，供只读查询销售退货单明细。创建日期：2026-03-30。创建人：廖尚华。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_APS_SalesReturnDetail';
GO

```

### 55.6 备注

- **汇总**：保存时建议由明细汇总回写头的 `TotalReturnQty`、`TotalGiftQty`、`TotalAmount`（赠品判断依 `IsGift`）。
- **与 §48**：一行明细最多对应一条 `APS_SalesOrderReturn` 时，可用 `SalesOrderReturnID` 对齐；若一对多需中间表或改模型。
- **同步视图**：`SELECT *`，基表增删列后 `EXEC sp_refreshview N'dbo.V_APS_SalesReturnDetail';`。

---

## 56. APS_SalesOrderDetailCompletion（销售订单行完成表）

按月（`YearMonth`）、销售订单号、料号等维度记录订单行**完成/出货/库存**类指标及组织、群组、客户与金额快照；供报表与达成分析。主键 **`ID`**。本表 **`SalesOrderID`**、**`MaterialID`** 为 **`int`**，与 **`APS_SalesOrder`** / **`APS_Material`** 常见键类型可能不一致，宜作**逻辑关联**。

### 56.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_SalesOrderDetailCompletion` |
| 主键 | `ID` (bigint, `IDENTITY(1,1)`)，约束名 **`PK_APS_SALESORDERDETAILCOMPLET`**（库内截断名） |
| 默认值 | `Status` = 1；`CreatedOn`、`ModifyedOn`、`SyncDatetime` = **getdate()**（知识库脚本使用规范约束名；已建库可能为 `DF__APS_Sales__*`） |
| 同步视图 | `V_APS_SalesOrderDetailCompletion` |

### 56.2 字段清单

#### 主键与维度
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| ID | bigint | NOT NULL | 主键，自增 |
| YearMonth | varchar(6) | Y | 月份（如 yyyyMM） |
| SalesOrderNo | varchar(30) | Y | 销售订单号 |
| OrganizeID | int | Y | 组织 ID |
| OrganizeName | nvarchar(50) | Y | 组织名称 |
| GroupID | int | Y | 群组 ID |
| GroupName | nvarchar(50) | Y | 群组名称 |
| DataSource | nvarchar(20) | Y | 数据来源 |

#### 料品快照
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Code | varchar(30) | Y | 料号 |
| Spec | nvarchar(1000) | Y | 规格 |
| MaterialID | int | Y | 料品 ID 快照 |
| MaterialName | nvarchar(1000) | Y | 料品名称 |
| SalesOrderID | int | Y | 销售订单头 ID 快照 |

#### 客户与金额
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| CustomerName | nvarchar(500) | Y | 客户名称（库内扩展属性或误写为「供应商」，以业务为准） |
| CustomerShortName | nvarchar(500) | Y | 客户简称 |
| Currency | nvarchar(10) | Y | 币别 |
| Price | decimal(24,6) | Y | 单价 |
| AllAmount | decimal(18,4) | Y | 价税合计 |
| AllAmountLc | decimal(18,4) | Y | 价税合计本位币 |
| ExchangeRate | decimal(24,9) | Y | 汇率 |

#### 数量与出货
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Qty | decimal(18,4) | Y | 数量 |
| UnFinishedQty | decimal(18,4) | Y | 未完成数量 |
| StockOutQty | decimal(18,4) | Y | 已出货数量 |
| UnShippedQty | decimal(18,4) | Y | 可出货数量 |
| PlanStockOutQty | decimal(18,4) | Y | 计划出货 |
| ActualStockOutQty | decimal(18,4) | Y | 实际出货 |
| ManPlanStockOutQty | decimal(18,4) | Y | 手工计划出货数 |
| StockDiffQty | int | Y | 出货差异数量 |
| MainPlanQty | decimal(18,4) | Y | 计划排产 |
| SumFinishQty | decimal(18,4) | Y | 完成数量 |
| ReportQty | decimal(18,4) | Y | 汇报数 |
| StockInQty | decimal(18,4) | Y | 总计完成数（列名与库注释一致；口径以业务为准） |
| TrayOfQty | int | Y | 托盘数量 |
| PalletQty | int | Y | 栈板数量 |

#### 分仓库存
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| StockQty1 | decimal(18,4) | Y | 分仓库存 1（如宁波） |
| StockQty2 | decimal(18,4) | Y | 分仓库存 2（如浙江） |
| StockQty3 | decimal(18,4) | Y | 分仓库存 3（如泰国） |
| StockQty | decimal(18,4) | Y | 总库存 |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | **0=草稿，1=确认**（全库约定；默认 1） |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注 1 |
| Remark2 | nvarchar(500) | Y | 备注 2 |
| ERPID | varchar(30) | Y | ERP 主键或外部标识 |
| SyncDatetime | datetime | Y | 同步时间 |

### 56.3 索引与同步视图

| 对象 | 名称 | 说明 |
|------|------|------|
| 聚集索引 | PK_APS_SALESORDERDETAILCOMPLET | 主键 |
| 非聚集索引 | IX_APS_SalesOrderDetailCompletion_Org_YearMonth | `OrganizeID`、`YearMonth` |
| 非聚集索引 | IX_APS_SalesOrderDetailCompletion_SalesOrderNo | `SalesOrderNo` |
| 视图 | V_APS_SalesOrderDetailCompletion | `SELECT *` 只读 |

### 56.4 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`（可选）
- `SalesOrderNo` → `APS_SalesOrder` 业务单号（逻辑）
- `SalesOrderID` → `APS_SalesOrder.SalesOrderID`（逻辑；类型可能不一致）
- `MaterialID` → `APS_Material.MaterialID`（逻辑；类型可能不一致）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`

### 56.5 表、索引、视图及扩展属性（完整脚本）

```sql
-- APS 销售订单行完成/达成表 — 与 APS_数据库表结构知识库.md §56.5、共享版 §56.5 保持一致。
-- 快照来源：APS 库 2026-04-15；主键约束名沿用库内 PK_APS_SALESORDERDETAILCOMPLET（COMPLETION 截断）。

CREATE TABLE [dbo].[APS_SalesOrderDetailCompletion](
    [ID]                 BIGINT         IDENTITY(1,1) NOT NULL,
    [YearMonth]          VARCHAR(6)     NULL,
    [SalesOrderNo]       VARCHAR(30)    NULL,
    [Code]               VARCHAR(30)    NULL,
    [Spec]               NVARCHAR(1000) NULL,
    [Qty]                DECIMAL(18,4)  NULL,
    [UnFinishedQty]      DECIMAL(18,4)  NULL,
    [StockOutQty]        DECIMAL(18,4)  NULL,
    [UnShippedQty]       DECIMAL(18,4)  NULL,
    [TrayOfQty]          INT            NULL,
    [StockInQty]         DECIMAL(18,4)  NULL,
    [PalletQty]          INT            NULL,
    [PlanStockOutQty]    DECIMAL(18,4)  NULL,
    [ActualStockOutQty]  DECIMAL(18,4)  NULL,
    [StockDiffQty]       INT            NULL,
    [MainPlanQty]        DECIMAL(18,4)  NULL,
    [SumFinishQty]       DECIMAL(18,4)  NULL,
    [OrganizeID]         INT            NULL,
    [OrganizeName]       NVARCHAR(50)   NULL,
    [GroupID]            INT            NULL,
    [GroupName]          NVARCHAR(50)   NULL,
    [Status]             INT            NULL CONSTRAINT [DF_APS_SalesOrderDetailCompletion_Status] DEFAULT (1),
    [CreatedBy]          VARCHAR(20)    NULL,
    [CreatedByName]      NVARCHAR(30)   NULL,
    [ModifiedBy]         VARCHAR(30)    NULL,
    [ModifiedByName]     NVARCHAR(30)   NULL,
    [CreatedOn]          DATETIME       NULL CONSTRAINT [DF_APS_SalesOrderDetailCompletion_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]         DATETIME       NULL CONSTRAINT [DF_APS_SalesOrderDetailCompletion_ModifyedOn] DEFAULT (GETDATE()),
    [Remark1]            NVARCHAR(500)  NULL,
    [Remark2]            NVARCHAR(500)  NULL,
    [ERPID]              VARCHAR(30)    NULL,
    [SyncDatetime]       DATETIME       NULL CONSTRAINT [DF_APS_SalesOrderDetailCompletion_SyncDatetime] DEFAULT (GETDATE()),
    [DataSource]         NVARCHAR(20)   NULL,
    [SalesOrderID]       INT            NULL,
    [MaterialID]         INT            NULL,
    [MaterialName]       NVARCHAR(1000) NULL,
    [CustomerName]       NVARCHAR(500)  NULL,
    [CustomerShortName]  NVARCHAR(500)  NULL,
    [Currency]           NVARCHAR(10)   NULL,
    [Price]              DECIMAL(24,6)  NULL,
    [AllAmount]          DECIMAL(18,4)  NULL,
    [AllAmountLc]        DECIMAL(18,4)  NULL,
    [ReportQty]          DECIMAL(18,4)  NULL,
    [ExchangeRate]       DECIMAL(24,9)  NULL,
    [ManPlanStockOutQty] DECIMAL(18,4)  NULL,
    [StockQty1]          DECIMAL(18,4)  NULL,
    [StockQty2]          DECIMAL(18,4)  NULL,
    [StockQty3]          DECIMAL(18,4)  NULL,
    [StockQty]           DECIMAL(18,4)  NULL,
    CONSTRAINT [PK_APS_SALESORDERDETAILCOMPLET] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesOrderDetailCompletion_Org_YearMonth]
ON [dbo].[APS_SalesOrderDetailCompletion]([OrganizeID], [YearMonth]);
GO

CREATE NONCLUSTERED INDEX [IX_APS_SalesOrderDetailCompletion_SalesOrderNo]
ON [dbo].[APS_SalesOrderDetailCompletion]([SalesOrderNo]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'销售订单行完成/达成：按月、订单号、料号等维度的数量、出货、库存与金额快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesOrderDetailCompletion';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'ID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'月份（如 yyyyMM）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'YearMonth';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'销售订单号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'SalesOrderNo';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'料号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'Code';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'规格。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'Spec';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'数量。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'Qty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'未完成数量。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'UnFinishedQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'已出货数量。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'StockOutQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'可出货数量。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'UnShippedQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'托盘数量。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'TrayOfQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'总计完成数（库内原注释；与入库/完成口径以业务为准）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'StockInQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'栈板数量。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'PalletQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'计划出货。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'PlanStockOutQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'实际出货。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'ActualStockOutQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'出货差异数量。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'StockDiffQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'计划排产。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'MainPlanQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'完成数量。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'SumFinishQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织ID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织名称。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'OrganizeName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'群组ID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'GroupID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'群组名称。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'GroupName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）；列默认 DEFAULT(1)。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'ERP主键或外部标识。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'同步时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'数据来源。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'DataSource';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'销售订单头 ID 快照（int；与 APS_SalesOrder.SalesOrderID 类型可能不一致，逻辑关联）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'SalesOrderID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'料品 ID 快照（int；与 APS_Material.MaterialID 常见为 bigint，逻辑关联）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'MaterialID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'料品名称。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'MaterialName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'客户名称（库内历史扩展属性或写为供应商名称，以业务为准）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'CustomerName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'客户简称。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'CustomerShortName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'币别。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'Currency';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'单价。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'Price';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'价税合计。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'AllAmount';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'价税合计本位币。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'AllAmountLc';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'汇报数。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'ReportQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'汇率。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'ExchangeRate';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'手工计划出货数。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'ManPlanStockOutQty';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'分仓库存1（如宁波）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'StockQty1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'分仓库存2（如浙江）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'StockQty2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'分仓库存3（如泰国）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'StockQty3';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'总库存。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'COLUMN', @level2name = N'StockQty';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按组织、年月查询。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesOrderDetailCompletion_Org_YearMonth';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按销售订单号查询。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_SalesOrderDetailCompletion',
    @level2type = N'INDEX',  @level2name = N'IX_APS_SalesOrderDetailCompletion_SalesOrderNo';
GO

IF OBJECT_ID(N'dbo.V_APS_SalesOrderDetailCompletion', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_SalesOrderDetailCompletion;
GO

/*
  创建人：廖尚华
  创建日期：2026-04-15
  作用：与基表 APS_SalesOrderDetailCompletion 列一致，供报表只读查询销售订单行完成数据。
*/
CREATE VIEW [dbo].[V_APS_SalesOrderDetailCompletion]
AS
SELECT *
FROM [dbo].[APS_SalesOrderDetailCompletion];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：与基表 APS_SalesOrderDetailCompletion 列一致，供报表只读查询销售订单行完成数据。创建日期：2026-04-15。创建人：廖尚华。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_APS_SalesOrderDetailCompletion';
GO

```

### 56.6 备注

- **已有库**：若表已存在，默认约束名可能为 `DF__APS_Sales__*`；本脚本使用规范约束名，仅适用于新建或手工对齐。
- **Status**：见文首 **Status 全库约定**（默认 **1**）。
- **CustomerName**：业务上多为客户名称；库内扩展属性或历史数据写「供应商」需自行核对。
- **同步视图**：`SELECT *`，基表增删列后 `EXEC sp_refreshview N'dbo.V_APS_SalesOrderDetailCompletion';`。

---

## 57. APS_InterfaceSAPOutputDetail（SAP 接口输出配置明细表）

挂 **`dbo.APS_InterfaceSAPOutput`**（**`EID`** 对齐；可按环境加 FK）；按需落库 **全量数据**、**最新数据** 与 **传入参数**，并冗余 **`FID`/`OutputName`/`APSTableName`** 快照。

### 57.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_InterfaceSAPOutputDetail` |
| 主键 | `DetailID` bigint 自增 |
| 默认值 | `Status`=1，`CreatedOn`/`ModifyedOn`/`SyncDatetime`=GETDATE() |

### 57.2 字段清单（核心）

| 字段 | 类型 | 说明 |
|------|------|------|
| EID | int | 关联 `APS_InterfaceSAPOutput.EID` |
| FID | int | 输出/接口配置快照 |
| FullData | varchar(max) | **全量数据**（大文本） |
| LatestData | varchar(max) | **最新数据**（大文本） |
| InputParameters | nvarchar(4000) | **传入参数** |
| OutputName、APSTableName | — | 与主配置冗余（可选快照） |

### 57.3 关联关系

- `EID` → `dbo.APS_InterfaceSAPOutput.EID`

### 57.4 备注

- **字段类型**：**FullData**、**LatestData** 均为 **`varchar(max)`**（大文本）；若需统一 Unicode，可改为 **`nvarchar(max)`**。
- 未预设 **`FOREIGN KEY`**，避免各环境与主表键不一致阻碍部署。

### 57.5 表、索引、视图及扩展属性（完整脚本）

```sql
-- APS_InterfaceSAPOutput 明细表 APS_InterfaceSAPOutputDetail
-- 挂靠逻辑键 EID（对应 dbo.APS_InterfaceSAPOutput.EID），冗余 FID/输出快照列便于检索。
-- 全量数据、最新数据：大文本字段，类型 VARCHAR(MAX)。
-- 与 APS_数据库表结构知识库.md §57.5、共享版 §57.5 对齐。

CREATE TABLE [dbo].[APS_InterfaceSAPOutputDetail](
    [DetailID]          BIGINT           IDENTITY(1, 1) NOT NULL,
    [EID]               INT              NOT NULL,
    [FID]               INT              NULL,
    [OutputName]        NVARCHAR(200)    NULL,
    [APSTableName]      SYSNAME          NULL,
    [FullData]          VARCHAR(MAX)     NULL,
    [LatestData]        VARCHAR(MAX)     NULL,
    [InputParameters]   NVARCHAR(4000)   NULL,
    [Remark1]           NVARCHAR(500)    NULL,
    [Remark2]           NVARCHAR(500)    NULL,
    [Status]            INT              NOT NULL CONSTRAINT [DF_APS_InterfaceSAPOutputDetail_Status] DEFAULT (1),
    [CreatedBy]         VARCHAR(30)      NULL,
    [CreatedByName]     NVARCHAR(30)     NULL,
    [ModifiedBy]        VARCHAR(30)      NULL,
    [ModifiedByName]    NVARCHAR(30)     NULL,
    [CreatedOn]         DATETIME         NULL CONSTRAINT [DF_APS_InterfaceSAPOutputDetail_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]        DATETIME         NULL CONSTRAINT [DF_APS_InterfaceSAPOutputDetail_ModifyedOn] DEFAULT (GETDATE()),
    [SyncDatetime]      DATETIME         NULL CONSTRAINT [DF_APS_InterfaceSAPOutputDetail_SyncDatetime] DEFAULT (GETDATE()),
    CONSTRAINT [PK_APS_InterfaceSAPOutputDetail] PRIMARY KEY CLUSTERED ([DetailID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_APS_InterfaceSAPOutputDetail_EID]
ON [dbo].[APS_InterfaceSAPOutputDetail]([EID] ASC);

CREATE NONCLUSTERED INDEX [IX_APS_InterfaceSAPOutputDetail_FID]
ON [dbo].[APS_InterfaceSAPOutputDetail]([FID] ASC, [CreatedOn] DESC);

GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'SAP 接口输出配置明细（挂 APS_InterfaceSAPOutput）；含全量/最新文本与传入参数。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_InterfaceSAPOutputDetail';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'DetailID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'APS_InterfaceSAPOutput.EID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'EID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'接口/输入配置 FID 快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'FID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'输出名称快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'OutputName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'APS 落地表名快照。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'APSTableName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'全量数据（varchar(max)）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'FullData';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'最新数据（varchar(max)）。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'LatestData';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'传入参数。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'InputParameters';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态；列默认 DEFAULT(1)。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'同步时间。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按 EID（输出配置）查明细。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'INDEX',  @level2name = N'IX_APS_InterfaceSAPOutputDetail_EID';

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按 FID + 创建时间查明细。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_InterfaceSAPOutputDetail',
    @level2type = N'INDEX',  @level2name = N'IX_APS_InterfaceSAPOutputDetail_FID';
GO

IF OBJECT_ID(N'dbo.V_APS_InterfaceSAPOutputDetail', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_InterfaceSAPOutputDetail;
GO

/*
  作用：与基表 APS_InterfaceSAPOutputDetail 列一致，仅供只读。
*/
CREATE VIEW [dbo].[V_APS_InterfaceSAPOutputDetail]
AS
SELECT *
FROM [dbo].[APS_InterfaceSAPOutputDetail];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：与基表 APS_InterfaceSAPOutputDetail 列一致，供 SAP 接口输出明细只读。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_APS_InterfaceSAPOutputDetail';
GO

```

### 57.6 备注

- **同步视图**：`SELECT *`；列变更后 `EXEC sp_refreshview N'dbo.V_APS_InterfaceSAPOutputDetail';`。
- **脚本文件**：**`APS_InterfaceSAPOutputDetail_DDL.sql`** 与 §57.5 保持一致时可任选其一归档。
- **主表增量（可选）**：**`dbo.APS_InterfaceSAPOutput`** 增加 **`EnableOutputDetail`**（`bit`，默认 `0`）用于控制是否写明细表；与 **`APS_InterfaceSAPOutput_Alter_EnableOutputDetail.sql`** 一致，可任选其一复制执行。

```sql
-- =============================================================================
-- APS_InterfaceSAPOutput：增量加列（可重复执行：列已存在则跳过）
-- EnableOutputDetail：是否在同步输出时写入 APS_InterfaceSAPOutputDetail（0=否，1=是）
-- =============================================================================

IF COL_LENGTH(N'dbo.APS_InterfaceSAPOutput', N'EnableOutputDetail') IS NULL
BEGIN
    ALTER TABLE [dbo].[APS_InterfaceSAPOutput] ADD
        [EnableOutputDetail] BIT NOT NULL
            CONSTRAINT [DF_APS_InterfaceSAPOutput_EnableOutputDetail] DEFAULT (0);
    PRINT N'已添加列 dbo.APS_InterfaceSAPOutput.EnableOutputDetail';
END
ELSE
    PRINT N'列 dbo.APS_InterfaceSAPOutput.EnableOutputDetail 已存在，跳过 ADD。';
GO

IF COL_LENGTH(N'dbo.APS_InterfaceSAPOutput', N'EnableOutputDetail') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.extended_properties ep
        INNER JOIN sys.columns c ON c.object_id = ep.major_id AND c.column_id = ep.minor_id AND ep.minor_id > 0
        INNER JOIN sys.tables t ON t.object_id = c.object_id AND t.schema_id = SCHEMA_ID(N'dbo')
        WHERE ep.name = N'MS_Description'
          AND t.name = N'APS_InterfaceSAPOutput'
          AND c.name = N'EnableOutputDetail'
    )
        EXEC sys.sp_addextendedproperty
            @name = N'MS_Description',
            @value = N'是否写入 APS_InterfaceSAPOutputDetail（0=否，1=是）；列默认 DEFAULT(0)。',
            @level0type = N'SCHEMA', @level0name = N'dbo',
            @level1type = N'TABLE',  @level1name = N'APS_InterfaceSAPOutput',
            @level2type = N'COLUMN', @level2name = N'EnableOutputDetail';
END
GO
```

- **主表传入参数（可选）**：**`dbo.APS_InterfaceSAPOutput`** 增加 **`InputParameters`**（**`nvarchar(max)`**，可空）；与 **`APS_InterfaceSAPOutput_Alter_InputParameters.sql`** 一致，可任选其一复制执行。

```sql
-- =============================================================================
-- APS_InterfaceSAPOutput：增量加列 InputParameters（可重复执行）
-- =============================================================================

IF COL_LENGTH(N'dbo.APS_InterfaceSAPOutput', N'InputParameters') IS NULL
BEGIN
    ALTER TABLE [dbo].[APS_InterfaceSAPOutput] ADD
        [InputParameters] NVARCHAR(MAX) NULL;
    PRINT N'已添加列 dbo.APS_InterfaceSAPOutput.InputParameters';
END
ELSE
    PRINT N'列 dbo.APS_InterfaceSAPOutput.InputParameters 已存在，跳过 ADD。';
GO

IF COL_LENGTH(N'dbo.APS_InterfaceSAPOutput', N'InputParameters') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.extended_properties ep
        INNER JOIN sys.columns c ON c.object_id = ep.major_id AND c.column_id = ep.minor_id AND ep.minor_id > 0
        INNER JOIN sys.tables t ON t.object_id = c.object_id AND t.schema_id = SCHEMA_ID(N'dbo')
        WHERE ep.name = N'MS_Description'
          AND t.name = N'APS_InterfaceSAPOutput'
          AND c.name = N'InputParameters'
    )
        EXEC sys.sp_addextendedproperty
            @name = N'MS_Description',
            @value = N'传入参数（如 JSON、查询串、业务自定义文本）；可与 APS_InterfaceSAPOutputDetail 按次快照配合使用。',
            @level0type = N'SCHEMA', @level0name = N'dbo',
            @level1type = N'TABLE',  @level1name = N'APS_InterfaceSAPOutput',
            @level2type = N'COLUMN', @level2name = N'InputParameters';
END
GO
```

---

## 58. APS_WorkCenter（工作中心清单表）

SAP/ERP 同步的**工作中心主数据**；以 **`WERKS`（工厂）+ `WCCode`（工作中心编码）** 为业务检索键，含有效期、反冲标志与负责人等字段。

### 58.1 表概述

| 项目 | 说明 |
|------|------|
| 表名 | `APS_WorkCenter` |
| 主键 | `WCID` int 自增 |
| 默认值 | `Status`=1，`CreatedOn`/`ModifyedOn`/`SyncDatetime`=GETDATE() |
| 来源 | APS 库导出脚本 2026-06-10 |

### 58.2 字段清单

#### 主键与组织
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| WCID | int | NOT NULL | 主键（自增） |
| OrganizeID | int | Y | 组织ID |
| OrganizeName | nvarchar(50) | Y | 组织 |
| GroupID | int | Y | 分组ID |
| GroupName | nvarchar(50) | Y | 分组名称 |

#### SAP/ERP 业务字段
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| WERKS | varchar(20) | Y | 工厂 |
| WCCode | varchar(20) | Y | 工作中心编码 |
| WCDesc | nvarchar(200) | Y | 描述 |
| BeginTime | datetime | Y | 开始日期 |
| EndTime | datetime | Y | 结束日期 |
| ChangeTime | datetime | Y | 更改日期 |
| RCMark | varchar(10) | Y | 反冲勾选标志 |
| Manager | nvarchar(50) | Y | 负责人/管理员 |
| ERPID | varchar(30) | Y | ERP 主键 ID |
| DataSource | nvarchar(20) | Y | 数据来源 |
| SyncDatetime | datetime | Y | 同步日期 |

#### 状态与审计
| 字段名 | 类型 | 可空 | 说明 |
|--------|------|------|------|
| Status | int | Y | 状态（0=草稿，1=确认；默认 1） |
| CreatedBy | varchar(20) | Y | 创建账号 |
| CreatedByName | nvarchar(30) | Y | 创建姓名 |
| ModifiedBy | varchar(30) | Y | 修改账号 |
| ModifiedByName | nvarchar(30) | Y | 修改姓名 |
| CreatedOn | datetime | Y | 创建日期 |
| ModifyedOn | datetime | Y | 修改日期 |
| Remark1 | nvarchar(500) | Y | 备注1 |
| Remark2 | nvarchar(500) | Y | 备注2 |

### 58.3 关联关系

- `OrganizeID` → `Dev_Organize.OrganizeID`（组织，多数业务场景可忽略 OrganizeID）
- `CreatedBy` / `ModifiedBy` → `Dev_Account.Account`（账号）

### 58.4 备注

- **业务用途**：工序、工艺路线、SAP 接口（如 `ARBPL` 工作中心）对照与排产资源主数据。
- **Status**：见文首 **Status 全库约定**（默认 **1**）。
- **脚本文件**：**`APS_WorkCenter_DDL.sql`** 与 §58.5 保持一致时可任选其一归档。

### 58.5 表、索引、视图及扩展属性（完整脚本）

```sql
-- APS 工作中心清单 APS_WorkCenter + V_APS_WorkCenter
-- 与 APS_数据库表结构知识库.md §58.5、共享版 §58.5、APS_WorkCenter_DDL.sql 对齐。

CREATE TABLE [dbo].[APS_WorkCenter](
    [WCID]           INT            IDENTITY(1, 1) NOT NULL,
    [OrganizeID]     INT            NULL,
    [OrganizeName]   NVARCHAR(50)   NULL,
    [GroupID]        INT            NULL,
    [GroupName]      NVARCHAR(50)   NULL,
    [Status]         INT            NULL CONSTRAINT [DF_APS_WorkCenter_Status] DEFAULT (1),
    [CreatedBy]      VARCHAR(20)    NULL,
    [CreatedByName]  NVARCHAR(30)   NULL,
    [ModifiedBy]     VARCHAR(30)    NULL,
    [ModifiedByName] NVARCHAR(30)   NULL,
    [CreatedOn]      DATETIME       NULL CONSTRAINT [DF_APS_WorkCenter_CreatedOn] DEFAULT (GETDATE()),
    [ModifyedOn]     DATETIME       NULL CONSTRAINT [DF_APS_WorkCenter_ModifyedOn] DEFAULT (GETDATE()),
    [Remark1]        NVARCHAR(500)  NULL,
    [Remark2]        NVARCHAR(500)  NULL,
    [ERPID]          VARCHAR(30)    NULL,
    [SyncDatetime]   DATETIME       NULL CONSTRAINT [DF_APS_WorkCenter_SyncDatetime] DEFAULT (GETDATE()),
    [DataSource]     NVARCHAR(20)   NULL,
    [WERKS]          VARCHAR(20)    NULL,
    [WCCode]         VARCHAR(20)    NULL,
    [WCDesc]         NVARCHAR(200)  NULL,
    [BeginTime]      DATETIME       NULL,
    [EndTime]        DATETIME       NULL,
    [ChangeTime]     DATETIME       NULL,
    [RCMark]         VARCHAR(10)    NULL,
    [Manager]        NVARCHAR(50)   NULL,
    CONSTRAINT [PK_APS_WorkCenter] PRIMARY KEY CLUSTERED ([WCID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_APS_WorkCenter_WERKS_WCCode]
ON [dbo].[APS_WorkCenter]([WERKS] ASC, [WCCode] ASC);

CREATE NONCLUSTERED INDEX [IX_APS_WorkCenter_OrganizeID]
ON [dbo].[APS_WorkCenter]([OrganizeID] ASC);

CREATE NONCLUSTERED INDEX [IX_APS_WorkCenter_ERPID]
ON [dbo].[APS_WorkCenter]([ERPID] ASC);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'工作中心清单（SAP/ERP 同步主数据；工厂+工作中心编码唯一业务键）。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_WorkCenter';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主键，自增。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'WCID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织ID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'OrganizeID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'组织。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'OrganizeName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'分组ID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'GroupID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'分组名称。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'GroupName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'状态：0草稿 1确认（全库约定）；列默认 DEFAULT(1)。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'Status';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'CreatedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'CreatedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改账号。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'ModifiedBy';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改姓名。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'ModifiedByName';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'创建日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'CreatedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'修改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'ModifyedOn';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注1。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'Remark1';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'备注2。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'Remark2';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'ERP主键ID。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'ERPID';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'同步日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'SyncDatetime';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'数据来源。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'DataSource';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'工厂。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'WERKS';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'工作中心编码。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'WCCode';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'描述。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'WCDesc';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'开始日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'BeginTime';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'结束日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'EndTime';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'更改日期。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'ChangeTime';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'反冲勾选标志。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'RCMark';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'负责人/管理员。',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'APS_WorkCenter',
    @level2type = N'COLUMN', @level2name = N'Manager';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按工厂+工作中心编码查询。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_WorkCenter',
    @level2type = N'INDEX',  @level2name = N'IX_APS_WorkCenter_WERKS_WCCode';

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按组织查询。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_WorkCenter',
    @level2type = N'INDEX',  @level2name = N'IX_APS_WorkCenter_OrganizeID';

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'按 ERP 主键查询。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'APS_WorkCenter',
    @level2type = N'INDEX',  @level2name = N'IX_APS_WorkCenter_ERPID';
GO

IF OBJECT_ID(N'dbo.V_APS_WorkCenter', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_WorkCenter;
GO

/*
  创建人：（按项目维护）
  创建日期：2026-06-10
  作用：与基表 APS_WorkCenter 列一致，仅供只读。
*/
CREATE VIEW [dbo].[V_APS_WorkCenter]
AS
SELECT *
FROM [dbo].[APS_WorkCenter];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'作用：与基表 APS_WorkCenter 列一致，供工作中心清单只读。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'VIEW',  @level1name = N'V_APS_WorkCenter';
GO
```

### 58.6 备注

- **同步视图**：`SELECT *`；列变更后 `EXEC sp_refreshview N'dbo.V_APS_WorkCenter';`。
- **已有库**：若表已存在（如 SSMS 直接导出建表），勿重复执行 CREATE；仅需补索引/视图/扩展属性时从 §58.5 摘取对应段落。
