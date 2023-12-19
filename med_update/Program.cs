
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Configuration;
using Basic;
using SQLUI;
using HIS_DB_Lib;

namespace med_update
{
    class Program
    {
        static void Main(string[] args)
        {
            string json = Basic.Net.WEBApiGet($"http://192.168.23.54:443/dbvm/BBCM");
            Console.WriteLine($"{json}");
            System.Threading.Thread.Sleep(5000);
        }
    }
}
