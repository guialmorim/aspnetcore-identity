using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Identity.Models
{
    public class User: IdentityUser
    {
        public string FullName { get; set; }
        public string Member { get; set; } = "Member";
        public string OrganizationId { get; set; }
    }

    public class Organization
    {
        public string Id { get; set; }
        public string Name { get; set; }       
    }
}
