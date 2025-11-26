using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_LTW.Models; // Đảm bảo namespace này đúng

namespace Project_LTW.Areas.Admin.Controllers
{
    public class StaffController : Controller
    {
        private FashionWebEntities db = new FashionWebEntities();

    
        public string TaoMaRandom()
        {
            Random r = new Random();
            string newID = "";
            bool isDuplicate = false;

          
            do
            {
         
                int num = r.Next(10000, 99999);
                newID = "NV" + num.ToString();
                isDuplicate = db.STAFFs.Any(x => x.MANV == newID);

            } while (isDuplicate == true); 

            return newID;
        }

        public ActionResult Create()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(STAFF staff)
        {
            if (ModelState.IsValid)
            {

                if (db.STAFFs.Any(s => s.MANV == staff.MANV))
                {
                    ModelState.AddModelError("MANV", "Mã nhân viên này đã tồn tại!");
                    return View(staff);
                }

              
                if (db.STAFFs.Any(s => s.EMAIL == staff.EMAIL))
                {
                    ModelState.AddModelError("EMAIL", "Email này đã được sử dụng!");
                    return View(staff);
             
                }
             
                if (staff.NGAYVAOLAM == null) staff.NGAYVAOLAM = DateTime.Now;


                db.STAFFs.Add(staff);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(staff);
        }

        [HttpGet]
        public ActionResult RegisterStaff()
        {
        
            return View();
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterStaffOnSubmit(STAFF newStaff, string ConfirmPassword, string SecretKey)
        {
           
            newStaff.MANV = TaoMaRandom();

            ModelState.Remove("MANV");

            if (ModelState.IsValid)
            {
                if (SecretKey != "toilastaffday")
                {
                    ViewBag.Error = "Sai mã xác thực cửa hàng!";
                    return View("RegisterStaff", newStaff);
                }

                if (newStaff.PASSWORD != ConfirmPassword)
                {
                    ViewBag.Error = "Mật khẩu xác nhận không chính xác";
                    return View("RegisterStaff", newStaff);
                }

                var checkEmail = db.STAFFs.FirstOrDefault(s => s.EMAIL == newStaff.EMAIL);
                if (checkEmail == null)
                {
                    newStaff.NGAYVAOLAM = DateTime.Now;

                    db.STAFFs.Add(newStaff);
                    db.SaveChanges();
                    Session["AdminUser"] = newStaff;
                    return RedirectToAction("Index", "HomeAdmin");
                }
                else
                {
                    ViewBag.Error = "Email này đã được sử dụng!";
                }
            }

            return View("RegisterStaff", newStaff);
        }
    }
}