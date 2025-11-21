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
        // 1. GET: Hiển thị trang thanh toán
        // ==========================================
        [HttpGet]
        public ActionResult Index()
        {
            if (Session["User"] == null)
                return RedirectToAction("Login", "User");

            var cart = Session["Cart"] as Cart;
            if (cart == null || cart.list == null || cart.list.Count == 0)
                return RedirectToAction("Index", "Cart");

            var user = Session["User"] as CUSTOMER;

            ViewBag.Cart = cart.list;
            ViewBag.User = user;
            ViewBag.UserAddress = user?.ADDRESSes;
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
            // 1. Chuẩn bị dữ liệu hiển thị lại nếu lỗi
            var cartInfo = Session["Cart"] as Cart;
            var user = Session["User"] as CUSTOMER;

            ViewBag.Cart = cartInfo?.list;
            if (cartInfo != null) ViewBag.Total = cartInfo.TongTien();
            ViewBag.User = user;
            ViewBag.UserAddress = user?.ADDRESSes;

            // 2. Validate
            if (string.IsNullOrEmpty(ShipName) || string.IsNullOrEmpty(ShipPhone) || string.IsNullOrEmpty(ShipAddress))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }
            if (cartInfo == null || cartInfo.list.Count == 0)
            {
                ViewBag.Error = "Giỏ hàng trống.";
                return View();
            }

            // 3. BẮT ĐẦU TRANSACTION (QUAN TRỌNG)
            using (var scope = db.Database.BeginTransaction())
            {
                try
                {
                    // ==========================================================
                    // BƯỚC 1: TẠO VÀ LƯU BẢNG ADDRESS (Phải làm trước Order)
                    // ==========================================================

                    // Tạo ID ngẫu nhiên không trùng cho Địa chỉ
                    string newAddressID = "DC" + DateTime.Now.Ticks.ToString().Substring(10);

                    ADDRESS newAddr = new ADDRESS();
                    newAddr.DIACHIID = newAddressID;
                    newAddr.KHACHHANGID = user.KHACHHANGID;

                    // Map dữ liệu từ Form vào Database
                    newAddr.DUONG = ShipAddress; // Lưu địa chỉ người dùng nhập vào đây

                    // Vì Form không có nhập Thành phố/Tỉnh/Zipcode, ta gán mặc định để không lỗi DB
                    newAddr.THANHPHO = "Toàn Quốc";
                    newAddr.TINH = "Việt Nam";
                    newAddr.ZIPCODE = "70000";

                    db.ADDRESSes.Add(newAddr);
                    db.SaveChanges(); // <--- LƯU NGAY ĐỂ CÓ MÃ 'DC...' TRONG DB

                    // ==========================================================
                    // BƯỚC 2: TẠO ORDER (Dùng ID địa chỉ vừa tạo)
                    // ==========================================================
                    ORDER newOrder = new ORDER();
                    string orderId = "DH" + DateTime.Now.Ticks.ToString().Substring(10);
                    newOrder.ORDERID = orderId;
                    newOrder.KHACHHANGID = user.KHACHHANGID;
                    newOrder.NGAYDAT = DateTime.Now;
                    newOrder.TONGTIEN = cartInfo.TongTien();
                    newOrder.TRANGTHAI = "Chờ xử lý";

                    // --- QUAN TRỌNG: GÁN KHÓA NGOẠI ---
                    newOrder.DIACHIID = newAddressID; // Lấy ID "DC..." ở trên gán vào đây

                    // Lưu ý: Cột MANV_XULY trong DB phải cho phép NULL
                    newOrder.MANV_XULY = null;

                    string fullNote = $"{ShipName} - {ShipPhone}";
                    if (!string.IsNullOrEmpty(Note)) fullNote += $" | Note: {Note}";
                    newOrder.GHICHU = fullNote.Length > 490 ? fullNote.Substring(0, 490) : fullNote;

                    db.ORDERS.Add(newOrder);
                    db.SaveChanges(); // Lưu Order

                    // ==========================================================
                    // BƯỚC 3: LƯU CHI TIẾT ĐƠN HÀNG
                    // ==========================================================
                    foreach (var item in cartInfo.list)
                    {
                        ORDERDETAIL detail = new ORDERDETAIL();
                        detail.ORDERID = newOrder.ORDERID;
                        detail.SANPHAMID = item.MASP;
                        detail.SOLUONG = item.SoLuong;
                        detail.DONGIA = item.DonGia;
                        detail.THANHTIEN = item.ThanhTien;
                        db.ORDERDETAILs.Add(detail);
                    }
                    db.SaveChanges();
                    db.SP_CAPNHATTONKHOSAUDATHANG(newOrder.ORDERID);

                    // ==========================================================
                    // BƯỚC 4: LƯU THANH TOÁN (PAYMENT)
                    // ==========================================================
                    PAYMENT pay = new PAYMENT();
                    pay.PAYMENTID = "PM" + DateTime.Now.Ticks.ToString().Substring(10);
                    pay.ORDERID = newOrder.ORDERID;
                    pay.PHUONGTHUCTT = "COD";
                    pay.TRANGTHAITT = "Chưa thanh toán";
                    pay.NGAYTT = DateTime.Now;

                    db.PAYMENTs.Add(pay);
                    db.SaveChanges();

                    // ==========================================================
                    // HOÀN TẤT
                    // ==========================================================
                    scope.Commit(); // Xác nhận lưu tất cả

                    Session["Cart"] = null;
                    TempData["SuccessMessage"] = "Đặt hàng thành công! Mã đơn: " + newOrder.ORDERID;
                    return RedirectToAction("Success", "Payment");
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    scope.Rollback();
                    string msg = "";
                    foreach (var item in dbEx.EntityValidationErrors)
                        foreach (var err in item.ValidationErrors) msg += $"Lỗi {err.PropertyName}: {err.ErrorMessage}<br/>";
                    ViewBag.Error = "Lỗi dữ liệu: " + msg;
                    return View();
                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    Exception real = ex;
                    while (real.InnerException != null) real = real.InnerException;
                    ViewBag.Error = "Lỗi hệ thống: " + real.Message;
                    return View();
                }
            }
        }
        // ==========================================
        // 3. Trang thông báo thành công
        // ==========================================
        public ActionResult Success()
        {
            if (TempData["SuccessMessage"] == null) return RedirectToAction("Index", "Home");
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View();
        }
 
        
    }
}