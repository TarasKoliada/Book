using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using BookWeb.Models.ViewModels;
using BookWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookWeb.Areas.Admin.Controllers
{
	[Area("admin")]
	[Authorize]
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
		[HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
        public IActionResult StartProcessing() 
		{
			_unitOfWork.OrderHeader.UpdateStatus(OrderVm.OrderHeader.Id, StaticDetails.StatusInProcess);
			_unitOfWork.Save();

			TempData["Success"] = "Order Processing is Successfull";

			return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id});
		}

		#region API CALLS
		[HttpGet]
		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeader> orders;

			if (User.IsInRole(StaticDetails.Role_Admin) || User.IsInRole(StaticDetails.Role_Employee))
				orders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();

			else
			{
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

				orders = _unitOfWork.OrderHeader.GetAll(o => o.ApplicationUserId == userId, includeProperties: "ApplicationUser");
			}

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
