
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
namespace order_update
{
    class Program
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
        static void Main(string[] args)
        {
            while(true)
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
                    for (int i = 0; i < list_src_order.Count; i++)
                    {
                        list_src_order[i][(int)enum_門診處方.開方日期] = DateTime.Now.ToDateString();
                        list_src_order[i][(int)enum_門診處方.總量] = list_src_order[i][(int)enum_門診處方.總量].ObjectToString().StringToInt32();

                    }

                    SQLControl sQLControl_醫囑資料 = new SQLControl("127.0.0.1", "DBVM", "order_list", "user", "66437068", 3306, MySql.Data.MySqlClient.MySqlSslMode.None);
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
                        if (list_order_buf.Count == 0)
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
                    Console.WriteLine($"共新增<{list_order_add.Count}>筆處方,{myTimerBasic}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"醫令串接異常,msg:{e.Message}");
                }
                System.Threading.Thread.Sleep(10000);
            }
           
        }
    }
}
