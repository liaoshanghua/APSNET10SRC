namespace EasyManufacture.Entitys;

public class V_APS_SalesOrderDetail
{
    public string? SalesOrderDetailID { get; set; }
    public string? SalesOrderID { get; set; }
    public int? Status { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? ModifiedBy { get; set; }
    public string? ModifiedByName { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? ModifyedOn { get; set; }
    public string? Remark1 { get; set; }
    public string? Remark2 { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal? Qty { get; set; }
    public decimal? Price { get; set; }
    public long? MaterialID { get; set; }
    public decimal? ProducedQty { get; set; }
    public int? ProductionStatus { get; set; }
    public DateTime? CompletionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public decimal? StockOutQty { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerCode { get; set; }
    public decimal? MOQty { get; set; }
    public string? MOStatus { get; set; }
}
