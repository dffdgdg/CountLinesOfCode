using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Document = DocumentFormat.OpenXml.Wordprocessing.Document;
class Program
{
    static void Main(string[] args)
    {
        string[] fileTypes = { "*.cs", "*.xaml" };
        int totalLines = 0;
        using WordprocessingDocument wordDocument = WordprocessingDocument.Create("CounterMega.docx", WordprocessingDocumentType.Document);
        MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
        mainPart.Document = new Document();
        Body body = mainPart.Document.AppendChild(new Body());
        body.AppendChild(new ParagraphProperties(new ParagraphStyleId() { Val = "Normal" }));
        body.AppendChild(new RunProperties(new RunFonts() { Ascii = "Times New Roman" }, new FontSize() { Val = "24" }));
        foreach (string fileType in fileTypes)
        {
            string[] files = Directory.GetFiles(Environment.CurrentDirectory, fileType, SearchOption.AllDirectories);
            foreach (string file in files)
            {
                int lines = 0;
                Paragraph header = body.AppendChild(new Paragraph());
                header.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading1" });
                Run headerRun = header.AppendChild(new Run());
                headerRun.AppendChild(new Text("Класс: " + Path.GetFileName(file)));
                headerRun.RunProperties = new RunProperties(new RunFonts() { Ascii = "Times New Roman" }, new FontSize() { Val = "28" }, new Bold());
                using StreamReader reader = new(file);
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().Trim();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        lines++;
                        Paragraph para = body.AppendChild(new Paragraph());
                        para.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Normal" });
                        Run run = para.AppendChild(new Run());
                        run.AppendChild(new Text(line));
                        run.RunProperties = new RunProperties(new RunFonts() { Ascii = "Times New Roman" }, new FontSize() { Val = "24" });
                    }
                }
                Console.WriteLine("{0}: {1} lines of code", file, lines);
                totalLines += lines;
            }
        }
        Console.WriteLine("Total lines of code: {0}", totalLines);
        wordDocument.Save();
        wordDocument.Close();
        Console.WriteLine("Готово");
        Console.ReadLine();
    }
}