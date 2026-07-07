using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyManufacture.Entitys.Ex
{
   public  class ScanSSNInput
    {
        /// <summary>
        /// 条码
        /// </summary>
        public string SSN
        {
            get;set;
        }
        /// <summary>
        /// 排产明细ID
        /// </summary>
        public long SchedulingDetail
        {
            get;set;
        }
        /// <summary>
        /// 是否启用验证
        /// </summary>
        public bool IsVerification
        {
            get;set;
        }
        /// <summary>
        /// 扫描的组织ID
        /// </summary>
        public string OrganizeID
        {
            get;set;
        }
        /// <summary>
        /// 工序
        /// </summary>
        public string ProcessID
        {
            get;set;
        }
        /// <summary>
        /// 创建人
        /// </summary>
        public string CreatedBy
        {
            get;set;
        }
    }
    /// <summary>
    /// 返回参数
    /// </summary>
    public class ScanSSNOutput
    {
        /// <summary>
        /// 结果
        /// </summary>
        public bool Result
        {
            get;set;
        }
        /// <summary>
        /// 信息
        /// </summary>
        public string Msg
        {
            get;set;
        }
    }
}
