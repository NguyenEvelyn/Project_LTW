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
        // 1. Hàm lấy giỏ hàng từ Session
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

        // 2. Thêm vào giỏ
        // Sửa tên tham số thành 'sanPhamID' để khớp với name input bên View Details
        [HttpPost]
        public ActionResult AddtoCart(string sanPhamID)
        {
            var cart = GetCart();
            cart.Them(sanPhamID);
            Session["Cart"] = cart; // Cập nhật lại session
            return RedirectToAction("Index");
        }

        // 3. Cập nhật số lượng (+ / -)
        public ActionResult UpdateSLCart(string id, int type)
        {
            var cart = GetCart();
            if (type == 1)
            {
                cart.Them(id);
            }
            else
            {
                cart.Giam(id);
            }
            return RedirectToAction("Index");
        }

        // 4. Xóa sản phẩm
        public ActionResult RemoveFromCart(string id)
        {
            var cart = GetCart();
            cart.Xoa(id);
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
            return View("Index", "Payment");
        }
    }

    
    }
