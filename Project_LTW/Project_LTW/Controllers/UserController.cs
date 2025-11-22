using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

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
                // Đăng nhập thành công quyền KHÁCH
                Session["User"] = khachHang;

                return RedirectToAction("Index", "Home", new { area = "" });
            }


            // Nếu không phải khách hàng, thử tìm trong bảng Staff
            var nhanVien = db.STAFFs.FirstOrDefault(s => s.EMAIL == email && s.PASSWORD == password);

            if (nhanVien != null)
            {
                // Đăng nhập thành công quyền ADMIN
                Session["AdminUser"] = nhanVien; // Lưu session riêng cho Admin

                // ********** ĐIỀU CHỈNH ĐỂ HOÀN THÀNH PHẦN 1 **********

                Session["MANV"] = nhanVien.MANV;
                // *****************************************************

                // Chuyển hướng thẳng vào Dashboard Admin
                return RedirectToAction("Index", "HomeAdmin", new { area = "Admin" });
            }
            // Nếu cả 2 bảng đều không tìm thấy
            ViewBag.Error = "Email hoặc mật khẩu không đúng!";
            return View("Login");
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
        // =============================================
        //  THÔNG TIN TÀI KHOẢN (PROFILE)
        // =============================================

        // GET: /User/AccountProfile
        public ActionResult AccountProfile()
        {
            // Bắt buộc phải đăng nhập để vào trang này
            if (Session["User"] == null)
                return RedirectToAction("Login");

            var user = Session["User"] as CUSTOMER;

            // Tải thông tin người dùng cùng với Địa chỉ (Eager Loading)
            var customerData = db.CUSTOMERs
                                 .Include(c => c.ADDRESSes) // Tải địa chỉ liên quan
                                 .FirstOrDefault(c => c.KHACHHANGID == user.KHACHHANGID);

            if (customerData == null)
            {
                // Điều này hiếm khi xảy ra nếu Session còn nhưng DB bị mất
                Session["User"] = null;
                return RedirectToAction("Login");
            }

            // Chúng ta sẽ dùng chính đối tượng CUSTOMER làm Model cho View
            return View(customerData);
        }

        // POST: /User/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(CUSTOMER model)
        {
            if (Session["User"] == null)
                return RedirectToAction("Login");

            // Lấy ID khách hàng hiện tại
            var currentUserId = (Session["User"] as CUSTOMER).KHACHHANGID;

            // 1. Kiểm tra tính hợp lệ của Model
            if (ModelState.IsValid)
            {
                // 2. Tìm đối tượng trong DB để cập nhật
                var customerInDb = db.CUSTOMERs.Find(currentUserId);

                if (customerInDb != null)
                {
                    // Kiểm tra trùng Email mới (nếu khách hàng thay đổi email)
                    var checkEmail = db.CUSTOMERs.FirstOrDefault(x => x.EMAIL == model.EMAIL && x.KHACHHANGID != currentUserId);
                    if (checkEmail != null)
                    {
                        ViewBag.Error = "Email mới đã được sử dụng bởi tài khoản khác.";
                        return View("AccountProfile", customerInDb);
                    }

                    // 3. Cập nhật các trường được phép
                    customerInDb.HOTEN = model.HOTEN;
                    customerInDb.EMAIL = model.EMAIL;
                    customerInDb.DIENTHOAI = model.DIENTHOAI;

                    // Nếu có trường Ngày sinh/Giới tính, bạn cũng có thể cập nhật
                    // customerInDb.NGAYSINH = model.NGAYSINH;
                    // customerInDb.GIOITINH = model.GIOITINH;

                    try
                    {
                        db.SaveChanges();

                        // Cập nhật lại Session để hiển thị tên và email mới ngay lập tức
                        Session["User"] = customerInDb;

                        TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công!";
                        return RedirectToAction("AccountProfile");
                    }
                    catch (Exception ex)
                    {
                        ViewBag.Error = "Lỗi hệ thống khi lưu: " + ex.Message;
                        // Trả lại View với dữ liệu cũ để khách hàng xem lỗi
                        return View("AccountProfile", customerInDb);
                    }
                }
            }

            // NẾU ModelState KHÔNG HỢP LỆ HOẶC CÓ LỖI XẢY RA TRƯỚC ĐÓ:
            // SỬA LỖI CÚ PHÁP CS1660: Lấy Model vào biến rồi mới truyền vào View()
            var modelToReturn = db.CUSTOMERs
                                  .Include(c => c.ADDRESSes)
                                  .FirstOrDefault(c => c.KHACHHANGID == currentUserId);

            return View("AccountProfile", modelToReturn);
        }

        // =============================================
        //  QUẢN LÝ ĐỊA CHỈ GIAO HÀNG (ADDRESS)
        // =============================================

        // GET: /User/AddAddress (Hiện form thêm mới)
        [HttpGet]
        public ActionResult AddAddress()
        {
            if (Session["User"] == null)
                return RedirectToAction("Login");

            // Tạo một đối tượng Address trống để binding trong View
            return View();
        }

        // POST: /User/AddAddress (Xử lý lưu địa chỉ mới)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddAddress(ADDRESS model)
        {
            if (Session["User"] == null)
                return RedirectToAction("Login");

            var user = Session["User"] as CUSTOMER;

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Tạo ID ngẫu nhiên không trùng cho Địa chỉ
                    Random r = new Random();
                    model.DIACHIID = "DC" + r.Next(1000, 99999).ToString();

                    // 2. Gán Khách hàng ID
                    model.KHACHHANGID = user.KHACHHANGID;

                    // 3. Lưu vào Database
                    db.ADDRESSes.Add(model);
                    db.SaveChanges();

                    // Cập nhật lại Session (để trang AccountProfile hiển thị ngay địa chỉ mới)
                    var updatedCustomer = db.CUSTOMERs.Include(c => c.ADDRESSes).FirstOrDefault(c => c.KHACHHANGID == user.KHACHHANGID);
                    Session["User"] = updatedCustomer;

                    TempData["SuccessMessage"] = "Thêm địa chỉ mới thành công!";
                    return RedirectToAction("AccountProfile");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi khi thêm địa chỉ: " + ex.Message;
                }
            }

            return View(model);
        }



        // GET: /User/EditAddress/DCxxxx (Lỗi 404 thường xảy ra ở đây)
        [HttpGet]
        public ActionResult EditAddress(string addressId) // <--- Đổi từ 'id' sang 'addressId'
        {
            if (Session["User"] == null)
                return RedirectToAction("Login");

            var user = Session["User"] as CUSTOMER;

            // Tìm địa chỉ theo ID và phải thuộc về người dùng hiện tại
            var address = db.ADDRESSes.FirstOrDefault(a => a.DIACHIID == addressId && a.KHACHHANGID == user.KHACHHANGID);

            if (address == null)
            {
                TempData["Error"] = "Địa chỉ không tồn tại hoặc không phải của bạn.";
                return RedirectToAction("AccountProfile");
            }

            return View(address);
        }

        // POST: /User/EditAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAddress(ADDRESS model)
        {
            if (Session["User"] == null)
                return RedirectToAction("Login");

            var user = Session["User"] as CUSTOMER;

            if (ModelState.IsValid)
            {
                // 1. TÌM đối tượng gốc từ DB (phương pháp an toàn nhất để update)
                var originalAddress = db.ADDRESSes.Find(model.DIACHIID);

                // Kiểm tra địa chỉ có tồn tại và thuộc về user hiện tại không
                if (originalAddress == null || originalAddress.KHACHHANGID != user.KHACHHANGID)
                {
                    TempData["Error"] = "Địa chỉ không tồn tại hoặc không phải của bạn.";
                    return RedirectToAction("AccountProfile");
                }

                try
                {
                    // 2. CẬP NHẬT các trường được phép sửa từ Model (form) vào đối tượng gốc
                    // Khóa chính (DIACHIID) và Khóa ngoại (KHACHHANGID) không bị đụng chạm.
                    originalAddress.DUONG = model.DUONG;
                    originalAddress.TINH = model.TINH;
                    originalAddress.THANHPHO = model.THANHPHO;
                    originalAddress.ZIPCODE = model.ZIPCODE;

                    // Dòng này là đủ để Entity Framework biết phải lưu đối tượng đã được theo dõi (originalAddress)
                    db.SaveChanges();

                    // Cập nhật lại Session
                    var updatedCustomer = db.CUSTOMERs.Include(c => c.ADDRESSes).FirstOrDefault(c => c.KHACHHANGID == user.KHACHHANGID);
                    Session["User"] = updatedCustomer;

                    TempData["SuccessMessage"] = "Cập nhật địa chỉ thành công!";
                    return RedirectToAction("AccountProfile");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi khi cập nhật địa chỉ: " + ex.Message;
                    // Trả về đối tượng gốc để giữ lại DIACHIID và KHACHHANGID
                    return View(originalAddress);
                }
            }

            // Nếu ModelState không hợp lệ, trả lại View với dữ liệu hiện có
            return View(model);
        }

        // POST: /User/DeleteAddress/DCxxxx (Xử lý xóa địa chỉ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAddress(string id)
        {
            if (Session["User"] == null)
                return RedirectToAction("Login");

            var user = Session["User"] as CUSTOMER;
            var address = db.ADDRESSes.FirstOrDefault(a => a.DIACHIID == id && a.KHACHHANGID == user.KHACHHANGID);

            if (address == null)
            {
                TempData["Error"] = "Địa chỉ không tồn tại.";
                return RedirectToAction("AccountProfile");
            }

            try
            {
                db.ADDRESSes.Remove(address);
                db.SaveChanges();

                // Cập nhật lại Session
                var updatedCustomer = db.CUSTOMERs.Include(c => c.ADDRESSes).FirstOrDefault(c => c.KHACHHANGID == user.KHACHHANGID);
                Session["User"] = updatedCustomer;

                TempData["SuccessMessage"] = "Địa chỉ đã được xóa thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi xóa: " + ex.Message;
            }

            return RedirectToAction("AccountProfile");
        }

        // =============================================
        //  ĐỔI MẬT KHẨU (CHANGE PASSWORD)
        // =============================================

        // GET: /User/ChangePassword
        [HttpGet]
        public ActionResult ChangePassword()
        {
            // Bắt buộc phải đăng nhập
            if (Session["User"] == null)
                return RedirectToAction("Login");

            // Sử dụng ViewBag để truyền thông báo lỗi/thành công
            return View();
        }

        // POST: /User/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            if (Session["User"] == null)
                return RedirectToAction("Login");

            var userInSession = Session["User"] as CUSTOMER;
            var currentUserId = userInSession.KHACHHANGID;

            // 1. Kiểm tra Mật khẩu mới và Xác nhận Mật khẩu
            if (NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "Mật khẩu mới và Xác nhận mật khẩu không khớp.";
                return View();
            }

            // 2. Kiểm tra tính hợp lệ của Model (nếu bạn có Data Annotation cho độ dài/độ mạnh)
            // Nếu bạn không dùng ViewModel, bỏ qua ModelState.IsValid

            // 3. Tải dữ liệu khách hàng từ DB để kiểm tra Mật khẩu cũ
            var customerInDb = db.CUSTOMERs.Find(currentUserId);

            if (customerInDb != null)
            {
                // 4. KIỂM TRA MẬT KHẨU CŨ
                // LƯU Ý: Nếu mật khẩu của bạn được mã hóa (MD5, Hash), bạn phải giải mã/hash OldPassword để so sánh.
                // GIẢ SỬ MẬT KHẨU KHÔNG MÃ HÓA (Chỉ để logic hoạt động):
                if (customerInDb.PASSWORD != OldPassword)
                {
                    ViewBag.Error = "Mật khẩu cũ không chính xác.";
                    return View();
                }

                // 5. CẬP NHẬT MẬT KHẨU MỚI
                try
                {
                    // LƯU Ý: Nếu mật khẩu được mã hóa, bạn phải MÃ HÓA NewPassword trước khi gán
                    customerInDb.PASSWORD = NewPassword;

                    db.SaveChanges();

                    // Cập nhật lại Session (Không bắt buộc, nhưng nên làm)
                    userInSession.PASSWORD = NewPassword;
                    Session["User"] = userInSession;

                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Bạn có thể dùng mật khẩu mới ngay.";
                    return RedirectToAction("AccountProfile"); // Chuyển về trang tổng quan
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi hệ thống khi lưu mật khẩu: " + ex.Message;
                    return View();
                }
            }

            // Trường hợp không tìm thấy user (rất hiếm)
            ViewBag.Error = "Không tìm thấy thông tin tài khoản.";
            return View();
        }
    }
}
