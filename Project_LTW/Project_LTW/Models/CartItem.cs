using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Project_LTW.Models
{
    public class CartItem
    {
        // 1. SỬA LỖI QUAN TRỌNG: Đổi int thành string để khớp với Database
        public string MASP { get; set; }

        public string TENSP { get; set; }
        public string AnhDaiDien { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }

        public decimal ThanhTien
        {
            get
            {
                return DonGia * SoLuong;
            }
        }

        // 2. THÊM: Constructor mặc định (Bắt buộc phải có để List hoạt động)
        public CartItem() { }

        // Constructor lấy dữ liệu từ DB
        public CartItem(string id)
        {
            // LƯU Ý: Đổi 'FashionWebEntities' thành tên DbContext của bạn nếu khác
            using (var db = new FashionWebEntities())
            {
                // Xử lý cắt khoảng trắng ID đầu vào
                string idChuan = id.Trim();

                // Tìm sản phẩm trong DB (So sánh string với string)
                var sp = db.PRODUCTs.FirstOrDefault(n => n.SANPHAMID.Trim() == idChuan);

                if (sp != null)
                {
                    MASP = sp.SANPHAMID.Trim();
                    TENSP = sp.TENSANPHAM;
                    AnhDaiDien = sp.HINHANHDAIDIEN;
                    DonGia = sp.GIA;
                    SoLuong = 1;
                }
            }
        }
    }
}