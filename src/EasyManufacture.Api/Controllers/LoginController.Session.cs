using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace EasyManufacture.Api.Controllers;

public partial class LoginController
{
    /// <summary>刷新页面恢复登录态；与 <see cref="CheckAccount"/> 成功响应一致。</summary>
    [HttpGet]
    [HttpPost]
    public Task<string> GetSession(CancellationToken cancellationToken = default) =>
        CheckAccount(isSSO: false, cancellationToken: cancellationToken);
}
