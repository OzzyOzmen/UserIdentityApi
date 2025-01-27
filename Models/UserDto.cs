﻿using System.ComponentModel.DataAnnotations;

namespace UserIdentityApi.Models
{
    public class UserDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string UserName { get; set; }

        public string PhoneNumber { get; set; }
    }
}