﻿using BACnetCommonLib;
using NPOI.HSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.IO.BACnet;
using System.Linq;
using System.Text;
using System.Windows;

namespace BACnetScanBIM
{
    public class MainVM : DependencyObject
    {
        #region Instance
        static MainVM instance = new MainVM();
        public static MainVM Instance { get { return instance; } }
        private MainVM() { }
        #endregion

        BacnetClient Bacnet_client;
        #region DevicesList
        public static readonly DependencyProperty DevicesListProperty = DependencyProperty.Register("DevicesList",
            typeof(ObservableCollection<BacDevice>), typeof(MainVM),
            new PropertyMetadata((sender, e) =>
            {
                var vm = sender as MainVM;
                if (vm == null) return;
            }));
        public ObservableCollection<BacDevice> DevicesList
        {
            get { return GetValue(DevicesListProperty) as ObservableCollection<BacDevice>; }
            set { SetValue(DevicesListProperty, value); }
        }
        #endregion
        #region SelectedDevice
        public static readonly DependencyProperty SelectedDeviceProperty = DependencyProperty.Register("SelectedDevice",
            typeof(BacDevice), typeof(MainVM),
            new PropertyMetadata((sender, e) =>
            {
                var vm = sender as MainVM;
                if (vm == null) return;
            }));
        public BacDevice SelectedDevice
        {
            get { return GetValue(SelectedDeviceProperty) as BacDevice; }
            set { SetValue(SelectedDeviceProperty, value); }
        }
        #endregion
        byte InvokeId = 0x00;
        public byte GetCurrentInvokeId()
        {
            InvokeId = (byte)((InvokeId + 1) % 256);
            return InvokeId;
        }

        public void Run()
        {
            this.InitSettings();
            Bacnet_client = new BacnetClient(new BacnetIpUdpProtocolTransport(int.Parse(ConfigurationManager.AppSettings["LocalBacPort"]), false));
            Bacnet_client.OnIam -= new BacnetClient.IamHandler(handler_OnIam);
            Bacnet_client.OnIam += new BacnetClient.IamHandler(handler_OnIam);
            Bacnet_client.Start();
            Bacnet_client.WhoIs();
        }
        void InitSettings()
        {
            this.ScanBatchStep = Convert.ToInt32(ConfigurationManager.AppSettings[nameof(this.ScanBatchStep)]);
        }
        int ScanBatchStep = 50;

