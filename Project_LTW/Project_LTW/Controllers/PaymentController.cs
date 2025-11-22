using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_LTW.Models;
using System.Data.Entity;

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

            var userInSession = Session["User"] as CUSTOMER;
            // PHẢI INCLUDE ADDRESSES NGAY TỪ ĐÂY ĐỂ TRUYỀN XUỐNG VIEW
            var fullUser = db.CUSTOMERs.Include(c => c.ADDRESSes).FirstOrDefault(c => c.KHACHHANGID == userInSession.KHACHHANGID);

            ViewBag.Cart = cart.list;
            ViewBag.User = fullUser; // Truyền fullUser xuống để lấy thông tin
            ViewBag.Addresses = fullUser?.ADDRESSes.ToList(); // TRUYỀN DANH SÁCH ĐỊA CHỈ
            ViewBag.Total = cart.TongTien();

            return View();
        }


        // 2. POST: Xử lý khi bấm nút "Xác nhận đặt hàng"
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Bổ sung tham số PaymentMethod
        public ActionResult Index(string ShipName, string ShipPhone, string ShipAddress, string Note, string addressOption, string PaymentMethod)
        {
            // 1. Chuẩn bị dữ liệu hiển thị lại nếu lỗi
            var cartInfo = Session["Cart"] as Cart;
            var userInSession = Session["User"] as CUSTOMER;

            // Tải lại fullUser và Addresses nếu cần hiển thị lại trang
            var fullUser = db.CUSTOMERs.Include(c => c.ADDRESSes).FirstOrDefault(c => c.KHACHHANGID == userInSession.KHACHHANGID);

            ViewBag.Cart = cartInfo?.list;
            if (cartInfo != null) ViewBag.Total = cartInfo.TongTien();
            ViewBag.User = fullUser;
            ViewBag.Addresses = fullUser?.ADDRESSes.ToList();

            // 2. Validate
            if (string.IsNullOrEmpty(ShipName) || string.IsNullOrEmpty(ShipPhone))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Họ tên và Điện thoại.";
                return View();
            }

            // --- Validate Phương thức Thanh toán ---
            if (string.IsNullOrEmpty(PaymentMethod))
            {
                ViewBag.Error = "Vui lòng chọn phương thức thanh toán.";
                return View();
            }

            string finalShipAddressContent = ShipAddress;
            string finalAddressID = null;

            // --- LOGIC XÁC ĐỊNH ĐỊA CHỈ TỪ CODE TRƯỚC (Giữ nguyên) ---
            if (!string.IsNullOrEmpty(addressOption) && addressOption.StartsWith("saved_"))
            {
                finalAddressID = addressOption.Replace("saved_", "");
                var savedAddr = fullUser?.ADDRESSes.FirstOrDefault(a => a.DIACHIID == finalAddressID);

                if (savedAddr == null)
                {
                    ViewBag.Error = "Địa chỉ đã chọn không hợp lệ. Vui lòng chọn lại.";
                    return View();
                }
            }

            if (string.IsNullOrEmpty(finalShipAddressContent))
            {
                ViewBag.Error = "Vui lòng nhập hoặc chọn địa chỉ giao hàng.";
                return View();
            }
            // ---------------------------------------

            // ... (logic kiểm tra giỏ hàng - bạn tự thêm vào nếu chưa có) ...

            // 3. BẮT ĐẦU TRANSACTION
            using (var scope = db.Database.BeginTransaction())
            {
                try
                {
                    // BƯỚC 1: TẠO VÀ LƯU BẢNG ADDRESS (CHỈ KHI LÀ ĐỊA CHỈ MỚI) (Giữ nguyên)
                    if (finalAddressID == null)
                    {
                        string newAddressID = "DC" + DateTime.Now.Ticks.ToString().Substring(10);
                        ADDRESS newAddr = new ADDRESS();
                        newAddr.DIACHIID = newAddressID;
                        newAddr.KHACHHANGID = userInSession.KHACHHANGID;
                        newAddr.DUONG = finalShipAddressContent;
                        newAddr.THANHPHO = "Toàn Quốc";
                        newAddr.TINH = "Việt Nam";
                        newAddr.ZIPCODE = "70000";

                        db.ADDRESSes.Add(newAddr);
                        db.SaveChanges();
                        finalAddressID = newAddressID;
                    }

                    // BƯỚC 2: TẠO ORDER (Giữ nguyên)
                    ORDER newOrder = new ORDER();
                    string orderId = "DH" + DateTime.Now.Ticks.ToString().Substring(10);
                    newOrder.ORDERID = orderId;
                    newOrder.KHACHHANGID = userInSession.KHACHHANGID;
                    newOrder.NGAYDAT = DateTime.Now;
                    newOrder.TONGTIEN = cartInfo.TongTien();
                    newOrder.TRANGTHAI = "Chờ xử lý";
                    newOrder.DIACHIID = finalAddressID;
                    newOrder.MANV_XULY = null;

                    string fullNote = $"{ShipName} - {ShipPhone}";
                    if (!string.IsNullOrEmpty(Note)) fullNote += $" | Note: {Note}";
                    newOrder.GHICHU = fullNote.Length > 490 ? fullNote.Substring(0, 490) : fullNote;

                    db.ORDERS.Add(newOrder);
                    db.SaveChanges();

                    // BƯỚC 3: LƯU CHI TIẾT ĐƠN HÀNG & CẬP NHẬT TỒN KHO (Giữ nguyên)
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
                    // BƯỚC 4: LƯU THANH TOÁN (PAYMENT) - CẬP NHẬT
                    // ==========================================================
                    PAYMENT pay = new PAYMENT();
                    pay.PAYMENTID = "PM" + DateTime.Now.Ticks.ToString().Substring(10);
                    pay.ORDERID = newOrder.ORDERID;

                    // Gán Phương thức thanh toán đã chọn từ View
                    pay.PHUONGTHUCTT = PaymentMethod;

                    // Xác định trạng thái thanh toán ban đầu
                    if (PaymentMethod == "COD")
                    {
                        pay.TRANGTHAITT = "Chưa thanh toán";
                        pay.NGAYTT = null; // Ngày thanh toán null cho COD
                    }
                    else // Các phương thức khác (chuyển khoản)
                    {
                        pay.TRANGTHAITT = "Chờ xác nhận";
                        pay.NGAYTT = DateTime.Now; // Ghi nhận thời điểm yêu cầu thanh toán
                    }

                    db.PAYMENTs.Add(pay);
                    db.SaveChanges();

                    // ==========================================================
                    // HOÀN TẤT
                    // ==========================================================
                    scope.Commit();
                    Session["Cart"] = null;
                    TempData["SuccessMessage"] = "Đặt hàng thành công! Mã đơn: " + newOrder.ORDERID;
                    return RedirectToAction("Success", "Payment");
                }
                // ... (Xử lý Catch Error giữ nguyên) ...
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