using Newtonsoft.Json;
using Quotes286Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Quotes286Bot
{
    public class QuotesBot
    {
        TelegramBotClient botClient = new TelegramBotClient("6009442655:AAF3TgE0OJHqMahakoU3263wpbK-3R8bYDA");
        //TelegramBotClient botClient = new TelegramBotClient("5849544258:AAH-9Y3Dvq6ZBK5xFDck_3wwHkpXIfo0g-U");
        CancellationToken cancellationToken = new CancellationToken();
        ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = { } };
        private Dictionary<long, bool> isFindingQuotes = new Dictionary<long, bool>();
        private Dictionary<long, List<string>> isFavoriteQuotes = new Dictionary<long, List<string>>();

        private Dictionary<long, List<QuoteFavqs>> quotesByAuthor = new Dictionary<long, List<QuoteFavqs>>();
        private Dictionary<long, int> currentPage = new Dictionary<long, int>();

        public async Task Start()
        {
            botClient.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var botMe = await botClient.GetMeAsync();
            Console.WriteLine($"Bot {botMe.Username} has started working");
            Console.ReadKey();
        }

        private Task HandlerError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram bot API error:\n {apiRequestException.ErrorCode}" +
                $"\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandlerMessageAsync(botClient, update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery && update?.CallbackQuery?.Data == "add_to_favorites")
            {
                var chatId = update.CallbackQuery.Message.Chat.Id;
                var messageId = update.CallbackQuery.Message.MessageId;
                var quoteText = update.CallbackQuery.Message.Text;

                if (!isFavoriteQuotes.ContainsKey(chatId))
                {
                    isFavoriteQuotes[chatId] = new List<string>();
                }

                if (!isFavoriteQuotes[chatId].Contains(quoteText))
                {
                    isFavoriteQuotes[chatId].Add(quoteText);
                    //await botClient.SendTextMessageAsync(chatId, "Quote added to favorites.");

                    // Add the "Remove from Favorites" button
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("❌ Remove", "remove_from_favorites")
                        }
                    });

                    await botClient.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: keyboard);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "The quote is already in the list of favorites.");
                }
            }
            else if (update.Type == UpdateType.CallbackQuery && update?.CallbackQuery?.Data == "remove_from_favorites")
            {
                var chatId = update.CallbackQuery.Message.Chat.Id;
                var messageId = update.CallbackQuery.Message.MessageId;
                var quoteText = update.CallbackQuery.Message.Text;

                if (isFavoriteQuotes.ContainsKey(chatId) && isFavoriteQuotes[chatId].Contains(quoteText))
                {
                    isFavoriteQuotes[chatId].Remove(quoteText);
                    //await botClient.SendTextMessageAsync(chatId, "Quote removed from favorites.");

                    // Add the "Add to Favorites" button
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("♥ Add to Favorites", "add_to_favorites")
                        }
                    });

                    await botClient.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: keyboard);
                }
            }

            else if (update.Type == UpdateType.CallbackQuery && (update?.CallbackQuery?.Data == "next_page_1" || update?.CallbackQuery?.Data == "previous_page_1"))
            {
                var chatId = update.CallbackQuery.Message.Chat.Id;

                int pageChange = update.CallbackQuery.Data == "next_page_1" ? 1 : -1;

                if (currentPage.ContainsKey(chatId))
                {
                    currentPage[chatId] += pageChange;
                }
                else
                {
                    currentPage[chatId] = 1;
                }

                await SendQuotesByAuthor(chatId, currentPage[chatId]);
            }
            else if (update.Type == UpdateType.CallbackQuery && (update?.CallbackQuery?.Data == "next_page_2" || update?.CallbackQuery?.Data == "previous_page_2"))
            {
                var chatId = update.CallbackQuery.Message.Chat.Id;

                int pageChange = update.CallbackQuery.Data == "next_page_2" ? 1 : -1;

                if (currentPage.ContainsKey(chatId))
                {
                    currentPage[chatId] += pageChange;
                }
                else
                {
                    currentPage[chatId] = 1;
                }

                await SendFavoriteQuotes(chatId, currentPage[chatId]);
            }
        }

        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    "✨ Welcome to the quote bot ✨\n" +
                    "Try to choose the following commands:\n\n" +

                    "/random - get a random quote\n" +
                    "/randomlist - get a list of random quotes\n" +
                    "/randomlist (number) - get desired number of random quotes (limit 25)\n" +
                    "/findquotes - find quotes by author\n" +
                    "/favorite - open list of favorite quotes");
                return;
            }

            else if (message.Text == "/random")
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://quotesapi20230603234422.azurewebsites.net/QuoteControllerQuotable/random")
                };
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<QuoteQuotable>(content);

                    string quoteText = $"{result.Content}. \n© {result.Author}";

                    // Add the "Add to Favorites" button
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("♥ Add to Favorites", "add_to_favorites")
                        }
                    });

                    await botClient.SendTextMessageAsync(message.Chat.Id, quoteText, replyMarkup: keyboard);
                }
                return;
            }

            else if (message.Text.StartsWith("/randomlist"))
            {
                string[] input = message.Text.Split(' ');
                int count = 5; // Значення за замовчуванням

                if (input.Length > 1 && int.TryParse(input[1], out int inputCount))
                {
                    count = inputCount;
                }

                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://quotesapi20230603234422.azurewebsites.net/QuoteControllerFavqs/quotes")
                };
                using (var response = await client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<List<QuoteFavqs>>(jsonResponse);
                        var quotes = result.Take(count).ToList();

                        foreach (var quote in quotes)
                        {
                            string quoteText = $"{quote.Body}. \n© {quote.Author}";

                            // Create inline keyboard markup with the "Add to favorite" button
                            var inlineKeyboard = new InlineKeyboardMarkup(new[]
                            {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("♥ Add to favorite", $"add_to_favorites")
                    }
                });

                            await botClient.SendTextMessageAsync(message.Chat.Id, quoteText, replyMarkup: inlineKeyboard);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Помилка запиту до API: {response.StatusCode}");
                    }
                }
                return;
            }


            else if (message.Text == "/findquotes")
            {
                isFindingQuotes[message.Chat.Id] = true;
                await botClient.SendTextMessageAsync(message.Chat.Id, "Enter the FULL name of the author:");
                return;
            }
            else if (isFindingQuotes.ContainsKey(message.Chat.Id) && isFindingQuotes[message.Chat.Id])
            {
                isFindingQuotes[message.Chat.Id] = false;
                var client = new HttpClient();
                string authorName = message.Text; // Отримуємо authorName з повідомлення
                authorName = Uri.EscapeDataString(authorName);
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://quotesapi20230603234422.azurewebsites.net/QuoteControllerFavqs/quotes/{authorName}")
                };
                using (var response = await client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<List<QuoteFavqs>>(jsonResponse);
                        List<QuoteFavqs> quotes = result.ToList();

                        if (quotes.Count > 0)
                        {
                            quotesByAuthor[message.Chat.Id] = quotes;
                            currentPage[message.Chat.Id] = 1;
                            await SendQuotesByAuthor(message.Chat.Id, currentPage[message.Chat.Id]);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "No quotes found for the given author.");
                        }
                    }
                    else
                    {
                        //Console.WriteLine($"Помилка запиту до API: {response.StatusCode}");
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Error occurred while requesting the API.");
                    }
                }

                return;
            }


            else if (message.Text == "/favorite")
            {
                if (isFavoriteQuotes.ContainsKey(message.Chat.Id))
                {
                    var favoriteQuotes = isFavoriteQuotes[message.Chat.Id];
                    if (favoriteQuotes.Count > 0)
                    {
                        currentPage[message.Chat.Id] = 1;
                        await SendFavoriteQuotes(message.Chat.Id, currentPage[message.Chat.Id]);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "No favorite quotes found.");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "No favorite quotes found.");
                }
            }

            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    "Try to choose the following commands:\n\n" +

                    "/random - get a random quote\n" +
                    "/randomlist - get a list of random quotes\n" +
                    "/randomlist (number) - get desired number of random quotes (limit 25)\n" +
                    "/findquotes - find quotes by author\n" +
                    "/favorite - open list of favorite quotes");
                return;
            }
        }

        private async Task SendQuotesByAuthor(long chatId, int page)
        {
            if (quotesByAuthor.ContainsKey(chatId))
            {
                var quotes = quotesByAuthor[chatId];
                int pageSize = 1;
                int totalPages = (int)Math.Ceiling((double)quotes.Count / pageSize);

                if (page < 1)
                {
                    page = 1;
                }
                else if (page > totalPages)
                {
                    page = totalPages;
                }

                int startIndex = (page - 1) * pageSize;
                int endIndex = Math.Min(startIndex + pageSize, quotes.Count);

                for (int i = startIndex; i < endIndex; i++)
                {
                    var quote = quotes[i];
                    string quoteText = $"{quote.Body}. \n© {quote.Author}";

                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("♥ Add to Favorites", "add_to_favorites")
                        }
                    });

                    await botClient.SendTextMessageAsync(chatId, quoteText, replyMarkup: keyboard);
                }

                var paginationButtons = new List<InlineKeyboardButton>();
                if (page > 1)
                {
                    paginationButtons.Add(InlineKeyboardButton.WithCallbackData("◀", "previous_page_1"));
                }

                if (page < totalPages)
                {
                    paginationButtons.Add(InlineKeyboardButton.WithCallbackData("▶", "next_page_1"));
                }

                if (paginationButtons.Count > 0)
                {
                    var paginationKeyboard = new InlineKeyboardMarkup(new[] { paginationButtons.ToArray() });
                    await botClient.SendTextMessageAsync(chatId, $"✨ Page {page}/{totalPages}", replyMarkup: paginationKeyboard);
                }
            }
        }

        private async Task SendFavoriteQuotes(long chatId, int page)
        {
            if (isFavoriteQuotes.ContainsKey(chatId))
            {
                var favoriteQuotes = isFavoriteQuotes[chatId];
                int pageSize = 1;
                int totalPages = (int)Math.Ceiling((double)favoriteQuotes.Count / pageSize);

                if (page < 1)
                {
                    page = 1;
                }
                else if (page > totalPages)
                {
                    page = totalPages;
                }

                int startIndex = (page - 1) * pageSize;
                int endIndex = Math.Min(startIndex + pageSize, favoriteQuotes.Count);

                for (int i = startIndex; i < endIndex; i++)
                {
                    string quoteText = favoriteQuotes[i];

                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("❌ Remove", "remove_from_favorites")
                        }
                    });

                    await botClient.SendTextMessageAsync(chatId, quoteText, replyMarkup: keyboard);
                }

                var paginationButtons = new List<InlineKeyboardButton>();
                if (page > 1)
                {
                    paginationButtons.Add(InlineKeyboardButton.WithCallbackData("◀", "previous_page_2"));
                }
                if (page < totalPages)
                {
                    paginationButtons.Add(InlineKeyboardButton.WithCallbackData("▶", "next_page_2"));
                }

                if (paginationButtons.Count > 0)
                {
                    var paginationKeyboard = new InlineKeyboardMarkup(new[] { paginationButtons.ToArray() });
                    await botClient.SendTextMessageAsync(chatId, $"✨ Page {page}/{totalPages}", replyMarkup: paginationKeyboard);
                }
            }
        }
    }
}
