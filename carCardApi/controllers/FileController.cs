
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using OfficeOpenXml.Style;


namespace carCard.Controllers
{
[ApiController]
[Route("api/[controller]")]

public class FileController : ControllerBase
{
    private readonly ConnectionProvider _connectionProvider;
    private readonly DataTableService _dataTableService;
    private ExceptionLogger exceptionLogger;
    public FileController(ConnectionProvider connectionProvider, DataTableService dataTableService)
    {
        
        _connectionProvider = connectionProvider;
        _dataTableService = dataTableService;
        exceptionLogger = new ExceptionLogger(_connectionProvider);
    }
    
        [HttpPost("file-upload")]
        [Authorize]
        public async Task<IActionResult> UploadExcelFile(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            try
            {
                // Read Excel into a DataTable
                var usageDataTable = new DataTable();

                using (var stream = excelFile.OpenReadStream())
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension.Rows;
                        var colCount = worksheet.Dimension.Columns;

                        // Find the header row (assuming header row has a "Data" cell in the first column)
                        int headerRow = 1;
                        for (int row = 1; row <= rowCount; row++)
                        {
                            var firstCellValue = worksheet.Cells[row, 1].Text.Trim();
                            if (!string.IsNullOrEmpty(firstCellValue) && firstCellValue == "Data")
                            {
                                headerRow = row;
                                break;
                            }
                        }
                        Console.WriteLine($"Header row detected at: {headerRow}");

                        // Add columns to DataTable
                        for (int col = 1; col <= colCount; col++)
                        {
                            var columnName = worksheet.Cells[headerRow, col].Text.Trim();
                            if (!string.IsNullOrEmpty(columnName))
                            {
                                if (!usageDataTable.Columns.Contains(columnName))
                                {
                                    usageDataTable.Columns.Add(columnName);
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Skipped empty column at index {col}");
                            }
                        }

                        // Add rows to DataTable
                        for (int row = headerRow + 1; row <= rowCount; row++)
                        {
                            var rowData = usageDataTable.NewRow();
                            bool isRowEmpty = true;
                            for (int col = 1; col <= colCount; col++)
                            {
                                var cellValue = worksheet.Cells[row, col].Text.Trim();
                                if (!string.IsNullOrEmpty(cellValue) && col - 1 < usageDataTable.Columns.Count)
                                {
                                    rowData[col - 1] = cellValue;
                                    isRowEmpty = false;
                                }
                            }
                            if (!isRowEmpty)
                            {
                                usageDataTable.Rows.Add(rowData);
                            }
                        }
                    }
                }

                // Validate all rows first: check that every row has a valid card.
                // We assume the mandatory columns are "Kortelės numeris", "Data", and "Kiekis"
                List<string> missingCards = new List<string>();
                List<(DataRow row, int cardId, int fuelType)> validRows = new List<(DataRow, int, int)>();

                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();

                    foreach (DataRow row in usageDataTable.Rows)
                    {
                        if (!usageDataTable.Columns.Contains("Kortelės numeris") ||
                            !usageDataTable.Columns.Contains("Data") ||
                            !usageDataTable.Columns.Contains("Kiekis"))
                        {
                            // If mandatory columns are missing, you can either skip or abort.
                            Console.WriteLine("Mandatory columns missing in Excel file.");
                            return BadRequest(new { message = "Privalomi stulpeliai nerasti Excel faile." });
                        }

                        var cardNumber = row["Kortelės numeris"].ToString();
                        if (string.IsNullOrEmpty(cardNumber))
                        {
                            missingCards.Add("Tuščias kortelės numeris");
                            continue;
                        }

                        // Query the card table for this card number
                        var cardQuery = "SELECT FCA_ID, FCA_FUEL_TYPE FROM CARDS WHERE FCA_NUMBER = @CardNumber";
                        int? cardId = null;
                        int? fuelType = null;
                        using (var cardCommand = new SqlCommand(cardQuery, connection))
                        {
                            cardCommand.Parameters.AddWithValue("@CardNumber", cardNumber);
                            using (var reader = await cardCommand.ExecuteReaderAsync())
                            {
                                if (reader.Read())
                                {
                                    cardId = reader.GetInt32(0);
                                    fuelType = reader.GetInt32(1);
                                }
                            }
                        }

                        if (cardId == null)
                        {
                            missingCards.Add(cardNumber);
                        }
                        else
                        {
                            validRows.Add((row, cardId.Value, fuelType ?? 0));
                        }
                    }
                }

