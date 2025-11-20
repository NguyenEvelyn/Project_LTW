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

            // Kiểm tra tài khoản tồn tại trong DB
            CUSTOMER kh = db.CUSTOMERs.FirstOrDefault(x => x.EMAIL == email && x.PASSWORD == password);

            if (kh != null)
            {
                // ✅ Đăng nhập thành công
                Session["User"] = kh;
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // ❌ Sai tài khoản hoặc mật khẩu
                ViewBag.Error = "Email hoặc mật khẩu không đúng!";
                return View("Login");
            }
        }

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
    }
}
