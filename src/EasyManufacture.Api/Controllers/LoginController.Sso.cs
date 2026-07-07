using EasyManufacture.Api.Infrastructure;
using EasyManufacture.Entitys;
using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EasyManufacture.Api.Controllers;

public partial class LoginController
{
    /// <summary>OAuth SSO 登录（旧 <c>APSRequestSSOByOauthAsync</c>）。</summary>
    [HttpPost]
    public async Task<string> APSRequestSSOByOauthAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_body.BodyJson))
        {
            return JsonConvert.SerializeObject(new
            {
                result = false,
                msg = "未接受到数据，请确认是否为JSON格式"
            });
        }

        JObject? jObject;
        try
        {
            jObject = JsonConvert.DeserializeObject<JObject>(_body.BodyJson);
        }
        catch
        {
            jObject = null;
        }

        if (jObject == null || jObject.Count == 0)
        {
            return JsonConvert.SerializeObject(new
            {
                result = false,
                msg = "未接受到数据，请确认是否为JSON格式"
            });
        }

        var code = jObject["code"]?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(code))
        {
            return JsonConvert.SerializeObject(new
            {
                result = false,
                msg = "未获取到code参数"
            });
        }

        try
        {
            var parts = AppInfo.SSOUrl.Split('‖').Select(p => p.Trim()).ToArray();
            if (parts.Length < 5)
            {
                return JsonConvert.SerializeObject(new
                {
                    result = false,
                    msg = "App:SSOUrl 格式应为：项目名‖地址‖密钥‖token路径‖用户信息路径（五段，分隔符 ‖）"
                });
            }

            var getTokenUrl = parts[1] + parts[3] + $"&code={code}";
            string accessToken;

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(getTokenUrl, null, cancellationToken);
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        result = false,
                        msg = $"请求失败，状态码：{(int)response.StatusCode}，响应：{responseString}"
                    });
                }

                var json = JObject.Parse(responseString);
                accessToken = json["access_token"]?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(accessToken))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        result = false,
                        msg = "access_token 获取失败，响应内容：" + responseString
                    });
                }
            }

            var getUserInfoUrl = parts[1] + parts[4] + $"?&access_token={accessToken}";
            JObject userInfo;
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(getUserInfoUrl, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        result = false,
                        msg = $"获取用户信息失败，状态码：{(int)response.StatusCode}，响应：{responseContent}"
                    });
                }

                userInfo = JObject.Parse(responseContent);
            }

            var appAccount = userInfo["attributes"]?["appAccount"]?["appAccount"]?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(appAccount))
            {
                return JsonConvert.SerializeObject(new
                {
                    result = false,
                    msg = "未获取到有效的 appAccount"
                });
            }

            var user = await _accountService.CheckAccountAsync(appAccount, "", isAutoLogin: true, cancellationToken: cancellationToken);
            if (user == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    result = false,
                    msg = "账号或密码错误"
                });
            }

            var legacy = new V_Dev_Account
            {
                Account = user.Account,
                Name = user.Name,
                OrganizeID = user.OrganizeID ?? 0,
                CenterID = user.CenterID ?? 0,
                GroupID = user.GroupID ?? 0,
                WorkFlowInstanceID = user.WorkFlowInstanceID ?? user.Extend1,
                Extend1 = user.Extend1,
                Extend2 = user.Extend2,
                Extend3 = user.Extend3,
                Status = user.Status
            };

            V_Dev_Account.SetDev_Account(HttpContext, legacy);
            HttpContext.Session.SetString(AuthTokenResolver.SessionAccountKey, legacy.Account);

            return await CheckAccount(isSSO: true, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new
            {
                result = false,
                msg = "处理过程中发生异常：" + ex.Message
            });
        }
    }
}
