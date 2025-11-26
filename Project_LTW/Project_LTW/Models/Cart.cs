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

        // Hàm Thêm (Đã có sẵn 3 tham số, giữ nguyên)
        public void Them(string id, string mau, string size)
        {
            if (string.IsNullOrEmpty(id)) return;
            string idChuan = id.Trim();

            // QUAN TRỌNG: Tìm kiếm dựa trên cả MASP, MAUSAC và SIZE
            CartItem sp = list.FirstOrDefault(x =>
                x.MASP == idChuan &&
                x.MAUSAC == mau &&
                x.SIZE == size);

            if (sp != null)
            {
                sp.SoLuong++; // Nếu trùng cả 3 yếu tố thì tăng số lượng
            }
            else
            {
                // TẠO CartItem MỚI (truyền cả 3 tham số)
                CartItem newItem = new CartItem(idChuan, mau, size);
                if (!string.IsNullOrEmpty(newItem.TENSP))
                {
                    list.Add(newItem);
                }
            }
        }


        // 🌟 Đã SỬA: Hàm Xóa nhận đủ 3 tham số để xác định CartItem duy nhất
        public void Xoa(string id, string mau, string size)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(mau) || string.IsNullOrEmpty(size)) return;
            string idChuan = id.Trim();

            // QUAN TRỌNG: Tìm kiếm dựa trên cả MASP, MAUSAC và SIZE
            CartItem sp = list.FirstOrDefault(x =>
                x.MASP == idChuan &&
                x.MAUSAC == mau &&
                x.SIZE == size);

            if (sp != null) list.Remove(sp);
        }

        // 🌟 Đã SỬA: Hàm Giảm nhận đủ 3 tham số để xác định CartItem duy nhất
        public void Giam(string id, string mau, string size)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(mau) || string.IsNullOrEmpty(size)) return;
            string idChuan = id.Trim();

            // QUAN TRỌNG: Tìm kiếm dựa trên cả MASP, MAUSAC và SIZE
            CartItem sp = list.FirstOrDefault(x =>
                x.MASP == idChuan &&
                x.MAUSAC == mau &&
                x.SIZE == size);

            if (sp != null)
            {
                sp.SoLuong--;
                if (sp.SoLuong <= 0) list.Remove(sp); // Xóa nếu số lượng bằng 0
            }
        }
    }
}