using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;


namespace Project_LTW.Controllers
{
    public class HomeController : Controller
    {
        FashionWebEntities db = new FashionWebEntities();
        public ActionResult Product()
        {

            var list = db.PRODUCTs.ToList(); 
            return View(list);
         }
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

        
        public ActionResult TimKiemCategory(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index");
            }

          
            string trimmedId = id.Trim();

      
            List<PRODUCT> list = db.PRODUCTs
                .Where(x => x.DANHMUCID == trimmedId && x.SOLUONGTONKHO > 0) 
                .ToList();

            var category = db.CATEGORies.FirstOrDefault(c => c.DANHMUCID == trimmedId);
            ViewBag.CategoryName = category?.TENDANHMUC ?? "Danh mục";

            if (list == null || list.Count == 0)
            {
                ViewBag.ThongBao = "Không có sản phẩm nào thuộc chủ đề này.";
            }

            
            return View("Product", list);
        }
      
        public ActionResult Index()
        {
      
            var topDM001Products = db.PRODUCTs
             
                .Where(p => p.DANHMUCID == "DM001" && p.SOLUONGTONKHO > 0)
                               .OrderByDescending(p => p.SANPHAMID)
                .Take(2); 
                       var topDM003Products = db.PRODUCTs
                               .Where(p => p.DANHMUCID == "DM003" && p.SOLUONGTONKHO > 0)
                               .OrderByDescending(p => p.SANPHAMID)
                .Take(2); 

            ViewBag.MainApparelProducts = topDM001Products; 
            ViewBag.AccessoryProducts = topDM003Products;   

            return View();

        }
        public ActionResult TimKiemTheoTuKhoa(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
         
                return RedirectToAction("Index");
            }

           
            string tukhoaBoDau = RemoveDiacritics(keyword.Trim().ToLower());

            
            List<PRODUCT> list = db.PRODUCTs.ToList();

            
            list = list.FindAll(x =>
                RemoveDiacritics(x.TENSANPHAM.ToLower()).Contains(tukhoaBoDau)
            );

            ViewBag.Keyword = keyword;
            return View("Product", list);
        }


        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }




    }
}