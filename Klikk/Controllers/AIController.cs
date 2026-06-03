using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Klikk.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Klikk.Controllers
{
    public class AIController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;

        public AIController(
            IConfiguration config,
            ApplicationDbContext context)
        {
            _config = config;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            var apiKey = _config["Groq:ApiKey"];

            var products = await _context.Products
                .Include(p => p.Category)
                .Take(50)
                .ToListAsync();

            var inventory = string.Join("\n",
                products.Select(p =>
                    $"{p.Name} | ₱{p.Price} | Stock:{p.StockQuantity}"));

            var systemPrompt = $@"
You are Klikk Assistant.

You ONLY recommend products that exist below.

Available products:

{inventory}

Be helpful, concise and friendly.
";

            using var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var body = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = systemPrompt
                    },
                    new
                    {
                        role = "user",
                        content = request.Message
                    }
                }
            };

            var json = JsonSerializer.Serialize(body);

            var response = await client.PostAsync(
    "https://api.groq.com/openai/v1/chat/completions",
    new StringContent(json, Encoding.UTF8, "application/json"));

            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("GROQ ERROR: " + result);
                return StatusCode((int)response.StatusCode, result);
            }

            Console.WriteLine("========== GROQ ==========");
            Console.WriteLine(result);
            Console.WriteLine("==========================");

            return Content(
                result,
                "application/json");
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
    }
}