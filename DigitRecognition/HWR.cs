using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DigitRecognition
{
    public static class Hwr
    {
        [DllImport("HWRDLL.dll", EntryPoint = "DigitRecognition", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int DigitRecognition(string imageFilePath, string templateFilePath);

        public static int Run(string img,string dataPath)
        {
            return DigitRecognition(img, dataPath);
        }
    }
}
