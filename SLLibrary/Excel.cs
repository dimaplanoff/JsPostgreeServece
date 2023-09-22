using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System.Text.Json;

namespace SLLibrary
{
    public static class Excel
    {
        public static List<Dictionary<string, object>> ToDictionary(List<List<object>> data)
        {
            string[] head = data[0].Select(m => m.ToString()).ToArray();

            data.RemoveAt(0);

            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

            foreach (var i in data)
            {
                var dict = new Dictionary<string, object>();
                for (int j = 0; j < head.Length; j++)
                    dict.Add(head[j], i[j]);
                rows.Add(dict);
            }

            return rows;

        }     
        
        public static string ToJson(List<List<object>> data, JsonSerializerOptions options)
        {

            return JsonSerializer.Serialize(ToDictionary(data), options);

        }

        private static object GetCellValue(ICell cell)
        {
            object cValue = string.Empty;
            switch (cell.CellType)
            {
                case CellType.Blank:
                case CellType.Unknown:
                    cValue = cell.ToString();
                    break;
                case CellType.Numeric:
                    cValue = cell.NumericCellValue;                                    
                    break;
                case CellType.String:
                    cValue = cell.StringCellValue;
                    break;
                case CellType.Boolean:
                    cValue = cell.BooleanCellValue;
                    break;
                case CellType.Formula:

                    switch (cell.CachedFormulaResultType)
                    {
                        case CellType.Numeric:
                            cValue = cell.NumericCellValue;
                            break;
                        case CellType.String:
                            cValue = cell.StringCellValue;
                            break;
                        case CellType.Boolean:
                            cValue = cell.BooleanCellValue;
                            break;
                    }
                    break;
                case CellType.Error:
                    cValue = cell.ErrorCellValue;
                    break;
                default:
                    cValue = string.Empty;
                    break;
            }
            return cValue;
        }


        public static T GetValue<T>(object[] array, int index)
        {
            try
            {
                if (index >= array.Length)
                    return (T)Activator.CreateInstance(typeof(T));
                return (T)array[index];
            }
            catch
            {
                var type = typeof(T);

                try
                {

                    if (type == typeof(DateTime))
                    {
                        if (DateTime.TryParse(array[index].ToString(), out DateTime dt))
                            return (T)(object)dt;
                        else
                            return (T)(object)DateTime.FromOADate((double)array[index]);
                    }
                    if (type == typeof(decimal))
                    {
                        if (array[index].ToString().TryParseValue(out decimal dec))
                            return (T)(object)dec;
                    }
                    if (type == typeof(double))
                    {
                        if (array[index].ToString().TryParseValue(out double dob))
                            return (T)(object)dob;
                    }
                    if (type == typeof(int))
                    {
                        if (array[index].ToString().TryParseValue(out int i))
                            return (T)(object)i;
                    }
                    if (type == typeof(long))
                    {
                        if (array[index].ToString().TryParseValue(out long l))
                            return (T)(object)l;
                    }
                    if (type == typeof(string))
                    {
                        return (T)(object)array[index].ToString();
                    }

                }
                catch
                {
                    return default;
                }

            }

            return default;
        }


