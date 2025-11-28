using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Project_LTW.Models;
using System.Data.Entity;
using Project_LTW.Areas.Admin.Controllers;


namespace Project_LTW.Areas.Admin.Controllers
{
    [CheckAdmin]
    public class ProductController : Controller
    {
        // GET: Admin/Product
        FashionWebEntities db = new FashionWebEntities();
        public ActionResult Index()
        {
            var items = db.PRODUCTs.OrderBy(x => x.TENSANPHAM).ToList();

            return View(items);
        }

        // Chức năng thêm mới SẢN PHẨM (CREATE)


        [HttpGet]
        public ActionResult Create()
        {
         
            ViewBag.DANHMUCID = new SelectList(db.CATEGORies, "DANHMUCID", "TENDANHMUC");
            return View();
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(PRODUCT model, HttpPostedFileBase UploadImage, string MainImageColor, List<HttpPostedFileBase> SubImageFiles, string[] SubImageColors, string strSIZE)
        {
            try
            {
            
                model.SANPHAMID = "SP" + DateTime.Now.Ticks.ToString().Substring(10);
                string mainImageFilename = "default.png";

                if (UploadImage != null && UploadImage.ContentLength > 0)
                {
                    string filename = System.IO.Path.GetFileName(UploadImage.FileName);
                    string path = Server.MapPath("~/assets/" + filename);
                    UploadImage.SaveAs(path);
                    mainImageFilename = filename;
                }
                model.HINHANHDAIDIEN = mainImageFilename;

                if (ModelState.IsValid)
                {
             
                    db.PRODUCTs.Add(model);

                    
                    HashSet<string> allUniqueColors = new HashSet<string>();

                    if (!string.IsNullOrWhiteSpace(MainImageColor))
                    {
                        string color = MainImageColor.Trim();
                        allUniqueColors.Add(color);
                    }
                    if (SubImageColors != null)
                    {
                        foreach (var colorInput in SubImageColors)
                        {
                            if (!string.IsNullOrWhiteSpace(colorInput))
                            {
                                string color = colorInput.Trim();
                                allUniqueColors.Add(color);
                            }
                        }
                    }

                    foreach (var color in allUniqueColors)
                    {
                        db.PRODUCT_COLOR.Add(new PRODUCT_COLOR { SANPHAMID = model.SANPHAMID, MAUSAC = color });
                    }
                   
                    if (!string.IsNullOrEmpty(strSIZE))
                    {
                        var arrSizes = strSIZE.Split(',');
                        foreach (var size in arrSizes)
                        {
                            if (!string.IsNullOrWhiteSpace(size))
                            {
                                db.PRODUCT_SIZE.Add(new PRODUCT_SIZE { SANPHAMID = model.SANPHAMID, SIZE = size.Trim() });
                            }
                        }
                    }
                  
                    if (mainImageFilename != "default.png")
                    {
                        PRODUCT_IMAGE mainImageEntry = new PRODUCT_IMAGE();
                        mainImageEntry.SANPHAMID = model.SANPHAMID;
                        mainImageEntry.TENHINH = mainImageFilename;

                        if (!string.IsNullOrWhiteSpace(MainImageColor))
                        {
                            mainImageEntry.MAUSAC = MainImageColor.Trim();
                        }
                        else
                        {
                            mainImageEntry.MAUSAC = null;
                        }
                        db.PRODUCT_IMAGE.Add(mainImageEntry);
                    }

                    if (SubImageFiles != null && SubImageFiles.Count > 0)
                    {
                        int colorCount = SubImageColors?.Length ?? 0;

                        for (int i = 0; i < SubImageFiles.Count; i++)
                        {
                            var file = SubImageFiles[i];

                            if (file != null && file.ContentLength > 0)
                            {
                                string filename = System.IO.Path.GetFileName(file.FileName);
                                string path = Server.MapPath("~/assets/" + filename);
                                file.SaveAs(path);

                                PRODUCT_IMAGE pImage = new PRODUCT_IMAGE();
                                pImage.SANPHAMID = model.SANPHAMID;
                                pImage.TENHINH = filename;

                                if (i < colorCount && !string.IsNullOrWhiteSpace(SubImageColors[i]))
                                {
                                    pImage.MAUSAC = SubImageColors[i].Trim();
                                }
                                else
                                {
                                    pImage.MAUSAC = null;
                                }

                                db.PRODUCT_IMAGE.Add(pImage);
                            }
                        }
                    }

                   
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi hệ thống: " + ex.Message;
            }

            
            ViewBag.DANHMUCID = new SelectList(db.CATEGORies, "DANHMUCID", "TENDANHMUC", model.DANHMUCID);
            return View(model);
        }

        
        [HttpGet]
        public ActionResult Edit(string id)
        {

            var product = db.PRODUCTs
                            .AsNoTracking() 
                            .Include(p => p.PRODUCT_IMAGE) 
                            .Include(p => p.PRODUCT_COLOR) 
                            .Include(p => p.PRODUCT_SIZE)  
                            .FirstOrDefault(p => p.SANPHAMID == id); 

            if (product == null) return HttpNotFound();

      
            ViewBag.DANHMUCID = new SelectList(db.CATEGORies, "DANHMUCID", "TENDANHMUC", product.DANHMUCID);

            return View(product);
        }




        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(
    PRODUCT model,
    HttpPostedFileBase UploadImage,
    string MainImageColor,
    List<HttpPostedFileBase> SubImageFiles,
    string[] SubImageColors,
    string strSIZE,
    string[] ExistingSubImageColors,
    int[] ImagesToDelete,
    int? SoLuongNhapThem 
)
        {
            if (ModelState.IsValid)
            {
                var productInDb = db.PRODUCTs.Find(model.SANPHAMID);
                if (productInDb != null)
                {
                   
                    int tonKhoHienTai = productInDb.SOLUONGTONKHO ;

                 
                    db.Entry(productInDb).CurrentValues.SetValues(model);

                   
                    if (SoLuongNhapThem.HasValue && SoLuongNhapThem.Value > 0)
                    {
                        
                        productInDb.SOLUONGTONKHO = tonKhoHienTai + SoLuongNhapThem.Value;
                    }
                    

                    string newMainImageFilename = productInDb.HINHANHDAIDIEN;
                    bool hasNewMainImage = false;

                    if (UploadImage != null && UploadImage.ContentLength > 0)
                    {
                        string filename = System.IO.Path.GetFileName(UploadImage.FileName);
                        string path = Server.MapPath("~/assets/" + filename);
                        UploadImage.SaveAs(path);
                        newMainImageFilename = filename;
                        productInDb.HINHANHDAIDIEN = filename;
                        hasNewMainImage = true;
                    }

           
                    var oldSizes = db.PRODUCT_SIZE.Where(x => x.SANPHAMID == model.SANPHAMID).ToList();
                    db.PRODUCT_SIZE.RemoveRange(oldSizes);

                    if (!string.IsNullOrEmpty(strSIZE))
                    {
                        foreach (var size in strSIZE.Split(','))
                        {
                            if (!string.IsNullOrWhiteSpace(size))
                            {
                                db.PRODUCT_SIZE.Add(new PRODUCT_SIZE { SANPHAMID = model.SANPHAMID, SIZE = size.Trim() });
                            }
                        }
                    }

                  
                    if (ImagesToDelete != null && ImagesToDelete.Length > 0)
                    {
                        var imagesToRemove = db.PRODUCT_IMAGE.Where(i => ImagesToDelete.Contains(i.IMAGEID)).ToList();
                        db.PRODUCT_IMAGE.RemoveRange(imagesToRemove);
                    }

                    if (ExistingSubImageColors != null)
                    {
                        var existingSubImages = db.PRODUCT_IMAGE.Where(i => i.SANPHAMID == model.SANPHAMID && i.TENHINH != productInDb.HINHANHDAIDIEN)
                                                                .OrderBy(i => i.IMAGEID).ToList();
                        for (int i = 0; i < existingSubImages.Count && i < ExistingSubImageColors.Length; i++)
                        {
                            existingSubImages[i].MAUSAC = ExistingSubImageColors[i]?.Trim();
                        }
                    }
                    if (SubImageFiles != null && SubImageFiles.Count > 0)
                    {
                        int colorCount = SubImageColors?.Length ?? 0;
                        for (int i = 0; i < SubImageFiles.Count; i++)
                        {
                            var file = SubImageFiles[i];
                            if (file != null && file.ContentLength > 0)
                            {
                                string filename = System.IO.Path.GetFileName(file.FileName);
                                string path = Server.MapPath("~/assets/" + filename);
                                file.SaveAs(path);

                                PRODUCT_IMAGE pImage = new PRODUCT_IMAGE();
                                pImage.SANPHAMID = model.SANPHAMID;
                                pImage.TENHINH = filename;
                                if (i < colorCount) pImage.MAUSAC = SubImageColors[i]?.Trim();
                                db.PRODUCT_IMAGE.Add(pImage);
                            }
                        }
                    }

           
                    var mainImageEntry = db.PRODUCT_IMAGE.FirstOrDefault(img => img.SANPHAMID == model.SANPHAMID && img.TENHINH == productInDb.HINHANHDAIDIEN);
                    if (mainImageEntry == null)
                    {
                        if (newMainImageFilename != "default.png") 
                        {
                            db.PRODUCT_IMAGE.Add(new PRODUCT_IMAGE
                            {
                                SANPHAMID = model.SANPHAMID,
                                TENHINH = newMainImageFilename,
                                MAUSAC = MainImageColor?.Trim()
                            });
                        }
                    }
                    else
                    {
                        mainImageEntry.MAUSAC = MainImageColor?.Trim();
                        if (hasNewMainImage) mainImageEntry.TENHINH = newMainImageFilename;
                    }


                    var oldColors = db.PRODUCT_COLOR.Where(x => x.SANPHAMID == model.SANPHAMID).ToList();
                    db.PRODUCT_COLOR.RemoveRange(oldColors);

                    HashSet<string> allUniqueColors = new HashSet<string>();

            
                    var allCurrentImages = db.PRODUCT_IMAGE
                                            .AsNoTracking()
                                            .Where(i => i.SANPHAMID == model.SANPHAMID)
                                            .Select(i => new { i.IMAGEID, i.MAUSAC })
                                            .ToList();

                    if (ImagesToDelete != null)
                    {
                        allCurrentImages = allCurrentImages.Where(i => !ImagesToDelete.Contains(i.IMAGEID)).ToList();
                    }

                    foreach (var image in allCurrentImages)
                    {
                        if (!string.IsNullOrWhiteSpace(image.MAUSAC)) allUniqueColors.Add(image.MAUSAC.Trim());
                    }

                    if (SubImageColors != null)
                    {
                        foreach (var colorInput in SubImageColors)
                        {
                            if (!string.IsNullOrWhiteSpace(colorInput)) allUniqueColors.Add(colorInput.Trim());
                        }
                    }

                    
                    if (!string.IsNullOrWhiteSpace(MainImageColor)) allUniqueColors.Add(MainImageColor.Trim());

                    foreach (var color in allUniqueColors)
                    {
                        db.PRODUCT_COLOR.Add(new PRODUCT_COLOR { SANPHAMID = model.SANPHAMID, MAUSAC = color });
                    }

                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.DANHMUCID = new SelectList(db.CATEGORies, "DANHMUCID", "TENDANHMUC", model.DANHMUCID);
            return View(model);
        }



        // 4. XÓA SẢN PHẨM

        public ActionResult Delete(string id)
        {
            var item = db.PRODUCTs.Find(id);
            if (item != null)
            {
                try
                {
               
                    var colors = db.PRODUCT_COLOR.Where(c => c.SANPHAMID == id).ToList();
                    if (colors.Any()) db.PRODUCT_COLOR.RemoveRange(colors);

                    var sizes = db.PRODUCT_SIZE.Where(s => s.SANPHAMID == id).ToList();
                    if (sizes.Any()) db.PRODUCT_SIZE.RemoveRange(sizes);

                    var images = db.PRODUCT_IMAGE.Where(i => i.SANPHAMID == id).ToList();
                    if (images.Any()) db.PRODUCT_IMAGE.RemoveRange(images);

                    
                    db.PRODUCTs.Remove(item);
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException)
                {
                   
                    TempData["Error"] = "Sản phẩm này đã có đơn hàng, không thể xóa! Hãy chuyển số lượng về 0 để ẩn.";
                    return RedirectToAction("Index");
                }
            }
            return RedirectToAction("Index");
        }
    
        public ActionResult CheckInventory()
        {

            var data = db.SP_KIEMTRATONKHO_CURSOR().ToList();

            return View(data);
        }
    }
}