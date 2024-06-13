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
using System.Data.Odbc;
using HIS_DB_Lib;
using VfpClient;
using dBASE.NET;

namespace DB2VM
{
    [Route("dbvm/[controller]")]
    [ApiController]
    public class BBARController : ControllerBase
    {

        public enum enum_門診處方
        {
            病歷號 = 0,
            病人姓名 = 1,
            領藥號 = 2,
            總量 = 5,
            藥名 = 7,
            藥碼 = 9,
            開方日期 = 10,
        }

        static string MySQL_server = $"{ConfigurationManager.AppSettings["MySQL_server"]}";
        static string MySQL_database = $"{ConfigurationManager.AppSettings["MySQL_database"]}";
        static string MySQL_userid = $"{ConfigurationManager.AppSettings["MySQL_user"]}";
        static string MySQL_password = $"{ConfigurationManager.AppSettings["MySQL_password"]}";
        static string MySQL_port = $"{ConfigurationManager.AppSettings["MySQL_port"]}";
        static string GetContentI(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                string query = uri.Query;

                // 解析查詢參數
                string[] queryParams = query.TrimStart('?').Split('&');
                foreach (var param in queryParams)
                {
                    string[] keyValue = param.Split('=');
                    if (keyValue.Length == 2 && keyValue[0] == "l")
                    {
                        return keyValue[1];
                    }
                }
            }
            catch (UriFormatException ex)
            {
                Console.WriteLine($"Invalid URL format: {ex.Message}");
            }
            return null;
        }

