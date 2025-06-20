using System.Diagnostics;

namespace QQJob.Helper
{
    public class ConversionHelper
    {
        public static IFormFile ConvertWordToPDF(IFormFile file)
        {
            if(file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(),"wwwroot","uploads");
            Directory.CreateDirectory(uploadsFolder);

            var originalFilePath = Path.Combine(uploadsFolder,file.FileName);
            var pdfFileName = Path.GetFileNameWithoutExtension(file.FileName) + ".pdf";
            var pdfFilePath = Path.Combine(uploadsFolder,pdfFileName);

            // Save uploaded DOCX
            using(var stream = new FileStream(originalFilePath,FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // Convert DOCX → PDF using LibreOffice
            var libreOfficePath = @"C:\Program Files\LibreOffice\program\soffice.exe"; // adjust path
            var process = new Process();
            process.StartInfo.FileName = libreOfficePath;
            process.StartInfo.Arguments = $"--headless --convert-to pdf \"{originalFilePath}\" --outdir \"{uploadsFolder}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            process.WaitForExit();

            if(!File.Exists(pdfFilePath))
            {
                throw new Exception("Failed to convert DOCX to PDF.");
            }

            // Read PDF back into stream
            var pdfBytes = File.ReadAllBytes(pdfFilePath);
            var pdfStream = new MemoryStream(pdfBytes);

            // Create FormFile to return
            IFormFile pdfFormFile = new FormFile(pdfStream,0,pdfStream.Length,"file",pdfFileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            return pdfFormFile;
        }

    }
}
