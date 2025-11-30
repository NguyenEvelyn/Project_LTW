using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Project_LTW.Models;

namespace Project_LTW.Areas.Admin.Controllers
{
    [CheckAdmin]
    public class DoanhThuController : Controller
    {
        FashionWebEntities db = new FashionWebEntities();

        public ActionResult Index()
        {
            int namHienTai = DateTime.Now.Year;
            ViewBag.Nam = namHienTai;

            try
            {
                // ✅ GỌI PROCEDURE - DÙNG RESULT CÓ SẴN
                var listDoanhThu = db.SP_THONGKEDOANHTHUTHEOTHANG(namHienTai).ToList();

                // ✅ TẠO MẢNG 12 THÁNG
                decimal[] arrDoanhThu = new decimal[12];

                foreach (var item in listDoanhThu)
                {
                    if (item.THANG.HasValue && item.DOANHTHU.HasValue)
                    {
                        int index = item.THANG.Value - 1;
                        if (index >= 0 && index < 12)
                        {
                            arrDoanhThu[index] = item.DOANHTHU.Value;
                        }
                    }
                }

                // ✅ TÍNH TỔNG DOANH THU
                ViewBag.TongDoanhThu = arrDoanhThu.Sum();

                // ✅ ĐẾM ĐƠN HÀNG (CHỈ "Hoàn tất")
                ViewBag.TongDonHang = db.ORDERS
                    .Where(o => o.NGAYDAT.Year == namHienTai && o.TRANGTHAI == "Hoàn tất")
                    .Count();

                // ✅ ĐẾM KHÁCH HÀNG UNIQUE
                ViewBag.TongKhachHang = db.ORDERS
                    .Where(o => o.NGAYDAT.Year == namHienTai && o.TRANGTHAI == "Hoàn tất")
                    .Select(o => o.KHACHHANGID)
                    .Distinct()
                    .Count();

                // ✅ DỮ LIỆU CHO BIỂU ĐỒ
                ViewBag.ChartData = string.Join(",", arrDoanhThu);

                // ✅ TOP KHÁCH HÀNG
                var topKhachHang = db.SP_THONGKEDONHANGTHEOKHACHHANG().ToList();

                // ✅ DEBUG
                System.Diagnostics.Debug.WriteLine($"Chart Data: {ViewBag.ChartData}");
                System.Diagnostics.Debug.WriteLine($"Số khách hàng top: {topKhachHang.Count}");

                // ✅ CHUYỂN ĐỔI SANG TopKhachHang (nếu cần)
                var topKH = topKhachHang.Select(x => new TopKhachHang
                {
                    TenKhachHang = x.TenKhachHang,
                    SoDonHang = x.SoDonHang,
                    TongTienDaMua = x.TongTienDaMua
                }).ToList();

                return View(topKH);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi: " + ex.Message;
                ViewBag.ChartData = "0,0,0,0,0,0,0,0,0,0,0,0";
                return View(new List<TopKhachHang>());
            }
        }
    }
}