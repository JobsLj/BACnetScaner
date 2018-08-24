﻿using BACnetCommonLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace BACnetTranslator
{
    public static class Settings
    {
        public static int BacSplitSize { get; private set; }
        public static int WaitTime { get; private set; }
        public static int HeartBeatInterval { get; private set; }
        public static int LocalBacPort { get; private set; }
        public static int LocalUdpPort { get; private set; }
        public static string RemoteUdpIP { get; private set; }
        public static int RemoteUdpPort { get; private set; }

        public static string BuildingSign { get; private set; }
        public static int Gateway { get; private set; }
        public static string SendType { get; private set; }
        public static string MeterSignPre { get; private set; }
        public static string FuncId { get; private set; }
        public static bool IgnoreFirstValue { get; private set; }

        public static void InitSettings()
        {
            BacSplitSize = int.Parse(ConfigurationManager.AppSettings["BacSplitSize"]);
            WaitTime = int.Parse(ConfigurationManager.AppSettings["WaitTime"]);
            HeartBeatInterval = int.Parse(ConfigurationManager.AppSettings["HeartBeatInterval"]);
            LocalBacPort = int.Parse(ConfigurationManager.AppSettings["LocalBacPort"]);
            LocalUdpPort = int.Parse(ConfigurationManager.AppSettings["LocalUdpPort"]);
            RemoteUdpIP = ConfigurationManager.AppSettings["RemoteUdpIP"];
            RemoteUdpPort = int.Parse(ConfigurationManager.AppSettings["RemoteUdpPort"]);


            BuildingSign = ConfigurationManager.AppSettings["BuildingSign"];
            Gateway = int.Parse(ConfigurationManager.AppSettings["Gateway"]);
            SendType = ConfigurationManager.AppSettings["SendType"];
            MeterSignPre = ConfigurationManager.AppSettings["MeterSignPre"];
            FuncId = ConfigurationManager.AppSettings["FuncId"];
            IgnoreFirstValue = string.Equals("true", ConfigurationManager.AppSettings["IgnoreFirstValue"], StringComparison.CurrentCultureIgnoreCase);
            Logger.Log("InitSettings Finished");
        }
    }
}
