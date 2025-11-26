using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Project_LTW.Areas.Admin.Controllers;



namespace Project_LTW.Areas.Admin.Controllers
{
    [CheckAdmin]
    public class HomeAdminController : Controller
    {
        // GET: Admin/HomeAdmin
  
            FashionWebEntities db = new FashionWebEntities();

        
        // GET: Admin/Home
        public ActionResult Index()
        {
            
            ViewBag.SoLuongSanPham = db.PRODUCTs.Count();
            ViewBag.DonHangMoi = db.ORDERS.Where(x => x.TRANGTHAI == "Chờ xử lý" || x.TRANGTHAI == "Chờ xác nhận").Count();
            ViewBag.SoLuongKhachHang = db.CUSTOMERs.Count();
            ViewBag.SoLuongDanhMuc = db.CATEGORies.Count();

            return View();
        }
        public ActionResult Logout()
        {
          
            Session["AdminUser"] = null;
                     return RedirectToAction("Index", "Home", new { area = "" });
        }

    }
}