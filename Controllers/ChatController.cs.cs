using ECommerceMarketplace.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMarketplace.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest(new { reply = "Message cannot be empty." });

            var msg = request.Message.ToLower().Trim();
            string reply;

            if (msg.Contains("hi") || msg.Contains("hello") || msg.Contains("مرحبا") || msg.Contains("أهلا"))
            {
                reply = "Hello! Welcome to our store. How can I help you today?";
            }
            else if (msg.Contains("product") || msg.Contains("item") || msg.Contains("منتج") || msg.Contains("سعر") || msg.Contains("price"))
            {
                var count = await _context.Products.CountAsync(p => !p.IsRemovedByAdmin);
                reply = $"We currently have {count} available products in our store. You can browse them on our catalog page!";
            }
            else if (msg.Contains("order") || msg.Contains("shipping") || msg.Contains("طلب") || msg.Contains("شحن") || msg.Contains("track"))
            {
                reply = "Orders are usually processed and shipped within 2-4 business days. You can track your order status in your Account profile.";
            }
            else if (msg.Contains("cart") || msg.Contains("pay") || msg.Contains("سلة") || msg.Contains("دفع") || msg.Contains("checkout"))
            {
                reply = "You can add items to your cart and proceed to Checkout safely using our secure payment options.";
            }
            else
            {
                reply = "Thank you for reaching out! For specific inquiries, feel free to browse our product catalog or check your order history in your profile.";
            }

            return Json(new { reply });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
}