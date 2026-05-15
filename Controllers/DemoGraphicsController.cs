using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace sample_webapp_azure.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EcommerceController : ControllerBase
    {
        /// <summary>
        ///  private readonly IDistributedCache _cache;
        /// </summary>
        private readonly ILogger<EcommerceController> _logger;
        private readonly IConfiguration _configuration;

        // Simulated persistent storage (replace with Azure SQL/Cosmos DB)
        private static readonly List<Order> _orderDb = new();

        public EcommerceController(
            // IDistributedCache cache,
            ILogger<EcommerceController> logger,
            IConfiguration configuration)
        {
            // _cache = cache;
            _logger = logger;
            _configuration = configuration;
        }

        // ENDPOINT 1: Submit order (with validation & inventory check)
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitOrder([FromBody] OrderRequest request)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation("[{RequestId}] Order submission started for Product: {ProductId}", requestId, request.ProductId);

            // Validate required fields
            if (request.ProductId <= 0)
                return BadRequest("Valid ProductId is required");

            if (request.Quantity <= 0)
                return BadRequest("Quantity must be greater than 0");

            if (string.IsNullOrWhiteSpace(request.CustomerEmail))
                return BadRequest("Customer email is required");

            if (!request.CustomerEmail.Contains("@"))
                return BadRequest("Invalid email format");

            // Validate inventory (simulated - would check Azure SQL/Cosmos DB)
            var availableStock = await GetAvailableStockAsync(request.ProductId);
            if (availableStock < request.Quantity)
                return BadRequest($"Insufficient stock. Available: {availableStock}");

            // Calculate order total
            var productPrice = await GetProductPriceAsync(request.ProductId);
            var totalAmount = productPrice * request.Quantity;

            // Create order
            var order = new Order
            {
                OrderId = await GetNextOrderIdAsync(),
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CustomerEmail = request.CustomerEmail,
                ShippingAddress = request.ShippingAddress,
                TotalAmount = totalAmount,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                EstimatedDelivery = DateTime.UtcNow.AddDays(5)
            };

            // Store in persistent storage
            _orderDb.Add(order);

            // Cache for quick retrieval during spikes
            var cacheKey = $"order:{order.OrderId}";
            var serializedOrder = JsonSerializer.Serialize(order);
            //await _cache.SetStringAsync(cacheKey, serializedOrder, new DistributedCacheEntryOptions
            //{
            //    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            //});

            // Reduce inventory (simulated)
            await UpdateInventoryAsync(request.ProductId, request.Quantity);

            _logger.LogInformation("[{RequestId}] Order {OrderId} created successfully", requestId, order.OrderId);

            return Ok(new
            {
                Message = "Order submitted successfully",
                OrderId = order.OrderId,
                Status = order.Status,
                TotalAmount = totalAmount,
                EstimatedDelivery = order.EstimatedDelivery
            });
        }

        // ENDPOINT 2: Get order by ID (with cache-first strategy)
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            _logger.LogInformation("Fetching order: {OrderId}", orderId);

            // Check cache first (reduces DB pressure during traffic spikes)
            var cacheKey = $"order:{orderId}";
            //  var cachedOrder = await _cache.GetStringAsync(cacheKey);

            //if (!string.IsNullOrEmpty(cachedOrder))
            //{
            //    var order = JsonSerializer.Deserialize<Order>(cachedOrder);
            //    return Ok(order);
            //}

            // Fallback to database
            var record = _orderDb.FirstOrDefault(o => o.OrderId == orderId);
            if (record == null)
                return NotFound($"No order found for OrderId: {orderId}");

            // Refresh cache
            // await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(record));

            return Ok(record);
        }

        // ENDPOINT 3: Get all orders (with customer filtering for support/analytics)
        [HttpGet("all")]
        public IActionResult GetAllOrders([FromQuery] string? customerEmail = null)
        {
            var results = _orderDb.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                results = results.Where(o => o.CustomerEmail?.Equals(customerEmail, StringComparison.OrdinalIgnoreCase) == true);
            }

            // Add cache headers for better performance
            Response.Headers["X-Total-Count"] = results.Count().ToString();
            Response.Headers["Cache-Control"] = "public, max-age=60";

            return Ok(new
            {
                Total = results.Count(),
                Records = results.OrderByDescending(o => o.OrderDate),
                Summary = new
                {
                    TotalRevenue = results.Sum(o => o.TotalAmount),
                    AverageOrderValue = results.Any() ? results.Average(o => o.TotalAmount) : 0
                }
            });
        }

        // ENDPOINT 4: Get sales statistics (for dashboard & autoscaling decisions)
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            if (!_orderDb.Any())
                return Ok(new { Message = "No sales data available" });

            // Calculate real-time stats from today's data
            var today = DateTime.UtcNow.Date;
            var todayOrders = _orderDb.Where(o => o.OrderDate.Date == today).ToList();

            var stats = new
            {
                TotalOrders = _orderDb.Count,
                TotalRevenue = _orderDb.Sum(o => o.TotalAmount),
                TodayOrders = new
                {
                    Count = todayOrders.Count,
                    Revenue = todayOrders.Sum(o => o.TotalAmount)
                },
                OrderStatusBreakdown = new
                {
                    Pending = _orderDb.Count(o => o.Status == "Pending"),
                    Processing = _orderDb.Count(o => o.Status == "Processing"),
                    Shipped = _orderDb.Count(o => o.Status == "Shipped"),
                    Delivered = _orderDb.Count(o => o.Status == "Delivered"),
                    Cancelled = _orderDb.Count(o => o.Status == "Cancelled")
                },
                TopProducts = _orderDb
                    .GroupBy(o => o.ProductId)
                    .Select(g => new { ProductId = g.Key, QuantitySold = g.Sum(o => o.Quantity), Revenue = g.Sum(o => o.TotalAmount) })
                    .OrderByDescending(g => g.QuantitySold)
                    .Take(5),
                PeakHour = _orderDb
                    .GroupBy(o => o.OrderDate.Hour)
                    .Select(g => new { Hour = g.Key, Orders = g.Count() })
                    .OrderByDescending(g => g.Orders)
                    .FirstOrDefault(),
                // For autoscaling metrics
                TrafficLoad = new
                {
                    OrdersLastHour = _orderDb.Count(o => o.OrderDate >= DateTime.UtcNow.AddHours(-1)),
                    OrdersLast5Minutes = _orderDb.Count(o => o.OrderDate >= DateTime.UtcNow.AddMinutes(-5)),
                    CurrentThroughput = $"{_orderDb.Count(o => o.OrderDate >= DateTime.UtcNow.AddMinutes(-5)) / 5.0:F1} orders/min"
                }
            };

            // Add custom header for App Service autoscaling rules
            Response.Headers["X-Traffic-Load"] = stats.TrafficLoad.OrdersLast5Minutes.ToString();

            return Ok(stats);
        }

        // Helper methods (would connect to Azure SQL/Cosmos DB in production)
        private async Task<int> GetNextOrderIdAsync()
        {
            // Simulate getting next ID from database sequence
            await Task.Delay(1); // Simulate async operation
            return _orderDb.Count + 1;
        }

        private async Task<int> GetAvailableStockAsync(int productId)
        {
            // Simulate inventory check from Azure SQL/Cosmos DB
            await Task.Delay(1);
            return productId switch
            {
                1 => 100,  // Laptop
                2 => 500,  // Mouse
                3 => 50,   // Monitor
                _ => 1000
            };
        }

        private async Task<decimal> GetProductPriceAsync(int productId)
        {
            await Task.Delay(1);
            return productId switch
            {
                1 => 999.99m,  // Laptop
                2 => 29.99m,   // Mouse
                3 => 299.99m,  // Monitor
                _ => 49.99m
            };
        }

        private async Task UpdateInventoryAsync(int productId, int quantity)
        {
            // Simulate inventory update in persistent storage
            await Task.Delay(10);
            _logger.LogInformation("Inventory updated: Product {ProductId}, Reduced by {Quantity}", productId, quantity);
        }
    }

    // Model classes
    public class OrderRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
    }

    public class Order
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime EstimatedDelivery { get; set; }
    }
}