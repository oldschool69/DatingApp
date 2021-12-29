using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace API.Helpers
{
    public class MessageParams : PaginationParams
    {
        public string Username { get; set; }

        public string Container { get; set; } = "Unread";
        
    }
}