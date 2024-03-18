using Microsoft.AspNetCore.Mvc;
using System.Text;
using WebWhisperer.IterativePromptCore.Parser;
using WebWhisperer.Services;

namespace WebWhisperer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
    }

    [Route("api/whisper")]
    [ApiController]
    public class WhisperController : ControllerBase
    {
        private readonly WhisperService _whisperService;

        public WhisperController(WhisperService whisperService)
        {
            _whisperService = whisperService;
        }

        [HttpPost]
        [Route("process")]
        public ActionResult<List<string>> ProcessInput([FromBody] string querySoFar)
        {
            if (!_whisperService.IsIntputFieldLoaded)
                return BadRequest("Input fields are not loaded");

            List<string> whisperText = _whisperService.ProcessInput(querySoFar).ToList();
            return Ok(whisperText);
        }

        [HttpPost]
        [Route("upload")]
        public ActionResult LoadUserInput([FromBody] string userInput)
        {
            _whisperService.LoadUserInput(userInput);
            return Ok();
        }

        [HttpGet]
        [Route("getCurrent")]
        public ActionResult GetCurrentTable()
        {
            // Call to get the current table data
            string csvData = _whisperService.GetCurrentTable();

            if (string.IsNullOrEmpty(csvData))
            {
                return NotFound();
            }

            return File(Encoding.UTF8.GetBytes(csvData), "text/plain; charset=UTF-8");
        }

        [HttpGet]
        [Route("startNewQuery")]
        public ActionResult StartNewQuery()
        {
            _whisperService.StartNewConversation();
            return Ok();
        }

        [HttpPost]
        [Route("uploadCsv")]
        public IActionResult UploadCsvFile([FromForm] IFormFile csvFile, [FromForm] string delimiter)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                return BadRequest("Invalid file.");
            }

            try
            {
                using (var fileStream = csvFile.OpenReadStream())
                {
                    var fields = CsvParser.ParseCsvFile(fileStream, delimiter);

                    _whisperService.LoadInputFields(fields);
                }

                return Ok("File uploaded and parsed successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message}");
            }
        }
    }
}