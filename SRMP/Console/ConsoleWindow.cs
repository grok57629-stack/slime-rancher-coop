using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace SRMultiplayer
{
    public class ConsoleWindow
    {
        private TextWriter oldOutput;
        
        private const int STD_OUTPUT_HANDLE = -11;
        /// <summary>
        /// Create the console window
        /// </summary>
        public void Initialize()
        {
            bool flag = !ConsoleWindow.AttachConsole(uint.MaxValue);
            if (flag)
            {
                ConsoleWindow.AllocConsole();
            }
            this.oldOutput = Console.Out;
            try
            {
                IntPtr stdHandle = ConsoleWindow.GetStdHandle(-11);
                SafeFileHandle handle = new SafeFileHandle(stdHandle, true);
                FileStream stream = new FileStream(handle, FileAccess.Write);
                Encoding ascii = Encoding.ASCII;
                Console.SetOut(new StreamWriter(stream, ascii)
                {
                    AutoFlush = true
                });
            }
            catch (Exception ex)
            {
                SRMP.Log("Couldn't redirect output: " + ex.Message);
            }
        }
        
        public void Shutdown()
        {
            Console.SetOut(this.oldOutput);
            ConsoleWindow.FreeConsole();
        }
        
        public void SetTitle(string strName)
        {
            ConsoleWindow.SetConsoleTitle(strName);
        }
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();
        
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleTitle(string lpConsoleTitle);
    }
}
