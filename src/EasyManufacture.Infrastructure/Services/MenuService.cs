using EasyManufacture.Application.Abstractions;
using EasyManufacture.Infrastructure.Data;
using Newtonsoft.Json;

namespace EasyManufacture.Infrastructure.Services;

public sealed class MenuService : IMenuService
{
    private readonly DapperSqlHelper _sql;

    public MenuService(DapperSqlHelper sql) => _sql = sql;

    public async Task<string> GetMenuVueAsync(string account, CancellationToken cancellationToken = default)
    {
        var dt = await _sql.ExecuteDataTableAsync($@"
SELECT * FROM Dev_Menu WITH (NOLOCK)
WHERE IsAllVisible = 1 OR MenuCode IN (
    SELECT MenuCode FROM Dev_RoleMenuMap A WITH (NOLOCK)
    INNER JOIN Dev_Role B WITH (NOLOCK) ON A.RoleID = B.RoleID
    INNER JOIN Dev_RoleMap C WITH (NOLOCK) ON B.RoleID = C.RoleID
    WHERE C.Account = '{account.Replace("'", "''")}'
)
AND IsEnable = 1
ORDER BY ParentCode, ViewSort", cancellationToken);

        var list = new List<object>();
        foreach (System.Data.DataRow dr in dt.Select("ISNULL(ParentCode,'') = ''"))
        {
            list.Add(new
            {
                name = "iframe",
                url = dr["Url"].ToString(),
                text = dr["MenuName"].ToString(),
                size = 18,
                type = "md-home",
                children = GetChildren(dt, dr["MenuCode"].ToString()!)
            });
        }

        return JsonConvert.SerializeObject(list);
    }

    private static List<object>? GetChildren(System.Data.DataTable dt, string parentCode)
    {
        var list = new List<object>();
        foreach (System.Data.DataRow dr in dt.Select($"ParentCode = '{parentCode.Replace("'", "''")}'"))
        {
            list.Add(new
            {
                name = "iframe",
                url = dr["Url"].ToString(),
                text = dr["MenuName"].ToString(),
                size = 18,
                type = "md-home",
                children = GetChildren(dt, dr["MenuCode"].ToString()!)
            });
        }

        return list.Count > 0 ? list : null;
    }
}
