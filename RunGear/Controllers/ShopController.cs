// ============================================================
// SHOP CONTROLLER
// File       : Controllers/ShopController.cs
// Handles    : Module 5 (Index), 6 (Cart), 7 (Checkout/PlaceOrder), 8 (OrderConfirmation)
// Tech Stack : ASP.NET Core MVC | C# | Entity Framework | MSSQL Stored Procedures
// ============================================================
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using RunGear.Models;

namespace RunGear.Controllers
{
    public class ShopController : Controller
    {
        private readonly string _connStr;

        public ShopController(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("RunGearDB")!;
        }


        // ═══════════════════════════════════════════════════════════════════
        // MODULE 5 – PRODUCTS & SERVICES
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult Index(
            List<string>? categories = null,
            List<string>? brands = null,
            List<string>? sizes = null,
            decimal maxPrice = 10000,
            string sortBy = "relevance")
        {
            var products = new List<ProductViewModel>();

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("sp_GetProducts", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Categories",
                    categories != null && categories.Any()
                        ? string.Join(",", categories) : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Brands",
                    brands != null && brands.Any()
                        ? string.Join(",", brands) : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Sizes",
                    sizes != null && sizes.Any()
                        ? string.Join(",", sizes) : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@MaxPrice", maxPrice);
                cmd.Parameters.AddWithValue("@SortBy", sortBy ?? "relevance");

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new ProductViewModel
                        {
                            ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl")),
                            Category = reader.GetString(reader.GetOrdinal("Category")),
                            Brand = reader.GetString(reader.GetOrdinal("Brand")),
                            Size = reader.GetString(reader.GetOrdinal("Size")),
                            Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                            OriginalPrice = reader.IsDBNull(reader.GetOrdinal("OriginalPrice"))
                                            ? (decimal?)null
                                            : reader.GetDecimal(reader.GetOrdinal("OriginalPrice")),
                            Rating = reader.GetDouble(reader.GetOrdinal("Rating")),
                            ReviewCount = reader.GetInt32(reader.GetOrdinal("ReviewCount")),
                            IsInStock = reader.GetBoolean(reader.GetOrdinal("IsInStock")),
                            IsNew = reader.GetBoolean(reader.GetOrdinal("IsNew")),
                        });
                    }
                }
            }

            ViewBag.Categories = new List<string> { "Running Shoes", "Apparel", "Accessories" };
            ViewBag.SelectedCategories = categories ?? new List<string>();
            ViewBag.BrandCounts = new Dictionary<string, int> { { "Nike", 24 }, { "Adidas", 18 }, { "On Running", 9 } };
            ViewBag.SelectedBrands = brands ?? new List<string>();
            ViewBag.SelectedSizes = sizes ?? new List<string>();
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortBy = sortBy;
            ViewBag.CategoryTitle = "Running Gear";
            ViewBag.CartCount = GetCartCount();

            return View(products);
        }


        // ═══════════════════════════════════════════════════════════════════
        // MODULE 5 – ADD TO CART
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            int memberId = GetCurrentMemberId();

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("sp_AddToCart", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MemberId", memberId);
                cmd.Parameters.AddWithValue("@ProductId", productId);
                cmd.Parameters.AddWithValue("@Quantity", quantity);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "Item added to cart!";
            return RedirectToAction("Index");
        }


        // ═══════════════════════════════════════════════════════════════════
        // MODULE 6 – SHOPPING CART
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult Cart()
        {
            int memberId = GetCurrentMemberId();
            var vm = new CartViewModel();

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("sp_GetCartByMember", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MemberId", memberId);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        vm.Items.Add(new CartItemViewModel
                        {
                            CartItemId = reader.GetInt32(reader.GetOrdinal("CartItemId")),
                            ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                            ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                            ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl")),
                            Size = reader.IsDBNull(reader.GetOrdinal("Size")) ? "" : reader.GetString(reader.GetOrdinal("Size")),
                            Color = reader.IsDBNull(reader.GetOrdinal("Color")) ? "" : reader.GetString(reader.GetOrdinal("Color")),
                            UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                            Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                            IsInStock = reader.GetBoolean(reader.GetOrdinal("IsInStock")),
                        });
                    }
                }
            }

            vm.Subtotal = vm.Items.Sum(i => i.UnitPrice * i.Quantity);
            vm.ShippingFee = vm.Items.Any() ? 150m : 0m;
            vm.Discount = (decimal)(HttpContext.Session.GetString("Discount") != null
                             ? decimal.Parse(HttpContext.Session.GetString("Discount")!) : 0m);
            vm.AppliedPromoCode = HttpContext.Session.GetString("PromoCode");
            vm.EstimatedDeliveryDate = DateTime.Now.AddBusinessDays(5);

            ViewBag.CartCount = vm.Items.Sum(i => i.Quantity);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int cartItemId)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("sp_RemoveFromCart", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CartItemId", cartItemId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCartQty(int cartItemId, string action)
        {
            int change = action == "increase" ? 1 : -1;

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("sp_UpdateCartQty", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CartItemId", cartItemId);
                cmd.Parameters.AddWithValue("@Change", change);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyPromo(string promoCode)
        {
            decimal discount = 0m;
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("sp_ValidatePromo", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PromoCode", promoCode ?? "");
                var outParam = new SqlParameter("@DiscountAmount", SqlDbType.Decimal) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outParam);
                conn.Open();
                cmd.ExecuteNonQuery();
                discount = outParam.Value == DBNull.Value ? 0m : (decimal)outParam.Value;
            }

            if (discount > 0)
            {
                HttpContext.Session.SetString("PromoCode", promoCode);
                HttpContext.Session.SetString("Discount", discount.ToString());
                TempData["SuccessMessage"] = $"Promo code applied! You saved ₱{discount:N0}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid or expired promo code.";
            }
            return RedirectToAction("Cart");
        }


        // ═══════════════════════════════════════════════════════════════════
        // MODULE 7 – CHECKOUT
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult Checkout()
        {
            int memberId = GetCurrentMemberId();
            var cartItems = GetCartItems(memberId);

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Cart");
            }

            var vm = new CheckoutViewModel
            {
                CartItems = cartItems,
                Subtotal = cartItems.Sum(i => i.UnitPrice * i.Quantity),
                ShippingFee = 150m,
                Discount = (decimal)(HttpContext.Session.GetString("Discount") != null
                              ? decimal.Parse(HttpContext.Session.GetString("Discount")!) : 0m),
            };

            ViewBag.CartCount = cartItems.Sum(i => i.Quantity);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder(CheckoutViewModel vm)
        {
            int memberId = GetCurrentMemberId();
            vm.CartItems = GetCartItems(memberId);
            vm.Subtotal = vm.CartItems.Sum(i => i.UnitPrice * i.Quantity);
            vm.ShippingFee = vm.DeliveryOption == "Express" ? 300m : 150m;
            vm.Discount = (decimal)(HttpContext.Session.GetString("Discount") != null
                            ? decimal.Parse(HttpContext.Session.GetString("Discount")!) : 0m);

            // ── Clear validation for non-form fields ──────────────────────
            ModelState.Remove("CartItems");
            ModelState.Remove("Subtotal");
            ModelState.Remove("ShippingFee");
            ModelState.Remove("Discount");
            ModelState.Remove("CardNumber");
            ModelState.Remove("CardExpiry");
            ModelState.Remove("CardCvv");

            // ── Card-specific validation ──────────────────────────────────
            if (vm.PaymentMethod == "Card")
            {
                if (string.IsNullOrWhiteSpace(vm.CardNumber))
                    ModelState.AddModelError("CardNumber", "Card number is required.");
                if (string.IsNullOrWhiteSpace(vm.CardExpiry))
                    ModelState.AddModelError("CardExpiry", "Expiry date is required.");
                if (string.IsNullOrWhiteSpace(vm.CardCvv))
                    ModelState.AddModelError("CardCvv", "CVV is required.");
            }

            if (!ModelState.IsValid)
            {
                // Debug: show exactly which fields are failing
                var errors = ModelState
                    .Where(x => x.Value!.Errors.Any())
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}")
                    .ToList();
                TempData["DebugErrors"] = string.Join(" | ", errors);

                ViewBag.CartCount = vm.CartItems.Sum(i => i.Quantity);
                return View("Checkout", vm);
            }

            // ── Call sp_PlaceOrder ────────────────────────────────────────
            string orderId;
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("sp_PlaceOrder", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MemberId", memberId);
                cmd.Parameters.AddWithValue("@FullName", vm.FullName);
                cmd.Parameters.AddWithValue("@Email", vm.Email);
                cmd.Parameters.AddWithValue("@Phone", vm.Phone);
                cmd.Parameters.AddWithValue("@Address", vm.Address);
                cmd.Parameters.AddWithValue("@City", vm.City);
                cmd.Parameters.AddWithValue("@PostalCode", vm.PostalCode);
                cmd.Parameters.AddWithValue("@DeliveryOption", vm.DeliveryOption);
                cmd.Parameters.AddWithValue("@PaymentMethod", vm.PaymentMethod);
                cmd.Parameters.AddWithValue("@Subtotal", vm.Subtotal);
                cmd.Parameters.AddWithValue("@ShippingFee", vm.ShippingFee);
                cmd.Parameters.AddWithValue("@Discount", vm.Discount);
                cmd.Parameters.AddWithValue("@Total", vm.Total);

                var orderIdOut = new SqlParameter("@OrderId", SqlDbType.NVarChar, 50)
                { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(orderIdOut);

                conn.Open();
                cmd.ExecuteNonQuery();
                orderId = orderIdOut.Value.ToString()!;
            }

            HttpContext.Session.Remove("Discount");
            HttpContext.Session.Remove("PromoCode");

            var confirmVm = new OrderConfirmationViewModel
            {
                OrderId = orderId,
                OrderStatus = "Placed",
                FullName = vm.FullName,
                Email = vm.Email,
                Phone = vm.Phone,
                Address = vm.Address,
                City = vm.City,
                PostalCode = vm.PostalCode,
                DeliveryOption = vm.DeliveryOption,
                EstimatedDeliveryDate = DateTime.Now.AddBusinessDays(vm.DeliveryOption == "Express" ? 2 : 5),
                OrderItems = vm.CartItems.Select(i => new OrderItemViewModel
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ImageUrl = i.ImageUrl,
                    Size = i.Size,
                    Color = i.Color,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                }).ToList(),
                Subtotal = vm.Subtotal,
                ShippingFee = vm.ShippingFee,
                Discount = vm.Discount,
            };

            TempData["OrderConfirmation"] = JsonSerializer.Serialize(confirmVm);
            return RedirectToAction("OrderConfirmation");
        }


        // ═══════════════════════════════════════════════════════════════════
        // MODULE 8 – ORDER CONFIRMATION
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult OrderConfirmation()
        {
            if (TempData["OrderConfirmation"] is not string json)
                return RedirectToAction("Index");

            var vm = JsonSerializer.Deserialize<OrderConfirmationViewModel>(json);
            if (vm == null)
                return RedirectToAction("Index");

            ViewBag.CartCount = 0;
            return View(vm);
        }

        [HttpGet]
        public IActionResult OrderDetail(string id)
        {
            return RedirectToAction("Index");
        }


        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════════════

        private int GetCurrentMemberId()
        {
            var val = HttpContext.Session.GetString("MemberId");
            return val != null ? int.Parse(val) : 0;
        }

        private int GetCartCount()
        {
            int memberId = GetCurrentMemberId();
            if (memberId == 0) return 0;

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("SELECT ISNULL(SUM(Quantity),0) FROM CartItems WHERE MemberId=@mid", conn))
            {
                cmd.Parameters.AddWithValue("@mid", memberId);
                conn.Open();
                return (int)cmd.ExecuteScalar()!;
            }
        }

        private List<CartItemViewModel> GetCartItems(int memberId)
        {
            var items = new List<CartItemViewModel>();
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("sp_GetCartByMember", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MemberId", memberId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new CartItemViewModel
                        {
                            CartItemId = reader.GetInt32(reader.GetOrdinal("CartItemId")),
                            ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                            ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                            ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl")),
                            Size = reader.IsDBNull(reader.GetOrdinal("Size")) ? "" : reader.GetString(reader.GetOrdinal("Size")),
                            Color = reader.IsDBNull(reader.GetOrdinal("Color")) ? "" : reader.GetString(reader.GetOrdinal("Color")),
                            UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                            Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                            IsInStock = reader.GetBoolean(reader.GetOrdinal("IsInStock")),
                        });
                    }
                }
            }
            return items;
        }
    }

    // ── Extension: AddBusinessDays ────────────────────────────────────────
    public static class DateTimeExtensions
    {
        public static DateTime AddBusinessDays(this DateTime date, int days)
        {
            int added = 0;
            while (added < days)
            {
                date = date.AddDays(1);
                if (date.DayOfWeek != DayOfWeek.Saturday &&
                    date.DayOfWeek != DayOfWeek.Sunday)
                    added++;
            }
            return date;
        }
    }
}