                // If there are any missing cards, abort the entire operation.
                if (missingCards.Count > 0)
                {
                    return BadRequest(new { message = "Nerastos kortelės: " + string.Join(", ", missingCards) });
                }

                // If all rows are valid, proceed to insert the usage records.
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    foreach (var (row, cardId, fuelType) in validRows)
                    {
                        var usageQuery = @"
                            INSERT INTO CARDS_USAGE(
                                FCU_FCA_ID, FCU_DATE, FCU_FUEL_TYPE, FCU_CHECK_NUMBER,
                                FCU_QTY, FCU_PRICE_PER_UNIT, FCU_AMOUNT_PVM_NO_DISCOUNT,
                                FCU_PVM, FCU_AMOUNT_NO_PVM_NO_DISCOUNT, FCU_DISCOUNT,
                                FCU_TOTAL_AMOUNT, FCU_FUELING_PLACE_NAME, FCU_COUNTRY
                            ) 
                            VALUES (
                                @CardId, @UsageDate, @FuelType, @CheckNumber,
                                @Quantity, @PricePerUnit, @PricePVMNoDiscount,
                                @PVM, @PriceNoPvmNoDiscount, @Discount,
                                @TotalAmount, @Place, @Country
                            )";
                        using (var usageCommand = new SqlCommand(usageQuery, connection))
                        {
                            usageCommand.Parameters.AddWithValue("@CardId", cardId);
                            usageCommand.Parameters.AddWithValue("@UsageDate", DateTime.Parse(row["Data"].ToString()));
                            usageCommand.Parameters.AddWithValue("@FuelType", fuelType);
                            usageCommand.Parameters.AddWithValue("@Quantity", Convert.ToDecimal(row["Kiekis"]));
                            // You need to ensure that the following columns exist in your Excel file and are properly named:
                            usageCommand.Parameters.AddWithValue("@PricePerUnit", Convert.ToDecimal(row["Vnt. kaina"]));
                            usageCommand.Parameters.AddWithValue("@PricePVMNoDiscount", Convert.ToDecimal(row["Suma su PVM (be nuolaidos)"]));
                            usageCommand.Parameters.AddWithValue("@PVM", Convert.ToDecimal(row["PVM"]));
                            usageCommand.Parameters.AddWithValue("@PriceNoPvmNoDiscount", Convert.ToDecimal(row["Suma, be PVM"]));
                            usageCommand.Parameters.AddWithValue("@Discount", Convert.ToDecimal(row["Nuolaida"]));
                            usageCommand.Parameters.AddWithValue("@CheckNumber", row["Kvito Nr."].ToString());
                            usageCommand.Parameters.AddWithValue("@TotalAmount", Convert.ToDecimal(row["Iš viso su PVM (su nuolaida)"]));
                            usageCommand.Parameters.AddWithValue("@Place", row["Degalinė"].ToString());
                            usageCommand.Parameters.AddWithValue("@Country", row["Šalis"].ToString());

                            await usageCommand.ExecuteNonQueryAsync();
                        }
                    }
                }

