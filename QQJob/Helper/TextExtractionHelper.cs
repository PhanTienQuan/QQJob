namespace QQJob.Helper
{
    public class TextExtractionHelper
    {
        public static async Task<string> ExtractCvTextAsync(byte[] fileBytes,string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            if(ext == ".pdf")
            {
                return ExtractPdfText(fileBytes);
            }
            else if(ext == ".docx")
            {
                return ExtractWordText(fileBytes);
            }
            else if(ext == ".jpg" || ext == ".jpeg" || ext == ".png")
            {
                return await ExtractImageTextAsync(fileBytes);
            }
            else
            {
                throw new NotSupportedException($"Unsupported file type: {ext}");
            }
        }
        public static string ExtractPdfText(byte[] fileBytes)
        {
            using var stream = new MemoryStream(fileBytes);
            using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);

            var text = string.Join("\n",pdf.GetPages().Select(p => p.Text));

            return text;
        }
        public static string ExtractWordText(byte[] fileBytes)
        {
            using var stream = new MemoryStream(fileBytes);
            using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(stream,false);

            return doc.MainDocumentPart.Document.Body.InnerText;
        }
        public static async Task<string> ExtractImageTextAsync(byte[] fileBytes)
        {
            var temp = Path.GetTempFileName();
            await File.WriteAllBytesAsync(temp,fileBytes);
            using var engine = new Tesseract.TesseractEngine(@"./tessdata","eng",Tesseract.EngineMode.Default);
            using var img = Tesseract.Pix.LoadFromFile(temp);
            using var page = engine.Process(img);
            return page.GetText();
        }

    }
}
