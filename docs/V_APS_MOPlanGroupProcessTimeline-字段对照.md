# 新视图字段对照 — V_APS_MOPlanGroupProcessTimeline（8.排产汇总）

> **[盈瑞丰专用]** 参考字典 **6734**（`V_APS_MOPlanStep4`），菜单名称 **8.排产汇总**

## 分组规则

- `RemarkSalesOrderInfo` 有值 → 按群组汇总
- 群组为空 → 按 `OrderID` 汇总
- 页面「群组」列 `GroupDisplay`：有群组显示群组文本，无群组显示 `OrderNo`

## 图片 15 列 ↔ 视图字段

| # | 图片列名 | ParameterName | 来源/逻辑 |
|---|----------|---------------|-----------|
| 1 | 群组 | GroupDisplay | COALESCE(RemarkSalesOrderInfo, OrderNo) |
| 2 | 客户 | CustomerName | V_APS_MOPlanStep4，分组 MAX |
| 3 | 产品编码 | Code | 分组 MAX |
| 4 | 产品名称 | MaterialName | 分组 MAX |
| 5 | 数量 | PlanQty | 分组 MAX(PlanQty) |
| 6 | 工艺 | ProcessGroupName | 分组 MAX |
| 7 | 要求开工 | PCDeliveryDate | 分组 MIN |
| 8 | 贴片开始 | SmtStartDate | ProcessName 含 SMT/贴片，MIN(StartDate) |
| 9 | 贴片完成 | SmtEndDate | 同上，MAX(EndDate) |
| 10 | 插件开始 | DipStartDate | ProcessName 含「插件」 |
| 11 | 插件完成 | DipEndDate | 同上 |
| 12 | 焊接开始 | WeldStartDate | ProcessName 含「焊接」 |
| 13 | 焊接完成 | WeldEndDate | 同上 |
| 14 | 组装开始 | AssemblyStartDate | ProcessName 含「组装」 |
| 15 | 组装完成 | AssemblyEndDate | 同上 |

## 产出文件

| 阶段 | 文件 |
|------|------|
| A 建视图 | `docs/sql/V_APS_MOPlanGroupProcessTimeline.sql` |
| A 映射 | `docs/sql/V_APS_MOPlanGroupProcessTimeline-field-mapping.json` |
| B 栏位 | `docs/sql/V_APS_MOPlanGroupProcessTimeline-UpdateDevDictionaryField.sql` |
| C 菜单 | `docs/sql/V_APS_MOPlanGroupProcessTimeline-InsertDevMenu.sql` |
| 验证 | `docs/sql/V_APS_MOPlanGroupProcessTimeline-VerifyAll.sql` |
