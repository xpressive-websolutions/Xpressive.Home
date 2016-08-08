(function () {

    var xha = angular.module("admin", ["ngRoute", "ui.bootstrap"]);

    xha.config(function($routeProvider) {
        $routeProvider
            .when("/", {
                templateUrl : "/app/admin/devices.html",
                controller  : "deviceController"
            })
            .when("/scripts", {
                templateUrl: "/app/admin/scripts.html",
                controller: "scriptController"
            })
            .when("/schedules", {
                templateUrl: "/app/admin/schedules.html",
                controller: "scheduleController"
            });
    });

    xha.controller("navigationController", ["$rootScope", "$location", function ($rootScope, $location) {
        var c = this;

        $rootScope.$on("$locationChangeSuccess", function() {
            c.path = $location.path();
        });
    }]);

    xha.controller("deviceController", ["$scope", "$log", "$http", "$uibModal", function ($scope, $log, $http, $uibModal) {
        var map = {};
        $scope.gateways = [];
        $scope.gatewaysWithFactory = [];

        var getDevices = function (gateway) {
            $http.get("/api/v1/gateway/" + gateway.name, { cache: false }).then(function (deviceResult) {
                var devices = _.sortBy(deviceResult.data, function (d) { return d.name; });
                gateway.devices = devices;
            });
        };

        $http.get("/api/v1/gateway", { cache: false }).then(function(result) {
            var gateways = _.sortBy(result.data, function (g) { return g.name; });

            _.each(gateways, function (g) {
                var gateway = {
                    name: g.name,
                    canCreateDevices: g.canCreateDevices,
                    devices: []
                };

                map[g.name] = gateway;

                $scope.gateways.push(gateway);

                if (g.canCreateDevices) {
                    $scope.gatewaysWithFactory.push(gateway.name);
                }
            });

            _.each($scope.gateways, getDevices);
        });

        $scope.addDevice = function(gatewayName) {
            $http.get("/api/v1/gateway/" + gatewayName + "/empty").then(function(result) {
                var modal = $uibModal.open({
                    animation: false,
                    templateUrl: 'createDeviceDialog.html',
                    controller: 'createDeviceController',
                    resolve: {
                        device: function () {
                            return result.data;
                        }
                    }
                });

                modal.result.then(function(device) {
                    $http.post("/api/v1/gateway/" + gatewayName, device).then(function() {
                        var gateway = map[gatewayName];
                        getDevices(gateway);
                    });
                });
            });
        };
    }]);

})();
