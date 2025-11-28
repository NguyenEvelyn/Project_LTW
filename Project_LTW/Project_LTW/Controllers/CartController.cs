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

        [HttpPost]
        public ActionResult AddtoCart(string sanPhamID, string mau, string size)
        {
            if (string.IsNullOrEmpty(sanPhamID))
            {
                return RedirectToAction("Index", "Home");
            }

           
            if (string.IsNullOrEmpty(mau)) mau = "Mặc định";
         
            if (string.IsNullOrEmpty(size)) size = "FreeSize";
           

            var cart = GetCart();
            cart.Them(sanPhamID, mau, size);
            Session["Cart"] = cart;

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng thành công!";
            return RedirectToAction("Index");
        }

        // 3. Cập nhật số lượng (Tăng/Giảm)

        public ActionResult UpdateSLCart(string id, string mau, string size, int type)
        {
            var cart = GetCart();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(mau) || string.IsNullOrEmpty(size))
            {
                return RedirectToAction("Index");
            }

            if (type == 1) // Tăng số lượng
            {
               
                cart.Them(id, mau, size);
            }
            else // Giảm số lượng
            {
                
                cart.Giam(id, mau, size);
            }
            Session["Cart"] = cart; 
            return RedirectToAction("Index");
        }

        // 4. Xóa sản phẩm
 
        public ActionResult RemoveFromCart(string id, string mau, string size)
        {
            var cart = GetCart();

            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(mau) && !string.IsNullOrEmpty(size))
            {
               
                cart.Xoa(id, mau, size);
                Session["Cart"] = cart; 
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
            
            return RedirectToAction("Index", "Payment");
        }

        // 7. Thêm vào giỏ hàng 

        public ActionResult ThemGioHang(string id, string mau, string size, string type = "normal")
        {
           
            if (string.IsNullOrEmpty(mau)) mau = "Mặc định";
            if (string.IsNullOrEmpty(size)) size = "FreeSize";
           
            var cart = GetCart();
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