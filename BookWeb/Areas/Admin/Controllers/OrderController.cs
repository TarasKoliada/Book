using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using BookWeb.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace BookWeb.Areas.Admin.Controllers
{
	[Area("admin")]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
		{
			return View();
		}

		#region API CALLS
		[HttpGet]
		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeader> orders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();

			switch (status)
			{
				case "inprocess":
					orders = orders.Where(o => o.OrderStatus == StaticDetails.StatusInProcess);
					break;
				case "pending":
					orders = orders.Where(o => o.OrderStatus == StaticDetails.StatusPending);
					break;
				case "completed":
                    orders = orders.Where(o => o.OrderStatus == StaticDetails.StatusShipped);
                    break;
				case "approved":
                    orders = orders.Where(o => o.OrderStatus == StaticDetails.StatusApproved);
                    break;
                default:
					break;
			}

			return Json(new { data = orders });
		}
		#endregion
	}
}
