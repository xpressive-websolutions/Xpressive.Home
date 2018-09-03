using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Gardena
{
    internal class GardenaGateway : GatewayBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(GardenaGateway));
        private readonly IMessageQueue _messageQueue;
        private readonly string _username;
        private readonly string _password;
        private readonly RestClient _client;
        private readonly RestClient _authClient;
        private readonly Dictionary<string, string> _substitutions;
        private Token _token;

        public GardenaGateway(IMessageQueue messageQueue) : base("Gardena")
        {
            _messageQueue = messageQueue;
            _username = ConfigurationManager.AppSettings["gardena.username"];
            _password = ConfigurationManager.AppSettings["gardena.password"];
            _canCreateDevices = false;

            _client = new RestClient("https://smart.gardena.com/");
            _authClient = new RestClient("https://iam-api.dss.husqvarnagroup.net/");

            _substitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"device_info_manufacturer", "manufacturer"},
                {"device_info_product", "product"},
                {"device_info_serial_number", "serial_number"},
                {"device_info_sgtin", "sgtin"},
                {"device_info_version", "version"},
                {"device_info_category", "category"},
                {"device_info_last_time_online", "last_time_online"},
                {"gateway_ip_address", "ip_address"},
                {"gateway_time_zone", "time_zone"},
                {"battery_level", "battery_level"},
                {"battery_disposable_battery_status", "battery_status"},
                {"ambient_temperature_temperature", "abient_temperature"},
                {"ambient_temperature_frost_warning", "frost_warning"},
                {"soil_temperature_temperature", "soil_temperature"},
                {"humidity_humidity", "humidity"},
                {"light_light", "light"},
                {"firmware_firmware_status", "firmware_status"},
                {"firmware_firmware_upload_progress", "firmware_upload_progress"},
                {"firmware_firmware_available_version", "firmware_available_version"},
                {"firmware_firmware_update_start", "firmware_update_start"},
                {"firmware_firmware_command", "firmware_command"},
                {"scheduling_scheduled_watering_next_start", "scheduled_watering_next_start"},
                {"scheduling_scheduled_watering_end", "scheduled_watering_end"},
                {"scheduling_adaptive_scheduling_last_decision", "adaptive_scheduling_last_decision"},
                {"mower_manual_operation", "manual_operation"},
                {"mower_status", "status"},
                {"mower_error", "error"},
                {"mower_last_error_code", "last_error_code"},
                {"mower_source_for_next_start", "source_for_next_start"},
                {"mower_timestamp_next_start", "timestamp_next_start"},
                {"mower_override_end_time", "override_end_time"},
                {"mower_timestamp_last_error_code", "timestamp_last_error_code"},
                {"mower_stats_cutting_time", "cutting_time"},
                {"mower_stats_charging_cycles", "charging_cycles"},
                {"mower_stats_collisions", "collisions"},
                {"mower_stats_running_time", "running_time"},
                {"mower_type_base_software_up_to_date", "base_software_up_to_date"},
                {"mower_type_mmi_version", "mmi_version"},
                {"mower_type_mainboard_version", "mainboard_version"},
                {"mower_type_comboard_version", "comboard_version"},
                {"mower_type_device_type", "device_type"},
                {"mower_type_device_variant", "device_variant"}
            };
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            var d = device as GardenaDevice;

            if (d == null)
            {
                yield break;
            }


        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add gardena configuration to config file."));
                return;
            }

            try
            {
                await LoginAsync();
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshTokenAsync();

                    var locations = await GetAsync<LocationsResponseDto>($"sg-1/locations?locationId=null&user_id={_token.UserId}");

                    foreach (var location in locations.Locations)
                    {
                        var devices = await GetAsync<DevicesResponseDto>($"sg-1/devices?locationId={location.Id}");

                        foreach (var device in devices.Devices)
                        {
                            var gardenaDevice = _devices.Values.SingleOrDefault(d => d.Id.Equals(device.Id, StringComparison.OrdinalIgnoreCase)) as GardenaDevice;

                            if (gardenaDevice == null)
                            {
                                gardenaDevice = new GardenaDevice
                                {
                                    Id = device.Id,
                                    Name = device.Name
                                };
                                _devices.TryAdd(device.Id, gardenaDevice);
                            }

                            foreach (var ability in device.Abilities)
                            {
                                var abilityName = ability.Name;

                                foreach (var property in ability.Properties)
                                {
                                    var key = abilityName + "_" + property.Name;
                                    var unit = property.Unit;
                                    object value;
                                    string substitution;

                                    if (_substitutions.TryGetValue(key, out substitution))
                                    {
                                        key = substitution;
                                    }

                                    if (property.Value == null)
                                    {
                                        continue;
                                    }

                                    if (property.Value is bool)
                                    {
                                        value = (bool) property.Value;
                                    }
                                    else if (property.Value is int)
                                    {
                                        value = (double) (int) property.Value;
                                    }
                                    else
                                    {
                                        value = property.Value.ToString();

                                        double doubleValue;
                                        if (double.TryParse(property.Value.ToString(), out doubleValue))
                                        {
                                            value = doubleValue;
                                        }
                                    }

                                    if (string.IsNullOrEmpty(unit))
                                    {
                                        unit = null;
                                    }

                                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, key, value, unit));

                                    if (key.Equals("category", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (property.Value.ToString().Equals("watering_computer", StringComparison.OrdinalIgnoreCase))
                                        {
                                            gardenaDevice.Type = GardenaDeviceType.WateringComputer;
                                            gardenaDevice.Icon = "gardena_watering_computer";
                                        }
                                        else if (property.Value.ToString().Equals("mower", StringComparison.OrdinalIgnoreCase))
                                        {
                                            gardenaDevice.Type = GardenaDeviceType.Mower;
                                            gardenaDevice.Icon = "gardena_mower";
                                        }
                                        else if (property.Value.ToString().Equals("sensor", StringComparison.OrdinalIgnoreCase))
                                        {
                                            gardenaDevice.Type = GardenaDeviceType.Sensor;
                                            gardenaDevice.Icon = "fa fa-thermometer-half";
                                        }
                                        else if (property.Value.ToString().Equals("gateway", StringComparison.OrdinalIgnoreCase))
                                        {
                                            gardenaDevice.Type = GardenaDeviceType.Gateway;
                                            gardenaDevice.Icon = "gardena_gateway";
                                        }
                                    }
                                }
                            }
                        }
                    }

                    await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken).ContinueWith(_ => { });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        protected override Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotImplementedException();
        }

        private async Task<T> GetAsync<T>(string url) where T : new()
        {
            var request = new RestRequest(url);
            request.AddHeader("authorization", "Bearer " + _token.AccessToken);
            request.AddHeader("referer", "https://smart.gardena.com/");
            request.AddHeader("authorization-provider", "husqvarna");
            var result = await _client.ExecuteGetTaskAsync<T>(request);

            return result.Data;
        }

        private async Task<T> GetEventStreamAsync<T>(string url) where T : new()
        {
            var request = new RestRequest(url);
            request.AddHeader("referer", "https://smart.gardena.com/");
            request.AddHeader("accept", "text/event-stream");
            var result = await _client.ExecuteGetTaskAsync<T>(request);

            return result.Data;
        }

        private async Task LoginAsync()
        {
            var request = new RestRequest("api/v3/token");
            request.AddHeader("origin", "https://smart.gardena.com");
            request.AddHeader("referer", "https://smart.gardena.com/");

            var settings = new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()};
            var json = JsonConvert.SerializeObject(new TokenRequestDto(_username, _password), settings);
            request.AddParameter("application/json", json, "application/json", ParameterType.RequestBody);

            var token = await _authClient.PostTaskAsync<TokenResponseDto>(request);

            _token = new Token
            {
                UserId = token.Data.Attributes.UserId,
                AccessToken = token.Data.Id,
                RefreshToken = token.Data.Attributes.RefreshToken,
                Expires = DateTime.UtcNow.AddSeconds(token.Data.Attributes.ExpiresIn)
            };
        }

        private async Task RefreshTokenAsync()
        {
            if ((_token.Expires - DateTime.UtcNow).TotalMinutes > 1.5)
            {
                return;
            }

            await LoginAsync();
        }
    }
}
