using Microsoft.AspNetCore.Mvc;

namespace ArcheryAlley.Controllers
{
    public class EquipmentController : Controller
    {
        // GET: /Equipment/MemberEquipment
        public IActionResult MemberEquipment()
        {
            return View();
        }
    }
}
