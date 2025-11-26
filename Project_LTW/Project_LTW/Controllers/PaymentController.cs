//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using System.Web.Mvc;
//using Project_LTW.Models;
//using System.Data.Entity;

//namespace Project_LTW.Controllers
//{
//    public class PaymentController : Controller
//    {
//        FashionWebEntities db = new FashionWebEntities();

//        private string GetIdSuffix()
//        {
//            string ticks = DateTime.Now.Ticks.ToString();
//            // Đảm bảo luôn lấy 8 ký tự cuối cùng
//            return ticks.Substring(ticks.Length - 8);
//        }
//        // ==========================================
//        // 1. GET: Hiển thị trang thanh toán
//        // ==========================================
//        [HttpGet]
//        public ActionResult Index()
//        {
//            if (Session["User"] == null)
//                return RedirectToAction("Login", "User");

//            var cart = Session["Cart"] as Cart;
//            if (cart == null || cart.list == null || cart.list.Count == 0)
//                return RedirectToAction("Index", "Cart");

//            var userInSession = Session["User"] as CUSTOMER;
//            // PHẢI INCLUDE ADDRESSES NGAY TỪ ĐÂY ĐỂ TRUYỀN XUỐNG VIEW
//            var fullUser = db.CUSTOMERs.Include(c => c.ADDRESSes).FirstOrDefault(c => c.KHACHHANGID == userInSession.KHACHHANGID);

//            ViewBag.Cart = cart.list;
//            ViewBag.User = fullUser; // Truyền fullUser xuống để lấy thông tin
//            ViewBag.Addresses = fullUser?.ADDRESSes.ToList(); // TRUYỀN DANH SÁCH ĐỊA CHỈ
//            ViewBag.Total = cart.TongTien();

//            return View();
//        }


//        // 2. POST: Xử lý khi bấm nút "Xác nhận đặt hàng"
//        // ==========================================
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        // Bổ sung tham số PaymentMethod
//        public ActionResult Index(string ShipName, string ShipPhone, string ShipAddress, string Note, string addressOption, string PaymentMethod)
//        {
//            var cartInfo = Session["Cart"] as Cart;
//            var userInSession = Session["User"] as CUSTOMER;

//            // ********** Bổ sung kiểm tra NULL ở đây **********
//            if (userInSession == null)
//                return RedirectToAction("Login", "User"); // Đảm bảo User có mặt

//            if (cartInfo == null || cartInfo.list == null || cartInfo.list.Count == 0)
//                return RedirectToAction("Index", "Cart"); // Đảm bảo Cart có mặt và có item

//            // 1. Chuẩn bị dữ liệu hiển thị lại nếu lỗi (Không thay đổi)
//            // Tải lại fullUser và Addresses nếu cần hiển thị lại trang
//            var fullUser = db.CUSTOMERs.Include(c => c.ADDRESSes).FirstOrDefault(c => c.KHACHHANGID == userInSession.KHACHHANGID);

//            ViewBag.Cart = cartInfo.list; // Đã kiểm tra null, dùng trực tiếp
//            ViewBag.Total = cartInfo.TongTien(); // Đã kiểm tra null, dùng trực tiếp
//            ViewBag.User = fullUser;
//            ViewBag.Addresses = fullUser?.ADDRESSes.ToList();

//            // 2. Validate
//            if (string.IsNullOrEmpty(ShipName) || string.IsNullOrEmpty(ShipPhone))
//            {
//                ViewBag.Error = "Vui lòng nhập đầy đủ Họ tên và Điện thoại.";
//                return View();
//            }

//            // --- Validate Phương thức Thanh toán ---
//            if (string.IsNullOrEmpty(PaymentMethod))
//            {
//                ViewBag.Error = "Vui lòng chọn phương thức thanh toán.";
//                return View();
//            }

//            string finalShipAddressContent = ShipAddress;
//            string finalAddressID = null;

//            // --- LOGIC XÁC ĐỊNH ĐỊA CHỈ TỪ CODE TRƯỚC (Giữ nguyên) ---
//            if (!string.IsNullOrEmpty(addressOption) && addressOption.StartsWith("saved_"))
//            {
//                finalAddressID = addressOption.Replace("saved_", "");
//                var savedAddr = fullUser?.ADDRESSes.FirstOrDefault(a => a.DIACHIID == finalAddressID);

