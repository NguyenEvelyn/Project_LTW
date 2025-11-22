using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Project_LTW.Areas.Admin.Controllers;



namespace Project_LTW.Areas.Admin.Controllers
{
    [CheckAdmin] // <--- THÊM DÒNG NÀY
    public class HomeAdminController : Controller
    {
        // GET: Admin/HomeAdmin
  
            FashionWebEntities db = new FashionWebEntities();

        
        // GET: Admin/Home
        public ActionResult Index()
        {
            // 1. Thống kê số lượng cho các thẻ 
            ViewBag.SoLuongSanPham = db.PRODUCTs.Count();

            // Đếm đơn hàng mới (Giả sử trạng thái là 'Chờ xử lý' hoặc 'Chờ xác nhận')
            ViewBag.DonHangMoi = db.ORDERS.Where(x => x.TRANGTHAI == "Chờ xử lý" || x.TRANGTHAI == "Chờ xác nhận").Count();

            ViewBag.SoLuongKhachHang = db.CUSTOMERs.Count();

          

            // 2. Thống kê cho Info Box 
            ViewBag.SoLuongDanhMuc = db.CATEGORies.Count();

            

            return View();
        }
        public ActionResult Logout()
        {
            // 1. Xóa Session Admin
            Session["AdminUser"] = null;
            // 2. Chuyển hướng về trang chủ bán hàng (Home/Index)
            return RedirectToAction("Index", "Home", new { area = "" });
        }

    }
}