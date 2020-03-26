using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using RegisterTaskWithOutlook.Entities;

namespace RegisterTaskWithOutlook.Helpers
{
    public class TableHelper
    {
        private static CloudTable GetTasksTable()
        {

            var accountName = Environment.GetEnvironmentVariable("StorageAccountName", EnvironmentVariableTarget.Process);
            var accountKey = Environment.GetEnvironmentVariable("StorageAccountKey", EnvironmentVariableTarget.Process);
            try
            {
                StorageCredentials creds = new StorageCredentials(accountName, accountKey);
                CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);

                CloudTableClient client = account.CreateCloudTableClient();

                var table = client.GetTableReference("Tasks");

                return table;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<List<OutlookTask>> GetAllUserTasks(string userName)
        {
            TableQuery<OutlookTask> query = new TableQuery<OutlookTask>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userName));
            try
            {
                var queryResult = await GetTasksTable().ExecuteQuerySegmentedAsync(query, null);
                return queryResult.Results;
            }
            catch (Exception)
            {
                return new List<OutlookTask>();
            }
        }

        public static async Task<List<OutlookTask>> GetUserCurrentTasks(string userName)
        {
            TableQuery<OutlookTask> query = new TableQuery<OutlookTask>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userName),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForBool("IsCurrentTask", QueryComparisons.Equal, true)));
            try
            {
                var queryResult = await GetTasksTable().ExecuteQuerySegmentedAsync(query, null);
                if (queryResult.Results == null || queryResult.Results.Count <= 0)
                {
                    return new List<OutlookTask>();
                }
                return queryResult.Results;
            }
            catch (Exception)
            {
                return new List<OutlookTask>();
            }
        }

        public static async Task<bool> InsertTask(OutlookTask task)
        {
            TableOperation insert = TableOperation.InsertOrReplace(task);
            try
            {
                await GetTasksTable().ExecuteAsync(insert);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<OutlookTask> UpdateUserCurrentTask(string userName, string code)
        {
            var table = GetTasksTable();
            var currentUserTasks = await GetUserCurrentTasks(userName);

            try
            {
                // 一意性を保持するため、ユーザーの現在タスクをリセットする
                // ループで回しているが、本来1タスクに対してしか行われない処理
                foreach (var t in currentUserTasks)
                {
                    t.IsCurrentTask = false;
                    var operation = TableOperation.Replace(t);
                    await table.ExecuteAsync(operation);
                }

                var retriveOperation = TableOperation.Retrieve<OutlookTask>(userName, code);
                var retrieveResult = await table.ExecuteAsync(retriveOperation);
                var newCurrentTask = retrieveResult.Result as OutlookTask;

                newCurrentTask.IsCurrentTask = true;
                var replaceOperation = TableOperation.Replace(newCurrentTask);
                var replaceResult = await table.ExecuteAsync(replaceOperation);

                // 見つからなければnullが割り当てられる
                return replaceResult.Result as OutlookTask;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}