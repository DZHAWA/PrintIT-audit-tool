using ClosedXML.Excel;
using PrintIT.Models;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace PrintIT.Services;

public class ExportService
{
    // Реквизиты документа (оформление в деловом стиле, близком к ГОСТ Р 7.0.97)
    private const string OrganizationName = "Комитет по образованию Санкт-Петербурга";
    private const string DocumentTitle = "ВЕДОМОСТЬ";
    private const string DocumentSubtitle = "учета печатающих устройств и состояния картриджей";
    private const string UrgentOnlyNote = "(устройства, требующие замены картриджа)";
    private const string StatusUrgent = "Требуется замена";
    private const string StatusOk = "Замена не требуется";
    private const string FontName = "Times New Roman";

    private static readonly string[] ColumnHeaders = { "№ п/п", "Кабинет", "Модель устройства", "Состояние картриджа" };

    private record ReportRow(string RoomNumber, string Model, bool IsUrgent);

    /// <summary>
    /// Генерация отчета в формате Word (.docx)
    /// </summary>
    public string GenerateDocx(InventorySession session, bool onlyUrgent = false)
    {
        string filePath = PrepareFilePath("docx");
        var rows = GetReportRows(session, onlyUrgent);
        var now = DateTime.Now;

        using (var doc = DocX.Create(filePath))
        {
            // Лист А4, поля: левое 3 см, правое 1,5 см, верхнее и нижнее 2 см
            doc.PageWidth = CmToPt(21.0f);
            doc.PageHeight = CmToPt(29.7f);
            doc.MarginLeft = CmToPt(3.0f);
            doc.MarginRight = CmToPt(1.5f);
            doc.MarginTop = CmToPt(2.0f);
            doc.MarginBottom = CmToPt(2.0f);
            doc.SetDefaultFont(new Xceed.Document.NET.Font(FontName), 12d);

            // Шапка
            AddParagraph(doc, OrganizationName, Alignment.center, spacingAfter: 18);
            AddParagraph(doc, DocumentTitle, Alignment.center, fontSize: 14, bold: true);
            AddParagraph(doc, DocumentSubtitle, Alignment.center, fontSize: 14, bold: true,
                         spacingAfter: onlyUrgent ? 0 : 18);
            if (onlyUrgent)
                AddParagraph(doc, UrgentOnlyNote, Alignment.center, spacingAfter: 18);

            AddParagraph(doc, $"Учет: «{session.Title}»", Alignment.left);
            AddParagraph(doc, $"Дата составления: {now:dd.MM.yyyy}", Alignment.left, spacingAfter: 12);

            if (rows.Count == 0)
            {
                AddParagraph(doc, onlyUrgent
                    ? "Устройства, требующие замены картриджа, не выявлены."
                    : "Устройства не зарегистрированы.", Alignment.left, spacingAfter: 12);
            }
            else
            {
                var table = doc.AddTable(rows.Count + 1, ColumnHeaders.Length);
                table.Design = TableDesign.TableGrid;
                table.Alignment = Alignment.center;
                table.AutoFit = AutoFit.Fixed;

                // Ширина колонок по ширине области печати (16,5 см)
                table.SetWidths(new[] { CmToPt(1.3f), CmToPt(3.2f), CmToPt(7.7f), CmToPt(4.3f) });
                table.SetTableCellMargin(TableCellMarginType.left, CmToPt(0.15f));
                table.SetTableCellMargin(TableCellMarginType.right, CmToPt(0.15f));

                // Шапка таблицы: повторяется на каждой странице
                var header = table.Rows[0];
                header.TableHeader = true;
                header.BreakAcrossPages = false;
                for (int c = 0; c < ColumnHeaders.Length; c++)
                {
                    var cell = header.Cells[c];
                    cell.FillColor = Xceed.Drawing.Color.Parse(242, 242, 242);
                    FillCell(cell, ColumnHeaders[c], Alignment.center, bold: true);
                }

                // Строки
                for (int i = 0; i < rows.Count; i++)
                {
                    var item = rows[i];
                    var row = table.Rows[i + 1];
                    row.BreakAcrossPages = false;

                    bool firstInRoom = i == 0 || rows[i - 1].RoomNumber != item.RoomNumber;

                    FillCell(row.Cells[0], (i + 1).ToString(), Alignment.center);
                    FillCell(row.Cells[1], firstInRoom ? item.RoomNumber : string.Empty, Alignment.center);
                    FillCell(row.Cells[2], item.Model, Alignment.left);
                    FillCell(row.Cells[3], item.IsUrgent ? StatusUrgent : StatusOk, Alignment.center, bold: item.IsUrgent);
                }

                // Объединяем ячейки "Кабинет" для устройств одного помещения
                int groupStart = 1;
                for (int r = 2; r <= rows.Count + 1; r++)
                {
                    bool groupEnds = r == rows.Count + 1 || rows[r - 1].RoomNumber != rows[r - 2].RoomNumber;
                    if (!groupEnds) continue;

                    if (r - 1 > groupStart)
                        table.MergeCellsInColumn(1, groupStart, r - 1);
                    groupStart = r;
                }

                doc.InsertTable(table);
                AddParagraph(doc, string.Empty, Alignment.left);
            }

            // Итоги
            int roomsCount = rows.Select(r => r.RoomNumber).Distinct().Count();
            int urgentCount = rows.Count(r => r.IsUrgent);
            AddParagraph(doc, $"Помещений: {roomsCount}. Устройств: {rows.Count}, " +
                              $"из них требуется замена картриджа: {urgentCount}.", Alignment.left, spacingAfter: 36);

            // Подпись
            AddParagraph(doc, "Ответственный     _______________     / _________________________ /", Alignment.left, spacingAfter: 0);
            AddParagraph(doc, "                                         (подпись)                         (расшифровка подписи)",
                         Alignment.left, fontSize: 9, spacingAfter: 18);
            AddParagraph(doc, "«____» _______________ 20____ г.", Alignment.left);

            doc.Save();
        }

        return filePath;
    }