//                if (savedAddr == null)
//                {
//                    ViewBag.Error = "Địa chỉ đã chọn không hợp lệ. Vui lòng chọn lại.";
//                    return View();
//                }
//            }

//            if (string.IsNullOrEmpty(finalShipAddressContent))
//            {
//                ViewBag.Error = "Vui lòng nhập hoặc chọn địa chỉ giao hàng.";
//                return View(); }
//            using (var scope = db.Database.BeginTransaction())
//            {
//                try
//                {

//                    if (finalAddressID == null)
//                    {
//                        string newAddressID = "DC" + GetIdSuffix();
//                        ADDRESS newAddr = new ADDRESS();
//                        newAddr.DIACHIID = newAddressID;
//                        newAddr.KHACHHANGID = userInSession.KHACHHANGID;
//                        newAddr.DUONG = finalShipAddressContent;
//                        newAddr.THANHPHO = "Toàn Quốc";
//                        newAddr.TINH = "Việt Nam";
//                        newAddr.ZIPCODE = "70000";

//                        db.ADDRESSes.Add(newAddr);
//                        db.SaveChanges();
//                        finalAddressID = newAddressID;
//                    }

//                    //  TẠO ORDER 
//                    ORDER newOrder = new ORDER();
//                    string orderId = "DH" + GetIdSuffix();
//                    newOrder.ORDERID = orderId;
//                    newOrder.KHACHHANGID = userInSession.KHACHHANGID;
//                    newOrder.NGAYDAT = DateTime.Now;
//                    newOrder.TONGTIEN = cartInfo.TongTien();
//                    newOrder.TRANGTHAI = "Chờ xử lý";
//                    newOrder.DIACHIID = finalAddressID;
//                    db.ORDERS.Add(newOrder);
//                    db.SaveChanges();


//                    string fullNote = $"{ShipName} - {ShipPhone}";
//                    if (!string.IsNullOrEmpty(Note)) fullNote += $" | Note: {Note}";
//                    newOrder.GHICHU = fullNote.Length > 490 ? fullNote.Substring(0, 490) : fullNote;

//                    db.ORDERS.Add(newOrder);
//                    db.SaveChanges();


//                    foreach (var item in cartInfo.list)
//                    {
//                        // Kiểm tra nếu có trường nào quan trọng bị NULL/rỗng
//                        if (string.IsNullOrEmpty(item.MAUSAC) || string.IsNullOrEmpty(item.SIZE))
//                        {
//                            // Rollback và báo lỗi cụ thể để người dùng biết
//                            scope.Rollback();
//                            ViewBag.Error = $"Lỗi dữ liệu: Sản phẩm trong giỏ hàng thiếu thông tin Màu sắc hoặc Kích thước. Vui lòng kiểm tra lại giỏ hàng.";
//                            ViewBag.Cart = cartInfo.list;
//                            ViewBag.Total = cartInfo.TongTien();
//                            ViewBag.User = fullUser;
//                            ViewBag.Addresses = fullUser?.ADDRESSes.ToList();
//                            return View();
//                        }

//                        ORDERDETAIL detail = new ORDERDETAIL();
//                        detail.ORDERID = newOrder.ORDERID;
//                        detail.SANPHAMID = item.MASP;

//                        // Gán giá trị, đảm bảo không NULL/rỗng
//                        detail.MAUSAC = item.MAUSAC;
//                        detail.SIZE = item.SIZE;

//                        detail.SOLUONG = item.SoLuong;
//                        detail.DONGIA = item.DonGia;
//                        db.ORDERDETAILs.Add(detail);
//                    }
//                    db.SaveChanges(); // Lệnh này sẽ chạy sau khi vòng lặp hoàn tất
//                                      // File: PaymentController.cs (Trong khối try, sau khi đã thêm ORDERDETAIL và gọi db.SaveChanges() lần đầu)

