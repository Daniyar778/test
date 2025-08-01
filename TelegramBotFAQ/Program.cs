// Program.cs
// dotnet add package Telegram.Bot --version 19.*  (или актуальную)
// Перед запуском установите переменную окружения TG_BOT_TOKEN
//   Linux/macOS: export TG_BOT_TOKEN=123456:ABC
//   PowerShell : $Env:TG_BOT_TOKEN = "123456:ABC"

using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var token = Environment.GetEnvironmentVariable("TG_BOT_TOKEN")
           ?? throw new InvalidOperationException("TG_BOT_TOKEN environment variable not set.");

ITelegramBotClient botClient = new TelegramBotClient(token);

// Не чувствителен к регистру благодаря StringComparer.OrdinalIgnoreCase
var faqs = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["что такое осаго"]    = "ОСАГО – это обязательное страхование автогражданской ответственности, покрывающее ущерб, причинённый другим участникам ДТП.",
    ["зачем нужно осаго"]  = "ОСАГО обязательно для всех владельцев авто в Кыргызстане, без него управлять автомобилем запрещено.",
    ["как оформить осаго"] = "Оформить ОСАГО можно онлайн за 5 минут через сервис Страхование.KG, без очередей.",
    ["сколько стоит осаго"] = "Базовая ставка полиса ОСАГО — 1680 сомов, цена зависит от типа авто, стажа водителя и других факторов.",
    ["что покрывает осаго"] = "ОСАГО покрывает ущерб чужому имуществу и лечение пострадавших, если вы виновник ДТП.",
    ["что не покрывает осаго"] = "ОСАГО не покрывает ремонт вашего авто, ущерб при опьянении и штрафы. Для этого нужен полис КАСКО.",
    ["штраф за отсутствие осаго"] = "Штраф за отсутствие ОСАГО в Кыргызстане — 3000 сомов с 1 июля 2025 года.",
    ["как проверить осаго"] = "Проверить полис можно онлайн на сайте Страхование.KG или в личном кабинете страховой компании.",
    ["документы для осаго"] = "Нужно указать ПИН владельца и госномер авто. Иностранцы указывают паспортные данные."
};

using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = new[] { UpdateType.Message } // получаем только сообщения
};

// запускаем асинхронный приём
var receivingTask = botClient.ReceiveAsync(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cts.Token);

var me = await botClient.GetMeAsync(cts.Token);
Console.WriteLine($"🤖 FAQ‑бот по ОСАГО запущен: @{me.Username}");
Console.WriteLine("Нажмите <Enter>, чтобы остановить.");
Console.ReadLine();

cts.Cancel();              // просим отмену
await receivingTask;       // дожидаемся корректного завершения
Console.WriteLine("Бот остановлен.");

// ---------- handlers ----------

async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken ct)
{
    if (update.Type != UpdateType.Message ||
        update.Message is not { Text.Length: > 0 } msg)
        return;

    var text = msg.Text.Trim();

    // обработка команд
    if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
    {
        await client.SendTextMessageAsync(
            chatId: msg.Chat.Id,
            text: "Привет! Я отвечаю на популярные вопросы об ОСАГО. Просто задайте вопрос.",
            cancellationToken: ct);
        return;
    }
    if (text.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
    {
        await client.SendTextMessageAsync(
            chatId: msg.Chat.Id,
            text: "Задайте вопрос в свободной форме, например: «Сколько стоит ОСАГО?»",
            cancellationToken: ct);
        return;
    }

    // 1) точное совпадение
    if (!faqs.TryGetValue(text, out var answer))
    {
        // 2) поиск по подстроке (инвариантно)
        var lower = text.ToLowerInvariant();
        answer = faqs.FirstOrDefault(p => lower.Contains(p.Key.ToLowerInvariant(), StringComparison.Ordinal)).Value;
    }

    answer ??= "🤔 Я не понял вопрос. Попробуйте переформулировать или загляните на сайт Страхование.KG.";

    await client.SendTextMessageAsync(
        chatId: msg.Chat.Id,
        text: answer,
        parseMode: ParseMode.Html, // если позже понадобится форматирование
        cancellationToken: ct);
}

Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken _)
{
    var error = exception switch
    {
        ApiRequestException api => $"Telegram API Error {api.ErrorCode}: {api.Message}",
        _                     => exception.ToString()
    };

    Console.WriteLine($"⚠️ {error}");
    return Task.CompletedTask;
}
