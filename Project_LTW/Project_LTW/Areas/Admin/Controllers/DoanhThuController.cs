using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_LTW.Models;
namespace Project_LTW.Areas.Admin.Controllers
{
    public class DoanhThuController : Controller
    {
        // GET: Admin/DoanhThu
        FashionWebEntities db = new FashionWebEntities();
        public ActionResult Index()
        {
            {
                // --- 1. THỐNG KÊ TỔNG QUAN (Các thẻ bài trên cùng) ---
                // Tính tổng các đơn đã giao thành công
                var donHangThanhCong = db.ORDERS.Where(o => o.TRANGTHAI == "Đã giao");
                ViewBag.TongDoanhThu = donHangThanhCong.Any() ? donHangThanhCong.Sum(o => o.TONGTIEN) : 0;
                ViewBag.TongDonHang = db.ORDERS.Count();
                ViewBag.TongKhachHang = db.CUSTOMERs.Count();


                // --- 2. XỬ LÝ BIỂU ĐỒ (QUAN TRỌNG: Dùng Procedure của bạn) ---
                int namHienTai = DateTime.Now.Year;
                ViewBag.Nam = namHienTai;

                // Gọi Procedure: db.SP_THONGKEDOANHTHUTHEOTHANG(int? NAM)
                // Lưu ý: Đảm bảo bạn đã Update Model .edmx và Function Import trả về Complex Type
                var listDoanhThu = db.SP_THONGKEDOANHTHUTHEOTHANG(namHienTai).ToList();

                // Tạo một mảng 12 phần tử, mặc định toàn số 0
                // arrDoanhThu[0] là tháng 1, arrDoanhThu[11] là tháng 12
                decimal[] arrDoanhThu = new decimal[12];

                foreach (var item in listDoanhThu)
                {
                    // Kiểm tra null để tránh lỗi
                    if (item.THANG.HasValue && item.DOANHTHU.HasValue)
                    {
                        // Ví dụ: Tháng 1 (item.THANG = 1) thì lưu vào index 0
                        int index = item.THANG.Value - 1;
                        if (index >= 0 && index < 12)
                        {
                            arrDoanhThu[index] = item.DOANHTHU.Value;
                        }
                    }
                }

                // Chuyển mảng thành chuỗi string để Javascript đọc được
                // Kết quả sẽ giống: "0, 0, 500000, 0, 1200000, ..."
                ViewBag.ChartData = string.Join(",", arrDoanhThu);

                // --- 3. LẤY TOP KHÁCH HÀNG (Dùng Cursor như cũ) ---
                var topKhach = db.Database.SqlQuery<TopKhachHang>("EXEC SP_THONGKEDONHANGTHEOKHACHHANG").ToList();

                return View(topKhach);
            }
        }
    }

}