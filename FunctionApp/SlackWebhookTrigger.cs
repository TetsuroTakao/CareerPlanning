using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SlackIntegration
{
    public class SlackWebhookTrigger
    {
        private readonly ILogger<SlackWebhookTrigger> _logger;

        public SlackWebhookTrigger(ILogger<SlackWebhookTrigger> logger)
        {
            _logger = logger;
        }

        [Function("SlackWebhook")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("Slackからイベントを受信しました。");

            // リクエストボディの読み込み
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            using (JsonDocument doc = JsonDocument.Parse(requestBody))
            {
                JsonElement root = doc.RootElement;

                // 1. URL検証（challenge）への対応
                if (root.TryGetProperty("type", out JsonElement typeProp) && 
                    typeProp.GetString() == "url_verification")
                {
                    string challenge = root.GetProperty("challenge").GetString()!;
                    _logger.LogInformation($"URL検証を実行中。Challenge: {challenge}");

                    // challenge文字列をプレーンテキスト(text/plain)でオウム返し
                    return new ContentResult
                    {
                        Content = challenge,
                        ContentType = "text/plain; charset=utf-8",
                        StatusCode = 200
                    };
                }

                // 2. 実際のメンバー増減イベント（team_join 等）の処理
                if (root.TryGetProperty("event", out JsonElement eventProp))
                {
                    string eventType = eventProp.GetProperty("type").GetString()!;
                    _logger.LogInformation($"イベントを受信: {eventType}");

                    // TODO: ここに台帳（スプレッドシート）への書き込みロジックを追記する
                }
            }

            return new OkResult();
        }
    }
}