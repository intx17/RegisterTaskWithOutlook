using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RegisterTaskWithOutlook.Entities;
using RegisterTaskWithOutlook.Helpers;
using RegisterTaskWithOutlook.Utilities;

namespace RegisterTaskWithOutlook.Function
{
    public static class EntryPoint
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string incomingWebhookUrl = Environment.GetEnvironmentVariable("IncomingWebhookUrl", EnvironmentVariableTarget.Process);

        [FunctionName("EntryPoint")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "entrypoint")]
    HttpRequest req, ILogger log)
        {
            var parsedReq = HttpUtility.ParseQueryString(HttpUtility.HtmlDecode(await req.ReadAsStringAsync()));
            var rawText = parsedReq["text"];
            var userName = parsedReq["user_name"];

            SlashCommandText text;
            try
            {
                text = SlashCommandWebhookParser.ParseText(rawText);
            }
            catch (ArgumentException e)
            {
                return new BadRequestObjectResult(e.Message);
            }

            switch (text.Action)
            {
                case "add":
                    _ = AddTask(name: text.Arg1, code: text.Arg2, userName: userName, log: log);
                    return new OkObjectResult("挿入処理を実行しました");
                case "list":
                    _ = ListTasks(userName: userName, log: log);
                    return new OkObjectResult("一覧処理を実行しました");
                case "confirm":
                    _ = ConfirmTask(userName: userName, log: log);
                    return new OkObjectResult("現在のタスク取得処理を実行しました");
                case "set":
                    _ = SetTask(userName: userName, code: text.Arg1, log: log);
                    return new OkObjectResult("現在のタスクセット処理を実行しました");
                case "help":
                    var help = "";
                    help += "/rwto add <タスク名> <タスクのTimeTrackerコード>  タスクの追加\n";
                    help += "/rwto list  タスクの一覧取得\n";
                    help += "/rwto confirm  現在のタスク取得\n";
                    help += "/rwto set <タスクのTimeTrackerコード>  現在のタスクを設定";

                    return new OkObjectResult(help);
                default:
                    return new BadRequestObjectResult("invalid action");
            }
        }

        private static async Task SendIncomingWebhook(IncomingWebhookPayload payload)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, incomingWebhookUrl);
            req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            await httpClient.SendAsync(req);
        }

        public static async Task AddTask(string name, string code, string userName, ILogger log)
        {
            log.LogInformation($"start adding task process");

            var payload = new IncomingWebhookPayload { };

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(userName))
            {
                payload.Text = $"タスク挿入に失敗しました. name={name}, code={code}, userName={userName}";
                await SendIncomingWebhook(payload);
            }

            var task = new OutlookTask
            {
                PartitionKey = userName,
                RowKey = code,
                Name = name,
                IsCurrentTask = false
            };

            var result = await TableHelper.InsertTask(task);

            payload.Text = result
            ? $"タスク挿入に成功しました. task={JsonConvert.SerializeObject(task)}"
            : $"タスク挿入に失敗しました. task={JsonConvert.SerializeObject(task)}";

            await SendIncomingWebhook(payload);
        }

        public static async Task ListTasks(string userName, ILogger log)
        {
            log.LogInformation($"start listing task process");

            var payload = new IncomingWebhookPayload { };

            if (string.IsNullOrEmpty(userName))
            {
                payload.Text = $"一覧取得に失敗しました。 userName={userName}";
                await SendIncomingWebhook(payload);
            }

            var result = await TableHelper.GetAllUserTasks(userName);

            if (result == null || result.Count <= 0)
            {
                payload.Text = "一覧するタスクが存在しません.";
            }
            else
            {
                var tasksStr = "";

                foreach (var t in result)
                {
                    tasksStr += $"Name={t.Name} Code={t.RowKey} \n";
                }

                payload.Text = "Tasks: \n " + tasksStr;
            }
            await SendIncomingWebhook(payload);
        }

        public static async Task ConfirmTask(string userName, ILogger log)
        {
            log.LogInformation($"start confirming task process");

            var payload = new IncomingWebhookPayload { };

            if (string.IsNullOrEmpty(userName))
            {
                payload.Text = $"現在のタスク取得に失敗しました。 userName={userName}";
                await SendIncomingWebhook(payload);
            }

            var result = await TableHelper.GetUserCurrentTasks(userName);

            if (result == null || result.Count <= 0)
            {
                payload.Text = "現在のタスクが存在しません.";
            }
            else
            {
                var firstTaskJson = JsonConvert.SerializeObject(result[0]);
                payload.Text = $"現在のタスク: {firstTaskJson} ";
            }

            await SendIncomingWebhook(payload);
        }

        public static async Task SetTask(string userName, string code, ILogger log)
        {
            log.LogInformation($"start setting task process");

            var payload = new IncomingWebhookPayload { };

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(code))
            {
                payload.Text = $"現在のタスク設定に失敗しました。 userName={userName} code={code}";
                await SendIncomingWebhook(payload);
            }

            var result = await TableHelper.UpdateUserCurrentTask(userName, code);

            payload.Text = result == null
            ? $"現在のタスク設定に成功しました. task={JsonConvert.SerializeObject(result)}"
            : $"現在のタスク設定に失敗しました.";
            await SendIncomingWebhook(payload);
        }
    }
}