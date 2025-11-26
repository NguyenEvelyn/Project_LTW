using System.Web.Mvc;
using System.Web.Routing;
using System.Web;

namespace Project_LTW.Areas.Admin.Controllers
{
    // CheckAdminAttribute 
    public class CheckAdminAttribute : ActionFilterAttribute
    {
        
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
        
            if (HttpContext.Current.Session["AdminUser"] == null)
            {
                
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Login", action = "Index", Area = "Admin" })
                );
            }
       
            base.OnActionExecuting(filterContext);
        }
    }
}