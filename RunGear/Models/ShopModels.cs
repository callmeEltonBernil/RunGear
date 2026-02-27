// ============================================================
// MODELS FOR ALL 4 FRONT-END MODULES
// Namespace  : RunGear.Models
// File       : ShopModels.cs
// ============================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RunGear.Models
{
    // ─────────────────────────────────────────────
    // MODULE 5 – PRODUCTS & SERVICES
    // ─────────────────────────────────────────────

    /// <summary>
    /// Represents a single product card on the product listing page.
    /// Populated from the stored procedure sp_GetProducts.
    /// </summary>
    public class ProductViewModel
    {
        public int    ProductId       { get; set; }
        public string Name            { get; set; }
        public string ImageUrl        { get; set; }
        public string Category        { get; set; }
        public string Brand           { get; set; }
        public string Size            { get; set; }   // CSV: "S,M,L,XL"
        public decimal Price          { get; set; }
        public decimal? OriginalPrice { get; set; }   // null if no discount
        public double Rating          { get; set; }   // 1.0 – 5.0
        public int    ReviewCount     { get; set; }
        public bool   IsInStock       { get; set; }
        public bool   IsNew           { get; set; }

        /// <summary>Returns the discount percent (e.g. 20 for 20%).</summary>
        public int DiscountPercent =>
            OriginalPrice.HasValue && OriginalPrice > 0
                ? (int)Math.Round((1 - Price / OriginalPrice.Value) * 100)
                : 0;
    }


    // ─────────────────────────────────────────────
    // MODULE 6 – PREVIEW BASKET / SHOPPING CART
    // ─────────────────────────────────────────────

    /// <summary>
    /// A single line item inside the shopping cart.
    /// </summary>
    public class CartItemViewModel
    {
        public int     CartItemId   { get; set; }
        public int     ProductId    { get; set; }
        public string  ProductName  { get; set; }
        public string  ImageUrl     { get; set; }
        public string  Size         { get; set; }
        public string  Color        { get; set; }
        public decimal UnitPrice    { get; set; }
        public int     Quantity     { get; set; }
        public bool    IsInStock    { get; set; }
    }

    /// <summary>
    /// Passed to Cart.cshtml. Contains all items + computed totals.
    /// </summary>
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();

        public decimal Subtotal     { get; set; }
        public decimal ShippingFee  { get; set; }
        public decimal Discount     { get; set; }
        public decimal Total        => Subtotal + ShippingFee - Discount;

        public string AppliedPromoCode       { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
    }


    // ─────────────────────────────────────────────
    // MODULE 7 – ORDER FORM / CHECKOUT
    // ─────────────────────────────────────────────

    /// <summary>
    /// Bound to the Checkout form (GET pre-fills, POST validates & submits).
    /// </summary>
    public class CheckoutViewModel
    {
        // Shipping Information
        [Required(ErrorMessage = "Full name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; }

        [Required(ErrorMessage = "Postal code is required.")]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        // Delivery & Payment
        [Required]
        public string DeliveryOption  { get; set; } = "Standard";  // "Standard" | "Express"

        [Required]
        public string PaymentMethod   { get; set; } = "Card";       // "Card" | "PayPal" | "COD"

        // Card fields (only validated server-side when PaymentMethod == "Card")
        public string CardNumber { get; set; }
        public string CardExpiry { get; set; }
        public string CardCvv    { get; set; }

        // Computed order totals (from session/cart)
        public List<CartItemViewModel> CartItems   { get; set; } = new List<CartItemViewModel>();
        public decimal Subtotal    { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount    { get; set; }
        public decimal Total       => Subtotal + ShippingFee - Discount;
    }


    // ─────────────────────────────────────────────
    // MODULE 8 – ORDER CONFIRMATION
    // ─────────────────────────────────────────────

    /// <summary>
    /// A single item displayed on the confirmation receipt.
    /// </summary>
    public class OrderItemViewModel
    {
        public int     ProductId    { get; set; }
        public string  ProductName  { get; set; }
        public string  ImageUrl     { get; set; }
        public string  Size         { get; set; }
        public string  Color        { get; set; }
        public decimal UnitPrice    { get; set; }
        public int     Quantity     { get; set; }
    }

    /// <summary>
    /// Passed to OrderConfirmation.cshtml after a successful PlaceOrder POST.
    /// </summary>
    public class OrderConfirmationViewModel
    {
        public string  OrderId       { get; set; }   // e.g. "RG-2024-001847"
        public string  OrderStatus   { get; set; } = "Placed"; // Placed | Processing | Shipped | Delivered

        // Shipping details (echoed from checkout)
        public string FullName   { get; set; }
        public string Email      { get; set; }
        public string Address    { get; set; }
        public string City       { get; set; }
        public string PostalCode { get; set; }
        public string Phone      { get; set; }

        public string DeliveryOption { get; set; }   // "Standard" | "Express"
        public DateTime EstimatedDeliveryDate { get; set; }

        // Items
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();

        // Totals
        public decimal Subtotal    { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount    { get; set; }
        public decimal Total       => Subtotal + ShippingFee - Discount;
    }
}
