using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Project_LTW.Areas.Admin.Controllers
{
    public class OrderController : Controller
    {
        // GET: Admin/Order
        FashionWebEntities db = new FashionWebEntities();
        public ActionResult Index()
        {
            var orders = db.ORDERS.OrderByDescending(o => o.NGAYDAT).ToList();
            return View(orders);
        }
       


        //  XEM CHI TIẾT ĐƠN HÀNG
        public ActionResult Details(string id)
        {
            var order = db.ORDERS.Find(id);
            if (order == null) return HttpNotFound();
            return View(order);
        }

        //  CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG
        [HttpPost]
        public ActionResult UpdateStatus(string id, string trangThai)
        {
            var order = db.ORDERS.Find(id);
            if (order != null)
            {
                order.TRANGTHAI = trangThai;

                // Nếu đơn hàng hoàn tất, cập nhật trạng thái thanh toán luôn (nếu cần)
                if (trangThai == "Đã giao" && order.PAYMENTs.Any())
                {
                    foreach (var pay in order.PAYMENTs) pay.TRANGTHAITT = "Đã thanh toán";
                }

                db.SaveChanges();
            }
            return RedirectToAction("Details", new { id = id });
        }
    }
}
