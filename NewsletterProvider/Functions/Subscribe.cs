using Data.Contexts;
using Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NewsletterProvider.Functions
{
    public class Subscribe
    {
        private readonly ILogger<Subscribe> _logger;
        private readonly DataContext _context;

        public Subscribe(ILogger<Subscribe> logger, DataContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Function("Subscribe")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                if (!string.IsNullOrEmpty(body))
                {
                    var subscribeEntity = JsonConvert.DeserializeObject<SubscribeEntity>(body);
                    if (subscribeEntity != null)
                    {
                        var existing = await _context.Subscribers.FirstOrDefaultAsync(x => x.Email == subscribeEntity.Email);
                        if (existing != null)
                        {
                            return new ConflictObjectResult(new { Status = 409, Message = "This email is already subscribed."});
                        }

                        _context.Subscribers.Add(subscribeEntity);
                        await _context.SaveChangesAsync();
                        return new OkObjectResult(new { Status = 200, Message = "This email is now subscribed." });
                    }
                    else
                    {
                        _logger.LogError("Invalid request");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: Subscribe.Run :: {ex.Message}");
            }
            
            return new BadRequestObjectResult(new {status = 400, Message = "Something went wrong, unable to subscribe."});
        }
    }
}
