using System.Data;
using EasyManufacture.Entitys;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EasyManufacture.Infrastructure.Legacy;

public sealed partial class EasyManufactureEntities
{
    public IQueryable<Dev_Organize> Dev_Organize => _db.DevOrganizes;

    public LegacyDatabaseFacade Database => new(_db);

    public int P_aps_getstartdatebyenddate(
        string processPartName,
        long? materialID,
        long? workShopID,
        long? orderID,
        decimal? planQty,
        ObjectParameter startDate,
        ObjectParameter endDate,
        ObjectParameter expectTime,
        ObjectParameter result,
        ObjectParameter msg)
    {
        var parameters = new[]
        {
            new SqlParameter("@ProcessPartName", (object?)processPartName ?? DBNull.Value),
            new SqlParameter("@MaterialID", (object?)materialID ?? DBNull.Value),
            new SqlParameter("@WorkShopID", (object?)workShopID ?? DBNull.Value),
            new SqlParameter("@OrderID", (object?)orderID ?? DBNull.Value),
            new SqlParameter("@PlanQty", (object?)planQty ?? DBNull.Value),
            CreateOutputParameter("@StartDate", startDate),
            CreateOutputParameter("@EndDate", endDate),
            CreateOutputParameter("@ExpectTime", expectTime),
            CreateOutputParameter("@Result", result),
            CreateOutputParameter("@Msg", msg, SqlDbType.NVarChar, 500)
        };

        var returnValue = ExecuteStoredProcedure("P_aps_getstartdatebyenddate", parameters);
        CopyOutputParameters(parameters, startDate, endDate, expectTime, result, msg);
        return returnValue;
    }

    public int P_GetKeyValue(string tableName, string fieldName, string appCode, ObjectParameter billno)
    {
        var parameters = new[]
        {
            new SqlParameter("@TableName", tableName),
            new SqlParameter("@FieldName", fieldName),
            new SqlParameter("@AppCode", appCode),
            CreateOutputParameter("@billno", billno, SqlDbType.VarChar, 50)
        };

        var returnValue = ExecuteStoredProcedure("P_GetKeyValue", parameters);
        billno.Value = parameters[3].Value;
        return returnValue;
    }

    public int P_APS_OrderProcessAdd(long? orderID)
    {
        var parameters = new[]
        {
            new SqlParameter("@OrderID", (object?)orderID ?? DBNull.Value)
        };
        return ExecuteStoredProcedure("P_APS_OrderProcessAdd", parameters);
    }

    private static SqlParameter CreateOutputParameter(
        string name,
        ObjectParameter target,
        SqlDbType dbType = SqlDbType.DateTime,
        int size = 0)
    {
        var parameter = size > 0
            ? new SqlParameter(name, dbType, size) { Direction = ParameterDirection.Output }
            : new SqlParameter(name, dbType) { Direction = ParameterDirection.Output };
        parameter.Value = target.Value ?? DBNull.Value;
        return parameter;
    }

    private static void CopyOutputParameters(SqlParameter[] parameters, params ObjectParameter[] targets)
    {
        for (var i = 0; i < targets.Length; i++)
            targets[i].Value = parameters[parameters.Length - targets.Length + i].Value;
    }

    private static int ExecuteStoredProcedure(string procedureName, SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(SqlHelper.MSSQLConnectionString);
        using var command = new SqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddRange(parameters);
        connection.Open();
        return command.ExecuteNonQuery();
    }
}

public sealed class LegacyDatabaseFacade
{
    private readonly ManufactureDbContext _db;

    public LegacyDatabaseFacade(ManufactureDbContext db) => _db = db;

    public int ExecuteSqlCommand(string sql, params object[] parameters) =>
        _db.Database.ExecuteSqlRaw(sql, parameters);
}
