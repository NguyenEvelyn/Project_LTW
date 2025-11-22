using System.Web.Mvc;
using System.Web.Routing;
using System.Web;

namespace Project_LTW.Areas.Admin.Controllers
{
    // CheckAdminAttribute kế thừa từ ActionFilterAttribute
    public class CheckAdminAttribute : ActionFilterAttribute
    {
        // Hàm này được gọi TRƯỚC khi Action của Controller được thực thi
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // LƯU Ý: Đảm bảo Session["AdminUser"] được thiết lập khi đăng nhập thành công
            if (HttpContext.Current.Session["AdminUser"] == null)
            {
                // Nếu không có Session, nghĩa là chưa đăng nhập.
                // Chuyển hướng người dùng về trang đăng nhập Admin.
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Login", action = "Index", Area = "Admin" })
                );
            }
            // Nếu có Session, tiếp tục thực thi Action (base method)
            base.OnActionExecuting(filterContext);
        }
    }
}