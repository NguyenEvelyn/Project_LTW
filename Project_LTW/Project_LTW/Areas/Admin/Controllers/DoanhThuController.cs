using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_LTW.Models;
using Project_LTW.Areas.Admin.Controllers;


namespace Project_LTW.Areas.Admin.Controllers
{
    [CheckAdmin]
    public class DoanhThuController : Controller
    {
        // GET: Admin/DoanhThu
        FashionWebEntities db = new FashionWebEntities();
        public ActionResult Index()
        {
            {
              
                // Tính tổng các đơn đã giao thành công
                var donHangThanhCong = db.ORDERS.Where(o => o.TRANGTHAI == "Đã giao");
                ViewBag.TongDoanhThu = donHangThanhCong.Any() ? donHangThanhCong.Sum(o => o.TONGTIEN) : 0;
                ViewBag.TongDonHang = db.ORDERS.Count();
                ViewBag.TongKhachHang = db.CUSTOMERs.Count();


       
                int namHienTai = DateTime.Now.Year;
                ViewBag.Nam = namHienTai;

              
                var listDoanhThu = db.SP_THONGKEDOANHTHUTHEOTHANG(namHienTai).ToList();

           
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

              
                ViewBag.ChartData = string.Join(",", arrDoanhThu);
                var topKhach = db.Database.SqlQuery<TopKhachHang>("EXEC SP_THONGKEDONHANGTHEOKHACHHANG").ToList();

                return View(topKhach);
            }
        }
    }

}