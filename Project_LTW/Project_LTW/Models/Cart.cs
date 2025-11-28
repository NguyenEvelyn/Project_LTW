using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Project_LTW.Models
{
    public class Cart
    {
        public List<CartItem> list { get; set; }

        public Cart()
        {
            list = new List<CartItem>();
        }

        public int SoLuongMatHang() { return list.Count; }
        public int TongSoLuong() { return list.Sum(x => x.SoLuong); }
        public decimal TongTien() { return list.Sum(x => x.ThanhTien); }

  
        public void Them(string id, string mau, string size)
        {
            if (string.IsNullOrEmpty(id)) return;
            string idChuan = id.Trim();

     
            CartItem sp = list.FirstOrDefault(x =>
                x.MASP == idChuan &&
                x.MAUSAC == mau &&
                x.SIZE == size);

            if (sp != null)
            {
                sp.SoLuong++; 
            }
            else
            {
               
                CartItem newItem = new CartItem(idChuan, mau, size);
                if (!string.IsNullOrEmpty(newItem.TENSP))
                {
                    list.Add(newItem);
                }
            }
        }


        public void Xoa(string id, string mau, string size)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(mau) || string.IsNullOrEmpty(size)) return;
            string idChuan = id.Trim();

            CartItem sp = list.FirstOrDefault(x =>
                x.MASP == idChuan &&
                x.MAUSAC == mau &&
                x.SIZE == size);

            if (sp != null) list.Remove(sp);
        }


        public void Giam(string id, string mau, string size)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(mau) || string.IsNullOrEmpty(size)) return;
            string idChuan = id.Trim();

            
            CartItem sp = list.FirstOrDefault(x =>
                x.MASP == idChuan &&
                x.MAUSAC == mau &&
                x.SIZE == size);

            if (sp != null)
            {
                sp.SoLuong--;
                if (sp.SoLuong <= 0) list.Remove(sp); 
            }
        }
    }
}