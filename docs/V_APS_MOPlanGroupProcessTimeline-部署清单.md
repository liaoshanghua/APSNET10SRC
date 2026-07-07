# 智能新视图 — 8.排产汇总 部署清单

> **[盈瑞丰]** 参考字典 6734 | 菜单 **8.排产汇总** | 无清理步骤

| 项 | 值 |
|---|---|
| 参考字典 | 6734 → `V_APS_MOPlanStep4` |
| 新视图 | `V_APS_MOPlanGroupProcessTimeline` |
| 菜单名称 | 8.排产汇总 |
| 上级菜单 | M2312230003 |
| 新菜单编码 | M2606120004 |
| 新字典 ID | 28649（系统复制后若不同请改 B/C 脚本） |
| 路由 | dic28649 |

## 执行顺序

| 步 | 阶段 | 操作 |
|----|------|------|
| ① | A | 执行 `V_APS_MOPlanGroupProcessTimeline.sql` 建视图 |
| ② | — | **系统界面**：复制字典 6734 → 新字典，表名 `V_APS_MOPlanGroupProcessTimeline`，插入栏位 |
| ③ | B | 执行 `UpdateDevDictionaryField.sql`（改 @DictionaryId） |
| ④ | C | 执行 `InsertDevMenu.sql`（仅 INSERT 新菜单） |
| ⑤ | — | 执行 `VerifyAll.sql`，重新登录 APS 验证 |

## 阶段 C 菜单字段

| 界面列 | 字段 | 值 |
|--------|------|-----|
| 菜单名称 | MenuName | 8.排产汇总 |
| 路由地址 | Url | dic28649 |
| 链接参数 | TargetFor | tableType=excel |
| ICO图标 | Ico | el-icon-s-order |
| 组件 | Component | PageCommon/CommonReport |
| 组件名称 | Name | CommonReport |
| dicID | Remark2 | 28649 |

字段定义见 `docs/菜单字典-Dev_Menu.xlsx`。
