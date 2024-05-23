﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Management;
using System.ComponentModel;
using OpenCVVideoRedactor.PopUpWindows;
using System.Text.RegularExpressions;
using System.Windows;

namespace OpenCVVideoRedactor.Helpers
{
    class KeyActivationHelper
    {
        private static bool _isActivated = false;
        private static string[] _keys = new string[] {
                "FO92FI18FO92FI18",
                "8IH2G41332M3PU74",
                "129L356GFV92FOR3",
                "E95UI845BD12JK0E",
                "W2467UI0O3ER42T1",
                "CV34694E20IOSYS2",
                "12ER983N1GG3RLK3",
                "4511OO22OOFUIY87",
            };
        public static bool IsActivated { get { return _isActivated; } }
        private static string GetMACAddress()
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            string MACAddress = String.Empty;
            foreach (ManagementObject mo in moc)
            {
                if (MACAddress == String.Empty)
                { // only return MAC Address from first card
                    if ((bool)mo["IPEnabled"] == true) MACAddress = mo["MacAddress"].ToString();
                }
                mo.Dispose();
            }
            return MACAddress;
        }
        private static string GetProcessorID()
        {
            var search = new ManagementObjectSearcher("SELECT * FROM Win32_baseboard");
            var mobos = search.Get();
            string id = "";
            foreach (var m in mobos)
            {
                var serial = m["SerialNumber"] as string;
                if (serial != null) id = serial;
            }
            return id;
        }
        public static void ShowDialog()
        {
            Regex regex = new Regex(@"[^A-Z0-9]");
            var code = InputBox.ShowDialog("Активация премиум версии", "Введите код активации", "",
                (str) => ActivateProduct(str), "Не верный код активации", 
                (str) => regex.Replace(str.ToUpper(), ""));
            if (code.Length > 0) MessageBox.Show("Премиум версия активирована");
        }
        public static bool ActivateProduct(string code)
        {
            if (!_keys.Contains(code)) return false;
            SHA256 sha256 = SHA256.Create();
            if (code.Length != 16) return false;
            string macAddres = GetMACAddress();
            string processorId = GetProcessorID();
            string hash = BitConverter.ToString(
                sha256.ComputeHash(
                        Encoding.UTF8.GetBytes(code+macAddres+processorId)
                    )
                ).Replace("-","");
            File.WriteAllText("activation.info", code + hash);
            _isActivated = true;
            return true;
        }
        public static bool CheckActivation()
        {
            string activation = File.ReadAllText("activation.info");
            if (activation.Length != 80) return false; //16 key + 64 hash
            SHA256 sha256 = SHA256.Create();
            string macAddres = GetMACAddress();
            string processorId = GetProcessorID();
            string code = activation.Substring(0, 16);
            if(!_keys.Contains(code)) return false;
            string hash = BitConverter.ToString(
                sha256.ComputeHash(
                        Encoding.UTF8.GetBytes(code + macAddres + processorId)
                    )
                ).Replace("-", "");
            _isActivated = activation == code + hash;
            return _isActivated;
        }
    }
}
