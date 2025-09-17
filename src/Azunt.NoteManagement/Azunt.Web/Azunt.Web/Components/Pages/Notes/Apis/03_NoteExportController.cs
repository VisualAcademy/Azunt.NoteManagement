using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azunt.NoteManagement;
using Microsoft.AspNetCore.Mvc;

// Open XML SDK
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Azunt.Apis.Notes
{
    [Route("api/[controller]")]
    [ApiController]
    public class NoteExportController : ControllerBase
    {
        private readonly INoteRepository _noteRepository;

        public NoteExportController(INoteRepository noteRepository)
        {
            _noteRepository = noteRepository;
        }

        /// <summary>
        /// 게시글 목록 엑셀 다운로드
        /// GET /api/NoteExport/Excel
        /// </summary>
        [HttpGet("Excel")]
        public async Task<IActionResult> ExportToExcel()
        {
            var items = (await _noteRepository.GetAllAsync())?.ToList() ?? [];
            if (items.Count == 0)
                return NotFound("No note records found.");

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true))
                {
                    // Workbook / Worksheet
                    var wbPart = doc.AddWorkbookPart();
                    wbPart.Workbook = new Workbook();

                    var wsPart = wbPart.AddNewPart<WorksheetPart>();
                    var sheetData = new SheetData();
                    wsPart.Worksheet = new Worksheet(sheetData);

                    var sheets = wbPart.Workbook.AppendChild(new Sheets());
                    sheets.Append(new Sheet
                    {
                        Id = wbPart.GetIdOfPart(wsPart),
                        SheetId = 1U,
                        Name = "Notes"
                    });

                    // ---- Header (A1~F1): Id, Name, Title, Category, Created, CreatedBy
                    uint headerRowIndex = 1;
                    var headerRow = new Row { RowIndex = headerRowIndex };
                    sheetData.Append(headerRow);

                    string[] headers = { "Id", "Name", "Title", "Category", "Created", "CreatedBy" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        headerRow.Append(TextCell(Ref(i + 1, (int)headerRowIndex), headers[i]));
                    }

                    // ---- Data (starting A2)
                    uint rowIndex = 2;
                    foreach (var m in items)
                    {
                        var row = new Row { RowIndex = rowIndex };
                        sheetData.Append(row);

                        // Created: DateTimeOffset -> 로컬 시간 문자열로 출력
                        string createdStr = m.Created.ToLocalTime()
                            .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                        var values = new[]
                        {
                            m.Id.ToString(CultureInfo.InvariantCulture),
                            m.Name ?? string.Empty,
                            m.Title ?? string.Empty,
                            m.Category ?? string.Empty,
                            createdStr,
                            m.CreatedBy ?? string.Empty
                        };

                        for (int i = 0; i < values.Length; i++)
                        {
                            row.Append(TextCell(Ref(i + 1, (int)rowIndex), values[i]));
                        }

                        rowIndex++;
                    }

                    wsPart.Worksheet.Save();
                    wbPart.Workbook.Save();
                }

                bytes = ms.ToArray();
            }

            var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_Notes.xlsx";
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        // ===== OpenXML helper methods =====
        private static Cell TextCell(string cellRef, string text) =>
            new Cell
            {
                CellReference = cellRef,
                DataType = CellValues.String,
                CellValue = new CellValue(text ?? string.Empty)
            };

        private static string Ref(int col1Based, int row) => $"{ColName(col1Based)}{row}";

        private static string ColName(int index)
        {
            // 1 -> A, 2 -> B, ... 26 -> Z, 27 -> AA ...
            var dividend = index;
            string col = string.Empty;
            while (dividend > 0)
            {
                var modulo = (dividend - 1) % 26;
                col = (char)('A' + modulo) + col;
                dividend = (dividend - modulo) / 26;
            }
            return col;
        }
    }
}
