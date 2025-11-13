using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Project_LTW.Controllers
{
    public class HomeController : Controller
    {
        FashionWebEntities db = new FashionWebEntities();
        public ActionResult Index()
        {

            var list = db.PRODUCTs.ToList();   // lấy dữ liệu từ DB
            return View(list);
            
        }

        // Trong file: HomeController.cs

        public ActionResult Details(string id) 
        {
           
            var product = db.PRODUCTs.Find(id);

      
            if (product == null)
            {
                return HttpNotFound();
            }

            
            ViewBag.SanPhamLienQuan = db.PRODUCTs
                .Where(p => p.DANHMUCID == product.DANHMUCID && p.SANPHAMID != id)
                .Take(4)
                .ToList();

            return View(product);
        }

    }
}