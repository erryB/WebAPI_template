using System;
using System.Collections.Generic;

namespace WebAPI.Model.Database
{
    public class Product
    {
        public Product()
        {
            RequestDetails = new HashSet<RequestDetail>();
        }

        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public decimal Price { get; set; }
        public string PriceCurrency { get; set; }

        public virtual ICollection<RequestDetail> RequestDetails { get; set; }
    }
}