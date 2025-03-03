using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;

namespace WebApi1Telemetry.Extensions;

public static class DistributedCacheExtensions
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> CacheLocks = new();

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

        // Получаем или создаем семафор для данного ключа
        var cacheLock = CacheLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await cacheLock.WaitAsync();
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
            cacheLock.Release();

            // Удаляем семафор из словаря, если он больше не нужен
            if (cacheLock.CurrentCount == 1)
            {
                CacheLocks.TryRemove(key, out _);
            }
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