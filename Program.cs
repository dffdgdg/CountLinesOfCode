using System.Text;
using System.Text.RegularExpressions;
using TextCopy;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

string projectPath = "C:\\Users\\laptop\\source\\repos\\Messenger\\MessengerAPI\\";

HashSet<string> targetExtensions = [".cs", ".axaml", ".xaml"];

HashSet<string> ignoredDirectories = ["Model","bin", "obj", ".git", ".vs", ".idea", "Assets", "Migrations"];

HashSet<string> ignoredFiles = ["GlobalUsings.cs", "AssemblyInfo.cs"];

Console.WriteLine($"Сканирование директории: {projectPath}");

var sb = new StringBuilder();

int fileCount = 0;

try
{
    ProcessDirectory(projectPath);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Ошибка: {ex.Message}");
    Console.ResetColor();
    return;
}

string finalOutput = sb.ToString();

var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
File.WriteAllText("context.txt", finalOutput, utf8WithBom);

try
{
    await ClipboardService.SetTextAsync(finalOutput);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\nГотово! Скопировано {fileCount} файлов ({finalOutput.Length} символов) в буфер обмена.");
    Console.WriteLine($"Также сохранено в context.txt");
    Console.ResetColor();
}
catch
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"Не удалось скопировать в буфер. Результат сохранён в context.txt");
    Console.ResetColor();
}

void ProcessDirectory(string targetDirectory)
{
    string[] files = Directory.GetFiles(targetDirectory);

    foreach (string fileName in files)
    {
        var fileInfo = new FileInfo(fileName);

        if (!targetExtensions.Contains(fileInfo.Extension.ToLower()))
            continue;

        if (ignoredFiles.Contains(fileInfo.Name) || fileInfo.Name.EndsWith(".Designer.cs"))
            continue;

        ProcessFile(fileInfo);
    }

    string[] subDirectories = Directory.GetDirectories(targetDirectory);
    foreach (string subDirectory in subDirectories)
    {
        var dirName = new DirectoryInfo(subDirectory).Name;

        if (ignoredDirectories.Contains(dirName))
            continue;

        ProcessDirectory(subDirectory);
    }
}

void ProcessFile(FileInfo file)
{
    try
    {
        string content = ReadFileWithEncoding(file.FullName);
        string relativePath = Path.GetRelativePath(projectPath, file.FullName);

        content = content.Trim('\uFEFF', '\u200B', '\uFFFE');

        if (file.Extension.ToLower() == ".cs")
        {
            content = CleanCSharp(content);
        }
        else if (file.Extension.ToLower() is ".axaml" or ".xaml")
        {
            content = CleanAxaml(content);
        }

        if (string.IsNullOrWhiteSpace(content)) return;

        sb.AppendLine($"--- FILE: {relativePath} ---");
        sb.AppendLine($"```{file.Extension.TrimStart('.')}");
        sb.AppendLine(content.Trim());
        sb.AppendLine("```");
        sb.AppendLine();

        Console.WriteLine($"[+] {relativePath}");
        fileCount++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[-] Ошибка чтения {file.Name}: {ex.Message}");
    }
}

/// <summary>
/// Читает файл, пытаясь определить кодировку автоматически.
/// Приоритет: UTF-8 с BOM → UTF-16 → UTF-8 без BOM → Windows-1251 (кириллица)
/// </summary>
string ReadFileWithEncoding(string filePath)
{
    byte[] bytes = File.ReadAllBytes(filePath);

    if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
    {
        return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
    }

    if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
    {
        return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
    }

    if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
    {
        return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
    }

    try
    {
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        return utf8.GetString(bytes);
    }
    catch
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1251).GetString(bytes);
    }
}

string CleanCSharp(string code)
{
    var lines = code.Split('\n'); // Универсальный сплит (работает и с \r\n)
    var cleanLines = new List<string>();

    foreach (var rawLine in lines)
    {
        var line = rawLine.TrimEnd('\r'); // Убираем возможный \r
        var trimmed = line.Trim();

        // Убираем using директивы (но не using statements вроде using var x = ...)
        if (Regex.IsMatch(trimmed, @"^using\s+[\w\.]+\s*;"))
            continue;

        if (Regex.IsMatch(trimmed, @"^using\s+static\s+[\w\.]+\s*;"))
            continue;

        if (Regex.IsMatch(trimmed, @"^using\s+\w+\s*=\s*[\w\.<>\[\],\s]+;"))
            continue;

        // Однострочные комментарии
        if (trimmed.StartsWith("//"))
            continue;

        // Пустые строки
        if (string.IsNullOrWhiteSpace(trimmed))
            continue;

        cleanLines.Add(line);
    }

    return string.Join(Environment.NewLine, cleanLines);
}

string CleanAxaml(string code)
{
    var lines = code.Split('\n');
    var cleanLines = lines.Select(l => l.TrimEnd('\r')).Where(l => !string.IsNullOrWhiteSpace(l));
    return string.Join(Environment.NewLine, cleanLines);
}