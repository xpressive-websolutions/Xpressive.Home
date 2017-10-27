using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MongoDB.Bson;
using MongoDB.Driver;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.CloudTwin
{
    internal sealed class CloudTwinGateway : GatewayBase, IMessageQueueListener<UpdateVariableMessage>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CloudTwinGateway));
        private readonly IMongoCollection<BsonDocument> _mongoDatabaseCollection;
        private readonly IMessageQueue _messageQueue;

        public CloudTwinGateway(IMessageQueue messageQueue) : base("CloudTwin")
        {
            _messageQueue = messageQueue;
            _canCreateDevices = true;

            var connectionString = ConfigurationManager.AppSettings["cloudtwin.connectionstring"];
            var databaseName = ConfigurationManager.AppSettings["cloudtwin.database"];
            var collectionName = ConfigurationManager.AppSettings["cloudtwin.collection"];

            if (!string.IsNullOrEmpty(connectionString) &&
                !string.IsNullOrEmpty(databaseName) &&
                !string.IsNullOrEmpty(collectionName))
            {
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databaseName);
                _mongoDatabaseCollection = database.GetCollection<BsonDocument>(collectionName);
            }
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            yield break;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { }).ConfigureAwait(false);

                if (_mongoDatabaseCollection == null)
                {
                    _messageQueue.Publish(new NotifyUserMessage("Add CloudTwin configuration to config file."));
                    return;
                }

                var cursor = await _mongoDatabaseCollection
                    .FindAsync(new BsonDocument(), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var documents = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);

                foreach (var document in documents)
                {
                    var twin = new CloudTwin();
                    var dict = document.ToDictionary();

                    foreach (var pair in dict)
                    {
                        if (pair.Key.Equals("_id", StringComparison.Ordinal))
                        {
                            twin.Id = pair.Value as string;
                        }
                        else if (pair.Key.Equals("name", StringComparison.OrdinalIgnoreCase))
                        {
                            twin.Name = pair.Value as string;
                        }
                        else
                        {
                            twin.Properties.Add(pair);
                        }
                    }

                    if (!string.IsNullOrEmpty(twin.Id))
                    {
                        _devices.Add(twin);
                    }
                }

                var lastUpdate = DateTime.MinValue;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var devices = _devices.OfType<CloudTwin>().Where(t => t.LastUpdate > lastUpdate).ToList();
                        var newLastUpdate = lastUpdate;

                        foreach (var twin in devices)
                        {
                            if (lastUpdate < twin.LastUpdate)
                            {
                                newLastUpdate = twin.LastUpdate;
                            }

                            var document = GetDocument(twin);
                            await _mongoDatabaseCollection
                                .ReplaceOneAsync(new BsonDocument("_id", BsonValue.Create(twin.Id)), document, cancellationToken: cancellationToken)
                                .ConfigureAwait(false);
                        }

                        lastUpdate = newLastUpdate;
                    }
                    catch (Exception e)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        _log.Error(e.Message, e);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ContinueWith(_ => { }).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _log.Error(e.Message, e);
                }
            }
        }

        public override IDevice CreateEmptyDevice()
        {
            return new CloudTwin();
        }

        protected override bool AddDeviceInternal(DeviceBase device)
        {
            var success = base.AddDeviceInternal(device);
            var twin = device as CloudTwin;

            if (success && twin != null && _mongoDatabaseCollection != null)
            {
                var document = GetDocument(twin);
                _mongoDatabaseCollection.InsertOne(document);
                return true;
            }

            return false;
        }

        protected override Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotSupportedException();
        }

        public void Notify(UpdateVariableMessage message)
        {
            if (string.IsNullOrEmpty(message.Name) || !message.Name.StartsWith("CloudTwin.", StringComparison.Ordinal))
            {
                return;
            }

            var parts = message.Name.Split(new [] {'.'}, 3);
            var device = _devices.OfType<CloudTwin>().SingleOrDefault(d => d.Id.Equals(parts[1], StringComparison.Ordinal));

            if (device != null)
            {
                device.Properties[parts[2]] = message.Value;
                device.LastUpdate = DateTime.UtcNow;
            }
        }

        private BsonDocument GetDocument(CloudTwin twin)
        {
            var properties = twin.Properties.ToDictionary(p => p.Key, p => p.Value);
            properties["_id"] = twin.Id;
            properties["name"] = twin.Name;
            return new BsonDocument(properties);
        }
    }
}
