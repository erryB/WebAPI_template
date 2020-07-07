using System;

namespace WebAPI.Model.Requests
{
    public class ProductRequestRequest
    {
        public Guid Id { get; set; }
        public long Quantity { get; set; }
    }
}
