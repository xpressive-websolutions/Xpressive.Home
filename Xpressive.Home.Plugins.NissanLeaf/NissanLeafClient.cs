using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using Serilog;

namespace Xpressive.Home.Plugins.NissanLeaf
{
    internal sealed class NissanLeafClient : INissanLeafClient
    {
        private readonly IBlowfishEncryptionService _encryptionService;
        private readonly RestClient _restClient;
        private string _basePrm;
        private string _timezone;

        public NissanLeafClient(IBlowfishEncryptionService encryptionService)
        {
            _encryptionService = encryptionService;
            _restClient = new RestClient("https://gdcportalgw.its-mo.com/gworchest_160803A/gdc/");
        }

        public async Task<bool> InitAsync()
        {
            try
            {
                var request = new RestRequest("InitialApp.php");
                request.AddParameter("initial_app_strings", "geORNtsZe5I4lRGjG9GZiA");
                request.OnBeforeDeserialization = restResponse => { restResponse.ContentType = "application/json"; };
                var response = await _restClient.ExecutePostTaskAsync<InitialResponse>(request);
                _basePrm = response.Data.baseprm;
                return !string.IsNullOrEmpty(_basePrm);
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                return false;
            }
        }

        public async Task<List<NissanLeafDevice>> LoginAsync(string username, string password)
        {
            try
            {
                var request = new RestRequest("UserLoginRequest.php");
                request.AddParameter("RegionCode", "NE");
                request.AddParameter("UserId", username);
                request.AddParameter("Password", _encryptionService.Encrypt(password, _basePrm));
                request.AddParameter("initial_app_strings", "geORNtsZe5I4lRGjG9GZiA");

                var response = await _restClient.ExecutePostTaskAsync<LoginResponse>(request);
                var devices = new List<NissanLeafDevice>();
                _timezone = response.Data.CustomerInfo.Timezone;

                foreach (var vehicleInfo in response.Data.VehicleInfoList.vehicleInfo)
                {
                    var device = new NissanLeafDevice(
                        vehicleInfo.vin,
                        response.Data.vehicle.profile.dcmId,
                        vehicleInfo.nickname,
                        response.Data.vehicle.profile.modelyear)
                    {
                        CustomSessionId = vehicleInfo.custom_sessionid
                    };
                    devices.Add(device);
                }

                return devices;
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                return new List<NissanLeafDevice>(0);
            }
        }

        public async Task<BatteryStatus> GetBatteryStatusAsync(NissanLeafDevice device, CancellationToken cancellationToken)
        {
            try
            {
                var checkRequest = new RestRequest("BatteryStatusCheckRequest.php");
                checkRequest.AddParameter("RegionCode", "NE");
                checkRequest.AddParameter("VIN", device.Vin);
                checkRequest.AddParameter("custom_sessionid", device.CustomSessionId);

                var checkResponse = await _restClient.PostAsync<BatteryStatusCheckResponse>(checkRequest);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var resultRequest = new RestRequest("BatteryStatusCheckResultRequest.php");
                    resultRequest.AddParameter("RegionCode", "NE");
                    resultRequest.AddParameter("VIN", device.Vin);
                    resultRequest.AddParameter("custom_sessionid", device.CustomSessionId);
                    resultRequest.AddParameter("resultKey", checkResponse.resultKey);

                    var resultResponse = await _restClient.PostAsync<BatteryStatusResultResponse>(resultRequest);

                    if (resultResponse.responseFlag == "1")
                    {
                        return CreateBatteryStatus(resultResponse);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ContinueWith(_ => { });
                }

                return null;
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                return null;
            }
        }

        //public async Task GetClimateControlStatusAsync(NissanLeafDevice device)
        //{
        //    var request = new RestRequest("RemoteACRecordsRequest.php");
        //    request.AddParameter("RegionCode", "NE");
        //    request.AddParameter("DCMID", device.DcmId);
        //    request.AddParameter("VIN", device.Vin);
        //    request.AddParameter("custom_sessionid", device.CustomSessionId);

        //    var response = await _restClient.PostTaskAsync<>(request);
        //}

        public async Task ActivateClimateControl(NissanLeafDevice device)
        {
            var request = new RestRequest("ACRemoteRequest.php");
            request.AddParameter("RegionCode", "NE");
            request.AddParameter("VIN", device.Vin);
            request.AddParameter("custom_sessionid", device.CustomSessionId);

            await _restClient.ExecutePostTaskAsync(request);
        }

        public async Task DeactivateClimateControl(NissanLeafDevice device)
        {
            var request = new RestRequest("ACRemoteOffRequest.php");
            request.AddParameter("RegionCode", "NE");
            request.AddParameter("VIN", device.Vin);
            request.AddParameter("custom_sessionid", device.CustomSessionId);

            await _restClient.ExecutePostTaskAsync(request);
        }

