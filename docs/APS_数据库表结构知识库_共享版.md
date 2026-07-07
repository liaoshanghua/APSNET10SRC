# APS 系统数据表结构知识库

**版本**：v1.20 | **更新日期**：2026-06-10  

> 本文档用于团队协作查阅 APS 系统数据库表结构，便于开发、排错和需求分析。与 `APS_数据库表结构知识库.md` 对照使用时，以主库完整字段与 SQL 为准。

---

## 使用说明

- 可按 **Ctrl+F** 搜索表名或字段名快速定位
- **OrganizeID**：大部分情况下不使用，可忽略
- **扩展属性**：新表须在库中维护 `MS_Description`（表 + 每列；重要索引可选）。完整 DDL 与脚本见 `APS_数据库表结构知识库.md` 对应表章节（**§44～§58**：税率、合同域、**库存调整单**、销售订单退货、支付方式、**销售退货单（按出库单）**、**销售订单行完成**、**SAP 接口输出明细**、**工作中心清单**等）。
- **Remark1 / Remark2**：新建业务表须包含 `Remark1`、`Remark2`（`nvarchar(500) NULL`），与 APS 其它表一致；详见 `APS_数据库表结构知识库.md` 文首「建表约定」。
- **同步视图**：新建业务表须同步创建 **`V_`+表名**（如 `V_APS_TaxRate`）。视图体默认 **`SELECT * FROM 基表`**（不逐一列字段）。**`CREATE VIEW` 前**须有注释块：**创建人**、**创建日期**、**作用**；并维护视图 **`MS_Description`** 与注释一致。主库 **§44～§58** 已将「建表 + 索引 + 扩展属性 + 视图」写在**同一段 SQL** 中，新建表请沿用。**例外**：**§49** `V_APS_SalesContract`、**§50** `V_APS_SalesContractDetail` 为 **多表 JOIN + 显式关键列**（非 `SELECT *`），见主库 **§49.5、§50.5**。
- **对话交付（约定）**：通过 AI 协助出表时，**当次回复须同时贴出完整 SQL**（表+索引+扩展属性+`V_` 视图），不能仅指向主库小节；主库仍作为归档与核对依据。
- **`Status`（int）**（与主库「建表约定」一致）：**0=草稿，1=确认**；列默认 **`DEFAULT (1)`**（新建即确认；需草稿时插入显式写 `0`）。
- 表结构可能因环境（APS、EK0721、APS20260323 等）略有差异，以实际数据库为准
- 标注「部分环境」的字段表示并非所有库都存在

---

## 目录

