using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using EasyManufacture.Licence;

namespace EasyManufacture.Infrastructure.Legacy
{
    public static class StringHelper
    {
        /// <summary>
        /// 判断是否是数字
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool IsNumber(string val)
        {
            decimal tmp;
            bool result = decimal.TryParse(val, out tmp);
            return result;
        }
        /// <summary>
        /// 根据一个数据集合返回XML字符串
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static string ConvertDataSetToXML(DataSet ds)
        {
            XmlDocument xmldoc = new XmlDocument();
            XmlNode xnode = xmldoc.CreateXmlDeclaration("1.0", "utf-8", "yes");
            xmldoc.AppendChild(xnode);
            XmlElement dsXml = xmldoc.CreateElement("NewDataSet");
            xmldoc.AppendChild(dsXml);
            foreach (DataTable dt in ds.Tables)
            {

                // 增加根节点

                foreach (DataRow dr in dt.Rows)
                {
                    XmlElement dtXml = xmldoc.CreateElement(dt.TableName);
                    dsXml.AppendChild(dtXml);
                    foreach (DataColumn dc in dt.Columns)
                    {
                        XmlElement subElm0 = xmldoc.CreateElement(dc.ColumnName);
                        subElm0.InnerText = dr[dc.ColumnName].ToString();
                        dtXml.AppendChild(subElm0);
                    }

                }

            }

            return xmldoc.InnerXml;
        }
        /// <summary>
        /// 将JS日期转成日期格式
        /// </summary>
        /// <returns></returns>
        public static DateTime? ReverseToDateTime(string date)
        {
            string fmtDate = "ddd MMM d HH:mm:ss 'UTC'zz'00' yyyy";
            CultureInfo ciDate = CultureInfo.CreateSpecificCulture("en-US");
            DateTime? d = null;
            //将JS时间字符串转换成C#时间  
            if (!string.IsNullOrEmpty(date))
            {
                if (date.IndexOf("UTC") > -1)
                {
                    d = DateTime.ParseExact(date, fmtDate, ciDate);
                }
                else
                {
                    d = DateTime.Parse(date);
                }
            }
            return d;
        }
        /// <summary>
        /// 将两个对象实例属性赋值
        /// </summary>
        /// <param name="source">源对象</param>
        /// <param name="target">目标对象</param>
        public static void SetObjectPro(object source, object target)
        {
            PropertyInfo[] pTarget = target.GetType().GetProperties();
            PropertyInfo[] pSource = source.GetType().GetProperties();
            foreach (PropertyInfo p in pTarget)
            {
                var obj = pSource.Where(m => m.Name == p.Name).FirstOrDefault();
                if (obj != null)
                {
                    p.SetValue(target, obj.GetValue(source, null), null);
                }
            }
        }

        /// <summary>
        /// 转JS日期格式
        /// </summary>
        /// <returns></returns>
        public static string ToUCTFormat(DateTime date)
        {
            //设置转换格式  需要引入命名空间：using System.Globalization;    
            string fmtDate = "ddd MMM d HH:mm:ss 'UTC'zz'00' yyyy";
            CultureInfo ciDate = CultureInfo.CreateSpecificCulture("en-US");
            //将C#时间转换成JS时间字符串    
            return date.ToString(fmtDate, ciDate);
        }

        private const string DESDecryptPassword = ")(*&!@#$";
        /// <summary> 
        /// 利用DES加密一个字符串 
        /// </summary> 
        /// <param name="str">要加密的字符串</param> 
        /// <param name="sKey">密钥</param> 
        /// <returns>密文</returns> 
        public static string DESEncrypt(string str)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            //将字符串转化为一个byte数组 
            byte[] inputByteArray = Encoding.UTF8.GetBytes(str);

            //Create the crypto objects, with the key, as passed in 
            des.Key = ASCIIEncoding.ASCII.GetBytes(DESDecryptPassword);
            des.IV = ASCIIEncoding.ASCII.GetBytes(DESDecryptPassword);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(),
            CryptoStreamMode.Write);
            //Write the byte array into the crypto stream 
            //(It will end up in the memory stream) 
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();

