using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;
using System.Data;
using System.Configuration;
using Basic;
using SQLUI;
using Oracle.ManagedDataAccess.Client;
using System.Text;
using HIS_DB_Lib;
using System.Xml;
using System.Text.Json.Serialization;
namespace DB2VM.Controller
{
   

    [Route("dbvm/[controller]")]
    [ApiController]
    public class BBCMController : ControllerBase
    {
        public class MsgClass
        {
            [JsonPropertyName("ReturnCode")]
            public string ReturnCode { get; set; }
            [JsonPropertyName("ReturnMsg")]
            public string ReturnMsg { get; set; }
        }
        public class listClass
        {
            [JsonPropertyName("drug_id")]
            public string drug_id { get; set; }
            [JsonPropertyName("drug_generic_name")]
            public string drug_generic_name { get; set; }
            [JsonPropertyName("drug_name")]
            public string drug_name { get; set; }
            [JsonPropertyName("chinese_control_drug_name")]
            public string chinese_control_drug_name { get; set; }
            [JsonPropertyName("drug_stock_format")]
            public string drug_stock_format { get; set; }
     
        }

        public class MedData
        {
            [JsonPropertyName("Msg")]
            public List<MsgClass> Msg { get; set; }
            [JsonPropertyName("list")]
            public List<listClass> list { get; set; }
        }

        static string check_id = "M$E#D@3!";
        static string MySQL_server = $"{ConfigurationManager.AppSettings["MySQL_server"]}";
        static string MySQL_database = $"{ConfigurationManager.AppSettings["MySQL_database"]}";
        static string MySQL_userid = $"{ConfigurationManager.AppSettings["MySQL_user"]}";
        static string MySQL_password = $"{ConfigurationManager.AppSettings["MySQL_password"]}";
        static string MySQL_port = $"{ConfigurationManager.AppSettings["MySQL_port"]}";
        [HttpGet]
        public string Get(string? Code)
        {
            System.Text.StringBuilder soap = new System.Text.StringBuilder();
            soap.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            soap.Append("<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
            soap.Append("<soap:Body>");
            soap.Append("<MedData xmlns=\"http://tempuri.org/\">");
            soap.Append($"<_CheckId>{check_id}</_CheckId>");
            soap.Append($"<_MedId>{Code}</_MedId>");
            soap.Append("</MedData>");
            soap.Append("</soap:Body>");
            soap.Append("</soap:Envelope>");
            string Xml = Basic.Net.WebServicePost("https://phamedtestws.chgh.org.tw/PHAMEDWebService.asmx?op=MedData", soap);
            string[] Node_array = new string[] { "soap:Body", "MedDataResponse"};
            XmlElement xmlElement = Xml.Xml_GetElement(Node_array);
            string json = xmlElement.Xml_GetInnerXml("MedDataResult");

            MedData medData = json.JsonDeserializet<MedData>();

            SQLControl sQLControl_UDSDBBCM = new SQLControl(MySQL_server, MySQL_database, "medicine_page_cloud", MySQL_userid, MySQL_password, (uint)MySQL_port.StringToInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);
            medClass medClass = null;
            List<object[]> list_藥檔資料 = new List<object[]>();
            List<object[]> list_藥檔資料_buf = new List<object[]>();
            list_藥檔資料 = sQLControl_UDSDBBCM.GetAllRows(null);
            if (medData.list.Count != 0)
            {
                list_藥檔資料_buf = list_藥檔資料.GetRows((int)enum_雲端藥檔.藥品碼, medData.list[0].drug_id);
                if (list_藥檔資料_buf.Count == 0)
                {
                    medClass = new medClass();
                    medClass.GUID = Guid.NewGuid().ToString();
                    medClass.藥品碼 = medData.list[0].drug_id;
                    medClass.藥品名稱 = medData.list[0].drug_name;
                    medClass.藥品學名 = medData.list[0].drug_generic_name;
                    medClass.中文名稱 = medData.list[0].chinese_control_drug_name;
                    medClass.包裝單位 = medData.list[0].drug_stock_format;
                    object[] value = medClass.ClassToSQL<medClass, enum_雲端藥檔>();
                    sQLControl_UDSDBBCM.AddRow(null, value);
                }
                else
                {
                    medClass = list_藥檔資料_buf[0].SQLToClass<medClass, enum_雲端藥檔>();
                    medClass.藥品碼 = medData.list[0].drug_id;
                    medClass.藥品名稱 = medData.list[0].drug_name;
                    medClass.藥品學名 = medData.list[0].drug_generic_name;
                    medClass.中文名稱 = medData.list[0].chinese_control_drug_name;
                    medClass.包裝單位 = medData.list[0].drug_stock_format;
                    object[] value = medClass.ClassToSQL<medClass, enum_雲端藥檔>();
                    List<object[]> list_value = new List<object[]>();
                    list_value.Add(value);
                    sQLControl_UDSDBBCM.UpdateByDefulteExtra(null, list_value);
                }
            }
            List<medClass> medClasses = new List<medClass>();
            if(medClass == null)
            {
                medClasses = list_藥檔資料.SQLToClass<medClass, enum_雲端藥檔>();
            }
            else
            {
                medClasses.Add(medClass);
            }
            returnData returnData = new returnData();
            returnData.Code = 200;
            returnData.Result = "取得藥品檔成功!";
            returnData.Data = medClasses;

            return $"{returnData.JsonSerializationt(true)}";
        }
    }
}
