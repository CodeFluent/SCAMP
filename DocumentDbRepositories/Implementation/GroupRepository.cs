using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace DocumentDbRepositories.Implementation
{

    internal class GroupRepository : IGroupRepository
    {
        DocDb docdb;

        public GroupRepository(DocDb docdb)
        {
            this.docdb = docdb;
        }

        public async Task<ScampResourceGroup> GetGroup(string groupId)
        {
            if (!(await docdb.IsInitialized))
                return null;

            var query = from g in docdb.Client.CreateDocumentQuery<ScampResourceGroup>(docdb.Collection.SelfLink)
                             where g.Id == groupId && g.Type == "group"
                             select g;
            return await query.AsDocumentQuery().FirstOrDefaultAsync();
        }

        public async Task<List<ScampResourceGroup>> GetGroupsByBudgetOwner(string userId)
        {
            if (!(await docdb.IsInitialized))
                return null;

            var groupQuery = from g in docdb.Client.CreateDocumentQuery<ScampResourceGroup>(docdb.Collection.SelfLink)
                             where g.Budget.OwnerId == userId && g.Type == "group"
                             select g;
            List<ScampResourceGroup> groupList = await groupQuery.AsDocumentQuery().ToListAsync();

            return groupList;
        }


        public async Task<IEnumerable<ScampResourceGroup>> GetGroups()
        {
            if (!(await docdb.IsInitialized))
                return null;

            var query =
                docdb.Client.CreateDocumentQuery<ScampResourceGroup>(docdb.Collection.SelfLink)
                    .Where(u => u.Type == "group");
            return await query.AsDocumentQuery().ToListAsync();
        }

        public async Task<IEnumerable<ScampResourceGroup>> GetGroupsByUser(string userId)
        {
            if (!(await docdb.IsInitialized))
                return null;

            // Want to be able to write this in LINQ (but get a runtime exception):
            //var groups =
            //    (from grp in docdb.Client.CreateDocumentQuery<ScampResourceGroup>(docdb.Collection.SelfLink)
            //    where grp.Type == "group"
            //    from admin in grp.Admins
            //    where admin.Id == user.Id
            //    select grp)
            //    .ToList();
            var sql = new SqlQuerySpec
            {
                QueryText = " SELECT VALUE g" +
                                " FROM groups g " +
                                " JOIN admin in g.admins" +
                                " WHERE g.type = 'group'" +
                                " AND admin.id = @adminId",
                Parameters = new SqlParameterCollection
                    {
                       new SqlParameter { Name = "@adminId", Value = userId }
                    }
            };
            var query = docdb.Client.CreateDocumentQuery<ScampResourceGroup>(docdb.Collection.SelfLink, sql);
            return await query.AsDocumentQuery().ToListAsync();
        }

        public async Task CreateGroup(ScampResourceGroup newGroup)
        {
            if (!(await docdb.IsInitialized))
                return;

            try
            {
                StoredProcedure sproc = docdb.Client.CreateStoredProcedureQuery(docdb.Collection.SelfLink)
                    .Where(s => s.Id == "CreateGroup").AsEnumerable().FirstOrDefault();

                await docdb.Client.ExecuteStoredProcedureAsync<dynamic>(sproc.SelfLink, newGroup);
            }
            catch (Exception ex)
            {
                //TODO: log issue
                throw;
            }

        }

        public async Task AddUserToGroup(string groupId, string userId, bool isManager)
        {
            if (!(await docdb.IsInitialized))
                return;

            try
            {
                StoredProcedure sproc = docdb.Client.CreateStoredProcedureQuery(docdb.Collection.SelfLink)
                    .Where(s => s.Id == "AddUserToGroup").AsEnumerable().FirstOrDefault();

                await docdb.Client.ExecuteStoredProcedureAsync<dynamic>(sproc.SelfLink, groupId, userId);
            }
            catch (Exception ex)
            {
                //TODO: log issue
                throw;
            }


        }
        public async Task UpdateUserInGroup(string groupId, string userId, bool isManager)
        {
            if (!(await docdb.IsInitialized))
                return;

            try
            {
                StoredProcedure sproc = docdb.Client.CreateStoredProcedureQuery(docdb.Collection.SelfLink)
                    .Where(s => s.Id == "UpdateUserInGroup").AsEnumerable().FirstOrDefault();

                StoredProcedureResponse<dynamic> response = await docdb.Client.ExecuteStoredProcedureAsync<dynamic>(sproc.SelfLink, groupId, userId, isManager);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task RemoveUserFromGroup(string groupId, string userId)
        {
            if (!(await docdb.IsInitialized))
                return;

            try
            {
                StoredProcedure sproc = docdb.Client.CreateStoredProcedureQuery(docdb.Collection.SelfLink)
                    .Where(s => s.Id == "RemoveUserFromGroup").AsEnumerable().FirstOrDefault();

                StoredProcedureResponse<dynamic> response = await docdb.Client.ExecuteStoredProcedureAsync<dynamic>(sproc.SelfLink, groupId, userId);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Get resources for a specific group member
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ScampUserGroupResources>> GetGroupMemberResources(string groupId, string userId)
        {
            if (!(await docdb.IsInitialized))
                return null;

            try
            {
                // get resources for the specified user and group
                //var query = docdb.Client.CreateDocumentQuery<ScampResourceGroup>(docdb.Collection.SelfLink)
                //            .Select(g => g.Members
                //                .Where(group => group.Id == groupId)
                //            .Select(mbrs => mbrs.Resources
                //                .Where(members => members.Id == userId)
                //                .SelectMany(resources => mbrs.Resources)));

                var sql = new SqlQuerySpec
                {
                    QueryText = "SELECT value rcs from groups" +
                        " JOIN mbrs in groups.members" +
                        " JOIN rcs in mbrs.resources" +
                        " where groups.id = @groupId" +
                        " and mbrs.id = @userId",
                    Parameters = new SqlParameterCollection
                    {
                       new SqlParameter { Name = "@groupId", Value = groupId },
                       new SqlParameter { Name = "@userId", Value = userId }
                    }
                };
                var query = docdb.Client.CreateDocumentQuery<ScampUserGroupResources>(docdb.Collection.SelfLink, sql);
                return await query.AsDocumentQuery().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task UpdateGroup(string groupID, ScampResourceGroup group)
        {

            if (!(await docdb.IsInitialized))
                return;

            var query = docdb.Client.CreateDocumentQuery(docdb.Collection.SelfLink)
                .Where(d => d.Id == groupID);
            var document = await query.AsDocumentQuery().FirstOrDefaultAsync();
			await docdb.Client.ReplaceDocumentAsync(document.SelfLink, group);
        }

	}
}