            //Get the data back from the memory stream, and into a string 
            StringBuilder ret = new StringBuilder();
            foreach (byte b in ms.ToArray())
            {
                //Format as hex 
                ret.AppendFormat("{0:X2}", b);
            }
            ms.Dispose();
            des.Dispose();
            return ret.ToString();
        }

        /// <summary> 
        /// 解密一个字符串 
        /// </summary> 
        /// <param name="str">密文</param> 
        /// <param name="sKey">密钥</param> 
        /// <returns>明文</returns> 
        public static string DESDecrypt(string str)
        {
            string result = str;
            try
            {
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();

                //Put the input string into the byte array 
                byte[] inputByteArray = new byte[str.Length / 2];
                for (int x = 0; x < str.Length / 2; x++)
                {
                    int i = (Convert.ToInt32(str.Substring(x * 2, 2), 16));
                    inputByteArray[x] = (byte)i;
                }

                //Create the crypto objects 
                des.Key = ASCIIEncoding.ASCII.GetBytes(DESDecryptPassword);
                des.IV = ASCIIEncoding.ASCII.GetBytes(DESDecryptPassword);
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(),
                CryptoStreamMode.Write);
                //Flush the data through the crypto stream into the memory stream 
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();

                //Get the decrypted data back from the memory stream 
                StringBuilder ret = new StringBuilder();
                foreach (byte b in ms.ToArray())
                {
                    ret.Append((char)b);
                }
                result = ret.ToString();
            }
            catch
            {
                 
            }
            return result;
        }

        /// <summary>
        /// 获取默认值
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="configValue"></param>
        /// <returns></returns>
        public static Object GetDefaultValue(string tableName, string configValue, string parameterName, bool isSearch)
        {
            object result = "";
            if (!string.IsNullOrEmpty(configValue))
            {



                switch (configValue.ToUpper())
                {
                    case "当前日期":
                        result = DateTime.Now;
                        break;
                    case "当前日期范围":
                        result = new string[2] { string.Format("{0:yyyy-MM-dd}", DateTime.Now.Date), string.Format("{0:yyyy-MM-dd}", DateTime.Now.Date) };
                        break;
                    case "选择项1":
                        result = 0;
                        break;
                    case "选择项2":
                        result = 1;
                        break;
                    case "选择项3":
                        result = 2;
                        break;
                    case "选择项4":
                        result = 3;
                        break;
                    case "选择项5":
                        result = 4;
                        break;
                    case "选择项6":
                        result = 5;
                        break;
                    case "当前月":
                       // result = new DateTime?[] { DateTime.Now.AddDays(-DateTime.Now.Day + 1), DateTime.Now.AddMonths(1).AddDays(-DateTime.Now.Day) };
                        result = new string[2] { string.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-DateTime.Now.Day + 1)), string.Format("{0:yyyy-MM-dd}", DateTime.Now.AddMonths(1).AddDays(-DateTime.Now.Day)) };
                        break;
                    case "本周":
                        int week = (int)DateTime.Now.DayOfWeek;
                        if (week == 0)
                        {
                            week = 7;
                        }
                      //  result = new DateTime?[] { DateTime.Now.AddDays(-week + 1), DateTime.Now.AddDays(-week).AddDays(7) };
                        result = new string[2] { string.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-week + 1)), string.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-week).AddDays(7)) };

                        break;
                    case "自动生成":
                        if (isSearch == false)
                        {
                            result = SqlHelper.GetKeyValue(tableName, parameterName, AppInfo.AppCode);
                        }

                        break;
                    case "大于等于今天":
                        result = new DateTime?[] { DateTime.Now.Date, DateTime.Now.AddDays(100).Date};
                        break;
                    default:
                        result = configValue;
                        break;
                    case "过去30天":
                        result = new string[2] { string.Format("{0:yyyy-MM-dd}", DateTime.Now.Date.AddDays(-30)), string.Format("{0:yyyy-MM-dd}", DateTime.Now.Date) };
                        break;
                    case "未来30天":
                        result = new string[2] { string.Format("{0:yyyy-MM-dd}", DateTime.Now.Date.AddDays(0)), string.Format("{0:yyyy-MM-dd}", DateTime.Now.Date.AddDays(30)) };
                        break;
                    case "年":
                        result = DateTime.Now.Year.ToString();
                        break;
                    case "月":
                        result = new string[2] { string.Format("{0:yyyy-MM}", DateTime.Now.Date.AddDays(0)), string.Format("{0:yyyy-MM-dd}", DateTime.Now.Date.AddDays(30)) };
                        break;
                    case "周":
                        GregorianCalendar calendar = new GregorianCalendar();

                        // 获取当前日期是第几周
                        int weekOfYear = calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                        result = weekOfYear.ToString();
                        break;
                  
                }

            }
            return result;
        }
        /// <summary>
        /// 计算滚动翻页区
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageCount"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static int[] ScrollRangeCalc(int pageIndex, int pageCount, int count)
        {
            var startPage = Math.Max(pageIndex - (count / 2), 1);
            var endPage = Math.Min(pageCount, startPage + count - 1);

            return new[] { startPage, endPage };
        }
        /// <summary>
        /// 获取翻页代码
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalCount">总记录数</param>
        /// <param name="hrefFormat"></param>
        /// <returns></returns>
        public static string Paging(int pageIndex, int pageSize, int totalCount, string hrefFormat)
        {


            if (pageSize == 0) return string.Empty;
            if (totalCount < pageSize)
                if (totalCount == 0) return "";
                else
                    return string.Format("<div class='msdn'><span>共{0}条记录</span></div>", totalCount);

            var sb = new StringBuilder();
            sb.Capacity = 200;
            int start = hrefFormat.LastIndexOf("{0}");
            string pageHref = hrefFormat.Remove(start, 3).Insert(start, "{1}");
            if (pageIndex > 1)
            {
                sb.AppendFormat(pageHref, pageIndex - 1, "上一页");//上一页
            }
            else
            {
                sb.AppendFormat(pageHref, pageIndex, "上一页");
            }

            int pageCount = 0;//页数

            // PagingCalc(totalCount, pageSize, ref pageIndex, out pageCount);
            pageCount = totalCount / pageSize + (totalCount % pageSize > 0 ? 1 : 0);
            pageIndex = Math.Min(pageIndex, pageCount);
            int[] ps = ScrollRangeCalc(pageIndex, pageCount, 8);

            if (ps[0] > 1) sb.AppendFormat(hrefFormat + "...", 1);

            for (int i = ps[0]; i <= ps[1]; i++)
            {
                if (i != pageIndex)
                    sb.AppendFormat(hrefFormat, i);
                else
                    sb.Append("<span>" + i + "</span>");

                if (i < ps[1])
                    sb.Append("&nbsp;");
            }
            if (ps[1] < pageCount) sb.AppendFormat("..." + hrefFormat, pageCount);
            if (pageIndex < pageCount)
            {
                sb.AppendFormat(pageHref, pageIndex + 1, "下一页");//下一页
            }
            else
            {
                sb.AppendFormat(pageHref, pageIndex, "下一页");
            }
            if (totalCount > 0)
                sb.Insert(0, string.Format(pageHref, 1, "首页"));
            sb.AppendFormat(pageHref, pageCount, "末页");
            sb.Append("<span>共" + totalCount + "条记录</span>");


            sb.Insert(0, "<div class=\"msdn\">");
            sb.Append("</div>");

            //if (pageSize > 8)
            //    sb.Insert(0, "<script>function gotopage(){  var page=$.trim($('#txtPageNumber').val());page=page==''?1:page;  $('#btnGotoPageNumber').parent().attr('href',$('#btnGotoPageNumber').parent().attr('href').replace('{0}',page)); }</script>");
            return sb.ToString();
        }
        /// <summary>
        /// 用MD5加密字符串
        /// </summary>
        /// <param name="password">待加密的字符串</param>
        /// <returns></returns>
        public static string MD5Encrypt(string password)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] hashedDataBytes;
            hashedDataBytes = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(password));
            string key = "";
            foreach (byte i in hashedDataBytes)
            {
                key += i.ToString("x2");
            }
            return key;
        }
        /// <summary>
        /// 获取物理地址
        /// </summary>
        /// <returns></returns>
        public static string GetMAC()
        {
            try
            {
                var nic = System.Net.NetworkInformation.NetworkInterface
                    .GetAllNetworkInterfaces()
                    .FirstOrDefault(n =>
                        n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                        n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback);
                return nic?.GetPhysicalAddress().ToString() ?? "unknow";
            }
            catch
            {
                return "unknow";
            }
        }
        /// <summary>
        /// 人民币传大写
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static String ConvertToChinese(Decimal number)
        {
            var s = number.ToString("#L#E#D#C#K#E#D#C#J#E#D#C#I#E#D#C#H#E#D#C#G#E#D#C#F#E#D#C#.0B0A");
            var d = Regex.Replace(s, @"((?<=-|^)[^1-9]*)|((?'z'0)[0A-E]*((?=[1-9])|(?'-z'(?=[F-L\.]|$))))|((?'b'[F-L])(?'z'0)[0A-L]*((?=[1-9])|(?'-z'(?=[\.]|$))))", "${b}${z}");
            var r = Regex.Replace(d, ".", m => "负元空零壹贰叁肆伍陆柒捌玖空分角拾佰仟万亿兆京垓秭穰"[m.Value[0] - '-'].ToString());
            return r;
        }

        public static int ID {
            get; set;
           }
       
        public static  DateTime ToDay
        {
            get;set;
        }
        public  static string GenerateStringID()

        {
            ID = ID + 1;
            if(ToDay==null)
            {
                ToDay = DateTime.Now;
            }
            if(ToDay.Date!=DateTime.Now.Date)
            {
                ToDay = DateTime.Now;
                ID = 1;
            }
            return string.Format(@"P{0:yyyyMMddHHmmss}{1}", DateTime.Now, ID);

        }
        public static  string GetSubString(string inputString, int length)
        {

            string result = "";
            if (string.IsNullOrEmpty(inputString))
            {
                return "";
            }
            byte[] bytes = System.Text.Encoding.Default.GetBytes(inputString);
            int maxBytesLength = bytes.Length;
            int len = length * 2;//1个中文等于2个
            if (len >= maxBytesLength)
            {
                result = inputString;
            }
            else
            {
                int index = 0;
                int returnLength = 0;//实际返回的长度
                foreach (char c in inputString)
                {
                    int charAscii = ((int)c);
                    if (charAscii <= 255)
                    {
                        index++;

                    }
                    else
                    {
                        index += 2;
                    }
                    if (index > len)
                    {
                        break;
                    }
                    returnLength++;
                }
                int inputLength = inputString.Length;
                if (returnLength >= inputLength)
                {
                    returnLength = inputLength;
                }
                result = inputString.Substring(0, returnLength);
                if (returnLength < inputLength)
                {
                    result += "…";

                }
            }
            return result;
        }
        /// <summary>
        /// 替换掉单引号
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ReplaceSQL(string s)
        {
          if(!string.IsNullOrEmpty(s))
            {
                s = s.Replace("'", "");
            }
            return s;
        }

        public static int GetWeek(DateTime dateTime)
        {
            var dt = dateTime;
            //找到今年的第一天是周几
            int firstWeekend = Convert.ToInt32(DateTime.Parse(dt.Year + "-1-1").DayOfWeek);

            //获取第一周的差额,如果是周日，则firstWeekend为0，第一周也就是从周天开始的。
            int weekDay = firstWeekend == 0 ? 1 : (7 - firstWeekend + 1);

            //获取今天是一年当中的第几天
            int currentDay = dt.DayOfYear;

            //（今天 减去 第一周周末）/7 等于 距第一周有多少周 再加上第一周的1 就是今天是今年的第几周了
            //    刚好考虑了惟一的特殊情况就是，今天刚好在第一周内，那么距第一周就是0 再加上第一周的1 最后还是1
            int week=  Convert.ToInt32(Math.Ceiling((currentDay - weekDay) / 7.0)) + 1;
            return week;
        }
        public static string GetJsDate(object value)
        {
            try
            {
                if (value.ToString().IndexOf("/OADate(") == 0)
                {
                    value = value.ToString().Replace("/OADate(", "").Replace(")/", "");
                    value = System.DateTime.FromOADate(double.Parse(value.ToString()));
                }
                else
                {

                    if (!string.IsNullOrEmpty(value.ToString()))
                    {
                        value = value.ToString().Replace("　", "");
                    }
                    if (StringHelper.IsNumber(value.ToString()))
                    {
                        value = DateTime.Parse("1900-01-01").AddDays(double.Parse(value.ToString())).AddDays(-2);
                    }

                }
            }
            catch (Exception ex)
            { }
            return value.ToString();
        }
        public static string ReplaceSqlValue(string a)
        {
            if(!string.IsNullOrEmpty(a)&&a!= "0000-00-00")
            {
                a = a.Trim();
                a = "N'"+a.Replace("'", "''")+"'";
            }
            else
            {
                a = "NULL";
            }
            return a;
        }
    }
}

