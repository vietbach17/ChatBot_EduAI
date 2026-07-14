using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
// using Microsoft.AspNetCore.Authorization; // Un-comment when TV1 sets up auth

namespace PresentationLayer.Pages.Admin
{
    // [Authorize(Roles = "Admin")]
    public class PaymentHistoryModel : PageModel
    {
        private readonly IPaymentHistoryService _historyService;

        public PaymentHistoryModel(IPaymentHistoryService historyService)
        {
            _historyService = historyService;
        }

        public IEnumerable<PaymentHistoryDto> Histories { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedMethod { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedStatus { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Histories = await _historyService.GetAllPaymentHistoriesAsync(SearchTerm, SelectedMethod, SelectedStatus);
            return Page();
        }
    }
}
