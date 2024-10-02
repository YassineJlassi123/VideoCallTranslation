using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VideoCallTranslation.Models;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using VideoCallTranslation.Data;
using Microsoft.EntityFrameworkCore;

namespace VideoCallTranslation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CohereService _cohereService;
        private readonly ApplicationDbContext _context;


        public HomeController(ApplicationDbContext context, CohereService cohereService, ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cohereService = cohereService;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult VideoCall()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromBody] string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return BadRequest("Password is required");
            }

            // Generate a unique RoomId
            var roomId = Guid.NewGuid().ToString();

            // Create a new VideoCallRoom object
            var videoCallRoom = new VideoCallRoom
            {
                RoomId = roomId,
                Password = password
            };

            // Save the room to the database
            _context.VideoCallRooms.Add(videoCallRoom);
            await _context.SaveChangesAsync();

            // Return the link for the room (replace with actual link generation)
            var roomLink = Url.Action("JoinRoom", "home", new { roomId }, protocol: HttpContext.Request.Scheme);
            return Ok(new { roomLink });
        }

        [HttpGet]
        public async Task<IActionResult> JoinRoom(string roomId)
        {
            // Find the room in the database by RoomId
            var room = await _context.VideoCallRooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null)
            {
                return NotFound("Room not found");
            }

            // Pass the room information to the view (for password entry)
            return View(room);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyRoom(string roomId, string password)
        {
            // Find the room by RoomId
            var room = await _context.VideoCallRooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null)
            {
                return NotFound("Room not found");
            }

            // Verify the password
            if (room.Password != password)
            {
                return BadRequest("Incorrect password");
            }

            // Password is correct, redirect to the video call
            return RedirectToAction("StartCall", new { roomId });
        }

        public IActionResult StartCall(string roomId)
        {
            // Video call logic starts here, roomId is available
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Command([FromBody] TranslationRequest request)
        {
            // Ensure the request contains valid text and language options
            if (request == null || string.IsNullOrWhiteSpace(request.Text) || string.IsNullOrWhiteSpace(request.SourceLang) || string.IsNullOrWhiteSpace(request.TargetLang))
            {
                return BadRequest("Invalid request. Ensure both text input and language options are provided.");
            }

            // Prepare the prompt with the necessary instructions
            var prompt = $@"
You are a language translation and correction expert. The user will provide input in one language, and you need to:
1. Correct any spelling or grammatical errors in the user's input.
2. Translate the corrected input from the user's source language to the target language.

Important:
- Do not include the original input in your response.
- Only return the final corrected and translated text.

User Input: '{request.Text}'
Source Language: {request.SourceLang}
Target Language: {request.TargetLang}";

            // Get completion from Cohere or any AI model service you are using
            var response = await _cohereService.GetChatResponseAsync(prompt);

            // Return the corrected and translated text
            return Ok(new { translatedText = response.Trim() });
        }
        [HttpGet]
        public IActionResult CreateRoom()
        {
            // Return a view to show the room creation form
            return View();
        }

        public class TranslationRequest
        {
            public string Text { get; set; }
            public string SourceLang { get; set; }
            public string TargetLang { get; set; }
        }
      
    }
}