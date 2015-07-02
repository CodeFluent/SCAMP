using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using ScampApi.Infrastructure;
using ScampTypes.ViewModels;
using DocumentDbRepositories;
using ProvisioningLibrary;

namespace ScampApi.Controllers
{
  [Route("api/user")]
  public class UserController : Controller
  {
    private readonly ISecurityHelper _securityHelper;
    private readonly IResourceRepository _resourceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGraphAPIProvider _graphAPIProvider;
    private static IVolatileStorageController _volatileStorageController = null;

    public UserController(ISecurityHelper securityHelper, IResourceRepository resourceRepository, IUserRepository userRepository, IVolatileStorageController volatileStorageController, IGraphAPIProvider graphAPIProvider)
    {
      _resourceRepository = resourceRepository;
      _userRepository = userRepository;
      _securityHelper = securityHelper;
      _volatileStorageController = volatileStorageController;
      _graphAPIProvider = graphAPIProvider;
    }

    /// <summary>
    /// retrieves current user
    /// GET: api/user
    /// </summary>
    /// <returns></returns>
    [HttpGet(Name = "User.CurrentUser")]
    public async Task<User> Get()
    {
        // fetch current user
        ScampUser currentUser = await _securityHelper.GetCurrentUser();
        if (currentUser == null)
        {
            currentUser = await _securityHelper.GetOrCreateCurrentUser();
            // Check if this was the first user created to the system
            int userCount = await _userRepository.GetUserCount();
            if (userCount == 1)
            {
                // Make the first user of the system a system admin so that
                // it has the permissions to setup the system
                currentUser.IsSystemAdmin = true;
                await _userRepository.UpdateUser(currentUser);
            }
        }

        // Create user object to be returned
        var user = new User()
        {
            Id = currentUser.Id,
            Name = currentUser.Name,
            Email = currentUser.Email,
            isSystemAdmin = await _securityHelper.IsSysAdmin(),
            isGroupAdmin = await _securityHelper.IsGroupAdmin(),
            isGroupManager = await _securityHelper.IsGroupManager()
        };

        return user;
    }
  }
}
