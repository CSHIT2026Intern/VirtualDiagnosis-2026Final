using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace VirtualPatDiagnosis.Controllers
{
    public class PythonController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public PythonController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> Transcribe(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
                return BadRequest("No file uploaded.");

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(audioFile.FileName);
            var filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            string pythonExe = @"python";
            string scriptPath = Path.Combine("Python", "transcribe.py");

            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\" \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            var output = await process!.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
                return Content($"Error: {error}");

            return Content(output.Trim());
        }

        [HttpPost]
        public async Task<IActionResult> TTS(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest("請輸入要轉換的文字。");

            var outputFileName = Guid.NewGuid().ToString() + ".mp3";
            var outputPath = Path.Combine(_env.WebRootPath, "tts", outputFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            string pythonExe = @"python";
            string scriptPath = Path.Combine("Python", "tts.py");

            // 確保文字正確處理引號與空格
            string safeText = text.Replace("\"", "\\\"");

            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\" \"{safeText}\" \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                var process = Process.Start(psi);
                if (process == null)
                    return StatusCode(500, "TTS 處理無法啟動。");

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(error))
                {
                    return StatusCode(500, $"TTS 轉換時發生錯誤：{error}");
                }

                var relativeUrl = "/tts/" + outputFileName;
                return Json(new
                {
                    success = true,
                    message = "TTS 轉換成功。",
                    audioUrl = relativeUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"系統例外錯誤：{ex.Message}");
            }
        }
    }
 }