        void handler_OnIam(BacnetClient sender, BacnetAddress adr, uint deviceId, uint maxAPDU,
                                    BacnetSegmentations segmentation, ushort vendorId)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (DevicesList == null) DevicesList = new ObservableCollection<BacDevice>();
                lock (DevicesList)
                {
                    if (DevicesList.Any(x => x.DeviceId == deviceId)) return;
                    int index = 0;
                    for (; index < this.DevicesList.Count; index++)
                    {
                        if (this.DevicesList[index].DeviceId > deviceId) break;
                    }

                    DevicesList.Insert(index, new BacDevice(adr, deviceId));
                    Logger.Log(@"Detect Device: " + deviceId);
                }
            }));
        }
        //批量扫点,注意不要太多,超过maxAPDU失败
        public void ScanPointsBatch(BacDevice device, uint count)
        {
            try
            {
                if (device == null) return;
                var pid = BacnetPropertyIds.PROP_OBJECT_LIST;
                var device_id = device.DeviceId;
                var bobj = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id);
                var adr = device.Address;
                if (adr == null) return;
                device.Properties = new ObservableCollection<BacProperty>();

                List<BacnetPropertyReference> rList = new List<BacnetPropertyReference>();

                for (uint i = 1; i < count; i++)
                {
                    rList.Add(new BacnetPropertyReference((uint)pid, i));
                    if (i % this.ScanBatchStep == 0 || i == count)//不要超了 MaxAPDU
                    {
                        IList<BacnetReadAccessResult> lstAccessRst;
                        var bRst = Bacnet_client.ReadPropertyMultipleRequest(adr, bobj, rList, out lstAccessRst, this.GetCurrentInvokeId());
                        if (bRst)
                        {
                            foreach (var aRst in lstAccessRst)
                            {
                                if (aRst.values == null) continue;
                                foreach (var bPValue in aRst.values)
                                {
                                    if (bPValue.value == null) continue;
                                    foreach (var bValue in bPValue.value)
                                    {
                                        var strBValue = "" + bValue.Value;
                                        Logger.Log(pid + " , " + strBValue + " , " + bValue.Tag);

                                        var strs = strBValue.Split(':');
                                        if (strs.Length < 2) continue;
                                        var strType = strs[0];
                                        var strObjId = strs[1];
                                        var subNode = new BacProperty();
                                        BacnetObjectTypes otype;
                                        Enum.TryParse(strType, out otype);
                                        if (otype == BacnetObjectTypes.OBJECT_NOTIFICATION_CLASS || otype == BacnetObjectTypes.OBJECT_DEVICE) continue;
                                        subNode.ObjectId = new BacnetObjectId(otype, Convert.ToUInt32(strObjId));
                                        device.Properties.Add(subNode);
                                    }
                                }
                            }
                        }
                        rList.Clear();
                    }
                }
            }
            catch (Exception exp)
            {
            }
        }
        //逐个扫点,速度较慢
        public void ScanPointSingle(BacDevice device, uint count)
        {
            if (device == null) return;
            var pid = BacnetPropertyIds.PROP_OBJECT_LIST;
            var device_id = device.DeviceId;
            var bobj = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id);
            var adr = device.Address;
            if (adr == null) return;
            device.Properties = new ObservableCollection<BacProperty>();

            for (uint index = 1; index <= count; index++)
            {
                try
                {
                    var list = ReadScalarValue(adr, bobj, pid, this.GetCurrentInvokeId(), index);
                    if (list == null) continue;
                    foreach (var bValue in list)
                    {
                        var strBValue = "" + bValue.Value;
                        Logger.Log(pid + " , " + strBValue + " , " + bValue.Tag);
                        var strs = strBValue.Split(':');
                        if (strs.Length < 2) continue;
                        var strType = strs[0];
                        var strObjId = strs[1];
                        var subNode = new BacProperty();
                        BacnetObjectTypes otype;
                        Enum.TryParse(strType, out otype);
                        subNode.ObjectId = new BacnetObjectId(otype, Convert.ToUInt32(strObjId));
                        device.Properties.Add(subNode);
                    }
                }
                catch (Exception exp)
                {
                    Logger.Log("Error: " + index + " , " + exp.Message);
                }
            }
        }
        public void ScanSubProperties(BacDevice device)
        {
            var adr = device.Address;
            if (adr == null) return;
            if (device.Properties == null) return;
            foreach (BacProperty subNode in device.Properties)
            {
                try
                {
                    List<BacnetPropertyReference> rList = new List<BacnetPropertyReference>();
                    rList.Add(new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_DESCRIPTION, uint.MaxValue));
                    rList.Add(new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_REQUIRED, uint.MaxValue));
                    IList<BacnetReadAccessResult> lstAccessRst;
                    var bRst = Bacnet_client.ReadPropertyMultipleRequest(adr, subNode.ObjectId, rList, out lstAccessRst, this.GetCurrentInvokeId());
                    if (bRst)
                    {
                        foreach (var aRst in lstAccessRst)
                        {
                            if (aRst.values == null) continue;
                            foreach (var bPValue in aRst.values)
                            {
                                if (bPValue.value == null || bPValue.value.Count == 0) continue;
                                var pid = (BacnetPropertyIds)(bPValue.property.propertyIdentifier);
                                var bValue = bPValue.value.First();
                                var strBValue = "" + bValue.Value;
                                Logger.Log(pid + " , " + strBValue + " , " + bValue.Tag);
                                switch (pid)
                                {
                                    case BacnetPropertyIds.PROP_DESCRIPTION://描述
                                        {
                                            subNode.PROP_DESCRIPTION = bValue + "";
                                        }
                                        break;
                                    case BacnetPropertyIds.PROP_OBJECT_NAME://点名
                                        {
                                            subNode.PROP_OBJECT_NAME = bValue + "";
                                        }
                                        break;
                                    case BacnetPropertyIds.PROP_PRESENT_VALUE://值
                                        {
                                            subNode.PROP_PRESENT_VALUE = bValue.Value;
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
                catch (Exception exp)
                {
                    Logger.Log("Error: " + exp.Message);
                }
            }
        }
        //获取子节点个数
        public uint GetDeviceArrayIndexCount(BacDevice device)
        {
            try
            {
                var adr = device.Address;
                if (adr == null) return 0;
                var list = ReadScalarValue(adr,
                    new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device.DeviceId),
                    BacnetPropertyIds.PROP_OBJECT_LIST, 0, 0);
                var rst = Convert.ToUInt32(list.FirstOrDefault().Value);
                return rst;
            }
            catch
            { }
            return 0;
        }
        IList<BacnetValue> ReadScalarValue(BacnetAddress adr, BacnetObjectId oid,
            BacnetPropertyIds pid, byte invokeId = 0, uint arrayIndex = uint.MaxValue)
        {
            try
            {
                IList<BacnetValue> NoScalarValue;
                var rst = Bacnet_client.ReadPropertyRequest(adr, oid, pid, out NoScalarValue, invokeId, arrayIndex);
                if (!rst) return null;
                return NoScalarValue;
            }
            catch { }
            return null;
        }
        public void Save2Excel()
        {
            if (this.DevicesList == null) return;
            foreach (var device in this.DevicesList)
            {
                device.Save2Excel();
            }
        }
        //处理乱码
        public void CombineExcel()
        {
            var dicND = this.LoadNameDesFromExcel();
            var files = Directory.GetFiles(Constants.ExcelDir);
            if (!Directory.Exists(Constants.AimExcelDir)) Directory.CreateDirectory(Constants.AimExcelDir);
            foreach (var file in files)
            {
                this.CombinExcel(file, dicND);
            }
        }
        void CombinExcel(string file, Dictionary<string, string> dicND)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var keyPre = @"Device" + fileName.Substring(0, fileName.Length - 4) + "-";
            var aimBook = new HSSFWorkbook();
            using (FileStream fp = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Logger.Log("Combine Excel : " + file);
                var book = new HSSFWorkbook(fp);
                for (int sheetNum = 0; sheetNum < book.NumberOfSheets; sheetNum++)
                {
                    var sheet = book.GetSheetAt(sheetNum);
                    if (sheet.FirstRowNum < 0 || sheet.FirstRowNum >= sheet.LastRowNum) continue;
                    var aimSheet = aimBook.CreateSheet(sheet.SheetName);
                    var aimRowNum = sheet.FirstRowNum;
                    for (var rowNum = sheet.FirstRowNum + 1; rowNum <= sheet.LastRowNum; rowNum++)
                    {
                        var rowContent = sheet.GetRow(rowNum);
                        if (rowContent == null) continue;
                        var dname = keyPre + rowContent.GetCell(rowContent.FirstCellNum + 1).ToString();
                        if (!dicND.Keys.Contains(dname)) continue;
                        #region CopyRow
                        var aimRow = aimSheet.CreateRow(aimRowNum);
                        for (var cellNum = rowContent.FirstCellNum; cellNum <= rowContent.LastCellNum; cellNum++)
                        {
                            var aimCell = aimRow.CreateCell(cellNum);
                            if (cellNum == rowContent.FirstCellNum + 5)
                            {
                                var ddes = dicND[dname];
                                aimCell.SetCellValue(ddes);
                            }
                            else
                            {
                                var cell = rowContent.GetCell(cellNum);
                                if (cell == null) continue;
                                switch (cell.CellType)
                                {
                                    case NPOI.SS.UserModel.CellType.Boolean:
                                        aimCell.SetCellValue(cell.BooleanCellValue);
                                        break;
                                    case NPOI.SS.UserModel.CellType.Numeric:
                                        aimCell.SetCellValue(cell.NumericCellValue);
                                        break;
                                    case NPOI.SS.UserModel.CellType.String:
                                        aimCell.SetCellValue(cell.StringCellValue);
                                        break;
                                    default:
                                        aimCell.SetCellValue(cell.ToString());
                                        break;
                                }
                            }
                        }
                        #endregion
                        aimRowNum++;
                    }
                }
                if (aimBook == null || aimBook.NumberOfSheets == 0) return;
                var firstSheet = aimBook.GetSheetAt(0);
                if (firstSheet == null) return;
                if (firstSheet.LastRowNum == 0) return;
                using (var ms = new MemoryStream())
                {
                    aimBook.Write(ms);
                    File.WriteAllBytes(Path.Combine(Constants.AimExcelDir, Path.GetFileName(file)), ms.ToArray());
                    aimBook = null;
                    book = null;
                }
            }
        }
        Dictionary<string, string> LoadNameDesFromExcel()
        {
            var CombineHeaderName = ConfigurationManager.AppSettings["CombineHeaderName"];
            var CombineHeaderDes = ConfigurationManager.AppSettings["CombineHeaderDes"];
            Dictionary<string, string> dicND = new Dictionary<string, string>();
            var files = Directory.GetFiles(Constants.CombineExcelDir);
            foreach (var file in files)
            {
                using (FileStream fp = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var book = new HSSFWorkbook(fp);
                    for (int sheetNum = 0; sheetNum < book.NumberOfSheets; sheetNum++)
                    {
                        var sheet = book.GetSheetAt(sheetNum);
                        if (sheet.FirstRowNum < 0 || sheet.FirstRowNum >= sheet.LastRowNum) continue;
                        int cellNumName = -1;
                        int cellNumDes = -1;
                        var rowHeader = sheet.GetRow(sheet.FirstRowNum);
                        for (var cellNum = rowHeader.FirstCellNum; cellNum <= rowHeader.LastCellNum; cellNum++)
                        {
                            var strCellHeader = rowHeader.GetCell(cellNum).ToString();
                            if (string.Equals(strCellHeader, CombineHeaderName)) cellNumName = cellNum;
                            if (string.Equals(strCellHeader, CombineHeaderDes)) cellNumDes = cellNum;
                        }
                        if (cellNumDes == -1 || cellNumDes == -1) continue;//没找到
                        for (var rowNum = sheet.FirstRowNum + 1; rowNum <= sheet.LastRowNum; rowNum++)
                        {
                            var rowContent = sheet.GetRow(rowNum);
                            if (rowContent == null) continue;
                            if (cellNumName < rowContent.FirstCellNum || cellNumName > rowContent.LastCellNum ||
                                cellNumDes < rowContent.FirstCellNum || cellNumDes > rowContent.LastCellNum)
                                continue;
                            var dname = rowContent.GetCell(cellNumName).ToString();
                            if (string.IsNullOrEmpty(dname)) continue;
                            var ddes = rowContent.GetCell(cellNumDes).ToString();
                            dicND[dname] = ddes;
                        }
                    }
                }
            }
            return dicND;
        }
    }
}
