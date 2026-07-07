# 盈瑞丰 APS 滚动日计划 — 规则对照表

> **对照基准**  
> - 文档：`docs/盈瑞丰APS滚动日计划规则说明.docx`（v1.2）  
> - 存储过程：`P_新组装计划自动重算For手动`  
> - 数据源视图：`V_APS_MOPlanStep4`  
> - 对照日期：2026-06-15  

**结论图例**

| 标记 | 含义 |
|------|------|
| ✅ | 文档与实现一致 |
| ⚠️ | 部分一致 / 待确认 / 文档已说明限制 |
| ❌ | 文档与实现不一致 |
| ➖ | 文档未涉及 / 实现未做 |

---

## 一、总览

| 维度 | 一致项 | 待确认 | 不一致/待完善 |
|------|--------|--------|----------------|
| 数据范围与参与 | 8 | 0 | 0 |
| 数量与产能 | 7 | 1 | 0 |
| 日期约束 | 5 | 0 | 0 |
| 工序顺序与衔接 | 6 | 0 | 0 |
| 排产排序 | 4 | 0 | 0 |
| 日计划读写 | 6 | 0 | 0 |
| 锁定与手工干预 | 2 | 0 | 1 |
| 其它 | 4 | 1 | 1 |
| **合计** | **42** | **2** | **2** |

**总体评价**：核心业务规则（产能、日期、工序衔接、两轮重算）**文档与存储过程基本一致**；产能回退在**视图层**完成；主要缺口在**手工改数扣工时**、**调试代码清理**。

---

## 二、逐项对照

### 2.1 数据范围与参与条件

| # | 业务规则 | 文档描述 | 实现位置 | 结论 | 备注 |
|---|----------|----------|----------|------|------|
| 1 | 数据源 | `V_APS_MOPlanStep4` | SP 全程引用该视图 | ✅ | |
| 2 | 计划日期范围 | 仅今天及以后 | `PlanDay >= @Today`、`WorkingDate >= @Today` | ✅ | |
| 3 | 须分配产线 | `LineID > 0` | SP 同条件 | ✅ | |
| 4 | 参与排产 | `NoScheduling = 0` | 视图过滤 + SP 条件 | ✅ | |
| 5 | 未锁定才重算 | `IsLock = 0` | 自动重算分支 `IsLock = 0` | ✅ | |
| 6 | 须占用工时 | `NoWorkHours = 0` | SP 同条件 | ✅ | |
| 7 | 待计算标记 | `extend2 = '待计算'` | 重算前 UPDATE 为待计算 | ✅ | |
| 8 | 锁定/忽略工时不重算 | 沿用已有 DayPlan 汇总 | `IsLock=1 OR NoWorkHours=1` 直接回写 Start/End | ✅ | |

---

### 2.2 数量与产能

| # | 业务规则 | 文档描述 | 实现位置 | 结论 | 备注 |
|---|----------|----------|----------|------|------|
| 9 | 待排数量 | `OWEQTY`（已扣 HasQty） | `#T5.PlanQty` ← `A.OWEQTY` | ✅ | |
| 10 | 计划员产能优先 | 优先 PCCapacity | SP 用 `A.PCCapacity` | ✅ | |
| 11 | 空产能回退 IE | PCCapacity 空 → Capacity | **视图** `V_APS_MOPlanStep4` 已返回合并后的 PCCapacity | ✅ | **不在 SP 重复处理，设计正确** |
| 12 | 产能系数 | × Coefficient，默认 1 | `PCCapacity * ISNULL(Coefficient,1)` | ✅ | |
| 13 | SMT 人数 | 固定按 1 | `#T4`: `OrganizeName LIKE 'SMT%'` → 1 | ✅ | |
| 14 | 日历人数 | 用 Peoples 算日产量 | `#T4.Peoples`，默认 `ISNULL(Peoples,1)` | ✅ | |
| 15 | 单日产量上限 | DayUpperCapacity 限制 TmpQty | SP 中逻辑（注释/启用状态） | ⚠️ | 文档写已生效；**需以数据库当前脚本为准**确认是否已取消注释 |
| 16 | 插件 10000 上限 | 文档未写 | SP 注释块中曾有 `@ProcessName='插件'` 且 `TmpQty>10000` | ➖ | 若库中仍保留，建议在文档补充 |
| 17 | 钢网产能 | 无数据，仅颜色提示 | SP 无钢网逻辑 | ✅ | 文档与实现一致（均不拦截） |

**计算公式对照**

| 公式 | 文档 | SP |
|------|------|-----|
| 有效产能 | 视图.PCCapacity × 系数 | `PCCapacity * Coefficient` ✅ |
| 单日理论量 | 有效产能 × 剩余工时 × Peoples | `Capacity * TotalHours * Peoples` ✅ |
| 占用工时 | 计划量 / 有效产能 / Peoples | `TmpQty / Capacity / Peoples` ✅ |
| 锁定 ExpectTime | PlanQty/产能/人数（SMT 除 1） | `planqty/PCCapacity/( CASE SMT THEN 1 ELSE TotalPeoples` ✅ |

