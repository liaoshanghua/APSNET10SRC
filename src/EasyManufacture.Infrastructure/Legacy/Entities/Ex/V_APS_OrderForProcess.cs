using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyManufacture.Entitys
{
    public partial class V_APS_OrderForProcess:ICloneable
    {
        public int RowNumber
        {
            get;set;
        }
        public object Clone()
        {

           // return this as object;      //引用同一个对象            

            return this.MemberwiseClone(); //浅复制            

          //  return new DrawBase() as object;//深复制        

        }
        public List<V_APS_MachineCombinationMaterial> Machines
        {
            get; set;
        }
        public List<Dev_Organize> Organizes
        {
            get; set;
        }
        /// <summary>
        /// 待拆分的元件数量
        /// </summary>
        public int TotalBomsPre
        {
            get; set;
        }
        /// <summary>
        /// 机台查询信息
        /// </summary>
        public string MachineMsg
        {
            get;set;
        }
        public bool IsNew
        {
            get;set;
        }
        /// <summary>
        /// 选中的开始时间
        /// </summary>
        public string StartTime
        {
            get;set;
        }
        /// <summary>
        /// 选中的开始时间
        /// </summary>
        public string EndTime
        {
            get; set;
        }
        public decimal TotalHours
        {
            get;set;
        }
        public decimal BadQty
        {
            get;set;
        }
        public decimal OutputQty
        {
            get;set;
        }
        /// <summary>
        /// 是否需要联机
        /// </summary>
        public string IsMachines
        {
            get;set;
        }
        public int? OrganizeID1
        {
            get;set;
        }
        public int? OrganizeIDFixDay1
        {
            get; set;
        }
        public List<Dev_Organize> Organizes1
        {
            get; set;
        }
        public int? OrganizeID2
        {
            get; set;
        }
        public int? OrganizeIDFixDay2
        {
            get; set;
        }
        public List<Dev_Organize> Organizes2
        {
            get; set;
        }
        public int? OrganizeID3
        {
            get; set;
        }
        public int? OrganizeIDFixDay3
        {
            get; set;
        }
        public List<Dev_Organize> Organizes3
        {
            get; set;
        }

        public int? OrganizeID4
        {
            get; set;
        }
        public int? OrganizeIDFixDay4
        {
            get; set;
        }
        public List<Dev_Organize> Organizes4
        {
            get; set;
        }
        public int? OrganizeID5
        {
            get; set;
        }
        public int? OrganizeIDFixDay5
        {
            get; set;
        }
        public List<Dev_Organize> Organizes5
        {
            get; set;
        }
        public int? OrganizeID6
        {
            get; set;
        }
        public int? OrganizeIDFixDay6
        {
            get; set;
        }
        public List<Dev_Organize> Organizes6
        {
            get; set;
        }
        /// <summary>
        /// 工序集合
        /// </summary>
        public List<APS_Process> Processes
        {
            get;set;
        }
        public bool IsAutoScheduling
        {
            get;set;
        }
        public bool IsAutoNext
        {
            get; set;
        }
        public long? Scheduling
        {
            get;set;
        }
        public List<WorkingTimesQty> WorkingTimesQty
        {
            get;set;
        }
        
    }
    public class WorkingTimesQty
    {
        public string WorkingTimesID
        {
            get;set;
        }
        public decimal? WorkingQty
        {
            get;set;
        }
        public decimal? TotalHour
        {
            get;set;
        }
        /// <summary>
        /// 总的排产数量
        /// </summary>
        public decimal? AllSchedulingQty
        {
            get;set;
        }
    }
}
