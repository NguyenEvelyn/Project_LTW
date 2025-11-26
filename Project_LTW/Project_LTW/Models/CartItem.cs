using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Project_LTW.Models
{
    public class CartItem
    {
        public string MASP { get; set; }
        public string TENSP { get; set; }
        public string AnhDaiDien { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }

        // ********** TRƯỜNG MỚI **********
        public string MAUSAC { get; set; }
        public string SIZE { get; set; }
        // ********************************

        public decimal ThanhTien
        {
            get { return DonGia * SoLuong; }
        }

        public CartItem() { }

        // Constructor lấy dữ liệu từ DB
        public CartItem(string id, string mau, string size)
        {
            using (var db = new FashionWebEntities())
            {
                string idChuan = id.Trim();
                // Giả sử tên bảng sản phẩm của bạn là PRODUCTs
                var sp = db.PRODUCTs.FirstOrDefault(n => n.SANPHAMID.Trim() == idChuan);

                if (sp != null)
                {
                    MASP = sp.SANPHAMID.Trim();
                    TENSP = sp.TENSANPHAM;
                    AnhDaiDien = sp.HINHANHDAIDIEN;
                    DonGia = sp.GIA;
                    SoLuong = 1;

                    // 🌟 SỬA LỖI TẠI ĐÂY: GÁN GIÁ TRỊ MÀU VÀ SIZE 🌟
                    MAUSAC = mau;
                    SIZE = size;
                    // ************************************
                }
            }
        }
    }
}