//                    // 1. Lấy tất cả chi tiết đơn hàng (ORDERDETAIL) của đơn hàng mới.
//                    var orderDetails = db.ORDERDETAILs
//                                         .Where(od => od.ORDERID == newOrder.ORDERID)
//                                         .ToList();

//                    // 2. Lặp qua từng chi tiết đơn hàng để trừ tồn kho.
//                    foreach (var detail in orderDetails)
//                    {
//                        // Tìm sản phẩm tương ứng trong bảng PRODUCT
//                        // Sử dụng Find() để tìm kiếm theo khóa chính (SANPHAMID)
//                        var product = db.PRODUCTs.Find(detail.SANPHAMID);

//                        if (product != null)
//                        {
//                            // Trừ số lượng tồn kho (SOLUONGTONKHO) đi số lượng đã bán (SOLUONG)
//                            product.SOLUONGTONKHO -= detail.SOLUONG;

//                            // **Quan trọng:** Bạn không cần phải gọi db.Update(product) 
//                            // vì Entity Framework đã theo dõi (Track) đối tượng product này.
//                        }
//                    }
//                    // *** LỆNH THIẾU CẦN THÊM VÀO ***
//                    db.SaveChanges(); // <-- THÊM LỆNH NÀY! LƯU CẬP NHẬT TỒN KHO.

//                    // 3. Sau khi cập nhật tồn kho (và trước khi thêm PAYMENT), bạn cần gọi db.SaveChanges() 
//                    // để lưu những thay đổi này vào database.
//                    // Sau đó mới tiếp tục thêm PAYMENT và gọi scope.Complete().


//                    PAYMENT pay = new PAYMENT();
//                    pay.PAYMENTID = "PM" + GetIdSuffix();
//                    pay.ORDERID = newOrder.ORDERID; // Dùng ID 10 ký tự đã tạo


//                    pay.PHUONGTHUCTT = PaymentMethod;


//                    if (PaymentMethod == "COD")
//                    {
//                        pay.TRANGTHAITT = "Chưa thanh toán";
//                        pay.NGAYTT = null; 
//                    }
//                    else 
//                    {
//                        pay.TRANGTHAITT = "Chờ xác nhận";
//                        pay.NGAYTT = DateTime.Now;
//                    }

//                    db.PAYMENTs.Add(pay);
//                    db.SaveChanges();


//                    scope.Commit();
//                    Session["Cart"] = null;
//                    TempData["SuccessMessage"] = "Đặt hàng thành công! Mã đơn: " + newOrder.ORDERID;
//                    return RedirectToAction("Success", "Payment");
//                }

//                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
//                {
//                    scope.Rollback();
//                    string msg = "";
//                    foreach (var item in dbEx.EntityValidationErrors)
//                        foreach (var err in item.ValidationErrors) msg += $"Lỗi {err.PropertyName}: {err.ErrorMessage}<br/>";
//                    ViewBag.Error = "Lỗi dữ liệu: " + msg;
//                    return View();
//                }
//                // PaymentController.cs, thay thế khối catch cũ
//                catch (Exception ex)
//                {
//                    string innerMessage = "";

//                    // 1. Thử Rollback giao dịch trước
//                    try
//                    {
//                        // Kiểm tra xem scope (giao dịch) còn hoạt động không và thực hiện Rollback
//                        // Nếu giao dịch đã bị lỗi quá nặng và mất kết nối, Rollback() sẽ gây ra lỗi 
//                        // ArgumentNullException: connection. Khối catch(rollbackEx) sẽ bắt nó.
//                        scope.Rollback();
//                    }
//                    catch (Exception rollbackEx)
//                    {
//                        // Ghi lại lỗi Rollback (ví dụ: in ra cửa sổ Output hoặc Log file) 
//                        // Dòng này giúp bạn biết Rollback đã thất bại vì lỗi kết nối
//                        // Console.WriteLine($"Rollback Failed: {rollbackEx.Message}"); 
//                    }

//                    // 2. Xử lý lỗi chính để hiển thị cho người dùng
//                    Exception real = ex;
//                    while (real.InnerException != null)
//                    {
//                        real = real.InnerException;
//                    }
//                    innerMessage = real.Message;

