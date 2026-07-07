using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyManufacture.Entitys
{
    public partial class V_APS_SchedulingMachinePre
    {
        [NotMapped]
        public List<APS_SchedulingDetailPre> Details
        {
            get;set;
        }
  
    }
    public partial class V_APS_SchedulingPre
    {
        [NotMapped]
        public List<APS_SchedulingDetailPre> Details
        {
            get; set;
        }

    }
    public partial class V_APS_SchedulingMachine
    {
        [NotMapped]
        public List<APS_SchedulingDetail> Details
        {
            get; set;
        }
        public DateTime? StartDatePre
        {
            get
            {
                return this.StartDate;
            }
        }
        public DateTime? EndDatePre
        {
            get
            {
                return this.EndDate;
            }
        }
    }
}
