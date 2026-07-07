using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyManufacture.Entitys
{
    public partial class V_APS_SchedulingAll
    {

        public List<Detail> Details
        {
            get;set;
        }
        public DateTime? WorkingDay
        {
            get;set;
        }
        public long OrganizeID
        {
            get;set;
        }
        public class Detail
        {

            public List<string> Account
            {
                get; set;
            }
            public string Name
            {
                get; set;
            }
            public decimal scrapNumber
            {
                get; set;
            }
            public decimal submitNumber
            {
                get; set;
            }
            public decimal? BadQty
            {
                get;set;
            }
            public long OrderOutputPersonsID
            {
                get;set;
            }
            public string StatusName
            {
                get;set;
            }
            public int Status
            {
                get;set;
            }
            public string OutputDate
            {
                get;set;
            }
            public string PWSProcessID
            {
                get;set;
            }
            public string Remark1
            {
                get;set;
            }
        }
    }
 
    public partial class V_APS_OrderScheduling2
    {
        public List<Dev_Organize> WorkShops
        {
            get;set;
        }
    }
}
