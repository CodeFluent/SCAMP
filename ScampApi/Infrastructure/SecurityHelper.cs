using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DocumentDbRepositories;
using DocumentDbRepositories.Implementation;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using ProvisioningLibrary;

namespace ScampApi.Infrastructure
{

    public class SecurityHelper : ISecurityHelper
    {
        private readonly HttpContext Context;
        private readonly IUserRepository _userRepository;
        private readonly IGroupRepository _groupRepository;
        private ScampUser cachedCurrentUser;

        public SecurityHelper(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository, IGroupRepository groupRepository)
        {
            Context = httpContextAccessor.HttpContext;
            _userRepository = userRepository;
            _groupRepository = groupRepository;
        }

        // retrieves the current user
        public async Task<ScampUser> GetCurrentUser()
        {
            // get current user's Id from security context
            var userId = Context.User.Claims.FirstOrDefault(c => c.Type.Contains("objectidentifier")).Value;
            return await _userRepository.GetUserById(userId); ;
        }

        // retrieves the current user and creates it if it is not yet found
        public async Task<ScampUser> GetOrCreateCurrentUser()
        {
            ScampUser currentUser = await GetCurrentUser();
            if (currentUser == null) // insert if user doesn't exist
            {
                var userId = Context.User.Claims.FirstOrDefault(c => c.Type.Contains("objectidentifier")).Value;
                // build user object
                currentUser = new ScampUser()
                {
                    Id = userId,
                    Name =
                        string.Format("{0} {1}", Context.User.FindFirst(ClaimTypes.GivenName).Value,
                            Context.User.FindFirst(ClaimTypes.Surname).Value).Trim(),
                    IsSystemAdmin = false,
                    // get email address
                    Email = Context.User.Claims.FirstOrDefault(c => c.Type.Contains("email") || c.Type.Contains("upn")).Value
                };

                // insert into database   
                await _userRepository.CreateUser(currentUser);
                // fetch from database so that the returned object will have
                // proper SelfLink property
                currentUser = await _userRepository.GetUserById(userId);
            }

            return currentUser;
        }

        /// <summary>
        /// checks to see if the user is a group manager of any group they are a member of
        /// </summary>
        /// <returns>true if the user is</returns>
        public async Task<bool> IsGroupManager()
        {
            var user = await GetOrCreateCurrentUser();
            // user is a manager of this group if they are in the group membership list and are flagged "isManager"
            var checkMgr = user.GroupMembership.ToList().Any(q => q.isManager);
            return checkMgr;
        }

        /// <summary>
        /// checks to see if the user is a group manager
        /// </summary>
        /// <param name="groupId">Id of group to be checked</param>
        /// <returns>true if the user is</returns>
        public async Task<bool> IsGroupManager(string groupId)
        {
            var user = await GetOrCreateCurrentUser();
            var grp = await _groupRepository.GetGroup(groupId);
            // user is a manager of this group if they are in the group membership list and are flagged "isManager"
            var checkMgr = grp.Members.ToList().Any(q => q.Id == user.Id && q.isManager);
            return checkMgr;
        }

        /// <summary>
        /// checks to see if the user is a group admin  of a specific group (aka the owner)
        /// </summary>
        /// <param name="groupId">Id of group to be checked</param>
        /// <returns>true is the user is</returns>
        public async Task<bool> IsGroupAdmin(string groupId)
        {
            var user = await GetOrCreateCurrentUser();
            var grp = await _groupRepository.GetGroup(groupId);
            // user is a manager of this group if they are in the group membership list and are flagged "isManager"
            var checkMgr = (grp.Budget.OwnerId == user.Id);
            return checkMgr;
        }

        /// <summary>
        /// checks to see if the user is a group manager
        /// </summary>
        /// <returns>true is the user is</returns>
        public async Task<bool> IsGroupAdmin()
        {
            var user = await GetOrCreateCurrentUser();
            // user is a group admin if they have a budget > 0
            var checkMgr = user.budget != null && user.budget.unitsBudgeted > 0;
            return checkMgr;
        }

        public async Task<bool> IsSysAdmin()
        {
            ScampUser user = await GetOrCreateCurrentUser();

            return user.IsSystemAdmin;
        }
        public async Task<bool> CurrentUserCanManageGroup(string groupId)
        {
            var currentUser = await GetOrCreateCurrentUser();
            var group = await _groupRepository.GetGroup(groupId);
            // Groups can be managers by system and group admins and the managers
            // of the group in question.
            return await IsSysAdmin() ||
                   await IsGroupAdmin() ||
                   group.Members.Any(u => u.Id == currentUser.Id && u.isManager);
        }

        public async Task<bool> CurrentUserCanEditGroupUsers()
        {
            return await IsSysAdmin() || await IsGroupAdmin();
        }
    }
}