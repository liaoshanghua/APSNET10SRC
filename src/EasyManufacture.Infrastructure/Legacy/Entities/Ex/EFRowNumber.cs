using System.ComponentModel.DataAnnotations.Schema;

namespace EasyManufacture.Entitys.Ex;

/// <summary>EF6 内存行号/选中态；非数据库列。</summary>
public class EFRowNumber
{
    private static int rowNumber;
    private int _rowNumber;
    private bool isAuto = true;

    [NotMapped]
    public int RowNumber
    {
        get
        {
            if (isAuto)
            {
                rowNumber++;
                return rowNumber;
            }
            return _rowNumber;
        }
        set => _rowNumber = value;
    }

    [NotMapped]
    public bool? isChecked { get; set; }

    public void Reset() => isAuto = false;
}
