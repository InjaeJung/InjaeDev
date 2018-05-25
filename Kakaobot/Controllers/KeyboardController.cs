using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Kakaobot.Controllers
{
    public class KeyboardController : Controller
    {
        // GET: Keyboard
        public ActionResult Index()
        {
            Models.Keyboard keyboard = new Models.Keyboard
            {
                type = "buttons",
                buttons = new string[] { "!동방상태", "!카드키" }
            };

            return Json(keyboard, JsonRequestBehavior.AllowGet);
        }
    }
}