        [HttpGet]
        public string Get(string? BarCode)
        {
         
            returnData returnData = new returnData();
            try
            {
                SQLControl sQLControl_醫囑資料 = new SQLControl(MySQL_server, MySQL_database, "order_list", MySQL_userid, MySQL_password, (uint)MySQL_port.StringToInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);

                List<object[]> list_醫囑資料 = new List<object[]>();
                List<object[]> list_醫囑資料_buf = new List<object[]>();
                List<object[]> list_醫囑資料_temp = new List<object[]>();
                List<object[]> list_醫囑資料_add = new List<object[]>();
                List<object[]> list_醫囑資料_replace = new List<object[]>();
                List<OrderClass> orderClasses = new List<OrderClass>();
                string 藥碼 = "";
                string 藥名 = "";
                string url = BarCode;
                string barCode = GetContentI(url);
                if (barCode == null)
                {
                    string[] barcode_ary_temp = BarCode.Split("I=");
                    if (barcode_ary_temp.Length == 2)
                    {
                        barCode = barcode_ary_temp[1];
                    }
                }
                string[] barcode_ary = barCode.Split(",");
                if (barcode_ary.Length == 3)
                {
                    
                    SQLControl sQLControl_UDSDBBCM = new SQLControl(MySQL_server, MySQL_database, "medicine_page_cloud", MySQL_userid, MySQL_password, (uint)MySQL_port.StringToInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);
                    List<object[]> list_藥檔資料 = sQLControl_UDSDBBCM.GetAllRows(null);
                    List<object[]> list_藥檔資料_buf = new List<object[]>();
                    string ty = barcode_ary[2].Substring(0, 1);
                    string PRI_KEY = $"{barCode},{ DateTime.Now.ToDateString()}";
                    string 住院序號 = barcode_ary[0].Substring(0, 7);
                    string 藥局代碼 = "";
                    string[] str_temp = barcode_ary[2].Split(";");
                    string 領藥號_temp = str_temp[0].Substring(str_temp[0].Length - 4, 4);
                    藥碼 = barcode_ary[0].Substring(7, 6);
                    string 頻次 = barcode_ary[1];
                    int 消耗量 = barcode_ary[0].Substring(13, 3).StringToInt32() * -1;
                    list_藥檔資料_buf = list_藥檔資料.GetRows((int)enum_藥品資料_藥檔資料.藥品碼, 藥碼);
                    if (list_藥檔資料_buf.Count > 0)
                    {
                        藥名 = list_藥檔資料_buf[0][(int)enum_藥品資料_藥檔資料.藥品名稱].ObjectToString();
                    }

                    if (ty == "u")
                    {
                        藥局代碼 = "住院";
                        PRI_KEY = $"{藥碼},{藥局代碼},{領藥號_temp},{消耗量 * -1 },{DateTime.Now.ToDateString()}";
                        list_醫囑資料_buf = sQLControl_醫囑資料.GetRowsByDefult(null, (int)enum_醫囑資料.PRI_KEY, PRI_KEY);
              
                        orderClasses = list_醫囑資料_buf.SQLToClass<OrderClass, enum_醫囑資料>();

                        returnData.Method = "barcode api";
                        returnData.Code = 200;
                        returnData.Data = orderClasses;
                        returnData.Result = $"藥單刷入成功!";
                        return returnData.JsonSerializationt(true);
                    }
                    else if (ty == "p")
                    {
                        藥局代碼 = "領退";
                    }
                    else if (ty == "o")
                    {
                        藥局代碼 = "出院";
                    }
                    else
                    {
                        藥局代碼 = "化療";
                    }
                    string 開方日期 = DateTime.Now.ToDateTimeString();
                    list_醫囑資料_buf = sQLControl_醫囑資料.GetRowsByDefult(null, (int)enum_醫囑資料.PRI_KEY, PRI_KEY);
                    if (list_醫囑資料_buf.Count == 0)
                    {
                        object[] value = new object[new enum_醫囑資料().GetLength()];
                        value[(int)enum_醫囑資料.GUID] = Guid.NewGuid().ToString();
                        value[(int)enum_醫囑資料.PRI_KEY] = PRI_KEY;
                        value[(int)enum_醫囑資料.藥局代碼] = 藥局代碼;
                        value[(int)enum_醫囑資料.藥品碼] = 藥碼;
                        value[(int)enum_醫囑資料.藥品名稱] = 藥名;
                        value[(int)enum_醫囑資料.頻次] = 頻次;
                        value[(int)enum_醫囑資料.病歷號] = 住院序號;
                        value[(int)enum_醫囑資料.交易量] = 消耗量;
                        value[(int)enum_醫囑資料.領藥號] = 領藥號_temp;
                        value[(int)enum_醫囑資料.病人姓名] = "";
                        value[(int)enum_醫囑資料.開方日期] = 開方日期;
                        value[(int)enum_醫囑資料.產出時間] = DateTime.Now.ToDateTimeString_6();
                        value[(int)enum_醫囑資料.結方日期] = DateTime.MinValue.ToDateTimeString();
                        value[(int)enum_醫囑資料.展藥時間] = DateTime.MinValue.ToDateTimeString();
                        value[(int)enum_醫囑資料.過帳時間] = DateTime.MinValue.ToDateTimeString();
                        value[(int)enum_醫囑資料.狀態] = enum_醫囑資料_狀態.未過帳.GetEnumName();
                        list_醫囑資料_temp.Add(value);
                        sQLControl_醫囑資料.AddRows(null, list_醫囑資料_temp);
                    }
                    else
                    {
                        object[] value = list_醫囑資料_buf[0];
                        value[(int)enum_醫囑資料.PRI_KEY] = PRI_KEY;
                        value[(int)enum_醫囑資料.藥局代碼] = 藥局代碼;
                        value[(int)enum_醫囑資料.藥品碼] = 藥碼;
                        value[(int)enum_醫囑資料.藥品名稱] = 藥名;
                        value[(int)enum_醫囑資料.頻次] = 頻次;
                        value[(int)enum_醫囑資料.病歷號] = 住院序號;
                        value[(int)enum_醫囑資料.交易量] = 消耗量;
                        value[(int)enum_醫囑資料.領藥號] = 領藥號_temp;
                        value[(int)enum_醫囑資料.病人姓名] = "";
                        value[(int)enum_醫囑資料.開方日期] = 開方日期;
                        value[(int)enum_醫囑資料.產出時間] = DateTime.Now.ToDateTimeString_6();
                        value[(int)enum_醫囑資料.結方日期] = DateTime.MinValue.ToDateTimeString();
                        value[(int)enum_醫囑資料.展藥時間] = DateTime.MinValue.ToDateTimeString();
                        list_醫囑資料_temp.Add(value);
                        sQLControl_醫囑資料.UpdateByDefulteExtra(null, list_醫囑資料_temp);

                    }

                    orderClasses = list_醫囑資料_temp.SQLToClass<OrderClass, enum_醫囑資料>();

                    returnData.Method = "barcode api";
                    returnData.Code = 200;
                    returnData.Data = orderClasses;
                    returnData.Result = $"藥單刷入成功!";
                    return returnData.JsonSerializationt(true);

                }
                if (barCode.Contains("%C2%BA") || barCode.Contains("%EF%BF%BD"))
                {
                    barCode = barCode.Replace("%C2%BA", "");
                    barCode = barCode.Replace("%EF%BF%BD", "");
                }
                if (barCode.Length < 26)
                {
                    returnData.Code = -200;
                    returnData.Result = $"傳入資訊錯誤:{url}";
                }
                string 看病日期 = barCode.Substring(0, 8);
                string year = 看病日期.Substring(0, 4);
                string month = 看病日期.Substring(4, 2);
                string day = 看病日期.Substring(6, 2);
                看病日期 = $"{year}/{month}/{day}";
                string 領藥號 = barCode.Substring(8, 5);
                string 病歷號 = barCode.Substring(13, 7);
                藥碼 = barCode.Substring(20, 6);

                string[] serch_colName = new string[] { enum_醫囑資料.領藥號.GetEnumName(), enum_醫囑資料.病歷號.GetEnumName(), enum_醫囑資料.開方日期.GetEnumName() };
                string[] serch_Value = new string[] { 領藥號, 病歷號, 看病日期 };
                list_醫囑資料 = sQLControl_醫囑資料.GetRowsByDefult(null, serch_colName, serch_Value);
                if(list_醫囑資料.Count == 0)
                {
                    領藥號 = $"7{領藥號}";
                    serch_colName = new string[] { enum_醫囑資料.領藥號.GetEnumName(), enum_醫囑資料.病歷號.GetEnumName(), enum_醫囑資料.開方日期.GetEnumName() };
                    serch_Value = new string[] { 領藥號, 病歷號, 看病日期 };
                    list_醫囑資料 = sQLControl_醫囑資料.GetRowsByDefult(null, serch_colName, serch_Value);
                }
                orderClasses = list_醫囑資料.SQLToClass<OrderClass , enum_醫囑資料>();

                returnData.Method = "barcode api";
                returnData.Code = 200;
                returnData.Data = orderClasses;
                returnData.Result = $"藥單刷入成功!";
                return returnData.JsonSerializationt(true);
            }
            catch(Exception e)
            {
                returnData.Code = -200;
                returnData.Result = $"{e.Message}";
                return returnData.JsonSerializationt(true);
            }
       
          
        }

