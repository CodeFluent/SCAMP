
using System;
using ProvisioningLibrary;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Microsoft.Framework.ConfigurationModel;
using ScampTypes.ViewModels;

namespace ProvisioningLibrary
{
    public class VolatileStorageController : IVolatileStorageController
    {
        // 2 storage accounts (can be one connection string) 
        // 2 tables one for activity log and one for state (can be same table)
        // it does done this way if you want to fan out the system to multiple 
        // table & storage account 
        private CloudStorageAccount _ActivityLogStorageAccount = null;
        private CloudStorageAccount _StateUpdateStorageAccount = null;
        private CloudTableClient    _ActivityLogTableClient = null;
        private CloudTableClient    _StateUpdateTableClient = null;
        private CloudTable _ActivityLogTable = null;
        private CloudTable _StateUpdateTable = null;

        private static IConfiguration Configuration { get; set; }

        private const int _ReadBackWindow = -10;

        private void Connect()
        {
            Debug.WriteLine("Storage for VolatileStorge Controller is not connected");

                string sActivityLogStorageConnectionString = VolatileStorageController.Configuration["ActivityLogStorage:ConnectionString"];
                string sUpdateStateStorageConnectionString = VolatileStorageController.Configuration["ResourceStateStorage:ConnectionString"];
                string sActivityLogTableName               = VolatileStorageController.Configuration["ActivityLogStorage:TableName"];
                string sStateUpdateLogTableName            = VolatileStorageController.Configuration["ResourceStateStorage:TableName"];

                this._ActivityLogStorageAccount = CloudStorageAccount.Parse(sActivityLogStorageConnectionString);
                this._StateUpdateStorageAccount = CloudStorageAccount.Parse(sUpdateStateStorageConnectionString);
                
                this._ActivityLogTableClient = this._ActivityLogStorageAccount.CreateCloudTableClient();
                this._StateUpdateTableClient = this._StateUpdateStorageAccount.CreateCloudTableClient();

                this._ActivityLogTable = this._ActivityLogTableClient.GetTableReference(sActivityLogTableName);
                this._ActivityLogTable.CreateIfNotExists();
                this._StateUpdateTable = this._StateUpdateTableClient.GetTableReference(sStateUpdateLogTableName);
                this._StateUpdateTable.CreateIfNotExists();

            Debug.WriteLine("Storage for VolatileStorage Controller is now connected");
        }
        
         
        public VolatileStorageController()
        {
            Trace.WriteLine("a new instance of VolatileStorageController has been created.");

            if (null == VolatileStorageController.Configuration)
            {
                // Setup configuration sources.
                VolatileStorageController.Configuration = new Configuration()
                    .AddEnvironmentVariables("APPSETTING_");


                if(
                    string.IsNullOrEmpty(VolatileStorageController.Configuration["ActivityLogStorage:ConnectionString"]) ||
                    string.IsNullOrEmpty(VolatileStorageController.Configuration["ResourceStateStorage:ConnectionString"]) ||
                    string.IsNullOrEmpty(VolatileStorageController.Configuration["ActivityLogStorage:TableName"]) ||
                    string.IsNullOrEmpty(VolatileStorageController.Configuration["ResourceStateStorage:TableName"])
                  )
                    throw new InvalidOperationException(@"couldn't load config either ENV or Config");
            }

            Connect(); 
            // each instance attached to controller is either singlton or instance. 
            // keep table storage connection open doesn't hurt performance and have
            // a little memory footprint. 
        }

        public async Task CreateActivityLog(ActivityLog activityLog)
        {
            TableOperation insertOperation = TableOperation.Insert(activityLog);
            await this._ActivityLogTable.ExecuteAsync(insertOperation);
        }

        public async Task CreateActivityLog(List<ActivityLog> activityLogs)
        {
            // assuming that a 100 activity 
            // log is les than a 4 megs. 

            if (activityLogs.Count > 100)
                throw new InvalidOperationException(@"Can't insert more than a 100 activity log at a time");

            TableBatchOperation tbo = new TableBatchOperation();
            activityLogs.ForEach(log =>
            {
                tbo.Add(TableOperation.Insert(log));
            });

            await this._ActivityLogTable.ExecuteBatchAsync(tbo);
        }