                return Ok(new { message = "Sekmingai ikelta" });
            }
            catch (Exception ex)
            {
                exceptionLogger.LogException(
                    source: "UploadExcelFile",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );

                return StatusCode(500, new { message = "Ivyko klaida", details = ex.Message });
            }
        }
    
    // New endpoint: Skip rows with missing card numbers and process only valid ones
        [HttpPost("file-upload-skip")]
        [Authorize]
        public async Task<IActionResult> UploadExcelFileSkipMissing(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                return BadRequest(new { message = "Nebuvo įkeltas failas" });
            }

            try
            {
                // Read Excel into a DataTable
                var usageDataTable = new DataTable();

                using (var stream = excelFile.OpenReadStream())
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;
                        int colCount = worksheet.Dimension.Columns;

                        // Find the header row (assuming header row has a "Data" cell in the first column)
                        int headerRow = 1;
                        for (int row = 1; row <= rowCount; row++)
                        {
                            var firstCellValue = worksheet.Cells[row, 1].Text.Trim();
                            if (!string.IsNullOrEmpty(firstCellValue) && firstCellValue == "Data")
                            {
                                headerRow = row;
                                break;
                            }
                        }
                        Console.WriteLine($"Header row detected at: {headerRow}");

                        // Add columns to DataTable
                        for (int col = 1; col <= colCount; col++)
                        {
                            var columnName = worksheet.Cells[headerRow, col].Text.Trim();
                            if (!string.IsNullOrEmpty(columnName))
                            {
                                if (!usageDataTable.Columns.Contains(columnName))
                                {
                                    usageDataTable.Columns.Add(columnName);
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Skipped empty column at index {col}");
                            }
                        }

                        // Add rows to DataTable
                        for (int row = headerRow + 1; row <= rowCount; row++)
                        {
                            var rowData = usageDataTable.NewRow();
                            bool isRowEmpty = true;
                            for (int col = 1; col <= colCount; col++)
                            {
                                var cellValue = worksheet.Cells[row, col].Text.Trim();
                                if (!string.IsNullOrEmpty(cellValue) && col - 1 < usageDataTable.Columns.Count)
                                {
                                    rowData[col - 1] = cellValue;
                                    isRowEmpty = false;
                                }
                            }
                            if (!isRowEmpty)
                            {
                                usageDataTable.Rows.Add(rowData);
                            }
                        }
                    }
                }

                // Validate rows: collect valid rows and track missing card numbers.
                List<string> missingCards = new List<string>();
                List<(DataRow row, int cardId, int fuelType)> validRows = new List<(DataRow, int, int)>();

                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();

                    foreach (DataRow row in usageDataTable.Rows)
                    {
                        // Check for mandatory columns.
                        if (!usageDataTable.Columns.Contains("Kortelės numeris") ||
                            !usageDataTable.Columns.Contains("Data") ||
                            !usageDataTable.Columns.Contains("Kiekis"))
                        {
                            Console.WriteLine("Mandatory columns missing in Excel file.");
                            return BadRequest(new { message = "Privalomi stulpeliai trūksta Excel faile." });
                        }

                        var cardNumber = row["Kortelės numeris"].ToString();
                        if (string.IsNullOrEmpty(cardNumber))
                        {
                            missingCards.Add("Tuščias kortelės numeris");
                            continue;
                        }

                        var cardQuery = "SELECT FCA_ID, FCA_FUEL_TYPE FROM CARDS WHERE FCA_NUMBER = @CardNumber";
                        int? cardId = null;
                        int? fuelType = null;
                        using (var cardCommand = new SqlCommand(cardQuery, connection))
                        {
                            cardCommand.Parameters.AddWithValue("@CardNumber", cardNumber);
                            using (var reader = await cardCommand.ExecuteReaderAsync())
                            {
                                if (reader.Read())
                                {
                                    cardId = reader.GetInt32(0);
                                    fuelType = reader.GetInt32(1);
                                }
                            }
                        }
                        if (cardId == null)
                        {
                            missingCards.Add(cardNumber);
                        }
                        else
                        {
                            validRows.Add((row, cardId.Value, fuelType ?? 0));
                        }
                    }
                }
                int insertedCount = 0;
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    foreach (var (row, cardId, fuelType) in validRows)
                    {
                        var usageQuery = @"
                            INSERT INTO CARDS_USAGE (
                                FCU_FCA_ID, FCU_DATE, FCU_FUEL_TYPE, FCU_CHECK_NUMBER,
                                FCU_QTY, FCU_PRICE_PER_UNIT, FCU_AMOUNT_PVM_NO_DISCOUNT,
                                FCU_PVM, FCU_AMOUNT_NO_PVM_NO_DISCOUNT, FCU_DISCOUNT,
                                FCU_TOTAL_AMOUNT, FCU_FUELING_PLACE_NAME, FCU_COUNTRY
                            ) 
                            VALUES (
                                @CardId, @UsageDate, @FuelType, @CheckNumber,
                                @Quantity, @PricePerUnit, @PricePVMNoDiscount,
                                @PVM, @PriceNoPvmNoDiscount, @Discount,
                                @TotalAmount, @Place, @Country
                            )";
                        using (var usageCommand = new SqlCommand(usageQuery, connection))
                        {
                            usageCommand.Parameters.AddWithValue("@CardId", cardId);
                            usageCommand.Parameters.AddWithValue("@UsageDate", DateTime.Parse(row["Data"].ToString()));
                            usageCommand.Parameters.AddWithValue("@FuelType", fuelType);
                            usageCommand.Parameters.AddWithValue("@Quantity", Convert.ToDecimal(row["Kiekis"]));
                            // You need to ensure that the following columns exist in your Excel file and are properly named:
                            usageCommand.Parameters.AddWithValue("@PricePerUnit", Convert.ToDecimal(row["Vnt. kaina"]));
                            usageCommand.Parameters.AddWithValue("@PricePVMNoDiscount", Convert.ToDecimal(row["Suma su PVM (be nuolaidos)"]));
                            usageCommand.Parameters.AddWithValue("@PVM", Convert.ToDecimal(row["PVM"]));
                            usageCommand.Parameters.AddWithValue("@PriceNoPvmNoDiscount", Convert.ToDecimal(row["Suma, be PVM"]));
                            usageCommand.Parameters.AddWithValue("@Discount", Convert.ToDecimal(row["Nuolaida"]));
                            usageCommand.Parameters.AddWithValue("@CheckNumber", row["Kvito Nr."].ToString());
                            usageCommand.Parameters.AddWithValue("@TotalAmount", Convert.ToDecimal(row["Iš viso su PVM (su nuolaida)"]));
                            usageCommand.Parameters.AddWithValue("@Place", row["Degalinė"].ToString());
                            usageCommand.Parameters.AddWithValue("@Country", row["Šalis"].ToString());

                            await usageCommand.ExecuteNonQueryAsync();
                            insertedCount++;
                        }
                    }
                }

                // Build a response message indicating success and any missing card numbers.
                string responseMessage = $"Sekmingai įkelta {insertedCount} įrašų.";
                if (missingCards.Count > 0)
                {
                    responseMessage += " Nerastos kortelės: " + string.Join(", ", missingCards);
                }

                return Ok(new { message = responseMessage });
            }
            catch (Exception ex)
            {
                exceptionLogger.LogException(
                    source: "UploadExcelFile-skip",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );

                return StatusCode(500, new { message = "Nepavyko pridėti failo", details = ex.Message });
            }
        }

        


            [HttpGet("GetExcelFile")]
            [Authorize]
            public async Task<IActionResult> GetExcelFile(
                [FromQuery] string CLI_ID, 
                [FromQuery] string date, 
                [FromQuery] int? kodas,
                [FromQuery] int? formType
            )
            {
                
                if (string.IsNullOrEmpty(date) || !DateTime.TryParse($"{date}-01", out DateTime startDate))
                {
                    return BadRequest("Blogas datos formatas");
                }      
                if(formType.HasValue){Console.WriteLine(""+formType.ToString);}
                
                string query = "";
                if (formType.HasValue && formType == 1)
                {
                    query = @"
                        SELECT
                            FT.FUEL_PRK_KODAS         AS [Prekės Nr.],
                            C.CAR_SANDELIS            AS [Sandėlis],
                            SUM(U.FCU_QTY)            AS [Kiekis],
                            SUM(U.FCU_TOTAL_AMOUNT)   AS [Grynoji suma],
                            ''                        AS [vIšlaidųCentras],
                            C.CAR_PADALINYS           AS [vPadalinys],
                            T.TYPE_CODE               AS [vPirkėjas],
                            C.CAR_TIKSLAS             AS [vTikslas]
                        FROM CARDS_USAGE U
                            LEFT JOIN FUEL_TYPE FT ON U.FCU_FUEL_TYPE = FT.FUEL_ID
                            LEFT JOIN CARS C       ON U.FCU_FCA_ID = C.CAR_FCA_ID
                            LEFT JOIN CAR_TYPE T   ON C.CAR_TYPE = T.TYPE_ID
                            LEFT JOIN CARDS CD     ON U.FCU_FCA_ID = CD.FCA_ID
                        WHERE CD.FCA_CLI_ID = @FCA_CLI_ID 
                            AND U.FCU_DATE >= @startDate
                            AND U.FCU_DATE < DATEADD(MONTH, 1, @startDate)
                    ";
                }
                else if (formType.HasValue && formType == 2)
                {
                    query = @"
                    SELECT
                    FT.FUEL_PRK_KODAS                              AS [Prekės Nr.],
                    C.CAR_SANDELIS                                 AS [Sandėlis],
                    SUM(U.FCU_QTY)                                 AS [Kiekis],
                    MAX(U.FCU_PRICE_PER_UNIT)                      AS [Vieneto kaina],  -- or AVG/ MIN as appropriate
                    SUM(U.FCU_TOTAL_AMOUNT)                        AS [Grynoji suma-apmokestinama PVM],
                    ''                                             AS [Grynoji suma-Neapmokestinama PVM],
                    ''                                             AS [Prekės PVM grupė, kai Grynoji suma-apmokestinama PVM],
                    ''                                             AS [Prekės PVM grupė, kai Grynoji suma-NEapmokestinama PVM],
                    ''                                             AS [vIšlaidųCentras],
                    C.CAR_PADALINYS                                AS [vPadalinys],
                    ''                                             AS [vPirkėjas],
                    C.CAR_TIKSLAS                                  AS [vTikslas]
                FROM CARDS_USAGE U
                    LEFT JOIN FUEL_TYPE FT ON U.FCU_FUEL_TYPE = FT.FUEL_ID
                    LEFT JOIN CARS C       ON U.FCU_FCA_ID     = C.CAR_FCA_ID
                    LEFT JOIN CAR_TYPE T   ON C.CAR_TYPE       = T.TYPE_ID
                    LEFT JOIN CARDS CD     ON U.FCU_FCA_ID     = CD.FCA_ID
                WHERE CD.FCA_CLI_ID = @FCA_CLI_ID 
                    AND U.FCU_DATE >= @startDate
                    AND U.FCU_DATE < DATEADD(MONTH, 1, @startDate)

                    ";
                }
                else if (formType.HasValue && formType == 3)
                {
                    query = @"
                        SELECT
                                ''                                         AS [Account Number],
                                SUM(U.FCU_TOTAL_AMOUNT)                    AS [Amount],             
                                ''                                         AS [Explanation -Remark-],
                                ''                                         AS [Co],
                                'C'                                        AS [Sub Type],           
                                '00' + COALESCE(C.CAR_PLATE_NUMBER, '')    AS [Sub-ledger],
                                'AA'                                       AS [Ledger Type],        
                                ''                                         AS [B C],
                                ''                                         AS [P C],
                                SUM(U.FCU_QTY)                             AS [Units],              
                                'LT'                                       AS [UM],                 
                                ''                                         AS [PO Doc Co],
                                ''                                         AS [PO Doc Type],
                                ''                                         AS [Purchase Order],
                                ''                                         AS [Subledger Description]
                            FROM CARDS_USAGE U
                                LEFT JOIN CARS C       ON U.FCU_FCA_ID = C.CAR_FCA_ID
                                LEFT JOIN FUEL_TYPE FT ON U.FCU_FUEL_TYPE = FT.FUEL_ID
                                LEFT JOIN CAR_TYPE T   ON C.CAR_TYPE     = T.TYPE_ID
                                LEFT JOIN CARDS CD     ON U.FCU_FCA_ID   = CD.FCA_ID
                            WHERE CD.FCA_CLI_ID = @FCA_CLI_ID
                            AND U.FCU_DATE >= @startDate
                            AND U.FCU_DATE < DATEADD(MONTH, 1, @startDate)";


                }
                if (kodas.HasValue)
                {
                    query += " AND T.TYPE_ID = @kodas";
                }
                if (formType == 1 || formType == 2){
                query += @"
                    GROUP BY
                        FT.FUEL_PRK_KODAS,
                        C.CAR_SANDELIS,
                        C.CAR_PADALINYS,
                        T.TYPE_CODE,
                        C.CAR_TIKSLAS;
                ";
                }
                else if (formType == 3)
                {
                       
                query += @"  GROUP BY
                    '00' + COALESCE(C.CAR_PLATE_NUMBER, '') 


                ";


                }
                var dataTable = new DataTable();
                SortedDictionary<string, string> dictionary = new SortedDictionary<string, string>();
                try
                {
                    using (var connection = _connectionProvider.GetConnection())
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }
                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@FCA_CLI_ID", CLI_ID);
                            command.Parameters.AddWithValue("@startDate", startDate);
                            if (kodas.HasValue)
                            {
                                command.Parameters.AddWithValue("@kodas", kodas.Value);
                            }
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                adapter.Fill(dataTable);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptionLogger.LogException(
                        source: "GetExcelFile",
                        message: ex.ToString(),
                        stackTrace: ex.StackTrace
                    );
                    return StatusCode(500, "Error executing SQL query: " + ex.Message);
                }
                
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                try
                {
                   using (var package = new ExcelPackage())
                    {
                        string name = "";
                        if (formType.HasValue && formType == 1)
                        {
                            name = "Nurašymui ";
                        }
                        else if (formType.HasValue && formType == 2)
                        {
                            name = "isigijimui ";
                        }
                        else if (formType.HasValue && formType == 3)
                        {
                            name = "Nurašymas ";
                        }

                        var worksheet = package.Workbook.Worksheets.Add(name + startDate.ToString("yyyy-MM"));
                        for (int col = 0; col < dataTable.Columns.Count; col++)
                        {
                            worksheet.Cells[1, col + 1].Value = dataTable.Columns[col].ColumnName;
                        }
                        
                        for (int row = 0; row < dataTable.Rows.Count; row++)
                        {
                            for (int col = 0; col < dataTable.Columns.Count; col++)
                            {
                                worksheet.Cells[row + 2, col + 1].Value = dataTable.Rows[row][col];
                            }
                        }
                        
                        if (formType == 2)
                        {
                            worksheet.Row(1).Style.Font.Size = 8;
                            worksheet.Cells[1, 1, 1, 12].Style.WrapText = true;
                            worksheet.Row(1).Height = 50;
                            worksheet.Row(1).CustomHeight = true;
                            worksheet.Cells[1, 1, 1, 12].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            worksheet.Cells[1, 1, 1, 12].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        }
                        else if (formType == 3)
                            {


                                worksheet.Cells["A1"].Value = "DEBETAS";

                                worksheet.Cells["A1:N1"].Merge = true;
                                worksheet.Cells["A1:N1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                worksheet.Cells["A1:N1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                                worksheet.Row(1).Height = 20;
                                worksheet.Row(1).CustomHeight = true;

                                var debetasHeaders = new string[]
                                {
                                    "Account Number",
                                    "Amount",
                                    "Explanation -Remark-",
                                    "Co",
                                    "Sub Type",
                                    "Sub-ledger",
                                    "Ledger Type",
                                    "B C",
                                    "P C",
                                    "Units",
                                    "UM",
                                    "PO Doc Co",
                                    "PO Doc Type",
                                    "Purchase Order",
                                    "Subledger Description"
                                };

                                for (int i = 0; i < debetasHeaders.Length; i++)
                                {
                                    worksheet.Cells[2, i + 1].Value = debetasHeaders[i];
                                }
                                worksheet.Cells[2, 1, 2, debetasHeaders.Length].Style.Font.Size = 8;
                                worksheet.Cells[2, 1, 2, debetasHeaders.Length].Style.WrapText = true;
                                worksheet.Cells[2, 1, 2, debetasHeaders.Length].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                worksheet.Cells[2, 1, 2, debetasHeaders.Length].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                               
                                int currentRow = 3;

                                for (int r = 0; r < dataTable.Rows.Count; r++)
                                {
                                    
                                    for (int c = 0; c < dataTable.Columns.Count; c++)
                                    {
                                        worksheet.Cells[currentRow, c + 1].Value = dataTable.Rows[r][c];
                                    }
                                    
                                    worksheet.Cells[currentRow, 1].Value = " LT1220.62800.200"; 

                                    currentRow++;
                                }
                                

                                currentRow++;

                                worksheet.Cells[currentRow, 1].Value = "KREDITAS";
                                worksheet.Cells[currentRow, 1, currentRow, debetasHeaders.Length].Merge = true;
                                worksheet.Cells[currentRow, 1, currentRow, debetasHeaders.Length].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                worksheet.Cells[currentRow, 1, currentRow, debetasHeaders.Length].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                                worksheet.Row(currentRow).Height = 20;
                                worksheet.Row(currentRow).CustomHeight = true;
                                currentRow++;

                                for (int i = 0; i < debetasHeaders.Length; i++)
                                {
                                    worksheet.Cells[currentRow, i + 1].Value = debetasHeaders[i];
                                }
                                worksheet.Cells[currentRow, 1, currentRow, debetasHeaders.Length].Style.Font.Size = 8;
                                worksheet.Cells[currentRow, 1, currentRow, debetasHeaders.Length].Style.WrapText = true;
                                worksheet.Cells[currentRow, 1, currentRow, debetasHeaders.Length].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                worksheet.Cells[currentRow, 1, currentRow, debetasHeaders.Length].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                                currentRow++;


                                currentRow += 2; 

                                for (int r = 0; r < dataTable.Rows.Count; r++)
                                {
                                    for (int c = 0; c < dataTable.Columns.Count; c++)
                                    {
                                        worksheet.Cells[currentRow, c + 1].Value = dataTable.Rows[r][c];
                                    }

                                    worksheet.Cells[currentRow, 1].Value = "LTBS.31000.210";


                                    currentRow++;
                                }

                            }

                        if (worksheet.Dimension != null)
                        {
                            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                        }

                        var excelBytes = package.GetAsByteArray();
                        return File(
                            excelBytes,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "CarUsage.xlsx"
                        );
                    }

                }
                catch (Exception ex)
                {
                     exceptionLogger.LogException(
                        source: "GetExcelFile",
                        message: ex.ToString(),
                        stackTrace: ex.StackTrace
                    );
                    return StatusCode(500, "Error generating Excel file: " + ex.Message);
                }
            }





    

        
        // [HttpPost("file-upload-all")]
        // [Authorize]
        // public async Task<IActionResult> UploadExcelFileAll(IFormFile excelFile)
        // {
        //     if (excelFile == null || excelFile.Length == 0)
        //     {
        //         return BadRequest(new { message = "Nebuvo įkeltas failas" });
        //     }

        //     try
        //     {

        //         var usageDataTable = new DataTable();

        //         using (var stream = excelFile.OpenReadStream())
        //         {
        //             ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //             using (var package = new ExcelPackage(stream))
        //             {
        //                 var worksheet = package.Workbook.Worksheets[0];
        //                 int rowCount = worksheet.Dimension.Rows;
        //                 int colCount = worksheet.Dimension.Columns;

        //                 // Find the header row (assuming the header row has "Data" in the first column)
        //                 int headerRow = 1;
        //                 for (int row = 1; row <= rowCount; row++)
        //                 {
        //                     var firstCellValue = worksheet.Cells[row, 1].Text.Trim();
        //                     if (!string.IsNullOrEmpty(firstCellValue) && firstCellValue == "Data")
        //                     {
        //                         headerRow = row;
        //                         break;
        //                     }
        //                 }
        //                 Console.WriteLine($"Header row detected at: {headerRow}");

        //                 // Add columns to DataTable.
        //                 for (int col = 1; col <= colCount; col++)
        //                 {
        //                     var columnName = worksheet.Cells[headerRow, col].Text.Trim();
        //                     if (!string.IsNullOrEmpty(columnName))
        //                     {
        //                         if (!usageDataTable.Columns.Contains(columnName))
        //                         {
        //                             usageDataTable.Columns.Add(columnName);
        //                         }
        //                     }
        //                     else
        //                     {
        //                         Console.WriteLine($"Skipped empty column at index {col}");
        //                     }
        //                 }

        //                 // Add rows to DataTable.
        //                 for (int row = headerRow + 1; row <= rowCount; row++)
        //                 {
        //                     var rowData = usageDataTable.NewRow();
        //                     bool isRowEmpty = true;
        //                     for (int col = 1; col <= colCount; col++)
        //                     {
        //                         var cellValue = worksheet.Cells[row, col].Text.Trim();
        //                         if (!string.IsNullOrEmpty(cellValue) && col - 1 < usageDataTable.Columns.Count)
        //                         {
        //                             rowData[col - 1] = cellValue;
        //                             isRowEmpty = false;
        //                         }
        //                     }
        //                     if (!isRowEmpty)
        //                     {
        //                         usageDataTable.Rows.Add(rowData);
        //                     }
        //                 }
        //             }
        //         }
        //         var rowsToInsert = new List<(DataRow row, int? cardId, int? fuelType)>();

        //         using (var connection = _connectionProvider.GetConnection())
        //         {
        //             connection.Open();

        //             foreach (DataRow row in usageDataTable.Rows)
        //             {
        //                 // Ensure mandatory columns exist.
        //                 if (!usageDataTable.Columns.Contains("Data") ||
        //                     !usageDataTable.Columns.Contains("Kiekis"))
        //                 {
        //                     Console.WriteLine("Mandatory columns missing in Excel file.");
        //                     return BadRequest(new { message = "Privalomi stulpeliai trūksta Excel faile." });
        //                 }

        //                 // Retrieve the card number if available.
        //                 string cardNumber = "";
        //                 if (usageDataTable.Columns.Contains("Kortelės numeris"))
        //                 {
        //                     cardNumber = row["Kortelės numeris"].ToString();
        //                 }

        //                 int? cardId = null;
        //                 int? fuelType = null;
        //                 if (!string.IsNullOrEmpty(cardNumber))
        //                 {
        //                     // Query the card table for this card number.
        //                     var cardQuery = "SELECT FCA_ID, FCA_FUEL_TYPE FROM CARDS WHERE FCA_NUMBER = @CardNumber";
        //                     using (var cardCommand = new SqlCommand(cardQuery, connection))
        //                     {
        //                         cardCommand.Parameters.AddWithValue("@CardNumber", cardNumber);
        //                         using (var reader = await cardCommand.ExecuteReaderAsync())
        //                         {
        //                             if (reader.Read())
        //                             {
        //                                 cardId = reader.GetInt32(0);
        //                                 fuelType = reader.GetInt32(1);
        //                             }
        //                         }
        //                     }
        //                 }
        //                 // Even if cardId remains null, add the row for insertion.
        //                 rowsToInsert.Add((row, cardId, fuelType));
        //             }
        //         }

        //         int insertedCount = 0;
        //         using (var connection = _connectionProvider.GetConnection())
        //         {
        //             connection.Open();
        //             foreach (var (row, cardId, fuelType) in rowsToInsert)
        //             {
        //                 var usageQuery = @"
        //                     INSERT INTO CARDS_USAGE (
        //                         FCU_FCA_ID, FCU_DATE, FCU_FUEL_TYPE, FCU_CHECK_NUMBER,
        //                         FCU_QTY, FCU_PRICE_PER_UNIT, FCU_AMOUNT_PVM_NO_DISCOUNT,
        //                         FCU_PVM, FCU_AMOUNT_NO_PVM_NO_DISCOUNT, FCU_DISCOUNT,
        //                         FCU_TOTAL_AMOUNT, FCU_FUELING_PLACE_NAME, FCU_COUNTRY
        //                     ) 
        //                     VALUES (
        //                         @CardId, @UsageDate, @FuelType, @CheckNumber,
        //                         @Quantity, @PricePerUnit, @PricePVMNoDiscount,
        //                         @PVM, @PriceNoPvmNoDiscount, @Discount,
        //                         @TotalAmount, @Place, @Country
        //                     )";
        //                 using (var usageCommand = new SqlCommand(usageQuery, connection))
        //                 {
        //                     usageCommand.Parameters.AddWithValue("@CardId", cardId.HasValue ? (object)cardId.Value : DBNull.Value);
        //                     usageCommand.Parameters.AddWithValue("@UsageDate", DateTime.Parse(row["Data"].ToString()));
        //                     usageCommand.Parameters.AddWithValue("@FuelType", fuelType.HasValue ? (object)fuelType.Value : DBNull.Value);
        //                     usageCommand.Parameters.AddWithValue("@Quantity", Convert.ToDecimal(row["Kiekis"]));
        //                     usageCommand.Parameters.AddWithValue("@PricePerUnit", Convert.ToDecimal(row["Vnt. kaina"]));
        //                     usageCommand.Parameters.AddWithValue("@PricePVMNoDiscount", Convert.ToDecimal(row["Suma su PVM (be nuolaidos)"]));
        //                     usageCommand.Parameters.AddWithValue("@PVM", Convert.ToDecimal(row["PVM"]));
        //                     usageCommand.Parameters.AddWithValue("@PriceNoPvmNoDiscount", Convert.ToDecimal(row["Suma, be PVM"]));
        //                     usageCommand.Parameters.AddWithValue("@Discount", Convert.ToDecimal(row["Nuolaida"]));
        //                     usageCommand.Parameters.AddWithValue("@CheckNumber", row["Kvito Nr."].ToString());
        //                     usageCommand.Parameters.AddWithValue("@TotalAmount", Convert.ToDecimal(row["Iš viso su PVM (su nuolaida)"]));
        //                     usageCommand.Parameters.AddWithValue("@Place", row["Degalinė"].ToString());
        //                     usageCommand.Parameters.AddWithValue("@Country", row["Šalis"].ToString());

        //                     await usageCommand.ExecuteNonQueryAsync();
        //                     insertedCount++;
        //                 }
        //             }
        //         }

        //         string responseMessage = $"Sekmingai įkelta {insertedCount} įrašų.";
        //         return Ok(new { message = responseMessage });
        //     }
        //     catch (Exception ex)
        //     {
        //         exceptionLogger.LogException(
        //             source: "UploadExcelFileAll",
        //             message: ex.Message,
        //             stackTrace: ex.StackTrace
        //         );

        //         return StatusCode(500, new { message = "Nepavyko pridėti failo", details = ex.Message });
        //     }
        // }

        
        
}
}







