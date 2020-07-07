using System;
using System.ComponentModel.DataAnnotations;
using WebAPI.Formatters;

namespace WebAPI.Model.Responses
{
    public class RequestCSVResponse
    {
        [CSVExportType("Text")] // Not necessary, applied here only to show how to use the custom attribute
        [Display(Name = "ref_no")]
        public Guid RefNo { get; set; }

        [Display(Name = "request_status")]
        public string RequestStatus { get; set; }

        [Display(Name = "user_email")]
        public string UserEmail { get; set; }

        [Display(Name = "quantity")]
        public long Qty { get; set; }

        [Display(Name = "product_name")]
        public string ProductDisplayName { get; set; }

        [Display(Name = "product_price")]
        public decimal ProductPrice { get; set; }

        [Display(Name = "product_currency")]
        public string ProductPriceCurrency { get; set; }
    }
}
