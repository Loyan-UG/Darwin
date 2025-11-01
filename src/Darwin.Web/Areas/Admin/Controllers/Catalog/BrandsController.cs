using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Areas.Admin.Controllers.Catalog
{
    public class BrandsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
