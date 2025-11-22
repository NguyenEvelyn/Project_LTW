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
    [CheckAdmin] // <--- THÊM DÒNG NÀY
    public class ProductController : Controller
    {
        // GET: Admin/Product
        FashionWebEntities db = new FashionWebEntities();
        public ActionResult Index()
        {
            var items = db.PRODUCTs.OrderBy(x => x.TENSANPHAM).ToList();

            return View(items);
        }

        // =========================================================
        // Chức năng thêm mới SẢN PHẨM (CREATE)
        // =========================================================

        [HttpGet]
        public ActionResult Create()
        {
            // Load danh sách Danh mục vào ViewBag để hiển thị Dropdown
            ViewBag.DANHMUCID = new SelectList(db.CATEGORies, "DANHMUCID", "TENDANHMUC");
            return View();
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(PRODUCT model, HttpPostedFileBase UploadImage, string MainImageColor, List<HttpPostedFileBase> SubImageFiles, string[] SubImageColors, string strSIZE)
        {
            try
            {
                // 1. Tạo ID sản phẩm
                model.SANPHAMID = "SP" + DateTime.Now.Ticks.ToString().Substring(10);
                string mainImageFilename = "default.png";

                // 2. Xử lý ảnh đại diện (Lưu file và tên file)
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
                    // 3. Lưu sản phẩm chính trước
                    db.PRODUCTs.Add(model);

                    // =========================================================
                    // 4. XỬ LÝ VÀ LƯU TẤT CẢ MÀU SẮC TỪ INPUT ẢNH (MỚI)
                    // =========================================================
                    HashSet<string> allUniqueColors = new HashSet<string>();

                    // 4a. Thu thập màu từ Ảnh Đại diện
                    if (!string.IsNullOrWhiteSpace(MainImageColor))
                    {
                        string color = MainImageColor.Trim();
                        allUniqueColors.Add(color);
                    }

                    // 4b. Thu thập màu từ Ảnh Phụ
                    if (SubImageColors != null)
                    {
                        foreach (var colorInput in SubImageColors)
                        {
                            if (!string.IsNullOrWhiteSpace(colorInput))
                            {
                                string color = colorInput.Trim();
                                // Thêm vào HashSet để đảm bảo màu là duy nhất
                                allUniqueColors.Add(color);
                            }
                        }
                    }

                    // 4c. Lưu tất cả các màu duy nhất vào bảng PRODUCT_COLOR
                    foreach (var color in allUniqueColors)
                    {
                        db.PRODUCT_COLOR.Add(new PRODUCT_COLOR { SANPHAMID = model.SANPHAMID, MAUSAC = color });
                    }
                    // =========================================================

                    // 5. Xử lý Size (Giữ nguyên)
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

                    // =========================================================
                    // 6. GHI ẢNH ĐẠI DIỆN VÀO BẢNG PRODUCT_IMAGE (Tách từ bước 3b cũ)
                    // =========================================================
                    if (mainImageFilename != "default.png")
                    {
                        PRODUCT_IMAGE mainImageEntry = new PRODUCT_IMAGE();
                        mainImageEntry.SANPHAMID = model.SANPHAMID;
                        mainImageEntry.TENHINH = mainImageFilename;

                        // Gán màu đã được nhập (đã được xác nhận là không rỗng ở bước 4a)
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

                    // 7. Xử lý Danh sách Ảnh Phụ (Tách từ bước 6 cũ)
                    if (SubImageFiles != null && SubImageFiles.Count > 0)
                    {
                        int colorCount = SubImageColors?.Length ?? 0;

                        for (int i = 0; i < SubImageFiles.Count; i++)
                        {
                            var file = SubImageFiles[i];

                            if (file != null && file.ContentLength > 0)
                            {
                                // Lưu file
                                string filename = System.IO.Path.GetFileName(file.FileName);
                                string path = Server.MapPath("~/assets/" + filename);
                                file.SaveAs(path);

                                PRODUCT_IMAGE pImage = new PRODUCT_IMAGE();
                                pImage.SANPHAMID = model.SANPHAMID;
                                pImage.TENHINH = filename;

                                // GÁN MÀU TỪ MẢNG (đã được xác nhận không rỗng ở bước 4b)
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

                    // 8. Lưu tất cả vào DB 1 lần
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi hệ thống: " + ex.Message;
            }

            // ... (Code xử lý lỗi) ...
            ViewBag.DANHMUCID = new SelectList(db.CATEGORies, "DANHMUCID", "TENDANHMUC", model.DANHMUCID);
            return View(model);
        }



        //  CHỨC NĂNG CHỈNH SỬA (EDIT)


        

        // ==============================================================
        // 1. XỬ LÝ TRUY XUẤT DỮ LIỆU (EDIT - GET)
        // ==============================================================
        [HttpGet]
        public ActionResult Edit(string id)
        {

            var product = db.PRODUCTs
                            .AsNoTracking() // Giúp tăng tốc độ đọc dữ liệu
                            .Include(p => p.PRODUCT_IMAGE) // Bắt buộc tải ảnh
                            .Include(p => p.PRODUCT_COLOR) // Bắt buộc tải màu
                            .Include(p => p.PRODUCT_SIZE)  // Bắt buộc tải size
                            .FirstOrDefault(p => p.SANPHAMID == id); // Tìm sản phẩm theo ID

            if (product == null) return HttpNotFound();

            // Load danh mục
            ViewBag.DANHMUCID = new SelectList(db.CATEGORies, "DANHMUCID", "TENDANHMUC", product.DANHMUCID);

            return View(product);
        }


        // ==============================================================
        // 2. XỬ LÝ CẬP NHẬT (EDIT - POST) ĐẢM BẢO CHÍNH XÁC
        // ==============================================================
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(
            PRODUCT model,
            HttpPostedFileBase UploadImage,
            string MainImageColor,             // Màu cho ảnh đại diện mới/cũ
            List<HttpPostedFileBase> SubImageFiles, // File ảnh phụ MỚI
            string[] SubImageColors,           // Màu cho ảnh phụ MỚI
            string strSIZE,                    // Chuỗi Size
            string[] ExistingSubImageColors,   // Màu đã chỉnh sửa của ảnh phụ HIỆN CÓ
            int[] ImagesToDelete               // ID (HINHANHID) của ảnh phụ cần xóa
        )
        {
            if (ModelState.IsValid)
            {
                var productInDb = db.PRODUCTs.Find(model.SANPHAMID);
                if (productInDb != null)
                {
                    // 1. Cập nhật thông tin cơ bản
                    db.Entry(productInDb).CurrentValues.SetValues(model); // Cập nhật các trường chung

                    // 2. Xử lý Ảnh Đại diện
                    string newMainImageFilename = productInDb.HINHANHDAIDIEN;
                    bool hasNewMainImage = false;

                    if (UploadImage != null && UploadImage.ContentLength > 0)
                    {
                        // Lưu file mới
                        string filename = System.IO.Path.GetFileName(UploadImage.FileName);
                        string path = Server.MapPath("~/assets/" + filename);
                        UploadImage.SaveAs(path);
                        newMainImageFilename = filename;
                        productInDb.HINHANHDAIDIEN = filename;
                        hasNewMainImage = true;
                    }

                    // 3. XỬ LÝ XÓA DỮ LIỆU CŨ VÀ CẬP NHẬT SIZE

                    // Xóa Size cũ -> Thêm mới
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

                    // 4. XỬ LÝ PRODUCT_IMAGE (Xóa ảnh, Cập nhật màu ảnh cũ, Thêm ảnh mới)

                    // 4a. Xóa Ảnh Phụ (Dùng HINHANHID)
                    if (ImagesToDelete != null && ImagesToDelete.Length > 0)
                    {
                        // Dùng .Where(i => ImagesToDelete.Contains(i.HINHANHID)) nếu ID là HINHANHID
                        var imagesToRemove = db.PRODUCT_IMAGE.Where(i => ImagesToDelete.Contains(i.IMAGEID)).ToList();
                        db.PRODUCT_IMAGE.RemoveRange(imagesToRemove);
                    }

                    // 4b. Cập nhật Màu Ảnh Phụ HIỆN CÓ
                    if (ExistingSubImageColors != null)
                    {
                        // Tải tất cả ảnh phụ hiện tại (để cập nhật màu)
                        var existingSubImages = db.PRODUCT_IMAGE.Where(i => i.SANPHAMID == model.SANPHAMID && i.TENHINH != productInDb.HINHANHDAIDIEN)
                                                                .OrderBy(i => i.IMAGEID).ToList();

                        for (int i = 0; i < existingSubImages.Count && i < ExistingSubImageColors.Length; i++)
                        {
                            // Cập nhật trường MAUSAC cho ảnh cũ
                            existingSubImages[i].MAUSAC = ExistingSubImageColors[i]?.Trim();
                        }
                    }

                    // 4c. Thêm Ảnh Phụ MỚI
                    if (SubImageFiles != null && SubImageFiles.Count > 0)
                    {
                        int colorCount = SubImageColors?.Length ?? 0;

                        for (int i = 0; i < SubImageFiles.Count; i++)
                        {
                            var file = SubImageFiles[i];

                            if (file != null && file.ContentLength > 0)
                            {
                                // Lưu file
                                string filename = System.IO.Path.GetFileName(file.FileName);
                                string path = Server.MapPath("~/assets/" + filename);
                                file.SaveAs(path);

                                PRODUCT_IMAGE pImage = new PRODUCT_IMAGE();
                                pImage.SANPHAMID = model.SANPHAMID;
                                pImage.TENHINH = filename;

                                if (i < colorCount)
                                {
                                    pImage.MAUSAC = SubImageColors[i]?.Trim();
                                }

                                db.PRODUCT_IMAGE.Add(pImage);
                            }
                        }
                    }

                    // 4d. Cập nhật/Thêm Ảnh Đại diện vào PRODUCT_IMAGE
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
                        // Luôn cập nhật màu cho ảnh đại diện
                        mainImageEntry.MAUSAC = MainImageColor?.Trim();
                        if (hasNewMainImage)
                        {
                            mainImageEntry.TENHINH = newMainImageFilename;
                        }
                    }

                    // ==============================================================
                    // 5. XỬ LÝ PRODUCT_COLOR
                    // ==============================================================

                    // 5a. Xóa hết màu cũ
                    var oldColors = db.PRODUCT_COLOR.Where(x => x.SANPHAMID == model.SANPHAMID).ToList();
                    db.PRODUCT_COLOR.RemoveRange(oldColors);

                    HashSet<string> allUniqueColors = new HashSet<string>();

                    // 5b. Thu thập màu từ các ảnh CÒN LẠI (SỬA LỖI SYSTEM.NOTSUPPORTEDEXCEPTION)

                    // 1. Tải tất cả các ảnh hiện tại của sản phẩm vào bộ nhớ.
                    // Dùng .Select(i => new { i.HINHANHID, i.MAUSAC }) để tải nhẹ nhàng hơn
                    var allCurrentImages = db.PRODUCT_IMAGE
                                             .AsNoTracking() // Không theo dõi thay đổi
                                             .Where(i => i.SANPHAMID == model.SANPHAMID)
                                             .Select(i => new { i.IMAGEID, i.MAUSAC })
                                             .ToList(); // <--- CHÍNH ĐÂY LÀ ĐIỂM SỬA LỖI: Thực thi SQL TẠI ĐÂY

                    // 2. Lọc bỏ các ảnh đã bị đánh dấu xóa trong bộ nhớ (In-memory filtering)
                    if (ImagesToDelete != null)
                    {
                        allCurrentImages = allCurrentImages
                                           .Where(i => !ImagesToDelete.Contains(i.IMAGEID))
                                           .ToList();
                    }

                    // 3. Thu thập màu từ các ảnh CÒN LẠI
                    foreach (var image in allCurrentImages)
                    {
                        if (!string.IsNullOrWhiteSpace(image.MAUSAC))
                        {
                            allUniqueColors.Add(image.MAUSAC.Trim());
                        }
                    }

                    // Thêm màu từ ảnh phụ MỚI (chưa được lưu vào DB)
                    if (SubImageColors != null)
                    {
                        foreach (var colorInput in SubImageColors)
                        {
                            if (!string.IsNullOrWhiteSpace(colorInput))
                            {
                                allUniqueColors.Add(colorInput.Trim());
                            }
                        }
                    }

                    // 5c. Thêm tất cả màu duy nhất vào PRODUCT_COLOR
                    foreach (var color in allUniqueColors)
                    {
                        db.PRODUCT_COLOR.Add(new PRODUCT_COLOR { SANPHAMID = model.SANPHAMID, MAUSAC = color });
                    }


                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            // Xử lý lỗi
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
                    //  Xóa dữ liệu bên bảng Màu sắc trước
                    var colors = db.PRODUCT_COLOR.Where(c => c.SANPHAMID == id).ToList();
                    if (colors.Any()) db.PRODUCT_COLOR.RemoveRange(colors);

                    //  Xóa dữ liệu bên bảng Size trước
                    var sizes = db.PRODUCT_SIZE.Where(s => s.SANPHAMID == id).ToList();
                    if (sizes.Any()) db.PRODUCT_SIZE.RemoveRange(sizes);

                    //  Xóa dữ liệu bên bảng Ảnh phụ (nếu có)
                    var images = db.PRODUCT_IMAGE.Where(i => i.SANPHAMID == id).ToList();
                    if (images.Any()) db.PRODUCT_IMAGE.RemoveRange(images);

                    //  Xóa sản phẩm chính
                    db.PRODUCTs.Remove(item);
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException)
                {
                    // Nếu vẫn lỗi => Sản phẩm này ĐÃ CÓ ĐƠN HÀNG (nằm trong OrderDetail)
                    // Không được phép xóa sản phẩm đã bán, chỉ có thể ẩn đi
                    TempData["Error"] = "Sản phẩm này đã có đơn hàng, không thể xóa! Hãy chuyển số lượng về 0 để ẩn.";
                    return RedirectToAction("Index");
                }
            }
            return RedirectToAction("Index");
        }
        // Kiểm trả tồn kho bằng Procedure Cursor
        public ActionResult CheckInventory()
        {

            var data = db.SP_KIEMTRATONKHO_CURSOR().ToList();

            return View(data);
        }
    }
}