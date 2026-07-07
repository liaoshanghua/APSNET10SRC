namespace EasyManufacture.Entitys;

public class V_APS_Order
{
    public long OrderID { get; set; }
    public int? MFGOrganizeID { get; set; }
    public string? OrderNo { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal? Qty { get; set; }
    public decimal? Price { get; set; }
    public string? SaleTo { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? SalesMan { get; set; }
    public long? MaterialID { get; set; }
    public int? OwnOrganizeID { get; set; }
    public int? SystemID { get; set; }
    public long? CustomerID { get; set; }
    public decimal? ProducedQty { get; set; }
    public int? ProductionStatus { get; set; }
    public DateTime? CompletionDate { get; set; }
    public decimal? StockOutQty { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? ModifiedBy { get; set; }
    public string? ModifiedByName { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? ModifyedOn { get; set; }
    public string? WorkFlowInstanceID { get; set; }
}
