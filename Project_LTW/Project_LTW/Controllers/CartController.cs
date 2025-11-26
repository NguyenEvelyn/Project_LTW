using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_LTW.Models;

namespace Project_LTW.Controllers
{
    public class CartController : Controller
    {
        // GET: Cart
        // 1. Hàm lấy giỏ hàng từ Session
        FashionWebEntities db = new FashionWebEntities();

        public Cart GetCart()
        {
            Cart cart = Session["Cart"] as Cart;
            if (cart == null || Session["Cart"] == null)
            {
                cart = new Cart();
                Session["Cart"] = cart;
            }
            return cart;
        }


        // 2. Thêm sản phẩm vào giỏ hàng
        // Đã SỬA: Thêm tham số mau và size, đặt giá trị mặc định để tránh lỗi khi không truyền
        [HttpPost]
        public ActionResult AddtoCart(string sanPhamID, string mau = "Default", string size = "S")
        {
            if (string.IsNullOrEmpty(sanPhamID))
            {
                return RedirectToAction("Index", "Home");
            }

            var cart = GetCart();
            // 🌟 TRUYỀN ĐỦ 3 THAM SỐ VÀO HÀM Them() 🌟
            cart.Them(sanPhamID, mau, size);
            Session["Cart"] = cart;

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng thành công!";
            return RedirectToAction("Index");
        }

        // 3. Cập nhật số lượng (Tăng/Giảm)
        // Đã SỬA: Thêm tham số mau và size để xác định chính xác CartItem
        public ActionResult UpdateSLCart(string id, string mau, string size, int type)
        {
            var cart = GetCart();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(mau) || string.IsNullOrEmpty(size))
            {
                return RedirectToAction("Index");
            }

            if (type == 1) // Tăng số lượng
            {
                // 🌟 TRUYỀN ĐỦ 3 THAM SỐ 🌟
                cart.Them(id, mau, size);
            }
            else // Giảm số lượng
            {
                // 🌟 TRUYỀN ĐỦ 3 THAM SỐ 🌟
                cart.Giam(id, mau, size);
            }
            Session["Cart"] = cart; // Cập nhật lại Session
            return RedirectToAction("Index");
        }

        // 4. Xóa sản phẩm
        // Đã SỬA: Thêm tham số mau và size để xác định chính xác CartItem
        public ActionResult RemoveFromCart(string id, string mau, string size)
        {
            var cart = GetCart();

            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(mau) && !string.IsNullOrEmpty(size))
            {
                // 🌟 TRUYỀN ĐỦ 3 THAM SỐ 🌟
                cart.Xoa(id, mau, size);
                Session["Cart"] = cart; // Cập nhật lại Session
                TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            }
            return RedirectToAction("Index");
        }

        // 5. Trang hiển thị giỏ hàng
        public ActionResult Index()
        {
            var cart = GetCart();
            if (cart.SoLuongMatHang() == 0)
            {
                ViewBag.ThongBao = "Giỏ hàng đang trống";
            }
            return View(cart);
        }

        // 6. Thanh toán
        public ActionResult PaymentConfirm()
        {
            // Logic chuyển hướng đến View "Index" của Controller "Payment"
            return RedirectToAction("Index", "Payment");
        }

        // 7. Thêm vào giỏ hàng (Hàm riêng cho Buynow)
        // Đã SỬA: Thêm tham số mau và size
        public ActionResult ThemGioHang(string id, string mau = "Default", string size = "S", string type = "normal")
        {
            var cart = GetCart();
            // 🌟 TRUYỀN ĐỦ 3 THAM SỐ VÀO HÀM Them() 🌟
            cart.Them(id, mau, size);
            Session["Cart"] = cart;

            if (type == "buynow")
            {
                return RedirectToAction("Index", "Payment");
            }

            return RedirectToAction("Index", "Cart");
        }
    }
}