        [Route("order_by_code")]
        [HttpGet]
        public string Get_order_by_code(string? BarCode)
        {

            returnData returnData = new returnData();
            try
            {
                SQLControl sQLControl_醫囑資料 = new SQLControl(MySQL_server, MySQL_database, "order_list", MySQL_userid, MySQL_password, (uint)MySQL_port.StringToInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);

                List<object[]> list_醫囑資料 = new List<object[]>();
                List<object[]> list_醫囑資料_buf = new List<object[]>();
                List<object[]> list_醫囑資料_temp = new List<object[]>();
                List<object[]> list_醫囑資料_add = new List<object[]>();
                List<object[]> list_醫囑資料_replace = new List<object[]>();
                List<OrderClass> orderClasses = new List<OrderClass>();
                string 藥碼 = "";
                string 藥名 = "";
                string url = BarCode;
                string barCode = GetContentI(url);
                if(barCode == null)
                {
                    string[] barcode_ary_temp = BarCode.Split("I=");
                    if (barcode_ary_temp.Length == 2)
                    {
                        barCode = barcode_ary_temp[1];
                    }
                }
                string[] barcode_ary = barCode.Split(",");
                if (barcode_ary.Length == 3)
                {
                    SQLControl sQLControl_UDSDBBCM = new SQLControl(MySQL_server, MySQL_database, "medicine_page_cloud", MySQL_userid, MySQL_password, (uint)MySQL_port.StringToInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);
                    List<object[]> list_藥檔資料 = sQLControl_UDSDBBCM.GetAllRows(null);
                    List<object[]> list_藥檔資料_buf = new List<object[]>();
                    string ty = barcode_ary[2].Substring(0, 1);
                    string PRI_KEY = $"{barCode},{ DateTime.Now.ToDateString()}";
                    string 住院序號 = barcode_ary[0].Substring(0, 7);
                    string 藥局代碼 = "";
                    string[] str_temp = barcode_ary[2].Split(";");
                    string 領藥號_temp = str_temp[0].Substring(str_temp[0].Length - 4, 4);
                    藥碼 = barcode_ary[0].Substring(7, 6);
                    string 頻次 = barcode_ary[1];
                    int 消耗量 = barcode_ary[0].Substring(13, 3).StringToInt32() * -1;
                    list_藥檔資料_buf = list_藥檔資料.GetRows((int)enum_藥品資料_藥檔資料.藥品碼, 藥碼);
                    if (list_藥檔資料_buf.Count > 0)
                    {
                        藥名 = list_藥檔資料_buf[0][(int)enum_藥品資料_藥檔資料.藥品名稱].ObjectToString();
                    }


                    if (ty == "u")
                    {
                        藥局代碼 = "住院";
                        PRI_KEY = $"{藥碼},{藥局代碼},{領藥號_temp},{消耗量 * -1 },{DateTime.Now.ToDateString()}";
                        list_醫囑資料_buf = sQLControl_醫囑資料.GetRowsByDefult(null, (int)enum_醫囑資料.PRI_KEY, PRI_KEY);

                        orderClasses = list_醫囑資料_buf.SQLToClass<OrderClass, enum_醫囑資料>();

                        returnData.Method = "barcode api";
                        returnData.Code = 200;
                        returnData.Data = orderClasses;
                        returnData.Result = $"藥單刷入成功!";
                        return returnData.JsonSerializationt(true);
                    }
                    else if (ty == "p")
                    {
                        藥局代碼 = "領退";
                    }
                    else if (ty == "o")
                    {
                        藥局代碼 = "出院";
                    }
                    else
                    {
                        藥局代碼 = "化療";
                    }
                    string 開方日期 = DateTime.Now.ToDateTimeString();
                    list_醫囑資料_buf = sQLControl_醫囑資料.GetRowsByDefult(null, (int)enum_醫囑資料.PRI_KEY, PRI_KEY);
                    if (list_醫囑資料_buf.Count == 0)
                    {
                        object[] value = new object[new enum_醫囑資料().GetLength()];
                        value[(int)enum_醫囑資料.GUID] = Guid.NewGuid().ToString();
                        value[(int)enum_醫囑資料.PRI_KEY] = PRI_KEY;
                        value[(int)enum_醫囑資料.藥局代碼] = 藥局代碼;
                        value[(int)enum_醫囑資料.藥品碼] = 藥碼;
                        value[(int)enum_醫囑資料.藥品名稱] = 藥名;
                        value[(int)enum_醫囑資料.頻次] = 頻次;
                        value[(int)enum_醫囑資料.病歷號] = 住院序號;
                        value[(int)enum_醫囑資料.交易量] = 消耗量;
                        value[(int)enum_醫囑資料.領藥號] = 領藥號_temp;
                        value[(int)enum_醫囑資料.病人姓名] = "";
                        value[(int)enum_醫囑資料.開方日期] = 開方日期;
                        value[(int)enum_醫囑資料.產出時間] = DateTime.Now.ToDateTimeString_6();
                        value[(int)enum_醫囑資料.結方日期] = DateTime.MinValue.ToDateTimeString();
                        value[(int)enum_醫囑資料.展藥時間] = DateTime.MinValue.ToDateTimeString();
                        value[(int)enum_醫囑資料.過帳時間] = DateTime.MinValue.ToDateTimeString();
                        value[(int)enum_醫囑資料.狀態] = enum_醫囑資料_狀態.未過帳.GetEnumName();
                        list_醫囑資料_temp.Add(value);
                        sQLControl_醫囑資料.AddRows(null, list_醫囑資料_temp);
                    }
                    else
                    {
                        object[] value = list_醫囑資料_buf[0];
                        value[(int)enum_醫囑資料.PRI_KEY] = PRI_KEY;
                        value[(int)enum_醫囑資料.藥局代碼] = 藥局代碼;
                        value[(int)enum_醫囑資料.藥品碼] = 藥碼;
                        value[(int)enum_醫囑資料.藥品名稱] = 藥名;
                        value[(int)enum_醫囑資料.頻次] = 頻次;
                        value[(int)enum_醫囑資料.病歷號] = 住院序號;
                        value[(int)enum_醫囑資料.交易量] = 消耗量;
                        value[(int)enum_醫囑資料.領藥號] = 領藥號_temp;
                        value[(int)enum_醫囑資料.病人姓名] = "";
                        value[(int)enum_醫囑資料.開方日期] = 開方日期;
                        value[(int)enum_醫囑資料.產出時間] = DateTime.Now.ToDateTimeString_6();
                        value[(int)enum_醫囑資料.結方日期] = DateTime.MinValue.ToDateTimeString();
                        value[(int)enum_醫囑資料.展藥時間] = DateTime.MinValue.ToDateTimeString();
                        list_醫囑資料_temp.Add(value);
                        sQLControl_醫囑資料.UpdateByDefulteExtra(null, list_醫囑資料_temp);

                    }

                    orderClasses = list_醫囑資料_temp.SQLToClass<OrderClass, enum_醫囑資料>();

                    returnData.Method = "barcode api";
                    returnData.Code = 200;
                    returnData.Data = orderClasses;
                    returnData.Result = $"藥單刷入成功!";
                    return returnData.JsonSerializationt(true);

                }
                if (barCode.Contains("%C2%BA") || barCode.Contains("%EF%BF%BD"))
                {
                    barCode = barCode.Replace("%C2%BA", "");
                    barCode = barCode.Replace("%EF%BF%BD", "");
                }
                if (barCode.Length < 26)
                {
                    returnData.Code = -200;
                    returnData.Result = $"傳入資訊錯誤:{url}";
                }


                string 看病日期 = barCode.Substring(0, 8);
                string year = 看病日期.Substring(0, 4);
                string month = 看病日期.Substring(4, 2);
                string day = 看病日期.Substring(6, 2);
                看病日期 = $"{year}/{month}/{day}";
                string 領藥號 = barCode.Substring(8, 5);
                string 病歷號 = barCode.Substring(13, 7);
                藥碼 = barCode.Substring(20, 6);

                
                string[] serch_colName = new string[] { enum_醫囑資料.領藥號.GetEnumName(), enum_醫囑資料.病歷號.GetEnumName(), enum_醫囑資料.開方日期.GetEnumName() };
                string[] serch_Value = new string[] { 領藥號, 病歷號, 看病日期 };
                list_醫囑資料 = sQLControl_醫囑資料.GetRowsByDefult(null, serch_colName, serch_Value);
                list_醫囑資料 = list_醫囑資料.GetRows((int)enum_醫囑資料.藥品碼, 藥碼);
                if (list_醫囑資料.Count == 0)
                {
                    領藥號 = $"7{領藥號}";
                    serch_colName = new string[] { enum_醫囑資料.領藥號.GetEnumName(), enum_醫囑資料.病歷號.GetEnumName(), enum_醫囑資料.開方日期.GetEnumName() };
                    serch_Value = new string[] { 領藥號, 病歷號, 看病日期 };
                    list_醫囑資料 = sQLControl_醫囑資料.GetRowsByDefult(null, serch_colName, serch_Value);
                    list_醫囑資料 = list_醫囑資料.GetRows((int)enum_醫囑資料.藥品碼, 藥碼);
                }
                orderClasses = list_醫囑資料.SQLToClass<OrderClass, enum_醫囑資料>();

                returnData.Method = "barcode api";
                returnData.Code = 200;
                returnData.Data = orderClasses;
                returnData.Result = $"藥單刷入成功!";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = $"{e.Message}";
                return returnData.JsonSerializationt(true);
            }


        }

