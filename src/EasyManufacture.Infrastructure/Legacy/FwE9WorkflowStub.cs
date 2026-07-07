using System.Data;
using FW.E9.Service.Model;

namespace FW.E9.Service.Service;

/// <summary>泛微 E9 OA 工作流占位（旧 Web References 未迁入 Net10 时的编译桩）。</summary>
public class WorkFlowService
{
    public int CreateOARequest(
        CreateOaEntity oaEntity,
        DataTable dataTable,
        string requestName,
        string workflowId,
        string tableDbName) => 0;
}
