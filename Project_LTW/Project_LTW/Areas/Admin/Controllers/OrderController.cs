//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using System.Web.Mvc;
//using Project_LTW.Models; // Đảm bảo đã có Model
//using System.Data.Entity; // Cần thiết cho EntityState

//namespace Project_LTW.Areas.Admin.Controllers
//{
//    public class OrderController : Controller
//    {
//        FashionWebEntities db = new FashionWebEntities();

//        // ==========================================
//        // 1. GET: Danh sách đơn hàng
//        // ==========================================
//        public ActionResult Index()
//        {
//            var orders = db.ORDERS.OrderByDescending(o => o.NGAYDAT).ToList();
//            return View(orders);
//        }

//        // ==========================================
//        // 2. GET: XEM CHI TIẾT ĐƠN HÀNG
//        // ==========================================
//        public ActionResult Details(string id)
//        {
//            // Cần Include các bảng liên quan để hiển thị đầy đủ
//            var order = db.ORDERS
//                .Include(o => o.ORDERDETAILs.Select(od => od.PRODUCT))
//                .Include(o => o.CUSTOMER)
//                .Include(o => o.ADDRESS)
//                .Include(o => o.PAYMENTs)
//                .FirstOrDefault(o => o.ORDERID == id);

//            if (order == null) return HttpNotFound();
//            return View(order);
//        }

//        // ==========================================
//        // 3. POST: CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG (TRANGTHAI)
//        // ==========================================
//        [HttpPost]
//        public ActionResult UpdateStatus(string id, string trangThai)
//        {
//            var order = db.ORDERS.Find(id);
//            if (order != null)
//            {
//                order.TRANGTHAI = trangThai;

//                // Nếu đơn hàng HOÀN TẤT ("Đã giao"), tự động cập nhật TT thanh toán
//                if (trangThai == "Đã giao" && order.PAYMENTs.Any())
//                {
//                    foreach (var pay in order.PAYMENTs)
//                    {
//                        if (pay.TRANGTHAITT != "Đã thanh toán")
//                        {
//                            pay.TRANGTHAITT = "Đã thanh toán";
//                            pay.NGAYTT = DateTime.Now; // Gán ngày thanh toán là ngày đơn hàng hoàn tất
//                        }
//                    }
//                }

//                db.SaveChanges();
//            }
//            return RedirectToAction("Details", new { id = id });
//        }

//        // ==========================================
//        // 4. POST: CẬP NHẬT TRẠNG THÁI THANH TOÁN (TRANGTHAITT)
//        // ==========================================
//        [HttpPost]
//        public ActionResult ConfirmPayment(string orderId)
//        {
//            var payment = db.PAYMENTs.FirstOrDefault(p => p.ORDERID == orderId);
//            var order = db.ORDERS.Find(orderId);

//            if (payment != null)
//            {
//                // Chỉ cập nhật nếu trạng thái chưa phải là "Đã thanh toán"
//                if (payment.TRANGTHAITT != "Đã thanh toán")
//                {
//                    payment.TRANGTHAITT = "Đã thanh toán";
//                    payment.NGAYTT = DateTime.Now; // Ghi nhận thời điểm xác nhận thanh toán

//                    db.Entry(payment).State = EntityState.Modified;

//                    // Nếu thanh toán được xác nhận, chuyển trạng thái đơn hàng sang "Đã xác nhận" (hoặc logic tùy chỉnh)
//                    if (order != null && order.TRANGTHAI == "Chờ xử lý")
//                    {
//                        order.TRANGTHAI = "Đã xác nhận";
//                        db.Entry(order).State = EntityState.Modified;
//                    }

//                    db.SaveChanges();
//                    TempData["SuccessMessage"] = "Xác nhận thanh toán thành công!";
//                }
//            }
//            else
//            {
//                TempData["ErrorMessage"] = "Không tìm thấy thông tin thanh toán.";
//            }

//            return RedirectToAction("Details", new { id = orderId });
//        }
//    }
//}

using System;
using System.Linq;
using System.Web.Mvc;
using Project_LTW.Models;
using System.Data.Entity; // Cần thiết cho EntityState
using Project_LTW.Areas.Admin.Controllers;



