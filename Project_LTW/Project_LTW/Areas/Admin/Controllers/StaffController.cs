using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Project_LTW.Areas.Admin.Controllers
{
    public class StaffController : Controller
    {
        // GET: Admin/Staff
       
        private FashionWebEntities db = new FashionWebEntities();

        
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
    }
}