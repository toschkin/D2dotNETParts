using System;
using System.Collections.Generic;
using System.Linq;
//using System.Security.Cryptography.X509Certificates;
using System.Text;
//using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;

namespace D2ExcelWrapper
{
    public interface IExcelWrapper
    {
        bool OpenExcelFile(string fileName, bool editable);
        string GetCellValue(string sheet, int row, int column);
        bool SetCellValue(string sheet, string str, int row, int column);

        void Close();
    }
    public class ExcelWrapper : IExcelWrapper
    {
        public ExcelWrapper() { }
        private SpreadsheetDocument _documentFile;

        #region IExcelWrapper implementation        
        public bool OpenExcelFile(string fileName, bool editable)
        {
            try
            {
                _documentFile = SpreadsheetDocument.Open(fileName, editable);
                if (_documentFile == null)
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
        public bool SetCellValue(string sheet, string str, int row, int column)
        {
            if (sheet == null || row < 1 || column < 1 || _documentFile == null)
                return false;

            try
            {
                // Retrieve a reference to the workbook part.
                WorkbookPart wbPart = _documentFile.WorkbookPart;

                // Find the sheet with the supplied name, and then use that 
                // Sheet object to retrieve a reference to the first worksheet.
                if (wbPart == null)
                    return false;

                Sheet theSheet = null;
                if (sheet == "")
                    theSheet = wbPart.Workbook.Descendants<Sheet>().FirstOrDefault();
                else
                    theSheet = wbPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name == sheet);

                if (theSheet == null)
                    return false;

                // Retrieve a reference to the worksheet part.
                WorksheetPart wsPart =
                    (WorksheetPart)(wbPart.GetPartById(theSheet.Id));

                if (wsPart == null)
                    return false;


                // Use its Worksheet property to get a reference to the cell 
                // whose address matches the address you supplied.                
                string addressName = GetExcelColumnA1Reference(column) + row;
                Cell theCell = wsPart.Worksheet.Descendants<Cell>().FirstOrDefault(c => c.CellReference == addressName);
                if (theCell == null)
                    return false;

                
                theCell.CellValue = new CellValue(str);
                theCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public string GetCellValue(string sheet, int row, int column)
        {
            string value = "";
            if (sheet == null || row < 1 || column < 1 || _documentFile == null)
                return "";

            try
            {
                // Retrieve a reference to the workbook part.
                WorkbookPart wbPart = _documentFile.WorkbookPart;

                // Find the sheet with the supplied name, and then use that 
                // Sheet object to retrieve a reference to the first worksheet.
                if (wbPart == null)
                    return "";                
                Sheet theSheet = null;
                if (sheet == "")
                    theSheet = wbPart.Workbook.Descendants<Sheet>().FirstOrDefault();
                else
                    theSheet = wbPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name == sheet);

                if (theSheet == null)
                    return "";

                // Retrieve a reference to the worksheet part.
                WorksheetPart wsPart =
                    (WorksheetPart) (wbPart.GetPartById(theSheet.Id));

                if (wsPart == null)
                    return "";


                // Use its Worksheet property to get a reference to the cell 
                // whose address matches the address you supplied.                
                string addressName = GetExcelColumnA1Reference(column) + row;
                Cell theCell = wsPart.Worksheet.Descendants<Cell>().FirstOrDefault(c => c.CellReference == addressName);
                if (theCell == null)
                    return "";
                
                value = theCell.InnerText;

                // If the cell represents an integer number, you are done. 
                // For dates, this code returns the serialized value that 
                // represents the date. The code handles strings and 
                // Booleans individually. For shared strings, the code 
                // looks up the corresponding value in the shared string 
                // table. For Booleans, the code converts the value into 
                // the words TRUE or FALSE.
                if (theCell.DataType != null)
                {
                    switch (theCell.DataType.Value)
                    {
                        case CellValues.SharedString:

                            // For shared strings, look up the value in the
                            // shared strings table.
                            var stringTable =
                                wbPart.GetPartsOfType<SharedStringTablePart>()
                                .FirstOrDefault();

                            // If the shared string table is missing, something 
                            // is wrong. Return the index that is in
                            // the cell. Otherwise, look up the correct text in 
                            // the table.
                            if (stringTable != null)
                            {
                                value =
                                    stringTable.SharedStringTable
                                    .ElementAt(int.Parse(value)).InnerText;
                            }
                            break;

                        case CellValues.Boolean:
                            switch (value)
                            {
                                case "0":
                                    value = "FALSE";
                                    break;
                                default:
                                    value = "TRUE";
                                    break;
                            }
                            break;
                    }
                }
                
            }
            catch (Exception)
            {
                return "";
            }
            return value;
        }

        public void Close()
        {
            if(_documentFile!= null)
                _documentFile.Close();
        }

        #endregion

        #region Helpers
        private static string GetExcelColumnA1Reference(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = String.Empty;
            int modulo = 0;
            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }
            return columnName;
        }
        #endregion
    }
}
