﻿using Microsoft.AspNetCore.Identity;

namespace UserIdentityApi.Data.Entities
{
    public class UserLogin : IdentityUserLogin<int>
    {
        public virtual User User { get; set; }
    }
}