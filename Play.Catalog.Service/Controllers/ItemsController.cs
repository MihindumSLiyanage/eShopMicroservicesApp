using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Common.Repositories;

namespace Play.Catalog.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<Item> _itemRepository;

        private static int requestCounter = 0;
        public ItemsController(IRepository<Item> itemRepository)
        {
            _itemRepository = itemRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            if (requestCounter <= 2)
            {
                Console.WriteLine($"Request {requestCounter}: Delaying ... ");
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
            if (requestCounter <= 4)
            {
                Console.WriteLine($"Request {requestCounter}: 500 (Internal Server Error).");
                return StatusCode(500);
            }
            var items = (await _itemRepository.GetAllAsync()).Select(item => item.AsDto());

            Console.WriteLine($"Request {requestCounter}: 200 (OK).");

            return Ok(items);
        }

        // GET /items/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await _itemRepository.GetAsync(id);

            if (item == null)
            {
                return NotFound();
            }
            return item.AsDto();
        }

        [HttpPost]
        public async Task<ActionResult<ItemDto>> PostAsync([FromBody] CreateItemDto createItemDto)
        {
            var item = new Item
            {
                Name = createItemDto.Name,
                Description = createItemDto.Description,
                Price = createItemDto.Price,
                CreatedDate = DateTimeOffset.UtcNow
            };

            await _itemRepository.CreateAsync(item);

            return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item.AsDto());
        }

        // PUT /items/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(Guid id, [FromBody] UpdateItemDto updateItemDto)
        {
            var existingItem = await _itemRepository.GetAsync(id);

            if (existingItem == null)
            {
                return NotFound();
            }

            existingItem.Name = updateItemDto.Name;
            existingItem.Description = updateItemDto.Description;
            existingItem.Price = updateItemDto.Price;

            await _itemRepository.UpdateAsync(existingItem);

            return NoContent();
        }

        // DELETE /items/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var item = await _itemRepository.GetAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            await _itemRepository.RemoveAsync(item.Id);

            return NoContent();
        }
    }
}
