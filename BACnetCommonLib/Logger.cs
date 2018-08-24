﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BACnetCommonLib
{

    public static class Logger
    {
        public static string Dir = "Logs";
        static object locker = new object();
        public static void Log(string fileName, string msg, bool hasTime = true)
        {
            try
            {
                lock (locker)
                {
                    Console.WriteLine(msg);
                    if (!Directory.Exists(Dir))
                        Directory.CreateDirectory(Dir);
                    File.AppendAllLines(Path.Combine(Dir, fileName),
                        new string[] { (hasTime ? DateTime.Now.ToString("HH:mm:ss  ") : "") + msg });
                }
            }
            catch
            {
            }
        }

        public static void Log(string msg, bool hasTime = true)
        {
            Log(DateTime.Today.ToString("yyyyMMdd") + @".txt", msg, hasTime);
        }

        public static void Warning(string msg)
        {
            //Log(DateTime.Today.ToString("yyyyMMdd"), msg);
            Log(DateTime.Today.ToString("yyyyMMdd") + ".Warnings", msg);
        }

        public static void Error(string msg)
        {
            //Log(DateTime.Today.ToString("yyyyMMdd"), msg);
            Log(DateTime.Today.ToString("yyyyMMdd") + ".Errors", msg);
        }

        public static void Error(Exception exp)
        {
            if (exp == null) return;
            Error(exp.Message + "\n" + exp.StackTrace);
        }

        public static void Error(string preStr, Exception exp)
        {
            if (exp == null) return;
            Error(preStr + ":" + exp.Message + "\n" + exp.StackTrace);
        }
    }
}
