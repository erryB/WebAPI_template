using System;

namespace WebAPI.Model.Database
{
    public class RequestDetail
    {
        public Guid Id { get; set; }
        public long Qty { get; set; }

        public virtual Product Product { get; set; }
        public virtual Request Request { get; set; }
    }
}