using iTextSharp.text;
using iTextSharp.text.pdf;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>ISGO 等场景：PDF 加水印（替代旧 Server.MapPath + iTextSharp）。</summary>
public partial class ApsCoreEngine
{
    protected byte[] AddWatermarkToPdf(string pdfFilePath, string watermarkText, float opacity = 0.1f, int fontSize = 20)
    {
        using var ms = new MemoryStream();
        using var reader = new PdfReader(pdfFilePath);
        using var stamper = new PdfStamper(reader, ms);
        var pageCount = reader.NumberOfPages;
        var fontPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "SimHei.ttf");
        if (!System.IO.File.Exists(fontPath))
            return System.IO.File.ReadAllBytes(pdfFilePath);

        var baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
        for (var i = 1; i <= pageCount; i++)
        {
            var content = stamper.GetOverContent(i);
            content.SetFontAndSize(baseFont, fontSize);
            content.SetColorFill(BaseColor.GRAY);
            content.SetGState(new PdfGState { FillOpacity = opacity });
            var pageSize = reader.GetPageSize(i);
            var textWidth = pageSize.Width / 3;
            var textHeight = pageSize.Height / 3;
            var columns = (int)(pageSize.Width / textWidth) + 1;
            var rows = (int)(pageSize.Height / textHeight) + 1;
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var x = col * textWidth;
                    var y = pageSize.Height - row * textHeight;
                    content.BeginText();
                    content.ShowTextAligned(Element.ALIGN_CENTER, watermarkText, x, y, 45);
                    content.EndText();
                }
            }
        }

        stamper.Close();
        return ms.ToArray();
    }
}
