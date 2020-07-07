using System;
using System.Collections.Generic;

namespace WebAPI.Model.Requests
{
    public class NewRequestRequest
    {
        public Guid? RefNo { get; set; }
        public IEnumerable<ProductRequestRequest> SelectedProducts { get; set; }
    }
}