- [一、表清单与快速索引](#一表清单与快速索引)
- [二、表间关联总览](#二表间关联总览)
- [三、各表详细结构](#三各表详细结构)

---

## 一、表清单与快速索引

| 序号 | 表名 | 说明 | 主键 |
|:---:|------|------|------|
| 1 | APS_Material | 料品表 | MaterialID |
| 2 | APS_Order | 生产订单表 | OrderID |
| 3 | Dev_Organize | 组织表 | OrganizeID |
| 4 | APS_SalesOrder | 销售订单主表 | SalesOrderID |
| 5 | APS_SalesOrderDetail | 销售订单行表 | SalesOrderDetailID |
| 6 | APS_OrderBOM | 生产订单用料清单 | OrderBOMID |
| 7 | APS_OrderProcess | 生产订单工序表 | ID |
| 8 | APS_ProcessGroup | 工艺表 | ProcessGroupID |
| 9 | APS_Process | 工序表 | ProcessID |
| 10 | APS_ProcessGroupInfo | 工艺工序关联表 | ProcessGroupInfoID |
| 11 | APS_ProcessGroupMaterial | 产品工艺表 | ProcessGroupMaterialID |
| 12 | Dev_Account | 账号表 | Account |
| 13 | APS_OrderPlan | 排产主表 | FirstPlanID |
| 14 | APS_ProcessPartName | 工段表 | ProcessPartName |
| 15 | APS_ProcessPartPlan | 工段计划表 | ProcessPartID |
| 16 | APS_ProcessPlan | 工序计划表 | ProcessPlanID |
| 17 | APS_DayPlan | 日计划表 | DayPlanID |
| 18 | APS_PO | 采购单表 | ID |
| 19 | APS_ProcessPosition | 工序职位关联表 | ProcessPositionID |
| 20 | Dev_PositionAccountMap | 人员岗位关联表 | PositionAccountID |
| 21 | Dev_PositionLevel | 岗位等级表 | PositionLevelID |
| 22 | Dev_PositionLevelMap | 岗位技能等级配置表 | PositionLevelID |
| 23 | APS_Machine | 机台表 | MachineID |
| 24 | APS_MachineMould | 模具表 | MachineMouldID |
| 25 | APS_MachineMouldRelative | 模具机台关系表 | MachineMouldRelativeID |
| 26 | APS_MachineMouldMaterial | 模具产品关系表 | MachineMouldMaterial |
| 27 | APS_MaterialBOM | 产品BOM表 | MaterialBomID |
| 28 | APS_PR | 采购申请表 | ID |
| 29 | Dev_PositionExamRecord | 员工技能考核记录表 | PositionAccountID |
| 30 | WMS_Stock | 库存表 | StockID |
| 31 | WMS_Warehouse | 仓库表 | WarehouseID |
| 32 | APS_WorkingTimes | 班别表 | WorkingTimesID |
| 33 | APS_Holiday | 放假表 | HolidayID |
| 34 | APS_OrganizeWorkingTimesDetail | 排班明细表 | WorkingTimesDetailID |
| 35 | APS_OrderPlanMaterialForm | 物料齐套明细表 | MaterialFormID |
| 36 | APS_OrderPlanMaterialFormByItem | 物料齐套按料号汇总表 | 无 |
| 37 | APS_DeliveryRule | 送货计划规则表 | ID |
| 38 | ERP_ZPPT036Temp | 送货计划运算中间表 | ID |
| 39 | APS_DeliveryDataDetail | 送货分配过程明细表 | ID |
| 40 | APS_DeliveryData | 供应商送货计划表 | ID |
| 41 | APS_OrderPlanMaterialFormExclude | 齐套物料排除规则表 | ID |
| 42 | APS_POScheduling | 运算送货分配采购订单临时表 | 无 |
| 43 | APS_DeliveryDataTmp | 供应商送货计划临时表 | ID |
| 44 | APS_TaxRate | 税率主数据表 | TaxRateID |
| 45 | APS_ContractPayment | 合同付款信息表 | ContractPaymentID |
| 46 | APS_CompanyPaymentAccount | 本公司付款账号信息表 | CompanyPaymentAccountID |
| 47 | APS_ContractTerms | 合同条款信息表 | ContractTermsID |
| 48 | APS_SalesOrderReturn | 销售订单退货表 | SalesOrderReturnID |
| 49 | APS_SalesContract | 销售合同表 | SalesContractID |
| 50 | APS_SalesContractDetail | 销售合同—条款关联表（无物料字段） | SalesContractDetailID |
| 51 | APS_PaymentMethod | 支付方式主数据表 | PaymentMethodID |
| 52 | WMS_StockAdjust | 库存调整单头表 | StockAdjustID |
| 53 | WMS_StockAdjustDetail | 库存调整单明细表 | StockAdjustDetailID |
| 54 | APS_SalesReturn | 销售退货单头表（按销售出库单；可关联上一退货单） | SalesReturnID |
| 55 | APS_SalesReturnDetail | 销售退货单明细表 | SalesReturnDetailID |
| 56 | APS_SalesOrderDetailCompletion | 销售订单行完成表（按月/订单/料号达成快照） | ID |
| 57 | APS_InterfaceSAPOutputDetail | SAP 接口输出配置明细（varchar(max)×2 + nvarchar(4000) 传入参数） | DetailID |
| 58 | APS_WorkCenter | 工作中心清单（SAP/ERP 同步；WERKS+WCCode） | WCID |

---

## 二、表间关联总览

### 2.1 数据流（业务链路）

```
客户下单 → APS_SalesOrder（销售订单头）
                │
                │ 1:N  SalesOrderID
                ▼
         APS_SalesOrderDetail（销售订单行：料品 + 数量 + 交期）
                │
                │ 1:N  SalesOrderDetailID
                ▼
         APS_Order（生产订单：具体生产任务）
                │
                ├── MaterialID → APS_Material
                ├── OrderID ← APS_OrderBOM（用料清单）、APS_OrderProcess（工序）
                ├── MFGOrganizeID / OwnOrganizeID → Dev_Organize
                └── SalesOrderDetailID ← APS_SalesOrderDetail
```

**销售合同域（与上并列，非订单必经）**：`APS_SalesContract`（头）与 **`ContractNo` / `SalesContractID`** 和 `APS_ContractTerms`、`APS_ContractPayment` 衔接；**`APS_SalesContractDetail`** 仅 **`SalesContractID` + `ContractTermsID`** 关联条款（无物料列）。**支付方式**见 **`APS_PaymentMethod`**（§51）。**库存调整单**：**`WMS_StockAdjust`** + **`WMS_StockAdjustDetail`**（§52～§53），对应「新增库存调整单」类界面。**销售退货单（按出库单）**：**`APS_SalesReturn`** + **`APS_SalesReturnDetail`**（§54～§55），头表 **`ParentSalesReturnID`** 可关联**上一张退货单头**；与 **§48** `APS_SalesOrderReturn`（按订单行退货流水）**并行可存**，明细可选 **`SalesOrderReturnID`** 衔接 §48。**SAP 接口输出明细**：**`APS_InterfaceSAPOutputDetail`**（§57，`EID` 挂 **`APS_InterfaceSAPOutput`**）。**工作中心清单**：**`APS_WorkCenter`**（§58，**`WERKS`+`WCCode`**）。

只读查询：`V_APS_SalesContract`、`V_APS_SalesContractDetail`（**§49.5、§50.5**）；`V_APS_PaymentMethod`（**§51.5**）；`V_WMS_StockAdjust`、`V_WMS_StockAdjustDetail`（**§52.5、§53.5**）；`V_APS_SalesReturn`、`V_APS_SalesReturnDetail`（**§54.5、§55.5**）；`V_APS_SalesOrderDetailCompletion`（**§56.5**）；`V_APS_InterfaceSAPOutputDetail`（**§57.5**）；`V_APS_WorkCenter`（**§58.5**）。

### 2.2 关联关系速查表

| 从表 | 字段 | 关联到 | 说明 |
|------|------|--------|------|
| APS_SalesOrder | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_SalesOrderDetail | SalesOrderID | APS_SalesOrder.SalesOrderID | 销售订单头 |
| APS_SalesOrderDetail | MaterialID | APS_Material.MaterialID | 料品 |
| APS_SalesOrderDetail | MFGOrganizeID | Dev_Organize.OrganizeID | 制造组织 |
| APS_Order | MaterialID | APS_Material.MaterialID | 料品 |
| APS_Order | SalesOrderDetailID | APS_SalesOrderDetail.SalesOrderDetailID | 销售订单行 |
| APS_Order | MFGOrganizeID | Dev_Organize.OrganizeID | 制造组织 |
| APS_Order | OwnOrganizeID | Dev_Organize.OrganizeID | 所属组织 |
| APS_Material | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_OrderBOM | OrderID | APS_Order.OrderID | 生产订单 |
| APS_OrderBOM | MaterialID | APS_Material.MaterialID | 子件料品 |
| APS_OrderProcess | OrderID | APS_Order.OrderID | 生产订单 |
| APS_OrderProcess | MaterialID | APS_Material.MaterialID | 产品料品 |
| APS_OrderProcess | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_OrderProcess | ProcessGroupID | APS_ProcessGroup.ProcessGroupID | 工艺 |
| APS_ProcessGroup | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_OrderProcess | ProcessID | APS_Process.ProcessID | 工序主数据 |
| APS_Process | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_OrderProcess | ProcessGroupInfoID | APS_ProcessGroupInfo.ProcessGroupInfoID | 工艺工序关联 |
| APS_ProcessGroupInfo | ProcessGroupID | APS_ProcessGroup.ProcessGroupID | 工艺 |
| APS_ProcessGroupInfo | ProcessID | APS_Process.ProcessID | 工序 |
| APS_ProcessGroupInfo | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_ProcessGroupMaterial | ProcessGroupID | APS_ProcessGroup.ProcessGroupID | 工艺 |
| APS_ProcessGroupMaterial | MaterialID | APS_Material.MaterialID | 料品 |
| APS_ProcessGroupMaterial | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| Dev_Account | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| Dev_Account | LeadUserCode | Dev_Account.Account | 上级账号 |
| APS_OrderPlan | OrderID | APS_Order.OrderID | 生产订单 |
| APS_OrderPlan | MaterialID | APS_Material.MaterialID | 料品 |
| APS_ProcessPartName | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_Process | ProcessPartName | APS_ProcessPartName.ProcessPartName | 工段 |
| APS_ProcessPartPlan | OrderID | APS_Order.OrderID | 生产订单 |
| APS_ProcessPartPlan | FirstPlanID | APS_OrderPlan.FirstPlanID | 预排主表 |
| APS_ProcessPartPlan | MaterialID | APS_Material.MaterialID | 料品 |
| APS_ProcessPartPlan | ProcessPartName | APS_ProcessPartName.ProcessPartName | 工段 |
| APS_ProcessPlan | ProcessPartID | APS_ProcessPartPlan.ProcessPartID | 工段计划 |
| APS_ProcessPlan | FirstPlanID | APS_OrderPlan.FirstPlanID | 预排主表 |
| APS_ProcessPlan | OrderID | APS_Order.OrderID | 生产订单 |
| APS_ProcessPlan | MaterialID | APS_Material.MaterialID | 料品 |
| APS_ProcessPlan | ProcessID | APS_Process.ProcessID | 工序 |
| APS_DayPlan | ProcessPlanID | APS_ProcessPlan.ProcessPlanID | 工序计划 |
| APS_DayPlan | OrderID | APS_Order.OrderID | 生产订单 |
| APS_DayPlan | MaterialID | APS_Material.MaterialID | 料品 |
| APS_PO | OrderID | APS_Order.OrderID | 生产订单 |
| APS_PO | MaterialID | APS_Material.MaterialID | 料品 |
| APS_PO | Code | APS_Material.Code | 料号 |
| APS_PO | SupplierCode | Dev_Supplier.Code | 供应商代码 |
| APS_ProcessPosition | ProcessID | APS_Process.ProcessID | 工序 |
| APS_ProcessPosition | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_ProcessPosition | Account | Dev_Account.Account | 账号 |
| Dev_PositionAccountMap | Account | Dev_Account.Account | 账号 |
| Dev_PositionAccountMap | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| Dev_PositionExamRecord | Account | Dev_Account.Account | 账号 |
| Dev_PositionExamRecord | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| Dev_PositionLevel | PorcessID | APS_Process.ProcessID | 工序 |
| Dev_PositionLevel | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| Dev_PositionAccountMap | PositionLevelID | Dev_PositionLevel/Dev_PositionLevelMap | 岗位等级 |
| Dev_PositionLevelMap | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_ProcessPlan | MachineID | APS_Machine.MachineID | 机台 |
| APS_Machine | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_ProcessPlan | MachineMouldID | APS_MachineMould.MachineMouldID | 模具 |
| APS_MachineMould | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_MachineMouldRelative | MachineMouldID | APS_MachineMould.MachineMouldID | 模具 |
| APS_MachineMouldRelative | MachineID | APS_Machine.MachineID | 机台 |
| APS_MachineMouldMaterial | MachineMouldID | APS_MachineMould.MachineMouldID | 模具 |
| APS_MachineMouldMaterial | MaterialID | APS_Material.MaterialID | 料品 |
| APS_MaterialBOM | MaterialID | APS_Material.MaterialID | 子件料品 |
| APS_MaterialBOM | BOMMasterID、ParentMaterialID | APS_Material.MaterialID | 母件/父级料品 |
| APS_MaterialBOM | ProcessGroupInfoID | APS_ProcessGroupInfo.ProcessGroupInfoID | 工序 |
| APS_MaterialBOM | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_PR | MaterialID | APS_Material.MaterialID | 料品 |
| APS_PR | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| WMS_Stock | MaterialID | APS_Material.MaterialID | 料品 |
| WMS_Stock | WarehouseID | WMS_Warehouse.WarehouseID | 仓库 |
| WMS_Stock | MFGOrganizeID | Dev_Organize.OrganizeID | 制造组织 |
| WMS_Stock | CreatedBy、ModifiedBy | Dev_Account.Account | 账号 |
| WMS_Warehouse | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| WMS_Warehouse | ParentWarehouseID | WMS_Warehouse.WarehouseID | 上级仓库 |
| WMS_Warehouse | CreatedBy、ModifiedBy | Dev_Account.Account | 账号 |
| APS_Order | WorkingTimesID | APS_WorkingTimes.WorkingTimesID | 班别 |
| APS_WorkingTimes | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_WorkingTimes | ParentWorkingTimesID | APS_WorkingTimes.WorkingTimesID | 上级班别 |
| APS_WorkingTimes | CreatedBy、ModifiedBy | Dev_Account.Account | 账号 |
| APS_Holiday | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_Holiday | CreatedBy、ModifiedBy | Dev_Account.Account | 账号 |
| APS_OrganizeWorkingTimesDetail | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_OrganizeWorkingTimesDetail | WorkingTimesID | APS_WorkingTimes.WorkingTimesID | 班别 |
| APS_OrganizeWorkingTimesDetail | CreatedBy、ModifiedBy | Dev_Account.Account | 账号 |
| APS_OrderPlanMaterialForm | MaterialID | APS_Material.MaterialID | 料品 |
| APS_OrderPlanMaterialForm | FirstPlanID | APS_OrderPlan.FirstPlanID | 预排 |
| APS_OrderPlanMaterialForm | OrderID | APS_Order.OrderID | 生产订单 |
| APS_OrderPlanMaterialForm | OrderBOMID | APS_OrderBOM.OrderBOMID | 订单用料 |
| APS_OrderPlanMaterialForm | SalesOrderDetailID | APS_SalesOrderDetail.SalesOrderDetailID | 销售订单行（类型以环境为准） |
| APS_OrderPlanMaterialForm | CreatedBy、ModifiedBy | Dev_Account.Account | 账号 |
| APS_OrderPlanMaterialFormByItem | MaterialID | APS_Material.MaterialID | 料品 |
| APS_DeliveryRule | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_DeliveryRule | SuplierCode | Dev_Supplier.Code | 供应商代码 |
| APS_DeliveryRule | Account | Dev_Account.Account | 物控账号 |
| APS_DeliveryRule | CreatedBy、ModifiedBy | Dev_Account.Account | 账号 |
| ERP_ZPPT036Temp | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| ERP_ZPPT036Temp | MATNR | APS_Material.Code/ERPID | 料品（SAP物料号） |
| ERP_ZPPT036Temp | LIFNR | Dev_Supplier.Code | 供应商（SAP格式） |
| ERP_ZPPT036Temp | POID | APS_PO.ID | 采购订单主键 |
| ERP_ZPPT036Temp | OrderID | APS_Order.OrderID | 生产订单 |
| APS_DeliveryDataDetail | MaterialID | APS_Material.MaterialID | 料品 |
| APS_DeliveryDataDetail | ItemCode | APS_Material.Code | 料号 |
| APS_DeliveryDataDetail | SuplierCode | Dev_Supplier.Code | 供应商代码 |
| APS_DeliveryDataDetail | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_DeliveryDataDetail | ResourceNO | APS_PO.PODocNo | 采购单号 |
| APS_DeliveryDataDetail | Account | Dev_Account.Account | 物控账号 |
| APS_DeliveryData | ItemCode | APS_Material.Code | 料号 |
| APS_DeliveryData | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_DeliveryData | ResourceNO | APS_PO.PODocNo | 采购单号 |
| APS_DeliveryData | Account | Dev_Account.Account | 物控账号 |
| APS_DeliveryData | CreatedBy、ModifiedBy | Dev_Account.Account | 账号 |
| APS_OrderPlanMaterialFormExclude | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_OrderPlanMaterialFormExclude | CreatedBy、ModifiedBy | Dev_Account.Account | 账号 |
| APS_POScheduling | ID | APS_PO.ID | 采购订单行 |
| APS_POScheduling | SupplierCode | Dev_Supplier.Code | 供应商代码 |
| APS_POScheduling | Code | APS_Material.Code | 料号 |
| APS_DeliveryDataTmp | ItemCode | APS_Material.Code | 料号 |
| APS_DeliveryDataTmp | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_DeliveryDataTmp | ResourceNO | APS_PO.PODocNo | 采购单号 |
| APS_DeliveryDataTmp | Account | Dev_Account.Account | 物控账号 |
| APS_DeliveryDataTmp | CreatedBy、ModifiedBy | Dev_Account.Account | 账号 |
| APS_TaxRate | OrganizeID | Dev_Organize.OrganizeID | 组织（可空表示全局/集团） |
| APS_ContractPayment | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_ContractPayment | PartnerCode | Dev_Supplier.Code 等 | 相对方（按合同类型） |
| APS_CompanyPaymentAccount | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_ContractTerms | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_ContractTerms | ContractNo | — | 可与 APS_ContractPayment 等合同域同号关联 |
| APS_ContractTerms | ContractTermsID | — | APS_SalesContractDetail.ContractTermsID（可选，§50） |
| APS_SalesOrderReturn | SalesOrderID | APS_SalesOrder.SalesOrderID | 销售订单头 |
| APS_SalesOrderReturn | SalesOrderDetailID | APS_SalesOrderDetail.SalesOrderDetailID | 销售订单行 |
| APS_SalesOrderReturn | MaterialID | APS_Material.MaterialID | 料品 |
| APS_SalesOrderReturn | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_SalesContract | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| APS_SalesContract | SalesOrderID | APS_SalesOrder.SalesOrderID | 可选关联订单 |
| APS_SalesContract | SalesContractID | — | APS_ContractPayment/ContractTerms 的 ContractID（销售） |
| APS_SalesContractDetail | SalesContractID | APS_SalesContract.SalesContractID | 合同头 |
| APS_SalesContractDetail | ContractTermsID | APS_ContractTerms.ContractTermsID | 可选，挂条款行 |
| APS_PaymentMethod | OrganizeID | Dev_Organize.OrganizeID | 组织（可空表示全局/集团） |
| WMS_StockAdjust | OrganizeID | Dev_Organize.OrganizeID | 组织 |
| WMS_StockAdjustDetail | StockAdjustID | WMS_StockAdjust.StockAdjustID | 调整单头 |
| WMS_StockAdjustDetail | MaterialID | APS_Material.MaterialID | 料品 |
| APS_SalesReturn | ParentSalesReturnID | APS_SalesReturn.SalesReturnID | 上一张销售退货单头（自引用） |
| APS_SalesReturn | SalesOrderID | APS_SalesOrder.SalesOrderID | 溯源销售订单（可选） |
| APS_SalesReturnDetail | SalesReturnID | APS_SalesReturn.SalesReturnID | 退货单头 |
| APS_SalesReturnDetail | SalesOrderDetailID | APS_SalesOrderDetail.SalesOrderDetailID | 销售订单行（可选） |
| APS_SalesReturnDetail | SalesOrderReturnID | APS_SalesOrderReturn.SalesOrderReturnID | §48 订单行退货（可选） |
| APS_SalesReturnDetail | MaterialID | APS_Material.MaterialID | 料品 |

### 2.3 JOIN 示例

```sql
-- 生产订单 + 料品 + 销售订单行
SELECT o.OrderID, o.OrderNo, o.Qty, m.MaterialName, m.Code, sod.SalesOrderID, sod.DeliveryDate
FROM APS_Order o
LEFT JOIN APS_Material m ON o.MaterialID = m.MaterialID
LEFT JOIN APS_SalesOrderDetail sod ON o.SalesOrderDetailID = sod.SalesOrderDetailID;

-- 销售订单头 + 行 + 料品
SELECT so.SalesOrderNo, sod.LineNum, sod.Qty, m.MaterialName
FROM APS_SalesOrder so
JOIN APS_SalesOrderDetail sod ON so.SalesOrderID = sod.SalesOrderID
LEFT JOIN APS_Material m ON sod.MaterialID = m.MaterialID;

-- 生产订单 + 制造组织
SELECT o.OrderNo, o.MFGOrganizeID, org.OrganizeName
FROM APS_Order o
LEFT JOIN Dev_Organize org ON o.MFGOrganizeID = org.OrganizeID;

-- 销售合同展宽（等同 V_APS_SalesContract：组织名、订单号、父合同、ContractStatus）
SELECT v.SalesContractID, v.ContractNo, v.OrganizeName, v.CustomerName,
       v.SalesOrderNo, v.ParentContractNo, v.ContractStatus, v.TotalAmount
FROM V_APS_SalesContract v;

-- 合同—条款关联展宽（等同 V_APS_SalesContractDetail）
SELECT d.SalesContractDetailID, d.ContractNo, d.ContractStatus, d.ClauseSeq, d.ClauseTitle, d.TermsCategory
FROM V_APS_SalesContractDetail d
WHERE d.SalesContractID = 1;  -- 示例：替换为实际主键
```

---

## 三、各表详细结构

---

### 1. APS_Material（料品表）

物料/料品主数据表，存储料品的基础信息、库存策略、包装规格及扩展属性。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | MaterialID (bigint, 自增) |
| 默认值 | CreatedOn = getdate()，IsScheduling = 1 |

#### 主要字段

| 分类 | 字段 | 类型 | 说明 |
|------|------|------|------|
| 基础 | MaterialName | nvarchar(100) | 物料名称 |
| 基础 | Code | varchar(30) | 料号 |
| 基础 | Spec | varchar(800) | 规格 |
| 基础 | Model | varchar(300) | 型号 |
| 基础 | Unit | varchar(20) | 单位 |
| 价格 | Price | decimal(18,4) | 单价 |
| 库存 | SafetyStockQty | decimal(18,4) | 安全库存 |
| 排产 | IsScheduling | bit | 是否需要排产（默认1） |
| 排产 | FixedDay | int | 提前期（负=提前，正=延后） |
| 组织 | OrganizeID | int | 组织ID → Dev_Organize |
| ERP | ERPID | varchar(30) | ERP 主键，用于对接 |

#### 关联

- OrganizeID → Dev_Organize.OrganizeID
- MaterialID ← 被 APS_Order.MaterialID、APS_SalesOrderDetail.MaterialID 引用

#### 备注

- 已废弃：SystemID、ParentMaterialID

---

### 2. APS_Order（生产订单表）

生产/制造订单主表，存储订单数量、交期、排产状态、备料、包装出货等信息。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | OrderID (bigint, 自增) |
| 默认值 | ProductionStatus=26, StockOutQty=0, CompletionStatus=0 等 |

#### 主要字段

| 分类 | 字段 | 类型 | 说明 |
|------|------|------|------|
| 关联 | MaterialID | bigint | 料品ID |
| 关联 | SalesOrderDetailID | varchar(20) | 销售订单行ID |
| 关联 | MFGOrganizeID | int | 制造组织 |
| 关联 | OwnOrganizeID | int | 所属组织 |
| 数量 | Qty | decimal(18,4) | 订单数量 |
| 数量 | ProducedQty | decimal(18,4) | 已生产 |
| 数量 | NoSehcduingQty | decimal(18,4) | 未排数量（注意拼写） |
| 排产 | PlanDate | datetime | 排产日期 |
| 排产 | SalesPlanStatus | int | 0主计划 1月计划 2周计划 3日计划 |
| 交期 | DeliveryDate | datetime | 计划交期 |
| 交期 | ShipmentDay | datetime | 出货日期 |

#### 关联

- MaterialID → APS_Material.MaterialID
- SalesOrderDetailID → APS_SalesOrderDetail.SalesOrderDetailID
- MFGOrganizeID、OwnOrganizeID → Dev_Organize.OrganizeID

#### 备注

- 已废弃：GroupCabinet
- NoSehcduingQty 为未排数量（字段名拼写有误）

---

### 3. Dev_Organize（组织表）

组织/制造单元主数据表，存储生产线、车间等的层级结构、人力、效率、班制等。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | OrganizeID (int, 自增) |
| 默认值 | LineCount=1，ReportDays=1 |

#### 主要字段

| 分类 | 字段 | 类型 | 说明 |
|------|------|------|------|
| 基础 | OrganizeName | nvarchar(200) | 组织名称 |
| 层级 | ParentID | int | 上级ID |
| 人力 | TotalPeoples | int | 总人数 |
| 排产 | LineCount | int | 拉线数量 |
| 排产 | OrgEfficiency | decimal(18,2) | 效率 |
| ERP | ERPID | varchar(50) | ERP 主键 |

#### 关联

- OrganizeID ← 被 APS_Material、APS_Order、APS_SalesOrder、APS_SalesOrderDetail 引用

---

### 4. APS_SalesOrder（销售订单主表）

销售订单头表，存储销售订单主信息。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | SalesOrderID (varchar(20), 非自增) |
| 默认值 | ProductionStatus = 26 |

#### 主要字段

| 分类 | 字段 | 类型 | 说明 |
|------|------|------|------|
| 基础 | SalesOrderNo | varchar(30) | 销售订单号 |
| 组织 | OrganizeID | int | 组织ID |
| 客户 | CustomerID | bigint | 客户ID |
| 销售 | SalesMan | nvarchar(10) | 销售员/业务员 |
| 日期 | OrderDate | datetime | 下单日期 |

#### 关联

- OrganizeID → Dev_Organize.OrganizeID
- SalesOrderID ← 被 APS_SalesOrderDetail.SalesOrderID 引用

---

### 5. APS_SalesOrderDetail（销售订单行表）

销售订单明细行，存储料品、数量、交期等，是 APS_Order 的来源。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | SalesOrderDetailID (varchar(20), 非自增) |
| 默认值 | ProductionStatus=26, StockOutQty=0, StockInQty=0 等 |

#### 主要字段

| 分类 | 字段 | 类型 | 说明 |
|------|------|------|------|
| 关联 | SalesOrderID | varchar(20) | 销售订单ID |
| 关联 | MaterialID | bigint | 料品ID |
| 关联 | MFGOrganizeID | int | 制造组织 |
| 数量 | Qty | decimal(18,4) | 数量 |
| 数量 | ProducedQty | decimal(18,4) | 已生产 |
| 交期 | DeliveryDate | datetime | 计划交期 |
| 交期 | ActualDeliveryDate | date | 实际交货日期 |
| 交期 | PCDeliveryDate | datetime | 成品交期 |

#### 关联

- SalesOrderID → APS_SalesOrder.SalesOrderID
- MaterialID → APS_Material.MaterialID
- MFGOrganizeID → Dev_Organize.OrganizeID
- SalesOrderDetailID ← 被 APS_Order.SalesOrderDetailID、`APS_SalesOrderReturn.SalesOrderDetailID` 引用

#### 版本差异

| 环境 | 特点 |
|------|------|
| APS | 较简版，无备料/下发相关、无 Extend |
| EK0721 | 有 Q1~Q10、CustomerMaterialNo/Name、TaxPrice/TaxRate、Extend1~21 |
| APS20260323 | 有 FARAMOUNT、MONo、QualifiedQty、CanStockIn、OrganizeID 等 |

---

### 6. APS_OrderBOM（生产订单用料清单）

生产订单 BOM 明细表，存储生产订单的用料清单（子件料品、用量、领料状态等）。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | OrderBOMID (bigint, 自增) |

#### 主要字段

| 分类 | 字段 | 类型 | 说明 |
|------|------|------|------|
| 关联 | OrderID | bigint | 订单ID |
| 关联 | MaterialID | bigint | 子件料品ID |
| 数量 | DemandQty | decimal(24,6) | 需求数 |
| 数量 | QPA | decimal(24,6) | 用量（单耗） |
| 数量 | IssuedQty | decimal(24,6) | 已领用量 |
| 数量 | UnIssuedQty | decimal(24,6) | 未领用量 |
| 数量 | RcvQty | decimal(24,6) | 库存数量 |
| 数量 | OnloadQty | decimal(24,6) | 在途数量 |
| 数量 | OncheckQty | decimal(24,6) | 质检数量 |
| 发料 | IssueStatus | nvarchar(50) | 发料状态 |
| 发料 | IssueDate | datetime | 发料日期 |
| 属性 | Extend1 | varchar(50) | 物料属性（自制、采购、虚拟） |

#### 关联

- OrderID → APS_Order.OrderID
- MaterialID → APS_Material.MaterialID

#### 备注

- QPA = Molecule/Denominator × 订单数量
- Extend1：自制、采购、虚拟

---

### 7. APS_OrderProcess（生产订单工序表）

生产订单工序明细表，存储每个生产订单的工序信息。

#### 主要字段

| 分类 | 字段 | 说明 |
|------|------|------|
| 关联 | OrderID | 订单ID |
| 关联 | MaterialID | 产品ID |
| 关联 | ProcessGroupID | 工艺ID |
| 工序 | ProcessID、ProcessName | 工序ID、名称 |
| 上下工序 | PreProcessID、NextProcessID | 上工序、下工序 |
| 数量 | DemandQty、ProducedQty、SchedulingQty | 需求、报工、已排 |
| 日期 | StartDate、EndDate | 开始、结束日期 |
| 产能 | Capacity、Seconds、StandardPeoples | 产能、工时、人数 |

#### 关联

- OrderID → APS_Order.OrderID
- MaterialID → APS_Material.MaterialID
- OrganizeID → Dev_Organize.OrganizeID
- ProcessGroupID → APS_ProcessGroup.ProcessGroupID

---

### 8. APS_ProcessGroup（工艺表）

工艺/工艺路线组主数据表。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ProcessGroupID | 工艺ID（主键） |
| ProcessGroupName | 工艺名称 |
| OrganizeID | 组织ID |

#### 关联

- ProcessGroupID ← 被 APS_Order、APS_OrderProcess 引用
- OrganizeID → Dev_Organize.OrganizeID

#### 备注

- ByRegion、SchedulingSeq：已不用

---

### 9. APS_Process（工序表）

工序主数据表，存储工序基础信息。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ProcessID | 工序ID（主键） |
| ProcessName | 工序名称 |
| OrganizeID | 组织ID |
| Manager | 负责人 |
| SchedulingType | 排产类型 |
| Priority | 优先级 |
| TransferTime | 转线时间 |
| ProcessPartName | 工段 |

#### 关联

- ProcessID ← 被 APS_OrderProcess.ProcessID 引用
- OrganizeID → Dev_Organize.OrganizeID

#### 备注

- **APS_Process**：工序主数据；**APS_OrderProcess**：订单工序实例（含报工、排产）

---

### 10. APS_ProcessGroupInfo（工艺工序关联表）

工艺与工序的关联表，定义工艺路线中的工序组成及顺序。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ProcessGroupInfoID | 主键 |
| ProcessGroupID | 工艺ID |
| ProcessID | 工序ID |
| IsScheduling | 是否排产 |
| IsProduction | 是否报产 |
| ProcessPriority | 顺序 |
| PostProcessID | 自动后工序关联 |

#### 关联

- ProcessGroupInfoID ← 被 APS_OrderProcess 引用
- ProcessGroupID → APS_ProcessGroup
- ProcessID → APS_Process
- OrganizeID → Dev_Organize

#### 备注

- 工艺路线：ProcessGroup → ProcessGroupInfo → Process

---

### 11. APS_ProcessGroupMaterial（产品工艺表）

产品与工艺的关联表，定义料品使用哪条工艺路线。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ProcessGroupMaterialID | 主键 |
| ProcessGroupID | 工艺ID |
| MaterialID | 物料ID |
| OrganizeID | 组织ID |

#### 关联

- ProcessGroupID → APS_ProcessGroup
- MaterialID → APS_Material
- OrganizeID → Dev_Organize

---

### 12. Dev_Account（账号表）

用户/账号主数据表。

#### 主要字段

| 字段 | 说明 |
|------|------|
| Account | 账号（主键） |
| Name | 姓名 |
| OrganizeID | 组织ID |
| PositionID | 职位ID |
| LeadUserCode | 上级账号 |

#### 关联

- OrganizeID → Dev_Organize
- LeadUserCode → Dev_Account.Account（自关联）
- Account ← 被 APS_Order.Accounts 引用（计划员）

---

### 13. APS_OrderPlan（排产主表）

排产主表，与 APS_Order 一对一；订单排产后在此生成记录，表示已排产。

#### 主要字段

| 字段 | 说明 |
|------|------|
| FirstPlanID | 主键 |
| OrderID | 订单ID |
| MaterialID | 料品ID |
| SchedulingQty | 计划数量 |
| IssueType | 发料方式（整单/部分发料） |
| ComputingTime | 齐套计算日期 |

#### 关联

- OrderID → APS_Order
- MaterialID → APS_Material

#### 备注

- 与 APS_Order 1:1，表示订单已排产
- HasQty、AwaitQty、PlanQty、StartDate、EndDate、BatchNo 等已不用

---

### 14. APS_ProcessPartName（工段表）

工段主数据表。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ProcessPartName | 工段名称（主键） |
| OrganizeID | 组织ID |
| Manager | 负责人 |
| SchedulingType | 排产类型 |

#### 关联

- ProcessPartName ← 被 APS_Process.ProcessPartName 引用
- OrganizeID → Dev_Organize

---

### 15. APS_ProcessPartPlan（工段计划表）

工段计划明细表，按工段存储排产计划。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ProcessPartID | 主键 |
| OrderID | 订单ID |
| FirstPlanID | 预排ID |
| MaterialID | 料品ID |
| ProcessPartName | 工段名称 |
| PlanQty | 计划数 |
| StartDate、EndDate | 开始、完成日期 |
| PrepareDate | 发料日期 |

#### 关联

- OrderID → APS_Order
- FirstPlanID → APS_OrderPlan
- MaterialID → APS_Material
- ProcessPartName → APS_ProcessPartName

#### 备注

- 排产层级：APS_OrderPlan → APS_ProcessPartPlan（工段明细）→ APS_ProcessPlan（工序明细）
- WorkShopID、HasQty、DocNo 已不用
- ProcessPartID ← 被 APS_ProcessPlan 引用

---

### 16. APS_ProcessPlan（工序计划表）

工序计划明细表，按工序存储排产计划（计划数、产能、机台等）。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ProcessPlanID | 主键 |
| ProcessPartID | 工段计划ID |
| FirstPlanID | 订单计划ID |
| OrderID | 订单ID |
| MaterialID | 料品ID |
| ProcessID | 工序ID |
| PlanQty | 计划数 |
| HasQty | 报工数 |
| Capacity | 每小时产能 |
| StartDate、EndDate | 开始、结束日期 |
| ProductionStatus | 生产状态（26待下达，21已排产，25已完成） |

#### 关联

- ProcessPartID → APS_ProcessPartPlan
- FirstPlanID → APS_OrderPlan
- OrderID → APS_Order
- MaterialID → APS_Material
- ProcessID → APS_Process

#### 备注

- 排产层级：APS_ProcessPartPlan → APS_ProcessPlan（工序明细）→ APS_DayPlan（日计划）
- ProcessPlanID ← 被 APS_DayPlan 引用

---

### 17. APS_DayPlan（日计划表）

工序计划按天拆分表，存储每天的计划数、报工数、计划日期、达成情况等（工序计划每天数量）。

#### 主要字段

| 字段 | 说明 |
|------|------|
| DayPlanID | 主键 |
| ProcessPlanID | 工序计划ID |
| OrderID | 订单ID |
| MaterialID | 料品ID |
| PlanQty | 计划数 |
| HasQty | 报工数 |
| PlanDay | 计划日期 |
| StartTime、EndTime | 开始、结束日期时间 |
| Reason | 不达成原因 |
| ResponsibleDept | 责任部门 |
| IsReach | 是否达成 |

#### 关联

- ProcessPlanID → APS_ProcessPlan
- OrderID → APS_Order
- MaterialID → APS_Material

#### 备注

- 排产层级：APS_ProcessPlan → APS_DayPlan（日计划，按天拆分）

---

### 18. APS_PO（采购单表）

采购单，用于齐套计算、备料、SRM 送货对接。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ID | 主键 |
| OrderID | 生产订单 ID（基本上不用） |
| MaterialID、Code | 料品 ID、料号 |
| SupplierName、SupplierCode | 供应商名称、代码 |
| PODocNo、POLineNo | 采购单号、行号 |
| POQty | 采购数量 |
| ReceivedQty、ReturnedQty | 已收、已退货 |
| OnloadQty | 在途数量（订单数−已收） |
| DeliveryDate、DemandDate | 交期、需求日期 |
| ActualDeliveryDate、ActuaArrivalQty | 实际送货日期、实际送货数量 |
| ReplyDate、ReplyQty | 回复日期、回复数量 |
| Price、Amount、MonetaryUnit | 单价、金额、货币单位 |
| SCMStatus | SRM 送货状态（默认「未生成」） |
| ERPID、SyncDatetime | ERP 主键、同步日期 |
| Extend12 | 厂区 |

#### 关联

- OrderID → APS_Order
- MaterialID → APS_Material
- Code → APS_Material.Code
- SupplierCode → Dev_Supplier.Code

---

### 19. APS_ProcessPosition（工序职位关联表）

工序与职位/岗位关联，定义组织下工序对应的岗位、账号、工序等级。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ProcessPositionID | 主键 |
| ProcessID | 工序ID |
| PositionID | 岗位ID |
| OrganizeID | 组织ID |
| GroupID、GroupName | 群组 |
| Account | 账号 |
| LevelID、LevelName | 工序等级 |

#### 关联

- ProcessID → APS_Process
- OrganizeID → Dev_Organize
- Account → Dev_Account

---

### 20. Dev_PositionAccountMap（人员岗位关联表）

人员与岗位关联，定义账号对应的职位及岗位等级。

#### 主要字段

| 字段 | 说明 |
|------|------|
| PositionAccountID | 主键 |
| Account | 账号 |
| PositionID | 职位ID |
| PositionLevelID | 岗位等级ID |
| OrganizeID | 组织ID |
| GroupID、GroupName | 群组 |

#### 关联

- Account → Dev_Account
- OrganizeID → Dev_Organize
- PositionLevelID → Dev_PositionLevel

#### 备注

- 与 APS_ProcessPosition 配合：APS_ProcessPosition 为工序-职位，本表为人员-岗位

---

### 21. Dev_PositionLevel（岗位等级表）

岗位等级主数据，定义工序与职位对应的等级（工序等级对照表）。

#### 主要字段

| 字段 | 说明 |
|------|------|
| PositionLevelID | 主键 |
| PorcessID | 工序ID（拼写 Porcess） |
| PositionID | 职位ID |
| PositionLevelName | 岗位等级名称 |
| PositionLevel | 岗位等级（数值） |

#### 关联

- PorcessID → APS_Process
- OrganizeID → Dev_Organize
- PositionLevelID ← 被 Dev_PositionAccountMap 引用

---

### 22. Dev_PositionLevelMap（岗位技能等级配置表）

岗位/技能等级配置，定义职位等级名称、分数区间、复审间隔等。

#### 主要字段

| 字段 | 说明 |
|------|------|
| PositionLevelID | 主键 |
| PositionID | 职位ID |
| LevelName | 等级名称 |
| MinScore、MaxScore | 最低/最高分数 |
| IntervalTime、IntervalUnit | 间隔时间、单位 |

#### 关联

- OrganizeID → Dev_Organize
- PositionID → 职位表
- LevelName 可与 Dev_PositionExamRecord.LevelName 关联

#### 备注

- 与 Dev_PositionLevel 区别：本表按职位配置分数区间，Dev_PositionLevel 按工序+职位

---

### 23. APS_Machine（机台表）

机台主数据，APS_ProcessPlan.MachineID 引用本表。

#### 主要字段

| 字段 | 说明 |
|------|------|
| MachineID | 主键 |
| MachineCode、MachineName | 机台编号、名称 |
| MachineModel、MachineTypeID | 机台型号、类型 |
| MachineCapacity | 机台产能 |
| Tonnage | 机台吨位 |
| BasePoints、BaseMinute | 基准点、基准分钟 |

#### 关联

- MachineID ← 被 APS_ProcessPlan.MachineID 引用
- OrganizeID → Dev_Organize

---

### 24. APS_MachineMould（模具表）

模具主数据，APS_ProcessPlan.MachineMouldID 引用本表。

#### 主要字段

| 字段 | 说明 |
|------|------|
| MachineMouldID | 主键 |
| MoldNO、MoldName | 模具编号、名称 |
| MouldTonnage | 模具吨位 |
| MoldHole | 穴数 |
| MaintenanceDay | 保养周期 |
| PROPeriod | 周期 |

#### 关联

- MachineMouldID ← 被 APS_ProcessPlan.MachineMouldID 引用
- OrganizeID → Dev_Organize

---

### 25. APS_MachineMouldRelative（模具机台关系表）

模具与机台多对多关联，定义哪些模具可安装在哪些机台上。

#### 主要字段

| 字段 | 说明 |
|------|------|
| MachineMouldRelativeID | 主键 |
| MachineMouldID | 模具ID |
| MachineID | 机台ID |

#### 关联

- MachineMouldID → APS_MachineMould
- MachineID → APS_Machine

---

### 26. APS_MachineMouldMaterial（模具产品关系表）

模具与料品关联，定义哪些模具可生产哪些料品。

#### 主要字段

| 字段 | 说明 |
|------|------|
| MachineMouldMaterial | 主键（关系表ID） |
| MachineMouldID | 模具ID |
| MaterialID | 料品ID |

#### 关联

- MachineMouldID → APS_MachineMould
- MaterialID → APS_Material

---

### 27. APS_MaterialBOM（产品BOM表）

产品BOM主数据，料品级标准用料结构。与 APS_OrderBOM 区别：本表为料品标准BOM，APS_OrderBOM 为订单用料清单。

#### 主要字段

| 字段 | 说明 |
|------|------|
| MaterialBomID | 主键 |
| MaterialID | 子件料品ID |
| BOMMasterID | 母件ID |
| ParentID、ParentMaterialID | 上级BOM、上级物料 |
| QPA、Molecule、Denominator | 用量、分子、分母 |
| ProcessGroupInfoID | 工序 |
| IsScheduling | 是否排产 |

#### 关联

- MaterialID、BOMMasterID、ParentMaterialID → APS_Material
- ParentID → 本表（BOM层级）
- ProcessGroupInfoID → APS_ProcessGroupInfo
- OrganizeID → Dev_Organize

---

### 28. APS_PR（采购申请表）

采购申请，与 APS_PO 配合：PR 为申请，PO 为采购单。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ID | 主键 |
| DocNo | 单号 |
| MaterialID | 料品ID |
| ReqQty | 申请数量 |
| OrderQty | 已转采购数 |
| SurplusQty、DeliveredQty、UnaccountedQty | 剩余、到货、未清数量 |
| POTracker | 采购员 |

#### 关联

- MaterialID → APS_Material
- OrganizeID → Dev_Organize

---

### 29. Dev_PositionExamRecord（员工技能考核记录表）

员工技能考核记录，存储考试时间、产能目标、实际完成、分数、是否通过等。

#### 主要字段

| 字段 | 说明 |
|------|------|
| PositionAccountID | 主键 |
| PositionID | 职位ID |
| Account | 账号 |
| TestTime | 考试时间 |
| TargetNumber、ActualNumber | 产能目标、实际完成 |
| DefectiveNumber | 不良统计 |
| Score、IsPass | 分数、是否通过 |
| Assessor | 考核人 |

#### 关联

- Account → Dev_Account
- OrganizeID → Dev_Organize
- PositionID → 职位表
- LevelName 可与 Dev_PositionLevel.PositionLevelName 关联

---

### 30. WMS_Stock（库存表）

WMS 库存表，存储仓库料品库存、入库数、待检数，用于齐套计算、物料需求等。

#### 主要字段

| 字段 | 说明 |
|------|------|
| StockID | 主键，库存ID |
| WarehouseID | 仓库ID |
| MaterialID | 料品ID |
| MaterialName | 料品名称 |
| MFGOrganizeID | 制造组织 |
| StockQty | 库存数量 |
| InQty | 入库数 |
| OncheckQty | 待检数 |
| CustomerID | 客户ID |
| SSN | 条码 |
| ERPID | ERP 对接 ID |
| Status | 状态 |

#### 关联

- MaterialID → APS_Material
- WarehouseID → WMS_Warehouse
- CustomerID → 客户主数据
- MFGOrganizeID → Dev_Organize
- CreatedBy / ModifiedBy → Dev_Account

---

### 31. WMS_Warehouse（仓库表）

WMS 仓库主数据，存储仓库基础信息、地址、联系人及齐套/预警等业务开关。

#### 主要字段

| 字段 | 说明 |
|------|------|
| WarehouseID | 主键，仓库ID |
| ParentWarehouseID | 上级仓库ID |
| WarehouseName | 仓库名称 |
| Code | 仓库编码 |
| OrganizeID | 组织ID |
| Address、Contacts、Tel | 地址、联系人、电话 |
| Area | 功能区 |
| Manager | 负责人 |
| DataType、WarehouseTypeID、Property | 数据类型、仓库类型、仓库属性 |
| IsUsable | 计算供需平衡表 |
| IsWarning | 库存预警 |
| IsMating | 齐套运算 |
| Status | 状态 |

#### 关联

- OrganizeID → Dev_Organize
- ParentWarehouseID → 本表 WarehouseID
- CreatedBy / ModifiedBy → Dev_Account
- WarehouseID ← WMS_Stock.WarehouseID

---

### 32. APS_WorkingTimes（班别表）

上班时间配置表，存储班次/班别、上下班时间、工时、打卡规则及加班配置，供排产、考勤使用。

#### 主要字段

| 字段 | 说明 |
|------|------|
| WorkingTimesID | 主键，班别ID |
| ParentWorkingTimesID | 上级班别ID |
| WorkingTimesName | 班次名称 |
| StartTime、EndTime | 开始时间、结束时间 |
| WorkingType | 班次类型（上班、加班） |
| OrganizeID | 组织ID |
| WorkHour、RestHour、TotalHour | 上班时长、休息时长、总时长 |
| OverTimeHour | 加班时长 |
| TimeScope、EndTimeScope | 打卡区间 |
| CardNum | 打卡次数 |
| IsPunch、IsPunch2 | 是否免打卡 |
| LateOverTime、LeaveOverTime | 迟到、早退 |
| LateOverTime2、LeaveOverTime2 | 旷工相关 |
| StartDate、EndDate | 开始日期、截止日期 |
| Extend5、Extend6 | 使用周、顺序 |
| Status | 状态 |

#### 关联

- OrganizeID → Dev_Organize
- ParentWorkingTimesID → 本表 WorkingTimesID
- CreatedBy / ModifiedBy → Dev_Account
- WorkingTimesID ← APS_Order.WorkingTimesID

---

### 33. APS_Holiday（放假表）

放假/节假日配置表，按组织存储放假日期区间，供排产排除非工作日使用。

#### 主要字段

| 字段 | 说明 |
|------|------|
| HolidayID | 主键，放假ID |
| OrganizeID | 组织ID |
| StartDate | 开始日期 |
| EndDate | 结束日期 |
| Status | 状态 |
| SyncDatetime | 同步日期 |

#### 关联

- OrganizeID → Dev_Organize
- CreatedBy / ModifiedBy → Dev_Account

---

### 34. APS_OrganizeWorkingTimesDetail（排班明细表）

组织班别排班明细，按日期存储工作日排班（工作日期、班别、人数、总时长），供排产、产能计算使用。

#### 主要字段

| 字段 | 说明 |
|------|------|
| WorkingTimesDetailID | 主键，明细ID |
| OrganizeID | 组织ID |
| WorkingTimesID | 班别ID |
| WorkingDate | 工作日期 |
| Peoples | 人数 |
| TotalHours | 总时长 |
| Status | 状态 |
| SyncDatetime | 同步日期 |

#### 关联

- OrganizeID → Dev_Organize
- WorkingTimesID → APS_WorkingTimes
- CreatedBy / ModifiedBy → Dev_Account

---

### 35. APS_OrderPlanMaterialForm（物料齐套明细表）

物料齐套明细，存储预排/订单的料品配套信息（需求、库存、分配、欠数、在途、采购与复期、点检、替代料等），为齐套计算核心表。**字段与类型以主库 §35 及 APS 库 2026-04 脚本为准**（`Spec` **nvarchar(1000)**，`PODocs` **varchar(max)**，`POSuplierName` **nvarchar(max)**；主键 **`PK_APS_ORDERPLANMATERIALFORM`**；**`InspectStatus` / `IsReplyStatus` 默认0**）。

#### 主要字段

| 字段 | 说明 |
|------|------|
| MaterialFormID | 主键，配套ID |
| MaterialID、Code、Spec、MaterialName | 料品快照 |
| ProductCode/ProductName/ProductSpec | 成品快照 |
| FirstPlanID、OrderID、OrderNo、OrderBOMID | 预排、工单、用料 |
| SourceOrderNo、SourceOrderLineNo、SalesOrderDetailID | 来源单与销售行 |
| DemandQty、FormQty、OweQty、OweQty1~3、QPA、PlanQty、PlanQtyQPA | 需求/欠数/用量/计划 |
| Denominator、Molecule | 分数用量 |
| StockQty、StockQty1/2、StockQtyAllocation、IssuedQty、OncheckQty、OnloadQty | 库存与在制/待检/在途 |
| StockQtyAllocationPrepare*、StockQtyAllocationResult、OnCheckQtyAllocation | 分配与质检分配 |
| SubstitutesStockQty*、SubstitutesIssuedQty | 替代料 |
| UnIssuedQty、AllUnIssuedQty、ShortQty | 未领/短缺 |
| POTracker、PODocs、PODeliveryDate、PODeliveryQty、POSuplierName | 采购与供应商 |
| ReplyDate/Qty、First/Last/SecondReplyDate、Suplier*、DemandReplyDate、SetReplyDate | 复期与回复 |
| InspectStatus、InspectDate、InspectUser、IssueDate、InDate、InQty | 点检与来料 |
| IsReplyStatus、IsAbnormal、Notice | 状态与通知 |
| PrepareType、PrepareDate(1)、LineID、LineName、SN | 备料与产线 |
| CloseDate1/2、Close1/2、LastDate(1)、OweDate、PlanCreatedOn | 关闭与日期类 |
| Q1~Q3、Extend*、Remark1~6、PMCRemark | 扩展与备注 |
| ERPID、SyncDatetime、审计字段 | 同步与审计 |
| StockState、SIndex、DataSource、CompanyName、WorkShopName 等 | 其它 |

#### 关联

- MaterialID → APS_Material
- FirstPlanID → APS_OrderPlan
- OrderID → APS_Order
- OrderBOMID → APS_OrderBOM
- SalesOrderDetailID → APS_SalesOrderDetail（键类型以环境为准）
- CreatedBy / ModifiedBy → Dev_Account

---

### 36. APS_OrderPlanMaterialFormByItem（物料齐套按料号汇总表）

物料齐套按料号汇总，按料号维度汇总需求、欠数、库存、在途、采购及分时段欠数/需求（Q1~Q9）。无主键，通常由存储过程写入。

#### 主要字段

| 字段 | 说明 |
|------|------|
| MaterialID、Code、MaterialName | 料品ID、料号、物料名称 |
| DemandQty、OweQty、OweQty2 | 总需求、总欠数、欠数2 |
| StockQty、OncheckQty、OnloadQty | 库存、待检、在途 |
| SaveDate | 计算日期 |
| StartDate、OweDay、ConflictDate | 最早开拉、最早欠料、冲突日期 |
| OweQtyStatus | 欠数状态 |
| Q1~Q4 | 3日/4日/15日/历史欠数 |
| Q5~Q8 | 三日/七日/十五日/三十日需求 |
| Q9 | 扩展 |
| POTracker、POSuplierName、PODocs | 物控、供应商、采购单号 |
| SubstituteCode | 替代料 |
| MPQ | 最小包装数 |

#### 关联

- MaterialID → APS_Material
- 数据来源于 APS_OrderPlanMaterialForm 等齐套明细汇总

---

### 37. APS_DeliveryRule（送货计划规则表）

送货计划规则配置，按供应商/组织/基地配置送货规则类型（按周、按月、按需求、按PO数量）、规则值、送货天数及前置期。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ID | 主键 |
| SuplierCode | 供应商代码 |
| RuleType | 规则类型（按周、按月、按需求、按PO数量） |
| RuleValue | 规则值 |
| OrganizeID、OrganizeName | 组织ID、组织名称 |
| GroupID、GroupName | 群组ID、基地名称 |
| TransitTime | 送货天数 |
| LT | 前置期 |
| DeliveryRate | 送货频率 |
| Account、ControlID | 物控账号、控制者 ID |
| WERKS | 工厂代码 |
| ERPID、DataSource、SyncDatetime | ERP 对接、数据来源、同步日期 |
| Status | 状态 |

#### 关联

- OrganizeID → Dev_Organize
- Account → Dev_Account
- CreatedBy / ModifiedBy → Dev_Account
- SuplierCode → Dev_Supplier.Code

---

### 38. ERP_ZPPT036Temp（送货计划运算中间表）

送货计划运算中间表，从 SAP/ERP 下载 MRP 缺料、采购、库存等信息，供送货计划计算使用。

#### 主要字段（SAP 风格）

| 字段 | 说明 |
|------|------|
| OrganizeID、OrganizeName、GroupID、GroupName | 组织、群组 |
| MATNR | 物料料号 |
| BAUGR | 母件 |
| MAKTX、MAKTX2 | 物料名称、描述 |
| EBELN、EBELP | 采购凭证号、行号 |
| LIFNR、NAME_ORG1 | 供应商代码、名称 |
| BDMNG、ERFMG、STOCK | 需求量、需发数量、即时库存 |
| ZQLS、ZWFP、TRNRES | 欠数、未分配、采购未交 |
| OweAllQty | 总欠料数 |
| BDTER、EINDT、LFDAT | 需求日期、交货日期 |
| ZQLZT | 欠料状态 |
| SupplierMatch | 配比 |
| POID | 采购订单 ID |
| OrderID | 生产订单 ID |
| ID | 主键 |

#### 关联

- OrganizeID → Dev_Organize
- MATNR → APS_Material（料品）
- LIFNR → Dev_Supplier.Code（供应商）
- POID → APS_PO.ID
- OrderID → APS_Order.OrderID

---

### 39. APS_DeliveryDataDetail（送货分配过程明细表）

送货分配过程明细，存储送货计划分配结果：采购单、料品、未交量、欠料数、需求日期、回复交期、供应商等，供送货计划展示、SRM 对接使用。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ID | 主键 |
| POCreateDate | PO 创建日期 |
| ResourceNO | 采购单号 |
| LineNum | 行号 |
| MaterialID、ItemCode、ItemName | 料品ID、料号、物料名称 |
| AvailableQty | 采购单未交数量 |
| OweQty | 欠料数 |
| DemandDay、DemandToDay、ReplyDay | 需求日期、需求到料日期、回复交期 |
| APSDemandDay | APS 需求日期 |
| ProcurementSection、Account、ControlID | 采购组、物控账号、控制者 |
| SuplierCode、SuplierName | 供应商代码、名称 |
| OrganizeID、GroupID | 组织、群组 |
| MergeCount | 合并数 |

#### 关联

- MaterialID → APS_Material
- ItemCode → APS_Material.Code
- SuplierCode → Dev_Supplier.Code
- OrganizeID → Dev_Organize
- ResourceNO → APS_PO.PODocNo
- Account → Dev_Account

---

### 40. APS_DeliveryData（供应商送货计划表）

供应商送货计划主表：采购单、料号、采购未交量、欠数、需求/到料/回复日期、`DemandToDay1` 合并、物控、PMC 备注、供方复期、配比、工厂等。与 APS_DeliveryDataDetail 配合使用。无 `MaterialID`、供应商代码列，以 `ItemCode` 对应料品。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ID | 主键 |
| POCreateDate | PO 创建日期 |
| ResourceNO、LineNum | 采购单、行号 |
| ItemCode、ItemName、Spec、UnitName | 料号、名称、规格、单位 |
| AvailableQty、OweQty | 采购未交数量、欠数 |
| DemandDay、DemandToDay、ReplyDay、ReplyDate、SuplierReplyDate、APSDemandDay | 日期相关 |
| DemandToDay1、MergeCount | 需求到料日期合并、合并数 |
| ProcurementSection、Account、ControlID、POTracker | 采购组、账号、控制者、物控 |
| PMCRemark | PMC 备注 |
| OrganizeID、GroupID、Extend12 | 组织、群组、工厂 |
| SupplierMatch | 配比 |
| Status | 状态 |

#### 关联

- ItemCode → APS_Material.Code
- OrganizeID → Dev_Organize
- ResourceNO → APS_PO.PODocNo
- Account → Dev_Account
- CreatedBy / ModifiedBy → Dev_Account

---

### 41. APS_OrderPlanMaterialFormExclude（齐套物料排除规则表）

齐套物料排除规则，按组织/群组配置规则字段（RuleType）与规则值（RuleValue），齐套运算时用于排除 `APS_OrderPlanMaterialForm` 中匹配的行。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ID | 主键 |
| RuleType | 规则字段 |
| RuleValue | 规则值 |
| OrganizeID、OrganizeName、GroupID、GroupName | 组织、群组 |
| Status | 状态（默认 1） |
| ERPID、DataSource、SyncDatetime | ERP、数据来源、同步日期 |

#### 关联

- OrganizeID → Dev_Organize
- CreatedBy / ModifiedBy → Dev_Account
- 业务上按 RuleType/RuleValue 与 APS_OrderPlanMaterialForm 匹配排除

---

### 42. APS_POScheduling（运算送货分配采购订单临时表）

送货分配运算过程中的采购单行临时数据：在途数、分配用在途数、供应商、料号、送货日期等。无表级主键约束，常由存储过程维护。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ID | 常与 APS_PO.ID 对应 |
| PODocNo、POLineNo | 采购单号、行号 |
| OnloadQty、OnloadQty1 | 在途数、用于分配的在途数 |
| SupplierCode、SupplierName | 供应商 |
| Code、UnitName | 物料编码、单位 |
| DELIVERYDATE | 送货日期 |
| Extend1 | 生产组织代码 |
| CreatedOn、SyncDatetime | 创建、同步日期 |

#### 关联

- ID → APS_PO.ID
- PODocNo/POLineNo → APS_PO
- SupplierCode → Dev_Supplier.Code
- Code → APS_Material.Code

---

### 43. APS_DeliveryDataTmp（供应商送货计划临时表）

供应商送货计划运算临时表：采购单、料号、采购未交量、欠数、需求/到料/回复日期、`DemandToDay1` 合并串、物控等。无 `MaterialID`/`SuplierCode`，以 `ItemCode` 对应料品。

#### 主要字段

| 字段 | 说明 |
|------|------|
| ID | 主键 |
| ResourceNO、LineNum | 采购单、行号 |
| ItemCode、ItemName、Spec | 料号、名称、规格 |
| AvailableQty、OweQty | 采购未交、欠数 |
| DemandDay、DemandToDay、ReplyDay、APSDemandDay | 日期 |
| DemandToDay1 | 需求到料日期合并 |
| ProcurementSection、Account、ControlID | 采购组、物控、控制者 |
| OrganizeID、GroupID | 组织、群组 |
| MergeCount | 合并数 |

#### 关联

- ItemCode → APS_Material.Code
- ResourceNO → APS_PO.PODocNo
- OrganizeID → Dev_Organize
- Account → Dev_Account
- CreatedBy / ModifiedBy → Dev_Account

---

### 44. APS_TaxRate（税率主数据表）

按税码、组织及生效区间维护税率（支持历史版本与地区字段）；**无**独立税种字典表，税码由业务约定。单据行存 `TaxCode`，按业务日 + `OrganizeID` 解析本表生效区间。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | TaxRateID (bigint, 自增) |
| 默认值 | IsPercent = 1，Status = 1，CreatedOn = getdate() |
| 同步视图 | `V_APS_TaxRate` |

#### 主要字段

| 分类 | 字段 | 类型 | 说明 |
|------|------|------|------|
| 税 | TaxCode | varchar(20) | 税码 |
| 税 | TaxName | nvarchar(100) | 税率说明 |
| 税 | TaxRate | decimal(9,6) | 数值；配合 IsPercent（1=百分数，0=小数） |
| 组织 | OrganizeID | int | NULL 常表示全局/集团，由业务约定 |
| 地区 | CountryCode、RegionCode | char(2)、varchar(20) | 可选 |
| 区间 | EffectiveFrom、EffectiveTo | date | 起含当日；To 为空表示至今 |
| 状态 | Status | int | 0 草稿，1 确认；默认 1（见使用说明） |
| 备注 | Remark1、Remark2 | nvarchar(500) | 备注 1、备注 2 |
| ERP | ERPID、SyncDatetime | varchar(30)、datetime | 对接与同步时间 |

#### 索引

- `IX_APS_TaxRate_Code_Org_From`：(`TaxCode`, `OrganizeID`, `EffectiveFrom`)
- 同步只读视图：`V_APS_TaxRate`（作用、创建日期、创建人见视图扩展属性）

#### 关联

- OrganizeID → Dev_Organize.OrganizeID（若使用组织限定）

#### 备注

- **单段可执行 SQL（表+视图+扩展属性）**：`APS_数据库表结构知识库.md` → **§44.5**。

---

### 45. APS_ContractPayment（合同付款信息表）

合同分期/分笔付款：**合同编号 + 期序** 定位一笔付款；含计划金额与日期、已付金额、发票号、相对方快照及 `PayStatus`。预留 `ContractID` 便于日后关联合同主表。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | ContractPaymentID (bigint, 自增) |
| 典型默认值 | PaidAmount=0，PayStatus=0，Status=1，Currency=CNY，PhaseNo=1，CreatedOn=getdate() |
| 同步视图 | `V_APS_ContractPayment` |

#### 主要字段

| 分类 | 字段 | 类型 | 说明 |
|------|------|------|------|
| 合同 | ContractNo、ContractID、ContractType | varchar(50)、bigint、int | 编号；销售时 `ContractID`→`APS_SalesContract.SalesContractID`（§49）；1采购/2销售 |
| 期次 | PhaseNo、PaymentItemName | int、nvarchar(100) | 第几期、款项名称 |
| 金额 | PayPercent、PlanAmount、PaidAmount、Currency | decimal、varchar(10) | 比例(%数值)、计划额、已付、币别 |
| 日期 | PlanPayDate、ActualPayDate | date | 计划/实际付款日 |
| 状态 | PayStatus、Status | int | 收付进度、行是否有效 |
| 票据 | InvoiceNo | nvarchar(100) | 发票号 |
| 相对方 | PartnerCode、PartnerName | varchar、nvarchar | 编码与名称快照 |
| 备注 | Remark1、Remark2 | nvarchar(500) | |
| 审计 | Created*/Modified*、CreatedOn、ModifyedOn | | 与其它 APS 表一致 |
| ERP | ERPID、SyncDatetime | | |

#### 索引

- `IX_APS_ContractPayment_Contract`：(`OrganizeID`, `ContractNo`, `PhaseNo`)
- 同步只读视图：`V_APS_ContractPayment`（作用、创建日期、创建人见视图扩展属性）

#### 关联

- OrganizeID → Dev_Organize
- PartnerCode → 供应商/客户主数据（视 ContractType）
- `ContractID`（`ContractType`=2 销售）→ `APS_SalesContract.SalesContractID`

#### 备注

- **单段可执行 SQL（表+视图+扩展属性）**：`APS_数据库表结构知识库.md` → **§45.5**。

---

### 46. APS_CompanyPaymentAccount（本公司付款账号信息表）

本公司对外**付款/收款**用银行账号主数据：户名、开户行、支行、账号、币别、联行号、SWIFT、用途/对公对私、默认收付户、`OrganizeID` 归属等。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | CompanyPaymentAccountID (bigint, 自增) |
| 典型默认值 | Status=1，Currency=CNY，IsDefaultPay/IsDefaultReceive=0，CreatedOn=getdate() |
| 同步视图 | `V_APS_CompanyPaymentAccount` |

#### 主要字段

| 分类 | 字段 | 说明 |
|------|------|------|
| 归属 | OrganizeID、AccountCode | 组织、内部编码 |
| 银行 | AccountName、BankName、BankBranch、BankAccountNo、Currency | 户名、银行、支行、账号、币别 |
| 清算 | CNAPSCode、SwiftCode | 联行号、SWIFT |
| 用途 | AccountPurpose、AccountType、IsDefaultPay、IsDefaultReceive | 付/收/共用、对公对私、默认户 |
| 其它 | Status、Remark1、Remark2、审计、ERPID、SyncDatetime | 与 APS 惯例一致 |

#### 索引

- `IX_APS_CompanyPaymentAccount_Org`：(`OrganizeID`, `AccountCode`)
- `IX_APS_CompanyPaymentAccount_Org_AccountNo`：(`OrganizeID`, `BankAccountNo`)
- 同步只读视图：`V_APS_CompanyPaymentAccount`

#### 关联

- OrganizeID → Dev_Organize

#### 备注

- **单段可执行 SQL（表+视图+扩展属性）**：`APS_数据库表结构知识库.md` → **§46.5**。

---

### 47. APS_ContractTerms（合同条款信息表）

按**合同编号**维护条款：顺序号、标题、正文（`nvarchar(max)`）、大类（付款/交货/质量等）、是否必备、生效区间；预留 `ContractID`。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | ContractTermsID (bigint, 自增) |
| 典型默认值 | ClauseSeq=1，IsMandatory=1，Status=1，CreatedOn=getdate() |
| 同步视图 | `V_APS_ContractTerms` |

#### 主要字段

| 分类 | 字段 | 说明 |
|------|------|------|
| 定位 | OrganizeID、ContractID、ContractNo | 组织；销售时 `ContractID`→`APS_SalesContract.SalesContractID`（§49）；合同号 |
| 条款 | ClauseSeq、ClauseCode、ClauseTitle、ClauseContent | 顺序、编码、标题、正文 |
| 分类 | TermsCategory、IsMandatory | 大类、必备 |
| 区间 | EffectiveFrom、EffectiveTo | 可选 |
| 其它 | Status、Remark1/2、审计、ERPID、SyncDatetime | APS 惯例 |

#### 索引

- `IX_APS_ContractTerms_Contract`：(`OrganizeID`, `ContractNo`, `ClauseSeq`)
- 同步只读视图：`V_APS_ContractTerms`

#### 关联

- OrganizeID → Dev_Organize
- ContractNo 可与 `APS_ContractPayment` 等同号
- `ContractID`（销售）→ `APS_SalesContract.SalesContractID`
- `ContractTermsID` ← `APS_SalesContractDetail`（可选，§50）

#### 备注

- **单段可执行 SQL（表+视图+扩展属性）**：`APS_数据库表结构知识库.md` → **§47.5**。

---

### 48. APS_SalesOrderReturn（销售订单退货表）

相对 **`APS_SalesOrderDetail`** 记录退货：`SalesOrderID` / `SalesOrderDetailID` 均为 **varchar(20)**，与明细一致；可**多笔退货同一销售行**；`ReturnDocNo` 可聚合一单多行。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | SalesOrderReturnID (bigint, 自增) |
| 典型默认值 | Status=1，CreatedOn=getdate() |
| 同步视图 | `V_APS_SalesOrderReturn` |

#### 主要字段

| 分类 | 字段 | 说明 |
|------|------|------|
| 来源 | SalesOrderID、SalesOrderDetailID、SourceLineNum | 头/行键、行号快照 |
| 退货单 | ReturnDocNo、ReturnLineNum | 单号、退货行号 |
| 数量金额 | MaterialID、ReturnQty、Price/TaxPrice/TaxRate、ReturnAmount | 与明细数量口径一致 |
| 业务 | ReturnDate、ReturnType、原因、WarehouseCode、Status | |
| 其它 | Remark1/2、审计、ERPID、SyncDatetime | |

#### 索引

- `IX_APS_SalesOrderReturn_Detail`：(`SalesOrderDetailID`)
- `IX_APS_SalesOrderReturn_Doc`：(`SalesOrderID`, `ReturnDocNo`)
- 视图：`V_APS_SalesOrderReturn`

#### 关联

- 见主库 §48.4；建议回写或汇总 `APS_SalesOrderDetail.SalesReturnQty`

#### 备注

- 与 **§54～§55** 销售退货单明细可选 **`SalesOrderReturnID`** 衔接；见主库 **§48.6**。
- **单段可执行 SQL**：`APS_数据库表结构知识库.md` → **§48.5**。

---

### 49. APS_SalesContract（销售合同表）

销售**合同头表**：`ContractNo` 与 `APS_ContractTerms`、`APS_ContractPayment` 对齐；`SalesContractID` 填子表 `ContractID`（销售）。可选挂 `SalesOrderID`。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | SalesContractID (bigint, 自增) |
| 视图 | `V_APS_SalesContract`（JOIN + 关键列，非全量镜像） |

#### 主要维度

客户、金额/税、生效与签署、业务员、付款/交货摘要、`ParentContractID` 补充协议。

#### 索引与备注

- 组织+合同号、组织+客户、销售订单过滤索引；详见主库 **§49.5**。
- **`V_APS_SalesContract`**：`LEFT JOIN` **组织**（`OrganizeName`）、**销售订单**（`SalesOrderNo`/下单日/状态/业务员）、**父合同**（`ParentContractNo`/`ParentContractName`）；合同本体 **`Status`** 输出为 **`ContractStatus`**。

#### 备注

- 与条款关联明细见 **§50** `APS_SalesContractDetail`（仅合同+条款，无物料列）。
- **单段可执行 SQL**：`APS_数据库表结构知识库.md` → **§49.5**。

---

### 50. APS_SalesContractDetail（销售合同明细表）

销售合同与 **`APS_ContractTerms`** 的**关联行**：`SalesContractID`、`ContractTermsID`、`LineSeq`、备注与审计；**不含**料品、规格、数量、单价、金额等字段。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | SalesContractDetailID (bigint, 自增) |
| 视图 | `V_APS_SalesContractDetail` |

#### 主要维度

`SalesContractID`、`LineSeq`、`ContractTermsID`、状态与 `Remark1`/`Remark2`。

#### 索引与备注

- 按合同+行序、按条款过滤索引；详见主库 **§50.5**、**§50.6**。
- **`V_APS_SalesContractDetail`**：`INNER JOIN` 合同头、`LEFT JOIN` 条款，返回关联主键、**`ContractNo`/客户/金额/合同状态**、**条款序号/标题/分类/`TermsContractNo`** 等关键列（非 `SELECT *`）。

#### 备注

- **单段可执行 SQL**：`APS_数据库表结构知识库.md` → **§50.5**。

---

### 51. APS_PaymentMethod（支付方式表）

**支付方式主数据**：`MethodCode` / `MethodName`、`PayChannelType`、排序、启用；合同/订单等可选用 **`PaymentMethodID`** 或冗余编码与本表对齐。

#### 表概述

| 项目 | 说明 |
|------|------|
| 主键 | PaymentMethodID (bigint, 自增) |
| 视图 | `V_APS_PaymentMethod`（`SELECT *`） |

#### 索引与备注

- `IX_APS_PaymentMethod_Org_Code`：(`OrganizeID`, `MethodCode`)；详见主库 **§51.5**、**§51.6**。

#### 备注

- **单段可执行 SQL**：`APS_数据库表结构知识库.md` → **§51.5**。

---

### 52. WMS_StockAdjust（库存调整单头表）

**业务类型、调整原因、单号、仓库、状态**；明细见 **§53**。

| 项目 | 说明 |
|------|------|
| 主键 | StockAdjustID |
| 视图 | `V_WMS_StockAdjust` |

- **`BizType`**：1 入库 2 出库。  
- **可执行 SQL**：**§52.5～§53.5** 整段见**下文代码块**，并与同目录 **`WMS_StockAdjust_DDL.sql`**、主库 `APS_数据库表结构知识库.md` **§52.5、§53.5** 保持一致（改表须三处同步）。

---

### 53. WMS_StockAdjustDetail（库存调整单明细表）

**产品编号/名称/规格/单位**、**`OnHandQty`**（界面库存数量）、**`AdjustQty`**（调整库存数量）、行备注。

| 项目 | 说明 |
|------|------|
| 主键 | StockAdjustDetailID |
| 视图 | `V_WMS_StockAdjustDetail` |

- **`AdjustQty`** 为目标存量还是增减量见主库 **§53.6**。  

---

### 54. APS_SalesReturn（销售退货单头表）

**客户、销售出库单号、合计（退货数量/赠品数量/总金额）、头备注**；**`ParentSalesReturnID`** / **`PriorReturnDocNo`** 关联**上一张退货单头**。明细见 **§55**。与 **§48** 关系见主库 **§54.6**。

| 项目 | 说明 |
|------|------|
| 主键 | SalesReturnID |
| 视图 | `V_APS_SalesReturn` |

- **可执行 SQL**：**§54.5～§55.5** 整段见**下文代码块**（在库存调整 SQL 之后），并与 **`APS_SalesReturn_DDL.sql`**、主库 **§54.5、§55.5** 保持一致。

---

### 55. APS_SalesReturnDetail（销售退货单明细表）

**规格/单位、参考销售价、是否赠品、折扣%、单价、出库数量、剩余可退、退货数量、行金额**；可选 **`SalesOrderReturnID`** 接 **§48**。

| 项目 | 说明 |
|------|------|
| 主键 | SalesReturnDetailID |
| 视图 | `V_APS_SalesReturnDetail` |

---

#### §52.5～§53.5 完整可执行 SQL（`WMS_StockAdjust` / `WMS_StockAdjustDetail`）

```sql
-- ---------- §52 头表 WMS_StockAdjust + V_WMS_StockAdjust ----------

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

-- ---------- §53 明细 WMS_StockAdjustDetail + V_WMS_StockAdjustDetail ----------

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

#### §54.5～§55.5 完整可执行 SQL（`APS_SalesReturn` / `APS_SalesReturnDetail`）

与同目录 **`APS_SalesReturn_DDL.sql`**、主库 **§54.5、§55.5** 保持一致（改表须同步）。

```sql
-- APS 销售退货单（头/明细）建表脚本 — 与 APS_数据库表结构知识库.md §54.5、§55.5 及共享版 §54.5～§55.5 保持一致。
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

---

## 57. APS_InterfaceSAPOutputDetail（SAP 接口输出配置明细）

挂靠 **`dbo.APS_InterfaceSAPOutput`**（**`EID`**）；**FullData** / **LatestData** 为 **`varchar(max)`**（大文本）；**InputParameters** 为 **`nvarchar(4000)`**。详情见主库 **§57**。

### 57.5 表、索引、视图及扩展属性（完整脚本，与 **`APS_InterfaceSAPOutputDetail_DDL.sql`、主库 §57.5** 一致）

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

- **Status**：见共享版使用说明末尾约定（默认 **1**）。
- **同步视图**：`SELECT *`，列变更后 `EXEC sp_refreshview N'dbo.V_APS_InterfaceSAPOutputDetail';`。
- **主表增量（可选）**：**`APS_InterfaceSAPOutput`** 增加 **`EnableOutputDetail`**（`bit`，默认 `0`）；与 **`APS_InterfaceSAPOutput_Alter_EnableOutputDetail.sql`**、主库 **§57.6** 同一段 SQL。

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

- **主表传入参数（可选）**：**`dbo.APS_InterfaceSAPOutput`** 增加 **`InputParameters`**（**`nvarchar(max)`**，可空）；与 **`APS_InterfaceSAPOutput_Alter_InputParameters.sql`**、主库 **§57.6** 同一段 SQL。

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

## 58. APS_WorkCenter（工作中心清单）

SAP/ERP 同步的**工作中心主数据**；业务键 **`WERKS`（工厂）+ `WCCode`（工作中心编码）**；含有效期、反冲标志 **`RCMark`**、负责人 **`Manager`** 等。详情见主库 **§58**。

### 58.5 表、索引、视图及扩展属性（完整脚本，与 **`APS_WorkCenter_DDL.sql`、主库 §58.5** 一致）

```sql
-- APS 工作中心清单 APS_WorkCenter + V_APS_WorkCenter
-- 与 APS_数据库表结构知识库.md §58.5、共享版 §58.5 对齐。

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

- **Status**：0=草稿，1=确认；列默认 **DEFAULT(1)**。
- **同步视图**：`SELECT *`；列变更后 `EXEC sp_refreshview N'dbo.V_APS_WorkCenter';`。

---

## 附录：完整字段清单

如需查看各表的完整字段（含扩展字段、审计字段等），请参考项目中的 `APS_数据库表结构知识库.md` 文件。

---

*文档维护：如有表结构变更，请及时同步主库与共享版、**`APS_WorkCenter_DDL.sql`**（**§58.5**）、**`APS_InterfaceSAPOutputDetail_DDL.sql`**（**§57.5**）、**`APS_InterfaceSAPOutput_Alter_EnableOutputDetail.sql`**、**`APS_InterfaceSAPOutput_Alter_InputParameters.sql`**（**§57.6**，主表加列）、**`WMS_StockAdjust_DDL.sql`**、**`APS_SalesReturn_DDL.sql`** 及共享版文末 **§52.5～§53.5**、**§54.5～§55.5**、**§57.5～§57.6**、**§58.5** 代码块；增量表 §44～§58（含工作中心清单 **§58**、SAP 接口输出明细 **§57**）。**v1.20**（2026-06-10）与主库对齐。*
