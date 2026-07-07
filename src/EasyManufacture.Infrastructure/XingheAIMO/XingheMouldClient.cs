using EasyManufacture.Licence;
using System.Text;
using System.Xml.Linq;

namespace EasyManufacture.Infrastructure.XingheAIMO;

/// <summary>星河模具 SOAP 客户端（仅 WApiGetMouldInfo，替代旧 Web References）。</summary>
public sealed class XingheMouldClient
{
    private readonly string _serviceUrl;

    public XingheMouldClient()
    {
        _serviceUrl = "http://192.168.1.250:85/MouldService.asmx";
    }

    public string WApiGetMouldInfo(string strSearchParam)
    {
        var envelope = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
              xmlns:xsd="http://www.w3.org/2001/XMLSchema"
              xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <WApiGetMouldInfo xmlns="http://tempuri.org/">
                  <strSearchParam>{System.Security.SecurityElement.Escape(strSearchParam)}</strSearchParam>
                </WApiGetMouldInfo>
              </soap:Body>
            </soap:Envelope>
            """;

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        using var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", "\"http://tempuri.org/WApiGetMouldInfo\"");
        var response = client.PostAsync(_serviceUrl, content).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        var xml = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var doc = XDocument.Parse(xml);
        XNamespace ns = "http://tempuri.org/";
        return doc.Descendants(ns + "WApiGetMouldInfoResult").FirstOrDefault()?.Value ?? string.Empty;
    }
}
