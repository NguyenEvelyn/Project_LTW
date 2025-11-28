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

     
        public string MAUSAC { get; set; }
        public string SIZE { get; set; }
      

        public decimal ThanhTien
        {
            get { return DonGia * SoLuong; }
        }

        public CartItem() { }

       
        public CartItem(string id, string mau, string size)
        {
            using (var db = new FashionWebEntities())
            {
                string idChuan = id.Trim();
               
                var sp = db.PRODUCTs.FirstOrDefault(n => n.SANPHAMID.Trim() == idChuan);

                if (sp != null)
                {
                    MASP = sp.SANPHAMID.Trim();
                    TENSP = sp.TENSANPHAM;
                    AnhDaiDien = sp.HINHANHDAIDIEN;
                    DonGia = sp.GIA;
                    SoLuong = 1;

                
                   
                }
                MAUSAC = string.IsNullOrEmpty(mau) ? "Mặc định" : mau;
                SIZE = string.IsNullOrEmpty(size) ? "FreeSize" : size;
            }
        }
    }
}