namespace Project_LTW.Areas.Admin.Controllers
{
    [CheckAdmin] // <--- THÊM DÒNG NÀY
    public class OrderController : Controller
    {
        FashionWebEntities db = new FashionWebEntities();

        // ==========================================
        // 1. GET: Danh sách đơn hàng
        // ==========================================
        public ActionResult Index()
        {
            var orders = db.ORDERS.OrderByDescending(o => o.NGAYDAT).ToList();
            return View(orders);
        }

        // ==========================================
        // 2. GET: XEM CHI TIẾT ĐƠN HÀNG
        // ==========================================
        public ActionResult Details(string id)
        {
            var order = db.ORDERS
                .Include(o => o.ORDERDETAILs.Select(od => od.PRODUCT))
                .Include(o => o.CUSTOMER)
                .Include(o => o.ADDRESS)
                .Include(o => o.PAYMENTs)
                .FirstOrDefault(o => o.ORDERID == id);

            if (order == null) return HttpNotFound();
            return View(order);
        }

        // ==========================================
        // 3. POST: CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG (TRANGTHAI)
        // ==========================================
        [HttpPost]
        public ActionResult UpdateStatus(string id, string trangThai)
        {
            var order = db.ORDERS.Find(id);
            string currentManv = Session["MANV"] as string; // Lấy MANV từ Session

            if (order != null)
            {
                // GÁN MANV_XULY NẾU NÓ ĐANG LÀ NULL VÀ CÓ MANV ĐĂNG NHẬP
                if (order.MANV_XULY == null && !string.IsNullOrEmpty(currentManv))
                {
                    order.MANV_XULY = currentManv;
                }

                order.TRANGTHAI = trangThai;

                // Nếu đơn hàng HOÀN TẤT ("Đã giao"), tự động cập nhật TT thanh toán
                if (trangThai == "Đã giao" && order.PAYMENTs.Any())
                {
                    foreach (var pay in order.PAYMENTs)
                    {
                        if (pay.TRANGTHAITT != "Đã thanh toán")
                        {
                            pay.TRANGTHAITT = "Đã thanh toán";
                            pay.NGAYTT = DateTime.Now;
                        }
                    }
                }

                db.SaveChanges();
            }
            return RedirectToAction("Details", new { id = id });
        }

        // ==========================================
        // 4. POST: CẬP NHẬT TRẠNG THÁI THANH TOÁN (TRANGTHAITT)
        // ==========================================
        [HttpPost]
        public ActionResult ConfirmPayment(string orderId)
        {
            var payment = db.PAYMENTs.FirstOrDefault(p => p.ORDERID == orderId);
            var order = db.ORDERS.Find(orderId);
            string currentManv = Session["MANV"] as string; // Lấy MANV từ Session

            if (payment != null)
            {
                // GÁN MANV_XULY NẾU NÓ ĐANG LÀ NULL VÀ CÓ MANV ĐĂNG NHẬP
                if (order != null && order.MANV_XULY == null && !string.IsNullOrEmpty(currentManv))
                {
                    order.MANV_XULY = currentManv;
                    // Không cần EntryState.Modified vì db.SaveChanges() sẽ cập nhật toàn bộ thay đổi
                }

                // Chỉ cập nhật nếu trạng thái chưa phải là "Đã thanh toán"
                if (payment.TRANGTHAITT != "Đã thanh toán")
                {
                    payment.TRANGTHAITT = "Đã thanh toán";
                    payment.NGAYTT = DateTime.Now;

                    db.Entry(payment).State = EntityState.Modified;

                    // Nếu thanh toán được xác nhận, chuyển trạng thái đơn hàng sang "Đã xác nhận"
                    if (order != null && order.TRANGTHAI == "Chờ xử lý")
                    {
                        order.TRANGTHAI = "Đã xác nhận";
                        db.Entry(order).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Xác nhận thanh toán thành công!";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin thanh toán.";
            }

            return RedirectToAction("Details", new { id = orderId });
        }
    }
}