---

### 2.3 日期约束

| # | 业务规则 | 文档描述 | 实现位置 | 结论 | 备注 |
|---|----------|----------|----------|------|------|
| 18 | 要求开工日 PCDeliveryDate | 仅影响排序，不强制开工 | 排序 `ORDER BY ... PCDeliveryDate/ReplyDate`；选日用 SuggestedStartDate | ✅ | |
| 19 | PCStartDate | 未使用 | SP 未参与 | ✅ | |
| 20 | ReplyDate 物料复期 | 有值不能早于此日排 | 循环内：`SuggestedStartDate = ReplyDate` | ✅ | |
| 21 | SuggestedStartDate | 工序链传递 + 排产滚动 | 多段 UPDATE + 排产后 `PlanDay+1` | ✅ | |
| 22 | PreStartDate | 前序工序 StartDate 约束 | 第 2 轮 `PreStartDate = Prev.StartDate` | ✅ | |

**日期优先级（文档） vs 实现**

| 优先级 | 文档 | SP 是否体现 |
|--------|------|-------------|
| 1 ReplyDate | 硬约束 | ✅ 循环内强制 |
| 2 前道工序 PreStartDate | 硬约束 | ✅ 第 2 轮 + 选日 `>= SuggestedStartDate` |
| 3 SuggestedStartDate | 系统推算 | ✅ |
| 4 PCDeliveryDate | 仅排序 | ✅ 不用于选日强制 |

---

### 2.4 工序顺序与下道间隔

| # | 业务规则 | 文档描述 | 实现位置 | 结论 | 备注 |
|---|----------|----------|----------|------|------|
| 23 | 标准工序链 | 贴片→插件→焊接→组装→测试 | `#循环的工序` + CHAIN_ORD 1~5 | ✅ | 测试在第 2 轮全量 |
| 24 | 第一轮分批 | 贴片→插件→焊接→组装 | `@PhaseR 1~4` WHILE 循环 | ✅ | |
| 25 | 第二轮全量 | 含测试等，加 Priority | 无工序名过滤 + `ORDER BY Priority` | ✅ | |
| 26 | 工序链分组 | Extend16（销售单号） | `#工序的顺序` JOIN `APS_Order.Extend16` | ✅ | 多成品错链风险文档已说明 |
| 27 | 跳工序 | 找组内下一道实际工序 | `MIN(CHAIN_ORD)` + CROSS APPLY | ✅ | |
| 28 | 后道不早于前道 | 硬约束 | PreStartDate + SuggestedStartDate + Priority | ✅ | |

**下道开工间隔（核心对照）**

| 场景 | 文档 | SP 代码 | 结论 |
|------|------|---------|------|
| 贴片批完成 | 下道 +1 天 | `@PhaseR = 1` → +1 | ✅ |
| 下道是组装/包装 | +1 天 | `MinNext.NextChainOrd = 4` → +1 | ✅ |
| 包装判断方式 | **非工序名称**，按 NextChainOrd=4 | 同左 SQL | ✅ |
| 插件→焊接 | 当天 +0 | ELSE 0 | ✅ |
| 焊接→组装 | +1 天（下道是 4） | NextChainOrd=4 → +1 | ✅ |
| 组装→测试 | 当天 +0 | 下道 CHAIN_ORD=5 → ELSE 0 | ✅ |

```sql
-- 文档与 SP 一致的核心逻辑
SuggestedStartDate = DATEADD(DAY,
  CASE WHEN @PhaseR = 1 OR MinNext.NextChainOrd = 4 THEN 1 ELSE 0 END,
  A.StartDate)
```

---

### 2.5 排产排序

| # | 业务规则 | 文档描述 | 实现位置 | 结论 | 备注 |
|---|----------|----------|----------|------|------|
| 29 | 第 1 轮排序 | LineID → 交期/建议日 → ViewSort → PlanQty | `ORDER BY LineID, SuggestedStartDate/ReplyDate/PCDeliveryDate, ViewSort, PlanQty` | ✅ | |
| 30 | 第 2 轮排序 | 增加 Priority（工序顺序） | `ORDER BY LineID, Priority, ...` | ✅ | |
| 31 | 产线内展示序 | SystemViewSort | 按 `@LineIDOld` 递增 + 末段 ROW_NUMBER | ✅ | |
| 32 | 最终展示序 | 按 StartDate 等重算 | `PARTITION BY LineID ORDER BY StartDate, EndDate, SystemViewSort` | ✅ | |

---

### 2.6 日计划读写与清理

