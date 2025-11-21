using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Project_LTW.Models;

namespace Project_LTW.Areas.Admin.Controllers
{
    public class ProductController : Controller
    {
        // GET: Admin/Product
        FashionWebEntities db = new FashionWebEntities();
        public ActionResult Index()
        {
            var items = db.PRODUCTs.OrderBy(x => x.TENSANPHAM).ToList();

            return View(items);
        }

        //  CHỨC NĂNG THÊM MỚI (CREATE)


        [HttpGet]
        public ActionResult Create()
        {
            // Load danh sách Danh mục vào ViewBag để hiển thị Dropdown
            ViewBag.DANHMUCID = new SelectList(db.CATEGORies, "DANHMUCID", "TENDANHMUC");
            return View();
        }


        // ==============================================================
        // 1. XỬ LÝ THÊM MỚI (CREATE)
        // ==============================================================
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(PRODUCT model, HttpPostedFileBase UploadImage, string strMAUSAC, string strSIZE)
        {
            try
            {
                // 1. Tạo ID tự động: SP + chuỗi số thời gian (để không trùng)
                model.SANPHAMID = "SP" + DateTime.Now.Ticks.ToString().Substring(10);

                // 2. Xử lý ảnh đại diện
                if (UploadImage != null && UploadImage.ContentLength > 0)
                {
                    string filename = System.IO.Path.GetFileName(UploadImage.FileName);
                    string path = Server.MapPath("~/assets/" + filename);
                    UploadImage.SaveAs(path);
                    model.HINHANHDAIDIEN = filename;
                }
                else
                {
                    model.HINHANHDAIDIEN = "default.png"; // Ảnh mặc định nếu không chọn
                }

                if (ModelState.IsValid)
                {
                    // 3. Lưu sản phẩm chính trước
                    db.PRODUCTs.Add(model);

                    // 4. Xử lý Màu sắc (Tách chuỗi dấu phẩy)
                    if (!string.IsNullOrEmpty(strMAUSAC))
                    {
                        var arrColors = strMAUSAC.Split(','); // Cắt chuỗi: "Đỏ,Xanh" -> ["Đỏ", "Xanh"]
                        foreach (var color in arrColors)
                        {
                            if (!string.IsNullOrWhiteSpace(color))
                            {
                                PRODUCT_COLOR pc = new PRODUCT_COLOR();
                                pc.SANPHAMID = model.SANPHAMID;
                                pc.MAUSAC = color.Trim(); // Xóa khoảng trắng thừa
                                db.PRODUCT_COLOR.Add(pc);
                            }
                        }
                    }

                    // 5. Xử lý Size (Tương tự màu)
                    if (!string.IsNullOrEmpty(strSIZE))
                    {
                        var arrSizes = strSIZE.Split(',');
                        foreach (var size in arrSizes)
                        {
                            if (!string.IsNullOrWhiteSpace(size))
                            {
                                PRODUCT_SIZE ps = new PRODUCT_SIZE();
                                ps.SANPHAMID = model.SANPHAMID;
                                ps.SIZE = size.Trim();
                                db.PRODUCT_SIZE.Add(ps);
                            }
                        }
                    }

                    // 6. Lưu tất cả vào DB 1 lần
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi hệ thống: " + ex.Message;
            }

            // Load lại danh mục nếu lỗi
            ViewBag.DANHMUCID = new SelectList(db.CATEGORies, "DANHMUCID", "TENDANHMUC", model.DANHMUCID);
            return View(model);
        }



        //  CHỨC NĂNG CHỈNH SỬA (EDIT)


        [HttpGet]
        public ActionResult Edit(string id)
        {
            var product = db.PRODUCTs.Find(id);
            if (product == null) return HttpNotFound();

            // Load danh mục, chọn sẵn danh mục hiện tại của sản phẩm
            ViewBag.DANHMUCID = new SelectList(db.CATEGORies, "DANHMUCID", "TENDANHMUC", product.DANHMUCID);
            return View(product);
        }


        // ==============================================================
        // 2. XỬ LÝ CẬP NHẬT (EDIT)
        // ==============================================================
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(PRODUCT model, HttpPostedFileBase UploadImage, string strMAUSAC, string strSIZE)
        {
            if (ModelState.IsValid)
            {
                var productInDb = db.PRODUCTs.Find(model.SANPHAMID);
                if (productInDb != null)
                {
                    // 1. Cập nhật thông tin cơ bản
                    productInDb.TENSANPHAM = model.TENSANPHAM;
                    productInDb.GIA = model.GIA;
                    productInDb.GIAGOC = model.GIAGOC; // Nhớ cập nhật cả giá gốc nếu có

                    productInDb.SOLUONGTONKHO = model.SOLUONGTONKHO;
                    productInDb.MOTA = model.MOTA;
                    productInDb.DANHMUCID = model.DANHMUCID;

                    // 2. Xử lý ảnh (Chỉ đổi nếu có upload mới)
                    if (UploadImage != null && UploadImage.ContentLength > 0)
                    {
                        string filename = System.IO.Path.GetFileName(UploadImage.FileName);
                        string path = Server.MapPath("~/assets/" + filename);
                        UploadImage.SaveAs(path);
                        productInDb.HINHANHDAIDIEN = filename;
                    }
                    // Nếu không up ảnh mới thì giữ nguyên ảnh cũ

                    // 3. Xử lý Màu (Xóa hết cũ -> Thêm mới)
                    var oldColors = db.PRODUCT_COLOR.Where(x => x.SANPHAMID == model.SANPHAMID).ToList();
                    db.PRODUCT_COLOR.RemoveRange(oldColors); // Xóa cũ

                    if (!string.IsNullOrEmpty(strMAUSAC))
                    {
                        foreach (var color in strMAUSAC.Split(','))
                        {
                            if (!string.IsNullOrWhiteSpace(color))
                            {
                                db.PRODUCT_COLOR.Add(new PRODUCT_COLOR { SANPHAMID = model.SANPHAMID, MAUSAC = color.Trim() });
                            }
                        }
                    }

                    // 4. Xử lý Size (Xóa hết cũ -> Thêm mới)
                    var oldSizes = db.PRODUCT_SIZE.Where(x => x.SANPHAMID == model.SANPHAMID).ToList();
                    db.PRODUCT_SIZE.RemoveRange(oldSizes); // Xóa cũ

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

                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.DANHMUCID = new SelectList(db.CATEGORies, "DANHMUCID", "TENDANHMUC", model.DANHMUCID);
            return View(model);
        }

        // 4. XÓA SẢN PHẨM
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