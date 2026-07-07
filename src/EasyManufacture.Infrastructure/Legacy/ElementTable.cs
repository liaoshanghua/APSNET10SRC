using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyManufacture.Core.ConfigTable
{
    public class ElementTableInput
    {
        public int ID
        {
            get;set;
        }
        public string Index
        {
            get;set;
        }
        public string ConfigStartWeek
        {
            get;set;
        }
      
    }
    public class Jspreadsheet
    {
        public string type
        {
            get;set;
        }
        public string title
        {
            get;set;
        }
        public double width
        {
            get;set;
        }
        public object[] source
        {
            get;set;
        }
        public string @decimal
        {
            get;set;
        }
        public string dicID
        {
            get;set;
        }
        public bool stripHTML
        {
            get
            {
                return false;
            }
        }
        public bool readOnly
        {
            get;set;
        }
    }
    public class ElementTableOuput
    {
        public ElementTableOuput()
        {
            this.visible = true;
        }
        public string label
        { get; set; }
        public string prop
        {
            get; set;
        }

        /// <summary>
        /// 替代显示的字符
        /// </summary>
        public string propName
        {
            get; set;
        }

        public string width
        {
            get; set;
        }
        public string fix
        {
            get; set;
        }
        public string sortable
        {
            get; set;
        }
        public object active
        {
            get; set;
        }
        public object icon
        {
            get; set;
        }
        public object button
        {
            get; set;
        }
        public string component
        {
            get; set;
        }
        public bool isEdit
        {
            get; set;
        }
        /// <summary>
        /// 行的记录是否需要合并
        /// </summary>
        public bool isMerge
        {
            get; set;
        }
        public string dicID
        {
            get; set;
        }
        public bool isLook
        {
            get; set;
        }
        public List<ElementTableOuput> children
        {
            get; set;
        }
        public bool routerName
        {
            get; set;
        }
        /// <summary>
        /// 列样式
        /// </summary>
        public bool cellStyle
        {
            get; set;
        }
        public string prop2
        {
            get; set;
        }
        public int left
        {
            get; set;
        }
        /// <summary>
        /// 树形显示的列
        /// </summary>
        public bool treeNode
        {
            get;set;
        }
        private string _align = "";
        public string align
        {
            get
            {
                if (string.IsNullOrEmpty(_align))
                {
                    _align = "left";
                }
                return _align;
            }
            set
            {
                _align = value;
            }
        }
        public string render
        {
            get; set;
        }
        public string className
        {
            get; set;
        }
        public string extend1
        {
            get; set;
        }
        public string extend2
        {
            get; set;
        }
        public string extend3
        {
            get; set;
        }
        public string extend4
        {
            get; set;
        }
        public string extend5
        {
            get; set;
        }
        public string extend6
        {
            get; set;
        }
        public string ValidType
        {
            get; set;
        }
        int _pageSize = 20;
        public int pageSize
        {
            get
            {
                return
                    _pageSize;

            }
            set
            {
                _pageSize = value;
            }
        }
        /// <summary>
        /// 后台格式化
        /// </summary>
        public string formater
        {
            get; set;
        }
        /// <summary>
        /// 前端格式化字符串
        /// </summary>
        public string formatter
        {
            get;set;
        }
        public string DataType
        {
            get; set;
        }
        /// <summary>
        /// 冻结的列数
        /// </summary>
        public int FixCount
        {
            get;set;
        }
        public string appWidth
        {
            get;set;
        }
        /// <summary>
        /// 数据集合
        /// </summary>
        public DataTable items
        {
            get; set;
        }
        public bool IsSelect
        {
            get;set;
        }
        public string name
        {
            get
            {
                return prop;
            }
        }
        public string displayName
        {
            get
            {
                return label;
            }
        }
        public int size
        {
            get
            {
                return  string.IsNullOrEmpty(width) ? 80 : int.Parse(width);
            }
        }
        public string cellType
        {
            get;set;
        }
        public string ControlType
        {
            get;set;
        }
        public string DataSourceName
        {
            get;set;
        }
        public string DataSourceID
        {
            get;set;
        }
        public bool? Required
        {
            get;set;
        }
        /// <summary>
        /// 数据集合
        /// </summary>
        public object Items
        {
            get;set;
        }
        public bool? IsVisibleApp
        {
            get;set;
        }
        public bool visible 
        {
            get;set;
        }
        /// <summary>
        /// 查询条件多少个
        /// </summary>
        public int? Region
        {
            get;set;
        }
    }
    /// <summary>
    /// 搜素
    /// </summary>
    public class SearchForm
    {
        public SearchForm()
        {
            this.queryType = new List<Dictionary<string, string>>();
        }
        public string type
        {
            get;set;
        }
        //type: "Input",
        //  label: "角色编码",
        //  prop: "RoleCode",
        //  width: "180px",
        //  placeholder: "",
        //  enter: () => this.handleSelect(),
        public string label
        {
            get;set;
        }
        public string width
        {
            get; set;
        }
        public string prop
        {
            get; set;
        }
        public string placeholder
        {
            get; set;
        }
        public string methods
        {
            get;set;
        }
        public DataTable options
        {
            get;set;
        }
        public string dicID
        {
            get;set;
        }
        public string icon
        {
            get;set;
        }
        public bool multiple
        {
            get;set;
        }
        public object value
        {
            get;set;
        }
        /// <summary>
        /// 查询方式
        /// </summary>
        public List<Dictionary<string, string>> queryType
        {
            get;set;
        }
    }
    public class ElButton
    {
        //label: "重置",
        //            type: "info",
        //            icon: "el-icon-refresh-right",
        //            methods: 'resetformsecond',
        //            signname: null,
        //            params: ''
        public string label
        {
            get;set;
        }
        public string icon
        {
            get; set;
        }
        public string methods
        {
            get; set;
        }
        public string signname
        {
            get; set;
        }
        public string param
        {
            get; set;
        }
    }
    public class Luckysheet
    {
        public Luckysheet()
        {
            this.data = new List<Sheet>();
        }
        public string title
        {
            get;set;
        }
        public List<Sheet> data
        {
            get;set;
        }
        public class Sheet
        {
            public Sheet()
                {
                 celldata = new List<CellData>();
                 config = new Config();
                filter_select = new Filter_Select();
                frozen = new Frozen();
                }
            /// <summary>
            /// 工作表名称
            /// </summary>
            public string name
            {
                get; set;
            } = "Sheet1";
            public int index
            {
                get; set;
            } = 0;
            public int status
            {
                get; set;
            } = 1;
            public List<CellData> celldata
            {
                get;set;
            }
            public class CellData
            {
                public CellData()
                {
                    v = new dataStyle();
                  
                }
                /// <summary>
                /// 行
                /// </summary>
                public int r
                {
                    get; set;
                } = 0;
                /// <summary>
                /// 列
                /// </summary>
                public int c
                {
                    get; set;
                } = 0;
                public dataStyle v
                {
                    get;set;
                }
                public  class dataStyle
                { 
          
                    public dataStyle()
                    {
                    
                    }
                    /// <summary>
                    /// 实际值
                    /// </summary>
                    public string v
                    {
                        get;set;
                    }
                    /// <summary>
                    /// 显示值
                    /// </summary>
                    public string m
                    {
                        get;set;
                    }
                    /// <summary>
                    /// 单元格值格式：文本、时间等
                    /// </summary>
                    public class celltype
                    {
                        public string fa
                        {
                            set; get;
                        } = "General";
                        public string t
                        {
                            get; set;
                        } = "g";

                    }
                    /// <summary>
                    /// 单元格值格式：文本、时间等
                    /// </summary>
                    public celltype ct
                    {
                        get;set;
                    }
              
                   /// <summary>
                   /// 背景色
                   /// </summary>
                    public string bg
                    {
                        get;set;
                    }
                    /// <summary>
                    /// 	0 Times New Roman、 1 Arial、2 Tahoma 、3 Verdana、4 微软雅黑、5 宋体（Song）、6 黑体（ST Heiti）、7 楷体（ST Kaiti）、 8 仿宋（ST FangSong）、9 新宋体（ST Song）、10 华文新魏、11 华文行楷、12 华文隶书
                    /// </summary>
                    public int ff
                    {
                        get; set;
                    } = 1;
                    /// <summary>
                    /// 字体,默认14；
                    /// </summary>
                    public int fs
                    {
                        get; set;
                    } = 10;
                    public string fc
                    {
                        get;set;
                    }
                    //public string bg
                    //{
                    //    get; set;
                    //}= "#fff000";
                    /// <summary>
                    /// 粗体,0 常规 、 1加粗
                    /// </summary>
                    public int bl
                    {
                        get; set;
                    } = 0;
                    /// <summary>
                    /// 斜体,0 常规 、 1 斜体
                    /// </summary>
                    public int it
                    {
                        get;set;
                    }
                    /// <summary>
                    /// 	垂直对齐,0 中间、1 上、2下
                    /// </summary>
                    public int vt
                    {
                        get;set;
                    }
                    /// <summary>
                    /// 	水平对齐,0 居中、1 左、2右
                    /// </summary>
                    public int ht
                    {
                        get; set;
                    } = 1;
                    ///// <summary>
                    ///// 合并单元格必备属性
                    ///// </summary>
                    //public mergecell mc
                    //{
                    //    get;set;
                    //}
                    ///// <summary>
                    ///// 合并单元格
                    ///// </summary>

                    //public class mergecell
                    //{
                    //    /// <summary>
                    //    /// 主单元格的行号
                    //    /// </summary>
                    //    public int r
                    //    {
                    //        get;set;
                    //    }
                    //    /// <summary>
                    //    /// 主单元格的列号
                    //    /// </summary>
                    //    public int c
                    //    {
                    //        get;set;
                    //    }
                    //    /// <summary>
                    //    /// 合并单元格占的行数
                    //    /// </summary>
                    //    public int rs
                    //    {
                    //        get;set;
                    //    }
                    //    /// <summary>
                    //    /// 合并单元格占的列数
                    //    /// </summary>
                    //    public int cs
                    //    {
                    //        get;set;
                    //    }
                    //}

                    /// <summary>
                    /// 	批注
                    /// </summary>
                    public class comment
                    {
                        public int height
                        {
                            get; set;
                        } = 140;
                        public int width
                        { get; set; } = 73;
                        public int left { get; set; } = 75;
                        public int top { get; set; } = 22;
                        /// <summary>
                        /// 是否显示
                        /// </summary>
                        public bool isshow { get; set; }
                        /// <summary>
                        /// 内容
                        /// </summary>
                        public string value
                        {
                            get;set;
                        }
                    }
                    /// <summary>
                    /// 批注
                    /// </summary>
                    public comment ps
                    {
                        get;set;
                    }
                }
            }

            public Config config
            {
                get;set;
            }
            public class Config
            {
                public Dictionary<string, int> columnlen
                {
                    get; set;
                } = new Dictionary<string, int>();
                public class BorderInfo
                {
                    /// <summary>
                    /// 范围类型分单个单元格和选区两种情况,默认cell
                    /// </summary>
                    public string rangeType
                    {
                        get;set;
                    } = "cell";
                   
                }
            }
            public Filter_Select filter_select
            {
                get;set;
            }
            /// <summary>
            /// 筛选
            /// </summary>
            public class Filter_Select
            {
                public int[] row
                {
                    get;set;
                }
                public int[] column
                {
                    get;set;
                }
            }
            public Frozen frozen
            {
                get;set;
            }
            public class Frozen
            {
                public Frozen()
                {
                    range = new Range();
                    type = "rangeBoth";
                    range.row_focus = 0;
                    range.column_focus = 6;
                }
                /// <summary>
                /// 类型，row，rangeRow，rangeBoth
                /// </summary>
                public string type
                {
                    get;set;
                }
                public Range range
                {
                    get;set;
                }
                public class Range
                {
                    public int row_focus
                    {
                        get;set;
                    }
                    public int column_focus
                    {
                        get;set;
                    }
                }
            }
        }
    }
    public class Spread
    {
         public string name
        {
            get;set;
        }
        public int size
        {
            get; set;
        } = 100;
        public string displayName
        {
            get;set;
        }
        public string formatter
        {
            get;set;
        }
        /// <summary>
        /// 单元格类型
        /// </summary>
        public int cellStyle
        {
            get;set;
        }
        /// <summary>
        /// 数据集合
        /// </summary>
        public DataTable  items
        {
            get;set;
        }
        public bool isEdit
        {
            get;set;
        }
    }
}
