using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using BookWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = StaticDetails.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CompanyController(IUnitOfWork unitOfWork) 
            {_unitOfWork = unitOfWork;}


        public IActionResult Index() => View(_unitOfWork.Company.GetAll().ToList());

        public IActionResult Upsert(int? id) => id == null || id == 0 ? View(new Company()) : View(_unitOfWork.Company.Get(c => c.Id == id));

        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {
                if (company.Id == 0) _unitOfWork.Company.Add(company);
                else _unitOfWork.Company.Update(company);

                _unitOfWork.Save();
                TempData["success"] = "Company created successfully";
                return RedirectToAction("Index", "Company");
            }
            else return View(company);
        }


        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var companies = _unitOfWork.Company.GetAll().ToList();
            return Json(new { data = companies });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var companyToDelete = _unitOfWork.Company.Get(p => p.Id == id);
            if(companyToDelete == null) return Json(new { success = false, message = "Error while deleting"});

            _unitOfWork.Company.Remove(companyToDelete);
            _unitOfWork.Save();
            var companys = _unitOfWork.Company.GetAll().ToList();
            return Json(new { success = true, message = "Delete successfull"});
        }
        #endregion
    }
}