        public async Task<List<ActivityLog>> GetRecentActivityLogForRequest(string RequestId)
        {
            string dateWhere = TableQuery.GenerateFilterCondition("RowKey", 
                                                            QueryComparisons.GreaterThanOrEqual, 
                                                            DateTime.UtcNow.AddMinutes(_ReadBackWindow).Ticks.ToString());
            string RequestWhere = TableQuery.GenerateFilterCondition("RequestId", 
                                                                     QueryComparisons.Equal, 
                                                                     RequestId);

            string finalFilter = TableQuery.CombineFilters(
                   dateWhere,
                   TableOperators.And,
                   RequestWhere);

            TableQuery<ActivityLog> query = new TableQuery<ActivityLog>().Where
               (finalFilter);

            TableContinuationToken token = null;
            List<ActivityLog> activtyLog = new List<ActivityLog>();

            do
            {
                var queryResult = await this._ActivityLogTable.ExecuteQuerySegmentedAsync<ActivityLog>(query, token);
                token = queryResult.ContinuationToken;
                activtyLog.AddRange(queryResult);

            } while (token != null);
            return activtyLog;
        }

        public async Task<List<ActivityLog>> GetRecentActivityLogForResource(string resouceId)
        {

            string dateWhere = TableQuery.GenerateFilterCondition("RowKey",
                                                            QueryComparisons.GreaterThanOrEqual,
                                                            DateTime.UtcNow.AddMinutes(_ReadBackWindow).Ticks.ToString());
            string ResourceId = TableQuery.GenerateFilterCondition("ResourceId",
                                                                     QueryComparisons.Equal,
                                                                     resouceId);

            string finalFilter = TableQuery.CombineFilters(
                   dateWhere,
                   TableOperators.And,
                   ResourceId);

            TableQuery<ActivityLog> query = new TableQuery<ActivityLog>().Where(finalFilter);

            
            TableContinuationToken token = null;
            List<ActivityLog> activtyLog = new List<ActivityLog>();

            do
            {
                var queryResult = await this._ActivityLogTable.ExecuteQuerySegmentedAsync<ActivityLog>(query, token);
                token = queryResult.ContinuationToken;
                activtyLog.AddRange(queryResult);

            } while (token != null);

            return activtyLog;
        }

        public async Task<CurrentResourceState> GetResourceState(string resourceId)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<CurrentResourceState>(resourceId, resourceId);
            TableResult retrievedResult = await _StateUpdateTable.ExecuteAsync(retrieveOperation);
            return (CurrentResourceState)retrievedResult.Result;
        }

        public async Task UpdateResourceState(CurrentResourceState newstate)
        {
            TableOperation insertOperation = TableOperation.InsertOrMerge(newstate);
            await this._StateUpdateTable.ExecuteAsync(insertOperation);
        }

        public async Task UpdateResourceState(string resourceId, ResourceState state)
        {
            // get current resource state
            CurrentResourceState updatedState = await this.GetResourceState(resourceId);
            if (updatedState == null)
                updatedState = new CurrentResourceState(resourceId);

            // update with new state
            updatedState.State = state;

            // save update
            await UpdateResourceState(updatedState);
        }

        public async Task<GroupBudgetState> GetGroupBudgetState(string groupId)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<GroupBudgetState>(groupId, groupId);
            TableResult retrievedResult = await _StateUpdateTable.ExecuteAsync(retrieveOperation);
            return (GroupBudgetState)retrievedResult.Result;
        }

        public async Task<List<UserBudgetState>> GetUserBudgetStates(string userId)
        {
            TableContinuationToken token = null;

            TableQuery<UserBudgetState> query = new TableQuery<UserBudgetState>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId));
            var results = await _StateUpdateTable.ExecuteQuerySegmentedAsync(query, token).ConfigureAwait(false);
                
            return results.ToList();
        }

        public async Task<UserBudgetState> GetUserBudgetState(string userId, string groupId)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<UserBudgetState>(userId, groupId);
            TableResult retrievedResult = await _StateUpdateTable.ExecuteAsync(retrieveOperation);
            return (UserBudgetState)retrievedResult.Result;
        }

        public async Task UpdateUserBudgetAllocation(string userId, string groupId, long newBudgetAllocation)
        {
            var budget = await GetUserBudgetState(userId, groupId);
            budget.UnitsBudgetted = newBudgetAllocation;
            await AddUserBudgetState(budget);
            return;
        }

        public async Task AddUserBudgetState(UserBudgetState budget)
        {
            TableOperation operation = TableOperation.InsertOrReplace(budget);
            TableResult retrievedResult = await _StateUpdateTable.ExecuteAsync(operation);
            return;
        }

        public async Task AddGroupBudgetState(GroupBudgetState budget)
        {
            TableOperation operation = TableOperation.InsertOrReplace(budget);
            TableResult retrievedResult = await _StateUpdateTable.ExecuteAsync(operation);
            return;
        }

        public async Task DeleteUserBudgetState(string userId, string groupId)
        {
            var budget = await GetUserBudgetState(userId, groupId);
            TableOperation operation = TableOperation.Delete(budget);
            TableResult retrievedResult = await _StateUpdateTable.ExecuteAsync(operation);
            return;
        }

    }
}
