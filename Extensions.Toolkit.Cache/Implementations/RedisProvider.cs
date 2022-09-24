using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;
using Extensions.Toolkit.Cache.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;

namespace Extensions.Toolkit.Cache.Implementations;

internal sealed class RedisProvider : IRedisProvider
    {
        private readonly ILogger<RedisProvider> _logger;
        private readonly Lazy<ConnectionMultiplexer> _connection;

        public event Action<ConnectionFailedEventArgs> ConnectionRestored;

        private IDatabase _db => _connection.Value.GetDatabase();
        private ISubscriber _subscriber => _connection.Value.GetSubscriber();

        public RedisProvider(
            ILoggerFactory loggerFactory,
            string connectionString, 
            Action<ConfigurationOptions>? configureConnection = null)
        {
            var connection = ConfigurationOptions.Parse(connectionString, true);
            configureConnection?.Invoke(connection);
            
            _logger = loggerFactory.CreateLogger<RedisProvider>();
            _connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(connection));

            _db.Multiplexer.ConnectionRestored += (_, ev) => ConnectionRestored?.Invoke(ev);
        }

        public Task ListLeftPush<T>(string key, T item) 
            => _db.ListLeftPushAsync(key, JsonConvert.SerializeObject(item));
        
        public Task ListLeftPush<T>(string key, T item, IContractResolver contractResolver)
        {
            return _db.ListLeftPushAsync(key, JsonConvert.SerializeObject(item, new JsonSerializerSettings()
            {
                ContractResolver = contractResolver
            }));
        }
        
        public async Task<T[]> ListRange<T>(string key, long start = 0, long stop = -1)
        {
            try
            {
                var items = await _db.ListRangeAsync(key, start, stop);

                return items
                    .Select(s => JsonConvert.DeserializeObject<T>(s))
                    .ToArray();
            }
            catch (RedisTimeoutException)
            {
                var resultData = new List<T>();
                var length = await _db.ListLengthAsync(key);
                var stopIndex = stop == -1 ? length : Math.Min(length, stop);
                for (long i = start; i < stopIndex; i++)
                {
                    var redisVal = await _db.ListGetByIndexAsync(key, i);
                    resultData.Add(JsonConvert.DeserializeObject<T>(redisVal));
                }

                return resultData.ToArray();
            }
        }

        public Task ListTrim(string key, long start, long stop) => _db.ListTrimAsync(key, start, stop);

        public async Task PubAsync<T>(string channel, T value)
        {
            var json = JsonConvert.SerializeObject(value);
            await _subscriber.PublishAsync(channel, json, CommandFlags.FireAndForget);
        }

        public async Task PubManyAsync<T>(string channel, IReadOnlyDictionary<string, T> values)
        {
            var tasks = values.Select(s =>
            {
                var json = JsonConvert.SerializeObject(s.Value);
                var entityChannelName = s.Key;
                return _subscriber.PublishAsync(entityChannelName, json, CommandFlags.FireAndForget);
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        public async Task<T?> WaitOne<T>(string channel)
        {
            var tcs = new TaskCompletionSource<T?>();
            
            void Handler(RedisChannel redisChannel, RedisValue value)
            {
                try
                {
                    var msg = JsonConvert.DeserializeObject<T>(value);
                    tcs.SetResult(msg);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }

            await _subscriber.SubscribeAsync(channel, Handler, CommandFlags.FireAndForget).ConfigureAwait(false);

            try
            {
                return await tcs.Task;
            }
            finally
            {
                await _subscriber.UnsubscribeAsync(channel).ConfigureAwait(false);
            }
        }

        public async Task<IAsyncDisposable> SubAsync<T>(string channel, ActionBlock<T> action)
        {
            void Handler(RedisChannel redisChannel, RedisValue value)
            {
                var msg = JsonConvert.DeserializeObject<T>(value);
                action.Post(msg);
            }

            await _subscriber.SubscribeAsync(channel, Handler, CommandFlags.FireAndForget).ConfigureAwait(false);

            return new AsyncDisposeProxy(() => _subscriber.UnsubscribeAsync(channel));
        }

        public async Task UnSubAsync(string channel)
        {
            await _subscriber.UnsubscribeAsync(
                channel,
                (_, _) => { }, CommandFlags.FireAndForget).ConfigureAwait(false);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl)
            => SetAsync(key, value, ttl, null);

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl, IContractResolver contractResolver)
        {
            var json = JsonConvert.SerializeObject(value, new JsonSerializerSettings()
            {
                ContractResolver = contractResolver
            });
            await _db.StringSetAsync(key, json, ttl);
        }

        public Task SetManyAsync<T>(IReadOnlyDictionary<string, T> values, TimeSpan ttl)
        {
            // temporary with batches
            var batch = _db.CreateBatch();
            
            var tasks = values.Select(s =>
            {
                var json = JsonConvert.SerializeObject(s.Value);
                return batch.StringSetAsync(s.Key, json, ttl);
            }).ToArray();
            
            batch.Execute();

            // batches it is good, but performance is worse than pipeline
            // var tasks = values.Select(s =>
            // {
            //     var json = JsonConvert.SerializeObject(s.Value);
            //     return _db.StringSetAsync(s.Key, json, ttl, When.Always, CommandFlags.FireAndForget);
            // }).ToArray();
            
            return Task.WhenAll(tasks);
        }
        
        public Task DeleteManyAsync(IEnumerable<string> keys)
        {
            return _db.KeyDeleteAsync(keys.Select(x => (RedisKey)x).ToArray(), CommandFlags.FireAndForget);
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(key);

            return value.IsNullOrEmpty ? default : JsonConvert.DeserializeObject<T>(value);
        }

        public async Task DeleteAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }

        public bool IsConnected()
            => _db.Multiplexer.IsConnected;

        public IAsyncEnumerable<string> GetKeys(Func<string, bool> keyFilter)
        {
            IServer server = _db.Multiplexer.GetServer(_db.IdentifyEndpoint());
            return server.KeysAsync(_db.Database)
                         .Where(x => keyFilter(x.ToString()))
                         .Select(x => x.ToString());
        }

        public IAsyncEnumerable<string> GetKeys(string pattern)
        {
            IServer server = _db.Multiplexer.GetServer(_db.IdentifyEndpoint());

            return server
                .KeysAsync(pattern: pattern)
                .Select(x => x.ToString());
        }

        public bool TryConsumeStream<TEntity>(
            string streamName,
            CacheConsumeSettings<string> consumeSettings,
            CancellationToken token,
            out IObservable<TEntity> stream)
        {
            if (IsConnected())
            {
                if (_db.KeyExists(streamName))
                {
                    stream = ConsumeStream<TEntity>(streamName, consumeSettings, token);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Unable to start reading from Redis stream with restrictions. " +
                                     "Unable to find stream '{Stream}'", streamName);
                }
            }
            else
            {
                _logger.LogWarning("Unable to start reading from Redis stream with restrictions. " +
                                 "Unable to connect to the Redis server with restrictions stream");
            }

            stream = null;
            return false;
        }

        public IObservable<TEntity> ConsumeStream<TEntity>(
            string streamName, 
            CacheConsumeSettings<string> consumeSettings, 
            CancellationToken token)
            => Observable.Create(async (IObserver<TEntity> x) =>
            {
                var currentPosition = consumeSettings.Position;

                while (IsConnected() && !token.IsCancellationRequested)
                {
                    var (items, newPosition) = await SafeRead(streamName, currentPosition, consumeSettings.PrefetchCount);
                    foreach (var item in items)
                    {
                        try
                        {
                            var value = JsonConvert.DeserializeObject<TEntity>(item);
                            x.OnNext(value);
                        }
                        catch (JsonException exception)
                        {
                            _logger.LogError(exception, "Error occured while trying to deserialize object of type {Type}" +
                                            "while reading from Redis {Stream} stream", typeof(TEntity).Name, streamName);
                        }

                    }

                    currentPosition = newPosition;
                }

                x.OnCompleted();

            }).Synchronize(new object());

        private async Task<(IEnumerable<string>, string lastPos)> SafeRead(string streamName, string position, int prefetchCount)
        {
            try
            {
                var messages = await _db.StreamReadAsync(streamName, position, prefetchCount);
                var items = messages.SelectMany(s => s.Values)
                                    .Select(s => s.Value.ToString());

                var lastMessage = messages.LastOrDefault();

                var lastMessagePosition = lastMessage.IsNull || lastMessage.Id.IsNullOrEmpty
                    ? position
                    : lastMessage.Id.ToString();

                return (items, lastMessagePosition);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error occured while trying to read {Stream} stream at position {Position} " +
                    "from Redis", streamName, position);
            }

            return (Array.Empty<string>(), position);
        }
    }