using EasyManufacture.Entitys.Ex;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/// <summary>
/// 相关的扩展类
/// </summary>
namespace EasyManufacture.Entitys
{
    /// <summary>
    /// 扩展类的文件
    /// </summary>
    public partial class V_APS_OrderProcessForSchduling : EFRowNumber
    {
  
        public string WorkShopName { get; set; }
 

        public string label
        {
            get
            {
                return this.LineName;
            }
        }
        public string value
        {
            get
            {
                return this.OrganizeID.ToString();
            }
        }

    }
    public partial class V_APS_OrderBOMNew
    {
        public List<V_APS_OrderBOMNew> children
        {
            get;set;
        }
        public bool  hasChildren
        {
            get
            {

                return children != null && children.Count > 0;
            }
        }
    }
    public partial class APS_ProcessPartPlan: EFRowNumber
    {
        public List<V_APS_OrganizeProcessID> childrens
        {
            get;set;
        }
        public List<string> ProcessIDs
        {
            get;set;
        }
        public string WorkShopName
        {
            get;set;
        }
       
         public bool? UpdateERP
        {
            get;set;
        }
    }
    public partial class V_OrderPlan2:EFRowNumber
    {
        public List<V_OrderPlan2> children
        {
            get; set;
        }
        /// <summary>
        /// 客户端是否选中
        /// </summary>
       public bool IsCheck
        {
            get;set;
        }

    }
    public partial class APS_ProcessPartPlan 
    {
       
        /// <summary>
        /// 客户端是否选中
        /// </summary>
        public bool IsCheck
        {
            get; set;
        }

    }

    
    public partial class V_APS_ProcessPartPlan : EFRowNumber
    {
        public List<V_APS_OrganizeProcessID> childrens
        {
            get; set;
        }
        public List<string> ProcessIDs
        {
            get; set;
        }
        public string WorkShopName
        {
            get; set;
        }

        /// <summary>
        /// 客户端是否选中
        /// </summary>
        public bool IsCheck
        {
            get; set;
        }
        public decimal? FormQty
        {
            get;set;
        }
        public bool IsBackground
        {
            get;set;
        }
        public int FixDay
        {
            get;set;
        }
        public string IssueMaterialBatchQty
        {
            get;set;
        }
        public string IssueType
        {
            get;set;
        }
        public bool? UpdateERP
        {
            get;set;
        }
        public string Extend8
        {
            get;set;
        }
        public string Extend1
        {
            get; set;
        }
        public string Extend2
        {
            get; set;
        }
        /// <summary>
        /// 工艺
        /// </summary>
        public string ProcessGroupName
        {
            get;set;
        }
    }
    public partial class V_APS_OrganizeProcessID : EFRowNumber
    {
        [NotMapped] public long MaterialID { get; set; }
        [NotMapped] public long OrderID { get; set; }
        [NotMapped] public string label => OrganizeName;
        [NotMapped] public string value => OrganizeID.ToString();
        [NotMapped] public decimal TotalSchedulingQty { get; set; }
    }
    public partial class V_APS_OrganizeProcess : EFRowNumber
    {
        [NotMapped] public long MaterialID { get; set; }
        [NotMapped] public long OrderID { get; set; }
        [NotMapped] public string label => OrganizeName;
        [NotMapped] public string value => OrganizeID.ToString();
        [NotMapped] public decimal TotalSchedulingQty { get; set; }
    }
    public partial class V_APS_ProcessPlanForSchedinng
    {
        public List<V_APS_OrderProcessForSchduling> childrens
        {
            get; set;
        }
    }
    public partial class V_APS_DayPlan
    {


        decimal workload = 0;
        /// <summary>
        /// 负荷
        /// </summary>
        public decimal Workload
        {
            get
            {
             
                
                if (this.ExpectTime.HasValue && this.Operation != "联动计划" && this.Operation != "忽略工时"&& TotalHours>0)
                {
                    workload = ExpectTime.GetValueOrDefault() / TotalHours;
                }
                else if (this.Capacity > 0 && TotalHours > 0 && this.Operation != "联动计划" && this.Operation != "忽略工时")
                {
                    workload = PlanQty.GetValueOrDefault() / (Capacity.GetValueOrDefault(1) * TotalHours);
                }
                return workload;
            }
        }
        /// <summary>
        /// 强制赋值负荷
        /// </summary>
        /// <param name="value"></param>
        public void SetWorkload(decimal value)
        {
            this.workload = value;
        }
    }

    public class LineWorkInfo
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime WorkDate
        {
            get; set;
        }
        /// <summary>
        /// 上班情况
        /// </summary>
        public string WorkStatus
        {
            get; set;
        }
        /// <summary>
        /// 组织ID
        /// </summary>
        public long OrganizeID
        {
            get; set;
        }
        /// <summary>
        /// 总上班工时
        /// </summary>
        public decimal TotalHours
        {
            get; set;
        }
        public long LineID
        {
            get
            {
                return OrganizeID;
            }
        }
      
    }
   
}
