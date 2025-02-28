using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;

namespace WebApi1Telemetry.Extensions;

public static class DistributedCacheExtensions
{
    private static readonly SemaphoreSlim CacheLock = new SemaphoreSlim(1, 1);

    public static async Task<TItem?> GetOrCreateAsync<TItem>(
        this IDistributedCache cache,
        string key,
        Func<Task<TItem>> factory,
        DistributedCacheEntryOptions options)
    {
        // Пытаемся получить данные из кэша
        var cachedData = await cache.GetAsync(key);
        if (cachedData != null)
        {
            return Deserialize<TItem>(cachedData);
        }

        await CacheLock.WaitAsync();
        try
        {
            // Повторно проверяем кэш после блокировки
            cachedData = await cache.GetAsync(key);
            if (cachedData != null)
            {
                return Deserialize<TItem>(cachedData);
            }

            // Получаем данные через фабрику
            var item = await factory();

            // Сериализуем данные
            var serializedData = Serialize(item);

            // Сохраняем данные в кэш
            await cache.SetAsync(key, serializedData, options);

            return item;
        }
        finally
        {
            CacheLock.Release();
        }
    }

    private static byte[] Serialize<T>(T item)
    {
        // Используем JSON для сериализации
        var json = JsonSerializer.Serialize(item,
            new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve });
        return Encoding.UTF8.GetBytes(json);
    }

    private static T? Deserialize<T>(byte[] bytes)
    {
        // Десериализуем из JSON
        var json = Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<T>(json,
            new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve });
    }
}