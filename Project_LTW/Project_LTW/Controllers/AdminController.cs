using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Project_LTW.Controllers
{
    public class AdminController : Controller
    {

        FashionWebEntities db = new FashionWebEntities();

        // ==================== CREATE (GET) ====================
        public ActionResult Create()
        {
            ViewBag.CategoryList = new SelectList(db.CATEGORies.ToList(),
                                                  "DANHMUCID",
                                                  "TENDANHMUC");
            return View();
        }

        // ==================== CREATE (POST) ====================
        [HttpPost]
        public ActionResult Create(PRODUCT p, HttpPostedFileBase Image)
        {
            if (ModelState.IsValid)
            {
                // ===== 1. Tạo ID cho sản phẩm =====
                p.SANPHAMID = "SP" + DateTime.Now.Ticks.ToString();  // hoặc tự tạo theo quy tắc riêng

                string fileName = "";
                string dir = "/assets/";
                string physicalDir = Server.MapPath(dir);

                // ===== 2. Lưu ảnh đại diện =====
                if (Image != null && Image.ContentLength > 0)
                {
                    if (!Directory.Exists(physicalDir))
                        Directory.CreateDirectory(physicalDir);

                    fileName = Path.GetFileName(Image.FileName);
                    string path = Path.Combine(physicalDir, fileName);
                    Image.SaveAs(path);

                    // Lưu đường dẫn vào PRODUCT (HINHANHDAIDIEN)
                    p.HINHANHDAIDIEN = dir + fileName;
                }

                // ===== 3. Lưu sản phẩm =====
                db.PRODUCTs.Add(p);
                db.SaveChanges();

                // ===== 4. Lưu vào PRODUCT_IMAGE =====
                if (!string.IsNullOrEmpty(fileName))
                {
                    PRODUCT_IMAGE img = new PRODUCT_IMAGE();
                    img.SANPHAMID = p.SANPHAMID;
                    img.MAUSAC = null;           // nếu không có màu
                    img.TENHINH = fileName;      // DB yêu cầu TENHINH không phải URL

                    db.PRODUCT_IMAGE.Add(img);
                    db.SaveChanges();
                }

                return RedirectToAction("Product", "Home");
            }

            // Load lại category nếu lỗi
            ViewBag.CategoryList = new SelectList(db.CATEGORies.ToList(),
                                                  "DANHMUCID",
                                                  "TENDANHMUC",
                                                  p.DANHMUCID);

            return View(p);
        }


    }
}