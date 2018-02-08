﻿// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Controllers.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using IdentityServer4.Extensions;
    using Ironclad.Application;
    using Ironclad.Client;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        // manage users
        // list, add, remove, manage roles

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        public async Task<IActionResult> Get(int skip = default, int take = 20)
        {
            skip = Math.Max(0, skip);
            take = take < 0 ? 20 : Math.Min(take, 100);

            var totalSize = await this.userManager.Users.CountAsync();

            var users = await this.userManager.Users.Skip(skip).Take(take).ToListAsync();
            var resources = users.Select(
                item =>
                new UserSummaryResource
                {
                    Url = this.HttpContext.GetIdentityServerRelativeUrl("~/api/users/" + item.Id),
                    Id = item.Id,
                    UserName = item.UserName,
                    Email = item.Email
                });

            var resourceSet = new ResourceSet<UserSummaryResource>(skip, totalSize, resources);

            return this.Ok(resourceSet);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var user = await this.userManager.Users.SingleOrDefaultAsync(item => item.Id == id);

            if (user == null)
            {
                this.NotFound();
            }

            return this.Ok(
                new UserResource
                {
                    Url = this.HttpContext.GetIdentityServerRelativeUrl("~/api/users/" + user.Id),
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                });
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]User model)
        {
            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
            };

            if (await this.userManager.FindByNameAsync(model.UserName) != null)
            {
                return this.StatusCode((int)HttpStatusCode.Conflict, new { Message = "Username already used" });
            }

            if (await this.userManager.FindByEmailAsync(model.Email) != null)
            {
                return this.StatusCode((int)HttpStatusCode.Conflict, new { Message = "Email already used" });
            }

            var result = await this.userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                this.Response.Headers.Add("Location", this.HttpContext.GetIdentityServerRelativeUrl("~/api/users/" + user.Id));

                return this.Ok();
            }

            return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = result.ToString() });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody]User model)
        {
            model.Id = id;

            var user = await this.userManager.FindByIdAsync(id);

            if (user == null)
            {
                return this.NotFound(new { Message = "User doesn't exist" });
            }

            user.Email = model.Email ?? user.Email;
            user.PhoneNumber = model.PhoneNumber ?? user.PhoneNumber;

            var result = await this.userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return this.Ok();
            }

            return this.BadRequest(new { Message = result.ToString() });
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromForm]string currentPassword, [FromForm]string newPassword)
        {
            var user = await this.userManager.FindByIdAsync(id);

            if (user == null)
            {
                return this.NotFound(new { Message = "User doesn't exist" });
            }

            var result = await this.userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                return this.Ok();
            }

            return this.BadRequest(new { Message = result.ToString() });
        }

#pragma warning disable SA1124
        #region Role Assignment

        [HttpGet("{id}/roles")]
        public async Task<IActionResult> GetAssignedRoles(string id)
        {
            var user = await this.userManager.FindByIdAsync(id);

            var roles = await this.roleManager.Roles.ToListAsync();

            var userRoles = new List<string>();

            foreach (var role in roles)
            {
                if (await this.userManager.IsInRoleAsync(user, role.Name))
                {
                    userRoles.Add(role.Name);
                }
            }

            return this.Ok(
                new
                {
                    Url = this.HttpContext.GetIdentityServerRelativeUrl(string.Concat("~/api/users/", user.Id, "/roles")),
                    Roles = userRoles
                });
        }

        [HttpPatch("{id}/roles")]
        public async Task<IActionResult> Post(string id, [FromBody]List<string> roles)
        {
            var user = await this.userManager.FindByIdAsync(id);

            if (user == null)
            {
                return this.NotFound(new { Message = "User doesn't exist" });
            }

            await this.userManager.AddToRolesAsync(user, roles);

            return this.Ok();
        }

        [HttpDelete("{id}/roles")]
        public async Task<IActionResult> Delete(string id, [FromBody]List<string> roles)
        {
            var user = await this.userManager.FindByIdAsync(id);

            if (user == null)
            {
                return this.NotFound(new { Message = "User doesn't exist" });
            }

            await this.userManager.RemoveFromRolesAsync(user, roles);

            return this.Ok();
        }

        #endregion Role Assignment

#pragma warning disable CA1034, CA1056
        public class UserResource : User
        {
            public string Url { get; set; }
        }

        private class UserSummaryResource : UserSummary
        {
            public string Url { get; set; }
        }
    }
}
