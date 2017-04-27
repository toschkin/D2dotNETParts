using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D2ExcelWrapper;

namespace TestD2ExcelWrapper
{
    class Program
    {
        static void Main(string[] args)
        {
            ExcelWrapper wrapper = new ExcelWrapper();
            if (wrapper.OpenExcelFile(@"c:\1.xlsx", true))
            {
                string val = wrapper.GetCellValue("Лист1", 1, 1);
                if (wrapper.SetCellValue("Лист1", val + "X", 1, 1))
                {
                    string val2 = wrapper.GetCellValue("Лист1", 1, 1);
                }
            }            
            wrapper.Close();
        }
    }
}
