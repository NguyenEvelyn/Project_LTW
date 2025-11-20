using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_LTW.Models;

namespace Project_LTW.Controllers
{
    public class PaymentController : Controller
    {
        FashionWebEntities db = new FashionWebEntities();

        // ==========================================
        // 1. GET: Hiển thị trang thanh toán (BẠN ĐANG THIẾU CÁI NÀY)
        // ==========================================
        [HttpGet]
        public ActionResult Index()
        {
            // Kiểm tra đăng nhập
            if (Session["User"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Kiểm tra giỏ hàng
            var cart = Session["Cart"] as Cart;
            if (cart == null || cart.list == null || cart.list.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            var user = Session["User"] as CUSTOMER;

            // Truyền dữ liệu sang View
            ViewBag.Cart = cart.list;
            ViewBag.User = user;
            ViewBag.UserAddress = user?.ADDRESSes; // Nếu bảng User có list Address
            ViewBag.Total = cart.TongTien();

            return View();
        }

        // ==========================================
        // 2. POST: Xử lý khi bấm nút "Xác nhận đặt hàng"
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string ShipName, string ShipPhone, string ShipAddress, string Note)
        {
            // 1. Load lại dữ liệu để chuẩn bị nếu có lỗi thì return View ngay
            var cartInfo = Session["Cart"] as Cart;
            var user = Session["User"] as CUSTOMER;

            // Setup lại ViewBag để tránh lỗi View khi return
            ViewBag.Cart = cartInfo?.list;
            if (cartInfo != null) ViewBag.Total = cartInfo.TongTien();
            ViewBag.User = user;
            ViewBag.UserAddress = user?.ADDRESSes;

            // 2. Validate Input form
            if (string.IsNullOrEmpty(ShipName) || string.IsNullOrEmpty(ShipPhone) || string.IsNullOrEmpty(ShipAddress))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin giao hàng.";
                return View();
            }

            try
            {
                if (cartInfo == null || cartInfo.list.Count == 0)
                {
                    ViewBag.Error = "Giỏ hàng trống!";
                    return View();
                }

                // 3. TẠO ĐƠN HÀNG (ORDER)
                ORDER newOrder = new ORDER();

                // Random ID
                Random r = new Random();
                string orderId;
                do { orderId = "DH" + r.Next(10000, 99999).ToString(); }
                while (db.ORDERS.Any(o => o.ORDERID == orderId));

                newOrder.ORDERID = orderId;
                newOrder.KHACHHANGID = user.KHACHHANGID;
                newOrder.NGAYDAT = DateTime.Now;
                newOrder.TONGTIEN = cartInfo.TongTien();
                newOrder.TRANGTHAI = "Chờ xử lý";
                newOrder.MANV_XULY = null;

                // --- KHẮC PHỤC LỖI DIACHIID ---
                // Đặt null để tránh lỗi Foreign Key nếu chưa có bảng Address
                // Đảm bảo trong SQL Server cột DIACHIID đã tích chọn "Allow Nulls"
                newOrder.DIACHIID = null;

                // Gán ghi chú
                string fullNote = $"Người nhận: {ShipName} | SĐT: {ShipPhone} | ĐC: {ShipAddress}";
                if (!string.IsNullOrEmpty(Note)) fullNote += $" | Note: {Note}";

                // Cắt ngắn ghi chú nếu DB giới hạn ký tự
                if (fullNote.Length > 500) fullNote = fullNote.Substring(0, 500);
                newOrder.GHICHU = fullNote;

                db.ORDERS.Add(newOrder);
                db.SaveChanges(); // <--- Lưu Order

                // 4. TẠO CHI TIẾT (ORDERDETAIL)
                foreach (var item in cartInfo.list)
                {
                    ORDERDETAIL detail = new ORDERDETAIL();
                    detail.ORDERID = newOrder.ORDERID;
                    detail.SANPHAMID = item.MASP;
                    detail.SOLUONG = item.SoLuong;
                    detail.DONGIA = item.DonGia;
                    detail.THANHTIEN = item.ThanhTien;

                    // Lưu ý: Kiểm tra kỹ tên bảng trong Model (ORDERDETAILs hay ORDERDETAILS)
                    db.ORDERDETAILs.Add(detail);
                }

                db.SaveChanges(); // <--- Lưu Chi tiết

                // 5. Thành công
                Session["Cart"] = null; // Xóa giỏ hàng
                TempData["SuccessMessage"] = $"Đặt hàng thành công! Mã đơn: {newOrder.ORDERID}";

                return RedirectToAction("Success","Payment");
            }
            // --- BẮT LỖI VALIDATION (QUAN TRỌNG) ---
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                string loiChiTiet = "";
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        loiChiTiet += $"Lỗi tại cột: {validationError.PropertyName} - {validationError.ErrorMessage} <br/>";
                    }
                }
                ViewBag.Error = "Lỗi dữ liệu: " + loiChiTiet;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi hệ thống: " + ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : "");
                return View();
            }
        }

        // ==========================================
        // 3. Trang thông báo thành công
        // ==========================================
        public ActionResult Success()
        {
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View();
        }
    }
}