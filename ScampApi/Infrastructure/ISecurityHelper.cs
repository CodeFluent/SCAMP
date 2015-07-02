using System.Threading.Tasks;
using DocumentDbRepositories;

namespace ScampApi.Infrastructure
{
    public interface ISecurityHelper
    {
        //string GetIPIDByContext();
        Task<ScampUser> GetCurrentUser();
        Task<ScampUser> GetOrCreateCurrentUser();
        Task<bool> IsGroupManager();
        Task<bool> IsGroupManager(string groupId);
        Task<bool> IsGroupAdmin(string groupId);
        Task<bool> IsGroupAdmin();
        Task<bool> IsSysAdmin();
        Task<bool> CurrentUserCanManageGroup(string groupId);
        Task<bool> CurrentUserCanEditGroupUsers();
    }
}