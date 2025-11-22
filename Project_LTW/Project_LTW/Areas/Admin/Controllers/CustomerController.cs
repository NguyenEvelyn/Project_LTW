using System;
using System.Linq;
using System.Web.Mvc;
using Project_LTW.Models;
using System.Data.Entity;

namespace Project_LTW.Areas.Admin.Controllers
{
    [CheckAdmin] // Bắt buộc đăng nhập Admin
    public class CustomerController : Controller
    {
        FashionWebEntities db = new FashionWebEntities();

        // ==========================================
        // 1. DANH SÁCH KHÁCH HÀNG
        // ==========================================
        public ActionResult Index()
        {
            var customers = db.CUSTOMERs
                              .OrderByDescending(c => c.KHACHHANGID)
                              .ToList();
            return View(customers);
        }

        // ==========================================
        // 2. THÊM KHÁCH HÀNG - GET
        // ==========================================
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        // ==========================================
        // 2. THÊM KHÁCH HÀNG - POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CUSTOMER model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra trùng EMAIL
                    var existingEmail = db.CUSTOMERs.FirstOrDefault(c => c.EMAIL == model.EMAIL);
                    if (existingEmail != null)
                    {
                        ViewBag.Error = "Email này đã được sử dụng. Vui lòng chọn email khác.";
                        return View(model);
                    }

                    // Kiểm tra trùng SỐ ĐIỆN THOẠI
                    if (!string.IsNullOrEmpty(model.DIENTHOAI))
                    {
                        var existingPhone = db.CUSTOMERs.FirstOrDefault(c => c.DIENTHOAI == model.DIENTHOAI);
                        if (existingPhone != null)
                        {
                            ViewBag.Error = "Số điện thoại này đã tồn tại. Vui lòng nhập số khác.";
                            return View(model);
                        }
                    }

                    // Tạo ID tự động
                    Random r = new Random();
                    model.KHACHHANGID = "KH" + r.Next(10000, 99999).ToString();

                    // Lưu vào database
                    db.CUSTOMERs.Add(model);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Thêm khách hàng thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi hệ thống: " + ex.Message;
                }
            }

            return View(model);
        }

        // ==========================================
        // 3. CẬP NHẬT KHÁCH HÀNG - GET
        // ==========================================
        [HttpGet]
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "ID không hợp lệ.";
                return RedirectToAction("Index");
            }

            // Trim để loại bỏ khoảng trắng
            id = id.Trim();

            var customer = db.CUSTOMERs.Find(id);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng với ID: " + id;
                return RedirectToAction("Index");
            }

            return View(customer);
        }

        // ==========================================
        // 3. CẬP NHẬT KHÁCH HÀNG - POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CUSTOMER model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Tìm khách hàng gốc
                    var customerInDb = db.CUSTOMERs.Find(model.KHACHHANGID);
                    if (customerInDb == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
                        return RedirectToAction("Index");
                    }

                    // Kiểm tra trùng EMAIL (nếu thay đổi)
                    if (customerInDb.EMAIL != model.EMAIL)
                    {
                        var existingEmail = db.CUSTOMERs.FirstOrDefault(c => c.EMAIL == model.EMAIL && c.KHACHHANGID != model.KHACHHANGID);
                        if (existingEmail != null)
                        {
                            ViewBag.Error = "Email này đã được sử dụng bởi khách hàng khác.";
                            return View(model);
                        }
                    }

                    // Kiểm tra trùng SỐ ĐIỆN THOẠI (nếu thay đổi)
                    if (!string.IsNullOrEmpty(model.DIENTHOAI) && customerInDb.DIENTHOAI != model.DIENTHOAI)
                    {
                        var existingPhone = db.CUSTOMERs.FirstOrDefault(c => c.DIENTHOAI == model.DIENTHOAI && c.KHACHHANGID != model.KHACHHANGID);
                        if (existingPhone != null)
                        {
                            ViewBag.Error = "Số điện thoại này đã tồn tại.";
                            return View(model);
                        }
                    }

                    // Cập nhật thông tin
                    customerInDb.HOTEN = model.HOTEN;
                    customerInDb.EMAIL = model.EMAIL;
                    customerInDb.DIENTHOAI = model.DIENTHOAI;
                    customerInDb.PASSWORD = model.PASSWORD; // Có thể thêm mã hóa nếu cần

                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Cập nhật thông tin khách hàng thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi hệ thống: " + ex.Message;
                }
            }

            return View(model);
        }

        // ==========================================
        // 4. XÓA KHÁCH HÀNG
        // ==========================================
        public ActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return HttpNotFound();
            }

            var customer = db.CUSTOMERs.Find(id);
            if (customer != null)
            {
                try
                {
                    // Xóa địa chỉ liên quan trước
                    var addresses = db.ADDRESSes.Where(a => a.KHACHHANGID == id).ToList();
                    if (addresses.Any())
                    {
                        db.ADDRESSes.RemoveRange(addresses);
                    }

                    // Xóa khách hàng
                    db.CUSTOMERs.Remove(customer);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Xóa khách hàng thành công!";
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException)
                {
                    // Nếu khách hàng đã có đơn hàng → Không thể xóa
                    TempData["ErrorMessage"] = "Không thể xóa khách hàng này vì đã có đơn hàng liên quan!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi hệ thống: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
            }

            return RedirectToAction("Index");
        }
    }
}