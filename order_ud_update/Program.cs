using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Configuration;
using Basic;
using SQLUI;
using HIS_DB_Lib;
using dBASE.NET;
using System.Threading;
using System.Data.OleDb;
namespace order_ud_update
{
    class Program
    {
        public enum enum_住院處方
        {
            病歷號 = 1,
            病人姓名 = 2,
            住院序號 = 0,
            領藥號 = 4,
            總量 = 8,
            藥名 = 11,
            藥碼 = 16,
            開方日期 = 19,
            藥袋類型 = 7,
        }
        static void Main(string[] args)
        {
            bool isNewInstance;
            Mutex mutex = new Mutex(true, "order_ud_update", out isNewInstance);
            try
            {


                if (!isNewInstance)
                {
                    Console.WriteLine("程式已經在運行中...");
                    return;
                }


                while (true)
                {
                    Console.WriteLine($"---------------------------------------------------------------------");
                    List<object[]> list_src_order = new List<object[]>();
                    MyTimerBasic myTimerBasic = new MyTimerBasic(50000);
                    string dbfFilePath = @"Y:\DRUG_UD.DBF"; // 替換成你的 DBF 檔案路徑
                    DataTable dataTable = null;
                    // 設定連接字串
                    string connectionString = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={System.IO.Path.GetDirectoryName(dbfFilePath)};Extended Properties=dBASE IV;";
                    using (OleDbConnection connection = new OleDbConnection(connectionString))
                    {
                        try
                        {
                            // 開啟連接
                            connection.Open();

                            // 執行 SQL 查詢
                            string sqlQuery = "SELECT * FROM " + System.IO.Path.GetFileNameWithoutExtension(dbfFilePath);
                            using (OleDbCommand command = new OleDbCommand(sqlQuery, connection))
                            {
                                using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                                {
                                    dataTable = new DataTable();
                                    adapter.Fill(dataTable);

                                    // 輸出資料到控制台
                                    foreach (DataRow row in dataTable.Rows)
                                    {
                                        object[] value = new object[dataTable.Columns.Count];
                                        for (int i = 0; i < dataTable.Columns.Count; i++)
                                        {
                                            value[i] = row[i];
                                        }
                                        list_src_order.Add(value);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("讀取資料時發生錯誤：" + ex.Message);
                        }
                    }
                    try
                    {

                        //myTimerBasic.StartTickTime();
                        //string src_path = @"phr2000\opd_drug\";
                        //string stc_filename = @"DRUG.DBF";
                        //string dst_path = @"C://";
                        //string dst_filename = @"DRUG.DBF";
                        ////Basic.FileIO.ServerFileCopy(src_path, stc_filename, dst_path, dst_filename, "user9", "win9");
                        //Basic.FileIO.CopyFile(@"Z:\DRUG.DBF", $"{dst_path}{dst_filename}");
                        //Dbf dbf = new Dbf();
                        //System.Text.EncodingInfo[] encodingInfos = System.Text.Encoding.GetEncodings();
                        //dbf.Encoding = System.Text.Encoding.GetEncoding("BIG5");
                        //dbf.Read($@"{dst_path}{dst_filename}");


                        //foreach (DbfRecord record in dbf.Records)
                        //{
                        //    object[] value = new object[dbf.Fields.Count];
                        //    for (int i = 0; i < dbf.Fields.Count; i++)
                        //    {
                        //        value[i] = record[i];
                        //    }
                        //    list_src_order.Add(value);
                        //}
                        list_src_order = (from temp in list_src_order
                                          where temp[(int)enum_住院處方.病歷號].ObjectToString().StringIsEmpty() == false
                                          select temp).ToList();
                        for (int i = 0; i < list_src_order.Count; i++)
                        {
                            string year = "";
                            string month = "";
                            string day = "";
                            string date = "";
                            string date_temp = list_src_order[i][(int)enum_住院處方.開方日期].ObjectToString();
                            if (date_temp.Length == 8)
                            {
                                year = date_temp.Substring(0, 4);
                                month = date_temp.Substring(4, 2);
                                day = date_temp.Substring(6, 2);
                                date = $"{year}/{month}/{day}";
                                if (date.Check_Date_String() == false) date = "";
                            }
                            if (date.StringIsEmpty() == false)
                            {
                                list_src_order[i][(int)enum_住院處方.開方日期] = date;
                            }

                            list_src_order[i][(int)enum_住院處方.總量] = list_src_order[i][(int)enum_住院處方.總量].ObjectToString().StringToInt32();

                        }
                        Console.WriteLine($"下載處方資料,共<{list_src_order.Count}>筆...,{myTimerBasic}");
                        myTimerBasic.TickStop();
                        myTimerBasic.StartTickTime();
                        SQLControl sQLControl_醫囑資料 = new SQLControl("127.0.0.1", "DBVM", "order_list", "user", "66437068", 3306, MySql.Data.MySqlClient.MySqlSslMode.None);

                        DateTime dateTime_st = DateTime.Now.AddDays(-1);
                        dateTime_st = new DateTime(dateTime_st.Year, dateTime_st.Month, dateTime_st.Day, 00, 00, 00);
                        DateTime dateTime_end = DateTime.Now.AddDays(2);
                        dateTime_end = new DateTime(dateTime_end.Year, dateTime_end.Month, dateTime_end.Day, 23, 59, 59);

                        List<object[]> list_order = sQLControl_醫囑資料.GetRowsByBetween(null, (int)enum_醫囑資料.開方日期, dateTime_st.ToDateTimeString(), dateTime_end.ToDateTimeString());
                        List<object[]> list_order_buf = new List<object[]>();
                        List<object[]> list_order_add = new List<object[]>();
                        Console.WriteLine($"從資料庫讀取處方資料,共<{list_order.Count}>筆...,{myTimerBasic}");
                        myTimerBasic.TickStop();
                        myTimerBasic.StartTickTime();
                        string 藥碼 = "";
                        string 藥名 = "";
                        string 病歷號 = "";
                        string 病人姓名 = "";
                        string 總量 = "";
                        string 住院序號 = "";
                        string 開方日期 = "";
                        string DAYS = "";
                        string 藥袋類型 = "";
                        string 處方類別 = "";
                        string 領藥號_temp = "";
                        Dictionary<object, List<object[]>> dictionary = list_order.ConvertToDictionary((int)enum_醫囑資料.PRI_KEY);
                        for (int i = 0; i < list_src_order.Count; i++)
                        {
                            藥碼 = list_src_order[i][(int)enum_住院處方.藥碼].ObjectToString();
                            藥名 = list_src_order[i][(int)enum_住院處方.藥名].ObjectToString();
                            病歷號 = list_src_order[i][(int)enum_住院處方.病歷號].ObjectToString();
                            病人姓名 = list_src_order[i][(int)enum_住院處方.病人姓名].ObjectToString();
                            總量 = list_src_order[i][(int)enum_住院處方.總量].ObjectToString();
                            住院序號 = list_src_order[i][(int)enum_住院處方.住院序號].ObjectToString();
                            藥袋類型 = list_src_order[i][(int)enum_住院處方.藥袋類型].ObjectToString();
                            開方日期 = list_src_order[i][(int)enum_住院處方.開方日期].ObjectToString();
                            開方日期 = 開方日期.StringToDateTime().ToDateString();
                            領藥號_temp = list_src_order[i][(int)enum_住院處方.領藥號].ObjectToString();
                            領藥號_temp = 領藥號_temp.Replace(" ", "");
                            if (領藥號_temp.Length < 4) continue;
                            領藥號_temp = 領藥號_temp.Substring(0, 4);
                            string PRI_KEY = $"{藥碼},{"住院"},{領藥號_temp},{總量},{DateTime.Now.ToDateString()}";
                            //list_order_buf = dictionary.GetRows(PRI_KEY);
                            list_order_buf = list_order.GetRows((int)enum_醫囑資料.PRI_KEY, PRI_KEY);

                            if (list_order_buf.Count == 0)
                            {
                                object[] value = new object[new enum_醫囑資料().GetLength()];
                                value[(int)enum_醫囑資料.GUID] = Guid.NewGuid().ToString();
                                value[(int)enum_醫囑資料.PRI_KEY] = PRI_KEY;
                                value[(int)enum_醫囑資料.藥局代碼] = "住院";
                                value[(int)enum_醫囑資料.藥品碼] = 藥碼;
                                value[(int)enum_醫囑資料.藥品名稱] = 藥名;
                                value[(int)enum_醫囑資料.病歷號] = 病歷號;
                                value[(int)enum_醫囑資料.交易量] = 總量.StringToInt32() * (-1);
                                value[(int)enum_醫囑資料.領藥號] = 領藥號_temp;
                                value[(int)enum_醫囑資料.藥袋類型] = 藥袋類型;
                                value[(int)enum_醫囑資料.病人姓名] = 病人姓名;
                                value[(int)enum_醫囑資料.開方日期] = 開方日期;
                                value[(int)enum_醫囑資料.產出時間] = DateTime.Now.ToDateTimeString_6();
                                value[(int)enum_醫囑資料.結方日期] = DateTime.MinValue.ToDateTimeString();
                                value[(int)enum_醫囑資料.展藥時間] = DateTime.MinValue.ToDateTimeString();
                                value[(int)enum_醫囑資料.過帳時間] = DateTime.MinValue.ToDateTimeString();
                                value[(int)enum_醫囑資料.狀態] = enum_醫囑資料_狀態.未過帳.GetEnumName();
                                list_order_add.Add(value);
                            }

                        }
                        sQLControl_醫囑資料.AddRows(null, list_order_add);
                        Console.WriteLine($"共新增<{list_order_add.Count}>筆處方,{myTimerBasic} {DateTime.Now.ToDateTimeString()}");
                        Console.WriteLine($"---------------------------------------------------------------------");

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"醫令串接異常,msg:{e.Message}");
                    }
                    System.Threading.Thread.Sleep(10000);
                }


            }
            catch
            {

            }
            finally
            {

                mutex.ReleaseMutex(); // 釋放互斥鎖

                Environment.Exit(0);
            }






        }
    }
}
