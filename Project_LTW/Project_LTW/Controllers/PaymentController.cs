using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Project_LTW.Models;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

namespace Project_LTW.Controllers
{
    public class PaymentController : Controller
    {
        FashionWebEntities db = new FashionWebEntities();

        private string GetIdSuffix()
        {
            var ticks = DateTime.Now.Ticks.ToString();
            return ticks.Substring(ticks.Length - 8);
        }

        [HttpGet]
        public ActionResult Index()
        {
            if (Session["User"] == null) return RedirectToAction("Login", "User");

            var cart = Session["Cart"] as Cart;
            if (cart == null || cart.list == null || !cart.list.Any())
                return RedirectToAction("Index", "Cart");

            var userSession = Session["User"] as CUSTOMER;
            var currentUser = db.CUSTOMERs.Include(c => c.ADDRESSes)
                                          .FirstOrDefault(c => c.KHACHHANGID == userSession.KHACHHANGID);

            ViewBag.Cart = cart.list;
            ViewBag.User = currentUser;
            ViewBag.Addresses = currentUser?.ADDRESSes.ToList();
            ViewBag.Total = cart.TongTien();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string ShipName, string ShipPhone, string ShipAddress, string Note, string addressOption, string PaymentMethod)
        {
            var cart = Session["Cart"] as Cart;
            var userSession = Session["User"] as CUSTOMER;

            if (userSession == null) return RedirectToAction("Login", "User");
            if (cart == null || cart.list == null || !cart.list.Any()) return RedirectToAction("Index", "Cart");

            var currentUser = db.CUSTOMERs.Include(c => c.ADDRESSes)
                                          .FirstOrDefault(c => c.KHACHHANGID == userSession.KHACHHANGID);
            ViewBag.Cart = cart.list;
            ViewBag.Total = cart.TongTien();
            ViewBag.User = currentUser;
            ViewBag.Addresses = currentUser?.ADDRESSes.ToList();

            if (string.IsNullOrEmpty(ShipName) || string.IsNullOrEmpty(ShipPhone))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Họ tên và Số điện thoại.";
                return View();
            }

            using (var scope = db.Database.BeginTransaction())
            {
                try
                {
                    string suffix = GetIdSuffix();
                    string finalAddressID = null;

                    // 1. Xử lý địa chỉ
                    if (!string.IsNullOrEmpty(addressOption) && addressOption.StartsWith("saved_"))
                    {
                        finalAddressID = addressOption.Replace("saved_", "").Trim();
                    }
                    else
                    {
                        finalAddressID = "DC" + suffix;
                        var newAddr = new ADDRESS
                        {
                            DIACHIID = finalAddressID,
                            KHACHHANGID = userSession.KHACHHANGID,
                            DUONG = ShipAddress,
                            THANHPHO = "Toàn Quốc",
                            TINH = "Việt Nam",
                            ZIPCODE = "70000"
                        };
                        db.ADDRESSes.Add(newAddr);
                    }

                   
                    string orderID = "DH" + suffix;
                    var newOrder = new ORDER
                    {
                        ORDERID = orderID,
                        KHACHHANGID = userSession.KHACHHANGID,
                        NGAYDAT = DateTime.Now,
                        TONGTIEN = cart.TongTien(),
                        TRANGTHAI = (PaymentMethod == "COD") ? "Chờ xác nhận (COD)" : "Chờ thanh toán (CK)",
                        DIACHIID = finalAddressID,
                        GHICHU = Note
                    };
                    db.ORDERS.Add(newOrder);


                    var groupedCart = cart.list
                        .Select(x => new
                        {
                            ItemOriginal = x,
                            RealColor = string.IsNullOrEmpty(x.MAUSAC) ? "Mặc định" : x.MAUSAC,
                            RealSize = string.IsNullOrEmpty(x.SIZE) ? "FreeSize" : x.SIZE
                        })
                        .GroupBy(x => new { x.ItemOriginal.MASP, x.RealColor, x.RealSize })
                        .Select(g => new
                        {
                            MASP = g.Key.MASP,
                            MAUSAC = g.Key.RealColor,
                            SIZE = g.Key.RealSize,
                            SoLuong = g.Sum(x => x.ItemOriginal.SoLuong),
                            DonGia = g.First().ItemOriginal.DonGia
                        })
                        .ToList();

                    // 4. Tạo Detail
                    foreach (var item in groupedCart)
                    {
                       
                      
                        var product = db.PRODUCTs.Find(item.MASP);
                        if (product == null)
                        {
                            ViewBag.Error = $"Sản phẩm {item.MASP} không tồn tại.";
                            return View();
                        }
                        if (product.SOLUONGTONKHO < item.SoLuong)
                        {
                            ViewBag.Error = $"Sản phẩm '{product.TENSANPHAM}' (Màu: {item.MAUSAC}, Size: {item.SIZE}) không đủ hàng.";
                            return View();
                        }

                        product.SOLUONGTONKHO -= item.SoLuong;

                        var detail = new ORDERDETAIL
                        {
                            ORDERID = orderID,
                            SANPHAMID = item.MASP,
                            SOLUONG = item.SoLuong,
                            DONGIA = item.DonGia,
                            MAUSAC = item.MAUSAC,
                            SIZE = item.SIZE
                        };
                        db.ORDERDETAILs.Add(detail);
                    }

                    db.SaveChanges();

                    // 5. Tạo Payment
                    var pay = new PAYMENT
                    {
                        PAYMENTID = "PM" + suffix,
                        ORDERID = orderID,
                        PHUONGTHUCTT = PaymentMethod,
                        TRANGTHAITT = "Chờ xác nhận",
                        NGAYTT = (PaymentMethod == "COD") ? null : (DateTime?)DateTime.Now
                    };
                    db.PAYMENTs.Add(pay);
                    db.SaveChanges();

                    scope.Commit();

                    Session["Cart"] = null;
                    return RedirectToAction("Success");
                }
                catch (Exception ex)
                {
                    // In lỗi ra màn hình
                    ViewBag.Error = "Lỗi xảy ra: " + ex.Message;
                    if (ex is DbEntityValidationException dbEx)
                    {
                        var msg = dbEx.EntityValidationErrors.SelectMany(x => x.ValidationErrors).Select(x => x.ErrorMessage);
                        ViewBag.Error = "Lỗi dữ liệu: " + string.Join("; ", msg);
                    }
                    if (ex is DbUpdateException dbUpEx)
                    {
                        var real = dbUpEx.InnerException;
                        while (real != null && real.InnerException != null) real = real.InnerException;
                        ViewBag.Error = "Lỗi Database: " + (real != null ? real.Message : dbUpEx.Message);
                    }

                    return View();
                }
            }
        }

        public ActionResult Success()
        {
            return View();
        }
    }
}