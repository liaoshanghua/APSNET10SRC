using System.Collections;
using System.Data;
using System.IO;
using System.Text;

namespace System.Web.UI
{
    /// <summary>兼容 System.Web.UI.HtmlTextWriter（编译桩）。</summary>
    public class HtmlTextWriter : TextWriter
    {
        private readonly TextWriter _inner;

        public HtmlTextWriter(TextWriter writer) => _inner = writer;

        public override Encoding Encoding => _inner.Encoding;

        public override void Write(char value) => _inner.Write(value);

        public override void Write(string? value) => _inner.Write(value);
    }
}

namespace System.Web.UI.WebControls
{
    /// <summary>兼容 System.Web.UI.WebControls.DataGrid（编译桩）。</summary>
    public class DataGrid
    {
        public object? DataSource { get; set; }

        public IDictionary Attributes { get; } = new Dictionary<string, string>();

        public void DataBind() { }

        public void RenderControl(UI.HtmlTextWriter writer) { }
    }
}
