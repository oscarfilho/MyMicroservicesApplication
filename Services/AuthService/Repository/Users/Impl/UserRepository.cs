﻿using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repository.Users.Impl
{
    public class UserRepository : IUserRepository
    {
        /// <summary>
        /// Check the user credentials
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>User claims</returns>
        public static async Task<ClaimsIdentity> GetIdentityAsync(string username, string password)
        {
            ClaimsIdentity cIdentity = null;
            
            using (var context = DbContextFactory.CreateTestDbContext())
            {
                var user = await context.TBU_users.FirstOrDefaultAsync(i => i.username == username);
                
                if (user != null)
                {
                    using (var md5Hash = MD5.Create())
                    {
                        var hash = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                        var sBuilder = new StringBuilder();
                        
                        foreach (var t in hash)
                        {
                            sBuilder.Append(t.ToString("x2"));
                        }
                        
                        if (user.password.Equals(sBuilder.ToString()))
                        {
                            cIdentity = new ClaimsIdentity(
                                new GenericIdentity(username, "Token"),
                                new Claim[]
                                {
                                    new Claim("Admin", user.level == 0 ? "true": "false")
                                });
                        }
                    }
                }
            }

            return cIdentity;
        }
    }
}