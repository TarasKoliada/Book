using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using BookWeb.Models.ViewModels;
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
		public IActionResult Details(int orderId) 
		{
			OrderVM orderVm = new()
			{
				OrderHeader = _unitOfWork.OrderHeader.Get(orderHeader => orderHeader.Id == orderId, includeProperties: "ApplicationUser"),
				OrderDetail = _unitOfWork.OrderDetail.GetAll(orderDetail => orderDetail.OrderHeaderId == orderId, includeProperties: "Product")
			};
			return View(orderVm);
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