        [Route("order_update")]
        [HttpGet]
        public string GET_order_update()
        {        
            try
            {
                MyTimerBasic myTimerBasic = new MyTimerBasic(50000);
                myTimerBasic.StartTickTime();
                string src_path = @"phr2000\opd_drug\";
                string stc_filename = @"DRUG_OPD.DBF";
                string dst_path = @"C://";
                string dst_filename = @"DRUG_OPD.DBF";
                Basic.FileIO.ServerFileCopy(src_path, stc_filename, dst_path, dst_filename, "user9", "win9");
                Dbf dbf = new Dbf();
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                System.Text.EncodingInfo[] encodingInfos = System.Text.Encoding.GetEncodings();
                dbf.Encoding = System.Text.Encoding.GetEncoding("BIG5");
                dbf.Read(@$"{dst_path}{dst_filename}");
                List<object[]> list_src_order = new List<object[]>();
           
                foreach (DbfRecord record in dbf.Records)
                {
                    object[] value = new object[dbf.Fields.Count];
                    for (int i = 0; i < dbf.Fields.Count; i++)
                    {
                        value[i] = record[i];
                    }
                    list_src_order.Add(value);
                }
                list_src_order = (from temp in list_src_order
                                  where temp[(int)enum_門診處方.病歷號].ObjectToString().StringIsEmpty() == false
                                  select temp).ToList();
                for(int i = 0; i < list_src_order.Count; i++)
                {
                    list_src_order[i][(int)enum_門診處方.開方日期] = DateTime.Now.ToDateString();
                    list_src_order[i][(int)enum_門診處方.總量] = list_src_order[i][(int)enum_門診處方.總量].ObjectToString().StringToInt32();

                }

                SQLControl sQLControl_醫囑資料 = new SQLControl(MySQL_server, MySQL_database, "order_list", MySQL_userid, MySQL_password, (uint)MySQL_port.StringToInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);
                List<object[]> list_order = sQLControl_醫囑資料.GetRowsByDefult(null, (int)enum_醫囑資料.開方日期, DateTime.Now.ToDateString());
                List<object[]> list_order_buf = new List<object[]>();
                List<object[]> list_order_add = new List<object[]>();
                string 藥碼 = "";
                string 藥名 = "";
                string 病歷號 = "";
                string 病人姓名 = "";
                string 總量 = "";
                string 領藥號 = "";
                string 開方日期 = "";

                for (int i = 0; i < list_src_order.Count; i++)
                {
                    藥碼 = list_src_order[i][(int)enum_門診處方.藥碼].ObjectToString();
                    藥名 = list_src_order[i][(int)enum_門診處方.藥名].ObjectToString();
                    病歷號 = list_src_order[i][(int)enum_門診處方.病歷號].ObjectToString();
                    病人姓名 = list_src_order[i][(int)enum_門診處方.病人姓名].ObjectToString();
                    總量 = list_src_order[i][(int)enum_門診處方.總量].ObjectToString();
                    領藥號 = list_src_order[i][(int)enum_門診處方.領藥號].ObjectToString();
                    開方日期 = list_src_order[i][(int)enum_門診處方.開方日期].ObjectToString();
                    string PRI_KEY = $"{藥碼},{病歷號},{總量},{領藥號},{開方日期}";
                    list_order_buf = list_order.GetRows((int)enum_醫囑資料.PRI_KEY, PRI_KEY);
                    if(list_order_buf.Count == 0)
                    {
                        object[] value = new object[new enum_醫囑資料().GetLength()];
                        value[(int)enum_醫囑資料.GUID] = Guid.NewGuid().ToString();
                        value[(int)enum_醫囑資料.PRI_KEY] = PRI_KEY;
                        value[(int)enum_醫囑資料.藥品碼] = 藥碼;
                        value[(int)enum_醫囑資料.藥品名稱] = 藥名;
                        value[(int)enum_醫囑資料.病歷號] = 病歷號;
                        value[(int)enum_醫囑資料.交易量] = 總量;
                        value[(int)enum_醫囑資料.領藥號] = 領藥號;
                        value[(int)enum_醫囑資料.病人姓名] = 病人姓名;
                        value[(int)enum_醫囑資料.開方日期] = 開方日期;
                        list_order_add.Add(value);
                    }
                }
                sQLControl_醫囑資料.AddRows(null, list_order_add);
                return $"共新增<{list_order_add.Count}>筆處方,{myTimerBasic}";
            }
            catch(Exception e)
            {
                return $"醫令串接異常,msg:{e.Message}";
            }
        }
        public static List<List<OrderClass>> GroupOrders(List<OrderClass> orders)
        {
            List<List<OrderClass>> groupedOrders = orders
                .GroupBy(o => new { o.藥品碼, o.病歷號, o.開方日期 })
                .Select(group => group.ToList())
                .ToList();

            return groupedOrders;
        }
    }

}
