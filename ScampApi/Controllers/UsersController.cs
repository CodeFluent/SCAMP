using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using ScampApi.Infrastructure;
using ScampTypes.ViewModels;
using DocumentDbRepositories.Implementation;
using DocumentDbRepositories;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using ProvisioningLibrary;
using Microsoft.AspNet.Authorization;

namespace ScampApi.Controllers.Controllers
{
    [Authorize]
    [Route("api/users")]
    public class UsersController : Controller
    {
        private readonly ISecurityHelper _securityHelper;
        private readonly IResourceRepository _resourceRepository;
        private readonly IUserRepository _userRepository;
        private readonly IGraphAPIProvider _graphAPIProvider;
        private static IVolatileStorageController _volatileStorageController = null;

        public UsersController(ISecurityHelper securityHelper, IResourceRepository resourceRepository, IUserRepository userRepository, IVolatileStorageController volatileStorageController, IGraphAPIProvider graphAPIProvider)
        {
            _resourceRepository = resourceRepository;
            _userRepository = userRepository;
            _securityHelper = securityHelper;
            _volatileStorageController = volatileStorageController;
            _graphAPIProvider = graphAPIProvider;
        }

        /// <summary>
        /// does a 'starts with' search against Azure AD User Principal Names
        ///  returning the first item that matches
        /// </summary>
        /// <param name="searchparm"></param>
        /// <returns>returns the user's Id and Name if found, otherwise returns null</returns>
        [HttpGet("FindbyUPN/{searchparm}")]
        public async Task<IActionResult> findUserbyUPN(string searchparm)
        {
            //TODO: implement search
            List<UserSummary> foundusers = await _graphAPIProvider.FindUser(searchparm);

            return new ObjectResult(foundusers) { StatusCode = 200 };
        }

        // get a list of the user's resources
        // GET /api/user/{userid}/resources/
        [HttpGet("{userId}/resources", Name = "User.GetResourcesForUser")]
        public async Task<IActionResult> GetResourcesforUser(string userId)
        {
            List<ScampResourceSummary> resourceList = new List<ScampResourceSummary>();
            ScampUser currentUser = await _securityHelper.GetOrCreateCurrentUser();

            // request must be systemAdmin, or the requesting user
            if (!currentUser.IsSystemAdmin && currentUser.Id != userId)
                return new ObjectResult("User is not authorized to perform this action against specific resource(s)") { StatusCode = 401 };

            // execute query
            ScampUser user = await _userRepository.GetUserById(userId);
            if (user == null)
                return new ObjectResult("requested resource not available") { StatusCode = 204 };

            foreach (ScampUserGroupMbrship groupMbrship in user.GroupMembership)
            {
                foreach (ScampUserGroupResources resource in groupMbrship.Resources)
                {
                    // get resource state from volatile store
                    CurrentResourceState currentState = await _volatileStorageController.GetResourceState(resource.Id);

                    ScampResourceSummary tmpSummary = new ScampResourceSummary()
                    {
                        Id = resource.Id,
                        Name = resource.Name,
                        Type = resource.type,
                        State = currentState.State,
                        totUnitsUsed = currentState.UnitsUsed
                    };

                    resourceList.Add(tmpSummary);
                }
            }

            return new ObjectResult(resourceList) { StatusCode = 200 };
        }

        private UserSummary map(ScampUser docDbUSer)
		{
			return new UserSummary
			{
				Id = docDbUSer.Id,
				Name = docDbUSer.Name
			};
		}
	}
}