//                    // Nếu lỗi chính là EntityValidationException (lỗi ràng buộc dữ liệu), 
//                    // chúng ta đã xử lý nó ở catch trên.
//                    if (ex is System.Data.Entity.Core.EntityException ||
//                        ex is System.Data.Entity.Infrastructure.DbUpdateException)
//                    {
//                        ViewBag.Error = "Lỗi hệ thống: Đã xảy ra lỗi trong quá trình xử lý đơn hàng.";
//                    }
//                    else
//                    {
//                        // Hiển thị lỗi gốc (ví dụ: lỗi SQL từ SP, hoặc lỗi code C# khác)
//                        ViewBag.Error = "Lỗi không xác định: " + innerMessage;
//                    }

//                    return View();
//                }
//            }
//        }


//        public ActionResult Success()
//        {
//            if (TempData["SuccessMessage"] == null) return RedirectToAction("Index", "Home");
//            ViewBag.SuccessMessage = TempData["SuccessMessage"];
//            return View();
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_LTW.Models;
using System.Data.Entity;
using System.Data.Entity.Infrastructure; // Để xử lý DbUpdateException

namespace Project_LTW.Controllers
{
    public class PaymentController : Controller
    {
        FashionWebEntities db = new FashionWebEntities();

        // Hàm giúp tạo phần số của ID, đảm bảo luôn lấy đủ 8 ký tự số.
        private string GetIdSuffix()
        {
            string ticks = DateTime.Now.Ticks.ToString();
            // Lấy 8 ký tự cuối cùng từ Ticks.
            return ticks.Substring(ticks.Length - 8);
        }

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
            var fullUser = db.CUSTOMERs.Include(c => c.ADDRESSes).FirstOrDefault(c => c.KHACHHANGID == userInSession.KHACHHANGID);

            ViewBag.Cart = cart.list;
            ViewBag.User = fullUser;
            ViewBag.Addresses = fullUser?.ADDRESSes.ToList();
            ViewBag.Total = cart.TongTien();

            return View();
        }