        public async Task StartCharging(NissanLeafDevice device)
        {
            var request = new RestRequest("BatteryRemoteChargingRequest.php");
            request.AddParameter("RegionCode", "NE");
            request.AddParameter("VIN", device.Vin);
            request.AddParameter("custom_sessionid", device.CustomSessionId);
            request.AddParameter("tz", _timezone);
            request.AddParameter("ExecuteTime", DateTime.Today.ToString("yyyy-MM-dd"));

            await _restClient.ExecutePostTaskAsync(request);
        }

        private static BatteryStatus CreateBatteryStatus(BatteryStatusResultResponse response)
        {
            var batteryStatus = new BatteryStatus
            {
                PluginState = response.pluginState,
                ChargingState = response.chargeMode
            };

            double acOn;
            if (double.TryParse(response.cruisingRangeAcOn, out acOn))
            {
                batteryStatus.CruisingRangeAcOn = acOn;
            }

            double acOff;
            if (double.TryParse(response.cruisingRangeAcOff, out acOff))
            {
                batteryStatus.CruisingRangeAcOff = acOff;
            }

            double degradation;
            double capacity;
            if (double.TryParse(response.batteryDegradation, out degradation) &&
                double.TryParse(response.batteryCapacity, out capacity) &&
                capacity > 0)
            {
                batteryStatus.Power = degradation / capacity;
            }

            return batteryStatus;
        }

        private class BatteryStatusResultResponse
        {
            public int status { get; set; }
            public string message { get; set; }
            public string responseFlag { get; set; }
            public string batteryCapacity { get; set; }
            public string batteryDegradation { get; set; }
            public string chargeMode { get; set; }
            public string chargeStatus { get; set; }
            public string charging { get; set; }
            public string cruisingRangeAcOff { get; set; }
            public string cruisingRangeAcOn { get; set; }
            public string operationResult { get; set; }
            public string pluginState { get; set; }
            public BatteryStatusTimeToFull timeRequiredToFull { get; set; }
            public BatteryStatusTimeToFull timeRequiredToFull200 { get; set; }
            public BatteryStatusTimeToFull timeRequiredToFull200_6kW { get; set; }
            public string timeStamp { get; set; }
        }

        private class BatteryStatusTimeToFull
        {
            public string hours { get; set; }
            public string minutes { get; set; }
        }

        private class BatteryStatusCheckResponse
        {
            public int status { get; set; }
            public string resultKey { get; set; }
        }

        private class InitialResponse
        {
            public int status { get; set; }
            public string message { get; set; }
            public string baseprm { get; set; }
        }

        private class LoginResponse
        {
            public int status { get; set; }
            public string sessionId { get; set; }
            public LoginResponseVehicleInfoList VehicleInfoList { get; set; }
            public LoginResponseVehicle vehicle { get; set; }
            public string EncAuthToken { get; set; }
            public LoginResponseCustomerInfo CustomerInfo { get; set; }
        }

        private class LoginResponseVehicleInfoList
        {
            public List<LoginResponseVehicleInfo> VehicleInfo { get; set; }
            public List<LoginResponseVehicleInfo> vehicleInfo { get; set; }
        }

        private class LoginResponseVehicleInfo
        {
            public string nickname { get; set; }
            public string telematicsEnabled { get; set; }
            public string vin { get; set; }
            public string custom_sessionid { get; set; }
        }

        private class LoginResponseVehicle
        {
            public LoginResponseVehicleProfile profile { get; set; }
        }

        private class LoginResponseVehicleProfile
        {
            public string vin { get; set; }
            public string encAuthToken { get; set; }
            public string dcmId { get; set; }
            public string nickname { get; set; }
            public string modelyear { get; set; }
        }

        private class LoginResponseCustomerInfo
        {
            public string UserId { get; set; }
            public string Language { get; set; }
            public string Timezone { get; set; }
            public string RegionCode { get; set; }
            public string OwnerId { get; set; }
            public string EMailAddress { get; set; }
            public string Nickname { get; set; }
            public string Country { get; set; }
            public string VehicleImage { get; set; }
            public string UserVehicleBoundDurationSec { get; set; }
            public LoginResponseCustomerInfoVehicleInfo VehicleInfo { get; set; }
        }

        private class LoginResponseCustomerInfoVehicleInfo
        {
            public string VIN { get; set; }
            public string DCMID { get; set; }
            public string SIMID { get; set; }
            public string NAVIID { get; set; }
            public string EncryptedNAVIID { get; set; }
            public string MSN { get; set; }
            public string LastVehicleLoginTime { get; set; }
            public string UserVehicleBoundTime { get; set; }
            public string LastDCMUseTime { get; set; }
        }
    }
}
