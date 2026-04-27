using OfficeOpenXml;
using OFUserControlApp.Application.Interfaces;
using OFUserControlApp.Domain.Entities;

namespace OFUserControlApp.Infrastructure.Services;

/// <summary>
/// Servicio para procesamiento de archivos Excel usando EPPlus
/// </summary>
public sealed class ExcelProcessorService : IExcelProcessorService
{
    private readonly ILogger<ExcelProcessorService> _logger;
    private static readonly string[] ValidExtensions = { ".xls", ".xlsx" };
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

    public ExcelProcessorService(ILogger<ExcelProcessorService> logger)
    {
        _logger = logger;
        // Configurar licencia para EPPlus 8 (versión no comercial)
        try
        {
            ExcelPackage.License.SetNonCommercialPersonal("OFUserControlApp");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo configurar la licencia de EPPlus: {Message}", ex.Message);
            // Continuar sin configuración de licencia - EPPlus funcionará en modo de evaluación
        }
    }

    public bool IsValidExcelFile(string fileName, long fileSize)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        if (fileSize <= 0 || fileSize > MaxFileSize)
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return ValidExtensions.Contains(extension);
    }

    public async Task<IEnumerable<UsuarioExcel>> ExtractUsersFromExcelAsync(Stream excelStream, string fileName)
    {
        try
        {
            _logger.LogInformation("Iniciando extracción de usuarios del archivo {FileName}", fileName);

            // Copiar el stream a un MemoryStream para evitar problemas de disposición
            using var memoryStream = new MemoryStream();
            await excelStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // Verificar si es el archivo HM_Listado_de_Usuarios o Listado_Usuarios
            if (fileName.Contains("HM_Listado_de_Usuarios") || fileName.Contains("Listado_Usuarios"))
            {
                _logger.LogInformation("Detectado archivo de listado de usuarios, usando procesamiento especializado");
                return await ExtractFromHMListadoAsync(memoryStream, fileName);
            }

            // Intentar primero con EPPlus
            try
            {
                _logger.LogInformation("Intentando procesar archivo {FileName} con EPPlus...", fileName);
                return await ExtractWithEPPlusAsync(memoryStream, fileName);
            }
            catch (Exception ex) when (ex.Message.Contains("not a valid Package file") || ex.Message.Contains("Package file"))
            {
                _logger.LogWarning("EPPlus no pudo procesar el archivo {FileName} como Excel estándar. Intentando método alternativo...", fileName);
                
                // Resetear la posición del stream para el método alternativo
                memoryStream.Position = 0;
                return await ExtractWithAlternativeMethodAsync(memoryStream, fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extrayendo usuarios del archivo {FileName}", fileName);
            throw new InvalidOperationException($"Error procesando archivo Excel: {ex.Message}", ex);
        }
    }

    private Task<IEnumerable<UsuarioExcel>> ExtractFromHMListadoAsync(MemoryStream memoryStream, string fileName)
    {
        using var package = new ExcelPackage(memoryStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        
        if (worksheet == null)
        {
            throw new InvalidOperationException("El archivo Excel no contiene hojas de trabajo");
        }

        _logger.LogInformation("Procesando archivo HM_Listado con estructura especial");

        var usuarios = new List<UsuarioExcel>();
        var rowCount = worksheet.Dimension?.Rows ?? 0;

        // Estructura específica del archivo HM_Listado_de_Usuarios:
        // - Fila 3: Títulos de columnas
        // - Fila 4+: Datos reales
        // - Columna A: Usuario (correo1)
        // - Columna B: Descripción (nombre de usuario)
        // - Columna F: Correo Electrónico (correo2)

        const int startRow = 4; // Los datos empiezan en la fila 4
        const int usuarioCol = 1; // Columna A
        const int descripcionCol = 2; // Columna B
        const int correoElectronicoCol = 6; // Columna F

        var filasProcesadas = 0;
        var filasValidas = 0;

        for (int row = startRow; row <= rowCount; row++)
        {
            filasProcesadas++;
            
            // Obtener valores de las columnas
            var usuarioVal = worksheet.Cells[row, usuarioCol].Value?.ToString()?.Trim();
            var correoElectronicoVal = worksheet.Cells[row, correoElectronicoCol].Value?.ToString()?.Trim();

            if (IsHeaderLikeRow(usuarioVal, correoElectronicoVal))
            {
                continue;
            }

            // Validar que tengamos al menos un identificador
            if (!string.IsNullOrWhiteSpace(usuarioVal) || !string.IsNullOrWhiteSpace(correoElectronicoVal))
            {
                filasValidas++;
                usuarios.Add(new UsuarioExcel
                {
                    // Prioridad según requerimiento: Columna A (Usuario) y Columna F (Correo)
                    Usuario = usuarioVal ?? string.Empty,
                    Correo = (correoElectronicoVal ?? string.Empty).ToLowerInvariant()
                });
            }
        }

        var usuariosUnicos = usuarios.DistinctBy(u => u.Usuario).ToList();

        _logger.LogInformation("Procesamiento HM_Listado completado: {Procesadas} filas procesadas, {Validas} válidas, {Unicos} usuarios únicos", 
            filasProcesadas, filasValidas, usuariosUnicos.Count);

        return Task.FromResult<IEnumerable<UsuarioExcel>>(usuariosUnicos);
    }

    private Task<IEnumerable<UsuarioExcel>> ExtractWithEPPlusAsync(MemoryStream memoryStream, string fileName)
    {
        using var package = new ExcelPackage(memoryStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        
        if (worksheet == null)
        {
            throw new InvalidOperationException("El archivo Excel no contiene hojas de trabajo");
        }

        _logger.LogInformation("Hoja encontrada: '{SheetName}' con {RowCount} filas y {ColumnCount} columnas", 
            worksheet.Name, worksheet.Dimension?.Rows ?? 0, worksheet.Dimension?.Columns ?? 0);

        var usuarios = new List<UsuarioExcel>();
        var rowCount = worksheet.Dimension?.Rows ?? 0;

        // Buscar encabezados en las primeras 3 filas
        var (usuarioCol, correoCol, startRow) = FindHeaderColumns(worksheet);

        if (usuarioCol == -1 || correoCol == -1)
        {
            // Si no encontramos las columnas esperadas, intentar detectar automáticamente
            _logger.LogWarning("No se encontraron las columnas 'Usuario' y 'Correo'. Intentando detección automática...");
            (usuarioCol, correoCol, startRow) = AutoDetectColumns(worksheet);
        }

        if (usuarioCol == -1 || correoCol == -1)
        {
            throw new InvalidOperationException("No se pudieron detectar las columnas de usuario/correo en el archivo Excel");
        }

        _logger.LogInformation("Columnas encontradas - Usuario: {UsuarioCol} ({ColName}), Correo: {CorreoCol} ({ColName}), Fila inicio: {StartRow}", 
            usuarioCol, worksheet.Cells[1, usuarioCol].Value?.ToString(), correoCol, worksheet.Cells[1, correoCol].Value?.ToString(), startRow);

        // Extraer usuarios
        var filasProcesadas = 0;
        var filasValidas = 0;
        var filasInvalidas = 0;

        for (int row = startRow; row <= rowCount; row++)
        {
            filasProcesadas++;
            var usuario = worksheet.Cells[row, usuarioCol].Value?.ToString()?.Trim();
            var correo = worksheet.Cells[row, correoCol].Value?.ToString()?.Trim();

            if (IsHeaderLikeRow(usuario, correo))
            {
                continue;
            }

            // Si no hay correo pero hay usuario, usar el usuario como correo
            if (string.IsNullOrWhiteSpace(correo) && !string.IsNullOrWhiteSpace(usuario))
            {
                correo = usuario;
            }

            // Validar que tengamos al menos un identificador
            if (!string.IsNullOrWhiteSpace(usuario) || !string.IsNullOrWhiteSpace(correo))
            {
                filasValidas++;
                usuarios.Add(new UsuarioExcel
                {
                    Usuario = usuario ?? string.Empty,
                    Correo = (correo ?? string.Empty).ToLowerInvariant()
                });
            }
            else
            {
                filasInvalidas++;
            }
        }

        var usuariosUnicos = usuarios.DistinctBy(u => u.Usuario).ToList();

        _logger.LogInformation("Procesamiento completado para {FileName}: {Procesadas} filas procesadas, {Validas} válidas, {Invalidas} inválidas, {Unicos} usuarios únicos", 
            fileName, filasProcesadas, filasValidas, filasInvalidas, usuariosUnicos.Count);

        return Task.FromResult<IEnumerable<UsuarioExcel>>(usuariosUnicos);
    }

    private Task<IEnumerable<UsuarioExcel>> ExtractWithAlternativeMethodAsync(MemoryStream memoryStream, string fileName)
    {
        _logger.LogInformation("Procesando archivo {FileName} con método alternativo para archivos Oracle Financials", fileName);

        // Leer el contenido como texto para detectar la estructura
        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        var content = reader.ReadToEnd();

        // Detectar si es un archivo HTML/XML (típico de Oracle Financials)
        if (content.Contains("<html") || content.Contains("<table") || content.Contains("<?xml"))
        {
            return ExtractFromHtmlXmlContentAsync(content, fileName);
        }

        // Si no es HTML/XML, intentar leerlo como texto delimitado (TSV/CSV)
        memoryStream.Position = 0;
        return ExtractWithDelimitedTextAsync(memoryStream, fileName);
    }

    private Task<IEnumerable<UsuarioExcel>> ExtractFromHtmlXmlContentAsync(string content, string fileName)
    {
        _logger.LogInformation("Extrayendo datos de contenido HTML/XML del archivo {FileName}", fileName);

        var usuarios = new List<UsuarioExcel>();

        // Buscar tablas en el contenido HTML/XML
        var tableMatches = System.Text.RegularExpressions.Regex.Matches(content, @"<table[^>]*>(.*?)</table>", 
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match tableMatch in tableMatches)
        {
            var tableContent = tableMatch.Value;
            
            // Buscar filas en la tabla
            var rowMatches = System.Text.RegularExpressions.Regex.Matches(tableContent, @"<tr[^>]*>(.*?)</tr>", 
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            var usuarioCol = -1;
            var correoCol = -1;
            var startRow = 0;

            // Buscar encabezados en las primeras filas
            for (int i = 0; i < Math.Min(5, rowMatches.Count); i++)
            {
                var rowContent = rowMatches[i].Value;
                var cellMatches = System.Text.RegularExpressions.Regex.Matches(rowContent, @"<t[dh][^>]*>(.*?)</t[dh]>", 
                    System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                for (int j = 0; j < cellMatches.Count; j++)
                {
                    var cellContent = System.Text.RegularExpressions.Regex.Replace(cellMatches[j].Groups[1].Value, @"<[^>]+", "").Trim().ToLowerInvariant();
                    
                    if (cellContent.Contains("usuario") || cellContent.Contains("user") || cellContent.Contains("login"))
                    {
                        usuarioCol = j;
                        startRow = i + 1;
                    }
                    else if ((cellContent.Contains("correo") || cellContent.Contains("email") || cellContent.Contains("mail")))
                    {
                        correoCol = j;
                        startRow = i + 1;
                    }
                }

                if (usuarioCol != -1 && correoCol != -1)
                    break;
            }

            if (usuarioCol != -1 && correoCol != -1)
            {
                // Extraer datos de las filas restantes
                for (int i = startRow; i < rowMatches.Count; i++)
                {
                    var rowContent = rowMatches[i].Value;
                    var cellMatches = System.Text.RegularExpressions.Regex.Matches(rowContent, @"<t[dh][^>]*>(.*?)</t[dh]>", 
                        System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    if (cellMatches.Count > Math.Max(usuarioCol, correoCol))
                    {
                        var usuario = System.Text.RegularExpressions.Regex.Replace(cellMatches[usuarioCol].Groups[1].Value, @"<[^>]+", "").Trim();
                        var correo = System.Text.RegularExpressions.Regex.Replace(cellMatches[correoCol].Groups[1].Value, @"<[^>]+", "").Trim();

                        if (!string.IsNullOrWhiteSpace(usuario) && !string.IsNullOrWhiteSpace(correo))
                        {
                            usuarios.Add(new UsuarioExcel
                            {
                                Usuario = usuario,
                                Correo = correo.ToLowerInvariant()
                            });
                        }
                    }
                }
            }
        }

        var usuariosUnicos = usuarios.DistinctBy(u => u.Usuario).ToList();
        _logger.LogInformation("Extracción HTML/XML completada: {Count} usuarios únicos encontrados", usuariosUnicos.Count);
        
        return Task.FromResult<IEnumerable<UsuarioExcel>>(usuariosUnicos);
    }

    private Task<IEnumerable<UsuarioExcel>> ExtractWithDelimitedTextAsync(MemoryStream memoryStream, string fileName)
    {
        _logger.LogInformation("Procesando archivo {FileName} como texto delimitado", fileName);

        var usuarios = new List<UsuarioExcel>();
        memoryStream.Position = 0;

        using var reader = new StreamReader(memoryStream);
        var lineas = new List<string>();
        string linea;
        while ((linea = reader.ReadLine()) != null)
        {
            lineas.Add(linea);
        }

        if (lineas.Count == 0)
            return Task.FromResult<IEnumerable<UsuarioExcel>>(usuarios);

        // Detectar delimitador (tabulación o coma)
        var delimitadores = new[] { '\t', ',', ';' };
        var delimitador = delimitadores.FirstOrDefault(d => lineas[0].Contains(d));

        // Buscar encabezados
        var usuarioCol = -1;
        var correoCol = -1;
        var startRow = 0;

        for (int i = 0; i < Math.Min(5, lineas.Count); i++)
        {
            var celdas = lineas[i].Split(delimitador);
            
            for (int j = 0; j < celdas.Length; j++)
            {
                var contenido = celdas[j].Trim().ToLowerInvariant();
                
                if (contenido.Contains("usuario") || contenido.Contains("user"))
                {
                    usuarioCol = j;
                    startRow = i + 1;
                }
                else if (contenido.Contains("correo") || contenido.Contains("email"))
                {
                    correoCol = j;
                    startRow = i + 1;
                }
            }

            if (usuarioCol != -1 && correoCol != -1)
                break;
        }

        // Si no encontramos encabezados, usar las primeras columnas
        if (usuarioCol == -1 && correoCol == -1 && lineas.Count > 0)
        {
            var primeraFila = lineas[0].Split(delimitador);
            if (primeraFila.Length >= 2)
            {
                usuarioCol = 0;
                correoCol = 1;
                startRow = 0;
            }
        }

        // Extraer datos
        for (int i = startRow; i < lineas.Count; i++)
        {
            var celdas = lineas[i].Split(delimitador);
            
            if (celdas.Length > Math.Max(usuarioCol, correoCol))
            {
                var usuario = celdas[usuarioCol].Trim();
                var correo = celdas[correoCol].Trim();

                if (!string.IsNullOrWhiteSpace(usuario) && !string.IsNullOrWhiteSpace(correo))
                {
                    usuarios.Add(new UsuarioExcel
                    {
                        Usuario = usuario,
                        Correo = correo.ToLowerInvariant()
                    });
                }
            }
        }

        var usuariosUnicos = usuarios.DistinctBy(u => u.Usuario).ToList();
        _logger.LogInformation("Procesamiento de texto delimitado completado: {Count} usuarios únicos", usuariosUnicos.Count);
        
        return Task.FromResult<IEnumerable<UsuarioExcel>>(usuariosUnicos);
    }

    private (int usuarioCol, int correoCol, int startRow) FindHeaderColumns(ExcelWorksheet worksheet)
    {
        var rowCount = Math.Min(10, worksheet.Dimension?.Rows ?? 0);
        var columnCount = worksheet.Dimension?.Columns ?? 0;

        for (int row = 1; row <= rowCount; row++)
        {
            int usuarioCol = -1;
            int correoCol = -1;

            for (int col = 1; col <= columnCount; col++)
            {
                var cellValue = worksheet.Cells[row, col].Value?.ToString()?.Trim().ToLowerInvariant();
                
                if (cellValue != null)
                {
                    if (cellValue.Contains("usuario") && usuarioCol == -1)
                    {
                        usuarioCol = col;
                    }
                    else if ((cellValue.Contains("correo") || cellValue.Contains("email")) && correoCol == -1)
                    {
                        correoCol = col;
                    }
                }
            }

            if (usuarioCol != -1 && correoCol != -1)
            {
                return (usuarioCol, correoCol, row + 1);
            }
        }

        return (-1, -1, 1);
    }

    private (int usuarioCol, int correoCol, int startRow) AutoDetectColumns(ExcelWorksheet worksheet)
    {
        var rowCount = Math.Min(20, worksheet.Dimension?.Rows ?? 0);
        var columnCount = Math.Min(10, worksheet.Dimension?.Columns ?? 0);

        // Buscar columnas que contengan datos de email/usuario
        for (int col = 1; col <= columnCount; col++)
        {
            var emailCount = 0;
            var textCount = 0;
            
            for (int row = 1; row <= Math.Min(10, rowCount); row++)
            {
                var cellValue = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                
                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    textCount++;
                    if (IsValidEmail(cellValue))
                    {
                        emailCount++;
                    }
                }
            }

            // Si encontramos una columna con emails, usarla
            if (emailCount > 0 && (double)emailCount / textCount > 0.5)
            {
                // Buscar la columna de usuario (normalmente a la izquierda del email)
                for (int usuarioCol = 1; usuarioCol < col; usuarioCol++)
                {
                    var usuarioCount = 0;
                    for (int row = 1; row <= Math.Min(10, rowCount); row++)
                    {
                        var cellValue = worksheet.Cells[row, usuarioCol].Value?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(cellValue) && cellValue.Length > 3)
                        {
                            usuarioCount++;
                        }
                    }
                    
                    if (usuarioCount > 0)
                    {
                        return (usuarioCol, col, 1);
                    }
                }

                // Si no hay columna de usuario, usar la misma columna para ambos
                return (col, col, 1);
            }
        }

        // Si no encontramos emails, usar las primeras dos columnas con datos
        for (int col = 1; col <= Math.Min(5, columnCount); col++)
        {
            var hasData = false;
            for (int row = 1; row <= Math.Min(5, rowCount); row++)
            {
                if (!string.IsNullOrWhiteSpace(worksheet.Cells[row, col].Value?.ToString()))
                {
                    hasData = true;
                    break;
                }
            }
            
            if (hasData)
            {
                // Usar esta columna para usuario y la siguiente para correo
                var nextCol = col + 1 <= columnCount ? col + 1 : col;
                return (col, nextCol, 1);
            }
        }

        return (-1, -1, 1);
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.ToLowerInvariant();
        }
        catch
        {
            return false;
        }
    }

    private static bool IsHeaderLikeRow(string? usuario, string? correo)
    {
        var normalizedUsuario = usuario?.Trim().ToLowerInvariant();
        var normalizedCorreo = correo?.Trim().ToLowerInvariant();

        return normalizedUsuario is "usuario" or "user" or "usuarios"
            || normalizedCorreo is "correo" or "correo electronico" or "correo electrónico" or "email" or "mail";
    }
}
