using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Project_LTW.Controllers
{
    public class UserController : Controller
    {
        // GET: User
        FashionWebEntities db = new FashionWebEntities();
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public ActionResult LoginOnSubmit(FormCollection collect)
        {
            var email = collect["Email"];
            var password = collect["Password"];
        
            var khachHang = db.CUSTOMERs.FirstOrDefault(k => k.EMAIL == email && k.PASSWORD == password);

            if (khachHang != null)
            {
                //  Đăng nhập thành công quyền KHÁCH
                Session["User"] = khachHang;

         
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            
            // Nếu không phải khách hàng, thử tìm trong bảng Staff
            var nhanVien = db.STAFFs.FirstOrDefault(s => s.EMAIL == email && s.PASSWORD == password);

            if (nhanVien != null)
            {
                //  Đăng nhập thành công quyền ADMIN
                Session["AdminUser"] = nhanVien; // Lưu session riêng cho Admin

                // Chuyển hướng thẳng vào Dashboard Admin
                
                return RedirectToAction("Index", "HomeAdmin", new { area = "Admin" });
            }
            // Nếu cả 2 bảng đều không tìm thấy
            ViewBag.Error = "Email hoặc mật khẩu không đúng!";
            return View("Login");}

        // --- PHẦN ĐĂNG KÝ ---
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }
        // 1. GET: Hiển thị trang đăng ký
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(CUSTOMER cus, string ConfirmPassword)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra Email trùng
                var checkEmail = db.CUSTOMERs.FirstOrDefault(x => x.EMAIL == cus.EMAIL);
                if (checkEmail != null)
                {
                    ViewBag.Error = "Email đã được sử dụng. Vui lòng chọn email khác.";
                    return View();
                }

                // 2. Kiểm tra mật khẩu xác nhận
                if (cus.PASSWORD != ConfirmPassword)
                {
                    ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                    return View();
                }

                // 3. TẠO ID TỰ ĐỘNG (Quan trọng: Phải làm trước khi Add)
                Random r = new Random();
                // Tạo ID dạng: KH12345
                cus.KHACHHANGID = "KH" + r.Next(1000, 99999).ToString();

                // 4. Lưu vào Database
                try
                {
                    db.CUSTOMERs.Add(cus);
                    db.SaveChanges();

                    // 5. Thành công -> Chuyển hướng
                    TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    // Bắt lỗi nếu có (ví dụ lỗi kết nối DB, hoặc ID bị trùng ngẫu nhiên)
                    ViewBag.Error = "Có lỗi xảy ra khi lưu dữ liệu: " + ex.Message;
                    return View();
                }
            }

            // Nếu dữ liệu input không hợp lệ (ModelState false) thì trả lại form
            return View();
        }
        public ActionResult Logout()
        {
            Session["User"] = null; // Xóa session
            return RedirectToAction("Index", "Home"); // Quay về trang chủ
        }

        // =============================================
        //  DANH SÁCH ĐƠN HÀNG ĐÃ ĐẶT
        // =============================================
        public ActionResult OrderHistory()
        {
            // Kiểm tra đăng nhập
            if (Session["User"] == null)
                return RedirectToAction("Login");

            var user = Session["User"] as CUSTOMER;

            // Lấy danh sách đơn hàng của user đó, sắp xếp mới nhất lên đầu
            var orders = db.ORDERS
                           .Where(o => o.KHACHHANGID == user.KHACHHANGID)
                           .OrderByDescending(o => o.NGAYDAT)
                           .ToList();

            return View(orders);
        }

        // =============================================
        // XEM CHI TIẾT MỘT ĐƠN HÀNG
        // =============================================
        public ActionResult OrderDetail(string id)
        {
            if (Session["User"] == null)
                return RedirectToAction("Login");

            // Lấy đơn hàng theo ID
            var order = db.ORDERS.FirstOrDefault(o => o.ORDERID == id);

            if (order == null)
            {
                return HttpNotFound(); // Không tìm thấy đơn
            }

            // Bảo mật: Kiểm tra đơn này có đúng của user đang đăng nhập không?
            var user = Session["User"] as CUSTOMER;
            if (order.KHACHHANGID != user.KHACHHANGID)
            {
                return RedirectToAction("OrderHistory"); // Không phải của mình thì đá về danh sách
            }

            return View(order);
        }
        // HỦY ĐƠN HÀNG CỦA KHÁCH
        // HỦY ĐƠN HÀNG (Sử dụng Procedure SQL để hoàn tồn kho)
        public ActionResult CancelOrder(string id)
        {
            if (Session["User"] == null) return RedirectToAction("Login");

            var user = Session["User"] as CUSTOMER;

            // Kiểm tra sơ bộ xem đơn hàng có tồn tại và đúng chủ không
            var order = db.ORDERS.FirstOrDefault(o => o.ORDERID == id);
            if (order == null || order.KHACHHANGID != user.KHACHHANGID)
            {
                TempData["Error"] = "Đơn hàng không hợp lệ.";
                return RedirectToAction("OrderHistory");
            }

            // Kiểm tra trạng thái trên C# trước cho chắc (dù SQL cũng có check)
            if (order.TRANGTHAI != "Chờ xử lý")
            {
                TempData["Error"] = "Chỉ có thể hủy đơn hàng khi đang Chờ xử lý.";
                return RedirectToAction("OrderHistory");
            }

            try
            {
                // --- GỌI PROCEDURE SQL ĐỂ HỦY VÀ HOÀN KHO ---
               
                db.Database.ExecuteSqlCommand("EXEC SP_HUYDONHANG @p0", id);

                TempData["Success"] = "Đã hủy đơn hàng và hoàn lại tồn kho thành công!";
            }
            catch (Exception ex)
            {
                // Lấy lỗi từ SQL (Ví dụ: Đơn đã giao không thể hủy...)
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["Error"] = "Lỗi hủy đơn: " + innerMessage;
            }

            return RedirectToAction("OrderHistory");
        }
    }
}
