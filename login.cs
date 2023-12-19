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
namespace DB2VM_API
{
    [Route("dbvm/[controller]")]
    [ApiController]
    public class login : ControllerBase
    {
        static private string API_Server = "http://127.0.0.1:4433/api/serversetting";
        public class MsgClass
        {
            [JsonPropertyName("ReturnCode")]
            public string ReturnCode { get; set; }
            [JsonPropertyName("ReturnMsg")]
            public string ReturnMsg { get; set; }
        }
        public class listClass
        {
            [JsonPropertyName("emp_id")]
            public string emp_id { get; set; }
            [JsonPropertyName("emp_name")]
            public string emp_name { get; set; }

        }

        public class LoginData
        {
            [JsonPropertyName("Msg")]
            public List<MsgClass> Msg { get; set; }
            [JsonPropertyName("list")]
            public List<listClass> list { get; set; }
        }

        static string check_id = "M$E#D@3!";
        [HttpPost]
        public string POST_login([FromBody] returnData returnData)
        {

            List<ServerSettingClass> serverSettingClasses = ServerSettingClassMethod.WebApiGet($"{API_Server}");
            serverSettingClasses = serverSettingClasses.MyFind(enum_ServerSetting_Type.網頁, enum_ServerSetting_網頁.人員資料);
            if (serverSettingClasses.Count == 0)
            {
                returnData.Code = -200;
                returnData.Result = "找無資料庫參數!";
                return returnData.JsonSerializationt();
            }
            string IP = serverSettingClasses[0].Server;
            string DataBaseName = serverSettingClasses[0].DBName;
            string UserName = serverSettingClasses[0].User;
            string Password = serverSettingClasses[0].Password;
            uint Port = (uint)serverSettingClasses[0].Port.StringToInt32();

            SQLControl sQLControl_person_page = new SQLControl(IP, DataBaseName, "person_page", UserName, Password, Port, MySql.Data.MySqlClient.MySqlSslMode.None);


            sessionClass data = returnData.Data.ObjToClass<sessionClass>();
            List<object[]> list_人員資料 = sQLControl_person_page.GetRowsByDefult(null ,(int)enum_人員資料.ID, data.ID);
            int index = sQLControl_person_page.GetAllRows(null).Count;
            if (list_人員資料.Count == 0)
            {
                try
                {
                    System.Text.StringBuilder soap = new System.Text.StringBuilder();
                    soap.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    soap.Append("<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
                    soap.Append("<soap:Body>");
                    soap.Append("<EmpData xmlns=\"http://tempuri.org/\">");
                    soap.Append($"<_CheckId>{check_id}</_CheckId>");
                    soap.Append($"<_EmpId>{data.ID }</_EmpId>");
                    soap.Append("</EmpData>");
                    soap.Append("</soap:Body>");
                    soap.Append("</soap:Envelope>");
                    string Xml = Basic.Net.WebServicePost("https://phamedtestws.chgh.org.tw/PHAMEDWebService.asmx?op=EmpData", soap);
                    string[] Node_array = new string[] { "soap:Body", "EmpDataResponse" };
                    XmlElement xmlElement = Xml.Xml_GetElement(Node_array);
                    string json = xmlElement.Xml_GetInnerXml("EmpDataResult");
                    LoginData loginData = json.JsonDeserializet<LoginData>();
                    string id = "";
                    string name = "";
                    if (loginData != null)
                    {
                        if (loginData.Msg[0].ReturnMsg == "成功")
                        {
                            id = loginData.list[0].emp_id.Trim();
                            name = loginData.list[0].emp_name.Trim();
                            data.Name = name;
                            data.Password = id;
                            data.BARCODE = id;
                            data.UID = id;

                            object[] value = new object[new enum_人員資料().GetLength()];
                            value[(int)enum_人員資料.GUID] = Guid.NewGuid();
                            value[(int)enum_人員資料.ID] = id;
                            value[(int)enum_人員資料.密碼] = id;
                            value[(int)enum_人員資料.一維條碼] = id;
                            value[(int)enum_人員資料.卡號] = id;
                            value[(int)enum_人員資料.姓名] = name;
                            value[(int)enum_人員資料.權限等級] = "1";
                            value[(int)enum_人員資料.顏色] = GetColor(index);
                            sQLControl_person_page.AddRow(null, value);
                        }
                    }
                }
                catch
                {

                }
               
              

            }
            string url = "http://127.0.0.1:4433/api/session/login";
            returnData.Data = data;
            string json_result = Basic.Net.WEBApiPostJson(url, returnData.JsonSerializationt(true));
            return json_result;

        }
        private System.Drawing.Color GetColor(int index)
        {
            index = index % 7;
            if (index == 0)
            {
                return System.Drawing.Color.Red;
            }
            else if (index == 1)
            {
                return System.Drawing.Color.Orange;
            }
            else if (index == 2)
            {
                return System.Drawing.Color.Yellow;
            }
            else if (index == 3)
            {
                return System.Drawing.Color.Green;
            }
            else if (index == 4)
            {
                return System.Drawing.Color.Blue;
            }
            else if (index == 5)
            {
                return System.Drawing.Color.SkyBlue;
            }
            else if (index == 6)
            {
                return System.Drawing.Color.White;
            }
            else if (index == 7)
            {
                return System.Drawing.Color.HotPink;
            }
            return System.Drawing.Color.HotPink;
        }
    }
}