        public static List<List<object>> Parse(byte[] xls, int startIndex = 0)
        {

            //using (var sw = new StreamWriter(Guid.NewGuid().ToString() + ".xlsx", false, System.Text.Encoding.UTF8))
            //{
            //    sw.Write(xls);
            //}

            MemoryStream ms = null;

            try
            {

                IWorkbook book;
                ISheet sheet;

                List<List<object>> result = new();


                try
                {
                    ms = new MemoryStream(xls);
                    ms.Position = 0;
                    book = new XSSFWorkbook(ms);
                }
                catch
                {
                    try
                    {
                        ms = new MemoryStream(xls);
                        ms.Position = 0;
                        book = new HSSFWorkbook(ms);
                    }
                    catch
                    {
                        ms = new MemoryStream(xls);
                        ms.Position = 0;
                        book = WorkbookFactory.Create(ms, true);
                    }
                }

                if (book == null)
                    return null;

                try
                {

                    int maxRowLength = 0;
                    int sheetNum = book.ActiveSheetIndex >= 0 ? book.ActiveSheetIndex : book.NumberOfSheets - 1;
                    sheet = book.GetSheetAt(sheetNum);
                    if (sheet == null)
                        return null;                    
                    int rowCount = sheet.LastRowNum + 1;
                    for (int i = startIndex; i < rowCount; i++)
                    {
                        var row = sheet.GetRow(i);

                        maxRowLength = Math.Max(maxRowLength, row.Cells.Last().Address.Column + 1);
                        var line = new object[maxRowLength].ToList();

                        for (int c = 0; c < row.Cells.Count; c++)
                        {
                            try
                            {
                                line[row.Cells[c].Address.Column] = GetCellValue(row.Cells[c]);
                            }
                            catch
                            {
                                throw;
                            }
                        }

                        result.Add(line);
                    }

                    for (int i = 0; i < result.Count; i++)                        
                        {
                            var dif = maxRowLength - result[i].Count;
                            for (int j = 0; j < dif; j++)
                                result[i].Add(null);
                        }

                }
                finally
                {
                    if (book != null)
                        book.Close();
                }





                for (int i = 0; i < result[0].Count; i++)
                    //if (result[0][i] == null || result[0][i].ToString() == string.Empty)
                        result[0][i] = "Column_" + (i + 1);


                return result;
            }
            catch
            {
                return null;
            }
            finally
            {
                ms?.Dispose();
            }
        }

        public static byte[] Gen(string title, string[] head, List<object[]> vals)
        {
            IWorkbook book = new HSSFWorkbook();
            book.CreateName();
            var sheet = book.CreateSheet("List 1");
            book.SetActiveSheet(0);
            IRow row = null;
            ICell cell = null;
            ICellStyle styleHead = book.CreateCellStyle();
            styleHead.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
            styleHead.Alignment = HorizontalAlignment.Center;
            styleHead.FillPattern = FillPattern.SolidForeground;
            styleHead.BorderTop = BorderStyle.Thin;
            styleHead.TopBorderColor = IndexedColors.Black.Index;
            styleHead.BorderBottom = BorderStyle.Thin;
            styleHead.BottomBorderColor = IndexedColors.Black.Index;


            int lineNumber = string.IsNullOrEmpty(title) ? 0 : 1;

            if (lineNumber == 1)
            {
                row = sheet.CreateRow(0);
                for (int i = 0; i < head.Length; i++)
                {
                    cell = row.CreateCell(i);
                }

                int regionIndex = sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, row.Cells.Count - 1));
                row.Cells[0].SetCellValue(title);
            }

            row = sheet.CreateRow(lineNumber);
            for (int i = 0; i < head.Length; i++)
            {
                cell = row.CreateCell(i);
                cell.SetCellValue(head[i]);
                cell.CellStyle = styleHead;
            }

            foreach (var line in vals)
            {
                lineNumber++;
                row = sheet.CreateRow(lineNumber);

                for (int i = 0; i < head.Length; i++)
                {
                    cell = row.CreateCell(i);
                    if (line.Length > i && line[i] != null)
                    {
                        var type = line[i].GetType();
                        if (type == typeof(string) || type == typeof(DateTime) || type == typeof(char))
                        {
                            cell.SetCellValue((string)line[i]);
                            continue;
                        }
                        if (type == typeof(bool))
                        {
                            cell.SetCellValue((bool)line[i]);
                            continue;
                        }
                        if (line[i] is KeyValuePair<string, string>)
                        {
                            cell.SetCellValue(((KeyValuePair<string, string>)line[i]).Value);
                            continue;
                        }

                        object v = null;
                        try
                        {
                            v = Convert.ChangeType(line[i], typeof(double));
                        }
                        catch
                        {
                            v = (double)line[i];
                        }
                        cell.SetCellValue((double)v);
                        continue;
                    }

                    cell.SetCellValue(string.Empty);
                }
            }

            byte[] r = null;
            MemoryStream ms = new MemoryStream();
            try
            {
                book.Write(ms);
                book.Close();
                r = ms.GetBuffer();
                return r;
            }
            finally
            {
                ms.Dispose();
                GC.Collect();
            }
        }
    }
}
