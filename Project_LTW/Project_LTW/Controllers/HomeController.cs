using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace Project_LTW.Controllers
{
    public class HomeController : Controller
    {
        FashionWebEntities db = new FashionWebEntities();
        public ActionResult Product()
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

        public ActionResult _Category()
        {
            var model = db.CATEGORies.ToList();
            return PartialView(model);
        }

        // GET: Tìm kiếm sản phẩm theo danh mục - ĐÃ SỬA
        public ActionResult TimKiemCategory(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index");
            }

            // THÊM DÒNG NÀY:
            string trimmedId = id.Trim();

            // Dùng 'trimmedId' cho tất cả các truy vấn bên dưới
            List<PRODUCT> list = db.PRODUCTs
                .Where(x => x.DANHMUCID == trimmedId && x.SOLUONGTONKHO > 0) // Sửa ở đây
                .ToList();

            var category = db.CATEGORies.FirstOrDefault(c => c.DANHMUCID == trimmedId); // Sửa ở đây
            ViewBag.CategoryName = category?.TENDANHMUC ?? "Danh mục";

            if (list == null || list.Count == 0)
            {
                ViewBag.ThongBao = "Không có sản phẩm nào thuộc chủ đề này.";
            }

            // Dùng lại View Index.cshtml
            return View("Product", list);
        }
        // trang homepage 
        public ActionResult Index()
        {
            // 1. Lấy TOP 3 sản phẩm thuộc danh mục DM001 
            var topDM001Products = db.PRODUCTs
                // Lọc theo DM001 và đảm bảo còn hàng
                .Where(p => p.DANHMUCID == "DM001" && p.SOLUONGTONKHO > 0)
                // Sắp xếp để lấy các sản phẩm nổi bật/mới nhất
                .OrderByDescending(p => p.SANPHAMID)
                .Take(2); // Lấy 2 sản phẩm

            // 2. Lấy TOP 2 sản phẩm thuộc danh mục DM003
            var topDM003Products = db.PRODUCTs
                // Lọc theo DM003 và đảm bảo còn hàng
                .Where(p => p.DANHMUCID == "DM003" && p.SOLUONGTONKHO > 0)
                // Sắp xếp để lấy các sản phẩm nổi bật/mới nhất
                .OrderByDescending(p => p.SANPHAMID)
                .Take(2); // Lấy 2 sản phẩm

            ViewBag.MainApparelProducts = topDM001Products; // Dùng cho vị trí 1
            ViewBag.AccessoryProducts = topDM003Products;   // Dùng cho vị trí 2

            return View();

        }


    }
}