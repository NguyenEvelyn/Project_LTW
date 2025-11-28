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
        //[HttpPost]
        //public ActionResult LoginOnSubmit(FormCollection collect)
        //{
        //    var email = collect["Email"];
        //    var password = collect["Password"];

        //    var khachHang = db.CUSTOMERs.FirstOrDefault(k => k.EMAIL == email && k.PASSWORD == password);

        //    if (khachHang != null)
        //    {
        //        // Đăng nhập thành công quyền KHÁCH
        //        Session["User"] = khachHang;

        //        return RedirectToAction("Index", "Home", new { area = "" });
        //    }


        //    // Nếu không phải khách hàng, thử tìm trong bảng Staff
        //    var nhanVien = db.STAFFs.FirstOrDefault(s => s.EMAIL == email && s.PASSWORD == password);

        //    if (nhanVien != null)
        //    {
        //        // Đăng nhập thành công quyền ADMIN
        //        Session["AdminUser"] = nhanVien; // Lưu session riêng cho Admin



        //        Session["MANV"] = nhanVien.MANV;

        //        return RedirectToAction("Index", "HomeAdmin", new { area = "Admin" });
        //    }

        //    ViewBag.Error = "Email hoặc mật khẩu không đúng!";
        //    return View("Login");
        //}

        [HttpPost]
        public ActionResult LoginOnSubmit(FormCollection collect)
        {
            var email = collect["Email"];
            var password = collect["Password"];

            var khachHang = db.CUSTOMERs.FirstOrDefault(k => k.EMAIL == email && k.PASSWORD == password);

            if (khachHang != null)
            {
                Session["User"] = khachHang;
                Session["AdminUser"] = null;
                Session["MANV"] = null;

                return RedirectToAction("Index", "Home", new { area = "" });
            }


            var nhanVien = db.STAFFs.FirstOrDefault(s => s.EMAIL == email && s.PASSWORD == password);

            if (nhanVien != null)
            {
                Session["AdminUser"] = nhanVien;
                Session["MANV"] = nhanVien.MANV;
                Session["User"] = null;

                return RedirectToAction("Index", "HomeAdmin", new { area = "Admin" });
            }


            ViewBag.Error = "Email hoặc mật khẩu không đúng!";
            return View("Login");
        }

        // --- PHẦN ĐĂNG KÝ ---
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }
      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(CUSTOMER cus, string ConfirmPassword)
        {
            if (ModelState.IsValid)
            {

                var checkEmail = db.CUSTOMERs.FirstOrDefault(x => x.EMAIL == cus.EMAIL);
                if (checkEmail != null)
                {
                    ViewBag.Error = "Email đã được sử dụng. Vui lòng chọn email khác.";
                    return View();
                }

             
                if (cus.PASSWORD != ConfirmPassword)
                {
                    ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                    return View();
                }

               
                Random r = new Random();
               
                cus.KHACHHANGID = "KH" + r.Next(1000, 99999).ToString();


                try
                {
                    db.CUSTOMERs.Add(cus);
                    db.SaveChanges();

         
                    TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Có lỗi xảy ra khi lưu dữ liệu: " + ex.Message;
                    return View();
                }
            }

         
            return View();
        }

        public ActionResult Logout()

        {

            Session["User"] = null;
            Session["AdminUser"] = null;
            Session["MANV"] = null;

            return RedirectToAction("Index", "Home");
        }


        public ActionResult OrderHistory()
        {


            if (Session["User"] == null)
            {

                if (Session["AdminUser"] != null)
                {
                    TempData["Error"] = "Admin/Nhân viên không thể truy cập Lịch sử đơn hàng của Khách hàng.";
                    return RedirectToAction("Index", "Home", new { area = "" });
                }

                return RedirectToAction("Login");
            }

            var user = Session["User"] as CUSTOMER;


            var orders = db.FN_DANHSACHDONHANG_KH(user.KHACHHANGID).ToList();

            return View(orders);
        }

       
        public ActionResult OrderDetail(string id)
        {
            if (Session["User"] == null)
                return RedirectToAction("Login");

          
            var order = db.ORDERS.FirstOrDefault(o => o.ORDERID == id);

            if (order == null)
            {
                return HttpNotFound(); 
            }

            
            var user = Session["User"] as CUSTOMER;
            if (order.KHACHHANGID != user.KHACHHANGID)
            {
                return RedirectToAction("OrderHistory"); 
            }

            return View(order);
        }
        
        public ActionResult CancelOrder(string id)
        {
            if (Session["User"] == null) return RedirectToAction("Login");

            var user = Session["User"] as CUSTOMER;

           
            var order = db.ORDERS.FirstOrDefault(o => o.ORDERID == id);
            if (order == null || order.KHACHHANGID != user.KHACHHANGID)
            {
                TempData["Error"] = "Đơn hàng không hợp lệ.";
                return RedirectToAction("OrderHistory");
            }


            if (order.TRANGTHAI == null || !order.TRANGTHAI.Contains("Chờ"))
            {
                TempData["Error"] = "Bạn chỉ có thể hủy đơn hàng khi đang ở trạng thái Chờ xử lý/xác nhận.";
                return RedirectToAction("OrderHistory");
            }

            try
            {
           
               
                db.Database.ExecuteSqlCommand("EXEC SP_HUYDONHANG @p0", id);

                TempData["Success"] = "Đã hủy đơn hàng thành công!";
            }
            catch (Exception ex)
            {
              
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["Error"] = "Lỗi hủy đơn: " + innerMessage;
            }

            return RedirectToAction("OrderHistory");
        }
      

        // GET: /User/AccountProfile
        public ActionResult AccountProfile()
        {
            
            if (Session["User"] == null)
                return RedirectToAction("Login");

            var user = Session["User"] as CUSTOMER;

           
            var customerData = db.CUSTOMERs
                                 .Include(c => c.ADDRESSes) 
                                 .FirstOrDefault(c => c.KHACHHANGID == user.KHACHHANGID);

            if (customerData == null)
            {
                Session["User"] = null;
                return RedirectToAction("Login");
            }

        
            return View(customerData);
        }

        // POST: /User/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(CUSTOMER model)
        {
            if (Session["User"] == null)
                return RedirectToAction("Login");

    
            var currentUserId = (Session["User"] as CUSTOMER).KHACHHANGID;

           
            if (ModelState.IsValid)
            {
          
                var customerInDb = db.CUSTOMERs.Find(currentUserId);

                if (customerInDb != null)
                {
    
                    var checkEmail = db.CUSTOMERs.FirstOrDefault(x => x.EMAIL == model.EMAIL && x.KHACHHANGID != currentUserId);
                    if (checkEmail != null)
                    {
                        ViewBag.Error = "Email mới đã được sử dụng bởi tài khoản khác.";
                        return View("AccountProfile", customerInDb);
                    }

          
                    customerInDb.HOTEN = model.HOTEN;
                    customerInDb.EMAIL = model.EMAIL;
                    customerInDb.DIENTHOAI = model.DIENTHOAI;

               
                    // customerInDb.NGAYSINH = model.NGAYSINH;
                    // customerInDb.GIOITINH = model.GIOITINH;

                    try
                    {
                        db.SaveChanges();

                 
                        Session["User"] = customerInDb;

                        TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công!";
                        return RedirectToAction("AccountProfile");
                    }
                    catch (Exception ex)
                    {
                        ViewBag.Error = "Lỗi hệ thống khi lưu: " + ex.Message;
                        
                        return View("AccountProfile", customerInDb);
                    }
                }
            }

      
            var modelToReturn = db.CUSTOMERs
                                  .Include(c => c.ADDRESSes)
                                  .FirstOrDefault(c => c.KHACHHANGID == currentUserId);

            return View("AccountProfile", modelToReturn);
        }

    

        // GET: /User/AddAddress 
        [HttpGet]
        public ActionResult AddAddress()
        {
            if (Session["User"] == null)
                return RedirectToAction("Login");

  
            return View();
        }

        // POST: /User/AddAddress 
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
                    Random r = new Random();
                    model.DIACHIID = "DC" + r.Next(1000, 99999).ToString();

                  
                    model.KHACHHANGID = user.KHACHHANGID;

                   
                    db.ADDRESSes.Add(model);
                    db.SaveChanges();

                  
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



        // GET: /User/EditAddress/
        [HttpGet]
        public ActionResult EditAddress(string addressId) 
        {
            if (Session["User"] == null)
                return RedirectToAction("Login");

            var user = Session["User"] as CUSTOMER;

    
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
                
                var originalAddress = db.ADDRESSes.Find(model.DIACHIID);

            
                if (originalAddress == null || originalAddress.KHACHHANGID != user.KHACHHANGID)
                {
                    TempData["Error"] = "Địa chỉ không tồn tại hoặc không phải của bạn.";
                    return RedirectToAction("AccountProfile");
                }

                try
                {
                  
                    originalAddress.DUONG = model.DUONG;
                    originalAddress.TINH = model.TINH;
                    originalAddress.THANHPHO = model.THANHPHO;
                    originalAddress.ZIPCODE = model.ZIPCODE;

             
                    db.SaveChanges();

                    var updatedCustomer = db.CUSTOMERs.Include(c => c.ADDRESSes).FirstOrDefault(c => c.KHACHHANGID == user.KHACHHANGID);
                    Session["User"] = updatedCustomer;

                    TempData["SuccessMessage"] = "Cập nhật địa chỉ thành công!";
                    return RedirectToAction("AccountProfile");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi khi cập nhật địa chỉ: " + ex.Message;
                  
                    return View(originalAddress);
                }
            }

            return View(model);
        }

        // POST: /User/DeleteAddress
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

      

        // GET: /User/ChangePassword
        [HttpGet]
        public ActionResult ChangePassword()
        {
            
            if (Session["User"] == null)
                return RedirectToAction("Login");

           
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

        
            if (NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "Mật khẩu mới và Xác nhận mật khẩu không khớp.";
                return View();
            }

          
            var customerInDb = db.CUSTOMERs.Find(currentUserId);

            if (customerInDb != null)
            {
                if (customerInDb.PASSWORD != OldPassword)
                {
                    ViewBag.Error = "Mật khẩu cũ không chính xác.";
                    return View();
                }

                try
                {

                    customerInDb.PASSWORD = NewPassword;

                    db.SaveChanges();


                    userInSession.PASSWORD = NewPassword;
                    Session["User"] = userInSession;

                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Bạn có thể dùng mật khẩu mới ngay.";
                    return RedirectToAction("AccountProfile");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi hệ thống khi lưu mật khẩu: " + ex.Message;
                    return View();
                }
            }

           
            ViewBag.Error = "Không tìm thấy thông tin tài khoản.";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateNgaySinh(DateTime? NGAYSINH)
        {

            if (Session["User"] == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện chức năng này." });
            }


            var userInSession = Session["User"] as CUSTOMER;
            var customerToUpdate = db.CUSTOMERs.Find(userInSession.KHACHHANGID);

            if (customerToUpdate == null)
            {

                return Json(new { success = false, message = "Không tìm thấy tài khoản." });
            }


            if (NGAYSINH.HasValue)
            {

                if (NGAYSINH.Value > DateTime.Now.Date)
                {
                    return Json(new { success = false, message = "Ngày sinh không được lớn hơn ngày hiện tại." });
                }


                customerToUpdate.NGAYSINH = NGAYSINH.Value.Date;
            }
            else
            {

                customerToUpdate.NGAYSINH = null;
            }

            try
            {

                db.SaveChanges();


                userInSession.NGAYSINH = customerToUpdate.NGAYSINH;
                Session["User"] = userInSession;

                return Json(new { success = true, message = "Cập nhật Ngày sinh thành công!" });
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = "Lỗi hệ thống khi lưu dữ liệu. Vui lòng thử lại. Lỗi: " + ex.Message });
            }
        }
    }
}