    /// <summary>
    /// Генерация отчета в формате Excel (.xlsx)
    /// </summary>
    public string GenerateXlsx(InventorySession session, bool onlyUrgent = false)
    {
        string filePath = PrepareFilePath("xlsx");
        var rows = GetReportRows(session, onlyUrgent);
        const int lastCol = 4;

        // В Excel — только таблица, без реквизитов документа: её удобно фильтровать и копировать
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Устройства");

        // Ширина колонок (в символах)
        ws.Column(1).Width = 8;
        ws.Column(2).Width = 18;
        ws.Column(3).Width = 42;
        ws.Column(4).Width = 22;

        const int headerRow = 1;
        for (int c = 0; c < ColumnHeaders.Length; c++)
            ws.Cell(headerRow, c + 1).Value = ColumnHeaders[c];

        var headerRange = ws.Range(headerRow, 1, headerRow, lastCol);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Alignment.WrapText = true;
        ws.Row(headerRow).Height = 22;

        for (int i = 0; i < rows.Count; i++)
        {
            int row = headerRow + 1 + i;
            var item = rows[i];

            ws.Cell(row, 1).Value = i + 1;

            // Номер кабинета: число пишем числом (иначе Excel помечает "число как текст")
            if (int.TryParse(item.RoomNumber, out int roomNum))
                ws.Cell(row, 2).Value = roomNum;
            else
                ws.Cell(row, 2).Value = item.RoomNumber;

            ws.Cell(row, 3).Value = item.Model;
            ws.Cell(row, 4).Value = item.IsUrgent ? StatusUrgent : StatusOk;
            if (item.IsUrgent) ws.Cell(row, 4).Style.Font.Bold = true;

            // Чуть свободнее стандартной высоты; длинные названия оставляем на автоподбор с переносом
            if (item.Model.Length <= 45) ws.Row(row).Height = 18;
        }

        int lastRow = headerRow + Math.Max(rows.Count, 0);
        var tableRange = ws.Range(headerRow, 1, lastRow, lastCol);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        if (rows.Count > 0)
        {
            var body = ws.Range(headerRow + 1, 1, lastRow, lastCol);
            body.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            body.Column(2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            body.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            body.Column(3).Style.Alignment.WrapText = true;
            body.Column(3).Style.Alignment.Indent = 1;
            body.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            tableRange.SetAutoFilter();
        }

        // Закрепляем шапку таблицы при прокрутке
        ws.SheetView.FreezeRows(headerRow);

        // Если таблицу всё же распечатают: А4, по ширине листа, шапка на каждой странице
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);

        workbook.SaveAs(filePath);
        return filePath;
    }

    // ===== Вспомогательные методы оформления =====

    private static string PrepareFilePath(string extension)
    {
        string fileName = $"Ведомость_оргтехники_{DateTime.Now:yyyy-MM-dd_HH-mm}.{extension}";
        string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
        if (File.Exists(filePath)) File.Delete(filePath);
        return filePath;
    }

    private static float CmToPt(float cm) => cm * 72f / 2.54f;

    private static void AddParagraph(DocX doc, string text, Alignment alignment,
                                     double fontSize = 12, bool bold = false, double spacingAfter = 0)
    {
        var p = doc.InsertParagraph(text).Font(FontName).FontSize(fontSize).SpacingAfter(spacingAfter);
        if (bold) p.Bold();
        p.Alignment = alignment;
    }

    private static void FillCell(Xceed.Document.NET.Cell cell, string text, Alignment alignment, bool bold = false)
    {
        cell.VerticalAlignment = Xceed.Document.NET.VerticalAlignment.Center;
        var p = cell.Paragraphs[0];
        p.Append(text).Font(FontName).FontSize(12);
        if (bold) p.Bold();
        p.Alignment = alignment;
        p.SpacingBefore(2).SpacingAfter(2);
    }


