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
                string url = BarCode;
                string barCode = GetContentI(url);
                if(barCode.Contains("%C2%BA") || barCode.Contains("%EF%BF%BD"))
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
                string 藥碼 = barCode.Substring(20, 6);

                SQLControl sQLControl_醫囑資料 = new SQLControl(MySQL_server, MySQL_database, "order_list", MySQL_userid, MySQL_password, (uint)MySQL_port.StringToInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);
                List<object[]> list_醫囑資料 = new List<object[]>();
                string[] serch_colName = new string[] { enum_醫囑資料.領藥號.GetEnumName(), enum_醫囑資料.病歷號.GetEnumName(), enum_醫囑資料.開方日期.GetEnumName() };
                string[] serch_Value = new string[] { 領藥號, 病歷號, 看病日期 };
                list_醫囑資料 = sQLControl_醫囑資料.GetRowsByDefult(null, serch_colName, serch_Value);
                List<OrderClass> orderClasses = list_醫囑資料.SQLToClass<OrderClass , enum_醫囑資料>();

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
                string url = BarCode;
                string barCode = GetContentI(url);
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
                string 藥碼 = barCode.Substring(20, 6);

                SQLControl sQLControl_醫囑資料 = new SQLControl(MySQL_server, MySQL_database, "order_list", MySQL_userid, MySQL_password, (uint)MySQL_port.StringToInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);
                List<object[]> list_醫囑資料 = new List<object[]>();
                string[] serch_colName = new string[] { enum_醫囑資料.領藥號.GetEnumName(), enum_醫囑資料.病歷號.GetEnumName(), enum_醫囑資料.開方日期.GetEnumName() };
                string[] serch_Value = new string[] { 領藥號, 病歷號, 看病日期 };
                list_醫囑資料 = sQLControl_醫囑資料.GetRowsByDefult(null, serch_colName, serch_Value);
                list_醫囑資料 = list_醫囑資料.GetRows((int)enum_醫囑資料.藥品碼, 藥碼);
                List<OrderClass> orderClasses = list_醫囑資料.SQLToClass<OrderClass, enum_醫囑資料>();

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
