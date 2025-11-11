using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Project_LTW.Controllers
{
    public class CartController : Controller
    {
        // GET: Cart
        FashionWebEntities db = new FashionWebEntities();
        public ActionResult Index()
        {
            return View();
        }
    }
}