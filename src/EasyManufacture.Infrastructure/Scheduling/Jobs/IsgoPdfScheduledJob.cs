using EasyManufacture.Domain.Options;
using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Text;

namespace EasyManufacture.Infrastructure.Scheduling.Jobs;

public sealed class IsgoPdfScheduledJob
{
    private readonly ScheduledTasksOptions _options;
    private readonly SystemLog _systemLog = new();
    private readonly StringBuilder _sqlPdf = new();
    private Dictionary<string, int> _dicPdf = new();
    private readonly List<string> _listPdf = new();
    private readonly List<string> _lstPdf = new();
    private readonly Dictionary<string, int> _lstPdfExt = new();
    private readonly Dictionary<string, int> _lstPdfExt有逗号 = new();

    public IsgoPdfScheduledJob(IOptions<ScheduledTasksOptions> options) => _options = options.Value;

    public void ScanDrawingPdf()
    {
        _sqlPdf.Clear();
        _dicPdf = new Dictionary<string, int>();
        string targetDirectory = _options.IsgoDrawingDirectory;

        if (Directory.Exists(targetDirectory))
        {
            TraverseDirectory(targetDirectory);
            }
            else
            {
                Console.WriteLine("目录不存在：" + targetDirectory);
            }
            if (_sqlPdf.Length > 0)
                try
                {
                    SqlHelper.ExecuteNonQuery(_sqlPdf.ToString());



                    //循环有文件的物料表数据，如果没有文件则清空
                    DataTable dataTable = SqlHelper.ExecuteDataTable(@"SELECT  MaterialID,FilePath FROM APS_Material
WHERE FilePath<>''");
                    if (dataTable.Rows.Count > 0)
                    {
                        _sqlPdf.Clear();
                        foreach (DataRow dataRow in dataTable.Rows)
                        {
                            string filePath = dataRow["FilePath"].ToString();
                            if (File.Exists(filePath) == false)
                            {
                                _sqlPdf.Append($@" update APS_Material set FilePath='',SyncDatetime=getdate(),FileName='',Extend10='' where MaterialID=" + dataRow["MaterialID"].ToString() + ";");
                            }
                        }
                        if (_sqlPdf.Length > 0)
                            SqlHelper.ExecuteNonQuery(_sqlPdf.ToString());
                    }
                }
                catch (Exception ex)
                {
                    _systemLog.SaveLog(SystemLog.SystemLogType.程序异常, ex.Message, null, null);
                }

        }

        #region 图纸文件分析的报表

        string ISGOGetPDF1(string targetDirectory)
        {


            if (Directory.Exists(targetDirectory))
            {
                TraverseDirectory1(targetDirectory);
            }
            else
            {
                Console.WriteLine("目录不存在：" + targetDirectory);
            }
            return JsonConvert.SerializeObject(new { lstPDF = _lstPdf });
        }

        void TraverseDirectory1(string path)
        {
            try
            {

                // 获取当前目录下所有文件
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (_lstPdfExt.ContainsKey(fileInfo.Extension) == false)
                    {
                        _lstPdfExt.Add(fileInfo.Extension, 1);
                    }
                    else
                    {
                        _lstPdfExt[fileInfo.Extension] += 1;
                    }
                    if (file.Contains(' '))
                    {
                        if (_lstPdfExt有逗号.ContainsKey(fileInfo.Extension) == false)
                        {
                            _lstPdfExt有逗号.Add(fileInfo.Extension, 1);
                        }
                        else
                        {
                            _lstPdfExt有逗号[fileInfo.Extension] += 1;
                        }
                    }

                    _lstPdf.Add(file);
                }

                // 获取所有子目录，并递归调用
                string[] directories = Directory.GetDirectories(path);
                foreach (string directory in directories)
                {
                    TraverseDirectory1(directory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生错误：" + ex.Message);
            }
        }
        public void ImportPdfReport()
        {
            StringBuilder sqlPDF1 = new StringBuilder();

            sqlPDF1.Append("truncate table APS_FilePDFImport");






            //图纸文件
            string targetDirectory = _options.IsgoDrawingDirectory;  // 修改为你的目标路径

            _lstPdf.Clear();
            string Files = ISGOGetPDF1(targetDirectory);

            JObject jObject = JsonConvert.DeserializeObject(Files) as JObject;
            JArray jArray = JsonConvert.DeserializeObject(jObject["_lstPdf"].ToString()) as JArray;
            string filePath = "";
            string createdon = "";
            string modifiedOn = "";
            try
            {

                for (int i = 0; i < jArray.Count; i++)
                {


                    filePath = jArray[i].ToString();
                    filePath = ConvertToLongPath(filePath);
                    string fileName = System.IO.Path.GetFileName(filePath);
                    string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filePath); // "AYT-6W-1-30-5 支架5"
                    string extension = System.IO.Path.GetExtension(filePath); // ".pdf"
                    try
                    {
                        createdon = File.GetCreationTime(filePath).ToString(); // 创建日期
                        modifiedOn = File.GetLastWriteTime(filePath).ToString(); // 修改日期
                    }
                    catch (Exception ex)
                    {
                        createdon = "";
                        modifiedOn = "";
                    }


                    sqlPDF1.Append($@" 

                      insert into [APS_FilePDFImport]
                      ([FilePath]
                          ,[FileName]
                          ,[FileNameWithoutExt]
                          ,[FileExt]
                          ,[DataSource]
    ,[CreatedOn],
					[ModifyedOn]
                        )
     
                      select '{filePath}',
                             '{fileName}',
                             '{fileNameWithoutExt}',
                             '{extension}',
                             '图纸文件'
,'{createdon}'
,'{modifiedOn}'



                    ");

                }


                //3D文档
                string targetDirectory1 = _options.IsgoCadDirectory;  // 修改为你的目标路径

                _lstPdf.Clear();
                string Files1 = ISGOGetPDF1(targetDirectory1);

                JObject jObject1 = JsonConvert.DeserializeObject(Files1) as JObject;
                JArray jArray1 = JsonConvert.DeserializeObject(jObject1["_lstPdf"].ToString()) as JArray;


                for (int i = 0; i < jArray1.Count; i++)
                {


                    filePath = jArray1[i].ToString();
                    filePath = ConvertToLongPath(filePath);
                    string fileName = System.IO.Path.GetFileName(filePath);
                    string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filePath); // "AYT-6W-1-30-5 支架5"

                    string extension = System.IO.Path.GetExtension(filePath); // ".pdf"

                    try
                    {
                        createdon = File.GetCreationTime(filePath).ToString(); // 创建日期
                        modifiedOn = File.GetLastWriteTime(filePath).ToString(); // 修改日期
                    }
                    catch (Exception ex)
                    {
                        createdon = "";
                        modifiedOn = "";
                    }

                    sqlPDF1.Append($@" 

                      insert into [APS_FilePDFImport]
                      ([FilePath]
                          ,[FileName]
                          ,[FileNameWithoutExt]
                          ,[FileExt]
                          ,[DataSource]
                          ,[CreatedOn],
					[ModifyedOn]
                        )
     
                      select '{filePath}',
                             '{fileName}',
                             '{fileNameWithoutExt}',
                             '{extension}',
                             'CAD文件'

,'{createdon}'
,'{modifiedOn}'


                    ");

                }




                if (sqlPDF1.Length > 0)
                {
                    sqlPDF1.Append($@" 
                        exec P_APS_FilePDF
                        ");
                    SqlHelper.ExecuteNonQuery(sqlPDF1.ToString());


                }
            }
            catch (Exception ex)
            {
                _systemLog.SaveLog(SystemLog.SystemLogType.程序异常, ex.Message + filePath, null, null);
            }


        }


        /// <summary>
        /// 转换为 Windows 长路径格式 (\\?\)
        /// 这是处理包含保留设备名和特殊字符路径的关键方法
        /// </summary>
        string ConvertToLongPath(string path)
        {

            return path;
        }
        #endregion

        void TraverseDirectory(string path)
        {
            try
            {
                // 获取当前目录下所有文件
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    // if (file.EndsWith(".pdf", StringComparison.InvariantCultureIgnoreCase))
                    //{
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.Extension.ToLower() == ".pdf")
                    {
                        // 获取文件的创建时间
                        DateTime creationTime = fileInfo.CreationTime;

                        // 获取文件的最后修改时间
                        DateTime lastWriteTime = fileInfo.LastWriteTime;
                        if (creationTime.Date == DateTime.Now.Date || lastWriteTime.Date == DateTime.Now.Date || DateTime.Now.Date == DateTime.Parse("2025-05-09") || true)
                        {
                            string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                            string fileNameExt = System.IO.Path.GetFileName(file);
                            string[] parts = fileName.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            //  string[] f = fileName.Split(' ');
                            string materialName = string.Join(" ", parts, 1, parts.Length - 1);  // 从第二个词开始拼接


                            string code = parts[0];
                            if (_dicPdf.ContainsKey(code) == false)
                            {
                                _dicPdf.Add(code, 1);
                            }
                            else
                            {
                                _dicPdf[code] = _dicPdf[code] + 1;
                            }

                            _listPdf.Add(fileName);
                            if (parts.Length > 1)
                            {
                                _sqlPdf.Append($@" 

INSERT INTO [dbo].[APS_Material]
           ( 
		   Extend2,
		   code,
		   [MaterialName]
		   ,FileName
		   ,Extend9
		   ,CreatedOn
		   ,CreatedBy
		   ,CreatedByName
        ,FilePath
,DataSource,Unit,MaterialType,SyncDatetime


      )
    
	select TOP 1 '{code}', '{code}','{materialName}','{fileNameExt}','{code}'
	,getdate(),'自动读取','自动读取','{file}','图纸解析','件','非标件',getdate()
	from Dev_Organize a
	where not exists(
	select  1
	from [APS_Material](NOLOCK)
	where Code='{code}'
	)

	UPDATE [APS_Material]
	SET FilePath='{file}',FileName='{fileNameExt}',MaterialName='{materialName}'
,extend10='{_dicPdf[code]}',SyncDatetime=getdate()
	WHERE CODE='{code}'


");
                            }
                            else
                            {
                                _sqlPdf.Append($@" 

INSERT INTO [dbo].[APS_Material]
           ( 
		   Extend2,
		   code,
		   [MaterialName]
		   ,FileName
		   ,Extend9
		   ,CreatedOn
		   ,CreatedBy
		   ,CreatedByName
        ,FilePath
,DataSource,Spec
      )
    
	select TOP 1 '图纸解析错误', '图纸解析错误','{materialName}','{fileNameExt}',''
	,getdate(),'自动读取','自动读取','{file}','图纸解析错误'
,{StringHelper.ReplaceSqlValue(file)}
	from Dev_Organize a
	where not exists(
	select  1
	from [APS_Material](NOLOCK)
	where MaterialName='{materialName}'
	)
 


");

                            }
                        }

                    }
                }

                // 获取所有子目录，并递归调用
                string[] directories = Directory.GetDirectories(path);
                foreach (string directory in directories)
                {
                    TraverseDirectory(directory);
                }
            }
            catch (Exception ex)
            {
            }
        }
}