    /// <summary>
    /// Копирование готового отчета в системную папку "Загрузки".
    /// Возвращает путь (или описание места), куда сохранён файл.
    /// </summary>
    public async Task<string> SaveToDownloadsAsync(string sourcePath)
    {
        string fileName = Path.GetFileName(sourcePath);

#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            // Android 10+: запись в общие "Загрузки" через MediaStore, разрешения не нужны
            var resolver = Platform.AppContext.ContentResolver
                ?? throw new InvalidOperationException("ContentResolver недоступен.");

            var values = new Android.Content.ContentValues();
            values.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, GetMimeType(fileName));
            values.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);

            var uri = resolver.Insert(Android.Provider.MediaStore.Downloads.ExternalContentUri!, values)
                ?? throw new IOException("Не удалось создать файл в папке «Загрузки».");

            using (var output = resolver.OpenOutputStream(uri)
                ?? throw new IOException("Не удалось открыть файл в папке «Загрузки» для записи."))
            using (var input = File.OpenRead(sourcePath))
            {
                await input.CopyToAsync(output);
            }

            return Path.Combine(Android.OS.Environment.DirectoryDownloads!, fileName);
        }
        else
        {
            // Android 9 и ниже: нужно разрешение на запись во внешнее хранилище
            var status = await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.StorageWrite>);
            if (status != PermissionStatus.Granted)
                throw new UnauthorizedAccessException("Нет разрешения на запись в папку «Загрузки».");

#pragma warning disable CA1422 // Устаревший API используется только на старых версиях Android
            string downloadsDir = Android.OS.Environment
                .GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)!.AbsolutePath;
#pragma warning restore CA1422
            return CopyWithUniqueName(sourcePath, downloadsDir);
        }
#elif WINDOWS
        return CopyWithUniqueName(sourcePath, GetWindowsDownloadsFolder());
#else
        string downloadsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        return CopyWithUniqueName(sourcePath, downloadsDir);
#endif
    }

    /// <summary>
    /// Копирует файл в папку, добавляя " (1)", " (2)"... если такое имя уже занято
    /// </summary>
    private static string CopyWithUniqueName(string sourcePath, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        string name = Path.GetFileNameWithoutExtension(sourcePath);
        string ext = Path.GetExtension(sourcePath);
        string targetPath = Path.Combine(targetDir, name + ext);

        for (int i = 1; File.Exists(targetPath); i++)
            targetPath = Path.Combine(targetDir, $"{name} ({i}){ext}");

        File.Copy(sourcePath, targetPath);
        return targetPath;
    }

    private static string GetMimeType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream"
    };

#if WINDOWS
    [System.Runtime.InteropServices.DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStruct)] Guid rfid,
        uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

    /// <summary>
    /// Реальный путь к "Загрузкам" (учитывает перенос папки пользователем)
    /// </summary>
    private static string GetWindowsDownloadsFolder()
    {
        var downloadsId = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        if (SHGetKnownFolderPath(downloadsId, 0, IntPtr.Zero, out IntPtr pathPtr) == 0)
        {
            try
            {
                string? path = System.Runtime.InteropServices.Marshal.PtrToStringUni(pathPtr);
                if (!string.IsNullOrEmpty(path)) return path;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pathPtr);
            }
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }
#endif

    /// <summary>
    /// Отправка или открытие сгенерированного документа
    /// </summary>
    public async Task ShareReportAsync(string filePath, string title = "Отчет по оргтехнике")
    {
        if (!File.Exists(filePath)) return;

        try
        {
            // На мобильных устройствах открывается системный диалог "Поделиться"
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = title,
                File = new ShareFile(filePath)
            });
        }
        catch
        {
            // На Windows рабочий стол: открываем файл напрямую в приложении по умолчанию
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                Title = title,
                File = new ReadOnlyFile(filePath)
            });
        }
    }

    /// <summary>
    /// Плоский список строк отчета: кабинеты по порядку (сначала номерные по возрастанию,
    /// затем названные — по алфавиту), внутри кабинета устройства в порядке добавления
    /// </summary>
    private static List<ReportRow> GetReportRows(InventorySession session, bool onlyUrgent)
    {
        return session.Rooms
            .OrderBy(room => int.TryParse(room.RoomNumber, out _) ? 0 : 1)
            .ThenBy(room => int.TryParse(room.RoomNumber, out int n) ? n : 0)
            .ThenBy(room => room.RoomNumber, StringComparer.CurrentCultureIgnoreCase)
            .SelectMany(room => room.Printers
                .Where(p => !onlyUrgent || p.IsUrgent)
                .Select(p => new ReportRow(room.RoomNumber.Trim(), p.Model, p.IsUrgent)))
            .ToList();
    }
}
