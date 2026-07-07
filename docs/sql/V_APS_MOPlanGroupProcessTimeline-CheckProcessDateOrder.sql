/*
  8.排产汇总 — 工序日期顺序异常检查
  规则：按工序顺序，仅比较「都有值」的相邻工序；后一工序日期早于前一工序则报出
  另：同工序「完成」早于「开始」也报出
*/
SET NOCOUNT ON;

;WITH Base AS (
    SELECT  V.GroupDisplay,
            V.CustomerName,
            V.Code,
            V.MaterialName,
            V.ProcessGroupName,
            V.PCDeliveryDate,
            V.SmtStartDate,
            V.SmtEndDate,
            V.DipStartDate,
            V.DipEndDate,
            V.WeldStartDate,
            V.WeldEndDate,
            V.AssemblyStartDate,
            V.AssemblyEndDate,
            V.GroupKey
    FROM    dbo.V_APS_MOPlanGroupProcessTimeline AS V
),
Steps AS (
    SELECT  B.GroupKey,
            B.GroupDisplay,
            B.CustomerName,
            B.Code,
            B.MaterialName,
            X.StepOrd,
            X.StepName,
            X.StepDate
    FROM    Base AS B
            CROSS APPLY ( VALUES
                ( 1, N'要求开工', B.PCDeliveryDate ),
                ( 2, N'贴片开始', B.SmtStartDate ),
                ( 3, N'贴片完成', B.SmtEndDate ),
                ( 4, N'插件开始', B.DipStartDate ),
                ( 5, N'插件完成', B.DipEndDate ),
                ( 6, N'焊接开始', B.WeldStartDate ),
                ( 7, N'焊接完成', B.WeldEndDate ),
                ( 8, N'组装开始', B.AssemblyStartDate ),
                ( 9, N'组装完成', B.AssemblyEndDate )
            ) AS X ( StepOrd, StepName, StepDate )
    WHERE   X.StepDate IS NOT NULL
),
NextStep AS (
    SELECT  S.GroupKey,
            S.GroupDisplay,
            S.CustomerName,
            S.Code,
            S.MaterialName,
            S.StepOrd,
            S.StepName,
            S.StepDate,
            (
                SELECT  MIN(T.StepOrd)
                FROM    Steps AS T
                WHERE   T.GroupKey = S.GroupKey
                        AND T.StepOrd > S.StepOrd
            ) AS NextOrd
    FROM    Steps AS S
),
CrossProcess AS (
    SELECT  N.GroupDisplay,
            N.CustomerName,
            N.Code,
            N.MaterialName,
            N.StepName AS PrevStep,
            CONVERT(VARCHAR(10), N.StepDate, 120) AS PrevDate,
            M.StepName AS NextStep,
            CONVERT(VARCHAR(10), M.StepDate, 120) AS NextDate,
            DATEDIFF(DAY, M.StepDate, N.StepDate) AS LeadDays,
            N'后工序早于前工序' AS IssueType
    FROM    NextStep AS N
            INNER JOIN Steps AS M
                ON M.GroupKey = N.GroupKey
               AND M.StepOrd = N.NextOrd
    WHERE   M.StepDate < N.StepDate
),
SameProcess AS (
    SELECT  B.GroupDisplay,
            B.CustomerName,
            B.Code,
            B.MaterialName,
            X.StartName AS PrevStep,
            CONVERT(VARCHAR(10), X.StartDate, 120) AS PrevDate,
            X.EndName AS NextStep,
            CONVERT(VARCHAR(10), X.EndDate, 120) AS NextDate,
            DATEDIFF(DAY, X.EndDate, X.StartDate) AS LeadDays,
            N'完成早于开始' AS IssueType
    FROM    Base AS B
            CROSS APPLY ( VALUES
                ( N'贴片开始', B.SmtStartDate, N'贴片完成', B.SmtEndDate ),
                ( N'插件开始', B.DipStartDate, N'插件完成', B.DipEndDate ),
                ( N'焊接开始', B.WeldStartDate, N'焊接完成', B.WeldEndDate ),
                ( N'组装开始', B.AssemblyStartDate, N'组装完成', B.AssemblyEndDate )
            ) AS X ( StartName, StartDate, EndName, EndDate )
    WHERE   X.StartDate IS NOT NULL
            AND X.EndDate IS NOT NULL
            AND X.EndDate < X.StartDate
)
SELECT  IssueType, GroupDisplay, CustomerName, Code, MaterialName,
        PrevStep, PrevDate, NextStep, NextDate, LeadDays
FROM    CrossProcess
UNION ALL
SELECT  IssueType, GroupDisplay, CustomerName, Code, MaterialName,
        PrevStep, PrevDate, NextStep, NextDate, LeadDays
FROM    SameProcess
ORDER   BY IssueType, GroupDisplay, PrevStep;

SELECT  IssueType, COUNT(*) AS Cnt
FROM (
    SELECT IssueType FROM CrossProcess
    UNION ALL
    SELECT IssueType FROM SameProcess
) AS T
GROUP BY IssueType;

SELECT COUNT(*) AS TotalRows FROM dbo.V_APS_MOPlanGroupProcessTimeline;
