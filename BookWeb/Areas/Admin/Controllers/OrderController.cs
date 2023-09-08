using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using BookWeb.Models.ViewModels;
using BookWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace BookWeb.Areas.Admin.Controllers
{
	[Area("admin")]
	public class OrderController : Controller
	{
		[BindProperty]
		public OrderVM OrderVm { get; set; }
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
			OrderVm = new()
			{
				OrderHeader = _unitOfWork.OrderHeader.Get(orderHeader => orderHeader.Id == orderId, includeProperties: "ApplicationUser"),
				OrderDetail = _unitOfWork.OrderDetail.GetAll(orderDetail => orderDetail.OrderHeaderId == orderId, includeProperties: "Product")
			};
			return View(OrderVm);
		}

		[HttpPost]
		[Authorize(Roles = StaticDetails.Role_Admin +","+ StaticDetails.Role_Employee)]
		public IActionResult UpdateOrderDetails()
		{
			var orderToUpdate = _unitOfWork.OrderHeader.Get(o => o.Id == OrderVm.OrderHeader.Id);
			orderToUpdate.Name = OrderVm.OrderHeader.Name;
			orderToUpdate.PhoneNumber = OrderVm.OrderHeader.PhoneNumber;
			orderToUpdate.StreetAddress = OrderVm.OrderHeader.StreetAddress;
			orderToUpdate.City = OrderVm.OrderHeader.City;
			orderToUpdate.State = OrderVm.OrderHeader.State;
			orderToUpdate.PostalCode = OrderVm.OrderHeader.PostalCode;

			if (!string.IsNullOrEmpty(OrderVm.OrderHeader.Carrier))
				orderToUpdate.Carrier = OrderVm.OrderHeader.Carrier;

			if (!string.IsNullOrEmpty(OrderVm.OrderHeader.TrackingNumber))
				orderToUpdate.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;

			_unitOfWork.OrderHeader.Update(orderToUpdate);
			_unitOfWork.Save();

			TempData["Success"] = "Order Details Updated Successfully";

			return RedirectToAction(nameof(Details), new { orderId = orderToUpdate.Id});
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