        // 2. POST: Xử lý khi bấm nút "Xác nhận đặt hàng"
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string ShipName, string ShipPhone, string ShipAddress, string Note, string addressOption, string PaymentMethod)
        {
            var cartInfo = Session["Cart"] as Cart;
            var userInSession = Session["User"] as CUSTOMER;

            // ********** Kiểm tra bắt buộc **********
            if (userInSession == null) return RedirectToAction("Login", "User");
            if (cartInfo == null || cartInfo.list == null || cartInfo.list.Count == 0) return RedirectToAction("Index", "Cart");

            // Chuẩn bị dữ liệu hiển thị lại nếu lỗi
            var fullUser = db.CUSTOMERs.Include(c => c.ADDRESSes).FirstOrDefault(c => c.KHACHHANGID == userInSession.KHACHHANGID);
            ViewBag.Cart = cartInfo.list;
            ViewBag.Total = cartInfo.TongTien();
            ViewBag.User = fullUser;
            ViewBag.Addresses = fullUser?.ADDRESSes.ToList();

            // 2. Validate
            if (string.IsNullOrEmpty(ShipName) || string.IsNullOrEmpty(ShipPhone))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Họ tên và Điện thoại.";
                return View();
            }

            if (string.IsNullOrEmpty(PaymentMethod))
            {
                ViewBag.Error = "Vui lòng chọn phương thức thanh toán.";
                return View();
            }

            string finalShipAddressContent = ShipAddress;
            string finalAddressID = null;

            // LOGIC XÁC ĐỊNH ĐỊA CHỈ
            if (!string.IsNullOrEmpty(addressOption) && addressOption.StartsWith("saved_"))
            {
                finalAddressID = addressOption.Replace("saved_", "");
                // So sánh trim() cho an toàn với NCHAR(10)
                var savedAddr = fullUser?.ADDRESSes.FirstOrDefault(a => a.DIACHIID.Trim() == finalAddressID.Trim());

                if (savedAddr == null)
                {
                    ViewBag.Error = "Địa chỉ đã chọn không hợp lệ. Vui lòng chọn lại.";
                    return View();
                }
            }

            if (string.IsNullOrEmpty(finalShipAddressContent) && finalAddressID == null)
            {
                ViewBag.Error = "Vui lòng nhập hoặc chọn địa chỉ giao hàng.";
                return View();
            }

            // BẮT ĐẦU GIAO DỊCH DATABASE
            using (var scope = db.Database.BeginTransaction())
            {
                // *** KHAI BÁO VÀ SỬ DỤNG HẬU TỐ CHUNG ĐỂ TRÁNH TRÙNG ID ***
                string transactionSuffix = GetIdSuffix();

                try
                {
                    // 1. TẠO ĐỊA CHỈ MỚI (nếu người dùng chọn nhập mới)
                    if (finalAddressID == null)
                    {
                        // SỬA LỖI ID: Đảm bảo độ dài 10 ký tự (DC + 8 số)
                        string newAddressID = "DC" + transactionSuffix;
                        ADDRESS newAddr = new ADDRESS();
                        newAddr.DIACHIID = newAddressID;
                        newAddr.KHACHHANGID = userInSession.KHACHHANGID;
                        newAddr.DUONG = finalShipAddressContent;
                        // Gán tạm giá trị mặc định cho các cột không được nhập
                        newAddr.THANHPHO = "Toàn Quốc";
                        newAddr.TINH = "Việt Nam";
                        newAddr.ZIPCODE = "70000";

                        db.ADDRESSes.Add(newAddr);
                        finalAddressID = newAddressID; // Cập nhật ID mới
                    }

                    // 2. TẠO ORDER
                    ORDER newOrder = new ORDER();
                    // SỬA LỖI ID: Đảm bảo độ dài 10 ký tự (DH + 8 số)
                    string orderId = "DH" + transactionSuffix;
                    newOrder.ORDERID = orderId;
                    newOrder.KHACHHANGID = userInSession.KHACHHANGID;
                    newOrder.NGAYDAT = DateTime.Now;
                    newOrder.TONGTIEN = cartInfo.TongTien();

                    // Gán trạng thái ban đầu dựa trên phương thức thanh toán
                    if (PaymentMethod == "COD")
                        newOrder.TRANGTHAI = "Chờ xác nhận (COD)";
                    else
                        newOrder.TRANGTHAI = "Chờ thanh toán (CK)";

                    newOrder.DIACHIID = finalAddressID;

                    string fullNote = $"{ShipName} - {ShipPhone}";
                    if (!string.IsNullOrEmpty(Note)) fullNote += $" | Note: {Note}";
                    newOrder.GHICHU = fullNote.Length > 490 ? fullNote.Substring(0, 490) : fullNote;

                    db.ORDERS.Add(newOrder);
                    // Đã bỏ lệnh db.ORDERS.Add(newOrder) bị lặp lại

                    // 3. TẠO ORDER DETAILS VÀ CẬP NHẬT TỒN KHO
                    foreach (var item in cartInfo.list)
                    {
                        if (string.IsNullOrEmpty(item.MAUSAC) || string.IsNullOrEmpty(item.SIZE))
                        {
                            scope.Rollback();
                            ViewBag.Error = $"Lỗi dữ liệu: Sản phẩm {item.TENSP} thiếu thông tin Màu sắc hoặc Kích thước. Vui lòng kiểm tra lại giỏ hàng.";
                            return View();
                        }

                        // Lấy thông tin sản phẩm để cập nhật tồn kho
                        var product = db.PRODUCTs.Find(item.MASP);

                        if (product != null)
                        {
                            // **KIỂM TRA TỒN KHO**
                            if (product.SOLUONGTONKHO < item.SoLuong)
                            {
                                scope.Rollback();
                                ViewBag.Error = $"Lỗi tồn kho: Sản phẩm **{product.TENSANPHAM}** chỉ còn **{product.SOLUONGTONKHO}** sản phẩm. Vui lòng điều chỉnh lại số lượng.";
                                return View();
                            }

                            // Trừ số lượng tồn kho
                            product.SOLUONGTONKHO -= item.SoLuong;
                        }
                        else
                        {
                            scope.Rollback();
                            ViewBag.Error = $"Lỗi hệ thống: Không tìm thấy sản phẩm có mã {item.MASP} trong cơ sở dữ liệu.";
                            return View();
                        }

                        // Thêm chi tiết đơn hàng
                        ORDERDETAIL detail = new ORDERDETAIL();
                        detail.ORDERID = newOrder.ORDERID;
                        detail.SANPHAMID = item.MASP;
                        detail.MAUSAC = item.MAUSAC;
                        detail.SIZE = item.SIZE;
                        detail.SOLUONG = item.SoLuong;
                        detail.DONGIA = item.DonGia;
                        db.ORDERDETAILs.Add(detail);
                    }

                    // 4. LƯU ĐỊA CHỈ, ORDER, ORDERDETAIL và CẬP NHẬT TỒN KHO (Tất cả trong một lệnh)
                    db.SaveChanges();

                    // 5. TẠO PAYMENT
                    PAYMENT pay = new PAYMENT();
                    // SỬA LỖI ID: Đảm bảo độ dài 10 ký tự (PM + 8 số)
                    pay.PAYMENTID = "PM" + transactionSuffix;
                    pay.ORDERID = newOrder.ORDERID;
                    pay.PHUONGTHUCTT = PaymentMethod;

                    if (PaymentMethod == "COD")
                    {
                        pay.TRANGTHAITT = "Chưa thanh toán";
                        pay.NGAYTT = null;
                    }
                    else // Chuyển khoản (CK)
                    {
                        pay.TRANGTHAITT = "Chờ xác nhận";
                        pay.NGAYTT = DateTime.Now;
                    }

                    db.PAYMENTs.Add(pay);
                    db.SaveChanges(); // Lệnh SaveChanges cuối cùng cho PAYMENT


                    // HOÀN TẤT GIAO DỊCH
                    scope.Commit();
                    Session["Cart"] = null;
                    TempData["SuccessMessage"] = "Đặt hàng thành công! Mã đơn: **" + newOrder.ORDERID.Trim() + "**";
                    return RedirectToAction("Success", "Payment");
                }

                // Bắt lỗi Validation (Lỗi ràng buộc do Entity Framework)
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    scope.Rollback();
                    string msg = "";
                    foreach (var item in dbEx.EntityValidationErrors)
                        foreach (var err in item.ValidationErrors) msg += $"Lỗi {err.PropertyName}: {err.ErrorMessage}<br/>";
                    ViewBag.Error = "Lỗi xác thực dữ liệu: " + msg;
                    return View();
                }
                catch (DbUpdateException dbUpdateEx)
                {
                    // === LƯU Ý: ĐÃ XÓA DÒNG scope.Rollback(); TẠI ĐÂY ===
                    // Khi DbUpdateException xảy ra, Transaction thường đã bị hủy ngầm.
                    // Gọi Rollback sẽ gây ra lỗi ArgumentNullException: connection.

                    string innerMessage = "";
                    Exception real = dbUpdateEx;
                    while (real.InnerException != null) real = real.InnerException;
                    innerMessage = real.Message;

                    ViewBag.Error = $"Lỗi cơ sở dữ liệu: Đã xảy ra lỗi trong quá trình ghi dữ liệu (Có thể do trùng ID, lỗi khóa ngoại, hoặc lỗi kết nối). Chi tiết: {innerMessage}";
                    return View();
                }
                // Bắt các lỗi khác (lỗi code, lỗi ngoại lệ hệ thống)
                catch (Exception ex)
                {
                    // Giữ lại Rollback ở khối catch cuối cùng để xử lý các lỗi khác.
                    // Khối này đã được tối ưu hóa để cố gắng Rollback an toàn hơn.
                    string innerMessage = "";

                    try
                    {
                        scope.Rollback();
                    }
                    catch { /* Bỏ qua lỗi Rollback nếu kết nối đã bị đóng */ }

                    Exception real = ex;
                    while (real.InnerException != null)
                    {
                        real = real.InnerException;
                    }
                    innerMessage = real.Message;

                    ViewBag.Error = "Lỗi hệ thống không xác định: " + innerMessage;
                    return View();
                }
            }
        }


        public ActionResult Success()
        {
            if (TempData["SuccessMessage"] == null) return RedirectToAction("Index", "Home");
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View();
        }
    }
}