| # | 业务规则 | 文档描述 | 实现位置 | 结论 | 备注 |
|---|----------|----------|----------|------|------|
| 33 | 重算前标记 | 待自动更新 | `UPDATE APS_DayPlan SET EXTEND2='待自动更新'` | ✅ | |
| 34 | 新增日计划 | 自动插入 | INSERT `Extend2='自动插入'` | ✅ | |
| 35 | 更新日计划 | 自动更新 | UPDATE `Extend2='自动更新'` | ✅ | |
| 36 | 清理过期 | 仍为待自动更新的删除 | DELETE `EXTEND2='待自动更新'` | ✅ | |
| 37 | 回写工序日期 | StartDate/EndDate/ExpectTime | MIN/MAX/SUM DayPlan | ✅ | |
| 38 | 年月周字段 | 文档正文未详述 | `Years/Months/Weeks` UPDATE | ✅ | 附录可补充 |
| 39 | 页面计算时间 | Dictionary 7945 | `UPDATE Dev_Dictionary ... 7945` | ✅ | |

---

### 2.7 锁定与手工干预

| # | 业务规则 | 文档描述 | 实现位置 | 结论 | 备注 |
|---|----------|----------|----------|------|------|
| 40 | 锁定不重算但占工时 | 锁定计划从产能池扣除 | `#T4` LEFT JOIN 仅 **`IsLock=1`** 的 ExpectTime | ⚠️ | 锁定一致；**未锁定手工日计划**重算时会被覆盖 |
| 41 | 手工改数须保留则锁定 | 改完锁定 | 业务操作，SP 按 IsLock 分支 | ✅ | |
| 42 | 手工改数同步扣工时 | 改数应更新 ExpectTime；重算前统一扣 | **未实现统一扣减** | ❌ | 文档标注「待完善」，与现状一致 |

---

### 2.8 其它

| # | 业务规则 | 文档描述 | 实现位置 | 结论 | 备注 |
|---|----------|----------|----------|------|------|
| 43 | 按 ERP 无限产能 | 文档未强调 | `SchedulingType='按ERP和无限产能'` 但 `1=2` 永不生效 | ➖ | 等同未启用 |
| 44 | StandardPeoples | 暂不使用 | SP 加载未参与日产量计算 | ✅ | |
| 45 | IsUsedUpper | 已废弃 | INSERT 固定 0 | ✅ | |
| 46 | 调试代码 | 文档未写 | `ProcessPlanID=1708` SELECT/IF | ❌ | **建议删除** |
| 47 | 同销售单多成品链条 | 可能不准，待客户确认 | Extend16 分组 | ⚠️ | 文档与实现一致，业务规则待定 |
| 48 | 安全循环上限 | 单条最多 300 天 | `@COUNT3=300` | ✅ | |

---

## 三、分层职责（建议实施理解）

```
┌─────────────────────────────────────────┐
│  V_APS_MOPlanStep4（视图）                  │
│  · OWEQTY、Priority、DayUpperCapacity 等    │
│  · PCCapacity 空 → 回退 Capacity          │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│  P_新组装计划自动重算For手动（存储过程）       │
│  · 产线工时池 #T4、分批/全量排产            │
│  · ReplyDate、工序链、+1天逻辑              │
│  · 写 APS_DayPlan、回写 ProcessPlan         │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│  前端 / 业务操作                           │
│  · 锁定、手工改数、钢网颜色提示             │
│  · 滚动日计划页（Dictionary 7945）          │
└─────────────────────────────────────────┘
```

---

## 四、仍待处理事项

| 优先级 | 事项 | 文档 | SP/视图 | 建议 |
|--------|------|------|---------|------|
| P1 | 删除调试代码 1708 | ➖ | ❌ 存在 | DBA 清理 |
| P1 | 手工改数扣工时 | 待完善 | ❌ 未统一 | 改数保存时重算 ExpectTime，或重算前扣全部有效 DayPlan |
| P2 | DayUpperCapacity | 已生效 | ⚠️ 待确认库脚本 | 核对生产库是否已取消注释 |
| P2 | 插件 10000 上限 | 未写 | 可能在 SP | 确认后补文档或删除 |
| P3 | 工序链分组 Extend16 | 待客户确认 | 已实现 | 客户确认后决定是否改 OrderID |
| P3 | 钢网 | 仅提示 | 无 | 维持现状 |

---

## 五、版本记录

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0 | 2026-06-15 | 初版对照（含误判 PCCapacity、包装工序名） |
| 1.1 | 2026-06-15 | 修正：包装 = NextChainOrd=4；PCCapacity 回退在视图 |
| 1.2 | 2026-06-15 | 完整对照表，标注一致/待确认/不一致 |

---

*本文档随 `盈瑞丰APS滚动日计划规则说明.docx` 及存储过程变更同步更新。*
